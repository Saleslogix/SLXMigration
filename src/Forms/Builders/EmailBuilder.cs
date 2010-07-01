using Interop.SLXControls;
using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMappingByEquality("AxLinkEdit", "LinkMode", TxLinkMode.lemEMail)]
    [BuilderMappingByEquality("AxPopupEdit", "ButtonType", TxButtonType.ptEMail)]
    public sealed class EmailBuilder : ControlBuilder
    {
        private new const int TextBindingCode = 43;

        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFSLXEmail();
        }

        protected override void OnBuild()
        {
            AddDataBinding(TextBindingCode, "Text");
        }
    }
}