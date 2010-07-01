using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sage.SalesLogix.Migration.Forms
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class BuilderMappingByEqualityAttribute : BuilderMappingAttribute
    {
        private readonly string _key;
        private readonly object _value;

        public BuilderMappingByEqualityAttribute(string name, string key, object value)
            : base(name)
        {
            _key = key;
            _value = value;
        }

        public override bool IsApplicable(string name, IDictionary<string, object> properties)
        {
            if (base.IsApplicable(name, properties))
            {
                object value;
                properties.TryGetValue(_key, out value);

                if (value != null && _value is Enum)
                {
                    return Equals(value, Convert.ToInt32(_value)) || Equals(value, _value.ToString());
                }
                else
                {
                    Debug.Assert(value == null || _value == null || value.GetType() == _value.GetType());
                    return Equals(value, _value);
                }
            }
            else
            {
                return false;
            }
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
                BuilderMappingByEqualityAttribute castObj = obj as BuilderMappingByEqualityAttribute;
                return (castObj != null &&
                        base.Equals(castObj) &&
                        _key == castObj._key &&
                        _value == castObj._value);
            }
        }

        public override int GetHashCode()
        {
            int code = base.GetHashCode();

            if (_key != null)
            {
                code ^= _key.GetHashCode();
            }

            if (_value != null)
            {
                code ^= _value.GetHashCode();
            }

            return code;
        }
    }
}