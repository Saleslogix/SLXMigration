using System;
using System.CodeDom;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeWhileStatement : CodeIterationStatement
    {
        public CodeWhileStatement()
        {
            CodeExpressionStatement emptyStmt = new CodeExpressionStatement(new CodeSnippetExpression());
            base.InitStatement = emptyStmt;
            base.IncrementStatement = emptyStmt;
        }

        public CodeWhileStatement(CodeExpression testExpression, params CodeStatement[] statements)
            : this()
        {
            TestExpression = testExpression;
            Statements.AddRange(statements);
        }

        [Obsolete]
        public new CodeStatement InitStatement
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeStatement IncrementStatement
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
    }
}