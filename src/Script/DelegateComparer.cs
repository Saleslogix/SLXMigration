using System;
using System.Collections.Generic;
using Sage.Platform.Exceptions;

namespace Sage.SalesLogix.Migration.Script
{
    public sealed class DelegateComparer<T> : IComparer<T>
    {
        private readonly Comparison<T> _comparison;

        public DelegateComparer(Comparison<T> comparison)
        {
            Guard.ArgumentNotNull(comparison, "comparison");
            _comparison = comparison;
        }

        #region IComparer<T> Members

        public int Compare(T x, T y)
        {
            return _comparison(x, y);
        }

        #endregion
    }
}