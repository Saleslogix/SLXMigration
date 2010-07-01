using System;
using System.CodeDom.Compiler;
using System.IO;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    public sealed class CustomIndentedTextWriter : IndentedTextWriter
    {
        public CustomIndentedTextWriter(string indentString, int indent, bool writeIndent)
            : base(new StringWriter(), indentString)
        {
            Indent = indent;

            if (writeIndent)
            {
                for (int i = 0; i < indent; i++)
                {
                    Write(indentString);
                }
            }
        }

        public override string ToString()
        {
            return InnerWriter.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                InnerWriter.Dispose();
            }

            base.Dispose(disposing);
        }

        public override void Write(string s)
        {
            string[] lines = s.Split(new string[] {"\r\n", "\n\r", "\r", "\n"}, StringSplitOptions.None);
            int count = lines.Length;

            for (int i = 0; i < count; i++)
            {
                if (i < count - 1)
                {
                    WriteLine(lines[i]);
                }
                else
                {
                    base.Write(lines[i]);
                }
            }
        }
    }
}