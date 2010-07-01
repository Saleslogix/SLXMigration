using System;
using System.CodeDom;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeNotificationExpressionCollection : CodeExpressionCollection
    {
        public delegate void CollectionChanged();

        private readonly CollectionChanged _changed;

        public CodeNotificationExpressionCollection(CollectionChanged changed)
        {
            _changed = changed;
        }

        public CodeNotificationExpressionCollection(CollectionChanged changed, CodeExpression[] value)
        {
            AddRange(value);
            _changed = changed;
        }

        protected override void OnClearComplete()
        {
            NotifyChanged();
            base.OnClearComplete();
        }

        protected override void OnInsertComplete(int index, object value)
        {
            NotifyChanged();
            base.OnInsertComplete(index, value);
        }

        protected override void OnRemoveComplete(int index, object value)
        {
            NotifyChanged();
            base.OnRemoveComplete(index, value);
        }

        protected override void OnSetComplete(int index, object oldValue, object newValue)
        {
            NotifyChanged();
            base.OnSetComplete(index, oldValue, newValue);
        }

        private void NotifyChanged()
        {
            if (_changed != null)
            {
                _changed();
            }
        }
    }
}