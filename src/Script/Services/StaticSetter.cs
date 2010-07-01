using System.CodeDom;

namespace Sage.SalesLogix.Migration.Script.Services
{
    public sealed class StaticSetter
    {
        public void Set(ScriptInfo script)
        {
            InternalSet(script);
        }

        private bool InternalSet(ScriptInfo script)
        {
            bool isStatic = !CodeDomUtils.AreMemberAttributesSet(script.TypeDeclaration, MemberAttributes.Final);

            if (isStatic && !CodeDomUtils.AreMemberAttributesSet(script.TypeDeclaration, MemberAttributes.Static))
            {
                isStatic = !CollectionUtils.Contains(
                                CodeObjectMetaData.GetFields(script.TypeDeclaration).Values,
                                delegate(CodeMemberField memberField)
                                    {
                                        return (!CodeDomUtils.AreMemberAttributesSet(memberField, MemberAttributes.Const));
                                    });

                if (isStatic)
                {
                    isStatic = !CollectionUtils.Contains(
                                    script.Dependencies.Values,
                                    delegate(ScriptInfo dependencyScript)
                                        {
                                            return (!dependencyScript.IsInvalid && !InternalSet(dependencyScript));
                                        });
                }

                MemberAttributes attribute = (isStatic
                                                  ? MemberAttributes.Static
                                                  : MemberAttributes.Final);
                CodeDomUtils.SetMemberAttributes(script.TypeDeclaration, attribute);

                foreach (CodeTypeMember typeMember in script.TypeDeclaration.Members)
                {
                    if (!(typeMember is CodeTypeDeclaration ||
                          (typeMember is CodeMemberField &&
                           CodeDomUtils.AreMemberAttributesSet(typeMember, MemberAttributes.Const))))
                    {
                        CodeDomUtils.SetMemberAttributes(typeMember, attribute);
                    }
                }
            }

            return isStatic;
        }
    }
}