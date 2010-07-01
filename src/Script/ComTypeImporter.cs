using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Iesi.Collections;
using Microsoft.Win32;

namespace Sage.SalesLogix.Migration.Script
{
    public sealed class ComTypeImporter : BaseTypeImporter
    {
        private static readonly object _remoteObj;
        private static readonly MethodInfo _runMethod;
        private static readonly FieldInfo _optionsField;
        private static readonly Hashtable _alreadyImportedLibraries;
        private static readonly FieldInfo _assemblyNameField;

        static ComTypeImporter()
        {
            string sdkDirectoryName;

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\.NETFramework"))
            {
                if (key == null)
                {
                    //TODO: log this - "Unable to find .NET Framework SDK registry key"
                    sdkDirectoryName = null;
                }
                else
                {
                    sdkDirectoryName = key.GetValue("sdkInstallRootv2.0") as string;
                }
            }

            if (sdkDirectoryName == null)
            {
                //TODO: log this - "Unable to find .NET Framework SDK install directory"
            }
            else
            {
                string tlbImpFileName = Path.Combine(sdkDirectoryName, @"Bin\TlbImp.exe");

                if (!File.Exists(tlbImpFileName))
                {
                    //TODO: log this - "Unable to find .NET Framework SDK type library importer utility"
                }
                else
                {
                    Assembly assembly = Assembly.LoadFrom(tlbImpFileName);
                    Type remoteType = assembly.GetType("TlbImpCode.RemoteTlbImp");
                    Type codeType = assembly.GetType("TlbImpCode.TlbImpCode");
                    Type optionsType = assembly.GetType("TlbImpCode.TlbImpOptions");

                    _remoteObj = Activator.CreateInstance(remoteType, true);
                    _runMethod = remoteType.GetMethod("Run");
                    _optionsField = codeType.GetField("s_Options", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    _alreadyImportedLibraries = (Hashtable) codeType.GetField("s_AlreadyImportedLibraries", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                    _assemblyNameField = optionsType.GetField("m_strAssemblyName");
                }
            }
        }

        public ComTypeImporter(string outputDirectory, StrongNameKeyPair keyPair, IDictionary<string, string> importedFileNames)
            : base(outputDirectory, keyPair, importedFileNames) {}

        public Type ImportProgId(string progId)
        {
            Type type;

            if (!CachedTypes.TryGetValue(progId, out type))
            {
                string clsID = null;

                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(string.Format(@"{0}\CLSID", progId)))
                {
                    if (key != null)
                    {
                        clsID = key.GetValue(null) as string;
                    }
                }

                if (clsID == null)
                {
                    type = null;
                }
                else
                {
                    type = Resolve(clsID);
                }

                CachedTypes.Add(progId, type);
            }

            return type;
        }

        protected override Type ResolveType(string clsId, string fileName)
        {
            Assembly assembly;

            if (!LoadedAssemblies.TryGetValue(fileName, out assembly))
            {
                assembly = CreateTypeLibWrapper(fileName);
                LoadedAssemblies.Add(fileName, assembly);
            }

            Type type = null;

            if (assembly != null)
            {
                Guid clsIDGuid = new Guid(clsId);
                type = Array.Find(
                    assembly.GetTypes(),
                    delegate(Type comType)
                        {
                            return (comType.GUID == clsIDGuid);
                        });
            }

            return type;
        }

        private Assembly CreateTypeLibWrapper(string typeLibFileName)
        {
            if (_remoteObj == null)
            {
                return null;
            }

            typeLibFileName = new FileInfo(typeLibFileName).FullName;

            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(typeLibFileName);

            byte[] publicKey;

            if (KeyPair == null)
            {
                publicKey = null;
            }
            else
            {
                publicKey = KeyPair.PublicKey;
            }

            bool hasOutputDirectory = !string.IsNullOrEmpty(OutputDirectory);
            string previousCurrentDirectory;

            if (hasOutputDirectory)
            {
                if (!Directory.Exists(OutputDirectory))
                {
                    Directory.CreateDirectory(OutputDirectory);
                }

                previousCurrentDirectory = Environment.CurrentDirectory;
                Environment.CurrentDirectory = OutputDirectory;
            }
            else
            {
                previousCurrentDirectory = null;
            }

            try
            {
                ICollection existingLibraries = new ArrayList(_alreadyImportedLibraries.Keys);
                _runMethod.Invoke(_remoteObj, new object[]
                                                  {
                                                      typeLibFileName,
                                                      null,
                                                      null,
                                                      OutputDirectory,
                                                      publicKey,
                                                      KeyPair,
                                                      null,
                                                      null,
                                                      new Version(versionInfo.ProductMajorPart, versionInfo.ProductMinorPart, versionInfo.ProductBuildPart, versionInfo.ProductPrivatePart),
                                                      TypeLibImporterFlags.ReflectionOnlyLoading,
                                                      true,
                                                      true,
                                                      false,
                                                      false,
                                                      false,
                                                      false
                                                  });
                ISet newLibraries = new HashedSet(_alreadyImportedLibraries.Keys);
                newLibraries.RemoveAll(existingLibraries);

                foreach (Guid newLibraryID in newLibraries)
                {
                    Assembly libraryAssembly = (Assembly) _alreadyImportedLibraries[newLibraryID];
                    ImportedFileNames[libraryAssembly.FullName] = libraryAssembly.GetName().Name + ".dll";
                }

                string assemblyFileName = (string) _assemblyNameField.GetValue(_optionsField.GetValue(null));
                Assembly assembly = Assembly.LoadFrom(hasOutputDirectory
                                                          ? Path.Combine(OutputDirectory, assemblyFileName)
                                                          : assemblyFileName);
                ImportedFileNames[assembly.FullName] = assemblyFileName;
                return assembly;
            }
            finally
            {
                if (previousCurrentDirectory != null)
                {
                    Environment.CurrentDirectory = previousCurrentDirectory;
                }
            }
        }
    }
}