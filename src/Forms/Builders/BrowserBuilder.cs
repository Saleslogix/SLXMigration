using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMapping("AxBrowser")]
    public sealed class BrowserBuilder : ControlBuilder
    {
        private const int UrlBindingCode = 1;

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

            AddDataBinding(UrlBindingCode, "ContentLocation");
        }
    }
}