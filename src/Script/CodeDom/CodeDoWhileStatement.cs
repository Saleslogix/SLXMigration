using System;
using System.CodeDom;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeDoWhileStatement : CodeIterationStatement
    {
        private static int _firstIterationFlagCounter;
        private readonly CodeBinaryOperatorExpression _testExpr;

        public CodeDoWhileStatement()
            : this(null) {}

        public CodeDoWhileStatement(CodeExpression testExpression, params CodeStatement[] statements)
        {
            string firstIterationFlagName = "firstIterationFlag" + (_firstIterationFlagCounter++);
            _testExpr = new CodeBinaryOperatorExpression(
                new CodeVariableReferenceExpression(firstIterationFlagName),
                CodeBinaryOperatorType.BooleanOr,
                testExpression);
            Statements.AddRange(statements);

            base.InitStatement = new CodeVariableDeclarationStatement(
                typeof (bool),
                firstIterationFlagName,
                new CodePrimitiveExpression(true));
            base.TestExpression = _testExpr;
            base.IncrementStatement = new CodeAssignStatement(
                new CodeVariableReferenceExpression(firstIterationFlagName),
                new CodePrimitiveExpression(false));
        }

        public new CodeExpression TestExpression
        {
            get { return _testExpr.Right; }
            set { _testExpr.Right = value; }
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