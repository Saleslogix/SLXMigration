using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Sage.SalesLogix.Migration.Forms
{
    public sealed class ActiveXTypeImporter : BaseTypeImporter
    {
        private readonly IDictionary<string, string> _existingWrappers;
        private AxImporter.Options _options;

        public ActiveXTypeImporter(string outputDirectory, StrongNameKeyPair keyPair, IDictionary<string, string> importedFileNames)
            : base(outputDirectory, keyPair, importedFileNames)
        {
            _existingWrappers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        public Type ImportCGuid(string cGuid)
        {
            Type type;

            if (!CachedTypes.TryGetValue(cGuid, out type))
            {
                type = Resolve(string.Format("{{{0}}}", cGuid.ToLower()));
                CachedTypes.Add(cGuid, type);
            }

            return type;
        }

        protected override Type ResolveType(string clsId, string fileName)
        {
            Assembly assembly;

            if (!LoadedAssemblies.TryGetValue(fileName, out assembly))
            {
                assembly = CreateActiveXWrapper(fileName);
                LoadedAssemblies.Add(fileName, assembly);
            }

            return Array.Find(
                assembly.GetTypes(),
                delegate(Type comType)
                    {
                        object[] attributes = comType.GetCustomAttributes(typeof (AxHost.ClsidAttribute), false);
                        return (attributes.Length > 0 && StringUtils.CaseInsensitiveEquals(((AxHost.ClsidAttribute) attributes[0]).Value, clsId));
                    });
        }

        private Assembly CreateActiveXWrapper(string ocxFileName)
        {
            bool hasOutputDirectory = !string.IsNullOrEmpty(OutputDirectory);

            if (hasOutputDirectory && !Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
            }

            if (_options == null)
            {
                _options = new AxImporter.Options();
                _options.outputDirectory = OutputDirectory;
                _options.keyPair = KeyPair;
                _options.references = new AxImporterResolver(ImportedFileNames);
            }
            else
            {
                _options.outputName = null;
            }

            AxImporter importer = new AxImporter(_options);
            importer.GenerateFromFile(new FileInfo(ocxFileName));

            foreach (string fileName in importer.GeneratedAssemblies)
            {
                _existingWrappers.Add(Path.GetFileNameWithoutExtension(fileName), fileName);
                Assembly libraryAssembly = Assembly.LoadFrom(fileName);
                ImportedFileNames[libraryAssembly.FullName] = Path.GetFileName(fileName);
            }

            return Assembly.LoadFrom(hasOutputDirectory
                                         ? Path.Combine(OutputDirectory, _options.outputName)
                                         : _options.outputName);
        }

        private sealed class AxImporterResolver : AxImporter.IReferenceResolver
        {
            private readonly IDictionary<string, string> _existingWrappers;

            public AxImporterResolver(IDictionary<string, string> existingWrappers)
            {
                _existingWrappers = existingWrappers;
            }

            #region AxImporter.IReferenceResolver Members

#pragma warning disable 618,612

            public string ResolveActiveXReference(UCOMITypeLib typeLib)
            {
                return null;
            }

            public string ResolveComReference(AssemblyName name)
            {
                string fileName;

                if (!_existingWrappers.TryGetValue(name.Name, out fileName))
                {
                    fileName = name.EscapedCodeBase;
                }

                return fileName;
            }

            public string ResolveComReference(UCOMITypeLib typeLib)
            {
                string fileName;
                _existingWrappers.TryGetValue(Marshal.GetTypeLibName(typeLib), out fileName);
                return fileName;
            }

            public string ResolveManagedReference(string assemName)
            {
                return (assemName + ".dll");
            }

#pragma warning restore 618,612

            #endregion
        }
    }
}