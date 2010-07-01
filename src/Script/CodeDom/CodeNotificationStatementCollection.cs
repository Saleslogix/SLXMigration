using System;
using System.CodeDom;
using System.ComponentModel;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeNotificationStatementCollection : CodeStatementCollection
    {
        public delegate void CollectionChanged(CollectionChangeAction action, CodeStatement stmt, int index);

        private CollectionChanged _changed;

        public CodeNotificationStatementCollection(CollectionChanged changed)
        {
            _changed = changed;
        }

        public CodeNotificationStatementCollection(CollectionChanged changed, CodeStatement[] value)
        {
            AddRange(value);
            _changed = changed;
        }

        public CollectionChanged Changed
        {
            get { return _changed; }
            set { _changed = value; }
        }

        protected override void OnClearComplete()
        {
            NotifyChanged(CollectionChangeAction.Refresh, null, -1);
            base.OnClearComplete();
        }

        protected override void OnInsertComplete(int index, object value)
        {
            NotifyChanged(CollectionChangeAction.Add, (CodeStatement) value, index);
            base.OnInsertComplete(index, value);
        }

        protected override void OnRemoveComplete(int index, object value)
        {
            NotifyChanged(CollectionChangeAction.Remove, (CodeStatement) value, index);
            base.OnRemoveComplete(index, value);
        }

        protected override void OnSetComplete(int index, object oldValue, object newValue)
        {
            NotifyChanged(CollectionChangeAction.Remove, (CodeStatement) oldValue, index);
            NotifyChanged(CollectionChangeAction.Add, (CodeStatement) newValue, index);
            base.OnSetComplete(index, oldValue, newValue);
        }

        private void NotifyChanged(CollectionChangeAction action, CodeStatement option, int index)
        {
            if (_changed != null)
            {
                _changed(action, option, index);
            }
        }
    }
}