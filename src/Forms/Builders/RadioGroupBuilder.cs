using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.QFControls;
using Sage.SalesLogix.LegacyBridge.Delphi;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMapping("AxRadioGroup")]
    public sealed class RadioGroupBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFRadioGroup();
        }

        protected override void OnBuild()
        {
            DelphiList Items = null;
            if (Component.TryGetPropertyValue("Items.Strings", out Items))
            {

                int ItemIndex = 0;
                Component.TryGetPropertyValue("ItemIndex", out ItemIndex);
                QFRadioGroup radioGroup = (QFRadioGroup)QfControl;
                radioGroup.Items.Clear();
                int index = 0;
                foreach (object Item in Items)
                {
                    if (Item != null)
                    {
                        QFListItem ListItem = radioGroup.Items.AddNew();
                        ListItem.Text = Item.ToString();
                        ListItem.Value = Item.ToString();
                        ListItem.Selected = (index == ItemIndex);
                        index++;
                    }
                }
            }
        }
    }
}