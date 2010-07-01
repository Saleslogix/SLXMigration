using System;
using System.Collections.Generic;

namespace Sage.SalesLogix.Migration.Forms
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public class BuilderMappingAttribute : Attribute
    {
        private readonly string _name;

        public BuilderMappingAttribute(string name)
        {
            _name = name;
        }

        public virtual bool IsApplicable(string name, IDictionary<string, object> properties)
        {
            return (name == _name);
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
                BuilderMappingAttribute castObj = obj as BuilderMappingAttribute;
                return (castObj != null &&
                        _name == castObj._name);
            }
        }

        public override int GetHashCode()
        {
            return _name.GetHashCode();
        }
    }
}