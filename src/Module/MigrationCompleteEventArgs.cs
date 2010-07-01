using System;

namespace Sage.SalesLogix.Migration.Module
{
    public sealed class MigrationCompleteEventArgs : EventArgs
    {
        private readonly MigrationReport _report;

        public MigrationCompleteEventArgs(MigrationReport report)
        {
            _report = report;
        }

        public MigrationReport Report
        {
            get { return _report; }
        }
    }
}