using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using NHibernate;
using NHibernate.Criterion;
using Sage.Platform.Application;
using Sage.Platform.Orm;
using Sage.Platform.Orm.Entities;
using Sage.Platform.Projects.Interfaces;
using Sage.SalesLogix.Migration.Collections;
using Sage.SalesLogix.Migration.Orm;
using Sage.SalesLogix.Migration.Services;

namespace Sage.SalesLogix.Migration.Module.Services
{
    public sealed class OrmEntityLoaderService : IOrmEntityLoaderService
    {
        private IProjectContextService _projectContext;
        private MigrationContext _context;
        private ISet<string> _fetchedLookupTables;
        private IDictionary<string, OrmLookup> _cachedLookups;

        [ServiceDependency]
        public IProjectContextService ProjectContext
        {
            set { _projectContext = value; }
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

        #region IOrmEntityLoaderService Members

        public IList<OrmProject> LoadProjects()
        {
            using (ISession session = new SessionScopeWrapper())
            {
                return session.CreateCriteria(typeof (OrmProject))
                    .AddOrder(new Order("Name", true))
                    .List<OrmProject>();
            }
        }

        public OrmProject LoadProject(string name)
        {
            Guard.ArgumentNotNullOrEmptyString(name, "name");

            using (ISession session = new SessionScopeWrapper())
            {
                IList<OrmProject> list = session.CreateCriteria(typeof (OrmProject))
                    .Add(Expression.Eq("Name", name))
                    .List<OrmProject>();
                return (list.Count > 0 ? list[0] : null);
            }
        }

        public OrmLookup LoadLookup(string lookupStr)
        {
            Guard.ArgumentNotNullOrEmptyString(lookupStr, "lookupStr");
            OrmLookup lookup;

            if (_cachedLookups == null)
            {
                _cachedLookups = new Dictionary<string, OrmLookup>(StringComparer.InvariantCultureIgnoreCase);
            }

            if (!_cachedLookups.TryGetValue(lookupStr, out lookup))
            {
                int pos = lookupStr.IndexOf(':');

                if (pos >= 0)
                {
                    string mainTable = lookupStr.Substring(0, pos);

                    if (_fetchedLookupTables == null)
                    {
                        _fetchedLookupTables = new ComparisonSet<string>(StringComparer.InvariantCultureIgnoreCase);
                    }

                    if (!_fetchedLookupTables.Contains(mainTable))
                    {
                        foreach (OrmLookup item in LoadLookups(mainTable))
                        {
                            CacheLookup(item);
                        }

                        _cachedLookups.TryGetValue(lookupStr, out lookup);
                        _fetchedLookupTables.Add(mainTable);
                    }
                }
                else
                {
                    lookup = LoadLookupByID(lookupStr);

                    if (lookup != null)
                    {
                        CacheLookup(lookup);
                    }
                }
            }

            return lookup;
        }

        public IList<OrmTable> LoadTables()
        {
            using (ISession session = new SessionScopeWrapper())
            {
                return session.CreateCriteria(typeof (OrmTable))
                    .AddOrder(new Order("TableName", true))
                    .List<OrmTable>();
            }
        }

        public IList<OrmKeyColumn> LoadKeyColumns(string tableName)
        {
            Guard.ArgumentNotNullOrEmptyString(tableName, "tableName");

            using (ISession session = new SessionScopeWrapper())
            {
                return session.CreateCriteria(typeof (OrmKeyColumn))
                    .Add(Expression.Eq("TableName", tableName))
                    .List<OrmKeyColumn>();
            }
        }

        public OrmPackage LoadPackage(string name)
        {
            return CollectionUtils.Find(
                _projectContext.ActiveProject.Models.Get<OrmModel>().Packages,
                delegate(OrmPackage package)
                    {
                        return (StringUtils.CaseInsensitiveEquals(package.Name, name));
                    });
        }

        public OrmEntity LoadEntity(string tableName)
        {
            return CollectionUtils.Find(
                OrmEntity.GetAll(_projectContext.ActiveProject),
                delegate(OrmEntity entity)
                    {
                        return (StringUtils.CaseInsensitiveEquals(entity.TableName, tableName));
                    });
        }

        #endregion

        private void CacheLookup(OrmLookup lookup)
        {
            _cachedLookups[lookup.LookupId] = lookup;

            string lookupStrWithAlias = null;

            try
            {
                lookupStrWithAlias = GenerateLookupDef(lookup, false);
            }
            catch (MigrationException ex)
            {
                LogWarning(ex.Message);
            }

            if (lookupStrWithAlias != null)
            {
                if (_cachedLookups.ContainsKey(lookupStrWithAlias))
                {
                    LogWarning("Duplicate lookup '{0}' found", lookupStrWithAlias);
                }
                else
                {
                    _cachedLookups.Add(lookupStrWithAlias, lookup);
                }
            }

            string lookupStrWithPhysical = null;

            try
            {
                lookupStrWithPhysical = GenerateLookupDef(lookup, true);
            }
            catch (MigrationException ex)
            {
                LogWarning(ex.Message);
            }

            if (lookupStrWithPhysical != null && !StringUtils.CaseInsensitiveEquals(lookupStrWithAlias, lookupStrWithPhysical))
            {
                if (_cachedLookups.ContainsKey(lookupStrWithPhysical))
                {
                    LogWarning("Duplicate lookup '{0}' found", lookupStrWithPhysical);
                }
                else
                {
                    _cachedLookups.Add(lookupStrWithPhysical, lookup);
                }
            }
        }

        private static IList<OrmLookup> LoadLookups(string tableName)
        {
            Guard.ArgumentNotNullOrEmptyString(tableName, "tableName");

            using (ISession session = new SessionScopeWrapper())
            {
                return session.CreateCriteria(typeof (OrmLookup))
                    .Add(Expression.Eq("MainTable", tableName))
                    .List<OrmLookup>();
            }
        }

        private static OrmLookup LoadLookupByID(string lookupId)
        {
            Guard.ArgumentNotNullOrEmptyString(lookupId, "lookupId");

            using (ISession session = new SessionScopeWrapper())
            {
                IList<OrmLookup> list = session.CreateCriteria(typeof (OrmLookup))
                    .Add(Expression.Eq("LookupId", lookupId))
                    .List<OrmLookup>();
                return (list.Count > 0 ? list[0] : null);
            }
        }

        private static OrmTable LoadTableByName(string tableName)
        {
            Guard.ArgumentNotNullOrEmptyString(tableName, "tableName");

            using (ISession session = new SessionScopeWrapper())
            {
                IList<OrmTable> list = session.CreateCriteria(typeof (OrmTable))
                    .Add(Expression.Eq("TableName", tableName))
                    .List<OrmTable>();
                return (list.Count > 0 ? list[0] : null);
            }
        }

        private static OrmCalculatedField LoadCalculatedField(string tableName, string fieldName)
        {
            Guard.ArgumentNotNullOrEmptyString(tableName, "tableName");
            Guard.ArgumentNotNullOrEmptyString(fieldName, "fieldName");

            using (ISession session = new SessionScopeWrapper())
            {
                IList<OrmCalculatedField> list = session.CreateCriteria(typeof (OrmCalculatedField))
                    .Add(Expression.Eq("TableName", tableName))
                    .Add(Expression.Like("CalculationData", string.Format("|{0}|", fieldName), MatchMode.Start))
                    .List<OrmCalculatedField>();
                return (list.Count > 0 ? list[0] : null);
            }
        }

        private static OrmColumn LoadColumn(string tableName, string fieldName)
        {
            Guard.ArgumentNotNullOrEmptyString(tableName, "tableName");
            Guard.ArgumentNotNullOrEmptyString(fieldName, "fieldName");

            using (ISession session = new SessionScopeWrapper())
            {
                IList<OrmColumn> list = session.CreateCriteria(typeof (OrmColumn))
                    .Add(Expression.Eq("TableName", tableName))
                    .Add(Expression.Eq("FieldName", fieldName))
                    .List<OrmColumn>();
                return (list.Count > 0 ? list[0] : null);
            }
        }

        private static string GenerateLookupDef(OrmLookup lookup, bool usePhysicalName)
        {
            List<string> idParts = new List<string>();
            DataPath searchDataPath;

            try
            {
                searchDataPath = DataPath.Parse(lookup.SearchField);
            }
            catch (FormatException)
            {
                throw new MigrationException(string.Format("Unable to parse '{0}' lookup search field", lookup.SearchField));
            }

            if (searchDataPath != null)
            {
                string tableName = searchDataPath.RootTable;
                string rootTable = tableName;

                foreach (DataPathJoin join in searchDataPath.Joins)
                {
                    tableName = join.ToTable;
                    idParts.Add(usePhysicalName
                                    ? tableName
                                    : LookupTableDisplayName(tableName));
                }

                if (usePhysicalName)
                {
                    idParts.Add(searchDataPath.TargetField);
                }
                else
                {
                    idParts.Add(LookupFieldDisplayName(tableName, searchDataPath.TargetField));
                }

                string def = string.Format("{0}:{1}", rootTable, string.Join(".", idParts.ToArray()));

                if (!string.IsNullOrEmpty(lookup.LookupName))
                {
                    def += ":" + lookup.LookupName;
                }

                return def;
            }
            else
            {
                return null;
            }
        }

        private static string LookupTableDisplayName(string tableName)
        {
            Guard.ArgumentNotNullOrEmptyString(tableName, "tableName");
            OrmTable table = LoadTableByName(tableName);
            return (table != null && !string.IsNullOrEmpty(table.DisplayName)
                        ? table.DisplayName
                        : char.ToUpper(tableName[0]) + tableName.Substring(1).ToLower());
        }

        private static string LookupFieldDisplayName(string tableName, string fieldName)
        {
            Guard.ArgumentNotNullOrEmptyString(tableName, "tableName");
            Guard.ArgumentNotNullOrEmptyString(fieldName, "fieldName");

            if (fieldName.StartsWith("@"))
            {
                fieldName = fieldName.Substring(1);
                OrmCalculatedField calculatedField = LoadCalculatedField(tableName, fieldName);

                if (calculatedField != null && !string.IsNullOrEmpty(calculatedField.Name))
                {
                    return calculatedField.Name;
                }
            }
            else
            {
                OrmColumn column = LoadColumn(tableName, fieldName);

                if (column != null && !string.IsNullOrEmpty(column.DisplayName))
                {
                    return column.DisplayName;
                }
            }

            return char.ToUpper(fieldName[0]) + fieldName.Substring(1).ToLower();
        }

        private void LogWarning(string text, params object[] args)
        {
            if (_context != null && _context.Log != null)
            {
                _context.Log.Warn(text, args);
            }
        }
    }
}