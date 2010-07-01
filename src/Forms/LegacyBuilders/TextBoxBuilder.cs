using Sage.Platform.QuickForms.Controls;

namespace Sage.SalesLogix.Migration.Forms.LegacyBuilders
{
    [BuilderMapping("TSLEdit")]
    [BuilderMapping("TSLMemo")]
    public sealed class TextBoxBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFTextBox();
        }

        protected override void OnBuild()
        {
            ((QFTextBox) QfControl).Multiline = (Component.Type == "TSLMemo");
        }
    }
}