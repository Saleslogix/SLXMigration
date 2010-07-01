using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sage.SalesLogix.Migration.Script.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    public sealed class WeakKeyValueDictionary<TKey, TValue> : BaseWeakDictionary<TKey, TValue, EqualityWeakReference<TKey>, WeakReference>
    {
        protected override bool IsCollected(KeyValuePair<EqualityWeakReference<TKey>, WeakReference> item)
        {
            return !item.Key.IsAlive || !item.Value.IsAlive;
        }

        public override ICollection<TKey> Keys
        {
            get
            {
                ICollection<TKey> keys = new List<TKey>();

                foreach (EqualityWeakReference<TKey> key in InnerDictionary.Keys)
                {
                    keys.Add(DecodeKey(key));
                }

                return keys;
            }
        }

        public override ICollection<TValue> Values
        {
            get
            {
                ICollection<TValue> values = new List<TValue>();

                foreach (WeakReference value in InnerDictionary.Values)
                {
                    values.Add(DecodeValue(value));
                }

                return values;
            }
        }

        protected override EqualityWeakReference<TKey> EncodeKey(TKey key)
        {
            return new EqualityWeakReference<TKey>(key);
        }

        protected override TKey DecodeKey(EqualityWeakReference<TKey> key)
        {
            return key.Target;
        }

        protected override WeakReference EncodeValue(TValue value)
        {
            return new WeakReference((object) value ?? typeof (NullObject));
        }

        protected override TValue DecodeValue(WeakReference value)
        {
            return (value.Target == typeof (NullObject) ? default(TValue) : (TValue) value.Target);
        }

        private sealed class NullObject {}
    }
}