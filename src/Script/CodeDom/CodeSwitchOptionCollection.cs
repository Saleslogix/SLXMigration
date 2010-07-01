using System;
using System.Collections;
using System.ComponentModel;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeSwitchOptionCollection : CollectionBase
    {
        public delegate void CollectionChanged(CollectionChangeAction action, CodeSwitchOption option);

        private CollectionChanged _changed;

        public CodeSwitchOptionCollection(CodeSwitchOption[] value)
        {
            AddRange(value);
        }

        public CodeSwitchOptionCollection(CollectionChanged changed)
        {
            _changed = changed;
        }

        public CodeSwitchOptionCollection(CollectionChanged changed, CodeSwitchOption[] value)
        {
            AddRange(value);
            _changed = changed;
        }

        public int Add(CodeSwitchOption value)
        {
            return List.Add(value);
        }

        public void AddRange(CodeSwitchOption[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            for (int i = 0; i < value.Length; i++)
            {
                Add(value[i]);
            }
        }

        public void AddRange(CodeSwitchOptionCollection value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            int count = value.Count;

            for (int i = 0; i < count; i++)
            {
                Add(value[i]);
            }
        }

        public bool Contains(CodeSwitchOption value)
        {
            return List.Contains(value);
        }

        public void CopyTo(CodeSwitchOption[] array, int index)
        {
            List.CopyTo(array, index);
        }

        public int IndexOf(CodeSwitchOption value)
        {
            return List.IndexOf(value);
        }

        public void Insert(int index, CodeSwitchOption value)
        {
            List.Insert(index, value);
        }

        public void Remove(CodeSwitchOption value)
        {
            List.Remove(value);
        }

        public CodeSwitchOption this[int index]
        {
            get { return (CodeSwitchOption) List[index]; }
            set { List[index] = value; }
        }

        public CollectionChanged Changed
        {
            get { return _changed; }
            set { _changed = value; }
        }

        protected override void OnClearComplete()
        {
            NotifyChanged(CollectionChangeAction.Refresh, null);
            base.OnClearComplete();
        }

        protected override void OnInsertComplete(int index, object value)
        {
            NotifyChanged(CollectionChangeAction.Add, (CodeSwitchOption) value);
            base.OnInsertComplete(index, value);
        }

        protected override void OnRemoveComplete(int index, object value)
        {
            NotifyChanged(CollectionChangeAction.Remove, (CodeSwitchOption) value);
            base.OnRemoveComplete(index, value);
        }

        protected override void OnSetComplete(int index, object oldValue, object newValue)
        {
            NotifyChanged(CollectionChangeAction.Remove, (CodeSwitchOption) oldValue);
            NotifyChanged(CollectionChangeAction.Add, (CodeSwitchOption) newValue);
            base.OnSetComplete(index, oldValue, newValue);
        }

        private void NotifyChanged(CollectionChangeAction action, CodeSwitchOption option)
        {
            if (_changed != null)
            {
                _changed(action, option);
            }
        }
    }
}