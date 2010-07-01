using System.CodeDom;
using Sage.SalesLogix.Migration.Script.CodeDom;

namespace Sage.SalesLogix.Migration.Script.Services
{
    /// <summary>
    /// In VBScript, the first parameter of a Set/Let property is the value being assigned.
    /// This needs to happen before PropertyPartConsolidator, as the set value parameter is lost.
    /// </summary>
    public sealed class PropertySetValueCorrector
    {
        private string _propertyName;
        private string _setValueName;

        public void Correct(CodeTypeDeclaration typeDecl)
        {
            foreach (CodeTypeMember typeMember in typeDecl.Members)
            {
                if (typeMember is CodeMemberProperty)
                {
                    CodeMemberProperty memberProperty = (CodeMemberProperty) typeMember;

                    if (memberProperty.HasSet && memberProperty.Parameters.Count == 1)
                    {
                        _propertyName = memberProperty.Name;
                        _setValueName = memberProperty.Parameters[0].Name;
                        new CodeDomWalker(memberProperty.SetStatements).Walk(Correct);
                        _setValueName = null;
                        _propertyName = null;
                        memberProperty.Parameters.Clear();
                    }
                }
                else if (typeMember is CodeTypeDeclaration)
                {
                    Correct((CodeTypeDeclaration) typeMember);
                }
            }
        }

        private void Correct(ref CodeObject target, CodeObject parent, int indent)
        {
            if (target is CodeVariableReferenceExpression)
            {
                CodeVariableReferenceExpression variableExpr = (CodeVariableReferenceExpression) target;

                if (StringUtils.CaseInsensitiveEquals(variableExpr.VariableName, _setValueName))
                {
                    variableExpr.VariableName = _propertyName;
                }
            }
            else if (target is CodeMethodInvokeExpression)
            {
                CodeMethodInvokeExpression methodInvokeExpr = (CodeMethodInvokeExpression) target;

                if (methodInvokeExpr.Method.TargetObject == null && StringUtils.CaseInsensitiveEquals(methodInvokeExpr.Method.MethodName, _setValueName))
                {
                    CodeIndexerExpression indexerExpr = new CodeIndexerExpression(
                        new CodeVariableReferenceExpression(_propertyName));
                    indexerExpr.Indices.AddRange(methodInvokeExpr.Parameters);
                    target = indexerExpr;
                }
            }
        }
    }
}