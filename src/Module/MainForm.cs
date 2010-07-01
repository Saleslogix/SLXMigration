using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Sage.Platform.Application;
using Sage.Platform.Application.Services;
using Sage.Platform.Application.UI;
using Sage.Platform.IDEModule;
using Sage.Platform.Orm.CodeGen;
using Sage.Platform.Orm.Entities;
using Sage.Platform.Projects.Interfaces;
using Sage.Platform.WebPortal.Design;
using Sage.SalesLogix.Migration.Module.Properties;
using Sage.SalesLogix.Migration.Services;
using _BundleModel = Sage.Platform.BundleModel.BundleModel;

namespace Sage.SalesLogix.Migration.Module
{
    [SmartPart]
    public sealed partial class MainForm : Form
    {
        private MigrationSettings _settings;
        private MigrationWorkItem _workItem;
        private IOrmEntityLoaderService _entityLoader;
        private IOperationStatus _status;
        private bool _cancelled;
        private IProjectContextService _projectContext;
        private IBuildLogView _buildLogView;
        private IOutputWindowService _outputWindow;
        private string _lastLegacyProject;

        public MainForm()
        {
            InitializeComponent();
        }

        [ServiceDependency]
        public MigrationWorkItem WorkItem
        {
            set { _workItem = value; }
        }

        [ServiceDependency]
        public IOrmEntityLoaderService EntityLoader
        {
            set { _entityLoader = value; }
        }

        [ServiceDependency]
        public IProjectContextService ProjectContext
        {
            set { _projectContext = value; }
        }

        [ServiceDependency]
        public IBuildLogView BuildLogView
        {
            set { _buildLogView = value; }
        }

        [ServiceDependency]
        public IOutputWindowService OutputWindow
        {
            set { _outputWindow = value; }
        }

