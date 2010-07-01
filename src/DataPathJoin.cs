using System;

namespace Sage.SalesLogix.Migration
{
    public sealed class DataPathJoin
    {
        private readonly string _fromTable;
        private readonly string _fromField;
        private readonly string _toTable;
        private readonly string _toField;

        public DataPathJoin(string fromTable, string fromField, string toTable, string toField)
        {
            _fromTable = fromTable;
            _fromField = fromField;
            _toTable = toTable;
            _toField = toField;
        }

        public string FromTable
        {
            get { return _fromTable; }
        }

        public string FromField
        {
            get { return _fromField; }
        }

        public string ToTable
        {
            get { return _toTable; }
        }

        public string ToField
        {
            get { return _toField; }
        }

        public static DataPathJoin Parse(string fromTable, string text)
        {
            string[] parts = text.Split('.');

            if (parts.Length < 2)
            {
                throw new FormatException("String was not recognized as a valid data path join");
            }

            string part = parts[0];
            int pos = part.IndexOfAny(new char[] {'<', '=', '>'});

            if (pos < 0)
            {
                throw new FormatException("String was not recognized as a valid data path join");
            }

            return new DataPathJoin(
                fromTable,
                part.Substring(0, pos),
                parts[1],
                part.Substring(pos + 1));
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }
            else if (obj == null)
            {
                return false;
            }
            else
            {
                DataPathJoin castObj = obj as DataPathJoin;
                return (castObj != null &&
                        StringUtils.CaseInsensitiveEquals(_fromTable, castObj._fromTable) &&
                        StringUtils.CaseInsensitiveEquals(_fromField, castObj._fromField) &&
                        StringUtils.CaseInsensitiveEquals(_toTable, castObj._toTable) &&
                        StringUtils.CaseInsensitiveEquals(_toField, castObj._toField));
            }
        }

        public override int GetHashCode()
        {
            return _fromTable.GetHashCode() ^ _fromField.GetHashCode() ^ _toField.GetHashCode() ^ _toTable.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}={2}.{3}", _fromTable, _fromField, _toField, _toTable);
        }
    }
}