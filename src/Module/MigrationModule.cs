using System;
using System.Windows.Forms;
using log4net;
using Sage.Platform.Application;
using Sage.Platform.Application.UI;
using Sage.Platform.Configuration;
using Sage.Platform.Data;
using Sage.Platform.IDEModule;
using Sage.Platform.Orm.Entities;
using Sage.Platform.Projects.Interfaces;
using Sage.SalesLogix.Migration.Orm;

namespace Sage.SalesLogix.Migration.Module
{
    public sealed class MigrationModule : ModuleInit<MigrationWorkItem>, IModuleConfigurationProvider
    {
        private const string SageSalesLogixReference = @"%BASEBUILDPATH%\assemblies\Sage.SalesLogix.dll";

        public const string STE_REPORTSWINDOWLOADED = "ReportsWindowLoaded";
        public const string CMD_SHOWMIGRATIONTOOL = "cmd://MigrationModule/ShowMigrationTool";
        public const string CMD_SHOWMIGRATIONREPORTS = "cmd://MigrationModule/ShowMigrationReports";
        public const string CMD_VIEWREPORT = "cmd://MigrationModule/ViewReport";
        public const string CMD_DELETEREPORT = "cmd://MigrationModule/DeleteReport";
        public const string EVT_MIGRATIONCOMPLETE = "evt://MigrationModule/MigrationComplete";
        public const string CTX_REPORT = "ctx://MigrationModule/Report";

        private IProjectContextService _projectContext;
        private IEditItemService _editItemService;
        private MainForm _mainForm;
        private MigrationReportsWindow _reportsWindow;
        private IWorkspace _dockingWorkspace;

        #region IModuleConfigurationProvider Members

        public ModuleConfiguration GetConfiguration()
        {
            return ModuleConfiguration.LoadFromResource(GetType().Namespace + ".ModuleConfig.xml", GetType().Assembly);
        }

        #endregion

        [ServiceDependency]
        public ConfigurationManager ConfigurationManager
        {
            set
            {
                UpdateHibernateConfig(value);
                UpdateSnippetConfig(value);
            }
        }

        [ServiceDependency]
        public IProjectContextService ProjectContext
        {
            set { _projectContext = value; }
        }

        [ServiceDependency]
        public IEditItemService EditItemService
        {
            set { _editItemService = value; }
        }

        private static void UpdateHibernateConfig(ConfigurationManager manager)
        {
            if (!manager.IsConfigurationTypeRegistered(typeof (HibernateConfiguration)))
            {
                manager.RegisterConfigurationType(typeof (HibernateConfiguration));
            }

            HibernateConfiguration config = manager.GetConfiguration<HibernateConfiguration>();
            string assemblyName = typeof (OrmProject).Assembly.GetName().Name;

            if (!config.MappingAssemblies.Contains(assemblyName))
            {
                config.MappingAssemblies.Add(assemblyName);
                manager.WriteConfiguration(config);
            }
        }

        private static void UpdateSnippetConfig(ConfigurationManager manager)
        {
            CodeSnippetConfiguration config = CodeSnippetManager.GetConfiguration();

            if (!config.DefaultReferences.Contains(SageSalesLogixReference))
            {
                if (!manager.IsConfigurationTypeRegistered(typeof (CodeSnippetConfiguration)))
                {
                    manager.RegisterConfigurationType(typeof (CodeSnippetConfiguration));
                }

                config.DefaultReferences.Add(SageSalesLogixReference);
                manager.WriteConfiguration(config);
            }
        }

        protected override void Load()
        {
            LogManager.GetLogger("Sage.Modules").Info("Loading " + ToString());
            _dockingWorkspace = ModuleWorkItem.Workspaces[UriConstants.WORKSPACE_DOCKING];
            _dockingWorkspace.SmartPartActivated += DockingWorkspace_SmartPartActivated;
            _dockingWorkspace.SmartPartClosing += DockingWorkspace_SmartPartClosing;
            ModuleWorkItem.Terminating += ModuleWorkItem_Terminating;
            ModuleWorkItem.ID = "750b3466-5398-4497-b4cc-92207620c43a";
            ModuleWorkItem.Load();
            ReloadWindowState();

            //workaround: SynchronizationContext.Current is now in a correct state
            ParentWorkItem.EventTopics[EVT_MIGRATIONCOMPLETE].AddSubscription(
                this,
                "MigrationComplete",
                new[] {typeof (object), typeof (MigrationCompleteEventArgs)},
                ParentWorkItem,
                ThreadOption.UserInterface);
        }

        private void ModuleWorkItem_Terminating(object sender, EventArgs e)
        {
            ModuleWorkItem.Save();
        }

        private void DockingWorkspace_SmartPartActivated(object sender, WorkspaceEventArgs e)
        {
            if (e.SmartPart is MigrationReportsWindow)
            {
                ModuleWorkItem.State[STE_REPORTSWINDOWLOADED] = true;
            }
        }

        private void DockingWorkspace_SmartPartClosing(object sender, WorkspaceCancelEventArgs e)
        {
            if (e.SmartPart is MigrationReportsWindow)
            {
                ModuleWorkItem.State[STE_REPORTSWINDOWLOADED] = false;
            }
        }

        private void ReloadWindowState()
        {
            bool? loaded = ModuleWorkItem.State[STE_REPORTSWINDOWLOADED] as bool?;

            if (loaded != null && loaded.Value)
            {
                ShowMigrationReports(null, null);
            }
        }

        [CommandHandler(CMD_SHOWMIGRATIONTOOL)]
        public void ShowMigrationTool(object sender, EventArgs e)
        {
            if (_mainForm == null)
            {
                _mainForm = ModuleWorkItem.SmartParts.AddNew<MainForm>();
            }

            _mainForm.ShowDialog();
        }

        [CommandHandler(CMD_SHOWMIGRATIONREPORTS)]
        public void ShowMigrationReports(object sender, EventArgs e)
        {
            if (_reportsWindow == null)
            {
                _reportsWindow = ModuleWorkItem.SmartParts.AddNew<MigrationReportsWindow>();
            }

            _dockingWorkspace.Show(_reportsWindow);
            _dockingWorkspace.Activate(_reportsWindow);
        }

        [CommandHandler(CMD_VIEWREPORT)]
        public void ViewReport(object sender, EventArgs e)
        {
            MigrationReport report = _reportsWindow.SelectedReport;

            if (report != null)
            {
                EditItem(report);
            }
        }

        [CommandHandler(CMD_DELETEREPORT)]
        public void DeleteReport(object sender, EventArgs e)
        {
            _reportsWindow.DeleteSelected();
        }

        [EventSubscription(EVT_MIGRATIONCOMPLETE, Thread=ThreadOption.UserInterface)]
        public void MigrationComplete(object sender, MigrationCompleteEventArgs e)
        {
            EditItem(e.Report);
        }

        private void EditItem(IModelItem item)
        {
            if (_mainForm.InvokeRequired)
            {
                _mainForm.Invoke(new Action<IModelItem>(EditItem), item);
            }

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                _editItemService.EditItem(item.Model.GetAsPersistentObject(item), ParentWorkItem, _projectContext.ActiveProject.InstanceId);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        public override string ToString()
        {
            return "Migration Module";
        }
    }
}