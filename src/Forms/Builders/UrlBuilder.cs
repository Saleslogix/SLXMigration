using Interop.SLXControls;
using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMappingByEquality("AxLinkEdit", "LinkMode", TxLinkMode.lemWebLink)]
    [BuilderMappingByEquality("AxPopupEdit", "ButtonType", TxButtonType.ptSmallGlobe)]
    public sealed class UrlBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFSLXUrl();
        }
    }
}