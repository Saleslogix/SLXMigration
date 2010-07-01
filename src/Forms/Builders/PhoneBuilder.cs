using Interop.SLXControls;
using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMappingByEquality("AxLinkEdit", "LinkMode", TxLinkMode.lemPhone)]
    [BuilderMappingByEquality("AxLinkEdit", "LinkMode", TxLinkMode.lemPager)]
    [BuilderMappingByEquality("AxPopupEdit", "ButtonType", TxButtonType.ptPhone)]
    [BuilderMappingByEquality("AxPopupEdit", "ButtonType", TxButtonType.ptPager)]
    public sealed class PhoneBuilder : ControlBuilder
    {
        private new const int TextBindingCode = 43;

        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFSLXPhone();
        }

        protected override void OnBuild()
        {
            AddDataBinding(TextBindingCode, "Text");
        }
    }
}