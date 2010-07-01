using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Sage.Platform.AdminModule;
using Sage.Platform.Application;
using Sage.Platform.Application.Services;
using Sage.Platform.Application.UI;
using Sage.Platform.Application.UI.WinForms;
using Sage.Platform.IDEModule;
using Sage.Platform.Projects.Interfaces;
using Sage.Platform.TemplateSupport;
using Sage.Platform.TemplateSupport.NV;

namespace Sage.SalesLogix.Migration.Module
{
    public sealed partial class MigrationReportEditor : UserControl, ISmartPartInfoProvider
    {
        private MigrationReportPersistentAdapter _reportAdapter;
        private ITemplate _template;

        public MigrationReportEditor(
            [ServiceDependency] WorkItem workItem,
            [ServiceDependency] IProjectContextService projectContext,
            [ServiceDependency] IEditItemService editItemService)
        {
            InitializeComponent();
            webBrowser.ObjectForScripting = new ObjectForScripting(workItem, projectContext, editItemService);
        }

        public MigrationReportPersistentAdapter ReportAdapter
        {
            get { return _reportAdapter; }
            set
            {
                _reportAdapter = value;
                ReportAdapterChanged();
            }
        }

        private void ReportAdapterChanged()
        {
            if (_template == null)
            {
                using (TextReader reader = new StreamReader(GetType().Assembly.GetManifestResourceStream(GetType(), "MigrationReportLayout.vm")))
                {
                    _template = new NVelocityTemplateEngine().CreateTemplate(reader.ReadToEnd());
                }
            }

            Hashtable context = new Hashtable();
            context.Add("report", _reportAdapter.Report);
            context.Add("types", BuildSummary(_reportAdapter.Report));
            webBrowser.DocumentText = _template.ApplyTemplate(context);
        }

        #region ISmartPartInfoProvider Members

        public ISmartPartInfo GetSmartPartInfo(Type smartPartInfoType)
        {
            var title = string.Format("{0} ({1})", _reportAdapter.Report.Settings.LegacyProject, _reportAdapter.Report.Date);
            return new SmartPartInfo(title, title);
        }

        #endregion

        [ComVisible(true)]
        public sealed class ObjectForScripting
        {
            private readonly WorkItem _workItem;
            private readonly IProjectContextService _projectContext;
            private readonly IEditItemService _editItemService;

            public ObjectForScripting(WorkItem workItem, IProjectContextService projectContext, IEditItemService editItemService)
            {
                _workItem = workItem;
                _projectContext = projectContext;
                _editItemService = editItemService;
            }

            public void EditItem(string itemUrl)
            {
                if (File.Exists(itemUrl))
                {
                    AdminModuleInit adminModuleInit = _workItem.Modules.Get<AdminModuleInit>();
                    MethodInfo method = typeof (AdminModuleInit).GetMethod("OpenCodeEditor", BindingFlags.NonPublic | BindingFlags.Instance);
                    method.Invoke(adminModuleInit, new object[] {itemUrl});
                }
                else
                {
                    IProject project = _projectContext.ActiveProject;
                    IModelItemInfo info = project.GetModelItemInfo(itemUrl);

                    if (info != null)
                    {
                        IModelItem item = project.Get(itemUrl, info.ModelItemType);

                        if (item != null)
                        {
                            Cursor.Current = Cursors.WaitCursor;
                            try
                            {
                                _editItemService.EditItem((item.Model ?? info.Model).GetAsPersistentObject(item), _workItem, _projectContext.ActiveProject.InstanceId);
                            }
                            finally
                            {
                                Cursor.Current = Cursors.Default;
                            }
                        }
                    }
                }
            }
        }

        public static TypeSummary[] BuildSummary(MigrationReport report)
        {
            IDictionary<string, TypeSummary> types = new Dictionary<string, TypeSummary>();

            foreach (MigrationReportMessage message in report.Messages)
            {
                TypeSummary type;
                string sourceType = message.SourceType ?? string.Empty;

                if (!types.TryGetValue(sourceType, out type))
                {
                    type = new TypeSummary(sourceType);
                    types.Add(sourceType, type);
                }

                type.AddMessage(message);
            }

            int typeCount = types.Count;
            string[] keys = new string[typeCount];
            TypeSummary[] values = new TypeSummary[typeCount];
            types.Keys.CopyTo(keys, 0);
            types.Values.CopyTo(values, 0);
            Array.Sort(keys, values);
            return values;
        }

        public sealed class TypeSummary
        {
            private readonly string _name;
            private int _errors;
            private int _warnings;
            private readonly IDictionary<string, ItemSummary> _items;

            public TypeSummary(string name)
            {
                _name = name;
                _items = new Dictionary<string, ItemSummary>();
            }

            public string Name
            {
                get { return _name; }
            }

            public int Errors
            {
                get { return _errors; }
            }

            public int Warnings
            {
                get { return _warnings; }
            }

            public IEnumerable<ItemSummary> Items
            {
                get
                {
                    int count = _items.Count;
                    string[] keys = new string[count];
                    ItemSummary[] values = new ItemSummary[count];
                    _items.Keys.CopyTo(keys, 0);
                    _items.Values.CopyTo(values, 0);
                    Array.Sort(keys, values);
                    return values;
                }
            }

            public void AddMessage(MigrationReportMessage message)
            {
                ItemSummary item;
                string sourceName = message.SourceName ?? string.Empty;

                if (!_items.TryGetValue(sourceName, out item))
                {
                    item = new ItemSummary(sourceName, message.GeneratedItem);
                    _items.Add(sourceName, item);
                }

                item.AddMessage(message);

                if (message.Type == BuildMessageType.Error)
                {
                    _errors++;
                }
                else if (message.Type == BuildMessageType.Warning)
                {
                    _warnings++;
                }
            }
        }

        public sealed class ItemSummary
        {
            private readonly string _sourceName;
            private readonly string _generatedItem;
            private int _errors;
            private int _warnings;
            private readonly List<string> _messages;

            public ItemSummary(string sourceName, string generatedItem)
            {
                _sourceName = sourceName;
                _generatedItem = generatedItem;
                _messages = new List<string>();
            }

            public string Name
            {
                get { return _sourceName; }
            }

            public string GeneratedItem
            {
                get { return (_generatedItem != null ? _generatedItem.Replace("\\", "\\\\") : null); }
            }

            public int Errors
            {
                get { return _errors; }
            }

            public int Warnings
            {
                get { return _warnings; }
            }

            public IEnumerable<string> Messages
            {
                get { return _messages; }
            }

            public void AddMessage(MigrationReportMessage message)
            {
                if (message.Type == BuildMessageType.Error || message.Type == BuildMessageType.Warning)
                {
                    if (message.Type == BuildMessageType.Error)
                    {
                        _errors++;
                    }
                    else
                    {
                        _warnings++;
                    }

                    _messages.Add(string.Format("{0}: {1}", message.Type, message.Message));
                }
            }
        }
    }
}