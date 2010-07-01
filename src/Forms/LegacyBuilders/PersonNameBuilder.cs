using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.LegacyBuilders
{
    [BuilderMapping("TSLNameEdit")]
    public sealed class PersonNameBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFSLXPersonName();
        }
    }
}