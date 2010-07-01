using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Serialization;
using Sage.Platform.Projects;
using Sage.Platform.Projects.Interfaces;

namespace Sage.SalesLogix.Migration.Module
{
    public sealed class MigrationReport : ModelItemBase
    {
        private DateTime _date;
        private MigrationSettings _settings;
        private List<MigrationReportMessage> _messages;
        private IDictionary<string, string> _generatedItemMappings;

        public DateTime Date
        {
            get { return _date; }
            set
            {
                if (_date != value)
                {
                    _date = value;
                    NotifyPropertyChanged("Date");
                }
            }
        }

        public MigrationSettings Settings
        {
            get { return _settings; }
            set
            {
                if (_settings != value)
                {
                    _settings = value;
                    NotifyPropertyChanged("Settings");
                }
            }
        }

        public List<MigrationReportMessage> Messages
        {
            get { return (_messages ?? (_messages = new List<MigrationReportMessage>())); }
        }

        [XmlIgnore]
        public IDictionary<string, string> GeneratedItemMappings
        {
            get { return (_generatedItemMappings ?? (_generatedItemMappings = new Dictionary<string, string>())); }
        }

        #region ModelItemBase Members

        public override string FileName
        {
            get { return string.Format(@"{0}.{1}{2}", Settings.LegacyProject, Date.Ticks, MigrationModel.ReportExtension); }
        }

        public override Image Image
        {
            get { return base.Image; }
        }

        protected override IModelItem FindParent()
        {
            return null;
        }

        protected override string ContainerDirectory
        {
            get { return MigrationModel.ModelRootPath; }
        }

        public override void Save()
        {
            if (_messages != null && _generatedItemMappings != null)
            {
                foreach (MigrationReportMessage message in _messages)
                {
                    string generatedItem;

                    if (message.GeneratedItem == null &&
                        GeneratedItemMappings.TryGetValue(string.Format("[{0}] {1}", message.SourceType, message.SourceName), out generatedItem))
                    {
                        message.GeneratedItem = generatedItem;
                    }
                }
            }

            base.Save();
        }

        #endregion
    }
}