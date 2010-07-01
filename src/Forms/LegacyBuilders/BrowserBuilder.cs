using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.LegacyBuilders
{
    [BuilderMapping("TSLBrowser")]
    public sealed class BrowserBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFBrowserControl();
        }

        protected override void OnBuild()
        {
            string url;

            if (Component.TryGetPropertyValue("URL", out url) && !string.IsNullOrEmpty(url))
            {
                ((QFBrowserControl) QfControl).ContentLocation = url;
            }

            AddDataBinding("Url", "ContentLocation");
        }
    }
}