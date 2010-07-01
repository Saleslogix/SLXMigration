using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sage.SalesLogix.Migration.Script.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    public sealed class WeakValueDictionary<TKey, TValue> : BaseWeakDictionary<TKey, TValue, TKey, WeakReference>
    {
        protected override bool IsCollected(KeyValuePair<TKey, WeakReference> item)
        {
            return !item.Value.IsAlive;
        }

        public override ICollection<TKey> Keys
        {
            get { return InnerDictionary.Keys; }
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

        protected override TKey EncodeKey(TKey key)
        {
            return key;
        }

        protected override TKey DecodeKey(TKey key)
        {
            return key;
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