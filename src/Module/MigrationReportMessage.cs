using Sage.Platform.Application.Services;

namespace Sage.SalesLogix.Migration.Module
{
    public sealed class MigrationReportMessage
    {
        private BuildMessageType _type;
        private string _message;
        private string _sourceType;
        private string _sourceName;
        private string _generatedItem;

        public MigrationReportMessage() {}

        public MigrationReportMessage(BuildMessageType type, string message, string sourceType, string sourceName, string generatedItem)
        {
            _type = type;
            _message = message;
            _sourceType = sourceType;
            _sourceName = sourceName;
            _generatedItem = generatedItem;
        }

        public BuildMessageType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public string SourceType
        {
            get { return _sourceType; }
            set { _sourceType = value; }
        }

        public string SourceName
        {
            get { return _sourceName; }
            set { _sourceName = value; }
        }

        public string GeneratedItem
        {
            get { return _generatedItem; }
            set { _generatedItem = value; }
        }
    }
}