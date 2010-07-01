using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Resources.Tools;
using Sage.Platform.Application;
using Sage.SalesLogix.Migration.Services;

namespace Sage.SalesLogix.Migration.Module.Services
{
    public sealed class VSProjectService : IVSProjectService
    {
        private readonly StepHandler[] _steps;
        private MigrationContext _context;

        public VSProjectService()
        {
            _steps = new StepHandler[]
                {
                    CreateProject,
                    AddReferences,
                    AddClasses,
                    AddResources,
                    PersistProject
                };
        }

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

        private string _outputDirectory;

        #region IVSProjectService Members

        public void Generate()
        {
            int totalSteps = _steps.Length
                             + _context.References.Count
                             + _context.Scripts.Count;

            using (_context.Status.BeginStep("Generating Visual Studio project...", totalSteps))
            {
                foreach (StepHandler step in _steps)
                {
                    step();

                    if (!_context.Status.Advance())
                    {
                        break;
                    }
                }
            }
        }

        #endregion

        private void CreateProject()
        {
            _outputDirectory = (string.IsNullOrEmpty(_context.Settings.OutputDirectory)
                                    ? Environment.CurrentDirectory
                                    : _context.Settings.OutputDirectory);
        }

        private void AddReferences()
        {
            foreach (KeyValuePair<string, string> reference in _context.References)
            {
                string hintPath;

                if (string.IsNullOrEmpty(reference.Value))
                {
                    hintPath = null;
                }
                else if (Path.IsPathRooted(reference.Value))
                {
                    string outputDirectory = Path.Combine(_outputDirectory, "lib");

                    if (!Directory.Exists(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }

                    string fileName = Path.GetFileName(reference.Value);
                    string fullFileName = Path.Combine(outputDirectory, fileName);

                    if (!File.Exists(fullFileName))
                    {
                        File.Copy(reference.Value, fullFileName);
                    }

                    hintPath = @".\lib\" + fileName;
                }
                else
                {
                    hintPath = @".\lib\" + reference.Value;
                }

                _context.VSProject.AddReference(reference.Key, hintPath);

                if (!_context.Status.Advance())
                {
                    break;
                }
            }
        }

        private void AddClasses()
        {
            foreach (ScriptInfo script in _context.Scripts.Values)
            {
                _context.VSProject.AddCompiledFile(string.Format(@"{0}\{1}.{2}", script.NamespaceName, script.ClassName, _context.CodeProvider.FileExtension));

                if (!_context.Status.Advance())
                {
                    break;
                }
            }
        }

        private void AddResources()
        {
            if (_context.LocalizedStrings.Count > 0)
            {
                string directoryName = Path.Combine(_outputDirectory, _context.VSProject.SpecialDirectory);

                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                string className = "Resources";
                string fileName = className + ".resx";
                string fullFileName = Path.Combine(directoryName, fileName);
                string designerFileName = string.Format("{0}.Designer.{1}", className, _context.CodeProvider.FileExtension);
                string fullDesignerFileName = Path.Combine(directoryName, designerFileName);

                using (IResourceWriter writer = new ResXResourceWriter(fullFileName))
                {
                    foreach (KeyValuePair<string, string> item in _context.LocalizedStrings)
                    {
                        writer.AddResource(item.Key, item.Value);
                    }
                }

                string[] unmatchable;
                CodeCompileUnit unit = StronglyTypedResourceBuilder.Create((IDictionary) _context.LocalizedStrings, className, _context.Settings.Namespace, _context.CodeProvider, true, out unmatchable);
                Debug.Assert(unmatchable == null || unmatchable.Length == 0);

                using (TextWriter writer = new StreamWriter(fullDesignerFileName))
                {
                    _context.CodeProvider.GenerateCodeFromCompileUnit(unit, writer, new CodeGeneratorOptions());
                }

                _context.VSProject.AddDesignerCompiledFile(_context.VSProject.SpecialDirectory + @"\" + designerFileName, fileName);
                _context.VSProject.AddResource(_context.VSProject.SpecialDirectory + @"\" + fileName, designerFileName);
            }
        }

        private void PersistProject()
        {
            _context.VSProject.Save(Path.Combine(_outputDirectory, string.Format("{0}.{1}proj", _context.Settings.VSProjectName, _context.CodeProvider.FileExtension)));
        }
    }
}