using System;
using System.CodeDom;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeExitForStatement : CodeCommentStatement
    {
        public CodeExitForStatement()
            : base("Warning: 'Exit For' not supported") {}

        [Obsolete]
        public new CodeComment Comment
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
    }
}