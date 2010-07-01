using System.Collections.Generic;
using System.Reflection;
using Iesi.Collections.Generic;
using Sage.Platform.Exceptions;
using Sage.Platform.Orm.Entities;
using Sage.Platform.QuickForms;
using Sage.Platform.WebPortal.Design;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration
{
    public sealed class FormInfo : BasePluginInfo
    {
        //private static readonly char[] _illegalSmartPartIdChars;

        /*static FormInfo()
        {
            FieldInfo field = typeof (PortalUtil).GetField("IllegalFileNameCharacters", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (field != null)
            {
                _illegalSmartPartIdChars = (char[]) field.GetValue(null);
            }
        }*/

        private readonly bool _isLegacy;
        private readonly string _baseTable;
        private readonly bool _isDataForm;
        private readonly string _dialogCaption;
        private readonly string _tabCaption;
        private readonly int _width;
        private readonly int _height;
        private readonly IList<ControlInfo> _controls;
        private readonly ISet<FormInfo> _dialogForms;
        private OrmEntity _entity;
        private IQuickFormDefinition _quickForm;
        private string _smartPartId;
        private bool _hasGroupNavigator;
        private bool _hasSaveButton;
        private bool _hasDeleteButton;
        private bool _isDetail;

        public FormInfo(Plugin plugin, string baseTable, bool isDataForm, string dialogCaption, string tabCaption, int width, int height)
            : base(plugin)
        {
            Guard.ArgumentNotNullOrEmptyString(baseTable, "baseTable");

            _isLegacy = (plugin.Type == PluginType.LegacyForm);
            _baseTable = baseTable;
            _isDataForm = isDataForm;
            _dialogCaption = dialogCaption;
            _tabCaption = tabCaption;
            _width = width;
            _height = height;
            _controls = new List<ControlInfo>();
            _dialogForms = new HashedSet<FormInfo>();
        }

        public bool IsLegacy
        {
            get { return _isLegacy; }
        }

        public string BaseTable
        {
            get { return _baseTable; }
        }

        public bool IsDataForm
        {
            get { return _isDataForm; }
        }

        public string DialogCaption
        {
            get { return _dialogCaption; }
        }

        public string TabCaption
        {
            get { return _tabCaption; }
        }

        public int Width
        {
            get { return _width; }
        }

        public int Height
        {
            get { return _height; }
        }

        public IList<ControlInfo> Controls
        {
            get { return _controls; }
        }

        public ISet<FormInfo> DialogForms
        {
            get { return _dialogForms; }
        }

        public OrmEntity Entity
        {
            get { return _entity; }
            set { _entity = value; }
        }

        public IQuickFormDefinition QuickForm
        {
            get { return _quickForm; }
            set { _quickForm = value; }
        }

        public string SmartPartId
        {
            get { return _smartPartId ?? (_smartPartId = StringUtils.ReplaceIllegalChars(FullName)); }
        }

        public bool HasGroupNavigator
        {
            get { return _hasGroupNavigator; }
            set { _hasGroupNavigator = value; }
        }

        public bool HasSaveButton
        {
            get { return _hasSaveButton; }
            set { _hasSaveButton = value; }
        }

        public bool HasDeleteButton
        {
            get { return _hasDeleteButton; }
            set { _hasDeleteButton = value; }
        }

        public bool IsDetail
        {
            get { return _isDetail; }
            set { _isDetail = value; }
        }
    }
}