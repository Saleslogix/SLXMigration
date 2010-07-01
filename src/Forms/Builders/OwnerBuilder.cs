using Interop.SLXControls;
using Sage.Platform.Orm.Entities;
using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMappingByEquality("AxLookupEdit", "LookupMode", TxLookupMode.lmOwner)]
    [BuilderMappingByEquality("AxEdit", "FormatType", TxFormatType.ftOwner)]
    public sealed class OwnerBuilder : ControlBuilder
    {
        private const int LookupIdBindingCode = 30;

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
                int bindingCode = (Control.LegacyType == "AxLookupEdit"
                                       ? LookupIdBindingCode
                                       : TextBindingCode);

                if (Control.Bindings.TryGetValue(bindingCode, out _bindingPath))
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