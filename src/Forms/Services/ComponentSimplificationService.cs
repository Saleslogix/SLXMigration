using System;
using System.Collections.Generic;
using Sage.SalesLogix.LegacyBridge.Delphi;

namespace Sage.SalesLogix.Migration.Forms.Services
{
    public sealed class ComponentSimplificationService : IComponentSimplificationService
    {
        public void Simplify(DelphiComponent component)
        {
            InternalSimplify(component);

            foreach (DelphiComponent subComponent in component.Components)
            {
                Simplify(subComponent);
            }
        }

        private static void InternalSimplify(DelphiComponent component)
        {
            IDictionary<string, object> changes = new Dictionary<string, object>();

            foreach (KeyValuePair<string, object> property in component.Properties)
            {
                IConvertible convertible = property.Value as IConvertible;

                if (convertible != null)
                {
                    switch (convertible.GetTypeCode())
                    {
                        case TypeCode.SByte:
                        case TypeCode.Int16:
                        case TypeCode.Int64:
                        case TypeCode.Byte:
                        case TypeCode.UInt16:
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                            changes.Add(property.Key, convertible.ToInt32(null));
                            break;
                        case TypeCode.Single:
                            changes.Add(property.Key, convertible.ToDouble(null));
                            break;
                    }
                }
                else if (property.Value is DelphiIdentity ||
                         property.Value is DelphiUTF8String ||
                         property.Value is DelphiWString)
                {
                    changes.Add(property.Key, property.Value.ToString());
                }
            }

            foreach (KeyValuePair<string, object> change in changes)
            {
                component.Properties[change.Key] = change.Value;
            }
        }
    }
}