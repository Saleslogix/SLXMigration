using log4net;
using Sage.Platform.Application.Services;
using Sage.Platform.Projects.Interfaces;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration.Module
{
    public sealed class ExtendedLog : IExtendedLog
    {
        private readonly ILog _outputLog;
        private readonly MigrationReport _report;
        private Plugin _sourcePlugin;
        private IModelItem _generatedItem;

        public ExtendedLog(ILog outputLog, MigrationReport report)
        {
            _outputLog = outputLog;
            _report = report;
        }

        #region IExtendedLog Members

        public Plugin SourcePlugin
        {
            get { return _sourcePlugin; }
            set { _sourcePlugin = value; }
        }

        public IModelItem GeneratedItem
        {
            get { return _generatedItem; }
            set { _generatedItem = value; }
        }

        public void SetGeneratedItemMapping(Plugin source, string url)
        {
            _report.GeneratedItemMappings[source.LongName] = url;
        }

        public void Info(string message, params object[] args)
        {
            Info(false, message, args);
        }

        public void Info(bool persist, string message, params object[] args)
        {
            message = string.Format(message, args);
            _outputLog.Info(message);

            if (persist)
            {
                AttachMessage(message, BuildMessageType.Info, _generatedItem);
            }
        }

        public void Warn(string message, params object[] args)
        {
            WarnInternal(string.Format(message, args), _generatedItem);
        }

        public void Warn(IModelItem item, string message, params object[] args)
        {
            WarnInternal(string.Format(message, args), item);
        }

        public void Error(string message, params object[] args)
        {
            ErrorInternal(string.Format(message, args), _generatedItem);
        }

        public void Error(IModelItem item, string message, params object[] args)
        {
            ErrorInternal(string.Format(message, args), item);
        }

        #endregion

        private void WarnInternal(string message, IModelItem item)
        {
            _outputLog.Warn(FormatLogMessage(message));
            AttachMessage(message, BuildMessageType.Warning, item);
        }

        private void ErrorInternal(string message, IModelItem item)
        {
            _outputLog.Error(FormatLogMessage(message));
            AttachMessage(message, BuildMessageType.Error, item);
        }

        private string FormatLogMessage(string message)
        {
            return (_sourcePlugin != null
                        ? string.Format("{0} - {1}", _sourcePlugin.LongName, message)
                        : message);
        }

        private void AttachMessage(string message, BuildMessageType type, IModelItem item)
        {
            _report.Messages.Add(
                new MigrationReportMessage(
                    type,
                    message,
                    (_sourcePlugin != null ? _sourcePlugin.Type.ToString() : null),
                    (_sourcePlugin != null ? string.Format("{0}:{1} ({2} {3})", _sourcePlugin.Family, _sourcePlugin.Name, _sourcePlugin.Company, _sourcePlugin.CompanyVersion) : null),
                    (item != null ? item.Url : null)));
        }
    }
}