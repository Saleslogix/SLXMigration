using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using log4net;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using Sage.Platform.AdminModule;
using Sage.Platform.Application;
using Sage.Platform.Application.UI;
using Sage.Platform.BundleModel;
using Sage.Platform.Data;
using Sage.Platform.Orm.CodeGen;
using Sage.Platform.Orm.Entities;
using Sage.Platform.Projects.Interfaces;
using Sage.Platform.VirtualFileSystem;
using Sage.Platform.WebPortal.Design;
using Sage.SalesLogix.Migration.Forms;
using Sage.SalesLogix.Migration.Forms.Services;
using Sage.SalesLogix.Migration.Module.Services;
using Sage.SalesLogix.Migration.Module.VSProject;
using Sage.SalesLogix.Migration.Orm;
using Sage.SalesLogix.Migration.Script;
using Sage.SalesLogix.Migration.Script.CodeDom;
using Sage.SalesLogix.Migration.Services;
using Sage.SalesLogix.Plugins;
using _BundleModel=Sage.Platform.BundleModel.BundleModel;
using PortalService=Sage.SalesLogix.Migration.Module.Services.PortalService;

namespace Sage.SalesLogix.Migration.Module
{
    public sealed class MigrationWorkItem : UIWorkItem
    {
        private readonly ILog _outputLog = LogManager.GetLogger("Sage.Migration");
        private readonly StepHandler[] _steps;

        public MigrationWorkItem()
        {
            _steps = new StepHandler[]
                {
                    Initialize,
                    Parse,
                    ProcessEntities,
                    Build,
                    Persist,
                    UpdatePortal,
                    GenerateProjectFile,
                    GenerateBundle,
                    SaveReport
                };
        }

        private IMigrationContextHolderService _contextHolder;
        private IOrmEntityLoaderService _entityLoader;
        private IEntityService _entityPersistor;
        private IPortalService _portalUpdater;
        private IVSProjectService _vsProjectGenerator;
        private IManifestService _manifestGenerator;
        private IHierarchyNodeService _hierarchyNodeService;
        private IProjectContextService _projectContext;
        private IMigrationSupport[] _migrators;
        private MigrationModel _migrationModel;

        [ServiceDependency]
        public IProjectContextService ProjectContext
        {
            set
            {
                _projectContext = value;
                _projectContext.ActiveProjectChanged += delegate
                    {
                        ActiveProjectChanged();
                    };
                ActiveProjectChanged();
            }
        }

        private void ActiveProjectChanged()
        {
            Guid modelId = typeof (MigrationModel).GUID;

            if (_projectContext.ActiveProject != null && !_projectContext.ActiveProject.Models.ContainsKey(modelId))
            {
                _migrationModel = new MigrationModel(_projectContext.ActiveProject);
                _projectContext.ActiveProject.Models.Add(modelId, _migrationModel);
            }
        }

        protected override void OnInitialized()
        {
            //general services
            _contextHolder = Services.AddNew<MigrationContextHolderService, IMigrationContextHolderService>();
            _entityLoader = Services.AddNew<OrmEntityLoaderService, IOrmEntityLoaderService>();
            _entityPersistor = Services.AddNew<EntityService, IEntityService>();
            _portalUpdater = Services.AddNew<PortalService, IPortalService>();
            _vsProjectGenerator = Services.AddNew<VSProjectService, IVSProjectService>();
            _manifestGenerator = Services.AddNew<ManifestService, IManifestService>();
            _hierarchyNodeService = Services.AddNew<HierarchyNodeService, IHierarchyNodeService>();

            //form services
            Services.AddNew<FormLayoutService, IFormLayoutService>();
            Services.AddNew<DataPathTranslationService, IDataPathTranslationService>();
            Services.AddNew<ComponentSimplificationService, IComponentSimplificationService>();
            Services.AddNew<FormSimplificationService, IFormSimplificationService>();
            Services.AddNew<FormFlatteningService, IFormFlatteningService>();
            Services.AddNew<ControlAlignmentService, IControlAlignmentService>();
            Services.AddNew<VisibilityDeterminationService, IVisibilityDeterminationService>();

            //TODO: script services

            _migrators = new IMigrationSupport[]
                {
                    BuildTransientItem<MainViewMigrationSupport>(),
                    BuildTransientItem<ToolbarMigrationSupport>(),
                    BuildTransientItem<FormMigrationSupport>(),
                    BuildTransientItem<LegacyFormMigrationSupport>(),
                    BuildTransientItem<ScriptMigrationSupport>()
                };

            base.OnInitialized();
        }

        private MigrationSettings _settings;
        private IOperationStatus _status;
        private IExtendedLog _log;
        private MigrationReport _report;

        public IExtendedLog Log
        {
            get { return _log; }
        }

        [EventPublication(MigrationModule.EVT_MIGRATIONCOMPLETE)]
        public event EventHandler<MigrationCompleteEventArgs> MigrationComplete;

