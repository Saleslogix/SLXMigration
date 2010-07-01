using System.Collections.Generic;
using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [SeparatorBuilderMappingAttribute]
    public sealed class SeparatorBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFHorizontalSeparator();
        }

        private sealed class SeparatorBuilderMappingAttribute : BuilderMappingAttribute
        {
            public SeparatorBuilderMappingAttribute()
                : base("AxPanel") {}

            public override bool IsApplicable(string name, IDictionary<string, object> properties)
            {
                object height;

                if (base.IsApplicable(name, properties) &&
                    properties.TryGetValue("Height", out height))
                {
                    int value = (int) height;
                    return (value > 0 && value <= 2);
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
                    SeparatorBuilderMappingAttribute castObj = obj as SeparatorBuilderMappingAttribute;
                    return (castObj != null && base.Equals(castObj));
                }
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
    }
}