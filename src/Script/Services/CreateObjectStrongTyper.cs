using System;
using System.CodeDom;
using Sage.SalesLogix.Migration.Script.CodeDom;

namespace Sage.SalesLogix.Migration.Script.Services
{
    /// <summary>
    /// Converts the CreateObject global function into a strongly typed
    /// constructor call. If a ComTypeImporter has been provided, it will
    /// be used to map from the ProgId to the actual type.
    /// </summary>
    public sealed class CreateObjectStrongTyper
    {
        private readonly ComTypeImporter _importer;

        public CreateObjectStrongTyper(ComTypeImporter importer)
        {
            _importer = importer;
        }

        public void Process(CodeTypeDeclaration typeDecl)
        {
            new CodeDomWalker(typeDecl).Walk(Process);
        }

        private void Process(ref CodeObject target, CodeObject parent, int indent)
        {
            CodeMethodInvokeExpression methodInvokeExpr = target as CodeMethodInvokeExpression;

            if (methodInvokeExpr != null &&
                methodInvokeExpr.Method.TargetObject == null &&
                StringUtils.CaseInsensitiveEquals(methodInvokeExpr.Method.MethodName, "CreateObject") &&
                methodInvokeExpr.Parameters.Count == 1)
            {
                CodePrimitiveExpression primitiveExpr = methodInvokeExpr.Parameters[0] as CodePrimitiveExpression;

                if (primitiveExpr != null)
                {
                    string progId = primitiveExpr.Value as string;

                    if (progId != null)
                    {
                        Type type = (_importer == null
                                         ? Type.GetTypeFromProgID(progId)
                                         : _importer.ImportProgId(progId));

                        if (type != null)
                        {
                            target = new CodeObjectCreateExpression(Utils.CreateTypeReference(type));
                        }
                    }
                }
            }
        }
    }
}