        public void Execute(MigrationSettings settings, IOperationStatus status)
        {
            Guard.ArgumentNotNull(settings, "settings");
            Guard.ArgumentNotNull(status, "status");

            _settings = settings;
            _status = status;
            int stepCount = _steps.Length;

            if (_settings.ProcessScripts)
            {
                stepCount += (_migrators.Length - 1)*3;
            }
            else
            {
                stepCount += (_migrators.Length - 1)*2;
            }

            _status.Reset(stepCount);

            foreach (StepHandler step in _steps)
            {
                step();

                if (_status.IsCancelled)
                {
                    break;
                }
            }

            if (!_status.IsCancelled && MigrationComplete != null)
            {
                MigrationComplete(this, new MigrationCompleteEventArgs(_report));
            }

            _settings = null;
            _status = null;
        }

        private void Initialize()
        {
            using (_status.BeginStep("Initializing...", 11))
            {
                if (string.IsNullOrEmpty(_settings.LegacyProject) && _settings.LegacyPlugins.Count == 0)
                {
                    throw new MigrationException("No plugins specified");
                }

                if (!_status.Advance())
                {
                    return;
                }

                bool isNewPackage;
                OrmPackage package = GetPackage(out isNewPackage);

                if (!_status.Advance())
                {
                    return;
                }

                bool isNewPortal;
                PortalApplication portal = GetPortal(out isNewPortal);

                if (!_status.Advance())
                {
                    return;
                }

                BundleManifest manifest = GetBundle();

                if (!_status.Advance())
                {
                    return;
                }

                IVSProject vsProject = (_settings.ProcessScripts
                                            ? GetVSProject()
                                            : null);

                if (!_status.Advance())
                {
                    return;
                }

                CodeDomProvider codeProvider = GetCodeProvider();

                if (!_status.Advance())
                {
                    return;
                }

                StrongNameKeyPair keyPair = GetKeyPair();

                if (!_status.Advance())
                {
                    return;
                }

                ICollection<Plugin> plugins = GetPlugins();

                if (!_status.Advance())
                {
                    return;
                }

                _report = new MigrationReport();
                _report.Date = DateTime.Now;
                _report.Settings = (MigrationSettings) _settings.Clone();

                if (!_status.Advance())
                {
                    return;
                }

                _log = new ExtendedLog(_outputLog, _report);

                if (!_status.Advance())
                {
                    return;
                }

                if (!string.IsNullOrEmpty(_settings.OutputDirectory) && !Directory.Exists(_settings.OutputDirectory))
                {
                    Directory.CreateDirectory(_settings.OutputDirectory);
                }

                if (!_status.Advance())
                {
                    return;
                }

                _contextHolder.Context = new MigrationContext(
                    _settings,
                    package,
                    portal,
                    manifest,
                    vsProject,
                    codeProvider,
                    keyPair,
                    _log,
                    _status,
                    plugins);
                _contextHolder.Context.IsNewPackage = isNewPackage;
                _contextHolder.Context.IsNewPortal = isNewPortal;
            }
        }

        private void Parse()
        {
            foreach (IMigrationSupport migrator in _migrators)
            {
                if (_settings.ProcessScripts || !(migrator is ScriptMigrationSupport))
                {
                    migrator.Parse();
                }
            }
        }

        private void ProcessEntities()
        {
            _entityPersistor.PersistEntities();
        }

        private void Build()
        {
            foreach (IMigrationSupport migrator in _migrators)
            {
                if (_settings.ProcessScripts || !(migrator is ScriptMigrationSupport))
                {
                    migrator.Build();
                }
            }
        }

        private void Persist()
        {
            foreach (IMigrationSupport migrator in _migrators)
            {
                if (_settings.ProcessScripts || !(migrator is ScriptMigrationSupport))
                {
                    migrator.Persist();
                }
            }
        }

        private void UpdatePortal()
        {
            _portalUpdater.Update();
        }

        private void GenerateProjectFile()
        {
            if (_contextHolder.Context.Settings.ProcessScripts)
            {
                _vsProjectGenerator.Generate();
            }
        }

        private void GenerateBundle()
        {
            _manifestGenerator.Generate();
        }

        private void SaveReport()
        {
            using (_status.BeginStep("Saving report...", 1))
            {
                _migrationModel.AddReport(_report);
                _report.Validate();
                _report.Save();
            }
        }

        private OrmPackage GetPackage(out bool isNew)
        {
            OrmPackage package = _entityLoader.LoadPackage(_settings.PackageName);
            isNew = (package == null);

            if (isNew)
            {
                OrmModel ormModel = _projectContext.ActiveProject.Models.Get<OrmModel>();
                package = ormModel.CreateNewPackage();
                package.Name = _settings.PackageName;
                package.SetAssemblyName(_settings.Namespace);
                package.SetDefaultNamespace(_settings.Namespace);
                package.Validate();
                package.Save();
            }

            return package;
        }

