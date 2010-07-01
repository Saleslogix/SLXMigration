using System;
using System.Collections.Generic;
using Sage.Platform.Application;
using Sage.Platform.BundleModel.Actions;
using Sage.Platform.Orm.Entities;
using Sage.Platform.Projects;
using Sage.Platform.Projects.Interfaces;
using Sage.Platform.QuickForms;
using Sage.Platform.WebPortal.Design;
using Sage.SalesLogix.BundleModel.BundleActions;
using Sage.SalesLogix.Migration.Services;
using Sage.SalesLogix.SchemaSupport;
using _BundleModel=Sage.Platform.BundleModel.BundleModel;

namespace Sage.SalesLogix.Migration.Module.Services
{
    public sealed class ManifestService : IManifestService
    {
        private readonly StepHandler[] _steps;
        private IProjectContextService _projectContext;
        private MigrationContext _context;

        public ManifestService()
        {
            _steps = new StepHandler[]
                {
                    Initialize,
                    AddTables,
                    AddPackage,
                    AddEntities,
                    AddForms,
                    AddRelationships,
                    AddPortal,
                    AddLinkedFiles,
                    AddSmartParts,
                    AddNavigation,
                    PersistManifest
                };
        }

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

        private IBundleSupport _ormSupport;
        private IBundleSupport _quickFormSupport;
        private IBundleSupport _portalSupport;

        #region IManifestService Members

        public void Generate()
        {
            int totalSteps = _steps.Length
                             + _context.Tables.Count
                             + _context.Entities.Count
                             + _context.Forms.Count
                             + _context.Relationships.Count
                             + _context.LinkedFiles.Count
                             + _context.SmartParts.Count;

            using (_context.Status.BeginStep("Generating manifest...", totalSteps))
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

            _ormSupport = null;
            _quickFormSupport = null;
            _portalSupport = null;
        }

        #endregion

        private void Initialize()
        {
            IModelCollection models = _projectContext.ActiveProject.Models;
            _ormSupport = models.Get<OrmModel>().GetModelService<IBundleSupport>();
            _quickFormSupport = models.Get<QuickFormModel>().GetModelService<IBundleSupport>();
            _portalSupport = models.Get<PortalModel>().GetModelService<IBundleSupport>();
        }

        private void AddTables()
        {
            foreach (TableInfo table in _context.Tables.Values)
            {
                if (table.Exists)
                {
                    LogInfo("Bundling '{0}' table", table.Name);
                    AddManifestTableAction(table.Name);
                }

                if (!_context.Status.Advance())
                {
                    break;
                }
            }
        }

        private void AddPackage()
        {
            if (_context.IsNewPackage)
            {
                LogInfo("Bundling '{0}' package", _context.Package);
                AddManifestItem(_ormSupport, _context.Package);
            }
        }

        private void AddEntities()
        {
            foreach (OrmEntity entity in _context.Entities.Values)
            {
                if (!(entity.Package == _context.Package && _context.IsNewPortal))
                {
                    LogInfo("Bundling '{0}' entity", entity);
                    AddManifestItem(_ormSupport, entity);
                }

                if (!_context.Status.Advance())
                {
                    break;
                }
            }
        }

        private void AddForms()
        {
            foreach (FormInfo form in _context.Forms.Values)
            {
                if (!(form.Entity.Package == _context.Package && _context.IsNewPortal))
                {
                    LogInfo("Bundling '{0}' form", form);
                    AddManifestItem(_quickFormSupport, form.QuickForm);
                }

                if (!_context.Status.Advance())
                {
                    break;
                }
            }
        }

        private void AddRelationships()
        {
            foreach (RelationshipInfo relationship in _context.Relationships.Values)
            {
                if (relationship.Relationship != null)
                {
                    LogInfo("Bundling '{0}' relationship", relationship);
                    AddManifestItem(_ormSupport, relationship.Relationship);
                }

                if (!_context.Status.Advance())
                {
                    break;
                }
            }
        }

        private void AddPortal()
        {
            if (_context.IsNewPortal)
            {
                LogInfo("Bundling '{0}' portal", _context.Portal);
                AddManifestItem(_portalSupport, _context.Portal);
            }
        }

        private void AddLinkedFiles()
        {
            foreach (LinkedFile linkedFile in _context.LinkedFiles)
            {
                if (!_context.IsNewPortal)
                {
                    LogInfo("Bundling '{0}' linked file", linkedFile.ProjectPath);
                    AddManifestItem(_portalSupport, linkedFile);
                }

                if (!_context.Status.Advance())
                {
                    break;
                }
            }
        }

