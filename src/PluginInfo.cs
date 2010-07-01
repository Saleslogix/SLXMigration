using Sage.Platform.Exceptions;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration
{
    public sealed class PluginInfo
    {
        private readonly PluginType _type;
        private readonly string _family;
        private readonly string _name;

        public PluginInfo(PluginType type, string family, string name)
        {
            Guard.EnumValueIsDefined(typeof (PluginType), type, "type");
            Guard.ArgumentNotNullOrEmptyString(family, "family");
            Guard.ArgumentNotNullOrEmptyString(name, "name");

            _family = family;
            _name = name;
            _type = type;
        }

        public PluginType Type
        {
            get { return _type; }
        }

        public string Family
        {
            get { return _family; }
        }

        public string Name
        {
            get { return _name; }
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }
            else if (obj == null)
            {
                return false;
            }
            else
            {
                PluginInfo castObj = obj as PluginInfo;
                return (castObj != null &&
                        _type == castObj._type &&
                        _family == castObj._family &&
                        _name == castObj._name);
            }
        }

        public override int GetHashCode()
        {
            return _type.GetHashCode() ^ _family.GetHashCode() ^ _name.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}:{2}", _type, _family, _name);
        }
    }
}