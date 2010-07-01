using Sage.Platform.Application;
using Sage.Platform.Orm.Entities;

namespace Sage.SalesLogix.Migration.Module
{
    [WinFormsSmartPart(typeof (MigrationReportEditor), "ReportAdapter")]
    public sealed class MigrationReportPersistentAdapter : PersistentObjectAdapterBase
    {
        private readonly MigrationReport _report;

        private MigrationReportPersistentAdapter(MigrationReport report)
            : base(report)
        {
            _report = report;
        }

        public MigrationReport Report
        {
            get { return _report; }
        }

        public static IPersistentObject GetPersistentObject(MigrationReport report)
        {
            Guard.ArgumentNotNull(report, "report");
            PersistentObjectAdapterBase adapter;

            if (!_cache.TryGetValue(report, out adapter))
            {
                adapter = new MigrationReportPersistentAdapter(report);
            }

            return adapter;
        }
    }
}