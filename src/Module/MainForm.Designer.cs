namespace Sage.SalesLogix.Migration.Module
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label2;
            System.Windows.Forms.Label label3;
            System.Windows.Forms.Label label5;
            System.Windows.Forms.Label label4;
            System.Windows.Forms.Label label6;
            System.Windows.Forms.Label label7;
            System.Windows.Forms.Label label8;
            System.Windows.Forms.Label label9;
            System.Windows.Forms.Label label10;
            System.Windows.Forms.Label label11;
            System.Windows.Forms.Label label14;
            System.Windows.Forms.Button btnBrowseOutputDirectory;
            System.Windows.Forms.GroupBox groupBox2;
            System.Windows.Forms.Button btnBrowseSNK;
            System.Windows.Forms.Label label15;
            System.Windows.Forms.Label label16;
            System.Windows.Forms.Label label17;
            this.txtCustomLanguage = new System.Windows.Forms.TextBox();
            this.btnOther = new System.Windows.Forms.RadioButton();
            this.btnVBnet = new System.Windows.Forms.RadioButton();
            this.btnCSharp = new System.Windows.Forms.RadioButton();
            this.txtVSProject = new System.Windows.Forms.TextBox();
            this.txtNamespace = new System.Windows.Forms.TextBox();
            this.cmbLegacyProject = new System.Windows.Forms.ComboBox();
            this.btnRun = new System.Windows.Forms.Button();
            this.txtOutputDirectory = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.txtKeyPairFileName = new System.Windows.Forms.TextBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.cmbPortal = new System.Windows.Forms.ComboBox();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.chkMergeLabels = new System.Windows.Forms.CheckBox();
            this.cmbPackage = new System.Windows.Forms.ComboBox();
            this.cmbManifest = new System.Windows.Forms.ComboBox();
            this.cmbMainTable = new System.Windows.Forms.ComboBox();
            this.boxScripts = new System.Windows.Forms.GroupBox();
            this.chkProcessScripts = new System.Windows.Forms.CheckBox();
            this.bindingSource = new System.Windows.Forms.BindingSource(this.components);
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            label4 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            label8 = new System.Windows.Forms.Label();
            label9 = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            label11 = new System.Windows.Forms.Label();
            label14 = new System.Windows.Forms.Label();
            btnBrowseOutputDirectory = new System.Windows.Forms.Button();
            groupBox2 = new System.Windows.Forms.GroupBox();
            btnBrowseSNK = new System.Windows.Forms.Button();
            label15 = new System.Windows.Forms.Label();
            label16 = new System.Windows.Forms.Label();
            label17 = new System.Windows.Forms.Label();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.boxScripts.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(20, 20);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(81, 13);
            label1.TabIndex = 0;
            label1.Text = "Legacy Project:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(20, 276);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(87, 13);
            label2.TabIndex = 15;
            label2.Text = "Output Directory:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(8, 24);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(91, 13);
            label3.TabIndex = 1;
            label3.Text = "VS Project Name:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(20, 208);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(67, 13);
            label5.TabIndex = 12;
            label5.Text = "Namespace:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(20, 496);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(114, 13);
            label4.TabIndex = 19;
            label4.Text = "Strong Name Key Pair:";
            // 
            // label6
            // 
            label6.Location = new System.Drawing.Point(48, 48);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(376, 16);
            label6.TabIndex = 3;
            label6.Text = "Converted code and interoped COM libraries are output here.";
            // 
            // label7
            // 
            label7.Location = new System.Drawing.Point(60, 232);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(384, 32);
            label7.TabIndex = 14;
            label7.Text = "The default package namespace/assembly name and the namespace into which all conv" +
                "erted script is inserted.";
            // 
            // label8
            // 
            label8.Location = new System.Drawing.Point(40, 80);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(368, 20);
            label8.TabIndex = 4;
            label8.Text = "The fully qualified name of a third party CodeDomProvider implementation.";
            // 
            // label9
            // 
            label9.Location = new System.Drawing.Point(68, 520);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(376, 20);
            label9.TabIndex = 22;
            label9.Text = "Used to sign interoped COM libraries.";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(20, 156);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(63, 13);
            label10.TabIndex = 9;
            label10.Text = "Main Table:";
            // 
            // label11
            // 
            label11.Location = new System.Drawing.Point(60, 180);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(384, 16);
            label11.TabIndex = 11;
            label11.Text = "Determines the entity under which manage (non-data) forms will be inserted.";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new System.Drawing.Point(20, 100);
            label14.Name = "label14";
            label14.Size = new System.Drawing.Size(71, 13);
            label14.TabIndex = 5;
            label14.Text = "Target Portal:";
            // 
            // btnBrowseOutputDirectory
            // 
            btnBrowseOutputDirectory.Location = new System.Drawing.Point(452, 268);
            btnBrowseOutputDirectory.Name = "btnBrowseOutputDirectory";
            btnBrowseOutputDirectory.Size = new System.Drawing.Size(76, 24);
            btnBrowseOutputDirectory.TabIndex = 17;
            btnBrowseOutputDirectory.Text = "Browse...";
            btnBrowseOutputDirectory.UseVisualStyleBackColor = true;
            btnBrowseOutputDirectory.Click += new System.EventHandler(this.btnBrowseOutputDirectory_Click);
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(label8);
            groupBox2.Controls.Add(this.txtCustomLanguage);
            groupBox2.Controls.Add(this.btnOther);
            groupBox2.Controls.Add(this.btnVBnet);
            groupBox2.Controls.Add(this.btnCSharp);
            groupBox2.Location = new System.Drawing.Point(8, 72);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(416, 104);
            groupBox2.TabIndex = 4;
            groupBox2.TabStop = false;
            groupBox2.Text = "Target Language";
            // 
            // txtCustomLanguage
            // 
            this.txtCustomLanguage.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindingSource, "CustomCodeProvider", true));
            this.txtCustomLanguage.Location = new System.Drawing.Point(124, 48);
            this.txtCustomLanguage.Name = "txtCustomLanguage";
            this.txtCustomLanguage.ReadOnly = true;
            this.txtCustomLanguage.Size = new System.Drawing.Size(284, 20);
            this.txtCustomLanguage.TabIndex = 3;
            // 
            // btnOther
            // 
            this.btnOther.AutoSize = true;
            this.btnOther.Location = new System.Drawing.Point(12, 52);
            this.btnOther.Name = "btnOther";
            this.btnOther.Size = new System.Drawing.Size(60, 17);
            this.btnOther.TabIndex = 2;
            this.btnOther.Text = "Custom";
            this.btnOther.UseVisualStyleBackColor = true;
            this.btnOther.CheckedChanged += new System.EventHandler(this.Language_CheckedChanged);
            // 
            // btnVBnet
            // 
            this.btnVBnet.AutoSize = true;
            this.btnVBnet.Location = new System.Drawing.Point(60, 24);
            this.btnVBnet.Name = "btnVBnet";
            this.btnVBnet.Size = new System.Drawing.Size(57, 17);
            this.btnVBnet.TabIndex = 1;
            this.btnVBnet.Text = "VB.net";
            this.btnVBnet.UseVisualStyleBackColor = true;
            this.btnVBnet.CheckedChanged += new System.EventHandler(this.Language_CheckedChanged);
            // 
            // btnCSharp
            // 
            this.btnCSharp.AutoSize = true;
            this.btnCSharp.Checked = true;
            this.btnCSharp.Location = new System.Drawing.Point(12, 24);
            this.btnCSharp.Name = "btnCSharp";
            this.btnCSharp.Size = new System.Drawing.Size(39, 17);
            this.btnCSharp.TabIndex = 0;
            this.btnCSharp.TabStop = true;
            this.btnCSharp.Text = "C#";
            this.btnCSharp.UseVisualStyleBackColor = true;
            this.btnCSharp.CheckedChanged += new System.EventHandler(this.Language_CheckedChanged);
            // 
            // btnBrowseSNK
            // 
            btnBrowseSNK.Location = new System.Drawing.Point(452, 488);
            btnBrowseSNK.Name = "btnBrowseSNK";
            btnBrowseSNK.Size = new System.Drawing.Size(76, 24);
            btnBrowseSNK.TabIndex = 21;
            btnBrowseSNK.Text = "Browse...";
            btnBrowseSNK.UseVisualStyleBackColor = true;
            btnBrowseSNK.Click += new System.EventHandler(this.btnBrowseSNK_Click);
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new System.Drawing.Point(20, 72);
            label15.Name = "label15";
            label15.Size = new System.Drawing.Size(87, 13);
            label15.TabIndex = 3;
            label15.Text = "Target Package:";
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new System.Drawing.Point(20, 128);
            label16.Name = "label16";
            label16.Size = new System.Drawing.Size(84, 13);
            label16.TabIndex = 7;
            label16.Text = "Target Manifest:";
            // 
            // label17
            // 
            label17.Location = new System.Drawing.Point(60, 44);
            label17.Name = "label17";
            label17.Size = new System.Drawing.Size(384, 16);
            label17.TabIndex = 2;
            label17.Text = "Name of the legacy project that contains all the plugins to be migrated.";
            // 
            // txtVSProject
            // 
            this.txtVSProject.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindingSource, "VSProjectName", true));
            this.txtVSProject.Location = new System.Drawing.Point(132, 20);
            this.txtVSProject.Name = "txtVSProject";
            this.txtVSProject.Size = new System.Drawing.Size(292, 20);
            this.txtVSProject.TabIndex = 2;
            // 
            // txtNamespace
            // 
            this.txtNamespace.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindingSource, "Namespace", true));
            this.txtNamespace.Location = new System.Drawing.Point(144, 204);
            this.txtNamespace.Name = "txtNamespace";
            this.txtNamespace.Size = new System.Drawing.Size(300, 20);
            this.txtNamespace.TabIndex = 13;
            // 
            // cmbLegacyProject
            // 
            this.cmbLegacyProject.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindingSource, "LegacyProject", true));
            this.cmbLegacyProject.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLegacyProject.FormattingEnabled = true;
            this.cmbLegacyProject.Location = new System.Drawing.Point(144, 16);
            this.cmbLegacyProject.MaxDropDownItems = 16;
            this.cmbLegacyProject.Name = "cmbLegacyProject";
            this.cmbLegacyProject.Size = new System.Drawing.Size(300, 21);
            this.cmbLegacyProject.TabIndex = 1;
            this.cmbLegacyProject.SelectedIndexChanged += new System.EventHandler(this.cmbLegacyProject_SelectedIndexChanged);
            // 
            // btnRun
            // 
            this.btnRun.Location = new System.Drawing.Point(452, 568);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(76, 24);
            this.btnRun.TabIndex = 26;
            this.btnRun.Text = "Run";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // txtOutputDirectory
            // 
            this.txtOutputDirectory.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindingSource, "OutputDirectory", true));
            this.txtOutputDirectory.Location = new System.Drawing.Point(144, 272);
            this.txtOutputDirectory.Name = "txtOutputDirectory";
            this.txtOutputDirectory.Size = new System.Drawing.Size(300, 20);
            this.txtOutputDirectory.TabIndex = 16;
            // 
            // txtKeyPairFileName
            // 
            this.txtKeyPairFileName.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindingSource, "KeyPairFileName", true));
            this.txtKeyPairFileName.Location = new System.Drawing.Point(144, 492);
            this.txtKeyPairFileName.Name = "txtKeyPairFileName";
            this.txtKeyPairFileName.Size = new System.Drawing.Size(300, 20);
            this.txtKeyPairFileName.TabIndex = 20;
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "Strong Name Key-Pair (*.snk)|*.snk|All Files (*.*)|*.*";
            // 
            // backgroundWorker
            // 
            this.backgroundWorker.WorkerReportsProgress = true;
            this.backgroundWorker.WorkerSupportsCancellation = true;
            this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
            this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_RunWorkerCompleted);
            this.backgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker_ProgressChanged);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 596);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(432, 24);
            this.progressBar.TabIndex = 25;
            // 
            // lblStatus
            // 
            this.lblStatus.Location = new System.Drawing.Point(12, 576);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(432, 16);
            this.lblStatus.TabIndex = 24;
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(452, 596);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(76, 24);
            this.btnClose.TabIndex = 27;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // cmbPortal
            // 
            this.cmbPortal.DisplayMember = "PortalTitle";
            this.cmbPortal.FormattingEnabled = true;
            this.cmbPortal.Location = new System.Drawing.Point(144, 96);
            this.cmbPortal.MaxDropDownItems = 16;
            this.cmbPortal.Name = "cmbPortal";
            this.cmbPortal.Size = new System.Drawing.Size(300, 21);
            this.cmbPortal.TabIndex = 6;
            this.cmbPortal.ValueMember = "PortalAlias";
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            // 
            // chkMergeLabels
            // 
            this.chkMergeLabels.AutoSize = true;
            this.chkMergeLabels.DataBindings.Add(new System.Windows.Forms.Binding("Checked", this.bindingSource, "SetRowAndColumnSizes", true));
            this.chkMergeLabels.Location = new System.Drawing.Point(36, 548);
            this.chkMergeLabels.Name = "chkMergeLabels";
            this.chkMergeLabels.Size = new System.Drawing.Size(198, 17);
            this.chkMergeLabels.TabIndex = 23;
            this.chkMergeLabels.Text = " Merge labels with adjacent controls ";
            this.chkMergeLabels.UseVisualStyleBackColor = true;
            this.chkMergeLabels.CheckedChanged += new System.EventHandler(this.chkMergeLabels_CheckedChanged);
            // 
            // cmbPackage
            // 
            this.cmbPackage.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindingSource, "PackageName", true));
            this.cmbPackage.FormattingEnabled = true;
            this.cmbPackage.Location = new System.Drawing.Point(144, 68);
            this.cmbPackage.MaxDropDownItems = 16;
            this.cmbPackage.Name = "cmbPackage";
            this.cmbPackage.Size = new System.Drawing.Size(300, 21);
            this.cmbPackage.TabIndex = 4;
            // 
            // cmbManifest
            // 
            this.cmbManifest.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindingSource, "ManifestName", true));
            this.cmbManifest.DisplayMember = "Name";
            this.cmbManifest.FormattingEnabled = true;
            this.cmbManifest.Location = new System.Drawing.Point(144, 124);
            this.cmbManifest.MaxDropDownItems = 16;
            this.cmbManifest.Name = "cmbManifest";
            this.cmbManifest.Size = new System.Drawing.Size(300, 21);
            this.cmbManifest.TabIndex = 8;
            this.cmbManifest.ValueMember = "Name";
            // 
            // cmbMainTable
            // 
            this.cmbMainTable.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.bindingSource, "MainTable", true));
            this.cmbMainTable.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMainTable.FormattingEnabled = true;
            this.cmbMainTable.Location = new System.Drawing.Point(144, 152);
            this.cmbMainTable.MaxDropDownItems = 16;
            this.cmbMainTable.Name = "cmbMainTable";
            this.cmbMainTable.Size = new System.Drawing.Size(300, 21);
            this.cmbMainTable.TabIndex = 10;
            // 
            // boxScripts
            // 
            this.boxScripts.Controls.Add(label3);
            this.boxScripts.Controls.Add(groupBox2);
            this.boxScripts.Controls.Add(this.txtVSProject);
            this.boxScripts.Controls.Add(label6);
            this.boxScripts.Enabled = false;
            this.boxScripts.Location = new System.Drawing.Point(12, 300);
            this.boxScripts.Name = "boxScripts";
            this.boxScripts.Size = new System.Drawing.Size(432, 184);
            this.boxScripts.TabIndex = 18;
            this.boxScripts.TabStop = false;
            // 
            // chkProcessScripts
            // 
            this.chkProcessScripts.AutoSize = true;
            this.chkProcessScripts.DataBindings.Add(new System.Windows.Forms.Binding("Checked", this.bindingSource, "ProcessScripts", true));
            this.chkProcessScripts.Location = new System.Drawing.Point(20, 300);
            this.chkProcessScripts.Name = "chkProcessScripts";
            this.chkProcessScripts.Size = new System.Drawing.Size(99, 17);
            this.chkProcessScripts.TabIndex = 17;
            this.chkProcessScripts.Text = "Process Scripts";
            this.chkProcessScripts.UseVisualStyleBackColor = true;
            this.chkProcessScripts.CheckedChanged += new System.EventHandler(this.chkProcessScripts_CheckedChanged);
            // 
            // bindingSource
            // 
            this.bindingSource.DataSource = typeof(Sage.SalesLogix.Migration.MigrationSettings);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(540, 629);
            this.Controls.Add(this.chkProcessScripts);
            this.Controls.Add(this.boxScripts);
            this.Controls.Add(label17);
            this.Controls.Add(this.cmbMainTable);
            this.Controls.Add(label16);
            this.Controls.Add(this.cmbManifest);
            this.Controls.Add(label15);
            this.Controls.Add(this.cmbPackage);
            this.Controls.Add(this.chkMergeLabels);
            this.Controls.Add(this.cmbPortal);
            this.Controls.Add(label14);
            this.Controls.Add(label9);
            this.Controls.Add(label11);
            this.Controls.Add(label7);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(btnBrowseSNK);
            this.Controls.Add(this.txtKeyPairFileName);
            this.Controls.Add(label4);
            this.Controls.Add(label10);
            this.Controls.Add(label5);
            this.Controls.Add(this.txtNamespace);
            this.Controls.Add(btnBrowseOutputDirectory);
            this.Controls.Add(this.txtOutputDirectory);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnRun);
            this.Controls.Add(label2);
            this.Controls.Add(label1);
            this.Controls.Add(this.cmbLegacyProject);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Migration Tool";
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.boxScripts.ResumeLayout(false);
            this.boxScripts.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtCustomLanguage;
        private System.Windows.Forms.RadioButton btnOther;
        private System.Windows.Forms.RadioButton btnVBnet;
        private System.Windows.Forms.RadioButton btnCSharp;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.ComponentModel.BackgroundWorker backgroundWorker;
        private System.Windows.Forms.BindingSource bindingSource;
        private System.Windows.Forms.ErrorProvider errorProvider;
        private System.Windows.Forms.ComboBox cmbLegacyProject;
        private System.Windows.Forms.ComboBox cmbPortal;
        private System.Windows.Forms.TextBox txtOutputDirectory;
        private System.Windows.Forms.TextBox txtKeyPairFileName;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.CheckBox chkMergeLabels;
        private System.Windows.Forms.ComboBox cmbManifest;
        private System.Windows.Forms.ComboBox cmbPackage;
        private System.Windows.Forms.ComboBox cmbMainTable;
        private System.Windows.Forms.CheckBox chkProcessScripts;
        private System.Windows.Forms.GroupBox boxScripts;
        private System.Windows.Forms.TextBox txtVSProject;
        private System.Windows.Forms.TextBox txtNamespace;

    }
}