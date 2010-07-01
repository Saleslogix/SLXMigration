using System;
using System.Runtime.Serialization;

namespace Sage.SalesLogix.Migration.Script.VBSParser
{
    [Serializable]
    public class ParserException : ApplicationException
    {
        public ParserException() {}

        public ParserException(string message)
            : base(message) {}

        protected ParserException(SerializationInfo info, StreamingContext context)
            : base(info, context) {}

        public ParserException(string message, Exception innerException)
            : base(message, innerException) {}
    }
}