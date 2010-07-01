using Microsoft.Build.BuildEngine;

namespace Sage.SalesLogix.Migration.Module.VSProject
{
    public sealed class CSharpProject : DefaultVSProject
    {
        public CSharpProject(string name, string namespaceName)
            : base(name, namespaceName)
        {
            AddNewImport(@"$(MSBuildBinPath)\Microsoft.CSharp.targets", null);
        }

        public override string SpecialDirectory
        {
            get { return "Properties"; }
        }

        protected override void OnSetupGeneralGroup(BuildPropertyGroup group)
        {
            group.AddNewProperty("AppDesignerFolder", "Properties");
        }

        protected override void OnSetupDebugGroup(BuildPropertyGroup group)
        {
            group.AddNewProperty("Optimize", "false");
            group.AddNewProperty("DefineConstants", "DEBUG;TRACE");
            group.AddNewProperty("ErrorReport", "prompt");
            group.AddNewProperty("WarningLevel", "4");
        }

        protected override void OnSetupReleaseGroup(BuildPropertyGroup group)
        {
            group.AddNewProperty("DefineConstants", "TRACE");
            group.AddNewProperty("ErrorReport", "prompt");
            group.AddNewProperty("WarningLevel", "4");
        }

        protected override void OnAddResource(BuildItem item)
        {
            item.SetMetadata("Generator", "ResXFileCodeGenerator");
        }
    }
}