        private void AddSmartParts()
        {
            foreach (SmartPartMapping smartPart in _context.SmartParts)
            {
                if (!_context.IsNewPortal)
                {
                    LogInfo("Bundling '{0}' smart part mapping", smartPart);
                    AddManifestItem(_portalSupport, smartPart);
                }

                if (!_context.Status.Advance())
                {
                    break;
                }
            }
        }

        private void AddNavigation()
        {
            foreach (NavigationInfo navigation in _context.Navigation)
            {
                if (!_context.IsNewPortal)
                {
                    LogInfo("Bundling '{0}' navigation", navigation);
                    AddManifestItem(_portalSupport, navigation.Item);
                }

                if (!_context.Status.Advance())
                {
                    break;
                }
            }
        }

        private void PersistManifest()
        {
            _context.Manifest.Validate();
            _context.Manifest.Save();
        }

        private void AddManifestTableAction(string tableName)
        {
            Actions actions = new Actions();
            CreateTableAction tableAction = new CreateTableAction();
            tableAction.TableName = tableName;
            tableAction.Caption = "Create Table " + tableName;

            foreach (ISlxDataColumn column in actions.GetDataColumns(tableAction.TableName))
            {
                CreateFieldAction fieldAction = new CreateFieldAction();
                fieldAction.TableName = column.TableName;
                fieldAction.FieldName = column.ColumnName;
                fieldAction.Caption = string.Format("Create Field {0}.{1}", column.TableName, column.ColumnName);
                fieldAction.InstallOptions = BundleItemOptions.SkipItemAndChildren;
                fieldAction.Parent = tableAction;
                tableAction.Children.Add(fieldAction);
            }

            foreach (ISlxIndex index in actions.GetIndices(tableAction.TableName))
            {
                CreateIndexAction indexAction = new CreateIndexAction();
                indexAction.TableName = tableAction.TableName;
                indexAction.IndexName = index.IndexName;
                indexAction.Caption = string.Format("Create Index {0}.{1}", tableAction.TableName, index.IndexName);
                indexAction.InstallOptions = BundleItemOptions.SkipItemAndChildren;
                indexAction.Parent = tableAction;
                tableAction.Children.Add(indexAction);
            }

            AddManifestAction(tableAction);
        }

        private void AddManifestAction(IBundleAction action)
        {
            if (!_context.Manifest.ContainsItem(action.ItemId))
            {
                IBundleAction parent = null;

                while (parent == null)
                {
                    IBundleAction contextItem = action.GetContextAction();

                    if (contextItem != null)
                    {
                        if (_context.Manifest.ContainsItem(contextItem.ItemId))
                        {
                            parent = (IBundleAction) _context.Manifest.GetItem(contextItem.ItemId);
                        }
                        else
                        {
                            contextItem.Children.Add(action);
                            action.Parent = contextItem;
                            action = contextItem;
                        }
                    }
                    else
                    {
                        parent = _context.Manifest.ActionHierarchy;
                    }
                }

                _context.Manifest.AddActionToHierarchy(action, parent);
            }
        }

        private void AddManifestItem(IBundleSupport support, object data)
        {
            IBundleItem item = support.GetBundleItem(data);

            if (!_context.Manifest.ContainsItem(item.ItemId))
            {
                IBundleItem parent = null;

                while (parent == null)
                {
                    IBundleItem contextItem = support.GetContextItem(item);

                    if (contextItem != null)
                    {
                        if (_context.Manifest.ContainsItem(contextItem.ItemId))
                        {
                            parent = _context.Manifest.GetItem(contextItem.ItemId);
                        }
                        else
                        {
                            if (contextItem.Model != item.Model)
                            {
                                support = contextItem.Model.GetModelService<IBundleSupport>();
                            }

                            contextItem.Children.Add(item);
                            item.Parent = contextItem;
                            item = contextItem;
                        }
                    }
                    else
                    {
                        parent = _context.Manifest.ItemHierarchy;
                    }
                }

                _context.Manifest.AddItemToHierarchy(item, parent);
            }
        }

        private void LogInfo(string text, params object[] args)
        {
            if (_context != null && _context.Log != null)
            {
                _context.Log.Info(text, args);
            }
        }
    }
}