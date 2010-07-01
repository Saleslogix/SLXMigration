using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.LegacyBuilders
{
    [BuilderMappingByEquality("TSLLinkEdit", "LinkMode", "lemPhone")]
    [BuilderMappingByEquality("TSLLinkEdit", "LinkMode", "lemPager")]
    public sealed class PhoneBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFSLXPhone();
        }
    }
}