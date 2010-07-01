using Sage.SalesLogix.QuickForms.QFControls;
using Sage.SalesLogix.QuickForms.QFControls.DataGrid;

namespace Sage.SalesLogix.Migration.Forms.Builders.Columns
{
    [BuilderMapping("TdxDBTreeListPickListColumn")]
    [BuilderMappingByEquality("TdxDBTreeListColumn", "FormatType", (int) FormatType.PickListItem)]
    public sealed class PickListColumnBuilder : ColumnBuilder
    {
        protected override IQFDataGridCol OnConstruct()
        {
            return new QFPickListCol();
        }

        protected override void OnBuild()
        {
            string pickList;

            if (Component.TryGetPropertyValue("PickList", out pickList))
            {
                ((QFPickListCol) Column).PickListName = pickList;
            }
        }
    }
}