using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using ADODB;
using Iesi.Collections.Generic;
using Interop.SalesLogix;
using Interop.SLXCharts;
using Interop.SLXControls;
using Interop.SLXDialogs;
using Microsoft.VisualBasic;
using Sage.SalesLogix.Migration.Script.CodeDom;
using Debug=System.Diagnostics.Debug;
using Image=System.Drawing.Image;

namespace Sage.SalesLogix.Migration.Script.Services
{
    public sealed class IdentifierResolver
    {
        private static readonly IDictionary<string, MethodInfo> _globalMethods;
        private static readonly IDictionary<string, FieldInfo> _globalFields;

        static IdentifierResolver()
        {
            _globalMethods = new Dictionary<string, MethodInfo>(StringComparer.InvariantCultureIgnoreCase);
            _globalFields = new Dictionary<string, FieldInfo>(StringComparer.InvariantCultureIgnoreCase);

            FillConstants(typeof (Connection).Assembly);
            FillConstants(typeof (ISlxApplication2).Assembly);
            FillConstants(typeof (IEditX).Assembly);
            FillConstants(typeof (IChart).Assembly);
            FillConstants(typeof (IOpenDialog).Assembly);

            FillConstants(typeof (Constants));

            FillMethods(typeof (Strings));
            FillMethods(typeof (Interaction));
            FillMethods(typeof (DateAndTime));
            FillMethods(typeof (Information));
            FillMethods(typeof (Conversion));
            FillMethods(typeof (Math));
        }

