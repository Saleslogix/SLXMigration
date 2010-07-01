using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    public static class ExtendedCodeProviderManager
    {
        private static readonly IDictionary<Type, Type> _registry;

        static ExtendedCodeProviderManager()
        {
            _registry = new Dictionary<Type, Type>();
            Register(typeof (ExtendedCSharpCodeProvider));
            Register(typeof (ExtendedVBCodeProvider));
        }

        public static void Register(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            Type baseType = type;
            Type providerType = null;

            while (baseType != typeof (object))
            {
                baseType = baseType.BaseType;

                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof (ExtendedCodeProvider<>))
                {
                    providerType = baseType.GetGenericArguments()[0];
                    break;
                }
            }

            if (providerType == null)
            {
                throw new ArgumentException("type");
            }

            if (_registry.ContainsKey(providerType))
            {
                throw new ArgumentException("type");
            }

            _registry[providerType] = type;
        }

        public static CodeDomProvider Create(Type providerType)
        {
            CodeDomProvider provider = (CodeDomProvider) Activator.CreateInstance(providerType);
            Type extensionType;

            if (_registry.TryGetValue(providerType, out extensionType))
            {
                ExtendedCodeProvider extendedProvider = (ExtendedCodeProvider) Activator.CreateInstance(extensionType);
                extendedProvider.Initialize(provider);
                provider = extendedProvider;
            }

            return provider;
        }
    }
}