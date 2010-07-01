using System;
using System.CodeDom;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeSwitchOption : CodeConditionStatement
    {
        private readonly CodeNotificationExpressionCollection _values;
        private CodeExpression _target;
        private CodeConditionStatement _actualStmt;

        public CodeSwitchOption(params CodeExpression[] values)
        {
            _actualStmt = this;
            _values = new CodeNotificationExpressionCollection(Refresh, values);
            Refresh();
        }

        public CodeSwitchOption(CodeExpression[] values, params CodeStatement[] statements)
        {
            _actualStmt = this;
            _values = new CodeNotificationExpressionCollection(Refresh, values);

            if (statements != null)
            {
                _actualStmt.TrueStatements.AddRange(statements);
            }

            Refresh();
        }

        internal void SetActualStmt(CodeConditionStatement actualStmt)
        {
            if (actualStmt == null)
            {
                actualStmt = this;
            }

            if (actualStmt != _actualStmt)
            {
                actualStmt.Condition = _actualStmt.Condition;
                actualStmt.TrueStatements.Clear();
                actualStmt.TrueStatements.AddRange(_actualStmt.TrueStatements);
                _actualStmt = actualStmt;
            }
        }

        internal void SetTarget(CodeExpression target)
        {
            if (_target != target)
            {
                _target = target;
                Refresh();
            }
        }

        public CodeExpressionCollection Values
        {
            get { return _values; }
        }

        public CodeStatementCollection Statements
        {
            get { return _actualStmt.TrueStatements; }
        }

        [Obsolete]
        public new CodeExpression Condition
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeStatementCollection TrueStatements
        {
            get { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeStatementCollection FalseStatements
        {
            get { throw new NotSupportedException(); }
        }

        private void Refresh()
        {
            CodeBinaryOperatorExpression binaryOperatorExpr = null;

            if (_target != null)
            {
                foreach (CodeExpression optionValue in _values)
                {
                    CodeBinaryOperatorExpression newBinaryOperatorExpr = new CodeBinaryOperatorExpression(
                        _target,
                        CodeBinaryOperatorType.IdentityEquality,
                        optionValue);

                    if (binaryOperatorExpr == null)
                    {
                        binaryOperatorExpr = newBinaryOperatorExpr;
                    }
                    else
                    {
                        binaryOperatorExpr = new CodeBinaryOperatorExpression(
                            binaryOperatorExpr,
                            CodeBinaryOperatorType.BooleanOr,
                            newBinaryOperatorExpr);
                    }
                }
            }

            _actualStmt.Condition = binaryOperatorExpr;
        }
    }
}