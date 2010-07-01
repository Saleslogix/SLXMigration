using Interop.SLXControls;
using Sage.SalesLogix.QuickForms.QFControls;
using Sage.SalesLogix.QuickForms.QFControls.DataGrid;

namespace Sage.SalesLogix.Migration.Forms.Builders.Columns
{
    [BuilderMapping("TdxDBTreeListColumn")]
    [BuilderMapping("TdxDBTreeListMemoColumnEx")]
    public sealed class TextColumnBuilder : ColumnBuilder
    {
        protected override IQFDataGridCol OnConstruct()
        {
            return new QFDataGridCol();
        }

        protected override void OnBuild()
        {
            int formatType;

            if (Component.TryGetPropertyValue("FormatType", out formatType))
            {
                string formatString;
                Component.TryGetPropertyValue("FormatString", out formatString);

                switch ((TxFormatType) formatType)
                {
                    case TxFormatType.ftFixed:
                        formatString = FormatUtils.ConvertNumberFormat(formatString, null);
                        break;
                    case TxFormatType.ftInteger:
                        formatString = FormatUtils.ConvertNumberFormat(formatString, "{0:n0}");
                        break;
                    case TxFormatType.ftDateTime:
                        formatString = FormatUtils.ConvertDateFormat(formatString, null);
                        break;
                    case TxFormatType.ftPercent:
                        formatString = FormatUtils.ConvertNumberFormat(formatString, "{0:n}%");
                        break;
                    case TxFormatType.ftCurrency:
                        formatString = FormatUtils.ConvertNumberFormat(formatString, "{0:c}");
                        break;
                    case TxFormatType.ftBoolean:
                        break;
                    case TxFormatType.ftTimeZone:
                        break;
                    default:
                        break;
                }

                if (!string.IsNullOrEmpty(formatString))
                {
                    QFDataGridCol col = Column as QFDataGridCol;

                    if (col != null)
                    {
                        col.TextFormatString = formatString;
                    }
                }
            }
        }
    }
}