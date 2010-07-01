using Microsoft.Build.BuildEngine;

namespace Sage.SalesLogix.Migration.Module.VSProject
{
    public sealed class VBNetProject : DefaultVSProject
    {
        public VBNetProject(string name, string namespaceName)
            : base(name, namespaceName)
        {
            AddNewImport(@"$(MSBuildBinPath)\Microsoft.VisualBasic.targets", null);
        }

        public override string SpecialDirectory
        {
            get { return "My Project"; }
        }

        protected override void OnSetupGeneralGroup(BuildPropertyGroup group)
        {
            group.AddNewProperty("MyType", "Windows");
        }

        protected override void OnSetupDebugGroup(BuildPropertyGroup group)
        {
            group.AddNewProperty("DefineDebug", "true");
            group.AddNewProperty("DefineTrace", "true");
            group.AddNewProperty("DocumentationFile", Name + ".xml");
            group.AddNewProperty("NoWarn", "42016,41999,42017,42018,42019,42032,42036,42020,42021,42022");
        }

        protected override void OnSetupReleaseGroup(BuildPropertyGroup group)
        {
            group.AddNewProperty("DefineDebug", "false");
            group.AddNewProperty("DefineTrace", "true");
            group.AddNewProperty("DocumentationFile", Name + ".xml");
            group.AddNewProperty("NoWarn", "42016,41999,42017,42018,42019,42032,42036,42020,42021,42022");
        }

        protected override void OnAddResource(BuildItem item)
        {
            item.SetMetadata("CustomToolNamespace", "My.Resources");
            item.SetMetadata("Generator", "VbMyResourcesResXFileCodeGenerator");
        }
    }
}