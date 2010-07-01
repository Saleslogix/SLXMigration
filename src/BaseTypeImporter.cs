using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Win32;

namespace Sage.SalesLogix.Migration
{
    public abstract class BaseTypeImporter
    {
        private readonly string _outputDirectory;
        private readonly StrongNameKeyPair _keyPair;
        private readonly IDictionary<string, string> _importedFileNames;
        private readonly IDictionary<string, Assembly> _loadedAssemblies;
        private readonly IDictionary<string, Type> _cachedTypes;

        protected BaseTypeImporter(string outputDirectory, StrongNameKeyPair keyPair, IDictionary<string, string> importedFileNames)
        {
            _outputDirectory = outputDirectory;
            _keyPair = keyPair;
            _importedFileNames = importedFileNames;
            _loadedAssemblies = new Dictionary<string, Assembly>();
            _cachedTypes = new Dictionary<string, Type>();
        }

        protected string OutputDirectory
        {
            get { return _outputDirectory; }
        }

        protected StrongNameKeyPair KeyPair
        {
            get { return _keyPair; }
        }

        protected IDictionary<string, string> ImportedFileNames
        {
            get { return _importedFileNames; }
        }

        protected IDictionary<string, Assembly> LoadedAssemblies
        {
            get { return _loadedAssemblies; }
        }

        protected IDictionary<string, Type> CachedTypes
        {
            get { return _cachedTypes; }
        }

        protected Type Resolve(string clsId)
        {
            string assemblyName;
            string typeName;
            string fileName;

            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(string.Format(@"CLSID\{0}\InprocServer32", clsId)))
            {
                if (key == null)
                {
                    assemblyName = null;
                    typeName = null;
                    fileName = null;
                }
                else
                {
                    int subKeyCount = key.SubKeyCount;

                    if (subKeyCount > 0)
                    {
                        using (RegistryKey subKey = key.OpenSubKey(key.GetSubKeyNames()[subKeyCount - 1]))
                        {
                            assemblyName = subKey.GetValue("assembly") as string;
                            typeName = subKey.GetValue("class") as string;
                            fileName = subKey.GetValue(null) as string;
                        }
                    }
                    else
                    {
                        assemblyName = key.GetValue("assembly") as string;
                        typeName = key.GetValue("class") as string;
                        fileName = key.GetValue(null) as string;
                    }
                }
            }

            Type type;

            if (assemblyName == null || typeName == null)
            {
                if (fileName == null)
                {
                    string typeLibId;

                    using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(string.Format(@"CLSID\{0}\TypeLib", clsId)))
                    {
                        if (key == null)
                        {
                            typeLibId = null;
                        }
                        else
                        {
                            typeLibId = key.GetValue(null) as string;
                        }
                    }

                    if (typeLibId != null)
                    {
                        using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"TypeLib\" + typeLibId))
                        {
                            if (key != null)
                            {
                                string[] subKeyNames = key.GetSubKeyNames();
                                Array.Sort(subKeyNames);

                                if (subKeyNames.Length > 0)
                                {
                                    using (RegistryKey subKey = key.OpenSubKey(subKeyNames[subKeyNames.Length - 1] + @"\0\win32"))
                                    {
                                        if (subKey != null)
                                        {
                                            fileName = subKey.GetValue(null) as string;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                type = (fileName != null
                            ? ResolveType(clsId, fileName)
                            : null);
            }
            else
            {
                Assembly assembly;

                if (!_loadedAssemblies.TryGetValue(assemblyName, out assembly))
                {
                    assembly = Assembly.Load(assemblyName);
                    _loadedAssemblies.Add(assemblyName, assembly);
                }

                type = assembly.GetType(typeName);
            }

            return type;
        }

        protected abstract Type ResolveType(string clsId, string fileName);
    }
}