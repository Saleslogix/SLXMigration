using System.Collections.Generic;
using System.Diagnostics;
using Sage.Platform.Application;
using Sage.Platform.Orm.Entities;
using Sage.SalesLogix.Migration.Services;

namespace Sage.SalesLogix.Migration.Forms.Services
{
    public sealed class DataPathTranslationService : IDataPathTranslationService
    {
        private MigrationContext _context;

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

        #region IDataPathTranslationService Members

        public void RegisterTable(string tableName)
        {
            InternalRegisterTable(tableName);
        }

        public void RegisterField(DataPath dataPath)
        {
            InternalRegisterField(dataPath);
        }

        public void RegisterJoin(DataPath leftPath, DataPath rightPath)
        {
            TableInfo leftTable = InternalRegisterField(leftPath);
            TableInfo rightTable = InternalRegisterField(rightPath);
            DataPathJoin join = new DataPathJoin(leftPath.TargetTable, leftPath.TargetField, rightPath.TargetTable, rightPath.TargetField);
            leftTable.Joins[join] = rightTable;
        }

        public string TranslateTable(string tableName)
        {
            Guard.ArgumentNotNull(tableName, "tableName");
            OrmEntity entity;

            if (!_context.Entities.TryGetValue(tableName, out entity))
            {
                throw new MigrationException(string.Format("Unable to resolve the entity for '{0}' table", tableName));
            }

            return entity.Name;
        }

        public string TranslateField(DataPath dataPath)
        {
            Guard.ArgumentNotNull(dataPath, "dataPath");
            string tableName = dataPath.RootTable;
            List<string> parts = new List<string>();

            foreach (DataPathJoin join in dataPath.Joins)
            {
                RelationshipInfo relationship;

                if (!_context.Relationships.TryGetValue(join, out relationship))
                {
                    throw new MigrationException(string.Format("Unable to find a relationship based on the '{0}' join string", join));
                }

                if (relationship.Relationship != null && relationship.IsOneToMany)
                {
                    throw new MigrationException(string.Format("Invalid join direction in '{0}' join string", join));
                }

                parts.Add(relationship.PropertyName);
                tableName = join.ToTable;
            }

            OrmEntity entity;

            if (!_context.Entities.TryGetValue(tableName, out entity))
            {
                throw new MigrationException(string.Format("Unable to resolve the entity for '{0}' table", tableName));
            }

            string targetField = dataPath.TargetField;

            if (targetField.StartsWith("@"))
            {
                targetField = targetField.Substring(1);
            }

            OrmEntityProperty property = entity.Properties.GetFieldPropertyByFieldName(targetField);

            if (property == null)
            {
                throw new MigrationException(string.Format("Unable to resolve property for field '{0}' in '{1}' entity", targetField, entity.Name));
            }

            string propertyName = (property == entity.KeyProperty
                                       ? "Id"
                                       : property.PropertyName);
            parts.Add(propertyName);
            return string.Join(".", parts.ToArray());
        }

        public string TranslateField(DataPath dataPath, DataPath prefixPath)
        {
            Guard.ArgumentNotNull(dataPath, "dataPath");

            if (prefixPath != null && prefixPath.Joins.Count > 0)
            {
                Debug.Assert(prefixPath.TargetTable == dataPath.RootTable);
                DataPath newDataPath = new DataPath(prefixPath.RootTable, dataPath.TargetField);

                foreach (DataPathJoin join in prefixPath.Joins)
                {
                    newDataPath.Joins.Add(join);
                }

                foreach (DataPathJoin join in dataPath.Joins)
                {
                    newDataPath.Joins.Add(join);
                }

                dataPath = newDataPath;
            }

            return TranslateField(dataPath);
        }

        public string TranslateReference(DataPath dataPath, string targetTable, string targetField)
        {
            return InternalTranslate(dataPath, targetTable, targetField, false);
        }

        public string TranslateCollection(DataPath dataPath, string targetTable, string targetField)
        {
            return InternalTranslate(dataPath, targetTable, targetField, true);
        }

        #endregion

        private TableInfo InternalRegisterTable(string tableName)
        {
            Guard.ArgumentNotNull(tableName, "tableName");
            TableInfo table;

            if (!_context.Tables.TryGetValue(tableName, out table))
            {
                table = new TableInfo(tableName);
                _context.Tables.Add(tableName, table);
            }

            return table;
        }

        private TableInfo InternalRegisterField(DataPath dataPath)
        {
            Guard.ArgumentNotNull(dataPath, "dataPath");
            TableInfo table = InternalRegisterTable(dataPath.RootTable);

            foreach (DataPathJoin join in dataPath.Joins)
            {
                table.Columns.Add(join.FromField);
                TableInfo nextTable;

                if (!table.Joins.TryGetValue(join, out nextTable))
                {
                    nextTable = InternalRegisterTable(join.ToTable);
                    table.Joins.Add(join, nextTable);
                }

                table = nextTable;
                table.Columns.Add(join.ToField);
            }

            table.Columns.Add(dataPath.TargetField);
            return table;
        }

        private string InternalTranslate(DataPath dataPath, string targetTable, string targetField, bool isOneToMany)
        {
            Guard.ArgumentNotNull(dataPath, "dataPath");
            Guard.ArgumentNotNull(targetTable, "targetTable");
            Guard.ArgumentNotNull(targetField, "targetField");
            int joinCount = dataPath.Joins.Count;
            List<string> parts = new List<string>();
            RelationshipInfo relationship;

            for (int i = 0; i < joinCount; i++)
            {
                DataPathJoin join = dataPath.Joins[i];

                if (!_context.Relationships.TryGetValue(join, out relationship))
                {
                    throw new MigrationException(string.Format("Unable to find a relationship based on the '{0}' join string", join));
                }

                if (!(i == joinCount - 1 &&
                      StringUtils.CaseInsensitiveEquals(join.ToTable, targetTable) &&
                      StringUtils.CaseInsensitiveEquals(join.ToField, targetField)))
                {
                    Debug.Assert(!relationship.IsOneToMany);
                    parts.Add(relationship.PropertyName);
                }
            }

            DataPathJoin targetJoin = new DataPathJoin(dataPath.TargetTable, dataPath.TargetField, targetTable, targetField);

            if (!_context.Relationships.TryGetValue(targetJoin, out relationship))
            {
                throw new MigrationException(string.Format("Unable to find a relationship based on the '{0}' join string", targetJoin));
            }

            Debug.Assert(relationship.IsOneToMany == isOneToMany);
            parts.Add(relationship.PropertyName);
            return string.Join(".", parts.ToArray());
        }
    }
}