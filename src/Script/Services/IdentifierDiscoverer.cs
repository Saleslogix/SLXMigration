using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using Sage.SalesLogix.Migration.Script.CodeDom;

namespace Sage.SalesLogix.Migration.Script.Services
{
    /// <summary>
    /// Creates dictionaries of fields, methods, properties, nested types and variables
    /// and stores them against the code element for quick retrieval.
    /// This process is a prerequisite for some of the other translations.
    /// Also sets a parent property on nested types that references the containing type.
    /// </summary>
    public sealed class IdentifierDiscoverer
    {
        private readonly IExtendedLog _log;

        public IdentifierDiscoverer(IExtendedLog log)
        {
            _log = log;
        }

        public void Process(CodeTypeDeclaration typeDecl)
        {
            IDictionary<string, CodeMemberField> fields = new Dictionary<string, CodeMemberField>(StringComparer.InvariantCultureIgnoreCase);
            IDictionary<string, CodeMemberMethod> methods = new Dictionary<string, CodeMemberMethod>(StringComparer.InvariantCultureIgnoreCase);
            IDictionary<string, CodeMemberProperty> properties = new Dictionary<string, CodeMemberProperty>(StringComparer.InvariantCultureIgnoreCase);
            IDictionary<string, CodeTypeDeclaration> nestedTypes = new Dictionary<string, CodeTypeDeclaration>(StringComparer.InvariantCultureIgnoreCase);

            foreach (CodeTypeMember typeMember in typeDecl.Members)
            {
                string memberName = typeMember.Name;

                if (typeMember is CodeMemberField)
                {
                    CodeMemberField memberField = (CodeMemberField) typeMember;

                    if (fields.ContainsKey(memberName))
                    {
                        LogWarning("Field '{0}' already defined", memberName);
                    }
                    else
                    {
                        fields.Add(memberName, memberField);
                    }
                }
                else if (typeMember is CodeMemberMethod)
                {
                    CodeMemberMethod memberMethod = (CodeMemberMethod) typeMember;
                    DiscoverVariables(memberMethod.Statements);

                    if (methods.ContainsKey(memberName))
                    {
                        LogWarning("Method '{0}' already defined", memberName);
                    }
                    else
                    {
                        methods.Add(memberName, memberMethod);
                    }

                    CodeConstructor constructor = typeMember as CodeConstructor;

                    if (constructor != null)
                    {
                        CodeObjectMetaData.SetConstructor(typeDecl, constructor);
                    }
                }
                else if (typeMember is CodeMemberProperty)
                {
                    CodeMemberProperty memberProperty = (CodeMemberProperty) typeMember;
                    DiscoverVariables(memberProperty.GetStatements);
                    DiscoverVariables(memberProperty.SetStatements);

                    if (properties.ContainsKey(memberName))
                    {
                        LogWarning("Property '{0}' already defined", memberName);
                    }
                    else
                    {
                        properties.Add(memberName, memberProperty);
                    }
                }
                else if (typeMember is CodeTypeDeclaration)
                {
                    CodeTypeDeclaration subTypeDecl = (CodeTypeDeclaration) typeMember;
                    Process(subTypeDecl);

                    if (nestedTypes.ContainsKey(memberName))
                    {
                        LogWarning("Nested type '{0}' already defined", memberName);
                    }
                    else
                    {
                        nestedTypes.Add(memberName, subTypeDecl);
                    }
                }
                else
                {
                    Debug.Assert(typeMember is CodeSnippetTypeMember, "Unexpected type member type: " + typeMember.GetType().Name);
                }

                CodeObjectMetaData.SetParent(typeMember, typeDecl);
            }

            CodeObjectMetaData.SetFields(typeDecl, fields);
            CodeObjectMetaData.SetMethods(typeDecl, methods);
            CodeObjectMetaData.SetProperties(typeDecl, properties);
            CodeObjectMetaData.SetNestedTypes(typeDecl, nestedTypes);
        }

        private IDictionary<string, CodeVariableDeclarationStatement> _variables;

        private void DiscoverVariables(CodeStatementCollection stmts)
        {
            _variables = new Dictionary<string, CodeVariableDeclarationStatement>(StringComparer.InvariantCultureIgnoreCase);
            new CodeDomWalker(stmts).Walk(DiscoverVariables);
            CodeObjectMetaData.SetVariables(stmts, _variables);
            _variables = null;
        }

        private void DiscoverVariables(ref CodeObject target, CodeObject parent, int indent)
        {
            if (target is CodeVariableDeclarationStatement)
            {
                CodeVariableDeclarationStatement variableDeclStmt = (CodeVariableDeclarationStatement) target;
                string variableName = variableDeclStmt.Name;

                if (_variables.ContainsKey(variableName))
                {
                    LogWarning("Variable '{0}' already defined", variableName);
                }
                else
                {
                    _variables.Add(variableName, variableDeclStmt);
                }
            }
        }

        private void LogWarning(string text, params object[] args)
        {
            if (_log != null)
            {
                _log.Warn(text, args);
            }
        }
    }
}