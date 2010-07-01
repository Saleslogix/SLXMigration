namespace Sage.SalesLogix.Migration.Script.VBSParser
{
    public sealed class Token
    {
        private readonly object _value;
        private readonly int? _line;

        public Token(object value, int? firstLine)
        {
            _value = value;
            _line = firstLine;
        }

        public object Value
        {
            get { return _value; }
        }

        public int? Line
        {
            get { return _line; }
        }
    }
}