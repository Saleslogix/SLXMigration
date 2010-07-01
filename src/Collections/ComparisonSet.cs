using System.Collections.Generic;
using System.Diagnostics;
using Iesi.Collections.Generic;

namespace Sage.SalesLogix.Migration.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    public sealed class ComparisonSet<T> : DictionarySet<T>
    {
        public ComparisonSet(IEqualityComparer<T> comparer)
        {
            InternalDictionary = new Dictionary<T, object>(comparer);
        }

        public ComparisonSet(ICollection<T> initialValues, IEqualityComparer<T> comparer)
            : this(comparer)
        {
            AddAll(initialValues);
        }
    }
}