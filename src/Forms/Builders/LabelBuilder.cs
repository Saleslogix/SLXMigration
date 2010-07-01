using System.Collections.Generic;
using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMapping("AxLabel")]
    [LabelBuilderMappingAttribute]
    public sealed class LabelBuilder : ControlBuilder
    {
        private bool _AutoSize = true; //it is true by Default for the Delphi controls
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFLabel();
        }

        protected override void OnBuild()
        {
            Component.TryGetPropertyValue("AutoSize", out _AutoSize);
        }

        public bool AutoSize { get { return _AutoSize; } }

        private sealed class LabelBuilderMappingAttribute : BuilderMappingAttribute
        {
            public LabelBuilderMappingAttribute()
                : base("AxButton") {}

            public override bool IsApplicable(string name, IDictionary<string, object> properties)
            {
                object flat;
                object glyph;
                return (base.IsApplicable(name, properties) &&
                        properties.TryGetValue("Flat", out flat) &&
                        Equals(flat, true) &&
                        (!properties.TryGetValue("Glyph.Data", out glyph) ||
                         Equals(glyph, null)));
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
                    LabelBuilderMappingAttribute castObj = obj as LabelBuilderMappingAttribute;
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