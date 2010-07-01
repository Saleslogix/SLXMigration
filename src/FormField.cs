using System;
using Sage.Platform.Exceptions;

namespace Sage.SalesLogix.Migration
{
    public sealed class FormField
    {
        private readonly string _name;
        private readonly Type _type;

        public FormField(string name, Type type)
        {
            Guard.ArgumentNotNullOrEmptyString(name, "name");
            Guard.ArgumentNotNull(type, "type");

            _name = name;
            _type = type;
        }

        public string Name
        {
            get { return _name; }
        }

        public Type Type
        {
            get { return _type; }
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
                FormField castObj = obj as FormField;
                return (castObj != null &&
                        _name == castObj._name &&
                        _type == castObj._type);
            }
        }

        public override int GetHashCode()
        {
            return _name.GetHashCode() ^ _type.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", _name, _type);
        }
    }
}