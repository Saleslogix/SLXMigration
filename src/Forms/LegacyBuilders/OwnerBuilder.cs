using Sage.Platform.Orm.Entities;
using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.LegacyBuilders
{
    [BuilderMappingByEquality("TSLLinkEdit", "LinkMode", "lemOwner")]
    [BuilderMappingByEquality("TSLEdit", "FormatType", "ftOwner")]
    public sealed class OwnerBuilder : ControlBuilder
    {
        private DataPath _bindingPath;
        private bool _isObject;

        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFSLXOwner();
        }

        protected override void OnExtractSchemaHints()
        {
            if (Control.Bindings != null)
            {
                string bindingProperty = (Control.LegacyType == "AxLookupEdit"
                                              ? "LookupId"
                                              : "Text");

                if (Control.Bindings.TryGetValue(bindingProperty, out _bindingPath))
                {
                    OrmEntity entity = EntityLoader.LoadEntity(_bindingPath.TargetTable);

                    if (entity != null)
                    {
                        string targetField = _bindingPath.TargetField;

                        if (targetField.StartsWith("@"))
                        {
                            targetField = targetField.Substring(1);
                        }

                        OrmEntityProperty property = entity.Properties.GetFieldPropertyByFieldName(targetField);

                        if (property != null)
                        {
                            _isObject = !property.Include;

                            if (_isObject)
                            {
                                DataPathTranslator.RegisterJoin(_bindingPath, new DataPath("SECCODE", "SECCODEID"));
                            }
                        }
                    }
                }
            }
        }

        protected override void OnBuild()
        {
            if (_bindingPath != null)
            {
                string propertyString = null;

                try
                {
                    propertyString = (_isObject
                                          ? DataPathTranslator.TranslateReference(_bindingPath, "SECCODE", "SECCODEID")
                                          : DataPathTranslator.TranslateField(_bindingPath));
                }
                catch (MigrationException ex)
                {
                    LogError(ex.Message);
                }

                if (propertyString != null)
                {
                    QfControl.DataBindings.Add(new QuickFormPropertyDataBindingDefinition(propertyString, "LookupResultValue"));
                }
            }
        }
    }
}