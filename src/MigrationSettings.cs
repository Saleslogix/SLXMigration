using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Iesi.Collections.Generic;
using Sage.Platform.Application;
using Sage.Platform.ComponentModel;
using Sage.Platform.Configuration;
using Sage.Platform.Projects;

namespace Sage.SalesLogix.Migration
{
    [XmlRoot("MigrationSettings")]
    [ConfigurationType("cfg://{Applications}/{Migration}", "MigrationSettings")]
    [ConfigurationPolicies(ConfigurationPolicies.User)]
    public sealed class MigrationSettings : EditableBase, ICloneable
    {
        private ConfigurationManager _manager;
        private string _legacyProject;
        private ICollection<PluginInfo> _legacyPlugins;
        private string _packageName = "SalesLogix Application Entities";
        private string _portalName = "Sage SalesLogix";
        private string _manifestName;
        private string _mainTable;
        private string _namespace;
        private string _outputDirectory;
        private bool _processScripts = true;
        private string _vsProjectName;
        private Language _language;
        private string _customCodeProvider;
        private string _keyPairFileName;
        private bool _setRowAndColumnSizes;
        private bool _mergeLabels = true;

        public string LegacyProject
        {
            get { return _legacyProject; }
            set { _legacyProject = value; }
        }

        public bool MergeLabels
        {
            get { return _mergeLabels; }
            set { _mergeLabels = value; }
        }

        public ICollection<PluginInfo> LegacyPlugins
        {
            get { return _legacyPlugins ?? (_legacyPlugins = new HashedSet<PluginInfo>()); }
        }

        public string PackageName
        {
            get { return _packageName; }
            set { _packageName = value; }
        }

        public string PortalName
        {
            get { return _portalName; }
            set { _portalName = value; }
        }

        public string ManifestName
        {
            get { return _manifestName; }
            set { _manifestName = value; }
        }

        public string MainTable
        {
            get { return _mainTable; }
            set { _mainTable = value; }
        }

        public string Namespace
        {
            get { return _namespace; }
            set { _namespace = value; }
        }

        public string OutputDirectory
        {
            get { return _outputDirectory; }
            set { _outputDirectory = value; }
        }

        public bool ProcessScripts
        {
            get { return _processScripts; }
            set { _processScripts = value; }
        }

        public string VSProjectName
        {
            get { return _vsProjectName; }
            set { _vsProjectName = value; }
        }

        public Language Language
        {
            get { return _language; }
            set
            {
                Guard.EnumValueIsDefined(typeof (Language), value, "value");
                _language = value;
            }
        }

        public string CustomCodeProvider
        {
            get { return _customCodeProvider; }
            set { _customCodeProvider = value; }
        }

        public string KeyPairFileName
        {
            get { return _keyPairFileName; }
            set { _keyPairFileName = value; }
        }

        //no longer used
        public bool SetRowAndColumnSizes
        {
            get { return _setRowAndColumnSizes; }
            set { _setRowAndColumnSizes = value; }
        }

        public void Save()
        {
            if (_manager != null)
            {
                _manager.WriteConfiguration(this);
            }

            AcceptChanges();
        }

        protected override bool DoValidation(ErrorMessageList errorList)
        {
            bool valid = true;

            if (string.IsNullOrEmpty(LegacyProject) && LegacyPlugins.Count == 0)
            {
                valid = false;
                AddError(errorList, "SolutionFolder", "Please specify either a legacy project or some discrete plugins.");
            }

            return (base.DoValidation(errorList) & valid);
        }

        public static MigrationSettings GetCurrentMigrationSettings(WorkItem workItem)
        {
            ConfigurationManager manager = workItem.Services.Get<ConfigurationManager>(true);
            Type type = typeof (MigrationSettings);

            if (!manager.IsConfigurationTypeRegistered(type))
            {
                manager.RegisterConfigurationType(type);
            }

            MigrationSettings settings = manager.GetConfiguration<MigrationSettings>(false);

            if (settings == null)
            {
                settings = new MigrationSettings();
                manager.WriteConfiguration(settings);
            }

            settings._manager = manager;
            return settings;
        }

        #region ICloneable Members

        public object Clone()
        {
            MigrationSettings settings = (MigrationSettings) MemberwiseClone();
            settings._manager = null;
            settings._legacyPlugins = new HashedSet<PluginInfo>(_legacyPlugins);
            return settings;
        }

        #endregion
    }
}