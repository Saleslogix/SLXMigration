using Sage.SalesLogix.QuickForms.QFControls;
using Sage.SalesLogix.QuickForms.QFControls.DataGrid;

namespace Sage.SalesLogix.Migration.Forms.Builders.Columns
{
    [BuilderMapping("TdxDBTreeListCurrencyColumn")]
    public sealed class CurrencyColumnBuilder : ColumnBuilder
    {
        protected override IQFDataGridCol OnConstruct()
        {
            return new QFSLXCurrencyCol();
        }
    }
}