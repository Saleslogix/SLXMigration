using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sage.SalesLogix.Migration
{
    public static class CollectionUtils
    {
        public static T Find<T>(IEnumerable<T> collection, Predicate<T> match)
        {
            Debug.Assert(!(collection is Array));
            List<T> list = collection as List<T>;

            if (list != null)
            {
                return list.Find(match);
            }
            else
            {
                foreach (T item in collection)
                {
                    if (match(item))
                    {
                        return item;
                    }
                }

                return default(T);
            }
        }

        public static bool Contains<T>(IEnumerable<T> collection, Predicate<T> match)
        {
            return !Equals(Find(collection, match), default(T));
        }
    }
}