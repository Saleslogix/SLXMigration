using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using Sage.Platform.BundleModel;
using Sage.Platform.Orm.Entities;
using Sage.Platform.Projects;
using Sage.Platform.Projects.Localization;
using Sage.Platform.WebPortal.Design;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration
{
    public sealed class MigrationContext
    {
        private readonly MigrationSettings _settings;
        private readonly OrmPackage _package;
        private readonly PortalApplication _portal;
        private readonly BundleManifest _manifest;
        private readonly IVSProject _vsProject;
        private readonly CodeDomProvider _codeProvider;
        private readonly StrongNameKeyPair _keyPair;
        private readonly IExtendedLog _log;
        private readonly IOperationStatus _status;
        private readonly ICollection<Plugin> _plugins;
        private readonly IDictionary<string, FormInfo> _forms;
        private readonly IDictionary<string, MainViewInfo> _mainViews;
        private readonly ICollection<NavigationInfo> _navigation;
        private readonly IDictionary<string, ScriptInfo> _scripts;
        private readonly IDictionary<string, TableInfo> _tables;
        private readonly IDictionary<string, OrmEntity> _entities;
        private readonly IDictionary<DataPathJoin, RelationshipInfo> _relationships;
        private readonly ICollection<LinkedFile> _linkedFiles;
        private readonly IDictionary<string, string> _localizedStrings;
        private readonly IDictionary<string, string> _references;
        private readonly ICollection<SmartPartMapping> _smartParts;
        private readonly IDictionary<DataPathJoin, DataPathJoin> _secondaryJoins;
        private ProjectResourceManager _globalImageResourceManager;
        private bool _isNewPackage;
        private bool _isNewPortal;
        private bool _requiresInterfaceBuild;

        public MigrationContext(
            MigrationSettings settings,
            OrmPackage package,
            PortalApplication portal,
            BundleManifest manifest,
            IVSProject vsProject,
            CodeDomProvider codeProvider,
            StrongNameKeyPair keyPair,
            IExtendedLog log,
            IOperationStatus status,
            ICollection<Plugin> plugins)
        {
            _settings = settings;
            _package = package;
            _portal = portal;
            _manifest = manifest;
            _vsProject = vsProject;
            _codeProvider = codeProvider;
            _keyPair = keyPair;
            _log = log;
            _status = status;
            _plugins = plugins;
            _forms = new Dictionary<string, FormInfo>(StringComparer.InvariantCultureIgnoreCase);
            _mainViews = new Dictionary<string, MainViewInfo>(StringComparer.InvariantCultureIgnoreCase);
            _navigation = new List<NavigationInfo>();
            _scripts = new Dictionary<string, ScriptInfo>(StringComparer.InvariantCultureIgnoreCase);
            _tables = new Dictionary<string, TableInfo>(StringComparer.InvariantCultureIgnoreCase);
            _entities = new Dictionary<string, OrmEntity>(StringComparer.InvariantCultureIgnoreCase);
            _relationships = new Dictionary<DataPathJoin, RelationshipInfo>();
            _linkedFiles = new List<LinkedFile>();
            _localizedStrings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            _references = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            _smartParts = new List<SmartPartMapping>();
            _secondaryJoins = new Dictionary<DataPathJoin, DataPathJoin>();
        }

        public MigrationSettings Settings
        {
            get { return _settings; }
        }

        public OrmPackage Package
        {
            get { return _package; }
        }

        public PortalApplication Portal
        {
            get { return _portal; }
        }

        public BundleManifest Manifest
        {
            get { return _manifest; }
        }

        public IVSProject VSProject
        {
            get { return _vsProject; }
        }

        public CodeDomProvider CodeProvider
        {
            get { return _codeProvider; }
        }

        public StrongNameKeyPair KeyPair
        {
            get { return _keyPair; }
        }

        public IExtendedLog Log
        {
            get { return _log; }
        }

        public IOperationStatus Status
        {
            get { return _status; }
        }

        public ICollection<Plugin> Plugins
        {
            get { return _plugins; }
        }

        public IDictionary<string, FormInfo> Forms
        {
            get { return _forms; }
        }

        public IDictionary<string, MainViewInfo> MainViews
        {
            get { return _mainViews; }
        }

        public ICollection<NavigationInfo> Navigation
        {
            get { return _navigation; }
        }

        public IDictionary<string, ScriptInfo> Scripts
        {
            get { return _scripts; }
        }

        public IDictionary<string, TableInfo> Tables
        {
            get { return _tables; }
        }

        public IDictionary<string, OrmEntity> Entities
        {
            get { return _entities; }
        }

        public IDictionary<DataPathJoin, RelationshipInfo> Relationships
        {
            get { return _relationships; }
        }

        public ICollection<LinkedFile> LinkedFiles
        {
            get { return _linkedFiles; }
        }

        public IDictionary<string, string> LocalizedStrings
        {
            get { return _localizedStrings; }
        }

        public IDictionary<string, string> References
        {
            get { return _references; }
        }

        public ICollection<SmartPartMapping> SmartParts
        {
            get { return _smartParts; }
        }

        public IDictionary<DataPathJoin, DataPathJoin> SecondaryJoins
        {
            get { return _secondaryJoins; }
        }

        public ProjectResourceManager GlobalImageResourceManager
        {
            get { return _globalImageResourceManager ?? (_globalImageResourceManager = ProjectResourceManager.LocalizationService.GetGlobalImageResourceManager()); }
        }

        public bool RequiresInterfaceBuild
        {
            get { return _requiresInterfaceBuild; }
            set { _requiresInterfaceBuild = value; }
        }

        public bool IsNewPackage
        {
            get { return _isNewPackage; }
            set { _isNewPackage = value; }
        }

        public bool IsNewPortal
        {
            get { return _isNewPortal; }
            set { _isNewPortal = value; }
        }
    }
}