using Sage.Platform.Projects.Interfaces;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration
{
    public interface IExtendedLog
    {
        Plugin SourcePlugin { get; set; }
        IModelItem GeneratedItem { get; set; }

        void SetGeneratedItemMapping(Plugin source, string url);

        void Info(string message, params object[] args);
        void Info(bool persist, string message, params object[] args);
        void Warn(string message, params object[] args);
        void Warn(IModelItem item, string message, params object[] args);
        void Error(string message, params object[] args);
        void Error(IModelItem item, string message, params object[] args);
    }
}