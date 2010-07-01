using System;
using System.CodeDom;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeUnaryMinusExpression : CodeBinaryOperatorExpression
    {
        public CodeUnaryMinusExpression()
        {
            base.Left = new CodePrimitiveExpression(0);
            base.Operator = CodeBinaryOperatorType.Subtract;
        }

        public CodeUnaryMinusExpression(CodeExpression target)
            : this()
        {
            base.Right = target;
        }

        public CodeExpression Target
        {
            get { return base.Right; }
            set { base.Right = value; }
        }

        [Obsolete]
        public new CodeExpression Left
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeBinaryOperatorType Operator
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeExpression Right
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
    }
}