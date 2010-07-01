using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Sage.Platform.Application;
using Sage.Platform.FileSystem;
using Sage.Platform.FileSystem.Interfaces;
using Sage.Platform.Projects;
using Sage.Platform.Projects.Collections;
using Sage.Platform.Projects.Interfaces;

namespace Sage.SalesLogix.Migration.Module
{
    [Guid("F89DDE8E-BE0B-42ac-AD34-55B328BFF1CF")]
    public sealed class MigrationModel : ModelBase
    {
        public const string ModelRootPath = @"\Migration Reports";
        public const string ReportExtension = ".report.xml";
        private readonly IDirectoryInfo _modelRoot;
        private readonly IModelItemInfo _reportInfo;
        private BindingList<MigrationReport> _reports;

        public MigrationModel(IProject project)
            : base(project)
        {
            Guard.ArgumentNotNull(project, "project");

            _reportInfo = new ModelItemInfo(this, typeof (MigrationReport));
            _modelRoot = Project.Drive.GetDirectoryInfo(ModelRootPath);

            if (!_modelRoot.Exists)
            {
                _modelRoot.Create();
            }
        }

        public IList<MigrationReport> Reports
        {
            get
            {
                if (_reports == null)
                {
                    FileQuery qry = new FileQuery(_modelRoot, "*" + ReportExtension, 1);
                    _reports = PersistentFileCollection<MigrationReport>.Load(false, this, qry);
                }

                return _reports;
            }
        }

        public void AddReport(MigrationReport report)
        {
            report.Model = this;

            if (_reports != null)
            {
                _reports.Add(report);
            }
        }

        public void RemoveReport(MigrationReport report)
        {
            report.Model = null;

            if (_reports != null)
            {
                _reports.Remove(report);
            }
        }

        #region ModelBase Members

        public override IModelItemInfo GetModelItemInfo(string url)
        {
            return (url.EndsWith(ReportExtension) ? _reportInfo : null);
        }

        public override IPersistentObject GetAsPersistentObject(IModelItem item)
        {
            return MigrationReportPersistentAdapter.GetPersistentObject((MigrationReport) item);
        }

        #endregion
    }
}