namespace Sage.SalesLogix.Migration
{
    public interface IMigrationSupport
    {
        void Parse();
        void Build();
        void Persist();
    }
}