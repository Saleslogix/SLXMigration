using System.Collections.Generic;
using Sage.Platform.Orm.Entities;
using Sage.SalesLogix.Migration.Orm;

namespace Sage.SalesLogix.Migration.Services
{
    public interface IOrmEntityLoaderService
    {
        IList<OrmProject> LoadProjects();
        OrmProject LoadProject(string name);
        OrmLookup LoadLookup(string lookupStr);
        IList<OrmTable> LoadTables();
        IList<OrmKeyColumn> LoadKeyColumns(string tableName);
        OrmPackage LoadPackage(string name);
        OrmEntity LoadEntity(string tableName);
    }
}