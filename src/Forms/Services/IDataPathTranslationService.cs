namespace Sage.SalesLogix.Migration.Forms.Services
{
    public interface IDataPathTranslationService
    {
        void RegisterTable(string tableName);
        void RegisterField(DataPath dataPath);
        void RegisterJoin(DataPath leftPath, DataPath rightPath);
        string TranslateTable(string tableName);
        string TranslateField(DataPath dataPath);
        string TranslateField(DataPath dataPath, DataPath prefixPath);
        string TranslateReference(DataPath dataPath, string targetTable, string targetField);
        string TranslateCollection(DataPath dataPath, string targetTable, string targetField);
    }
}