        private static void FillConstants(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsEnum)
                {
                    foreach (string name in Enum.GetNames(type))
                    {
                        FieldInfo fieldInfo;

                        if (_globalFields.TryGetValue(name, out fieldInfo))
                        {
                            //workaround due to duplicate enum defs in ADODB, specifically SearchDirection and SearchDirectionEnum
                            //the latter should win as it is the one referenced elsewhere

                            if (type.Name.EndsWith("Enum"))
                            {
                                _globalFields[name] = type.GetField(name);
                            }
                        }
                        else
                        {
                            _globalFields.Add(name, type.GetField(name));
                        }
                    }
                }
            }
        }

        private static void FillConstants(Type type)
        {
            foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                _globalFields.Add(fieldInfo.Name, fieldInfo);
            }
        }

        private static void FillMethods(Type type)
        {
            foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (!methodInfo.IsSpecialName)
                {
                    if (!_globalMethods.ContainsKey(methodInfo.Name))
                    {
                        _globalMethods.Add(methodInfo.Name, methodInfo);
                    }
                }
            }
        }

        private const int _attempts = 5;
        private readonly IExtendedLog _log;
        private bool _finalAttempt;

        public IdentifierResolver(IExtendedLog log)
        {
            _log = log;
        }

        private CodeTypeDeclaration _typeDecl;

        private IDictionary<string, CodeMemberField> _localFields;
        private IDictionary<string, CodeMemberMethod> _localMethods;
        private IDictionary<string, CodeMemberProperty> _localProperties;
        private IDictionary<string, CodeTypeDeclaration> _localNestedTypes;

        private IDictionary<string, CodeMemberField> _outerFields;
        private IDictionary<string, CodeMemberMethod> _outerMethods;
        private IDictionary<string, CodeMemberProperty> _outerProperties;
        private IDictionary<string, CodeTypeDeclaration> _outerNestedTypes;

        private string _formName;
        private IDictionary<string, FormField> _formFields;

        private bool _requiresMissingValue;
        private ICollection<CodeTypeDeclaration> _requiredOuterReferences;
        private ICollection<FormField> _requiredFormFields;

        public void Resolve(ICollection<ScriptInfo> scripts)
        {
            _warnings = new HashedSet<string>();
            _errors = new HashedSet<string>();
            int counter;

            for (counter = 0; counter <= _attempts; counter++)
            {
                bool changes = false;

                if (counter == _attempts)
                {
                    _finalAttempt = true;
                }

                foreach (ScriptInfo script in scripts)
                {
                    if (!script.IsInvalid)
                    {
                        changes |= ResolveScript(script);
                    }
                }

                if (!changes)
                {
                    break;
                }
            }

            //Debug.Assert(counter < _attempts);
            _finalAttempt = false;
            _warnings = null;
            _errors = null;
        }

        private bool ResolveScript(ScriptInfo script)
        {
            IDictionary<string, CodeMemberField> includeFields;
            IDictionary<string, CodeMemberMethod> includeMethods;
            IDictionary<string, CodeMemberProperty> includeProperties;
            IDictionary<string, CodeTypeDeclaration> includeNestedTypes;

            if (script.Dependencies.Count > 0)
            {
                includeFields = new Dictionary<string, CodeMemberField>(StringComparer.InvariantCultureIgnoreCase);
                includeMethods = new Dictionary<string, CodeMemberMethod>(StringComparer.InvariantCultureIgnoreCase);
                includeProperties = new Dictionary<string, CodeMemberProperty>(StringComparer.InvariantCultureIgnoreCase);
                includeNestedTypes = new Dictionary<string, CodeTypeDeclaration>(StringComparer.InvariantCultureIgnoreCase);

                foreach (ScriptInfo dependencyScript in script.Dependencies.Values)
                {
                    if (!dependencyScript.IsInvalid)
                    {
                        foreach (KeyValuePair<string, CodeMemberField> field in CodeObjectMetaData.GetFields(dependencyScript.TypeDeclaration))
                        {
                            if (includeFields.ContainsKey(field.Key))
                            {
                                LogWarning("Field '{0}' already defined", field.Key);
                            }
                            else
                            {
                                includeFields.Add(field.Key, field.Value);
                            }
                        }

                        foreach (KeyValuePair<string, CodeMemberMethod> method in CodeObjectMetaData.GetMethods(dependencyScript.TypeDeclaration))
                        {
                            if (includeMethods.ContainsKey(method.Key))
                            {
                                LogWarning("Method '{0}' already defined", method.Key);
                            }
                            else
                            {
                                includeMethods.Add(method.Key, method.Value);
                            }
                        }

                        foreach (KeyValuePair<string, CodeMemberProperty> property in CodeObjectMetaData.GetProperties(dependencyScript.TypeDeclaration))
                        {
                            if (includeProperties.ContainsKey(property.Key))
                            {
                                LogWarning("Property '{0}' already defined", property.Key);
                            }
                            else
                            {
                                includeProperties.Add(property.Key, property.Value);
                            }
                        }

                        foreach (KeyValuePair<string, CodeTypeDeclaration> nestedType in CodeObjectMetaData.GetNestedTypes(dependencyScript.TypeDeclaration))
                        {
                            if (includeNestedTypes.ContainsKey(nestedType.Key))
                            {
                                LogWarning("Nested type '{0}' already defined", nestedType.Key);
                            }
                            else
                            {
                                includeNestedTypes.Add(nestedType.Key, nestedType.Value);
                            }
                        }
                    }
                }
            }
            else
            {
                includeFields = null;
                includeMethods = null;
                includeProperties = null;
                includeNestedTypes = null;
            }

            return ProcessTypeDecl(
                script.TypeDeclaration,
                includeFields,
                includeMethods,
                includeProperties,
                includeNestedTypes,
                script.FormName,
                script.FormControls,
                new HashedSet<FormField>());
        }

        private bool ProcessTypeDecl(
            CodeTypeDeclaration typeDecl,
            IDictionary<string, CodeMemberField> outerFields,
            IDictionary<string, CodeMemberMethod> outerMethods,
            IDictionary<string, CodeMemberProperty> outerProperties,
            IDictionary<string, CodeTypeDeclaration> outerNestedTypes,
            string formName,
            IDictionary<string, FormField> formFields,
            ICollection<FormField> requiredFormFields)
        {
            _typeDecl = typeDecl;
            _localFields = CodeObjectMetaData.GetFields(typeDecl);
            _localMethods = CodeObjectMetaData.GetMethods(typeDecl);
            _localProperties = CodeObjectMetaData.GetProperties(typeDecl);
            _localNestedTypes = CodeObjectMetaData.GetNestedTypes(typeDecl);
            _outerFields = outerFields;
            _outerMethods = outerMethods;
            _outerProperties = outerProperties;
            _outerNestedTypes = outerNestedTypes;
            _formName = formName;
            _formFields = formFields;
            _requiredFormFields = requiredFormFields;
            bool overallChanges = false;
            int counter;

            for (counter = 0; counter < _attempts; counter++)
            {
                bool loopChanges = false;

                foreach (CodeTypeMember typeMember in typeDecl.Members)
                {
                    loopChanges |= ProcessTypeMember(typeMember);
                }

                if (_finalAttempt)
                {
                    break;
                }

                if (loopChanges)
                {
                    overallChanges = true;
                }
                else
                {
                    break;
                }
            }

            //Debug.Assert(counter < _attempts);

            if (_requiredFormFields != null && CodeObjectMetaData.GetParent(_typeDecl) == null)
            {
                foreach (FormField formField in _requiredFormFields)
                {
                    CodeMemberField memberField = new CodeMemberField(
                        Utils.CreateTypeReference(formField.Type),
                        formField.Name);
                    CodeObjectMetaData.SetEntitySource(memberField, CodeObjectSource.Create(formField.Type));
                    typeDecl.Members.Insert(0, memberField);
                }
            }

            if (_requiredOuterReferences != null)
            {
                foreach (CodeTypeDeclaration outerTypeDecl in _requiredOuterReferences)
                {
                    CodeMemberField memberField = new CodeMemberField(
                        Utils.CreateTypeReference(outerTypeDecl),
                        GenerateFieldName(outerTypeDecl.Name));
                    CodeObjectMetaData.SetEntitySource(memberField, CodeObjectSource.Create(outerTypeDecl));
                    typeDecl.Members.Insert(0, memberField);
                }
            }

            if (_requiresMissingValue)
            {
                CodeMemberField memberField = new CodeMemberField(
                    Utils.CreateTypeReference(typeof (Missing)),
                    "_missingValue");
                memberField.InitExpression = new CodeFieldReferenceExpression(
                    new CodeTypeReferenceExpression(
                        Utils.CreateTypeReference(typeof (Missing))), "Value");
                CodeObjectMetaData.SetEntitySource(memberField, CodeObjectSource.Create(typeof (Missing)));
                typeDecl.Members.Insert(0, memberField);
            }

            _typeDecl = null;
            _localFields = null;
            _localMethods = null;
            _localProperties = null;
            _localNestedTypes = null;
            _outerFields = null;
            _outerMethods = null;
            _outerProperties = null;
            _outerNestedTypes = null;
            _formName = null;
            _formFields = null;
            _requiredFormFields = null;
            _requiresMissingValue = false;
            _requiredOuterReferences = null;
            return overallChanges;
        }

        private bool ProcessTypeMember(CodeTypeMember typeMember)
        {
            if (typeMember is CodeMemberField)
            {
                return ProcessMemberField((CodeMemberField) typeMember);
            }
            else if (typeMember is CodeMemberMethod)
            {
                return ProcessMemberMethod((CodeMemberMethod) typeMember);
            }
            else if (typeMember is CodeMemberProperty)
            {
                return ProcessMemberProperty((CodeMemberProperty) typeMember);
            }
            else if (typeMember is CodeTypeDeclaration)
            {
                return ProcessNestedType((CodeTypeDeclaration) typeMember);
            }
            else
            {
                Debug.Assert(typeMember is CodeSnippetTypeMember);
            }

            return false;
        }

        private bool ProcessMemberField(CodeMemberField memberField)
        {
            if (memberField.InitExpression != null && !CodeObjectMetaData.HasEntitySource(memberField))
            {
                if (memberField.InitExpression is CodePrimitiveExpression)
                {
                    CodePrimitiveExpression primitiveExpr = (CodePrimitiveExpression) memberField.InitExpression;
                    CodeObjectSource source = CodeObjectSource.Create(primitiveExpr.Value == null
                                                                          ? (object) NullPlaceholder.Value
                                                                          : primitiveExpr.Value.GetType());
                    return ImplyEntityType(CodeObjectSource.Create(memberField), source);
                }
                else if (memberField.InitExpression is CodeObjectCreateExpression)
                {
                    CodeObjectCreateExpression objectCreateExpr = (CodeObjectCreateExpression) memberField.InitExpression;
                    return ImplyEntityType(
                        CodeObjectSource.Create(memberField),
                        Utils.GetTypeReferenceSource(objectCreateExpr.CreateType));
                }
                else
                {
                    Debug.Assert(memberField.InitExpression is CodeCastExpression || memberField.InitExpression is CodeArrayCreateExpression);
                }
            }

            return false;
        }

        private bool ProcessMemberMethod(CodeMemberMethod memberMethod)
        {
            IDictionary<string, CodeParameterDeclarationExpression> parameters = new Dictionary<string, CodeParameterDeclarationExpression>(StringComparer.InvariantCultureIgnoreCase);

            foreach (CodeParameterDeclarationExpression parameterDeclExpr in memberMethod.Parameters)
            {
                parameters.Add(parameterDeclExpr.Name, parameterDeclExpr);
            }

            return ProcessStatements(memberMethod, memberMethod.Statements, parameters);
        }

        private bool ProcessMemberProperty(CodeMemberProperty memberProperty)
        {
            IDictionary<string, CodeParameterDeclarationExpression> parameters = new Dictionary<string, CodeParameterDeclarationExpression>(StringComparer.InvariantCultureIgnoreCase);

            foreach (CodeParameterDeclarationExpression parameterDeclExpr in memberProperty.Parameters)
            {
                parameters.Add(parameterDeclExpr.Name, parameterDeclExpr);
            }

            bool changed = ProcessStatements(memberProperty, memberProperty.GetStatements, parameters);
            changed |= ProcessStatements(memberProperty, memberProperty.SetStatements, parameters);
            return changed;
        }

        private bool ProcessNestedType(CodeTypeDeclaration nestedTypeDecl)
        {
            return new IdentifierResolver(_log).ProcessTypeDecl(
                nestedTypeDecl,
                MergeMembers(_localFields, _outerFields, "field"),
                MergeMembers(_localMethods, _outerMethods, "method"),
                MergeMembers(_localProperties, _outerProperties, "property"),
                MergeMembers(_localNestedTypes, _outerNestedTypes, "nested type"),
                _formName,
                _formFields,
                _requiredFormFields);
        }

        //---------------------------------------

        private CodeTypeMember _typeMember;
        private IDictionary<string, CodeVariableDeclarationStatement> _variables;
        private IDictionary<string, CodeParameterDeclarationExpression> _parameters;
        private bool _changes;

        private bool ProcessStatements(
            CodeTypeMember typeMember,
            CodeStatementCollection statements,
            IDictionary<string, CodeParameterDeclarationExpression> parameters)
        {
            _typeMember = typeMember;
            _variables = CodeObjectMetaData.GetVariables(statements);
            _parameters = parameters;
            bool overallChanges = false;
            int counter;

            for (counter = 0; counter < _attempts; counter++)
            {
                bool loopChanges;

                _changes = false;
                new CodeDomWalker(statements).Walk(ProcessStatements);
                loopChanges = _changes;
                _changes = false;

                if (_finalAttempt)
                {
                    break;
                }

                if (loopChanges)
                {
                    overallChanges = true;
                }
                else
                {
                    break;
                }
            }

            //Debug.Assert(counter < _attempts);
            _typeMember = null;
            _variables = null;
            _parameters = null;
            return overallChanges;
        }

        private void ProcessStatements(ref CodeObject target, CodeObject parent, int indent)
        {
            if (target is CodeExpression)
            {
                CodeExpression expr = (CodeExpression) target;

                if (!CodeObjectMetaData.HasExpressionSource(expr))
                {
                    if (target is CodeVariableReferenceExpression)
                    {
                        _changes = ProcessVariableReference(ref target);
                    }
                    else if (target is CodeMethodInvokeExpression)
                    {
                        _changes = ProcessMethodInvoke(ref target);
                    }
                    else if (target is CodePropertyReferenceExpression)
                    {
                        _changes = ProcessPropertyReference(ref target);
                    }
                    else if (target is CodeObjectCreateExpression)
                    {
                        _changes = ProcessObjectCreate((CodeObjectCreateExpression) target);
                    }
                    else if (target is CodePrimitiveExpression)
                    {
                        _changes = ProcessPrimitive((CodePrimitiveExpression) target);
                    }
                    else if (target is CodeBinaryOperatorExpression)
                    {
                        _changes = ProcessBinaryOperator((CodeBinaryOperatorExpression) target);
                    }
                    else if (target is CodeArrayCreateExpression)
                    {
                        _changes = ProcessArrayCreate((CodeArrayCreateExpression) target);
                    }
                    else if (target is CodeFieldReferenceExpression)
                    {
                        _changes = ProcessFieldReference((CodeFieldReferenceExpression) target);
                    }
                    else if (target is CodeTypeOfExpression)
                    {
                        _changes = ProcessTypeOf((CodeTypeOfExpression) target);
                    }
                    else if (target is CodeCastExpression)
                    {
                        _changes = ProcessCast((CodeCastExpression) target);
                    }
                    else if (target is CodeMethodReferenceExpression) {}
                    else if (target is CodeTypeReferenceExpression) {}
                    else
                    {
                        Debug.Assert(false);
                    }
                }
                else
                {
                    CodeObjectSource source = CodeObjectMetaData.GetExpressionSource(expr);

                    if (source != null && source.ArrayRanks.Length == 0)
                    {
                        if (target is CodeMethodInvokeExpression)
                        {
                            CodeMethodInvokeExpression methodInvokeExpr = (CodeMethodInvokeExpression) target;
                            CodeMemberMethod memberMethod = source.Target as CodeMemberMethod;

                            if (memberMethod != null)
                            {
                                _changes = ProcessParameters(memberMethod.Parameters, methodInvokeExpr.Parameters, memberMethod.Name);
                            }
                        }
                        else if (target is CodeIndexerExpression)
                        {
                            CodeIndexerExpression indexerExpr = (CodeIndexerExpression) target;
                            CodeMemberProperty memberProperty = source.Target as CodeMemberProperty;

                            if (memberProperty != null)
                            {
                                _changes = ProcessParameters(memberProperty.Parameters, indexerExpr.Indices, memberProperty.Name);
                            }
                        }
                    }
                }
            }
            else if (target is CodeAssignStatement)
            {
                _changes = ProcessAssign((CodeAssignStatement) target);
            }
            else if (target is CodeForEachStatement)
            {
                _changes = ProcessForEach((CodeForEachStatement) target);
            }
            else if (target is CodeConditionStatement)
            {
                _changes = ProcessCondition((CodeConditionStatement) target);
            }
            else if (target is CodeIterationStatement)
            {
                _changes = ProcessIteration((CodeIterationStatement) target);
            }
            else if (target is CodeVariableDeclarationStatement)
            {
                _changes = ProcessVariableDeclaration((CodeVariableDeclarationStatement) target);
            }
        }

        //---------------------------------------

        private bool ProcessVariableReference(ref CodeObject target)
        {
            CodeVariableReferenceExpression variableRefExpr = (CodeVariableReferenceExpression) target;
            string name = variableRefExpr.VariableName;
            CodeVariableDeclarationStatement variableDeclStmt;
            CodeParameterDeclarationExpression parameterDeclExpr;
            CodeMemberField memberField;
            CodeMemberMethod memberMethod;
            CodeMemberProperty memberProperty;
            FormField formField;
            MemberInfo[] members;
            FieldInfo globalFieldInfo;
            MethodInfo globalMethodInfo;

            if (StringUtils.CaseInsensitiveEquals(name, _typeMember.Name))
            {
                variableRefExpr.VariableName = _typeMember.Name;
                CodeObjectMetaData.SetExpressionSource(variableRefExpr, CodeObjectSource.Create(_typeMember));
            }
            else if (_formName != null && StringUtils.CaseInsensitiveEquals(name, _formName))
            {
                CodeTypeDeclaration parentTypeDecl = CodeObjectMetaData.GetParent(_typeDecl);
                CodeExpression targetExpr = new CodeThisReferenceExpression();

                if (parentTypeDecl != null)
                {
                    CodeObjectMetaData.SetExpressionSource(targetExpr, CodeObjectSource.Create(_typeDecl));
                    targetExpr = new CodeFieldReferenceExpression(
                        targetExpr,
                        GenerateFieldName(parentTypeDecl.Name));
                    (_requiredOuterReferences ?? (_requiredOuterReferences = new HashedSet<CodeTypeDeclaration>())).Add(parentTypeDecl);
                }

                CodeObjectMetaData.SetExpressionSource(targetExpr, CodeObjectSource.Create(FormPlaceholder.Value));
                target = targetExpr;
            }
            else if (_variables.TryGetValue(name, out variableDeclStmt))
            {
                variableRefExpr.VariableName = variableDeclStmt.Name;
                CodeObjectMetaData.SetExpressionSource(variableRefExpr, CodeObjectSource.Create(variableDeclStmt));
            }
            else if (_parameters.TryGetValue(name, out parameterDeclExpr))
            {
                CodeArgumentReferenceExpression argumentRefExpr = new CodeArgumentReferenceExpression(parameterDeclExpr.Name);
                CodeObjectMetaData.SetExpressionSource(argumentRefExpr, CodeObjectSource.Create(parameterDeclExpr));
                target = argumentRefExpr;
            }
            else if (_localFields.TryGetValue(name, out memberField))
            {
                CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(
                    CreateLocalMemberTarget(memberField),
                    memberField.Name);
                CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(memberField));
                target = fieldRefExpr;
            }
            else if (_localMethods.TryGetValue(name, out memberMethod))
            {
                CodeMethodInvokeExpression methodInvokeExpr = new CodeMethodInvokeExpression(
                    CreateLocalMemberTarget(memberMethod),
                    memberMethod.Name);
                CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(memberMethod));
                target = methodInvokeExpr;
            }
            else if (_localProperties.TryGetValue(name, out memberProperty))
            {
                CodePropertyReferenceExpression propertyRefExpr = new CodePropertyReferenceExpression(
                    CreateLocalMemberTarget(memberProperty),
                    memberProperty.Name);
                CodeObjectMetaData.SetExpressionSource(propertyRefExpr, CodeObjectSource.Create(memberProperty));
                target = propertyRefExpr;
            }
            else if (_outerFields != null && _outerFields.TryGetValue(name, out memberField))
            {
                CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(
                    CreateOuterMemberTarget(memberField),
                    memberField.Name);
                CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(memberField));
                CodeDomUtils.SetMemberAttributes(memberField, MemberAttributes.Public);
                target = fieldRefExpr;
            }
            else if (_outerMethods != null && _outerMethods.TryGetValue(name, out memberMethod))
            {
                CodeMethodInvokeExpression methodInvokeExpr = new CodeMethodInvokeExpression(
                    CreateOuterMemberTarget(memberMethod),
                    memberMethod.Name);
                CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(memberMethod));
                CodeDomUtils.SetMemberAttributes(memberMethod, MemberAttributes.Public);
                target = methodInvokeExpr;
            }
            else if (_outerProperties != null && _outerProperties.TryGetValue(name, out memberProperty))
            {
                CodePropertyReferenceExpression propertyRefExpr = new CodePropertyReferenceExpression(
                    CreateOuterMemberTarget(memberProperty),
                    memberProperty.Name);
                CodeObjectMetaData.SetExpressionSource(propertyRefExpr, CodeObjectSource.Create(memberProperty));
                CodeDomUtils.SetMemberAttributes(memberProperty, MemberAttributes.Public);
                target = propertyRefExpr;
            }
            else if (_formFields != null && _formFields.TryGetValue(name, out formField))
            {
                CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(
                    CreateFormMemberTarget(),
                    formField.Name);
                CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(formField.Type));
                (_requiredFormFields ?? (_requiredFormFields = new HashedSet<FormField>())).Add(formField);
                target = fieldRefExpr;
            }
            else if ((members = typeof (IAxForm).GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)) != null && members.Length == 1)
            {
                PropertyInfo property = (PropertyInfo) members[0];
                CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(
                    CreateFormMemberTarget(),
                    property.Name);
                CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(property.PropertyType));
                (_requiredFormFields ?? (_requiredFormFields = new HashedSet<FormField>())).Add(new FormField(property.Name, property.PropertyType));
                target = fieldRefExpr;
            }
            else if (_globalFields.TryGetValue(name, out globalFieldInfo))
            {
                CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(
                    new CodeTypeReferenceExpression(Utils.CreateTypeReference(globalFieldInfo.DeclaringType)),
                    globalFieldInfo.Name);
                CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(globalFieldInfo.FieldType));
                target = fieldRefExpr;
            }
            else if (_globalMethods.TryGetValue(name, out globalMethodInfo))
            {
                CodeMethodInvokeExpression methodInvokeExpr = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression(Utils.CreateTypeReference(globalMethodInfo.DeclaringType)),
                    globalMethodInfo.Name);
                CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(globalMethodInfo.ReturnType));
                ProcessParameters(globalMethodInfo.GetParameters(), methodInvokeExpr.Parameters);
                target = methodInvokeExpr;
            }
            else
            {
                switch (name.ToLower())
                {
                    case "me":
                        CodeThisReferenceExpression thisRefExpr = new CodeThisReferenceExpression();
                        CodeObjectMetaData.SetExpressionSource(thisRefExpr, CodeObjectSource.Create(_typeDecl));
                        target = thisRefExpr;
                        break;
                    case "now":
                        target = CreatePropertyReference(typeof (DateTime), "Now");
                        break;
                    case "date":
                        target = CreatePropertyReference(typeof (DateTime), "Today");
                        break;
                    case "time":
                        target = CreatePropertyReference(typeof (DateTime), "TimeOfDay");
                        break;
                    case "vberror":
                        target = CreateFieldReference(typeof (VariantType), "Error");
                        break;
                    case "vbdataobject":
                        target = CreateFieldReference(typeof (VariantType), "DataObject");
                        break;
                    case "vbblack":
                        target = CreateColorReference("Black");
                        break;
                    case "vbred":
                        target = CreateColorReference("Red");
                        break;
                    case "vbgreen":
                        target = CreateColorReference("Lime");
                        break;
                    case "vbyellow":
                        target = CreateColorReference("Yellow");
                        break;
                    case "vbblue":
                        target = CreateColorReference("Blue");
                        break;
                    case "vbmagenta":
                        target = CreateColorReference("Magenta");
                        break;
                    case "vbcyan":
                        target = CreateColorReference("Cyan");
                        break;
                    case "vbwhite":
                        target = CreateColorReference("White");
                        break;
                    default:
                        LogWarning("Identifier '{0}' not found in class '{1}'", name, _typeDecl.Name);
                        CodeObjectMetaData.SetExpressionSource(variableRefExpr, null);
                        return false;
                }
            }

            return true;
        }

        private bool ProcessMethodInvoke(ref CodeObject target)
        {
            CodeMethodInvokeExpression methodInvokeExpr = (CodeMethodInvokeExpression) target;
            CodeMethodReferenceExpression methodRefExpr = methodInvokeExpr.Method;
            string name = methodRefExpr.MethodName;

            if (methodRefExpr.TargetObject == null)
            {
                CodeVariableDeclarationStatement variableDeclStmt;
                CodeParameterDeclarationExpression parameterDeclExpr;
                CodeMemberField memberField;
                CodeMemberMethod memberMethod;
                CodeMemberProperty memberProperty;
                FieldInfo globalFieldInfo;
                MethodInfo globalMethodInfo;

                if (_variables.TryGetValue(name, out variableDeclStmt))
                {
                    CodeVariableReferenceExpression variableRefExpr = new CodeVariableReferenceExpression(variableDeclStmt.Name);
                    CodeObjectMetaData.SetExpressionSource(variableRefExpr, CodeObjectSource.Create(variableDeclStmt));
                    CodeIndexerExpression indexerExpr = new CodeIndexerExpression(variableRefExpr);
                    indexerExpr.Indices.AddRange(methodInvokeExpr.Parameters);
                    CodeObjectMetaData.SetExpressionSource(indexerExpr, CodeObjectSource.CreateIndexer(variableDeclStmt, indexerExpr.Indices.Count));
                    target = indexerExpr;
                }
                else if (_parameters.TryGetValue(name, out parameterDeclExpr))
                {
                    CodeArgumentReferenceExpression argumentRefExpr = new CodeArgumentReferenceExpression(parameterDeclExpr.Name);
                    CodeObjectMetaData.SetExpressionSource(argumentRefExpr, CodeObjectSource.Create(parameterDeclExpr));
                    CodeIndexerExpression indexerExpr = new CodeIndexerExpression(argumentRefExpr);
                    indexerExpr.Indices.AddRange(methodInvokeExpr.Parameters);
                    CodeObjectMetaData.SetExpressionSource(indexerExpr, CodeObjectSource.CreateIndexer(parameterDeclExpr, indexerExpr.Indices.Count));
                    target = indexerExpr;
                }
                else if (_localFields.TryGetValue(name, out memberField))
                {
                    CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(CreateLocalMemberTarget(memberField), memberField.Name);
                    CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(memberField));
                    CodeIndexerExpression indexerExpr = new CodeIndexerExpression(fieldRefExpr);
                    indexerExpr.Indices.AddRange(methodInvokeExpr.Parameters);
                    CodeObjectMetaData.SetExpressionSource(indexerExpr, CodeObjectSource.CreateIndexer(memberField, indexerExpr.Indices.Count));
                    target = indexerExpr;
                }
                else if (_localMethods.TryGetValue(name, out memberMethod))
                {
                    methodRefExpr.TargetObject = CreateLocalMemberTarget(memberMethod);
                    methodRefExpr.MethodName = memberMethod.Name;
                    CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(memberMethod));
                }
                else if (_localProperties.TryGetValue(name, out memberProperty))
                {
                    CodePropertyReferenceExpression propertyRefExpr = new CodePropertyReferenceExpression(CreateLocalMemberTarget(memberProperty), memberProperty.Name);
                    CodeObjectMetaData.SetExpressionSource(propertyRefExpr, CodeObjectSource.Create(memberProperty));
                    CodeIndexerExpression indexerExpr = new CodeIndexerExpression(propertyRefExpr);
                    indexerExpr.Indices.AddRange(methodInvokeExpr.Parameters);
                    CodeObjectMetaData.SetExpressionSource(indexerExpr, CodeObjectSource.CreateIndexer(memberProperty, indexerExpr.Indices.Count));
                    target = indexerExpr;
                }
                else if (_outerFields != null && _outerFields.TryGetValue(name, out memberField))
                {
                    CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(CreateOuterMemberTarget(memberField), memberField.Name);
                    CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(memberField));
                    CodeIndexerExpression indexerExpr = new CodeIndexerExpression(fieldRefExpr);
                    indexerExpr.Indices.AddRange(methodInvokeExpr.Parameters);
                    CodeObjectMetaData.SetExpressionSource(indexerExpr, CodeObjectSource.CreateIndexer(memberField, indexerExpr.Indices.Count));
                    target = indexerExpr;
                }
                else if (_outerMethods != null && _outerMethods.TryGetValue(name, out memberMethod))
                {
                    methodRefExpr.TargetObject = CreateOuterMemberTarget(memberMethod);
                    methodRefExpr.MethodName = memberMethod.Name;
                    CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(memberMethod));
                    CodeDomUtils.SetMemberAttributes(memberMethod, MemberAttributes.Public);
                }
                else if (_outerProperties != null && _outerProperties.TryGetValue(name, out memberProperty))
                {
                    CodePropertyReferenceExpression propertyRefExpr = new CodePropertyReferenceExpression(CreateOuterMemberTarget(memberProperty), memberProperty.Name);
                    CodeObjectMetaData.SetExpressionSource(propertyRefExpr, CodeObjectSource.Create(memberProperty));
                    CodeIndexerExpression indexerExpr = new CodeIndexerExpression(propertyRefExpr);
                    indexerExpr.Indices.AddRange(methodInvokeExpr.Parameters);
                    CodeObjectMetaData.SetExpressionSource(indexerExpr, CodeObjectSource.CreateIndexer(memberProperty, indexerExpr.Indices.Count));
                    target = indexerExpr;
                }
                else if (_globalFields.TryGetValue(name, out globalFieldInfo))
                {
                    CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(
                        new CodeTypeReferenceExpression(Utils.CreateTypeReference(globalFieldInfo.DeclaringType)),
                        globalFieldInfo.Name);
                    CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(globalFieldInfo));
                    CodeIndexerExpression indexerExpr = new CodeIndexerExpression(fieldRefExpr);
                    indexerExpr.Indices.AddRange(methodInvokeExpr.Parameters);
                    target = indexerExpr;
                    MethodInfo methodInfo = globalFieldInfo.FieldType.GetMethod("get_Item");

                    if (methodInfo != null)
                    {
                        CodeObjectMetaData.SetExpressionSource(indexerExpr, CodeObjectSource.Create(methodInfo.ReturnType));
                    }
                }
                else if (_globalMethods.TryGetValue(name, out globalMethodInfo))
                {
                    methodRefExpr.TargetObject = new CodeTypeReferenceExpression(Utils.CreateTypeReference(globalMethodInfo.DeclaringType));
                    methodRefExpr.MethodName = globalMethodInfo.Name;
                    CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(globalMethodInfo.ReturnType));
                    ProcessParameters(globalMethodInfo.GetParameters(), methodInvokeExpr.Parameters);
                }
                else
                {
                    switch (name.ToLower())
                    {
                        case "string":
                            target = ProcessStringMethodInvoke(methodInvokeExpr);
                            break;
                        case "array":
                            target = ProcessArrayMethodInvoke(methodInvokeExpr);
                            break;
                        case "escape":
                            RealignMethodInvoke(methodInvokeExpr, typeof (HttpUtility), "HtmlEncode");
                            break;
                        case "unescape":
                            RealignMethodInvoke(methodInvokeExpr, typeof (HttpUtility), "HtmlDecode");
                            break;
                        case "loadpicture":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Image), "FromFile");
                            break;
                        case "cbool":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Convert), "ToBoolean");
                            break;
                        case "cbyte":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Convert), "ToByte");
                            break;
                        case "ccur":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Convert), "ToDecimal");
                            break;
                        case "cdate":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Convert), "ToDateTime");
                            break;
                        case "cdbl":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Convert), "ToDouble");
                            break;
                        case "cint":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Convert), "ToInt32");
                            break;
                        case "clng":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Convert), "ToInt64");
                            break;
                        case "csng":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Convert), "ToSingle");
                            break;
                        case "cstr":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Convert), "ToString");
                            break;
                        case "isnull":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Information), "IsDBNull");
                            break;
                        case "isempty":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Information), "IsNothing");
                            break;
                        case "isobject":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Information), "IsReference");
                            break;
                        case "atn":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Math), "Atan");
                            break;
                        case "sgn":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Math), "Sign");
                            break;
                        case "sqr":
                            RealignMethodInvoke(methodInvokeExpr, typeof (Math), "Sqrt");
                            break;
                        case "rnd":
                            RealignMethodInvoke(methodInvokeExpr, typeof (VBMath), "Rnd");
                            break;
                        case "now":
                            target = CreatePropertyReference(typeof (DateTime), "Now");
                            break;
                        case "date":
                            target = CreatePropertyReference(typeof (DateTime), "Today");
                            break;
                        case "time":
                            target = CreatePropertyReference(typeof (DateTime), "TimeOfDay");
                            break;
                        default:
                            LogWarning("Identifier '{0}' not found in class '{1}'", name, _typeDecl.Name);
                            CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, null);
                            return false;
                    }
                }
            }
            else
            {
                CodeObjectSource targetSource = CodeObjectMetaData.GetExpressionSource(methodRefExpr.TargetObject);

                if (string.IsNullOrEmpty(name))
                {
                    if (FindRealType(targetSource).ArrayRanks.Length > 0)
                    {
                        CodeIndexerExpression indexerExpr = new CodeIndexerExpression(methodRefExpr.TargetObject);
                        indexerExpr.Indices.AddRange(methodInvokeExpr.Parameters);
                        CodeObjectMetaData.SetExpressionSource(indexerExpr, CodeObjectSource.CreateIndexer(targetSource, indexerExpr.Indices.Count));
                        target = indexerExpr;
                        return true;
                    }
                    else
                    {
                        //TODO: handle default functions
                    }
                }
                else if (targetSource == null)
                {
                    CodeTypeReferenceExpression typeRefExpr = methodRefExpr.TargetObject as CodeTypeReferenceExpression;

                    if (typeRefExpr != null)
                    {
                        CodeObjectSource source = Utils.GetTypeReferenceSource(typeRefExpr.Type);

                        if (source != null &&
                            source.Target == typeof (string) &&
                            source.ArrayRanks.Length == 0 &&
                            name == "Concat")
                        {
                            CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, source);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (targetSource.Target is NullPlaceholder)
                {
                    Debug.Assert(false);
                }
                else if (targetSource.Target is FormPlaceholder)
                {
                    if (targetSource.ArrayRanks.Length > 0)
                    {
                        Debug.Assert(false);
                    }

                    MemberInfo[] members;

                    if ((members = typeof (IAxForm).GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)) != null && members.Length == 1)
                    {
                        MemberInfo memberInfo = members[0];

                        if (memberInfo is MethodInfo)
                        {
                            MethodInfo methodInfo = (MethodInfo) memberInfo;
                            CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(
                                CreateFormMemberTarget(),
                                methodInfo.Name);
                            CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(methodInfo.ReturnType));
                            methodRefExpr.MethodName = memberInfo.Name;
                            methodRefExpr.TargetObject = fieldRefExpr;
                            ProcessParameters(methodInfo.GetParameters(), methodInvokeExpr.Parameters);
                            CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(methodInfo.ReturnType));
                            (_requiredFormFields ?? (_requiredFormFields = new HashedSet<FormField>())).Add(new FormField(methodInfo.Name, methodInfo.ReturnType));
                        }
                        else
                        {
                            PropertyInfo propertyInfo = (PropertyInfo) members[0];
                            CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(
                                CreateFormMemberTarget(),
                                propertyInfo.Name);
                            CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(propertyInfo.PropertyType));
                            CodeIndexerExpression indexerExpr = new CodeIndexerExpression(fieldRefExpr);
                            indexerExpr.Indices.AddRange(methodInvokeExpr.Parameters);
                            MethodInfo methodInfo = propertyInfo.PropertyType.GetMethod("get_Item");
                            Type itemType = (methodInfo != null
                                                 ? methodInfo.ReturnType
                                                 : typeof (object));
                            CodeObjectMetaData.SetExpressionSource(indexerExpr, CodeObjectSource.Create(itemType));
                            (_requiredFormFields ?? (_requiredFormFields = new HashedSet<FormField>())).Add(new FormField(propertyInfo.Name, propertyInfo.PropertyType));
                            target = indexerExpr;
                        }
                    }
                    else
                    {
                        LogWarning("Form member '{0}' not found in class '{1}'", name, _typeDecl.Name);
                    }
                }
                else if (targetSource.Target is Type)
                {
                    if (targetSource.ArrayRanks.Length > 0)
                    {
                        Debug.Assert(false);
                    }

                    MemberInfo[] members = ((Type) targetSource.Target).GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                    if (members.Length == 1)
                    {
                        MemberInfo memberInfo = members[0];

                        if (memberInfo is MethodInfo)
                        {
                            MethodInfo methodInfo = (MethodInfo) memberInfo;
                            methodRefExpr.MethodName = memberInfo.Name;
                            ProcessParameters(methodInfo.GetParameters(), methodInvokeExpr.Parameters);

                            if (methodInfo == typeof (ISlxApplication).GetMethod("GetNewConnection"))
                            {
                                CodeObjectSource source = CodeObjectSource.Create(typeof (_Connection));
                                CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, source);
                                CodeCastExpression castExpr = new CodeCastExpression(
                                    Utils.CreateTypeReference(typeof (_Connection)),
                                    methodInvokeExpr);
                                CodeObjectMetaData.SetExpressionSource(castExpr, source);
                                target = castExpr;
                            }
                            else
                            {
                                CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(GetActualType(methodInfo.ReturnType)));
                            }
                        }
                        else if (memberInfo is PropertyInfo)
                        {
                            if (methodInvokeExpr.Parameters.Count > 0)
                            {
                                PropertyInfo propertyInfo = (PropertyInfo) memberInfo;
                                CodeIndexerExpression indexerExpr;
                                CodeObjectSource source;

                                if (StringUtils.CaseInsensitiveEquals(name, "item"))
                                {
                                    indexerExpr = new CodeIndexerExpression(methodRefExpr.TargetObject);
                                    source = CodeObjectSource.Create(GetActualType(propertyInfo.PropertyType));
                                }
                                else
                                {
                                    indexerExpr = new CodeIndexerExpression(
                                        new CodePropertyReferenceExpression(
                                            methodRefExpr.TargetObject,
                                            memberInfo.Name));
                                    MethodInfo methodInfo = GetActualType(propertyInfo.PropertyType).GetMethod("get_Item");

                                    if (methodInfo != null)
                                    {
                                        source = CodeObjectSource.Create(GetActualType(methodInfo.ReturnType));
                                    }
                                    else
                                    {
                                        LogWarning("Unable to resolve indexer property '{0}' in class '{1}'", name, _typeDecl.Name);
                                        source = null;
                                    }
                                }

                                indexerExpr.Indices.AddRange(methodInvokeExpr.Parameters);
                                CodeObjectMetaData.SetExpressionSource(indexerExpr, source);
                                ProcessParameters(propertyInfo.GetIndexParameters(), indexerExpr.Indices);
                                target = indexerExpr;
                            }
                            else
                            {
                                CodePropertyReferenceExpression propertyRefExpr = new CodePropertyReferenceExpression(
                                    methodRefExpr.TargetObject,
                                    memberInfo.Name);
                                CodeObjectMetaData.SetExpressionSource(propertyRefExpr, CodeObjectSource.Create(GetActualType(((PropertyInfo) memberInfo).PropertyType)));
                                target = propertyRefExpr;
                            }
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    else
                    {
                        LogWarning("Member '{0}' not found in class '{1}'", name, _typeDecl.Name);
                        CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, null);
                        return false;
                    }
                }
                else if (targetSource.Target is CodeTypeDeclaration)
                {
                    Debug.Assert(false);
                }
                else if (targetSource.Target is CodeObject)
                {
                    CodeObjectSource realType = FindRealType(CodeObjectMetaData.GetEntitySource((CodeObject) targetSource.Target));

                    if (targetSource.ArrayRanks.Length > 0)
                    {
                        if (targetSource.Type == CodeObjectSourceType.Indexer)
                        {
                            if (realType.ArrayRanks.Length >= targetSource.ArrayRanks.Length && realType.Type == CodeObjectSourceType.Array)
                            {
                                realType = CodeObjectSource.CreateMerge(realType, targetSource.ArrayRanks, realType.Type);
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }

                    if (realType != null)
                    {
                        if (realType.Target is Type)
                        {
                            MemberInfo[] members = ((Type) realType.Target).GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                            if (members.Length == 1)
                            {
                                MemberInfo memberInfo = members[0];

                                if (memberInfo is MethodInfo)
                                {
                                    MethodInfo methodInfo = (MethodInfo) memberInfo;
                                    methodRefExpr.MethodName = memberInfo.Name;
                                    ProcessParameters(methodInfo.GetParameters(), methodInvokeExpr.Parameters);

                                    if (memberInfo == typeof (IMainView).GetMethod("TabsViews"))
                                    {
                                        CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(typeof (IAxForm)));
                                    }
                                    else
                                    {
                                        CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(GetActualType(methodInfo.ReturnType)));
                                    }
                                }
                                else if (memberInfo is PropertyInfo)
                                {
                                    PropertyInfo propertyInfo = (PropertyInfo) memberInfo;
                                    CodePropertyReferenceExpression propertyRefExpr = new CodePropertyReferenceExpression(
                                        methodRefExpr.TargetObject,
                                        memberInfo.Name);
                                    CodeObjectSource source = CodeObjectSource.Create(GetActualType(propertyInfo.PropertyType));
                                    CodeObjectMetaData.SetExpressionSource(propertyRefExpr, source);

                                    if (methodInvokeExpr.Parameters.Count > 0)
                                    {
                                        CodeIndexerExpression indexerExpr = new CodeIndexerExpression(propertyRefExpr);
                                        indexerExpr.Indices.AddRange(methodInvokeExpr.Parameters);
                                        CodeObjectMetaData.SetExpressionSource(indexerExpr, source);
                                        ProcessParameters(propertyInfo.GetIndexParameters(), indexerExpr.Indices);
                                        target = indexerExpr;
                                    }
                                    else
                                    {
                                        target = propertyRefExpr;
                                    }
                                }
                                else
                                {
                                    Debug.Assert(false);
                                }
                            }
                            else
                            {
                                LogWarning("Member '{0}' not found in class '{1}'", name, _typeDecl.Name);
                                CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, null);
                                return false;
                            }
                        }
                        else if (realType.Target is CodeTypeDeclaration)
                        {
                            CodeTypeDeclaration typeDecl = (CodeTypeDeclaration) realType.Target;
                            CodeMemberField memberField;
                            CodeMemberMethod memberMethod;
                            CodeMemberProperty memberProperty;

                            if (CodeObjectMetaData.GetFields(typeDecl).TryGetValue(name, out memberField))
                            {
                                Debug.Assert(false);
                            }
                            else if (CodeObjectMetaData.GetMethods(typeDecl).TryGetValue(name, out memberMethod))
                            {
                                methodRefExpr.MethodName = memberMethod.Name;
                                CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(memberMethod));
                                ProcessParameters(memberMethod.Parameters, methodInvokeExpr.Parameters, memberMethod.Name);
                            }
                            else if (CodeObjectMetaData.GetProperties(typeDecl).TryGetValue(name, out memberProperty))
                            {
                                CodePropertyReferenceExpression propertyRefExpr = new CodePropertyReferenceExpression(
                                    methodRefExpr.TargetObject,
                                    memberProperty.Name);
                                CodeObjectSource source = CodeObjectSource.Create(memberProperty);
                                CodeObjectMetaData.SetExpressionSource(propertyRefExpr, source);

                                if (methodInvokeExpr.Parameters.Count > 0)
                                {
                                    CodeIndexerExpression indexerExpr = new CodeIndexerExpression(propertyRefExpr);
                                    indexerExpr.Indices.AddRange(methodInvokeExpr.Parameters);
                                    CodeObjectMetaData.SetExpressionSource(indexerExpr, source);
                                    ProcessParameters(memberProperty.Parameters, indexerExpr.Indices, memberProperty.Name);
                                    target = indexerExpr;
                                }
                                else
                                {
                                    target = propertyRefExpr;
                                }
                            }
                            else
                            {
                                Debug.Assert(false);
                            }
                        }
                        else
                        {
                            Debug.Assert(false);
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    Debug.Assert(false);
                }
            }

            return true;
        }

        private bool ProcessPropertyReference(ref CodeObject target)
        {
            CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) target;
            string name = propertyRefExpr.PropertyName;

            if (propertyRefExpr.TargetObject == null)
            {
                Debug.Assert(false);
            }
            else
            {
                CodeObjectSource targetSource = CodeObjectMetaData.GetExpressionSource(propertyRefExpr.TargetObject);

                if (targetSource == null)
                {
                    return false;
                }
                else if (targetSource.ArrayRanks.Length > 0)
                {
                    Debug.Assert(false);
                }
                else if (targetSource.Target is NullPlaceholder)
                {
                    Debug.Assert(false);
                }
                else if (targetSource.Target is FormPlaceholder)
                {
                    FormField formField;
                    MemberInfo[] members;

                    if ((_formFields != null && _formFields.TryGetValue(name, out formField)))
                    {
                        CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(
                            CreateFormMemberTarget(),
                            formField.Name);
                        CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(formField.Type));
                        (_requiredFormFields ?? (_requiredFormFields = new HashedSet<FormField>())).Add(formField);
                        target = fieldRefExpr;
                    }
                    else if ((members = typeof (IAxForm).GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)) != null && members.Length == 1)
                    {
                        PropertyInfo property = (PropertyInfo) members[0];
                        CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(
                            CreateFormMemberTarget(),
                            property.Name);
                        CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(property.PropertyType));
                        (_requiredFormFields ?? (_requiredFormFields = new HashedSet<FormField>())).Add(new FormField(property.Name, property.PropertyType));
                        target = fieldRefExpr;
                    }
                    else
                    {
                        LogWarning("Form control, property or method '{0}' not found in class '{1}'", name, _typeDecl.Name);
                    }
                }
                else if (targetSource.Target is Type)
                {
                    Type type = (Type) targetSource.Target;
                    MemberInfo[] members = type.GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                    if (members.Length > 0)
                    {
                        MemberInfo memberInfo = null;

                        if (members.Length > 1)
                        {
                            foreach (MemberInfo item in members)
                            {
                                if (item is MethodInfo)
                                {
                                    if (((MethodInfo) item).GetParameters().Length == 0)
                                    {
                                        memberInfo = item;
                                        break;
                                    }
                                }
                                else if (item is PropertyInfo)
                                {
                                    memberInfo = item;
                                    break;
                                }
                                else
                                {
                                    Debug.Assert(false);
                                }
                            }
                        }
                        else
                        {
                            memberInfo = members[0];
                        }

                        if (memberInfo == null)
                        {
                            Debug.Assert(false);
                        }

                        if (memberInfo is MethodInfo)
                        {
                            MethodInfo methodInfo = (MethodInfo) memberInfo;
                            CodeMethodInvokeExpression methodInvokeExpr = new CodeMethodInvokeExpression(
                                propertyRefExpr.TargetObject,
                                memberInfo.Name);
                            ProcessParameters(methodInfo.GetParameters(), methodInvokeExpr.Parameters);

                            if (memberInfo == typeof (ISlxApplication).GetMethod("GetNewConnection"))
                            {
                                CodeObjectSource source = CodeObjectSource.Create(typeof (_Connection));
                                CodeObjectMetaData.SetExpressionSource(propertyRefExpr, source);
                                CodeCastExpression castExpr = new CodeCastExpression(
                                    Utils.CreateTypeReference(typeof (_Connection)),
                                    methodInvokeExpr);
                                target = castExpr;
                                CodeObjectMetaData.SetExpressionSource(castExpr, source);
                            }
                            else
                            {
                                target = methodInvokeExpr;
                                CodeObjectMetaData.SetExpressionSource(propertyRefExpr, CodeObjectSource.Create(GetActualType(methodInfo.ReturnType)));
                            }
                        }
                        else if (memberInfo is PropertyInfo)
                        {
                            propertyRefExpr.PropertyName = memberInfo.Name;
                            CodeObjectMetaData.SetExpressionSource(propertyRefExpr, CodeObjectSource.Create(GetActualType(((PropertyInfo) memberInfo).PropertyType)));
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    else
                    {
                        MethodInfo methodInfo = type.GetMethod("get_Item");

                        if (methodInfo != null)
                        {
                            ParameterInfo[] parms = methodInfo.GetParameters();

                            if (parms.Length == 1)
                            {
                                ParameterInfo parm = parms[0];

                                if (parm.ParameterType.IsAssignableFrom(typeof (string)) || parm.ParameterType.IsAssignableFrom(typeof (int)))
                                {
                                    CodeIndexerExpression indexerExpr = new CodeIndexerExpression(
                                        propertyRefExpr.TargetObject,
                                        new CodePrimitiveExpression(name));
                                    CodeObjectMetaData.SetExpressionSource(indexerExpr, CodeObjectSource.Create(methodInfo.ReturnType));
                                    target = indexerExpr;
                                }
                                else
                                {
                                    Debug.Assert(false);
                                }
                            }
                            else
                            {
                                Debug.Assert(false);
                            }
                        }
                        else
                        {
                            LogWarning("Unable to resolve property '{0}' in class '{1}'", name, _typeDecl.Name);
                            return false;
                        }
                    }
                }
                else if (targetSource.Target is CodeTypeDeclaration)
                {
                    Debug.Assert(false);
                }
                else if (targetSource.Target is CodeObject)
                {
                    CodeObjectSource realType = FindRealType(CodeObjectMetaData.GetEntitySource((CodeObject) targetSource.Target));

                    if (realType != null)
                    {
                        if (realType.Target is Type)
                        {
                            MemberInfo[] members = ((Type) realType.Target).GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                            if (members.Length == 1)
                            {
                                MemberInfo memberInfo = members[0];

                                if (memberInfo is MethodInfo)
                                {
                                    MethodInfo methodInfo = (MethodInfo) memberInfo;
                                    CodeMethodInvokeExpression methodInvokeExpr = new CodeMethodInvokeExpression(
                                        propertyRefExpr.TargetObject,
                                        memberInfo.Name);
                                    CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(GetActualType(methodInfo.ReturnType)));
                                    ProcessParameters(methodInfo.GetParameters(), methodInvokeExpr.Parameters);
                                    target = methodInvokeExpr;
                                }
                                else if (memberInfo is PropertyInfo)
                                {
                                    propertyRefExpr.PropertyName = memberInfo.Name;

                                    if (memberInfo == typeof (IDataGrid).GetProperty("Recordset"))
                                    {
                                        CodeObjectMetaData.SetExpressionSource(propertyRefExpr, CodeObjectSource.Create(typeof (object)));
                                        CodeCastExpression castExpr = new CodeCastExpression(
                                            Utils.CreateTypeReference(typeof (_Recordset)),
                                            propertyRefExpr);
                                        CodeObjectMetaData.SetExpressionSource(castExpr, CodeObjectSource.Create(typeof (_Recordset)));
                                        target = castExpr;
                                    }
                                    else if (memberInfo == typeof (IMainView).GetProperty("MiddleView"))
                                    {
                                        CodeObjectMetaData.SetExpressionSource(propertyRefExpr, CodeObjectSource.Create(typeof (IAxForm)));
                                    }
                                    else
                                    {
                                        CodeObjectMetaData.SetExpressionSource(propertyRefExpr, CodeObjectSource.Create(GetActualType(((PropertyInfo) memberInfo).PropertyType)));
                                    }
                                }
                                else
                                {
                                    Debug.Assert(false);
                                }
                            }
                            else
                            {
                                LogWarning("Member '{0}' not found in class '{1}'", name, _typeDecl.Name);
                                CodeObjectMetaData.SetExpressionSource(propertyRefExpr, null);
                                return false;
                            }
                        }
                        else if (realType.Target is CodeTypeDeclaration)
                        {
                            CodeTypeDeclaration typeDecl = (CodeTypeDeclaration) realType.Target;
                            CodeMemberField memberField;
                            CodeMemberMethod memberMethod;
                            CodeMemberProperty memberProperty;

                            if (CodeObjectMetaData.GetFields(typeDecl).TryGetValue(name, out memberField))
                            {
                                CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(
                                    propertyRefExpr.TargetObject,
                                    name);
                                CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(memberField));
                                target = fieldRefExpr;
                            }
                            else if (CodeObjectMetaData.GetMethods(typeDecl).TryGetValue(name, out memberMethod))
                            {
                                CodeMethodInvokeExpression methodInvokeExpr = new CodeMethodInvokeExpression(
                                    propertyRefExpr.TargetObject,
                                    memberMethod.Name);
                                CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(memberMethod));
                                ProcessParameters(memberMethod.Parameters, methodInvokeExpr.Parameters, memberMethod.Name);
                                target = methodInvokeExpr;
                            }
                            else if (CodeObjectMetaData.GetProperties(typeDecl).TryGetValue(name, out memberProperty))
                            {
                                propertyRefExpr.PropertyName = memberProperty.Name;
                                CodeObjectMetaData.SetExpressionSource(propertyRefExpr, CodeObjectSource.Create(memberProperty));
                            }
                            else
                            {
                                LogWarning("Member '{0}' not found in class '{1}'", name, _typeDecl.Name);
                            }
                        }
                        else
                        {
                            Debug.Assert(false);
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    Debug.Assert(false);
                }
            }

            return true;
        }

        private bool ProcessObjectCreate(CodeObjectCreateExpression objectCreateExpr)
        {
            CodeObjectSource source = Utils.GetTypeReferenceSource(objectCreateExpr.CreateType);

            if (source == null)
            {
                CodeTypeDeclaration typeDecl;
                string name = objectCreateExpr.CreateType.BaseType;

                if (_localNestedTypes.TryGetValue(name, out typeDecl) ||
                    (_outerNestedTypes != null && _outerNestedTypes.TryGetValue(name, out typeDecl)))
                {
                    objectCreateExpr.CreateType = Utils.CreateTypeReference(typeDecl);
                    source = CodeObjectSource.Create(typeDecl);
                }
                else
                {
                    LogWarning("CodeObjectSource '{0}' not found", name);
                }
            }

            CodeObjectMetaData.SetExpressionSource(objectCreateExpr, source);
            return true;
        }

        private bool ProcessPrimitive(CodePrimitiveExpression primitiveExpr)
        {
            object value = primitiveExpr.Value;
            CodeObjectMetaData.SetExpressionSource(primitiveExpr, CodeObjectSource.Create(
                                                                      value == null
                                                                          ? (object) NullPlaceholder.Value
                                                                          : value.GetType()));
            return true;
        }

        private bool ProcessBinaryOperator(CodeBinaryOperatorExpression binaryExpr)
        {
            switch (binaryExpr.Operator)
            {
                case CodeBinaryOperatorType.Add:
                case CodeBinaryOperatorType.BitwiseAnd:
                case CodeBinaryOperatorType.BitwiseOr:
                case CodeBinaryOperatorType.Divide:
                case CodeBinaryOperatorType.Modulus:
                case CodeBinaryOperatorType.Multiply:
                case CodeBinaryOperatorType.Subtract:
                    {
                        CodeObjectSource source = FindCommonSource(
                            CodeObjectMetaData.GetExpressionSource(binaryExpr.Left),
                            CodeObjectMetaData.GetExpressionSource(binaryExpr.Right));
                        bool result = (source != null);

                        if (result)
                        {
                            CodeObjectMetaData.SetExpressionSource(binaryExpr, source);
                        }

                        return result;
                    }
                case CodeBinaryOperatorType.IdentityEquality:
                case CodeBinaryOperatorType.IdentityInequality:
                case CodeBinaryOperatorType.BooleanAnd:
                case CodeBinaryOperatorType.BooleanOr:
                case CodeBinaryOperatorType.GreaterThan:
                case CodeBinaryOperatorType.GreaterThanOrEqual:
                case CodeBinaryOperatorType.LessThan:
                case CodeBinaryOperatorType.LessThanOrEqual:
                case CodeBinaryOperatorType.ValueEquality:
                    CodeObjectMetaData.SetExpressionSource(binaryExpr, CodeObjectSource.Create(typeof (bool)));
                    return true;
                case CodeBinaryOperatorType.Assign:
                default:
                    Debug.Assert(false);
                    return false;
            }
        }

        private bool ProcessArrayCreate(CodeArrayCreateExpression arrayCreateExpr)
        {
            if (arrayCreateExpr.Initializers.Count == 0)
            {
                CodeObjectMetaData.SetExpressionSource(arrayCreateExpr, CodeObjectSource.CreateArray(typeof (object), 1));
            }
            else
            {
                CodeObjectSource commonSource = null;

                foreach (CodeExpression initExpr in arrayCreateExpr.Initializers)
                {
                    CodeObjectSource currentSource = CodeObjectMetaData.GetExpressionSource(initExpr);
                    commonSource = (commonSource == null
                                        ? currentSource
                                        : FindCommonSource(commonSource, currentSource));

                    if (commonSource == null || (commonSource.Target == typeof (object) && commonSource.ArrayRanks.Length == 0))
                    {
                        break;
                    }
                }

                if (commonSource == null)
                {
                    return false;
                }
                else
                {
                    if (commonSource.Target is CodeVariableDeclarationStatement)
                    {
                        commonSource = CodeObjectMetaData.GetEntitySource((CodeObject) commonSource.Target);
                    }

                    if (commonSource == null)
                    {
                        return false;
                    }
                    else
                    {
                        CodeObjectSource source = CodeObjectSource.CreateArray(commonSource, 1);

                        if (Utils.IsRealType(commonSource.Target))
                        {
                            arrayCreateExpr.CreateType = Utils.CreateTypeReference(source);
                        }

                        CodeObjectMetaData.SetExpressionSource(arrayCreateExpr, source);
                    }
                }
            }

            return true;
        }

        private bool ProcessFieldReference(CodeFieldReferenceExpression fieldRefExpr)
        {
            if (fieldRefExpr.FieldName == "Value")
            {
                CodeTypeReferenceExpression typeRefExpr = fieldRefExpr.TargetObject as CodeTypeReferenceExpression;

                if (typeRefExpr != null && Utils.CompareTypeReferences(typeRefExpr.Type, typeof (DBNull)))
                {
                    CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(typeof (DBNull)));
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else
            {
                Debug.Assert(false);
            }

            return true;
        }

        private bool ProcessTypeOf(CodeTypeOfExpression typeOfExpr)
        {
            CodeObjectMetaData.SetExpressionSource(typeOfExpr, CodeObjectSource.Create(typeof (Type)));
            return true;
        }

        private bool ProcessCast(CodeCastExpression castExpr)
        {
            CodeObjectSource source = Utils.GetTypeReferenceSource(castExpr.TargetType);
            CodeObjectMetaData.SetExpressionSource(castExpr, source);
            return true;
        }

        //---------------------------------------

        private bool ProcessAssign(CodeAssignStatement assignStmt)
        {
            CodeObjectSource leftSource = CodeObjectMetaData.GetExpressionSource(assignStmt.Left);
            CodeObjectSource rightSource = CodeObjectMetaData.GetExpressionSource(assignStmt.Right);

            if (leftSource != null && rightSource != null)
            {
                if (Utils.IsEntity(leftSource.Target))
                {
                    return ImplyEntityType(leftSource, rightSource);
                }
                else if (Utils.IsEntity(rightSource.Target))
                {
                    //return ImplyEntityType(rightSource, leftSource);
                    //TODO: figure out this secondary implication
                }
            }

            return false;
        }

        private bool ProcessForEach(CodeForEachStatement forEachStmt)
        {
            CodeObjectSource source = CodeObjectMetaData.GetExpressionSource(forEachStmt.EnumerableTarget);

            if (source != null && source.ArrayRanks.Length > 0)
            {
                Type type = source.Target as Type;

                if (type != null)
                {
                    source = CodeObjectSource.CreateIndexer(source, 1);
                    bool changed = source != Utils.GetTypeReferenceSource(forEachStmt.ElementType);

                    if (changed)
                    {
                        forEachStmt.ElementType = Utils.CreateTypeReference(source);
                    }

                    return changed;
                }
            }

            return false;
        }

        private bool ProcessCondition(CodeConditionStatement conditionStmt)
        {
            //TODO: remove
            if (conditionStmt.Condition == null)
            {
                return false;
            }

            CodeObjectSource source = CodeObjectMetaData.GetExpressionSource(conditionStmt.Condition);
            return (source != null &&
                    Utils.IsEntity(source.Target) &&
                    ImplyEntityType(source, CodeObjectSource.Create(typeof (bool))));
        }

        private bool ProcessIteration(CodeIterationStatement iterationStmt)
        {
            CodeObjectSource source = CodeObjectMetaData.GetExpressionSource(iterationStmt.TestExpression);
            return (source != null &&
                    Utils.IsEntity(source.Target) &&
                    ImplyEntityType(source, CodeObjectSource.Create(typeof (bool))));
        }

        private bool ProcessVariableDeclaration(CodeVariableDeclarationStatement variableDeclStmt)
        {
            if (variableDeclStmt.InitExpression != null)
            {
                if (variableDeclStmt.InitExpression is CodePrimitiveExpression)
                {
                    CodePrimitiveExpression primitiveExpr = (CodePrimitiveExpression) variableDeclStmt.InitExpression;
                    CodeObjectSource source = CodeObjectSource.Create(primitiveExpr.Value == null
                                                                          ? (object) NullPlaceholder.Value
                                                                          : primitiveExpr.Value.GetType());
                    return ImplyEntityType(CodeObjectSource.Create(variableDeclStmt), source);
                }
                else if (variableDeclStmt.InitExpression is CodeObjectCreateExpression)
                {
                    CodeObjectCreateExpression objectCreateExpr = (CodeObjectCreateExpression) variableDeclStmt.InitExpression;
                    return ImplyEntityType(
                        CodeObjectSource.Create(variableDeclStmt),
                        Utils.GetTypeReferenceSource(objectCreateExpr.CreateType));
                }
                else if (!(variableDeclStmt.InitExpression is CodeCastExpression || variableDeclStmt.InitExpression is CodeArrayCreateExpression))
                {
                    Debug.Assert(false);
                }
            }

            return false;
        }

        //---------------------------------------

        private CodeObject ProcessStringMethodInvoke(CodeMethodInvokeExpression methodInvokeExpr)
        {
            CodeObjectCreateExpression objectCreateExpr = new CodeObjectCreateExpression(
                Utils.CreateTypeReference(typeof (string)));
            objectCreateExpr.Parameters.Add(methodInvokeExpr.Parameters[1]);
            objectCreateExpr.Parameters.Add(methodInvokeExpr.Parameters[0]);
            CodeObjectMetaData.SetExpressionSource(objectCreateExpr, CodeObjectSource.Create(typeof (string)));
            return objectCreateExpr;
        }

        private CodeObject ProcessArrayMethodInvoke(CodeMethodInvokeExpression methodInvokeExpr)
        {
            CodeArrayCreateExpression arrayCreateExpr = new CodeArrayCreateExpression(
                Utils.CreateTypeReference(typeof (object)));
            arrayCreateExpr.Initializers.AddRange(methodInvokeExpr.Parameters);
            ProcessArrayCreate(arrayCreateExpr);
            return arrayCreateExpr;
        }

        //---------------------------------------

        private void ProcessParameters(ParameterInfo[] parms, CodeExpressionCollection args)
        {
            int invokeCount = args.Count;
            int defCount = parms.Length;

            for (int i = 0; i < defCount; i++)
            {
                ParameterInfo parm = parms[i];

                if (i < invokeCount)
                {
                    if (parm.ParameterType.IsEnum)
                    {
                        CodeExpression expr = args[i];
                        CodePrimitiveExpression primitiveExpr = expr as CodePrimitiveExpression;
                        expr = CreateEnumReference(parm.ParameterType, primitiveExpr == null ? null : primitiveExpr.Value, expr);
                        args[i] = expr;
                    }
                    else if (parm.ParameterType == typeof (string))
                    {
                        CodeExpression expr = args[i];
                        CodeObjectSource source = CodeObjectMetaData.GetExpressionSource(expr);

                        if (source != null && source.Target == typeof (int))
                        {
                            CodePrimitiveExpression primitiveExpr = expr as CodePrimitiveExpression;

                            if (primitiveExpr != null)
                            {
                                primitiveExpr.Value = primitiveExpr.Value.ToString();
                            }
                            else
                            {
                                expr = new CodeMethodInvokeExpression(expr, "ToString");
                                args[i] = expr;
                            }

                            CodeObjectMetaData.SetExpressionSource(expr, CodeObjectSource.Create(parm.ParameterType));
                        }
                    }
                }
                else
                {
                    OptionalAttribute optional = null;
                    object[] attributes = parm.GetCustomAttributes(typeof (OptionalAttribute), true);

                    if (attributes.Length > 0)
                    {
                        optional = (OptionalAttribute) attributes[0];
                    }

                    Type parameterType = parm.ParameterType;
                    Type elementType;

                    while ((elementType = parameterType.GetElementType()) != null)
                    {
                        parameterType = elementType;
                    }

                    CodeExpression expr;
                    Type type;

                    if (optional != null && parameterType == typeof (object))
                    {
                        CodeThisReferenceExpression thisExpr = new CodeThisReferenceExpression();
                        CodeObjectMetaData.SetExpressionSource(thisExpr, CodeObjectSource.Create(_typeDecl));
                        expr = new CodeFieldReferenceExpression(
                            thisExpr,
                            "_missingValue");
                        type = typeof (Missing);
                        _requiresMissingValue = true;
                    }
                    else if (parm.DefaultValue != null)
                    {
                        type = parm.DefaultValue.GetType();

                        if (type == typeof (string) || type.IsPrimitive)
                        {
                            expr = new CodePrimitiveExpression(parm.DefaultValue);
                        }
                        else if (type.IsEnum)
                        {
                            type = parameterType;
                            expr = CreateEnumReference(type, parm.DefaultValue, new CodePrimitiveExpression(parm.DefaultValue));
                        }
                        else if (type == typeof (DBNull))
                        {
                            expr = new CodeFieldReferenceExpression(
                                new CodeTypeReferenceExpression(
                                    Utils.CreateTypeReference(type)),
                                "Value");
                        }
                        else
                        {
                            Debug.Assert(false);
                            continue;
                        }
                    }
                    else
                    {
                        expr = new CodePrimitiveExpression(parm.DefaultValue);
                        Debug.Assert(!parm.IsOut && !parameterType.IsByRef);
                        type = null;
                    }

                    if (type != null)
                    {
                        CodeObjectMetaData.SetExpressionSource(expr, CodeObjectSource.Create(type));

                        if (parm.IsOut)
                        {
                            expr = new CodeDirectionExpression(FieldDirection.Out, expr);
                            CodeObjectMetaData.SetExpressionSource(expr, CodeObjectSource.Create(type));
                        }
                        else if (parameterType.IsByRef)
                        {
                            expr = new CodeDirectionExpression(FieldDirection.Ref, expr);
                            CodeObjectMetaData.SetExpressionSource(expr, CodeObjectSource.Create(type));
                        }
                    }

                    args.Add(expr);
                }
            }
        }

        private bool ProcessParameters(CodeParameterDeclarationExpressionCollection parms, CodeExpressionCollection args, string name)
        {
            int parmCount = parms.Count;
            int argCount = args.Count;
            bool changed = false;

            if (parmCount != argCount)
            {
                LogWarning("Incorrect number of parameters passed to method or property indexer '{0}' in class '{1}'", name, _typeDecl.Name);
            }

            for (int i = 0; i < parmCount && i < argCount; i++)
            {
                changed |= ImplyEntityType(CodeObjectSource.Create(parms[i]), CodeObjectMetaData.GetExpressionSource(args[i]));
            }

            return changed;
        }

        //---------------------------------------

        private CodeExpression CreateLocalMemberTarget(CodeTypeMember typeMember)
        {
            CodeExpression targetExpr;

            if (CodeDomUtils.AreMemberAttributesSet(typeMember, MemberAttributes.Static) || CodeDomUtils.AreMemberAttributesSet(typeMember, MemberAttributes.Const))
            {
                targetExpr = new CodeTypeReferenceExpression(
                    Utils.CreateTypeReference(_typeDecl));
            }
            else
            {
                targetExpr = new CodeThisReferenceExpression();
            }

            CodeObjectMetaData.SetExpressionSource(targetExpr, CodeObjectSource.Create(_typeDecl));
            return targetExpr;
        }

        private CodeExpression CreateOuterMemberTarget(CodeTypeMember typeMember)
        {
            CodeTypeDeclaration typeDecl = CodeObjectMetaData.GetParent(typeMember);
            CodeExpression targetExpr;

            if (CodeDomUtils.AreMemberAttributesSet(typeMember, MemberAttributes.Static) || CodeDomUtils.AreMemberAttributesSet(typeMember, MemberAttributes.Const))
            {
                targetExpr = new CodeTypeReferenceExpression(
                    Utils.CreateTypeReference(typeDecl));
            }
            else
            {
                CodeThisReferenceExpression thisExpr = new CodeThisReferenceExpression();
                CodeObjectMetaData.SetExpressionSource(thisExpr, CodeObjectSource.Create(_typeDecl));
                targetExpr = new CodeFieldReferenceExpression(
                    thisExpr,
                    GenerateFieldName(typeDecl.Name));
                (_requiredOuterReferences ?? (_requiredOuterReferences = new HashedSet<CodeTypeDeclaration>())).Add(typeDecl);
            }

            CodeObjectMetaData.SetExpressionSource(targetExpr, CodeObjectSource.Create(typeDecl));
            return targetExpr;
        }

        private CodeExpression CreateFormMemberTarget()
        {
            CodeTypeDeclaration parentTypeDecl = CodeObjectMetaData.GetParent(_typeDecl);
            CodeExpression targetExpr = new CodeThisReferenceExpression();
            CodeObjectMetaData.SetExpressionSource(targetExpr, CodeObjectSource.Create(_typeDecl));

            if (parentTypeDecl != null)
            {
                targetExpr = new CodeFieldReferenceExpression(
                    targetExpr,
                    GenerateFieldName(parentTypeDecl.Name));
                CodeObjectMetaData.SetExpressionSource(targetExpr, CodeObjectSource.Create(parentTypeDecl));
                (_requiredOuterReferences ?? (_requiredOuterReferences = new HashedSet<CodeTypeDeclaration>())).Add(parentTypeDecl);
            }
            return targetExpr;
        }

        //---------------------------------------

        private static Type GetActualType(Type type)
        {
            if (type.IsInterface && type.IsImport && type.GetMembers().Length == 0)
            {
                Type[] interfaces = type.GetInterfaces();

                if (interfaces.Length > 0)
                {
                    return type.GetInterfaces()[0];
                }
            }

            return type;
        }

        private static CodePropertyReferenceExpression CreatePropertyReference(Type type, string propertyName)
        {
            CodePropertyReferenceExpression propertyRefExpr = new CodePropertyReferenceExpression(
                new CodeTypeReferenceExpression(Utils.CreateTypeReference(type)),
                propertyName);
            CodeObjectMetaData.SetExpressionSource(propertyRefExpr, CodeObjectSource.Create(type.GetProperty(propertyName).PropertyType));
            return propertyRefExpr;
        }

        private static CodeFieldReferenceExpression CreateFieldReference(Type type, string fieldName)
        {
            CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression(Utils.CreateTypeReference(type)),
                fieldName);
            CodeObjectMetaData.SetExpressionSource(fieldRefExpr, CodeObjectSource.Create(type.GetField(fieldName).FieldType));
            return fieldRefExpr;
        }

        private static CodeMethodInvokeExpression CreateColorReference(string colorName)
        {
            CodeMethodInvokeExpression methodInvokeExpr = new CodeMethodInvokeExpression(
                CreateFieldReference(typeof (Color), colorName),
                "ToArgb");
            CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(typeof (Color)));
            return methodInvokeExpr;
        }

        private static void RealignMethodInvoke(CodeMethodInvokeExpression methodInvokeExpr, Type type, string methodName)
        {
            methodInvokeExpr.Method.TargetObject = new CodeTypeReferenceExpression(Utils.CreateTypeReference(type));
            methodInvokeExpr.Method.MethodName = methodName;
            Type returnType = null;

            foreach (MethodInfo method in type.GetMember(methodName))
            {
                if (returnType == null)
                {
                    returnType = method.ReturnType;
                }
                else if (returnType != method.ReturnType)
                {
                    Debug.Assert(false);
                }
            }

            CodeObjectMetaData.SetExpressionSource(methodInvokeExpr, CodeObjectSource.Create(returnType));
        }

        private static string GenerateFieldName(string typeName)
        {
            int len = typeName.Length;
            int count = 0;

            while (count < len && char.IsUpper(typeName[count]))
            {
                count++;
            }

            if (count >= len)
            {
                typeName = typeName.ToLower();
            }
            else if (count > 2)
            {
                count--;
                typeName = typeName.Substring(0, count).ToLower() + typeName.Substring(count);
            }
            else if (count > 0)
            {
                typeName = char.ToLower(typeName[0]) + typeName.Substring(1);
            }

            return "_" + typeName;
        }

        private static CodeExpression CreateEnumReference(Type type, object value, CodeExpression originalExpr)
        {
            CodeExpression expr = null;

            if (value != null)
            {
                Type valueType = value.GetType();
                int valueCode = (int) Convert.GetTypeCode(value);

                if (valueCode >= 5 && valueCode <= 12)
                {
                    value = Enum.ToObject(type, value);
                }
                else if (type != valueType)
                {
                    value = null;
                }

                if (value != null)
                {
                    CodeTypeReferenceExpression typeRefExpr = new CodeTypeReferenceExpression(
                        Utils.CreateTypeReference(type));

                    foreach (Enum part in (Enum[]) TypeDescriptor.GetConverter(type).ConvertTo(value, typeof (Enum[])))
                    {
                        if (Enum.IsDefined(type, part))
                        {
                            string partStr = part.ToString();
                            int pos = partStr.LastIndexOf(", ");

                            if (pos >= 0)
                            {
                                partStr = partStr.Substring(pos + 2);
                            }

                            CodeFieldReferenceExpression fieldRefExpr = new CodeFieldReferenceExpression(typeRefExpr, partStr);

                            if (expr == null)
                            {
                                expr = fieldRefExpr;
                            }
                            else
                            {
                                CodeObjectSource source = CodeObjectSource.Create(type);
                                CodeObjectMetaData.SetExpressionSource(expr, source);
                                CodeObjectMetaData.SetExpressionSource(fieldRefExpr, source);
                                expr = new CodeBinaryOperatorExpression(expr, CodeBinaryOperatorType.BitwiseOr, fieldRefExpr);
                            }
                        }
                        else
                        {
                            expr = null;
                            break;
                        }
                    }
                }
            }

            if (expr == null)
            {
                expr = new CodeCastExpression(
                    Utils.CreateTypeReference(type),
                    originalExpr);
            }

            CodeObjectMetaData.SetExpressionSource(expr, CodeObjectSource.Create(type));
            return expr;
        }

        private IDictionary<string, T> MergeMembers<T>(IDictionary<string, T> localMembers, IDictionary<string, T> outerMembers, string memberTypeName)
            where T : CodeTypeMember
        {
            IDictionary<string, T> allMembers;

            if (localMembers != null)
            {
                allMembers = new Dictionary<string, T>(localMembers, StringComparer.InvariantCultureIgnoreCase);

                if (outerMembers != null)
                {
                    foreach (KeyValuePair<string, T> localMember in localMembers)
                    {
                        if (allMembers.ContainsKey(localMember.Key))
                        {
                            LogWarning("{0} '{1}' already defined", memberTypeName, localMember.Value.Name);
                        }

                        allMembers[localMember.Key] = localMember.Value;
                    }
                }
            }
            else if (outerMembers != null)
            {
                allMembers = new Dictionary<string, T>(outerMembers, StringComparer.InvariantCultureIgnoreCase);
            }
            else
            {
                allMembers = null;
            }

            return allMembers;
        }

        private static CodeTypeReference GetEntityType(CodeObject obj)
        {
            if (obj is CodeVariableDeclarationStatement)
            {
                return ((CodeVariableDeclarationStatement) obj).Type;
            }
            else if (obj is CodeParameterDeclarationExpression)
            {
                return ((CodeParameterDeclarationExpression) obj).Type;
            }
            else if (obj is CodeMemberField)
            {
                return ((CodeMemberField) obj).Type;
            }
            else if (obj is CodeMemberMethod)
            {
                return ((CodeMemberMethod) obj).ReturnType;
            }
            else if (obj is CodeMemberProperty)
            {
                return ((CodeMemberProperty) obj).Type;
            }
            else
            {
                Debug.Assert(false);
                return null;
            }
        }

        private static void SetEntityType(CodeObject obj, CodeTypeReference typeRef)
        {
            if (obj is CodeVariableDeclarationStatement)
            {
                CodeVariableDeclarationStatement variableDeclStmt = (CodeVariableDeclarationStatement) obj;
                variableDeclStmt.Type = typeRef;
                SetInitExpressionType(variableDeclStmt.InitExpression, typeRef);
            }
            else if (obj is CodeParameterDeclarationExpression)
            {
                ((CodeParameterDeclarationExpression) obj).Type = typeRef;
            }
            else if (obj is CodeMemberField)
            {
                CodeMemberField memberField = (CodeMemberField) obj;
                memberField.Type = typeRef;
                SetInitExpressionType(memberField.InitExpression, typeRef);
            }
            else if (obj is CodeMemberMethod)
            {
                ((CodeMemberMethod) obj).ReturnType = typeRef;
            }
            else if (obj is CodeMemberProperty)
            {
                ((CodeMemberProperty) obj).Type = typeRef;
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private static void SetInitExpressionType(CodeExpression initExpr, CodeTypeReference typeRef)
        {
            if (typeRef.ArrayElementType != null)
            {
                if (initExpr is CodeArrayCreateExpression)
                {
                    CodeArrayCreateExpression arrayCreateExpr = (CodeArrayCreateExpression) initExpr;
                    arrayCreateExpr.CreateType = typeRef.ArrayElementType;
                }
                else if (initExpr is CodeRectangularArrayCreateExpression)
                {
                    CodeRectangularArrayCreateExpression rectangularArrayCreateExpr = (CodeRectangularArrayCreateExpression) initExpr;
                    rectangularArrayCreateExpr.CreateType = typeRef.ArrayElementType;
                }
            }
        }

        //---------------------------------------

        private static CodeObjectSource FindRealType(CodeObjectSource source)
        {
            if (source != null)
            {
                return InternalFindRealType(new List<CodeObjectSource>(new CodeObjectSource[] {source}));
            }

            return CodeObjectSource.Create(typeof (object));
        }

        private static CodeObjectSource InternalFindRealType(IList<CodeObjectSource> sources)
        {
            CodeObjectSource source = sources[sources.Count - 1];

            if (source != null)
            {
                if (Utils.IsRealType(source.Target))
                {
                    return source;
                }
                else if (Utils.IsEntity(source.Target))
                {
                    CodeObjectSource targetSource = CodeObjectMetaData.GetEntitySource((CodeObject) source.Target);

                    if (targetSource != null && !sources.Contains(targetSource))
                    {
                        sources.Add(CodeObjectSource.CreateMerge(targetSource, source.ArrayRanks, source.Type));
                        return InternalFindRealType(sources);
                    }
                }
            }

            return CodeObjectSource.Create(typeof (object));
        }

        private static bool ImplyEntityType(CodeObjectSource entitySource, CodeObjectSource newSource)
        {
            Debug.Assert(Utils.IsEntity(entitySource.Target));

            if (newSource != null)
            {
                CodeObject entity = (CodeObject) entitySource.Target;

                if (entity is CodeVariableDeclarationStatement)
                {
                    CodeVariableDeclarationStatement variableDeclStmt = (CodeVariableDeclarationStatement) entity;

                    if (CodeObjectMetaData.GetFailed(variableDeclStmt))
                    {
                        return false;
                    }
                    else
                    {
                        newSource = CodeObjectSource.CreateMergeXXX(newSource, entitySource.ArrayRanks, entitySource.Type);

                        CodeObjectSource newRealType = FindRealType(newSource);

                        if (Utils.IsEntity(newSource.Target))
                        {
                            newSource = newRealType;
                        }

                        CodeObjectSource oldRealType = Utils.GetTypeReferenceSource(GetEntityType(entity));

                        if (CodeObjectMetaData.HasEntitySource(entity))
                        {
                            CodeObjectSource oldSource = oldRealType;

                            if (oldRealType.Target == typeof (object) && oldRealType.ArrayRanks.Length == newSource.ArrayRanks.Length)
                            {
                                CodeObjectSource lastSource = CodeObjectMetaData.GetEntitySource(entity);

                                if (lastSource != null && lastSource.Target == NullPlaceholder.Value && lastSource.ArrayRanks.Length == newSource.ArrayRanks.Length)
                                {
                                    oldSource = lastSource;
                                }
                            }

                            newSource = FindCommonSource(oldSource, newSource);
                            newRealType = FindRealType(newSource);

                            if (newSource != null && newSource.Target == typeof (object) && newSource.ArrayRanks.Length == 0)
                            {
                                CodeObjectMetaData.SetFailed(variableDeclStmt, true);
                            }
                        }

                        CodeObjectMetaData.SetEntitySource(entity, newSource);
                        bool changed = (newRealType != oldRealType);

                        if (changed)
                        {
                            SetEntityType(entity, Utils.CreateTypeReference(newRealType));
                        }

                        return changed;
                    }
                }
                else
                {
                    newSource = CodeObjectSource.CreateMergeXXX(newSource, entitySource.ArrayRanks, entitySource.Type);
                    CodeObjectSource oldRealType = Utils.GetTypeReferenceSource(GetEntityType(entity));
                    CodeObjectSource newRealType = FindRealType(newSource);
                    CodeObjectSource oldSource = CodeObjectMetaData.GetEntitySource(entity);

                    if (oldSource != null)
                    {
                        newSource = FindCommonSource(oldSource, newSource);

                        if (newSource == null)
                        {
                            newSource = CodeObjectSource.Create(typeof (object));
                            newRealType = newSource;
                        }
                        else
                        {
                            newRealType = FindRealType(newSource);
                        }
                    }

                    if (newSource != oldSource)
                    {
                        CodeObjectMetaData.SetEntitySource(entity, newSource);
                    }

                    if (newRealType == oldRealType)
                    {
                        return false;
                    }
                    else
                    {
                        CodeTypeReference typeRef = Utils.CreateTypeReference(newRealType);
                        SetEntityType(entity, typeRef);
                    }

                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        private static CodeObjectSource FindCommonSource(CodeObjectSource leftSource, CodeObjectSource rightSource)
        {
            if (leftSource == null || rightSource == null)
            {
                return null;
            }
            else if (Equals(leftSource, rightSource))
            {
                return leftSource;
            }
            else if (leftSource.Type == rightSource.Type && leftSource.ArrayRanks.Length == rightSource.ArrayRanks.Length)
            {
                bool equal = true;

                for (int i = 0; i < leftSource.ArrayRanks.Length; i++)
                {
                    if (leftSource.ArrayRanks[i] != rightSource.ArrayRanks[i])
                    {
                        equal = false;
                    }
                }

                if (equal)
                {
                    if (rightSource.Target is NullPlaceholder)
                    {
                        CodeObjectSource tempSource = rightSource;
                        rightSource = leftSource;
                        leftSource = tempSource;
                    }

                    if (leftSource.Target is NullPlaceholder)
                    {
                        if (Utils.IsRealType(rightSource.Target))
                        {
                            Type type = rightSource.Target as Type;

                            if (type != null && type.IsValueType && !type.IsGenericType)
                            {
                                rightSource = CodeObjectSource.CreateNullable(rightSource);
                            }

                            return rightSource;
                        }
                        else if (Utils.IsEntity(rightSource.Target))
                        {
                            CodeObjectSource source = CodeObjectMetaData.GetTypeReferenceSource(GetEntityType((CodeObject) rightSource.Target));
                            return FindCommonSource(leftSource, CodeObjectSource.CreateMerge(source, rightSource.ArrayRanks, rightSource.Type));
                        }
                        else if (rightSource.Target is FormPlaceholder)
                        {
                            Debug.Assert(false);
                        }
                        else
                        {
                            Debug.Assert(false);
                        }
                    }
                    else if (leftSource.Target.GetType() == rightSource.Target.GetType())
                    {
                        Type leftType = leftSource.Target as Type;
                        Type rightType = rightSource.Target as Type;

                        if (leftType != null && rightType != null)
                        {
                            bool isLeftGeneric = leftType.IsGenericType;
                            bool isRightGeneric = rightType.IsGenericType;

                            if (isLeftGeneric == isRightGeneric)
                            {
                                if (isRightGeneric)
                                {
                                    leftType = leftType.GetGenericArguments()[0];
                                    rightType = rightType.GetGenericArguments()[0];
                                }

                                if (leftType == rightType)
                                {
                                    return leftSource;
                                }
                                else if (leftType == typeof (int) && rightType == typeof (double))
                                {
                                    return rightSource;
                                }
                                else if (leftType == typeof (double) && rightType == typeof (int))
                                {
                                    return leftSource;
                                }
                            }
                            else
                            {
                                if (isRightGeneric)
                                {
                                    leftSource = rightSource;
                                    Type type = leftType;
                                    leftType = rightType;
                                    rightType = type;
                                }

                                Type genericType = leftType.GetGenericArguments()[0];

                                if (genericType == rightType)
                                {
                                    return leftSource;
                                }
                                else if (genericType == typeof (int) && rightType == typeof (double))
                                {
                                    return CodeObjectSource.CreateNullable(rightSource);
                                }
                                else if (genericType == typeof (double) && rightType == typeof (int))
                                {
                                    return leftSource;
                                }
                            }
                        }
                    }
                    else
                    {
                        bool found = false;

                        if (Utils.IsEntity(leftSource.Target))
                        {
                            CodeObjectSource leftRealType = CodeObjectMetaData.GetTypeReferenceSource(GetEntityType((CodeObject) leftSource.Target));
                            leftSource = CodeObjectSource.CreateMerge(leftRealType, leftSource.ArrayRanks, leftSource.Type);
                            found = true;
                        }

                        if (Utils.IsEntity(rightSource.Target))
                        {
                            CodeObjectSource rightRealType = CodeObjectMetaData.GetTypeReferenceSource(GetEntityType((CodeObject) rightSource.Target));
                            rightSource = CodeObjectSource.CreateMerge(rightRealType, rightSource.ArrayRanks, rightSource.Type);
                            found = true;
                        }

                        if (found)
                        {
                            return FindCommonSource(leftSource, rightSource);
                        }
                    }
                }
            }
            else
            {
                if (leftSource.Target is NullPlaceholder && leftSource.ArrayRanks.Length == 0 &&
                    rightSource.Type == CodeObjectSourceType.Array)
                {
                    return rightSource;
                }
                else if (rightSource.Target is NullPlaceholder && rightSource.ArrayRanks.Length == 0 &&
                         leftSource.Type == CodeObjectSourceType.Array)
                {
                    return leftSource;
                }
            }

            return CodeObjectSource.Create(typeof (object));
        }

        //---------------------------------------

        private static ISet<string> _warnings;
        private static ISet<string> _errors;

        private void LogWarning(string text, params object[] args)
        {
            if (_log != null && _finalAttempt)
            {
                string formatText = string.Format(text, args);

                if (!_warnings.Contains(formatText))
                {
                    _log.Warn(formatText);
                    _warnings.Add(formatText);
                }
            }
        }

        private void LogError(string text, params object[] args)
        {
            if (_log != null && _finalAttempt)
            {
                string formatText = string.Format(text, args);

                if (!_errors.Contains(formatText))
                {
                    _log.Error(formatText);
                    _errors.Add(formatText);
                }
            }
        }
    }
}