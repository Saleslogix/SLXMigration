using System;
using Sage.SalesLogix.Migration.Services;

namespace Sage.SalesLogix.Migration.Module.Services
{
    public sealed class MigrationContextHolderService : IMigrationContextHolderService
    {
        private MigrationContext _context;

        #region IMigrationContextHolderService Members

        public MigrationContext Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;

                    if (ContextChanged != null)
                    {
                        ContextChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public event EventHandler ContextChanged;

        #endregion
    }
}