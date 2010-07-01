using System;
using System.CodeDom;
using System.Collections;
using Sage.SalesLogix.Migration.Script.CodeDom;

namespace Sage.SalesLogix.Migration.Script.Services
{
    /// <summary>
    /// In VBS, each function and property has an implied return variable of the same name.
    /// This translation changes the name of these variables to 'returnValue', a variable
    /// that is declared on the first line and returned on the last.
    /// Also replaces set value variable references in property setters with the correct
    /// CodeDom node.
    /// Must be done after type inference due to initialization of return value declaration.
    /// </summary>
    public sealed class ReturnValueCorrector
    {
        private const string _returnVariableName = "returnValue";

        public void Correct(CodeTypeDeclaration typeDecl)
        {
            foreach (CodeTypeMember typeMember in typeDecl.Members)
            {
                if (typeMember is CodeMemberMethod)
                {
                    CodeMemberMethod memberMethod = (CodeMemberMethod) typeMember;
                    CodeObjectSource source = Utils.GetTypeReferenceSource(memberMethod.ReturnType);

                    if (source == null || source.Target != typeof (void) || source.ArrayRanks.Length > 0)
                    {
                        CorrectStatements(memberMethod.Name, memberMethod.Statements, memberMethod.ReturnType);
                    }
                }
                else if (typeMember is CodeMemberProperty)
                {
                    CodeMemberProperty memberProperty = (CodeMemberProperty) typeMember;

                    if (memberProperty.HasGet)
                    {
                        CorrectStatements(memberProperty.Name, memberProperty.GetStatements, memberProperty.Type);
                    }

                    if (memberProperty.HasSet)
                    {
                        ProcessSetStatements(memberProperty.Name, memberProperty.SetStatements);
                    }
                }
                else if (typeMember is CodeTypeDeclaration)
                {
                    Correct((CodeTypeDeclaration) typeMember);
                }
            }
        }

        private string _name;
        private int _referenceCount;

        private void CorrectStatements(string name, CodeStatementCollection stmts, CodeTypeReference returnType)
        {
            _name = name;
            _referenceCount = 0;
            new CodeDomWalker(stmts).Walk(RenameReturnValue);
            _name = null;

            CodeObjectSource source = Utils.GetTypeReferenceSource(returnType);
            Type type = (source == null ? null : source.Target as Type);
            CodeExpression defaultExpr = (type != null && type.IsPrimitive
                                              ? (CodeExpression) new CodeDefaultValueExpression(returnType)
                                              : new CodePrimitiveExpression(null));
            CodeExpression returnExpr = null;

            if (_referenceCount > 0)
            {
                bool requiresDeclaration = true;
                CodeAssignStatement assignStmt = stmts[stmts.Count - 1] as CodeAssignStatement;

                if (assignStmt != null)
                {
                    CodeVariableReferenceExpression variableStmt = assignStmt.Left as CodeVariableReferenceExpression;

                    if (variableStmt != null && StringUtils.CaseInsensitiveEquals(variableStmt.VariableName, _returnVariableName))
                    {
                        stmts.RemoveAt(stmts.Count - 1);
                        returnExpr = assignStmt.Right;
                        requiresDeclaration = (_referenceCount > 1);
                    }
                }

                if (requiresDeclaration)
                {
                    CodeExpression initExpression = null;
                    assignStmt = stmts[0] as CodeAssignStatement;

                    if (assignStmt != null)
                    {
                        CodeVariableReferenceExpression variableStmt = assignStmt.Left as CodeVariableReferenceExpression;

                        if (variableStmt != null && StringUtils.CaseInsensitiveEquals(variableStmt.VariableName, _returnVariableName))
                        {
                            stmts.RemoveAt(0);
                            initExpression = assignStmt.Right;
                        }
                    }

                    if (initExpression == null)
                    {
                        initExpression = defaultExpr;
                    }

                    stmts.Insert(
                        0,
                        new CodeVariableDeclarationStatement(
                            returnType,
                            _returnVariableName,
                            initExpression));
                }

                if (returnExpr == null)
                {
                    returnExpr = new CodeVariableReferenceExpression(_returnVariableName);
                }
            }
            else
            {
                returnExpr = defaultExpr;
            }

            stmts.Add(new CodeMethodReturnStatement(returnExpr));
        }

        private void RenameReturnValue(ref CodeObject target, CodeObject parent, int indent)
        {
            if (target is CodeVariableReferenceExpression)
            {
                CodeVariableReferenceExpression variableRefExpr = (CodeVariableReferenceExpression) target;

                if (StringUtils.CaseInsensitiveEquals(variableRefExpr.VariableName, _name))
                {
                    variableRefExpr.VariableName = _returnVariableName;
                    _referenceCount++;
                }
            }
            else if (target is CodePropertyReferenceExpression)
            {
                CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) target;

                if (propertyRefExpr.TargetObject == null && StringUtils.CaseInsensitiveEquals(propertyRefExpr.PropertyName, _name))
                {
                    target = new CodeVariableReferenceExpression(_returnVariableName);
                    _referenceCount++;
                }
            }
            else if (target is CodeMethodReferenceExpression)
            {
                CodeMethodReferenceExpression methodRefExpr = (CodeMethodReferenceExpression) target;

                if (methodRefExpr.TargetObject == null && StringUtils.CaseInsensitiveEquals(methodRefExpr.MethodName, _name))
                {
                    methodRefExpr.MethodName = _returnVariableName;
                    _referenceCount++;
                }
            }
            else if (target is CodeMethodReturnStatement)
            {
                CodeMethodReturnStatement methodReturnStmt = (CodeMethodReturnStatement) target;

                if (methodReturnStmt.Expression == null)
                {
                    methodReturnStmt.Expression = new CodeVariableReferenceExpression(_returnVariableName);
                    _referenceCount++;
                }
            }
        }

        private void ProcessSetStatements(string name, IList stmts)
        {
            _name = name;
            new CodeDomWalker(stmts).Walk(ReplaceSetValue);
            _name = null;
        }

        private void ReplaceSetValue(ref CodeObject target, CodeObject parent, int indent)
        {
            CodeVariableReferenceExpression variableRefExpr = target as CodeVariableReferenceExpression;

            if (variableRefExpr != null && StringUtils.CaseInsensitiveEquals(variableRefExpr.VariableName, _name))
            {
                target = new CodePropertySetValueReferenceExpression();
            }
        }
    }
}