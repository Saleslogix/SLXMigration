using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMapping("AxImage")]
    public sealed class ImageBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFImage();
        }

        protected override void OnBuild()
        {
            base.OnBuild();
        }
    }
}