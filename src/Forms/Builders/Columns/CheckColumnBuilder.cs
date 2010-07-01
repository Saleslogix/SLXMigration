using Sage.SalesLogix.QuickForms.QFControls;
using Sage.SalesLogix.QuickForms.QFControls.DataGrid;

namespace Sage.SalesLogix.Migration.Forms.Builders.Columns
{
    [BuilderMapping("TdxDBTreeListCheckColumn")]
    public sealed class CheckColumnBuilder : ColumnBuilder
    {
        protected override IQFDataGridCol OnConstruct()
        {
            return new QFCheckBoxCol();
        }
    }
}