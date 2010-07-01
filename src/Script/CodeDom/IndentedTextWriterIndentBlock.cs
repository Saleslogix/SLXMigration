using System;
using System.CodeDom.Compiler;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    public sealed class IndentedTextWriterIndentBlock : IDisposable
    {
        private readonly IndentedTextWriter _writer;
        private readonly int _amount;

        public IndentedTextWriterIndentBlock(IndentedTextWriter writer)
            : this(writer, 1) {}

        public IndentedTextWriterIndentBlock(IndentedTextWriter writer, int amount)
        {
            _writer = writer;
            _amount = amount;
            _writer.Indent += amount;
        }

        #region IDisposable Members

        public void Dispose()
        {
            _writer.Indent -= _amount;
        }

        #endregion
    }
}