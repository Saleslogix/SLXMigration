using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMapping("AxCheckBox")]
    public sealed class CheckBoxBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFCheckBox();
        }

        protected override void OnBuild()
        {
            AddDataBinding(TextBindingCode, "Checked");
        }
    }
}