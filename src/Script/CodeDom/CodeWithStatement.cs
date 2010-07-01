using System;
using System.CodeDom;
using System.Collections;
using System.ComponentModel;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeWithStatement : CodeConditionStatement
    {
        public static readonly CodeExpression TargetPlaceholder = new CodeSnippetExpression();

        private CodeExpression _target;
        private readonly CodeNotificationStatementCollection _statements;
        private CodeWithStatement _parentWithStatement;

        public CodeWithStatement()
        {
            _statements = new CodeNotificationStatementCollection(StatementsChanged);
            base.Condition = new CodePrimitiveExpression(true);
        }

        public CodeWithStatement(CodeExpression target, params CodeStatement[] statements)
        {
            _target = target;
            _statements = new CodeNotificationStatementCollection(StatementsChanged, statements);
            base.TrueStatements.AddRange(statements);
            base.Condition = new CodePrimitiveExpression(true);
            SetAllTargets(_statements, TargetPlaceholder, _target);
        }

        public CodeExpression Target
        {
            get { return _target; }
            set
            {
                if (_target != value)
                {
                    CodeExpression previousTarget = _target;
                    _target = value;
                    SetAllTargets(_statements, previousTarget, _target);
                }
            }
        }

        public CodeStatementCollection Statements
        {
            get { return _statements; }
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

        internal CodeWithStatement ParentWithStatement
        {
            get { return _parentWithStatement; }
        }

        private void StatementsChanged(CollectionChangeAction action, CodeStatement stmt, int index)
        {
            if (action == CollectionChangeAction.Add)
            {
                base.TrueStatements.Insert(index, stmt);
                SetTargets(stmt, TargetPlaceholder, _target);
            }
            else if (action == CollectionChangeAction.Remove)
            {
                base.TrueStatements.Remove(stmt);
                SetTargets(stmt, _target, TargetPlaceholder);
            }
            else
            {
                SetAllTargets(base.TrueStatements, _target, TargetPlaceholder);
                base.TrueStatements.Clear();
            }
        }

        private CodeExpression _oldTargetExpr;
        private CodeExpression _newTargetExpr;

        private void SetTargets(CodeObject obj, CodeExpression previousTargetExpr, CodeExpression newTargetExpr)
        {
            _oldTargetExpr = previousTargetExpr;
            _newTargetExpr = newTargetExpr;
            new CodeDomWalker(obj).Walk(SetTarget);
            _oldTargetExpr = null;
            _newTargetExpr = null;
        }

        private void SetAllTargets(IList stmts, CodeExpression previousTargetExpr, CodeExpression newTargetExpr)
        {
            _oldTargetExpr = previousTargetExpr;
            _newTargetExpr = newTargetExpr;
            new CodeDomWalker(stmts).Walk(SetTarget);
            _oldTargetExpr = null;
            _newTargetExpr = null;
        }

        private void SetTarget(ref CodeObject target, CodeObject parent, int indent)
        {
            if (target is CodeWithStatement)
            {
                CodeWithStatement withStmt = (CodeWithStatement) target;

                if (withStmt._parentWithStatement == null)
                {
                    withStmt._parentWithStatement = this;
                }
            }
            else if (target is CodeMethodInvokeExpression)
            {
                CodeMethodInvokeExpression methodInvokeExpr = (CodeMethodInvokeExpression) target;

                if (methodInvokeExpr.Method.TargetObject == _oldTargetExpr && methodInvokeExpr != _newTargetExpr)
                {
                    methodInvokeExpr.Method.TargetObject = _newTargetExpr;
                }
            }
            else if (target is CodePropertyReferenceExpression)
            {
                CodePropertyReferenceExpression propertyExpr = (CodePropertyReferenceExpression) target;

                if (propertyExpr.TargetObject == _oldTargetExpr && propertyExpr != _newTargetExpr)
                {
                    propertyExpr.TargetObject = _newTargetExpr;
                }
            }
            else if (target is CodeIndexerExpression)
            {
                CodeIndexerExpression indexerExpr = (CodeIndexerExpression) target;

                if (indexerExpr.TargetObject == _oldTargetExpr && indexerExpr != _newTargetExpr)
                {
                    indexerExpr.TargetObject = _newTargetExpr;
                }
            }
        }
    }
}