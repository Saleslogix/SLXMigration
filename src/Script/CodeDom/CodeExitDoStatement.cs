using System;
using System.CodeDom;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeExitDoStatement : CodeCommentStatement
    {
        public CodeExitDoStatement()
            : base("Warning: 'Exit Do' not supported") {}

        [Obsolete]
        public new CodeComment Comment
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
    }
}