        protected override void OnLoad(EventArgs e)
        {
            Icon = Icon.FromHandle(Resources.ToolIcon.GetHicon());
            _settings = MigrationSettings.GetCurrentMigrationSettings(_workItem);
            bindingSource.DataSource = _settings;

            switch (_settings.Language)
            {
                case Language.CSharp:
                    btnCSharp.Checked = true;
                    break;
                case Language.VBNet:
                    btnVBnet.Checked = true;
                    break;
                default:
                    btnOther.Checked = true;
                    txtCustomLanguage.ReadOnly = !btnOther.Checked;
                    break;
            }

            PopulateComboBoxes();

            chkMergeLabels.Checked = _settings.MergeLabels;

            base.OnLoad(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            backgroundWorker.CancelAsync();
            bindingSource.EndEdit();

            string selectedPortal = cmbPortal.SelectedValue as string;
            _settings.PortalName = (!string.IsNullOrEmpty(selectedPortal) ? selectedPortal : cmbPortal.Text);
            _settings.MergeLabels = chkMergeLabels.Checked;
            _settings.Save();

            base.OnFormClosed(e);
        }

        private void PopulateComboBoxes()
        {
            cmbLegacyProject.DataSource = null;
            cmbPackage.DataSource = null;
            cmbPortal.DataSource = null;
            cmbManifest.DataSource = null;
            cmbMainTable.DataSource = null;

            cmbLegacyProject.DataSource = _entityLoader.LoadProjects();
            cmbPackage.DataSource = GetDynamicPackages();
            cmbPortal.DataSource = PortalApplication.GetAll();
            cmbManifest.DataSource = _projectContext.ActiveProject.Models.Get<_BundleModel>().BundleManifests;
            cmbMainTable.DataSource = _entityLoader.LoadTables();

            cmbLegacyProject.Text = _settings.LegacyProject;
            cmbPackage.Text = _settings.PackageName;

            try
            {
                cmbPortal.SelectedValue = _settings.PortalName;
            }
            catch (ArgumentNullException) {}

            if (cmbPortal.SelectedValue == null)
            {
                cmbPortal.Text = _settings.PortalName;
            }

            cmbManifest.Text = _settings.ManifestName;
            cmbMainTable.Text = _settings.MainTable;
        }

        private IList<OrmPackage> GetDynamicPackages()
        {
            List<OrmPackage> packages = new List<OrmPackage>(_projectContext.ActiveProject.Models.Get<OrmModel>().Packages);
            packages.RemoveAll(
                delegate(OrmPackage package)
                    {
                        return !package.GetGenerateAssembly();
                    });
            return packages;
        }

        private void cmbLegacyProject_SelectedIndexChanged(object sender, EventArgs e)
        {
            string legacyProject = cmbLegacyProject.Text;

            if (!string.IsNullOrEmpty(legacyProject))
            {
                if (!string.IsNullOrEmpty(_lastLegacyProject))
                {
                    if (cmbManifest.Text == _lastLegacyProject)
                    {
                        cmbManifest.Text = legacyProject;
                    }

                    if (txtNamespace.Text == StringUtils.UnderscoreInvalidChars(_lastLegacyProject))
                    {
                        txtNamespace.Text = StringUtils.UnderscoreInvalidChars(legacyProject);
                    }

                    if (txtOutputDirectory.Text.EndsWith(@"\" + _lastLegacyProject))
                    {
                        txtOutputDirectory.Text = txtOutputDirectory.Text.Substring(0, txtOutputDirectory.Text.Length - _lastLegacyProject.Length) + legacyProject;
                    }

                    if (txtVSProject.Text == _lastLegacyProject)
                    {
                        txtVSProject.Text = legacyProject;
                    }
                }

                _lastLegacyProject = legacyProject;
            }
        }

        private void btnBrowseOutputDirectory_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = FindExistingPath(txtOutputDirectory.Text);

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtOutputDirectory.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void chkProcessScripts_CheckedChanged(object sender, EventArgs e)
        {
            boxScripts.Enabled = chkProcessScripts.Checked;
        }

        private void Language_CheckedChanged(object sender, EventArgs e)
        {
            if (btnCSharp.Checked)
            {
                _settings.Language = Language.CSharp;
            }
            else if (btnVBnet.Checked)
            {
                _settings.Language = Language.VBNet;
            }
            else
            {
                _settings.Language = Language.Custom;
                txtCustomLanguage.ReadOnly = !btnOther.Checked;
            }
        }

        private void btnBrowseSNK_Click(object sender, EventArgs e)
        {
            string path = txtKeyPairFileName.Text;

            if (File.Exists(path))
            {
                openFileDialog.FileName = path;
            }
            else
            {
                openFileDialog.FileName = null;
                openFileDialog.InitialDirectory = FindExistingPath(path);
            }

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtKeyPairFileName.Text = openFileDialog.FileName;
            }
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            _cancelled = backgroundWorker.IsBusy;

            if (_cancelled)
            {
                backgroundWorker.CancelAsync();
            }
            else
            {
                bindingSource.EndEdit();
                string selectedPortal = cmbPortal.SelectedValue as string;
                _settings.PortalName = (!string.IsNullOrEmpty(selectedPortal) ? selectedPortal : cmbPortal.Text);
                Cursor = Cursors.WaitCursor;
                btnRun.Text = "Cancel";
                btnClose.Enabled = false;
                _buildLogView.Clear();
                _outputWindow.Clear("migration");
                _workItem.Commands[IDECommandConstants.CMD_SHOW_OUTPUTWINDOW].Execute();
                backgroundWorker.RunWorkerAsync();
            }
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (_workItem == null)
            {
                throw new MigrationException("WorkItem not set");
            }

            if (_status == null)
            {
                _status = new BackgroundWorkerStatus(backgroundWorker, _workItem.Log);
            }

            _workItem.Execute(_settings, _status);
            e.Cancel = _cancelled;
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;

            if (e.UserState != null)
            {
                lblStatus.Text = (string) e.UserState;
            }
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Cursor = Cursors.Default;
            btnRun.Text = "Run";
            btnClose.Enabled = true;

            if (e.Error != null)
            {
                lblStatus.Text = "Error";

                if (_workItem != null && _workItem.Log != null)
                {
                    _workItem.Log.Error(e.Error.ToString());
                }
            }
            else if (e.Cancelled)
            {
                lblStatus.Text = "Cancelled";

                if (_workItem != null && _workItem.Log != null)
                {
                    _workItem.Log.Info("Cancelled");
                }
            }
            else
            {
                lblStatus.Text = "Finished";

                if (_workItem != null && _workItem.Log != null)
                {
                    _workItem.Log.Info("Finished");
                }
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private static string FindExistingPath(string path)
        {
            while (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                int pos = path.LastIndexOf('\\');

                if (pos < 0)
                {
                    path = null;
                    break;
                }
                else
                {
                    path = path.Substring(0, pos);
                }
            }

            return path;
        }

        private void chkMergeLabels_CheckedChanged(object sender, EventArgs e)
        {
            _settings.MergeLabels = chkMergeLabels.Checked;
        }
    }
}