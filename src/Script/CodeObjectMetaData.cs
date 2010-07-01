using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using Sage.SalesLogix.Migration.Script.Collections;

namespace Sage.SalesLogix.Migration.Script
{
    public static class CodeObjectMetaData
    {
        private static IDictionary<CodeVariableDeclarationStatement, bool> _failed;
        private static IDictionary<CodeTypeReference, CodeObjectSource> _typeReferenceSources;
        private static IDictionary<CodeExpression, CodeObjectSource> _expressionSources;
        private static IDictionary<CodeObject, CodeObjectSource> _entitySources;
        private static IDictionary<CodeTypeDeclaration, string> _namespaceNames;
        private static IDictionary<CodeTypeDeclaration, CodeConstructor> _constructors;
        private static IDictionary<CodeTypeMember, CodeTypeDeclaration> _parents;
        private static IDictionary<CodeTypeDeclaration, IDictionary<string, CodeMemberField>> _fields;
        private static IDictionary<CodeTypeDeclaration, IDictionary<string, CodeMemberMethod>> _methods;
        private static IDictionary<CodeTypeDeclaration, IDictionary<string, CodeMemberProperty>> _properties;
        private static IDictionary<CodeTypeDeclaration, IDictionary<string, CodeTypeDeclaration>> _nestedTypes;
        private static IDictionary<CodeStatementCollection, IDictionary<string, CodeVariableDeclarationStatement>> _variables;

        public static bool GetFailed(CodeVariableDeclarationStatement variableDeclStmt)
        {
            return GetValue(_failed, variableDeclStmt);
        }

        public static void SetFailed(CodeVariableDeclarationStatement variableDeclStmt, bool flag)
        {
            SetValue(ref _failed, variableDeclStmt, flag, false);
        }

        public static bool TryGetTypeReferenceSource(CodeTypeReference typeRef, out CodeObjectSource source)
        {
            return TryGetValue(_typeReferenceSources, typeRef, out source);
        }

        public static CodeObjectSource GetTypeReferenceSource(CodeTypeReference typeRef)
        {
            return GetValue(_typeReferenceSources, typeRef);
        }

        public static void SetTypeReferenceSource(CodeTypeReference typeRef, CodeObjectSource source)
        {
            Debug.Assert(source == null || Utils.IsRealType(source.Target));
            SetValue(ref _typeReferenceSources, typeRef, source, false);
        }

        public static bool HasExpressionSource(CodeExpression expression)
        {
            return HasValue(_expressionSources, expression);
        }

        public static CodeObjectSource GetExpressionSource(CodeExpression expression)
        {
            return GetValue(_expressionSources, expression);
        }

        public static void SetExpressionSource(CodeExpression expression, CodeObjectSource source)
        {
            SetValue(ref _expressionSources, expression, source, false);
        }

        public static bool HasEntitySource(CodeObject entity)
        {
            Debug.Assert(Utils.IsEntity(entity));
            return HasValue(_entitySources, entity);
        }

        public static CodeObjectSource GetEntitySource(CodeObject entity)
        {
            Debug.Assert(Utils.IsEntity(entity));
            return GetValue(_entitySources, entity);
        }

        public static void SetEntitySource(CodeObject entity, CodeObjectSource source)
        {
            Debug.Assert(Utils.IsEntity(entity));
            SetValue(ref _entitySources, entity, source, false);
        }

        public static string GetNamespaceName(CodeTypeDeclaration typeDecl)
        {
            return GetValue(_namespaceNames, typeDecl);
        }

        public static void SetNamespaceName(CodeTypeDeclaration typeDecl, string namespaceName)
        {
            SetValue(ref _namespaceNames, typeDecl, namespaceName, false);
        }

        public static CodeConstructor GetConstructor(CodeTypeDeclaration typeDecl)
        {
            return GetValue(_constructors, typeDecl);
        }

        public static void SetConstructor(CodeTypeDeclaration typeDecl, CodeConstructor constructor)
        {
            SetValue(ref _constructors, typeDecl, constructor, false);
        }

        public static CodeTypeDeclaration GetParent(CodeTypeMember typeMember)
        {
            return GetValue(_parents, typeMember);
        }

        public static void SetParent(CodeTypeMember typeMember, CodeTypeDeclaration parent)
        {
            SetValue(ref _parents, typeMember, parent, true);
        }

        public static IDictionary<string, CodeMemberField> GetFields(CodeTypeDeclaration typeDecl)
        {
            return GetValue(_fields, typeDecl);
        }

        public static void SetFields(CodeTypeDeclaration typeDecl, IDictionary<string, CodeMemberField> fields)
        {
            SetValue(ref _fields, typeDecl, fields, false);
        }

        public static IDictionary<string, CodeMemberMethod> GetMethods(CodeTypeDeclaration typeDecl)
        {
            return GetValue(_methods, typeDecl);
        }

        public static void SetMethods(CodeTypeDeclaration typeDecl, IDictionary<string, CodeMemberMethod> methods)
        {
            SetValue(ref _methods, typeDecl, methods, false);
        }

        public static IDictionary<string, CodeMemberProperty> GetProperties(CodeTypeDeclaration typeDecl)
        {
            return GetValue(_properties, typeDecl);
        }

        public static void SetProperties(CodeTypeDeclaration typeDecl, IDictionary<string, CodeMemberProperty> properties)
        {
            SetValue(ref _properties, typeDecl, properties, false);
        }

        public static IDictionary<string, CodeTypeDeclaration> GetNestedTypes(CodeTypeDeclaration typeDecl)
        {
            return GetValue(_nestedTypes, typeDecl);
        }

        public static void SetNestedTypes(CodeTypeDeclaration typeDecl, IDictionary<string, CodeTypeDeclaration> nestedTypes)
        {
            SetValue(ref _nestedTypes, typeDecl, nestedTypes, false);
        }

        public static IDictionary<string, CodeVariableDeclarationStatement> GetVariables(CodeStatementCollection statements)
        {
            return GetValue(_variables, statements);
        }

        public static void SetVariables(CodeStatementCollection statements, IDictionary<string, CodeVariableDeclarationStatement> variables)
        {
            SetValue(ref _variables, statements, variables, false);
        }

        //---------------------------------------

        private static bool HasValue<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return (dictionary != null && dictionary.ContainsKey(key));
        }

        private static bool TryGetValue<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, out TValue value)
        {
            if (dictionary == null)
            {
                value = default(TValue);
                return false;
            }
            else
            {
                return dictionary.TryGetValue(key, out value);
            }
        }

        private static TValue GetValue<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary == null)
            {
                return default(TValue);
            }
            else
            {
                TValue value;
                dictionary.TryGetValue(key, out value);
                return value;
            }
        }

        private static void SetValue<TKey, TValue>(ref IDictionary<TKey, TValue> dictionary, TKey key, TValue value, bool useWeakValue)
        {
            if (dictionary == null)
            {
                dictionary = (useWeakValue
                                  ? (IDictionary<TKey, TValue>) new WeakValueDictionary<TKey, TValue>()
                                  : new WeakKeyDictionary<TKey, TValue>());
            }

            dictionary[key] = value;
        }
    }
}