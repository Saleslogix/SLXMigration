using Sage.SalesLogix.QuickForms.QFControls;
using Sage.SalesLogix.QuickForms.QFControls.DataGrid;

namespace Sage.SalesLogix.Migration.Forms.Builders.Columns
{
    [BuilderMapping("TdxDBTreeListDateColumn")]
    [BuilderMapping("TdxDBTreeListTimeColumn")]
    public sealed class DateTimePickerColumnBuilder : ColumnBuilder
    {
        protected override IQFDataGridCol OnConstruct()
        {
            return new QFDateTimePickerCol();
        }

        protected override void OnBuild()
        {
            ((QFDateTimePickerCol) Column).DateOnly = false;
        }
    }
}