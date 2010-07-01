using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.LegacyBuilders
{
    [BuilderMapping("TSLLabel")]
    public sealed class LabelBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFLabel();
        }
    }
}