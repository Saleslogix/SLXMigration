using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Sage.SalesLogix.Migration.Script
{
    public static class Utils
    {
        public static CodeTypeReference CreateTypeReference(CodeObjectSource source)
        {
            CodeTypeReference typeRef;

            if (source.Type == CodeObjectSourceType.Indexer)
            {
                typeRef = new CodeTypeReference(typeof (object));
                CodeObjectMetaData.SetTypeReferenceSource(typeRef, CodeObjectSource.Create(typeof (object)));
                return typeRef;
            }

            if (source.Target is Type)
            {
                Type type = (Type) source.Target;

                for (int i = source.ArrayRanks.Length - 1; i >= 0; i--)
                {
                    int rank = source.ArrayRanks[i];
                    type = (rank == 1
                                ? type.MakeArrayType()
                                : type.MakeArrayType(rank));
                }

                source = CodeObjectSource.Create(type);
                typeRef = new CodeTypeReference(type);
            }
            else if (source.Target is CodeTypeDeclaration)
            {
                CodeTypeDeclaration typeDecl = (CodeTypeDeclaration) source.Target;
                StringBuilder builder = new StringBuilder();

                for (int i = source.ArrayRanks.Length - 1; i >= 0; i--)
                {
                    builder.Insert(0, "[");

                    for (int j = 0; j < source.ArrayRanks[i] - 1; j++)
                    {
                        builder.Insert(0, ",");
                    }

                    builder.Insert(0, "]");
                }

                CodeTypeDeclaration currentTypeDecl = typeDecl;

                while (true)
                {
                    builder.Insert(0, currentTypeDecl.Name);
                    CodeTypeDeclaration parentTypeDecl = CodeObjectMetaData.GetParent(currentTypeDecl);

                    if (parentTypeDecl == null)
                    {
                        builder.Insert(0, ".");
                        builder.Insert(0, CodeObjectMetaData.GetNamespaceName(currentTypeDecl));
                        break;
                    }
                    else
                    {
                        builder.Insert(0, "+");
                        currentTypeDecl = parentTypeDecl;
                    }
                }

                typeRef = new CodeTypeReference(builder.ToString());
            }
            else if (source.Target == null)
            {
                Debug.Assert(false, "Unexpected null source target");
                return null;
            }
            else
            {
                Debug.Assert(false, "Unexpected source target type: " + source.Target.GetType().Name);
                return null;
            }

            CodeObjectMetaData.SetTypeReferenceSource(typeRef, source);
            return typeRef;
        }

        public static CodeTypeReference CreateTypeReference(Type type)
        {
            CodeTypeReference typeRef = new CodeTypeReference(type);
            CodeObjectMetaData.SetTypeReferenceSource(typeRef, CodeObjectSource.Create(type));
            return typeRef;
        }

        public static CodeTypeReference CreateTypeReference(CodeTypeDeclaration typeDecl)
        {
            List<string> typeNameParts = new List<string>();
            CodeTypeDeclaration currentTypeDecl = typeDecl;

            while (true)
            {
                typeNameParts.Add(currentTypeDecl.Name);
                CodeTypeDeclaration parentTypeDecl = CodeObjectMetaData.GetParent(currentTypeDecl);

                if (parentTypeDecl == null)
                {
                    typeNameParts.Add(".");
                    typeNameParts.Add(CodeObjectMetaData.GetNamespaceName(currentTypeDecl));
                    break;
                }
                else
                {
                    typeNameParts.Add("+");
                    currentTypeDecl = parentTypeDecl;
                }
            }

            typeNameParts.Reverse();
            CodeTypeReference typeRef = new CodeTypeReference(string.Join(string.Empty, typeNameParts.ToArray()));
            CodeObjectMetaData.SetTypeReferenceSource(typeRef, CodeObjectSource.Create(typeDecl));
            return typeRef;
        }

        public static Type CreateTypeFromTypeReference(CodeTypeReference typeRef)
        {
            Type type;

            if (typeRef.ArrayRank == 1)
            {
                type = CreateTypeFromTypeReference(typeRef.ArrayElementType);

                if (type != null)
                {
                    type = type.MakeArrayType();
                }
            }
            else
            {
                type = Type.GetType(typeRef.BaseType);

                if (type != null)
                {
                    int typeArgCount = typeRef.TypeArguments.Count;

                    if (typeArgCount > 0)
                    {
                        if (type.IsGenericTypeDefinition)
                        {
                            Type[] typeArgs = new Type[typeArgCount];

                            for (int i = 0; i < typeArgCount; i++)
                            {
                                Type typeArgType = CreateTypeFromTypeReference(typeRef.TypeArguments[i]);

                                if (typeArgType == null)
                                {
                                    return null;
                                }

                                typeArgs[i] = typeArgType;
                            }

                            type = type.MakeGenericType(typeArgs);
                        }
                        else
                        {
                            return null;
                        }
                    }

                    if (typeRef.ArrayRank > 0)
                    {
                        type = (typeRef.ArrayRank == 1
                                    ? type.MakeArrayType()
                                    : type.MakeArrayType(typeRef.ArrayRank));
                    }
                }
            }

            return type;
        }

        public static CodeObjectSource GetTypeReferenceSource(CodeTypeReference typeRef)
        {
            CodeObjectSource source;

            if (!CodeObjectMetaData.TryGetTypeReferenceSource(typeRef, out source))
            {
                source = CodeObjectSource.Create(CreateTypeFromTypeReference(typeRef));
                CodeObjectMetaData.SetTypeReferenceSource(typeRef, source);
            }

            return source;
        }

        public static bool CompareTypeReferences(CodeTypeReference left, Type right)
        {
            if (right == null)
            {
                return (left == null);
            }
            else
            {
                return CompareTypeReferences(left, new CodeTypeReference(right));
            }
        }

        public static bool CompareTypeReferences(CodeTypeReference left, CodeTypeReference right)
        {
            if (left == right)
            {
                return true;
            }
            else if (left == null || right == null)
            {
                return false;
            }
            else if (left.ArrayRank != right.ArrayRank ||
                     left.Options != right.Options ||
                     left.BaseType != right.BaseType ||
                     !CompareTypeReferences(left.ArrayElementType, right.ArrayElementType))
            {
                return false;
            }
            else
            {
                int count = left.TypeArguments.Count;

                if (count != right.TypeArguments.Count)
                {
                    return false;
                }
                else
                {
                    for (int i = 0; i < left.TypeArguments.Count; i++)
                    {
                        if (!CompareTypeReferences(left.TypeArguments[i], right.TypeArguments[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
        }

        //=======================================

        public static bool IsEntity(object obj)
        {
            return (obj is CodeVariableDeclarationStatement ||
                    obj is CodeParameterDeclarationExpression ||
                    obj is CodeMemberField ||
                    obj is CodeMemberMethod ||
                    obj is CodeMemberProperty);
        }

        public static bool IsRealType(object obj)
        {
            return (obj is Type ||
                    obj is CodeTypeDeclaration);
        }

        public static bool IsSource(object obj)
        {
            return (IsEntity(obj) ||
                    IsRealType(obj) ||
                    obj is NullPlaceholder ||
                    obj is FormPlaceholder);
        }
    }
}