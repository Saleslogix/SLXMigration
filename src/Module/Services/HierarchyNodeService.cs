using System;
using System.Collections.Generic;
using System.Reflection;
using Sage.Platform.AdminModule.EntityModel;
using Sage.Platform.Application;
using Sage.Platform.BundleModel;
using Sage.Platform.Orm.Entities;
using Sage.Platform.Projects;
using Sage.Platform.Projects.Interfaces;
using Sage.Platform.QuickForms;
using Sage.Platform.WebPortal.Design;
using Sage.Platform.WebPortal.Design.Hierarchy;
using Sage.SalesLogix.Migration.Services;
using _BundleModel=Sage.Platform.BundleModel.BundleModel;

namespace Sage.SalesLogix.Migration.Module.Services
{
    public sealed class HierarchyNodeService : IHierarchyNodeService
    {
        private static readonly IDictionary<Type, FieldInfo> _childrenFields = new Dictionary<Type, FieldInfo>();

        private IProjectContextService _projectContext;

        [ServiceDependency]
        public IProjectContextService ProjectContext
        {
            set { _projectContext = value; }
        }

        #region IHierarchyNodeService Members

        public void InsertPortalNode(PortalApplication portal)
        {
            IHierarchyNode node;

            if (LookupByType<PortalModel, ManagerNode>(_projectContext.ActiveProjectNode.Children, out node))
            {
                node.Children.Add(new ApplicationNode(portal, node));
            }
        }

        public void InsertBundleManifestNode(BundleManifest manifest)
        {
            IHierarchyNode node;

            if (LookupByType<_BundleModel, BundleModelNode>(_projectContext.ActiveProjectNode.Children, out node))
            {
                node.Children.Add(new BundleManifestNode(manifest, node));
            }
        }

        public void InsertQuickFormNode(OrmEntity entity, IQuickFormDefinition form)
        {
            IHierarchyNode entityModelNode;
            IHierarchyNode packagesNode;
            IHierarchyNode packageNode;
            IHierarchyNode entityNode;
            IHierarchyNode quickFormsNode;

            if (LookupByKey<OrmModel>(_projectContext.ActiveProjectNode.Children, "entitymodel", out entityModelNode) &&
                LookupByKey<OrmModel>(entityModelNode.Children, "packages", out packagesNode) &&
                LookupByModelItem<OrmPackage>(packagesNode.Children, entity.Package, out packageNode) &&
                LookupByModelItem<OrmEntity>(packageNode.Children, entity, out entityNode) &&
                LookupByKey<OrmEntity>(entityNode.Children, "quickforms", out quickFormsNode))
            {
                quickFormsNode.Children.Add(
                    new QuickFormDefinitionNode(
                        form,
                        quickFormsNode));
            }
        }

        #endregion

        private static bool LookupByPredicate<TModelItem>(IEnumerable<IHierarchyNode> nodes, Predicate<IHierarchyNode> match, out IHierarchyNode node)
        {
            node = CollectionUtils.Find(nodes, match);
            Type nodeType = typeof (HierarchyNodeBase<TModelItem>);
            FieldInfo field;

            if (!_childrenFields.TryGetValue(nodeType, out field))
            {
                field = nodeType.GetField(
                    "_children",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _childrenFields.Add(nodeType, field);
            }

            return (field == null || field.GetValue(node) != null);
        }

        private static bool LookupByType<TModel, TNode>(IEnumerable<IHierarchyNode> nodes, out IHierarchyNode node)
        {
            return LookupByPredicate<TModel>(
                nodes,
                delegate(IHierarchyNode item)
                    {
                        return (item is TNode);
                    },
                out node);
        }

        private static bool LookupByKey<TModel>(IEnumerable<IHierarchyNode> nodes, string nodeKey, out IHierarchyNode node)
        {
            return LookupByPredicate<TModel>(
                nodes,
                delegate(IHierarchyNode item)
                    {
                        return (item.NodeKey == nodeKey);
                    },
                out node);
        }

        private static bool LookupByModelItem<TModel>(IEnumerable<IHierarchyNode> nodes, IModelItem modelItem, out IHierarchyNode node)
        {
            return LookupByPredicate<TModel>(
                nodes,
                delegate(IHierarchyNode item)
                    {
                        return (item.ModelItem == modelItem);
                    },
                out node);
        }
    }
}