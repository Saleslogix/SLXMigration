using System;

namespace Sage.SalesLogix.Migration.Services
{
    public interface IMigrationContextHolderService
    {
        MigrationContext Context { get; set; }
        event EventHandler ContextChanged;
    }
}