using Sage.Platform.QuickForms.Controls;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMapping("AxEdit")]
    [BuilderMapping("AxMemo")]
    [BuilderMapping("AxLinkEdit")]
    public sealed class TextBoxBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFTextBox();
        }

        protected override void OnBuild()
        {
            QFTextBox TextBox = (QFTextBox)QfControl;
            TextBox.Multiline = (Component.Type == "TMemoEx");
            string strText = string.Empty;
            if (Component.TryGetPropertyValue("Text", out strText)) TextBox.Text = strText;
            
        }
    }
}