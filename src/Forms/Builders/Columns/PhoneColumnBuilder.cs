using Interop.SLXControls;
using Sage.SalesLogix.QuickForms.QFControls;
using Sage.SalesLogix.QuickForms.QFControls.DataGrid;

namespace Sage.SalesLogix.Migration.Forms.Builders.Columns
{
    [BuilderMappingByEquality("TdxDBTreeListColumn", "FormatType", TxFormatType.ftPhone)]
    public sealed class PhoneColumnBuilder : ColumnBuilder
    {
        protected override IQFDataGridCol OnConstruct()
        {
            return new QFPhoneCol();
        }
    }
}