using System.Collections.Generic;
using Sage.Platform.Exceptions;
using Sage.Platform.QuickForms.Controls;

namespace Sage.SalesLogix.Migration
{
    public sealed class ControlInfo
    {
        private readonly string _legacyType;
        private readonly IDictionary<object, DataPath> _bindings;
        private readonly int _originalLeft;
        private readonly int _originalTop;
        private readonly int _width;
        private readonly int _height;
        private readonly bool _isVisible;
        private int _left;
        private int _top;
        private string _caption;
        private string _hint;
        private QuickFormsControlBase _qfControl;
        private IControlBuilder _builder;
        private int _column;
        private int _row;
        private int _columnSpan;
        private int _rowSpan;
        private bool _isExcluded;
        private bool _isTool;
        private bool _isAutoSize; //used by the label control only
        private IList<ControlInfo> _controls = new List<ControlInfo>();

        public ControlInfo(string legacyType, IDictionary<object, DataPath> bindings, int left, int top, int width, int height, bool isVisible, string caption, string hint)
        {
            Guard.ArgumentNotNull(legacyType, "legacyType");

            _legacyType = legacyType;
            _bindings = bindings;
            _originalLeft = left;
            _originalTop = top;
            _width = width;
            _height = height;
            _isVisible = isVisible;
            _caption = caption;
            _hint = hint;
            ResetLocation();
        }

        public string LegacyType
        {
            get { return _legacyType; }
        }

        public IList<ControlInfo> Controls
        {
            get { return _controls; }
        }

        public IDictionary<object, DataPath> Bindings
        {
            get { return _bindings; }
        }

        public int Width
        {
            get { return _width; }
        }

        public int Height
        {
            get { return _height; }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
        }

        public int Left
        {
            get { return _left; }
            set { _left = value; }
        }

        public int Top
        {
            get { return _top; }
            set { _top = value; }
        }

        public string Caption
        {
            get { return _caption; }
            set { _caption = value; }
        }

        public string Hint
        {
            get { return _hint; }
            set { _hint = value; }
        }

        public QuickFormsControlBase QfControl
        {
            get { return _qfControl; }
            set { _qfControl = value; }
        }

        public IControlBuilder Builder
        {
            get { return _builder; }
            set { _builder = value; }
        }

        public int Column
        {
            get { return _column; }
            set { _column = value; }
        }

        public int Row
        {
            get { return _row; }
            set { _row = value; }
        }

        public int ColumnSpan
        {
            get { return _columnSpan; }
            set { _columnSpan = value; }
        }

        public int RowSpan
        {
            get { return _rowSpan; }
            set { _rowSpan = value; }
        }

        public bool IsExcluded
        {
            get { return _isExcluded; }
            set { _isExcluded = value; }
        }

        public bool IsTool
        {
            get { return _isTool; }
            set { _isTool = value; }
        }

        public bool IsAutoSize
        {
            get { return _isAutoSize; }
            set { _isAutoSize = value; }
        }

        public void ResetLocation()
        {
            _left = _originalLeft;
            _top = _originalTop;
        }
    }
}