namespace Sage.SalesLogix.Migration
{
    public interface IVSProject
    {
        void AddReference(string assemblyName, string hintPath);
        void AddCompiledFile(string fileName);
        void AddResource(string fileName, string designerFileName);
        void AddDesignerCompiledFile(string fileName, string dependentUpon);
        string SpecialDirectory { get; }
        void Save(string projectFileName);
    }
}