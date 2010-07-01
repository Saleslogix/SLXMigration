using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using Sage.SalesLogix.Migration.Script.CodeDom;

namespace Sage.SalesLogix.Migration.Script.Services
{
    public sealed class AssemblyReferenceGatherer
    {
        private readonly IDictionary<string, string> _references;
        private static readonly Assembly _mscorlibAssembly = typeof (void).Assembly;
        private static readonly Assembly _thisAssembly = typeof (IAxForm).Assembly;

        public AssemblyReferenceGatherer(IDictionary<string, string> references)
        {
            _references = references;
        }

        public void Gather(CodeTypeDeclaration typeDecl)
        {
            new CodeDomWalker(typeDecl).Walk(Gather);
        }

        private void Gather(ref CodeObject target, CodeObject parent, int depth)
        {
            CodeTypeReference typeRef = target as CodeTypeReference;

            if (typeRef != null)
            {
                CodeObjectSource source = Utils.GetTypeReferenceSource(typeRef);

                if (source != null)
                {
                    Type type = source.Target as Type;

                    if (type != null)
                    {
                        Assembly assembly = type.Assembly;

                        if (assembly != _mscorlibAssembly && assembly != _thisAssembly && !_references.ContainsKey(assembly.FullName))
                        {
                            _references.Add(assembly.FullName, (assembly.GlobalAssemblyCache ? null : assembly.Location));
                        }
                    }
                }
            }
        }
    }
}