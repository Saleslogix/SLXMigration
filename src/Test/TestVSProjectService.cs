using System.CodeDom.Compiler;
using System.Windows.Forms;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using Sage.SalesLogix.Migration.Module.Services;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration.Tests
{
    public static class TestVSProjectService
    {
        public static void Test()
        {
            Test(Language.VBNet, new VBCodeProvider());
            Test(Language.CSharp, new CSharpCodeProvider());
        }

        private static void Test(Language language, CodeDomProvider provider)
        {
            MigrationSettings settings = new MigrationSettings();
            settings.VSProjectName = "ClassLibrary1";
            settings.Namespace = "ClassLibrary1";
            settings.Language = language;

            MigrationContext context = new MigrationContext(
                settings,
                null,
                null,
                null,
                null,
                provider,
                null,
                null,
                new EmptyOperationStatus(),
                null);
            context.LocalizedStrings.Add("One", "Uno");
            context.References.Add("AxSLXControls, Version=1.0.0.0, Culture=neutral, processorArchitecture=MSIL", @"AxSLXControls.dll");
            context.References.Add(typeof (Form).Assembly.GetName().Name, null);

            Plugin plugin = new Plugin();
            plugin.Family = "Test";
            plugin.Name = "Class1";
            plugin.Blob.Data = new byte[0];
            ScriptInfo script = new ScriptInfo(plugin);
            context.Scripts.Add("0", script);

            MigrationContextHolderService holder = new MigrationContextHolderService();
            holder.Context = context;

            VSProjectService p = new VSProjectService();
            p.ContextHolder = holder;
            p.Generate();
        }
    }
}