using System;
using System.CodeDom;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeRectangularArrayCreateExpression : CodeCastExpression
    {
        private readonly CodeTypeOfExpression _typeOfExpr;
        private readonly CodeExpressionCollection _lengths;
        private CodeMethodInvokeExpression _methodInvokeExpr;

        public CodeRectangularArrayCreateExpression()
        {
            _typeOfExpr = new CodeTypeOfExpression();
            _lengths = new CodeNotificationExpressionCollection(Refresh);
            Initialize();
            Refresh();
        }

        public CodeRectangularArrayCreateExpression(Type createType, params CodeExpression[] lengths)
            : this(new CodeTypeReference(createType), lengths) {}

        public CodeRectangularArrayCreateExpression(CodeTypeReference createType, params CodeExpression[] lengths)
        {
            _typeOfExpr = new CodeTypeOfExpression(createType);
            _lengths = new CodeNotificationExpressionCollection(Refresh, lengths);
            Initialize();
            Refresh();
        }

        private void Initialize()
        {
            _methodInvokeExpr = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression(
                    typeof (Array)),
                "CreateInstance");
            base.Expression = _methodInvokeExpr;
        }

        public CodeTypeReference CreateType
        {
            get { return _typeOfExpr.Type; }
            set
            {
                if (_typeOfExpr.Type != value)
                {
                    _typeOfExpr.Type = value;
                    Refresh();
                }
            }
        }

        public CodeExpressionCollection Lengths
        {
            get { return _lengths; }
        }

        [Obsolete]
        public new CodeTypeReference TargetType
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeExpression Expression
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        private void Refresh()
        {
            _methodInvokeExpr.Parameters.Clear();
            _methodInvokeExpr.Parameters.Add(_typeOfExpr);
            _methodInvokeExpr.Parameters.AddRange(_lengths);
            base.TargetType = new CodeTypeReference(CreateType, _lengths.Count);
        }
    }
}