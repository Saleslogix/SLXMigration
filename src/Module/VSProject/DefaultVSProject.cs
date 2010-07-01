using System;
using System.ComponentModel;
using Microsoft.Build.BuildEngine;

namespace Sage.SalesLogix.Migration.Module.VSProject
{
    public class DefaultVSProject : Project, IVSProject
    {
        private static readonly string _systemAssemblyName = typeof (EditorBrowsableAttribute).Assembly.GetName().Name;
        private readonly string _name;
        private readonly string _namespaceName;
        private BuildItemGroup _referenceGroup;
        private BuildItemGroup _compileGroup;
        private BuildItemGroup _embeddedResourceGroup;
        private bool _isSystemReferenced;

        public DefaultVSProject(string name, string namespaceName)
            : base(new Engine("dummy"))
        {
            _name = name;
            _namespaceName = namespaceName;
            DefaultTargets = "Build";
            SetupGeneralGroup();
            SetupDebugGroup();
            SetupReleaseGroup();
        }

        private BuildItemGroup ReferenceGroup
        {
            get { return _referenceGroup ?? (_referenceGroup = AddNewItemGroup()); }
        }

        private BuildItemGroup CompileGroup
        {
            get { return _compileGroup ?? (_compileGroup = AddNewItemGroup()); }
        }

        private BuildItemGroup EmbeddedResourceGroup
        {
            get { return _embeddedResourceGroup ?? (_embeddedResourceGroup = AddNewItemGroup()); }
        }

        public string Name
        {
            get { return _name; }
        }

        private void SetupGeneralGroup()
        {
            BuildPropertyGroup group = AddNewPropertyGroup(true);
            group.AddNewProperty("Configuration", "Debug").Condition = " '$(Configuration)' == '' ";
            group.AddNewProperty("Platform", "AnyCPU").Condition = " '$(Platform)' == '' ";
            group.AddNewProperty("ProductVersion", "8.0.50727");
            group.AddNewProperty("SchemaVersion", "2.0");
            group.AddNewProperty("ProjectGuid", Guid.NewGuid().ToString("B").ToUpper());
            group.AddNewProperty("OutputType", "Library");

            if (!string.IsNullOrEmpty(_namespaceName))
            {
                group.AddNewProperty("RootNamespace", _namespaceName);
            }

            group.AddNewProperty("AssemblyName", _name);
            OnSetupGeneralGroup(group);
        }

        private void SetupDebugGroup()
        {
            BuildPropertyGroup group = AddNewPropertyGroup(true);
            group.Condition = " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ";
            group.AddNewProperty("DebugSymbols", "true");
            group.AddNewProperty("DebugType", "full");
            group.AddNewProperty("OutputPath", @"bin\Debug\");
            OnSetupDebugGroup(group);
        }

        private void SetupReleaseGroup()
        {
            BuildPropertyGroup group = AddNewPropertyGroup(true);
            group.Condition = " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ";
            group.AddNewProperty("DebugType", "pdbonly");
            group.AddNewProperty("Optimize", "true");
            group.AddNewProperty("OutputPath", @"bin\Release\");
            OnSetupReleaseGroup(group);
        }

        protected virtual void OnSetupGeneralGroup(BuildPropertyGroup group) {}
        protected virtual void OnSetupDebugGroup(BuildPropertyGroup group) {}
        protected virtual void OnSetupReleaseGroup(BuildPropertyGroup group) {}
        protected virtual void OnAddResource(BuildItem item) {}

        #region IVSProject Members

        public void AddReference(string assemblyName, string hintPath)
        {
            bool isSystem = (assemblyName == _systemAssemblyName);

            if (!isSystem || !_isSystemReferenced)
            {
                BuildItem item = ReferenceGroup.AddNewItem("Reference", assemblyName);

                if (!string.IsNullOrEmpty(hintPath))
                {
                    item.SetMetadata("SpecificVersion", "False");
                    item.SetMetadata("HintPath", hintPath);
                }

                _isSystemReferenced |= isSystem;
            }
        }

        public void AddCompiledFile(string fileName)
        {
            CompileGroup.AddNewItem("Compile", fileName);
        }

        public void AddResource(string fileName, string designerFileName)
        {
            BuildItem item = EmbeddedResourceGroup.AddNewItem("EmbeddedResource", fileName);
            item.SetMetadata("SubType", "Designer");
            OnAddResource(item);
            item.SetMetadata("LastGenOutput", designerFileName);
        }

        public void AddDesignerCompiledFile(string fileName, string dependentUpon)
        {
            BuildItem item = CompileGroup.AddNewItem("Compile", fileName);
            item.SetMetadata("AutoGen", "True");
            item.SetMetadata("DesignTime", "True");
            item.SetMetadata("DependentUpon", dependentUpon);

            if (!_isSystemReferenced)
            {
                AddReference(_systemAssemblyName, null);
            }
        }

        public virtual string SpecialDirectory
        {
            get { return null; }
        }

        #endregion
    }
}