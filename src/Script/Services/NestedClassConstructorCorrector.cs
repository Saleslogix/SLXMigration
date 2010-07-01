using System.CodeDom;
using Sage.SalesLogix.Migration.Script.CodeDom;

namespace Sage.SalesLogix.Migration.Script.Services
{
    public sealed class NestedClassConstructorCorrector
    {
        public void Process(CodeTypeDeclaration typeDecl)
        {
            for (int i = 0; i < typeDecl.Members.Count; i++)
            {
                CodeTypeMember typeMember = typeDecl.Members[i];

                if (typeMember is CodeMemberMethod)
                {
                    CodeMemberMethod memberMethod = (CodeMemberMethod) typeMember;
                    CodeObjectSource source = Utils.GetTypeReferenceSource(memberMethod.ReturnType);

                    if (source.Target == typeof (void) && source.ArrayRanks.Length == 0 && memberMethod.Parameters.Count == 0)
                    {
                        if (StringUtils.CaseInsensitiveEquals(memberMethod.Name, "Class_Initialize"))
                        {
                            CodeConstructor constructor = new CodeConstructor();
                            constructor.Statements.AddRange(memberMethod.Statements);
                            typeDecl.Members[i] = constructor;
                        }
                        else if (StringUtils.CaseInsensitiveEquals(memberMethod.Name, "Class_Terminate"))
                        {
                            CodeDestructor destructor = new CodeDestructor();
                            destructor.Statements.AddRange(memberMethod.Statements);
                            typeDecl.Members[i] = destructor;
                        }
                    }
                }
                else if (typeMember is CodeTypeDeclaration)
                {
                    Process((CodeTypeDeclaration) typeMember);
                }
            }
        }
    }
}