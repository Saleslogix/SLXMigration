using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using Sage.Platform.Application;
using Sage.SalesLogix.Migration.Script.Services;
using Sage.SalesLogix.Migration.Script.VBSParser;
using Sage.SalesLogix.Migration.Services;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration.Script
{
    public sealed class ScriptMigrationSupport : IMigrationSupport
    {
        private MigrationContext _context;

        [ServiceDependency]
        public IMigrationContextHolderService ContextHolder
        {
            set
            {
                value.ContextChanged += delegate
                    {
                        _context = value.Context;
                    };
                _context = value.Context;
            }
        }

        private static readonly Parser _parser = new Parser();
        private DependencyBuilder _dependencyBuilder;
        private PropertySetValueCorrector _propertySetValueCorrector;
        private PropertyPartConsolidator _propertyPartConsolidator;
        private CreateObjectStrongTyper _createObjectStrongTyper;
        private NestedClassConstructorCorrector _nestedClassConstructorCorrector;
        private IdentifierDiscoverer _identifierDiscoverer;
        private StaticSetter _staticSetter;
        private IdentifierResolver _identifierResolver;
        private ReturnValueCorrector _returnValueCorrector;
        private StringLocalizer _stringLocalizer;
        private AssemblyReferenceGatherer _assemblyReferenceGatherer;
        private NamespaceFactorizer _namespaceFactorizer;

        #region IMigrationSupport Members

        public void Parse()
        {
            _dependencyBuilder = new DependencyBuilder(_context.Log, _context.Scripts);
            _propertySetValueCorrector = new PropertySetValueCorrector();
            _propertyPartConsolidator = new PropertyPartConsolidator();
            _createObjectStrongTyper = new CreateObjectStrongTyper(
                new ComTypeImporter(
                    Path.Combine(_context.Settings.OutputDirectory, "lib"),
                    _context.KeyPair,
                    _context.References));
            _nestedClassConstructorCorrector = new NestedClassConstructorCorrector();
            _identifierDiscoverer = new IdentifierDiscoverer(_context.Log);
            _staticSetter = new StaticSetter();
            _identifierResolver = new IdentifierResolver(_context.Log);
            _returnValueCorrector = new ReturnValueCorrector();
            _stringLocalizer = new StringLocalizer(_context.Settings.Namespace, _context.LocalizedStrings);
            _assemblyReferenceGatherer = new AssemblyReferenceGatherer(_context.References);
            _namespaceFactorizer = new NamespaceFactorizer();

            using (_context.Status.BeginStep("Parsing scripts...", _context.Plugins.Count + 1))
            {
                foreach (Plugin plugin in _context.Plugins)
                {
                    if (plugin.Type == PluginType.ActiveScript)
                    {
                        _context.Log.SourcePlugin = plugin;
                        LogInfo(false, "Parsing '{0}' script", plugin);
                        Parse(plugin);
                        _context.Log.SourcePlugin = null;
                    }

                    if (!_context.Status.Advance())
                    {
                        return;
                    }
                }

                bool unfinished = true;

                while (unfinished)
                {
                    unfinished = false;

                    foreach (ScriptInfo script in new List<ScriptInfo>(_context.Scripts.Values))
                    {
                        if (script.TypeDeclaration == null && !script.IsInvalid)
                        {
                            _context.Log.SourcePlugin = script.Plugin;
                            LogInfo(false, "Parsing '{0}' script", script);

                            if (script.IsForm)
                            {
                                ParseNew(script);
                            }
                            else
                            {
                                Parse(script);
                            }

                            unfinished = true;
                            _context.Log.SourcePlugin = null;
                        }

                        if (_context.Status.IsCancelled)
                        {
                            return;
                        }
                    }
                }
            }
        }

        public void Build()
        {
            using (_context.Status.BeginStep("Building scripts...", _context.Scripts.Count))
            {
                _identifierResolver.Resolve(_context.Scripts.Values);

                foreach (ScriptInfo script in _context.Scripts.Values)
                {
                    if (!script.IsInvalid)
                    {
                        _context.Log.SourcePlugin = script.Plugin;
                        LogInfo(false, "Building '{0}' script", script);
                        Build(script);
                        _context.Log.SourcePlugin = null;
                    }

                    if (!_context.Status.Advance())
                    {
                        break;
                    }
                }
            }
        }

        public void Persist()
        {
            using (_context.Status.BeginStep("Persisting scripts...", _context.Scripts.Count))
            {
                foreach (ScriptInfo script in _context.Scripts.Values)
                {
                    if (!script.IsInvalid)
                    {
                        _context.Log.SourcePlugin = script.Plugin;
                        LogInfo(false, "Persisting '{0}' script", script);
                        Persist(script);

                        if (script.Plugin.Type == PluginType.ActiveScript)
                        {
                            string outputDirectory = (string.IsNullOrEmpty(_context.Settings.OutputDirectory)
                                                          ? script.NamespaceName
                                                          : Path.Combine(_context.Settings.OutputDirectory, script.NamespaceName));
                            string fileName = string.Format("{0}.{1}", script.ClassName, _context.CodeProvider.FileExtension);
                            _context.Log.SetGeneratedItemMapping(script.Plugin, Path.Combine(outputDirectory, fileName));
                        }

                        LogInfo(true, "Item successfully migrated");
                        _context.Log.SourcePlugin = null;
                    }

                    if (!_context.Status.Advance())
                    {
                        break;
                    }
                }
            }
        }

        #endregion

        private void Parse(Plugin plugin)
        {
            string fullName = ScriptInfo.FormatPrefixedFullName("Script", plugin.Family, plugin.Name);

            if (!_context.Scripts.ContainsKey(fullName))
            {
                ParseNew(new ScriptInfo(plugin));
            }
        }

        public void Parse(Plugin plugin, string code, string formName, IDictionary<string, FormField> formControls)
        {
            ParseNew(new ScriptInfo(plugin, code, formName, formControls));
        }

        private void ParseNew(ScriptInfo script)
        {
            if (!_context.Scripts.ContainsKey(script.PrefixedFullName))
            {
                _context.Scripts.Add(script.PrefixedFullName, script);
            }

            _dependencyBuilder.Build(script);
            Parse(script);
        }

        public void Parse(ScriptInfo script)
        {
            try
            {
                script.TypeDeclaration = _parser.Parse(script.Code);
            }
            catch (ParserException ex)
            {
                LogError(ex.Message);
                script.IsInvalid = true;
            }

            if (script.TypeDeclaration != null)
            {
                script.TypeDeclaration.Name = script.ClassName;
                CodeObjectMetaData.SetNamespaceName(script.TypeDeclaration, script.NamespaceName);

                _propertySetValueCorrector.Correct(script.TypeDeclaration);
                _propertyPartConsolidator.Consolidate(script.TypeDeclaration);
                _createObjectStrongTyper.Process(script.TypeDeclaration);
                _nestedClassConstructorCorrector.Process(script.TypeDeclaration);
                _identifierDiscoverer.Process(script.TypeDeclaration);
            }
        }

        private void Build(ScriptInfo script)
        {
            _staticSetter.Set(script);
            _returnValueCorrector.Correct(script.TypeDeclaration);
            _stringLocalizer.Localize(script.TypeDeclaration);
        }

        private void Persist(ScriptInfo script)
        {
            _assemblyReferenceGatherer.Gather(script.TypeDeclaration);

            CodeGeneratorOptions generatorOptions = new CodeGeneratorOptions();
            generatorOptions.VerbatimOrder = true;
            string nameSpacePrefix = (string.IsNullOrEmpty(_context.Settings.Namespace)
                                          ? null
                                          : _context.Settings.Namespace + ".");
            CodeNamespace nameSpace = new CodeNamespace(nameSpacePrefix + script.NamespaceName);
            nameSpace.Types.Add(script.TypeDeclaration);
            _namespaceFactorizer.Factorize(nameSpace);
            string outputDirectory = (string.IsNullOrEmpty(_context.Settings.OutputDirectory)
                                          ? script.NamespaceName
                                          : Path.Combine(_context.Settings.OutputDirectory, script.NamespaceName));

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string fileName = string.Format("{0}.{1}", script.ClassName, _context.CodeProvider.FileExtension);

            using (TextWriter writer = new StreamWriter(Path.Combine(outputDirectory, fileName)))
            {
                _context.CodeProvider.GenerateCodeFromNamespace(nameSpace, writer, generatorOptions);
            }
        }

        private void LogInfo(bool persist, string text, params object[] args)
        {
            if (_context != null && _context.Log != null)
            {
                _context.Log.Info(persist, text, args);
            }
        }

        private void LogError(string text, params object[] args)
        {
            if (_context != null && _context.Log != null)
            {
                _context.Log.Error(text, args);
            }
        }
    }
}