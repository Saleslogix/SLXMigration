using Sage.Platform.QuickForms.Controls;

namespace Sage.SalesLogix.Migration.Forms.LegacyBuilders
{
    [BuilderMapping("TSLButton")]
    [BuilderMapping("TSLLabelButton")]
    public sealed class ButtonBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFButton();
        }
    }
}