using System;
using System.CodeDom;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using Sage.SalesLogix.Migration.Script.CodeDom;

namespace Sage.SalesLogix.Migration.Script.Services
{
    public sealed class NamespaceFactorizer
    {
        private readonly IComparer<string> _comparer;
        private ISet<string> _namespaceNames;

        public NamespaceFactorizer()
        {
            _comparer = new DelegateComparer<string>(CompareNames);
        }

        private string _currentNameSpaceName;

        public void Factorize(CodeNamespace nameSpace)
        {
            _namespaceNames = new SortedSet<string>(_comparer);
            _currentNameSpaceName = nameSpace.Name;
            new CodeDomWalker(nameSpace).Walk(Factorize);
            _currentNameSpaceName = null;

            foreach (string nameSpaceName in _namespaceNames)
            {
                nameSpace.Imports.Add(new CodeNamespaceImport(nameSpaceName));
            }

            _namespaceNames = null;
        }

        private void Factorize(ref CodeObject target, CodeObject parent, int indent)
        {
            CodeTypeReference typeRef = target as CodeTypeReference;

            if (typeRef != null)
            {
                CodeObjectSource source = Utils.GetTypeReferenceSource(typeRef);

                if (source != null)
                {
                    Type type = source.Target as Type;

                    if (type != null &&
                        type != typeof (void) &&
                        type != typeof (object) &&
                        type != typeof (string) &&
                        type != typeof (decimal) &&
                        !type.IsPrimitive)
                    {
                        string baseType = typeRef.BaseType;
                        int pos = baseType.LastIndexOf('.');

                        if (pos >= 0)
                        {
                            string name = baseType.Substring(0, pos);

                            if (name != _currentNameSpaceName)
                            {
                                _namespaceNames.Add(name);
                            }

                            typeRef.BaseType = baseType.Substring(pos + 1);
                        }
                    }
                }
            }
        }

        private static int CompareNames(string left, string right)
        {
            if (left == right)
            {
                return 0;
            }
            else if (left == "System")
            {
                return -1;
            }
            else if (right == "System")
            {
                return 1;
            }
            else
            {
                bool leftIsSystem = left.StartsWith("System.");
                bool rightIsSystem = right.StartsWith("System.");

                if (leftIsSystem == rightIsSystem)
                {
                    return string.Compare(left, right);
                }
                else if (leftIsSystem)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }
    }
}