using Sage.Platform.QuickForms.Controls;

namespace Sage.SalesLogix.Migration
{
    public interface IControlBuilder
    {
        QuickFormsControlBase Construct();
        void Build();
        void PostBuild();
    }
}