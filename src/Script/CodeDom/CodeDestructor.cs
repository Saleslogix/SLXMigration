using System;
using System.CodeDom;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeDestructor : CodeMemberMethod
    {
        public CodeDestructor(params CodeStatement[] statements)
        {
            base.Name = "Finalize";
            base.Attributes = MemberAttributes.Family | MemberAttributes.Override;
            Statements.AddRange(statements);
        }

        [Obsolete]
        public new string Name
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new MemberAttributes Attributes
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeTypeReferenceCollection ImplementationTypes
        {
            get { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeParameterDeclarationExpressionCollection Parameters
        {
            get { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeTypeReference PrivateImplementationType
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeTypeReference ReturnType
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeAttributeDeclarationCollection ReturnTypeCustomAttributes
        {
            get { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeTypeParameterCollection TypeParameters
        {
            get { throw new NotSupportedException(); }
        }
    }
}