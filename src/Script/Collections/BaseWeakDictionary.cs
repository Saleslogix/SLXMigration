using System;
using System.Collections;
using System.Collections.Generic;

namespace Sage.SalesLogix.Migration.Script.Collections
{
    public abstract class BaseWeakDictionary<TKey, TValue, TInternalKey, TInternalValue> : IDictionary<TKey, TValue>
    {
        protected readonly IDictionary<TInternalKey, TInternalValue> InnerDictionary = new Dictionary<TInternalKey, TInternalValue>();
        private int _lastCount;
        private long _lastTotalMemory;

        #region IDictionary<TKey,TValue> Members

        public void Add(TKey key, TValue value)
        {
            Scavenge();
            InnerDictionary.Add(EncodeKey(key), EncodeValue(value));
        }

        public bool ContainsKey(TKey key)
        {
            return InnerDictionary.ContainsKey(EncodeKey(key));
        }

        public bool Remove(TKey key)
        {
            return InnerDictionary.Remove(EncodeKey(key));
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            TInternalValue internalValue;
            bool result = InnerDictionary.TryGetValue(EncodeKey(key), out internalValue);
            value = (result ? DecodeValue(internalValue) : default(TValue));
            return result;
        }

        public TValue this[TKey key]
        {
            get { return DecodeValue(InnerDictionary[EncodeKey(key)]); }
            set
            {
                Scavenge();
                InnerDictionary[EncodeKey(key)] = EncodeValue(value);
            }
        }

        public abstract ICollection<TKey> Keys { get; }

        public abstract ICollection<TValue> Values { get; }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Scavenge();
            InnerDictionary.Add(CreateInternalPair(item));
        }

        public void Clear()
        {
            InnerDictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return InnerDictionary.Contains(CreateInternalPair(item));
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            int count = array.Length;

            if (count > InnerDictionary.Count + arrayIndex)
            {
                count = InnerDictionary.Count + arrayIndex;
            }

            KeyValuePair<TInternalKey, TInternalValue>[] weakArray = new KeyValuePair<TInternalKey, TInternalValue>[count];
            InnerDictionary.CopyTo(weakArray, arrayIndex);

            for (int i = arrayIndex; i < count; i++)
            {
                array[i] = CreateExternalPair(weakArray[i]);
            }
        }

        public int Count
        {
            get { return InnerDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return InnerDictionary.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return InnerDictionary.Remove(CreateInternalPair(item));
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (KeyValuePair<TInternalKey, TInternalValue> item in InnerDictionary)
            {
                yield return CreateExternalPair(item);
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        private void Scavenge()
        {
            int count = InnerDictionary.Count;

            if (count > 0)
            {
                if (_lastCount == 0)
                {
                    _lastCount = count;
                }
                else
                {
                    long totalMemory = GC.GetTotalMemory(false);

                    if (_lastTotalMemory == 0)
                    {
                        _lastTotalMemory = totalMemory;
                    }
                    else
                    {
                        if (totalMemory < _lastTotalMemory && count > _lastCount)
                        {
                            ICollection<TInternalKey> collectedKeys = null;

                            foreach (KeyValuePair<TInternalKey, TInternalValue> item in InnerDictionary)
                            {
                                if (IsCollected(item))
                                {
                                    if (collectedKeys == null)
                                    {
                                        collectedKeys = new List<TInternalKey>();
                                    }

                                    collectedKeys.Add(item.Key);
                                }
                            }

                            if (collectedKeys != null)
                            {
                                foreach (TInternalKey key in collectedKeys)
                                {
                                    InnerDictionary.Remove(key);
                                }
                            }
                        }

                        _lastTotalMemory = totalMemory;
                        _lastCount = count;
                    }
                }
            }
        }

        private KeyValuePair<TInternalKey, TInternalValue> CreateInternalPair(KeyValuePair<TKey, TValue> item)
        {
            return new KeyValuePair<TInternalKey, TInternalValue>(EncodeKey(item.Key), EncodeValue(item.Value));
        }

        private KeyValuePair<TKey, TValue> CreateExternalPair(KeyValuePair<TInternalKey, TInternalValue> item)
        {
            return new KeyValuePair<TKey, TValue>(DecodeKey(item.Key), DecodeValue(item.Value));
        }

        protected abstract bool IsCollected(KeyValuePair<TInternalKey, TInternalValue> item);
        protected abstract TInternalKey EncodeKey(TKey key);
        protected abstract TKey DecodeKey(TInternalKey key);
        protected abstract TInternalValue EncodeValue(TValue value);
        protected abstract TValue DecodeValue(TInternalValue value);
    }
}