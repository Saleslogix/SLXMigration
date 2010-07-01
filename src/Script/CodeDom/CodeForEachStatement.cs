using System;
using System.CodeDom;
using System.Collections;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeForEachStatement : CodeIterationStatement
    {
        private static int _enumeratorCount;
        private CodeTypeReference _elementType;
        private CodeExpression _enumerableTarget;
        private CodeAssignStatement _firstStatement;
        private readonly CodeVariableReferenceExpression _variableRefExpr;

        public CodeForEachStatement()
        {
            _elementType = new CodeTypeReference(typeof (object));
            _variableRefExpr = new CodeVariableReferenceExpression(null);
            Refresh();
        }

        public CodeForEachStatement(Type elementType, string elementName, CodeExpression enumerableTarget, params CodeStatement[] statements)
            : this(new CodeTypeReference(elementType), elementName, enumerableTarget, statements) {}

        public CodeForEachStatement(CodeTypeReference elementType, string elementName, CodeExpression enumerableTarget, params CodeStatement[] statements)
        {
            _elementType = elementType;
            _variableRefExpr = new CodeVariableReferenceExpression(elementName);
            _enumerableTarget = enumerableTarget;
            Statements.AddRange(statements);
            Refresh();
        }

        public CodeTypeReference ElementType
        {
            get { return _elementType; }
            set
            {
                if (_elementType != value)
                {
                    _elementType = value;
                    Refresh();
                }
            }
        }

        public string ElementName
        {
            get { return _variableRefExpr.VariableName; }
            set
            {
                if (_variableRefExpr.VariableName != value)
                {
                    _variableRefExpr.VariableName = value;
                    Refresh();
                }
            }
        }

        public CodeExpression EnumerableTarget
        {
            get { return _enumerableTarget; }
            set
            {
                if (_enumerableTarget != value)
                {
                    _enumerableTarget = value;
                    Refresh();
                }
            }
        }

        internal void SuppressFirstStatement()
        {
            Statements.Remove(_firstStatement);
        }

        internal void ExposeFirstStatement()
        {
            Statements.Insert(0, _firstStatement);
        }

        [Obsolete]
        public new CodeStatement InitStatement
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeExpression TestExpression
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

        private void Refresh()
        {
            string enumeratorName = "enumerator" + (_enumeratorCount++);
            CodeVariableReferenceExpression variableRefExpr = new CodeVariableReferenceExpression(enumeratorName);
            base.InitStatement = new CodeVariableDeclarationStatement(
                typeof (IEnumerator),
                enumeratorName,
                new CodeMethodInvokeExpression(
                    _enumerableTarget,
                    "GetEnumerator"));
            base.TestExpression = new CodeMethodInvokeExpression(
                variableRefExpr,
                "MoveNext");
            base.IncrementStatement = new CodeExpressionStatement(
                new CodeSnippetExpression());

            CodeExpression rightExpr = new CodePropertyReferenceExpression(
                variableRefExpr,
                "Current");

            if (_elementType.BaseType != typeof (object).FullName ||
                _elementType.ArrayRank != 0 ||
                _elementType.ArrayElementType != null ||
                _elementType.TypeArguments.Count > 0)
            {
                rightExpr = new CodeCastExpression(_elementType, rightExpr);
            }

            if (_firstStatement == null)
            {
                _firstStatement = new CodeAssignStatement(
                    _variableRefExpr,
                    rightExpr);
                Statements.Insert(0, _firstStatement);
            }
            else
            {
                _firstStatement.Right = rightExpr;
                int pos = Statements.IndexOf(_firstStatement);

                if (pos != 0)
                {
                    if (pos > 0)
                    {
                        Statements.RemoveAt(pos);
                    }

                    Statements.Insert(0, _firstStatement);
                }
            }
        }
    }
}