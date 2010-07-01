using System.IO;
using Sage.Platform.Application;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration
{
    public abstract class BasePluginInfo
    {
        private readonly Plugin _plugin;
        private string _fullName;
        private string _safeFamily;
        private string _safeName;
        private string _safeFullName;

        public BasePluginInfo(Plugin plugin)
        {
            Guard.ArgumentNotNull(plugin, "plugin");
            _plugin = plugin;
        }

        public Plugin Plugin
        {
            get { return _plugin; }
        }

        public string Family
        {
            get { return Plugin.Family; }
        }

        public string Name
        {
            get { return Plugin.Name; }
        }

        public string FullName
        {
            get { return _fullName ?? (_fullName = FormatFullName(Family, Name)); }
        }

        public string SafeFamily
        {
            get { return _safeFamily ?? (_safeFamily = StringUtils.ReplaceIllegalChars(Family)); }
        }

        public string SafeName
        {
            get { return _safeName ?? (_safeName = StringUtils.ReplaceIllegalChars(Name)); }
        }

        public string SafeFullName
        {
            get { return _safeFullName ?? (_safeFullName = FormatFullName(SafeFamily, SafeName)); }
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }
            else if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            else
            {
                BasePluginInfo castObj = obj as BasePluginInfo;
                return (castObj != null &&
                        Family == castObj.Family &&
                        Name == castObj.Name);
            }
        }

        public override int GetHashCode()
        {
            return Family.GetHashCode() ^ Name.GetHashCode();
        }

        public override string ToString()
        {
            return FullName;
        }

        public static string FormatFullName(string family, string name)
        {
            return string.Format("{0}_{1}", family, name);
        }
    }
}