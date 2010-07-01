using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Iesi.Collections.Generic;
using Sage.Platform.Application;
using Sage.Platform.Orm.CodeGen;
using Sage.Platform.Orm.Entities;
using Sage.Platform.Projects.Interfaces;
using Sage.Platform.TemplateSupport;
using Sage.Platform.TemplateSupport.NV;
using Sage.SalesLogix.Migration.Collections;
using Sage.SalesLogix.Migration.Services;

namespace Sage.SalesLogix.Migration.Module.Services
{
    public sealed class EntityService : IEntityService
    {
        private readonly StepHandler[] _steps;
        private MigrationContext _context;
        private MigrationWorkItem _workItem;
        private IProjectContextService _projectContext;
        private IOrmEntityLoaderService _entityLoader;
        private IEntityNameCreationService _entityNameCreator;

        public EntityService()
        {
            _steps = new StepHandler[]
                {
                    InternalPersistEntities,
                    PersistJoins,
                    PersistSecondaryJoins,
                    BuildInterfaces,
                };
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

        [ServiceDependency]
        public MigrationWorkItem WorkItem
        {
            set { _workItem = value; }
        }

        [ServiceDependency]
        public IProjectContextService ProjectContext
        {
            set { _projectContext = value; }
        }

        [ServiceDependency]
        public IOrmEntityLoaderService EntityLoader
        {
            set { _entityLoader = value; }
        }

        [ServiceDependency]
        public IEntityNameCreationService EntityNameCreator
        {
            set { _entityNameCreator = value; }
        }

        private OrmModel _ormModel;

        #region IEntityService Members

        public void PersistEntities()
        {
            _ormModel = _projectContext.ActiveProject.Models.Get<OrmModel>();

            using (_context.Status.BeginStep("Persisting entities...", _context.Tables.Count*2 + 1))
            {
                foreach (StepHandler step in _steps)
                {
                    step();

                    if (!_context.Status.Advance())
                    {
                        break;
                    }
                }
            }

            _ormModel = null;
        }

        #endregion

        private void InternalPersistEntities()
        {
            foreach (TableInfo table in _context.Tables.Values)
            {
                LogInfo("Resolving '{0}' table", table.Name);
                OrmEntity entity = _entityLoader.LoadEntity(table.Name);

                if (entity == null)
                {
                    entity = OrmEntity.CreateOrmEntity(_context.Package, table.Name);
                    table.Exists = (entity.Properties.Count > 0);

                    if (!table.Exists)
                    {
                        LogError("Table '{0}' not found", table.Name);
                        entity = null;
                    }
                    else
                    {
                        ISet<string> columns = new ComparisonSet<string>(table.Columns, StringComparer.InvariantCultureIgnoreCase);

                        foreach (OrmEntityProperty property in entity.Properties)
                        {
                            OrmFieldProperty fieldProperty = property as OrmFieldProperty;

                            if (fieldProperty != null)
                            {
                                string column = fieldProperty.ColumnName;

                                if (columns.Contains(column))
                                {
                                    columns.Remove(column);
                                }
                                else
                                {
                                    property.Include = false;
                                }
                            }
                        }

                        if (columns.Count > 0)
                        {
                            IList<OrmCalculatedField> calculatedFields = OrmCalculatedField.GetCalculatedFields(table.Name);

                            foreach (string column in columns)
                            {
                                if (column.StartsWith("@"))
                                {
                                    string name = column.Substring(1);

                                    foreach (OrmCalculatedField calculatedField in calculatedFields)
                                    {
                                        if (StringUtils.CaseInsensitiveEquals(calculatedField.FieldName, name))
                                        {
                                            OrmFieldProperty property = new OrmFieldProperty(entity);
                                            entity.Properties.Add(property);
                                            property.Copy(calculatedField.GetColumnInformation());
                                        }
                                    }
                                }
                                else
                                {
                                    LogError(entity, "Column '{0}' not found in table '{1}'", column, entity.TableName);
                                }
                            }
                        }

                        _context.Package.Entities.Add(entity);
                        _context.RequiresInterfaceBuild = true;
                        entity.Validate();
                        entity.Save();
                    }
                }

                if (entity != null)
                {
                    _context.Entities[table.Name] = entity;
                }

                if (!_context.Status.Advance())
                {
                    break;
                }
            }
        }

        private void PersistJoins()
        {
            foreach (TableInfo table in _context.Tables.Values)
            {
                LogInfo("Resolving '{0}' table relationships", table.Name);
                OrmEntity leftEntity;

                if (_context.Entities.TryGetValue(table.Name, out leftEntity))
                {
                    foreach (KeyValuePair<DataPathJoin, TableInfo> join in table.Joins)
                    {
                        if (!_context.Relationships.ContainsKey(join.Key))
                        {
                            OrmEntity rightEntity;

                            if (_context.Entities.TryGetValue(join.Value.Name, out rightEntity))
                            {
                                IList<OrmKeyColumn> fromKeyColumns = _entityLoader.LoadKeyColumns(join.Key.FromTable);
                                string leftKeyField;
                                bool isLeftPK;

                                if (fromKeyColumns.Count == 1)
                                {
                                    leftKeyField = fromKeyColumns[0].ColumnName;
                                    isLeftPK = StringUtils.CaseInsensitiveEquals(leftKeyField, join.Key.FromField);
                                }
                                else
                                {
                                    leftKeyField = null;
                                    isLeftPK = false;
                                }

                                IList<OrmKeyColumn> toKeyColumns = _entityLoader.LoadKeyColumns(join.Key.ToTable);
                                string rightKeyField;
                                bool isRightPK;

                                if (toKeyColumns.Count == 1)
                                {
                                    rightKeyField = toKeyColumns[0].ColumnName;
                                    isRightPK = StringUtils.CaseInsensitiveEquals(rightKeyField, join.Key.ToField);
                                }
                                else
                                {
                                    rightKeyField = null;
                                    isRightPK = false;
                                }

                                RelationshipInfo relationship;

                                if (!isLeftPK && !isRightPK)
                                {
                                    LogError("Neither field referenced by join '{0}' is a primary key", join.Key);
                                    continue;
                                }
                                else if (isLeftPK && isRightPK)
                                {
                                    relationship = CreateExtentedEntity(join.Key, leftEntity, rightEntity, leftKeyField, rightKeyField);
                                }
                                else
                                {
                                    relationship = CreateRelationship(join.Key, leftEntity, rightEntity, isRightPK);

                                    if (relationship != null)
                                    {
                                        relationship.Relationship.Validate();
                                        relationship.Relationship.Save();
                                    }
                                }

                                if (relationship != null)
                                {
                                    _context.Relationships[join.Key] = relationship;
                                }
                            }
                        }
                    }
                }

                if (!_context.Status.Advance())
                {
                    break;
                }
            }
        }

        private void PersistSecondaryJoins()
        {
            foreach (KeyValuePair<DataPathJoin, DataPathJoin> join in _context.SecondaryJoins)
            {
                Debug.Assert(join.Key.FromTable == join.Value.FromTable);
                Debug.Assert(join.Key.ToTable == join.Value.ToTable);
                string targetField = join.Value.FromField;

                if (targetField.StartsWith("@"))
                {
                    targetField = targetField.Substring(1);
                }

                OrmEntity localEntity;

                if (_context.Entities.TryGetValue(join.Key.FromTable, out localEntity))
                {
                    OrmFieldProperty localProperty = localEntity.Properties.GetFieldPropertyByFieldName(targetField);

                    if (localProperty != null)
                    {
                        RelationshipInfo relationship;

                        if (_context.Relationships.TryGetValue(join.Key, out relationship))
                        {
                            Debug.Assert(!relationship.IsOneToMany);
                            targetField = join.Value.ToField;

                            if (targetField.StartsWith("@"))
                            {
                                targetField = targetField.Substring(1);
                            }

                            OrmEntity foreignEntity;

                            if (_context.Entities.TryGetValue(join.Key.ToTable, out foreignEntity))
                            {
                                OrmFieldProperty foreignProperty = foreignEntity.Properties.GetFieldPropertyByFieldName(targetField);

                                AttachSnippet(
                                    localEntity.OnBeforeInsertMethod,
                                    "$targetInstance.$localProperty = ($targetInstance.$relationshipProperty != null ? $targetInstance.$relationshipProperty.$foreignProperty : null);",
                                    relationship.PropertyName,
                                    localProperty.PropertyName,
                                    foreignProperty.PropertyName);
                                AttachSnippet(
                                    localEntity.OnBeforeUpdateMethod,
                                    @"if (((IEntityState) $targetInstance).GetChanges().ContainsKey(""$relationshipProperty""))
            {
                $targetInstance.$localProperty = ($targetInstance.$relationshipProperty != null ? $targetInstance.$relationshipProperty.$foreignProperty : null);
            }",
                                    relationship.PropertyName,
                                    localProperty.PropertyName,
                                    foreignProperty.PropertyName,
                                    "Sage.SalesLogix.Orm");
                            }
                        }
                    }
                }
            }
        }

        private void BuildInterfaces()
        {
            if (_context.RequiresInterfaceBuild)
            {
                _workItem.BuildInterfaces();
                _context.RequiresInterfaceBuild = false;
            }
        }

        private RelationshipInfo CreateExtentedEntity(DataPathJoin join, OrmEntity leftEntity, OrmEntity rightEntity, string leftKeyField, string rightKeyField)
        {
            if (leftEntity.ExtendedEntity == rightEntity)
            {
                return new RelationshipInfo(rightEntity, leftEntity, true);
            }
            else if (rightEntity.ExtendedEntity == leftEntity)
            {
                return new RelationshipInfo(leftEntity, rightEntity, false);
            }
            else
            {
                int leftSimilarity = MeasureSimilarity(join.FromTable, leftKeyField);
                int rightSimilarity = MeasureSimilarity(join.ToTable, rightKeyField);

                if (leftSimilarity == rightSimilarity)
                {
                    LogError("Unable to establish owning table in join '{0}'", join);
                    return null;
                }

                RelationshipInfo relationship = (rightSimilarity > leftSimilarity
                                                     ? CreateExtentedEntity(rightEntity, leftEntity, true)
                                                     : CreateExtentedEntity(leftEntity, rightEntity, false));
                leftEntity.Validate();
                leftEntity.Save();
                rightEntity.Validate();
                rightEntity.Save();
                return relationship;
            }
        }

        private RelationshipInfo CreateExtentedEntity(OrmEntity parentEntity, OrmEntity childEntity, bool isInverted)
        {
            if (childEntity.ExtendedEntity != null && childEntity.ExtendedEntity != parentEntity)
            {
                LogError(childEntity, "Entity '{0}' already extends entity '{1}'", childEntity, childEntity.ExtendedEntity);
                return null;
            }

            Debug.Assert(parentEntity != childEntity);
            object dummy = parentEntity.ExtensionEntities;
            childEntity.ExtendedEntity = parentEntity;
            childEntity.IsExtension = true;
            return new RelationshipInfo(parentEntity, childEntity, isInverted);
        }

        private RelationshipInfo CreateRelationship(DataPathJoin join, OrmEntity leftEntity, OrmEntity rightEntity, bool isRightPK)
        {
            string targetField = join.FromField;

            if (targetField.StartsWith("@"))
            {
                targetField = targetField.Substring(1);
            }

            OrmFieldProperty leftProperty = leftEntity.Properties.GetFieldPropertyByFieldName(targetField);

            if (leftProperty == null)
            {
                LogError(leftEntity, "Property based on field '{0}' not found on entity based on table '{1}'", join.FromField, leftEntity.TableName);
                return null;
            }

            targetField = join.ToField;

            if (targetField.StartsWith("@"))
            {
                targetField = targetField.Substring(1);
            }

            OrmFieldProperty rightProperty = rightEntity.Properties.GetFieldPropertyByFieldName(targetField);

            if (rightProperty == null)
            {
                LogError(rightEntity, "Property based on field '{0}' not found on entity based on table '{1}'", join.ToField, rightEntity.TableName);
                return null;
            }

            OrmRelationship ormRelationship = CollectionUtils.Find(
                _ormModel.Relationships,
                delegate(OrmRelationship item)
                    {
                        if (item.Columns.Count > 0)
                        {
                            OrmRelationshipColumn column = item.Columns[0];
                            return ((item.ParentEntity == leftEntity && item.ChildEntity == rightEntity &&
                                     column.ParentProperty == leftProperty && column.ChildProperty == rightProperty &&
                                     (item.Cardinality == OrmRelationship.ManyToOne) == isRightPK &&
                                     item.ParentProperty.Include) ||
                                    (item.ParentEntity == rightEntity && item.ChildEntity == leftEntity &&
                                     column.ParentProperty == rightProperty && column.ChildProperty == leftProperty &&
                                     (item.Cardinality == OrmRelationship.OneToMany) == isRightPK &&
                                     item.ChildProperty.Include));
                        }

                        return false;
                    });

            bool isInverted;

            if (ormRelationship == null)
            {
                bool isLeftDynamic = leftEntity.Package.GetGenerateAssembly();

                if (!isLeftDynamic)
                {
                    LogError("Cannot create the necessary relationship property for '{0}' join", join);
                    return null;
                }

                string cardinality;
                string parentPropertyName;

                if (isRightPK)
                {
                    cardinality = OrmRelationship.ManyToOne;
                    parentPropertyName = GenerateReferencePropertyName(rightEntity, leftEntity, leftProperty);
                }
                else
                {
                    cardinality = OrmRelationship.OneToMany;
                    parentPropertyName = GenerateCollectionPropertyName(leftEntity, rightEntity);
                }

                ormRelationship = new OrmRelationship(
                    leftEntity,
                    rightEntity,
                    cardinality,
                    leftProperty,
                    rightProperty,
                    CascadeOption.SaveUpdate);
                ormRelationship.ParentProperty.PropertyName = parentPropertyName;
                leftEntity.ChildEntities.Add(ormRelationship);
                rightEntity.ParentEntities.Add(ormRelationship);
                ormRelationship.ChildProperty.Include = false;
                isInverted = false;
            }
            else
            {
                isInverted = (ormRelationship.ParentEntity == rightEntity);
            }

            return new RelationshipInfo(ormRelationship, isInverted);
        }

        private string GenerateReferencePropertyName(OrmEntity parentEntity, OrmEntity childEntity, OrmEntityProperty childProperty)
        {
            string propertyName = childProperty.PropertyName;

            if (propertyName.EndsWith("ID", StringComparison.InvariantCultureIgnoreCase))
            {
                propertyName = propertyName.Substring(0, propertyName.Length - 2).Trim();
            }
            else if (propertyName.EndsWith("CODE", StringComparison.InvariantCultureIgnoreCase))
            {
                propertyName = propertyName.Substring(0, propertyName.Length - 4).Trim();
            }
            else
            {
                propertyName = parentEntity.Name;
            }

            if (StringUtils.CaseInsensitiveEquals(propertyName, parentEntity.Name))
            {
                propertyName = parentEntity.Name;
            }

            return _entityNameCreator.CreateName(childEntity, typeof (OrmEntityProperty), propertyName);
        }

        private string GenerateCollectionPropertyName(OrmEntity parentEntity, OrmEntity childEntity)
        {
            string baseName = childEntity.Name;
            char lastChar = baseName[baseName.Length - 1];

            if (lastChar == 'y')
            {
                baseName = baseName.Substring(0, baseName.Length - 1) + "ies";
            }
            else if (lastChar != 's')
            {
                baseName += 's';
            }

            return _entityNameCreator.CreateName(parentEntity, typeof (OrmEntityProperty), baseName);
        }

        private static void AttachSnippet(
            OrmEntityMethod entityMethod,
            string methodBodyTemplate,
            string relationshipProperty,
            string localProperty,
            string foreignProperty,
            params string[] usings)
        {
            string methodName = string.Format("{0}_Set{1}", entityMethod.MethodName, localProperty);
            OrmMethodTargetSnippet snippet = (OrmMethodTargetSnippet) CollectionUtils.Find(
                                                                          entityMethod.PostExecuteTargets,
                                                                          delegate(OrmMethodTarget item)
                                                                              {
                                                                                  return (item is OrmMethodTargetSnippet &&
                                                                                          StringUtils.CaseInsensitiveEquals(item.TargetMethod, methodName));
                                                                              });

            if (snippet == null)
            {
                snippet = new OrmMethodTargetSnippet(entityMethod, MethodTargetType.PostExecute);
                entityMethod.PostExecuteTargets.Add(snippet);
            }

            string namespaceName = CodeSnippetManager.GetConfiguration().DefaultNamespace;
            string className = entityMethod.Entity.Name + "BusinessRules";
            CodeSnippetTemplateContext context = new CodeSnippetTemplateContext(
                namespaceName,
                className,
                methodName,
                "void");

            foreach (string use in usings)
            {
                context.Usings.Add(use);
            }

            context.Usings.Add("NHibernate");
            string targetInstance = entityMethod.Entity.Name.ToLowerInvariant();
            context.AddParameter(entityMethod.Entity.InterfaceName, targetInstance);
            context.AddParameter("ISession", "session");

            if (!(snippet.CodeSnippet is CodeSnippetCSharp))
            {
                if (snippet.CodeSnippet != null)
                {
                    snippet.CodeSnippet.Delete();
                }

                snippet.CodeSnippet = new CodeSnippetCSharp(entityMethod, context);
            }

            snippet.CodeSnippet.Header.Name = methodName;
            snippet.CodeSnippet.Header.AssemblyReferences.Add(
                new AssemblyReference(
                    "Sage.Entity.Interfaces.dll",
                    Path.Combine("%BASEBUILDPATH%", @"interfaces\bin\Sage.Entity.Interfaces.dll")));
            snippet.CodeSnippet.Header.AssemblyReferences.Add(
                new AssemblyReference(
                    "Sage.Platform.dll",
                    Path.Combine("%BASEBUILDPATH%", @"assemblies\Sage.Platform.dll")));
            snippet.TargetMethod = methodName;
            snippet.TargetType = string.Format("{0}.{1}, {2}", namespaceName, className, snippet.CodeSnippet.Header.Assembly);

            string code = snippet.CodeSnippet.Code;
            int pos = code.IndexOf(methodName);

            if (pos >= 0)
            {
                pos = code.IndexOf("// TODO: ", pos);

                if (pos >= 0)
                {
                    int pos2 = code.IndexOfAny(new char[] {'\r', '\n'}, pos);

                    if (pos2 >= 0)
                    {
                        Hashtable contexts = new Hashtable(4);
                        contexts.Add("targetInstance", targetInstance);
                        contexts.Add("relationshipProperty", relationshipProperty);
                        contexts.Add("localProperty", localProperty);
                        contexts.Add("foreignProperty", foreignProperty);
                        ITemplateEngine engine = new NVelocityTemplateEngine();
                        ITemplate template = engine.CreateTemplate(methodBodyTemplate, contexts);
                        snippet.CodeSnippet.Code = code.Replace(
                            code.Substring(pos, pos2 - pos),
                            template.ApplyTemplate());
                    }
                }
            }

            entityMethod.Validate();
            entityMethod.Save();
        }

        private static int MeasureSimilarity(string tableName, string keyField)
        {
            if (keyField.EndsWith("id", StringComparison.InvariantCultureIgnoreCase))
            {
                keyField = keyField.Substring(0, keyField.Length - 2);

                if (StringUtils.CaseInsensitiveEquals(tableName, keyField))
                {
                    return 4;
                }

                if (tableName.StartsWith(keyField, StringComparison.InvariantCultureIgnoreCase))
                {
                    return 3;
                }

                if (tableName.EndsWith(keyField, StringComparison.InvariantCultureIgnoreCase))
                {
                    return 2;
                }

                if (tableName.IndexOf(keyField, StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    return 1;
                }
            }

            return 0;
        }

        private void LogInfo(string text, params object[] args)
        {
            if (_context != null && _context.Log != null)
            {
                _context.Log.Info(text, args);
            }
        }

        private void LogError(string text, params object[] args)
        {
            if (_context != null && _context.Log != null)
            {
                _context.Log.Error(text, args);
            }
        }

        private void LogError(IModelItem item, string text, params object[] args)
        {
            if (_context != null && _context.Log != null)
            {
                _context.Log.Error(item, text, args);
            }
        }
    }
}