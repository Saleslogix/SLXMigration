using System.Collections;
using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.LegacyBuilders
{
    [BuilderMapping("TSLComboBox")]
    [BuilderMapping("TSLListBox")]
    public sealed class ListBoxBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFListBox();
        }

        protected override void OnBuild()
        {
            IEnumerable items;

            if (Component.TryGetPropertyValue("Items.Strings", out items))
            {
                QFListBox box = (QFListBox) QfControl;

                foreach (object item in items)
                {
                    string str = item.ToString();
                    box.Items.Add(new QFListItem(str, str));
                }
            }
        }
    }
}