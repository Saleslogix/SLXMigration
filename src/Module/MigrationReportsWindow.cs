using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Sage.Platform.Application;
using Sage.Platform.Application.UI;
using Sage.Platform.Application.UI.WinForms;
using Sage.Platform.Projects.Interfaces;
using Sage.SalesLogix.Migration.Module.Properties;

namespace Sage.SalesLogix.Migration.Module
{
    public sealed partial class MigrationReportsWindow : UserControl, ISmartPartInfoProvider
    {
        private MigrationWorkItem _workItem;
        private IList<MigrationReport> _reports;
        private Command _editCommand;
        private MigrationModel _model;
        private ContextMenuStrip _contextMenu;

        public MigrationReportsWindow()
        {
            InitializeComponent();
            dataGridView.CellMouseClick += DataGridView_CellMouseClick;
        }

        [ServiceDependency]
        public IProjectContextService ProjectContext
        {
            set
            {
                value.ActiveProjectChanged += (sender, e) => ActiveProjectChanged((IProject) e.Item);
                ActiveProjectChanged(value.ActiveProject);
            }
        }

        [ServiceDependency]
        public MigrationWorkItem WorkItem
        {
            set
            {
                _workItem = value;
                _contextMenu = (_workItem.RootWorkItem.Items.Get(MigrationModule.CTX_REPORT) as ContextMenuStrip);
            }
        }

        private void ActiveProjectChanged(IProject project)
        {
            _model = project != null ? project.Models.Get<MigrationModel>() : null;
            _reports = _model != null ? _model.Reports : null;
            bindingSource.DataSource = (object) _reports ?? typeof (MigrationReport);
        }

        #region ISmartPartInfoProvider Members

        public ISmartPartInfo GetSmartPartInfo(Type smartPartInfoType)
        {
            if (smartPartInfoType == typeof (UltraDockSmartPartInfo))
            {
                return new UltraDockSmartPartInfo
                           {
                               Title = Resources.ReportsWindow_Title,
                               Description = Resources.ReportsWindow_Description,
                               Image = Resources.ReportsIcon,
                               DefaultLocation = DockedLocation.DockedBottom,
                               DefaultPaneStyle = ChildPaneStyle.TabGroup,
                               PreferredGroup = UriConstants.UILOCATION_BOTTOM
                           };
            }

            if (smartPartInfoType == typeof (UltraMdiTabSmartPartInfo))
            {
                return new UltraMdiTabSmartPartInfo
                           {
                               Title = Resources.ReportsWindow_Title,
                               Description = Resources.ReportsWindow_Description,
                               Image = Resources.ReportsIcon
                           };
            }

            return new SmartPartInfo(Resources.ReportsWindow_Title, Resources.ReportsWindow_Description);
        }

        #endregion

        public MigrationReport SelectedReport
        {
            get
            {
                return (dataGridView.SelectedRows.Count > 0
                            ? (MigrationReport) dataGridView.SelectedRows[0].DataBoundItem
                            : null);
            }
        }

        public void DeleteSelected()
        {
            var count = dataGridView.SelectedRows.Count;

            if (count > 0 &&
                MessageBox.Show(FindForm(),
                                "Are you sure you want to permanently delete the selected reports?",
                                "Confirmation",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (DataGridViewRow row in dataGridView.SelectedRows)
                {
                    var report = (MigrationReport) row.DataBoundItem;
                    report.Delete();
                    _model.RemoveReport(report);
                }
            }
        }

        private void dataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (_editCommand == null)
                {
                    _editCommand = _workItem.Commands[MigrationModule.CMD_VIEWREPORT];
                }

                _editCommand.Execute();
            }
        }

        private void DataGridView_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.Button == MouseButtons.Right)
            {
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    row.Selected = (row.Index == e.RowIndex);
                }

                var r = dataGridView.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
                _contextMenu.Show(dataGridView, r.Left + e.X, r.Top + e.Y);
            }
        }
    }
}