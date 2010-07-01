using Interop.SLXControls;
using Sage.Platform.Orm.Entities;
using Sage.SalesLogix.QuickForms.QFControls;
using Sage.SalesLogix.QuickForms.QFControls.DataGrid;

namespace Sage.SalesLogix.Migration.Forms.Builders.Columns
{
    [BuilderMappingByEquality("TdxDBTreeListColumn", "FormatType", TxFormatType.ftUser)]
    public sealed class UserColumnBuilder : ColumnBuilder
    {
        private bool _isObject;

        protected override void OnExtractSchemaHints()
        {
            if (BindingPath != null)
            {
                OrmEntity entity = EntityLoader.LoadEntity(BindingPath.TargetTable);

                if (entity != null)
                {
                    string targetField = BindingPath.TargetField;

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
                            DataPathTranslator.RegisterJoin(BindingPath, new DataPath("USERSECURITY", "USERID"));
                        }
                    }
                }
            }
        }

        protected override IQFDataGridCol OnConstruct()
        {
            return new QFSLXUserCol();
        }

        protected override string BuildDataField(DataPath prefixPath)
        {
            try
            {
                return (_isObject
                            ? DataPathTranslator.TranslateReference(BindingPath, "USERSECURITY", "USERID")
                            : DataPathTranslator.TranslateField(BindingPath));
            }
            catch (MigrationException ex)
            {
                LogError(ex.Message);
                return null;
            }
        }
    }
}