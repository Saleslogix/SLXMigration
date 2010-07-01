using Sage.Platform.Orm.Entities;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration
{
    public sealed class MainViewInfo : BasePluginInfo
    {
        private readonly string _mainTable;
        private readonly string _detailFormName;
        private OrmEntity _entity;
        private FormInfo _detailForm;

        public MainViewInfo(Plugin plugin, string mainTable, string detailFormName)
            : base(plugin)
        {
            _mainTable = mainTable;
            _detailFormName = detailFormName;
        }

        public string MainTable
        {
            get { return _mainTable; }
        }

        public string DetailFormName
        {
            get { return _detailFormName; }
        }

        public OrmEntity Entity
        {
            get { return _entity; }
            set { _entity = value; }
        }

        public FormInfo DetailForm
        {
            get { return _detailForm; }
            set { _detailForm = value; }
        }
    }
}