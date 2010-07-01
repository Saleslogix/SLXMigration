using System;
using System.Runtime.Serialization;

namespace Sage.SalesLogix.Migration
{
    [Serializable]
    public class MigrationException : ApplicationException
    {
        public MigrationException() {}

        public MigrationException(string message)
            : base(message) {}

        protected MigrationException(SerializationInfo info, StreamingContext context)
            : base(info, context) {}

        public MigrationException(string message, Exception innerException)
            : base(message, innerException) {}
    }
}