        private PortalApplication GetPortal(out bool isNew)
        {
            string alias = StringUtils.ReplaceIllegalChars(_settings.PortalName);
            PortalApplication portal = PortalApplication.Get(alias);
            isNew = (portal == null);

            if (isNew)
            {
                alias = alias.Replace(" ", string.Empty).Replace("!", "_");
                portal = BuildTransientItem<PortalApplication>();
                portal.PortalAlias = alias;
                portal.PortalTitle = _settings.PortalName;
                portal.Validate();
                portal.Save();

                _hierarchyNodeService.InsertPortalNode(portal);

                using (Stream stream = new VirtualFileStream(string.Format(@"\Webroot\{0}\hibernate.xml", alias), VirtualFileMode.Create))
                {
                    new XmlSerializer(typeof (HibernateConfiguration)).Serialize(stream, new HibernateConfiguration());
                }
            }

            return portal;
        }

        private BundleManifest GetBundle()
        {
            _BundleModel model = _projectContext.ActiveProject.Models.Get<_BundleModel>();
            BundleManifest manifest = CollectionUtils.Find(
                model.BundleManifests,
                delegate(BundleManifest item)
                    {
                        return (StringUtils.CaseInsensitiveEquals(item.Name, _settings.ManifestName));
                    });

            if (manifest == null)
            {
                manifest = new BundleManifest(model);
                manifest.Name = _settings.ManifestName;
                manifest.AutoAddChildren = false;
                manifest.Validate();
                manifest.Save();
                model.BundleManifests.Add(manifest);

                _hierarchyNodeService.InsertBundleManifestNode(manifest);
            }

            return manifest;
        }

        private IVSProject GetVSProject()
        {
            IVSProject vsProject;

            if (_settings.Language == Language.VBNet)
            {
                vsProject = new VBNetProject(_settings.VSProjectName, _settings.Namespace);
            }
            else if (_settings.Language == Language.CSharp)
            {
                vsProject = new CSharpProject(_settings.VSProjectName, _settings.Namespace);
            }
            else
            {
                vsProject = new DefaultVSProject(_settings.VSProjectName, _settings.Namespace);
            }

            return vsProject;
        }

        private CodeDomProvider GetCodeProvider()
        {
            CodeDomProvider codeProvider;

            if (_settings.Language == Language.VBNet)
            {
                codeProvider = ExtendedCodeProviderManager.Create(typeof (VBCodeProvider));
            }
            else if (_settings.Language == Language.CSharp)
            {
                codeProvider = ExtendedCodeProviderManager.Create(typeof (CSharpCodeProvider));
            }
            else
            {
                Type customType = Type.GetType(_settings.CustomCodeProvider);

                if (customType == null)
                {
                    throw new MigrationException("Unable to load custom code provider type");
                }

                if (!typeof (CodeDomProvider).IsAssignableFrom(customType))
                {
                    throw new MigrationException("Custom code provider type does not implement CodeDomProvider");
                }

                codeProvider = (CodeDomProvider) Activator.CreateInstance(customType);
            }

            return codeProvider;
        }

        private StrongNameKeyPair GetKeyPair()
        {
            return (string.IsNullOrEmpty(_settings.KeyPairFileName)
                        ? null
                        : new StrongNameKeyPair(File.ReadAllBytes(_settings.KeyPairFileName)));
        }

        private ICollection<Plugin> GetPlugins()
        {
            ICollection<Plugin> plugins = new List<Plugin>();

            foreach (PluginInfo plugin in _settings.LegacyPlugins)
            {
                plugins.Add(Plugin.LoadByName(plugin.Name, plugin.Family, plugin.Type));
            }

            if (!string.IsNullOrEmpty(_settings.LegacyProject))
            {
                OrmProject project = _entityLoader.LoadProject(_settings.LegacyProject);

                if (project == null)
                {
                    throw new MigrationException("Legacy project not found");
                }
                else
                {
                    foreach (OrmProjectItem item in project.Items)
                    {
                        if (item.Plugin != null)
                        {
                            plugins.Add(item.Plugin);
                        }
                    }
                }
            }

            return plugins;
        }

        public void BuildInterfaces()
        {
            AdminModuleInit adminModule = Modules.Get<AdminModuleInit>();
            bool backgroundBuild = adminModule.BuildUsingBackgroundThread;
            adminModule.BuildUsingBackgroundThread = false;

            //bool clearLog = adminModule.ClearLogBeforeBuild;
            bool? clearLog = (bool?) adminModule.ModuleWorkItem.State["ste://EntityModel/ClearLogBeforeBuild"];
            //adminModule.ClearLogBeforeBuild = false;
            if (clearLog != null)
            {
                adminModule.ModuleWorkItem.State["ste://EntityModel/ClearLogBeforeBuild"] = false;
            }

            try
            {
                Commands[AdminModuleConstants.CMD_BUILD_INTERFACES].Execute();
            }
            finally
            {
                adminModule.BuildUsingBackgroundThread = backgroundBuild;

                //adminModule.ClearLogBeforeBuild = clearLog;
                if (clearLog != null)
                {
                    adminModule.ModuleWorkItem.State["ste://EntityModel/ClearLogBeforeBuild"] = clearLog;
                }
            }
        }
    }
}