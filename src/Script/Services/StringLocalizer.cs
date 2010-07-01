using System.CodeDom;
using System.Collections.Generic;
using Interop.SalesLogix;
using Sage.Platform.Exceptions;
using Sage.SalesLogix.Migration.Script.CodeDom;

namespace Sage.SalesLogix.Migration.Script.Services
{
    public sealed class StringLocalizer
    {
        private readonly string _namespaceName;
        private readonly IDictionary<string, string> _localizedStrings;

        public StringLocalizer(string namespaceName, IDictionary<string, string> localizedStrings)
        {
            Guard.ArgumentNotNull(localizedStrings, "localizedStrings");

            _namespaceName = namespaceName;
            _localizedStrings = localizedStrings;
        }

        public void Localize(CodeTypeDeclaration typeDecl)
        {
            new CodeDomWalker(typeDecl).Walk(Localize);
        }

        private void Localize(ref CodeObject target, CodeObject parent, int indent)
        {
            CodeMethodInvokeExpression methodInvokeExpr = target as CodeMethodInvokeExpression;

            if (methodInvokeExpr != null && methodInvokeExpr.Parameters.Count == 1 && methodInvokeExpr.Method.TargetObject != null)
            {
                CodeObjectSource source = CodeObjectMetaData.GetExpressionSource(methodInvokeExpr.Method.TargetObject);

                if (source != null && source.Target == typeof (ITranslator) && source.ArrayRanks.Length == 0)
                {
                    CodePrimitiveExpression primitiveExpr = methodInvokeExpr.Parameters[0] as CodePrimitiveExpression;

                    if (primitiveExpr != null)
                    {
                        string value = primitiveExpr.Value as string;

                        if (value != null)
                        {
                            string baseName = GenerateResourceName(value);
                            int counter = 0;
                            string name = baseName;
                            string existingValue;

                            while (_localizedStrings.TryGetValue(name, out existingValue) && value != existingValue)
                            {
                                name = baseName + (++counter);
                            }

                            target = new CodePropertyReferenceExpression(
                                new CodeTypeReferenceExpression(
                                    new CodeTypeReference(
                                        _namespaceName + (string.IsNullOrEmpty(_namespaceName) ? string.Empty : ".") + "Properties.Resources")),
                                name);

                            if (existingValue == null)
                            {
                                _localizedStrings.Add(name, value);
                            }
                        }
                    }
                }
            }
        }

        private static string GenerateResourceName(string value)
        {
            char[] letters = value.ToCharArray();

            for (int i = 0; i < value.Length; i++)
            {
                char c = letters[i];

                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    letters[i] = '_';
                }
            }

            return new string(letters);
        }
    }
}