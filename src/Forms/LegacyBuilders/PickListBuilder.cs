using Sage.Platform.Controls;
using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.LegacyBuilders
{
    [BuilderMapping("TSLPickList")]
    public sealed class PickListBuilder : ControlBuilder
    {
        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFSLXPickList();
        }

        protected override void OnBuild()
        {
            QFSLXPickList pickList = (QFSLXPickList) QfControl;
            string listName;

            if (Component.TryGetPropertyValue("ListName", out listName))
            {
                pickList.PickListName = listName;
            }

            if (Control.Bindings != null)
            {
                DataPath bindingPath;
                string textType;
                string propertyString = null;

                if (Control.Bindings.TryGetValue("Text", out bindingPath) && Component.TryGetPropertyValue("TextType", out textType))
                {
                    switch (textType)
                    {
                        case "ttItem":
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
                        case "ttCode":
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

                AttemptBinding(pickList, "LinkId", StorageModeEnum.ID, ref propertyString);
                AttemptBinding(pickList, "TextItem", StorageModeEnum.Text, ref propertyString);
                AttemptBinding(pickList, "TextCode", StorageModeEnum.Code, ref propertyString);

                if (propertyString != null)
                {
                    QfControl.DataBindings.Add(new QuickFormPropertyDataBindingDefinition(propertyString, "PickListValue"));
                }
            }
        }

        private void AttemptBinding(QFSLXPickList pickList, string bindingProperty, StorageModeEnum storageMode, ref string propertyString)
        {
            DataPath bindingPath;

            if (Control.Bindings.TryGetValue(bindingProperty, out bindingPath))
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