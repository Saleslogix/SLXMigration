using Sage.Platform.BundleModel;
using Sage.Platform.Orm.Entities;
using Sage.Platform.QuickForms;
using Sage.Platform.WebPortal.Design;

namespace Sage.SalesLogix.Migration.Services
{
    public interface IHierarchyNodeService
    {
        void InsertPortalNode(PortalApplication portal);
        void InsertBundleManifestNode(BundleManifest manifest);
        void InsertQuickFormNode(OrmEntity entity, IQuickFormDefinition form);
    }
}