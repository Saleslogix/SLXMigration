using System;
using System.CodeDom;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeOnErrorStatement : CodeCommentStatement
    {
        private bool _enabled;

        public CodeOnErrorStatement()
        {
            Refresh();
        }

        public CodeOnErrorStatement(bool enabled)
        {
            _enabled = enabled;
            Refresh();
        }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    Refresh();
                }
            }
        }

        [Obsolete]
        public new CodeComment Comment
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        private void Refresh()
        {
            base.Comment = new CodeComment(_enabled
                                               ? "Warning: 'On Error Resume Next' not supported"
                                               : "Warning: 'On Error GoTo 0' not supported");
        }
    }
}