using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    public abstract class ExtendedCodeProvider : CodeDomProvider
    {
        internal abstract void Initialize(CodeDomProvider innerProvider);
    }

    public abstract class ExtendedCodeProvider<T> : ExtendedCodeProvider, ICodeGenerator
        where T : CodeDomProvider
    {
        private readonly IDictionary<Type, SubstitutionHandler> _handlers;
        private T _innerProvider;
        private ICodeGenerator _generator;
        private CodeGeneratorOptions _options;
        private int _indent;
        private IDictionary<CodeObject, CodeObject> _substitutions;

        protected delegate CodeObject SubstitutionHandler(CodeObject obj, CodeObject parent);

        private delegate void MethodInvoker();

        protected ExtendedCodeProvider()
        {
            _handlers = new Dictionary<Type, SubstitutionHandler>();
        }

        internal override void Initialize(CodeDomProvider innerProvider)
        {
            _innerProvider = (T) innerProvider;
#pragma warning disable 618,612
            _generator = innerProvider.CreateGenerator();
#pragma warning restore 618,612
        }

        #region CodeDomProvider members

        [Obsolete("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class. Those inheriting from CodeDomProvider must still implement this interface, and should exclude this warning or also obsolete this method.")]
        public override ICodeCompiler CreateCompiler()
        {
            return _innerProvider.CreateCompiler();
        }

        [Obsolete("Callers should not use the ICodeGenerator interface and should instead use the methods directly on the CodeDomProvider class. Those inheriting from CodeDomProvider must still implement this interface, and should exclude this warning or also obsolete this method.")]
        public override ICodeGenerator CreateGenerator()
        {
            return _generator;
        }

        public override CodeCompileUnit Parse(TextReader codeStream)
        {
            return _innerProvider.Parse(codeStream);
        }

        public override CompilerResults CompileAssemblyFromDom(CompilerParameters options, params CodeCompileUnit[] compilationUnits)
        {
            return _innerProvider.CompileAssemblyFromDom(options, compilationUnits);
        }

        public override CompilerResults CompileAssemblyFromFile(CompilerParameters options, params string[] fileNames)
        {
            return _innerProvider.CompileAssemblyFromFile(options, fileNames);
        }

        public override CompilerResults CompileAssemblyFromSource(CompilerParameters options, params string[] sources)
        {
            return _innerProvider.CompileAssemblyFromSource(options, sources);
        }

        public override ICodeGenerator CreateGenerator(TextWriter output)
        {
            return _innerProvider.CreateGenerator(output);
        }

        public override ICodeGenerator CreateGenerator(string fileName)
        {
            return _innerProvider.CreateGenerator(fileName);
        }

        [Obsolete("Callers should not use the ICodeParser interface and should instead use the methods directly on the CodeDomProvider class. Those inheriting from CodeDomProvider must still implement this interface, and should exclude this warning or also obsolete this method.")]
        public override ICodeParser CreateParser()
        {
            return _innerProvider.CreateParser();
        }

        public override TypeConverter GetConverter(Type type)
        {
            return _innerProvider.GetConverter(type);
        }

        public override bool IsValidIdentifier(string value)
        {
            return _innerProvider.IsValidIdentifier(value);
        }

        public override bool Supports(GeneratorSupport generatorSupport)
        {
            return _innerProvider.Supports(generatorSupport);
        }

        public override string CreateEscapedIdentifier(string value)
        {
            return _innerProvider.CreateEscapedIdentifier(value);
        }

        public override string CreateValidIdentifier(string value)
        {
            return _innerProvider.CreateValidIdentifier(value);
        }

        public override string GetTypeOutput(CodeTypeReference type)
        {
            return _innerProvider.GetTypeOutput(type);
        }

        public override void GenerateCodeFromMember(CodeTypeMember member, TextWriter writer, CodeGeneratorOptions options)
        {
            _innerProvider.GenerateCodeFromMember(member, writer, options);
        }

        public override LanguageOptions LanguageOptions
        {
            get { return _innerProvider.LanguageOptions; }
        }

        public override string FileExtension
        {
            get { return _innerProvider.FileExtension; }
        }

        #endregion

        #region ICodeGenerator Members

        public override void GenerateCodeFromCompileUnit(CodeCompileUnit e, TextWriter w, CodeGeneratorOptions o)
        {
            PerformSubstitutions(e, o, delegate
                {
                    _generator.GenerateCodeFromCompileUnit(e, w, o);
                });
        }

        public override void GenerateCodeFromExpression(CodeExpression e, TextWriter w, CodeGeneratorOptions o)
        {
            PerformSubstitutions(e, o, delegate
                {
                    _generator.GenerateCodeFromExpression(e, w, o);
                });
        }

        public override void GenerateCodeFromNamespace(CodeNamespace e, TextWriter w, CodeGeneratorOptions o)
        {
            PerformSubstitutions(e, o, delegate
                {
                    _generator.GenerateCodeFromNamespace(e, w, o);
                });
        }

        public override void GenerateCodeFromStatement(CodeStatement e, TextWriter w, CodeGeneratorOptions o)
        {
            PerformSubstitutions(e, o, delegate
                {
                    _generator.GenerateCodeFromStatement(e, w, o);
                });
        }

        public override void GenerateCodeFromType(CodeTypeDeclaration e, TextWriter w, CodeGeneratorOptions o)
        {
            PerformSubstitutions(e, o, delegate
                {
                    _generator.GenerateCodeFromType(e, w, o);
                });
        }

        public void ValidateIdentifier(string value)
        {
            _generator.ValidateIdentifier(value);
        }

        #endregion

        protected void RegisterSubstitution(Type objectType, SubstitutionHandler handler)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException("objectType");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            if (!typeof (CodeObject).IsAssignableFrom(objectType))
            {
                throw new ArgumentException("objectType");
            }

            _handlers[objectType] = handler;
        }

        protected CodeDomProvider InnerProvider
        {
            get { return _innerProvider; }
        }

        protected CodeGeneratorOptions Options
        {
            get { return _options; }
        }

        protected int Indent
        {
            get { return _indent; }
        }

        protected string GenerateCurrentIndentString()
        {
            string[] parts = new string[_indent];

            for (int i = 0; i < _indent; i++)
            {
                parts[i] = _options.IndentString;
            }

            return string.Join(string.Empty, parts);
        }

        protected void WriteStatements(CodeStatementCollection stmts, IndentedTextWriter writer, bool indented)
        {
            using (new IndentedTextWriterIndentBlock(writer, (indented ? 1 : 0)))
            {
                foreach (CodeStatement stmt in stmts)
                {
                    using (new IndentedTextWriterIndentBlock(writer, (stmt is CodeSnippetStatement ? -writer.Indent : 0)))
                    {
                        GenerateCodeFromStatement(stmt, writer, Options);
                    }
                }
            }
        }

        private void PerformSubstitutions(CodeObject obj, CodeGeneratorOptions options, MethodInvoker invoker)
        {
            bool rootPerform = (_substitutions == null);

            if (rootPerform)
            {
                _substitutions = new Dictionary<CodeObject, CodeObject>();
                _options = options;
                CodeObject[] array = new CodeObject[] {obj};
                new CodeDomWalker(array).Walk(MakeSubstitutions);
                obj = array[0];
                _options = null;
            }

            invoker();

            if (rootPerform)
            {
                if (_substitutions.Count > 0)
                {
                    new CodeDomWalker(obj).Walk(RevertSubstitutions);
                }

                _substitutions = null;
            }
        }

        private void MakeSubstitutions(ref CodeObject target, CodeObject parent, int indent)
        {
            SubstitutionHandler handler;
            Type type = target.GetType();

            while (!_handlers.TryGetValue(type, out handler) && typeof (CodeObject).IsAssignableFrom(type))
            {
                type = type.BaseType;
            }

            if (handler != null)
            {
                _indent = indent;
                CodeObject newObj = handler(target, parent);

                if (newObj != target)
                {
                    _substitutions.Add(newObj, target);
                    target = newObj;
                }
            }
        }

        private void RevertSubstitutions(ref CodeObject target, CodeObject parent, int indent)
        {
            CodeObject originalObj;

            if (_substitutions.TryGetValue(target, out originalObj))
            {
                new CodeDomWalker(originalObj).Walk(RevertSubstitutions);
                target = originalObj;
            }
        }
    }
}