using Interop.SLXControls;
using Sage.Platform.Controls;
using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMapping("AxPickList")]
    public sealed class PickListBuilder : ControlBuilder
    {
        private const int LinkIdBindingCode = 29;
        //private const int TextOrderBindingCode = 31;
        private const int TextCodeBindingCode = 32;
        private const int TextItemBindingCode = 33;
        private new const int TextBindingCode = 43;

        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFSLXPickList();
        }

        protected override void OnBuild()
        {
            QFSLXPickList pickList = (QFSLXPickList) QfControl;
            string pickListName;

            if (Component.TryGetPropertyValue("PickListName", out pickListName))
            {
                pickList.PickListName = pickListName;
            }

            if (Control.Bindings != null)
            {
                DataPath bindingPath;
                int textType;
                string propertyString = null;

                if (Control.Bindings.TryGetValue(TextBindingCode, out bindingPath) && Component.TryGetPropertyValue("TextType", out textType))
                {
                    switch ((TxPickListTextType) textType)
                    {
                        case TxPickListTextType.ttItem:
                            pickList.StorageMode = StorageModeEnum.Text;

                            try
                            {
                                propertyString = DataPathTranslator.TranslateField(bindingPath);
                            }
                            catch (MigrationException ex)
                            {
                                LogError(ex.Message);
                            }
                            break;
                        case TxPickListTextType.ttCode:
                            pickList.StorageMode = StorageModeEnum.Code;

                            try
                            {
                                propertyString = DataPathTranslator.TranslateField(bindingPath);
                            }
                            catch (MigrationException ex)
                            {
                                LogError(ex.Message);
                            }
                            break;
                        default:
                            LogWarning("Unsupported TextType on '{0}' picklist", Component.Name);
                            break;
                    }
                }

                AttemptBinding(pickList, LinkIdBindingCode, StorageModeEnum.ID, ref propertyString);
                AttemptBinding(pickList, TextItemBindingCode, StorageModeEnum.Text, ref propertyString);
                AttemptBinding(pickList, TextCodeBindingCode, StorageModeEnum.Code, ref propertyString);

                if (propertyString != null)
                {
                    QfControl.DataBindings.Add(new QuickFormPropertyDataBindingDefinition(propertyString, "PickListValue"));
                }
            }
        }

        private void AttemptBinding(QFSLXPickList pickList, int bindingCode, StorageModeEnum storageMode, ref string propertyString)
        {
            DataPath bindingPath;

            if (Control.Bindings.TryGetValue(bindingCode, out bindingPath))
            {
                if (string.IsNullOrEmpty(propertyString))
                {
                    pickList.StorageMode = storageMode;

                    try
                    {
                        propertyString = DataPathTranslator.TranslateField(bindingPath);
                    }
                    catch (MigrationException ex)
                    {
                        LogError(ex.Message);
                    }
                }
                else
                {
                    LogWarning("PickList '{0}' does not support multiple bindings", Component.Name);
                }
            }
        }
    }
}