using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sage.SalesLogix.Migration.Script.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    public sealed class WeakKeyDictionary<TKey, TValue> : BaseWeakDictionary<TKey, TValue, EqualityWeakReference<TKey>, TValue>
    {
        protected override bool IsCollected(KeyValuePair<EqualityWeakReference<TKey>, TValue> item)
        {
            return !item.Key.IsAlive;
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
            get { return InnerDictionary.Values; }
        }

        protected override EqualityWeakReference<TKey> EncodeKey(TKey key)
        {
            return new EqualityWeakReference<TKey>(key);
        }

        protected override TKey DecodeKey(EqualityWeakReference<TKey> key)
        {
            return key.Target;
        }

        protected override TValue EncodeValue(TValue value)
        {
            return value;
        }

        protected override TValue DecodeValue(TValue value)
        {
            return value;
        }
    }

    public sealed class EqualityWeakReference<T> : WeakReference
    {
        private readonly int _hashCode;

        public EqualityWeakReference(T target)
            : base(target)
        {
            _hashCode = target.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj.GetHashCode() == _hashCode)
            {
                EqualityWeakReference<T> weakRef = obj as EqualityWeakReference<T>;

                if (weakRef != null && ReferenceEquals(Target, weakRef.Target))
                {
                    return true;
                }
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public new T Target
        {
            get { return (T) base.Target; }
        }
    }
}