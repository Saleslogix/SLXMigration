using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Sage.Platform.Application;
using Sage.Platform.Controls;
using Sage.Platform.Projects.Interfaces;
using Sage.Platform.QuickForms;
using Sage.Platform.QuickForms.ActionItems;
using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.Elements;
using Sage.Platform.QuickForms.QFControls;
using Sage.SalesLogix.LegacyBridge.Delphi;
using Sage.SalesLogix.Migration.Forms.Builders;
using Sage.SalesLogix.Migration.Forms.Services;
using Sage.SalesLogix.Migration.Services;
using Sage.SalesLogix.Plugins;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms
{
    public sealed class FormMigrationSupport : IMigrationSupport
    {
        private WorkItem _workItem;
        private IProjectContextService _projectContext;
        private MigrationContext _context;
        private IDataPathTranslationService _dataPathTranslator;
        private IFormSimplificationService _formSimplifier;
        private IVisibilityDeterminationService _visibilityDeterminer;
        private IFormFlatteningService _formFlattener;
        private IControlAlignmentService _controlAligner;
        private IFormLayoutService _formLayout;
        private IHierarchyNodeService _hierarchyNodeService;
        private ActiveXTypeImporter _typeImporter;

        [ServiceDependency]
        public WorkItem WorkItem
        {
            set { _workItem = value; }
        }

        [ServiceDependency]
        public IProjectContextService ProjectContext
        {
            set { _projectContext = value; }
        }

        [ServiceDependency]
        public IMigrationContextHolderService ContextHolder
        {
            set
            {
                value.ContextChanged += delegate
                    {
                        _context = value.Context;
                    };
                _context = value.Context;
            }
        }

        [ServiceDependency]
        public IDataPathTranslationService DataPathTranslator
        {
            set { _dataPathTranslator = value; }
        }

        [ServiceDependency]
        public IFormSimplificationService FormSimplifier
        {
            set { _formSimplifier = value; }
        }

        [ServiceDependency]
        public IVisibilityDeterminationService VisibilityDeterminer
        {
            set { _visibilityDeterminer = value; }
        }

        [ServiceDependency]
        public IFormFlatteningService FormFlattener
        {
            set { _formFlattener = value; }
        }

        [ServiceDependency]
        public IControlAlignmentService ControlAligner
        {
            set { _controlAligner = value; }
        }

        [ServiceDependency]
        public IFormLayoutService FormLayout
        {
            set { _formLayout = value; }
        }

        [ServiceDependency]
        public IHierarchyNodeService HierarchyNodeService
        {
            set { _hierarchyNodeService = value; }
        }

        #region IMigrationSupport Members

        public void Parse()
        {
            using (_context.Status.BeginStep("Parsing forms...", _context.Plugins.Count))
            {
                foreach (Plugin plugin in _context.Plugins)
                {
                    if (plugin.Type == PluginType.ActiveForm)
                    {
                        _context.Log.SourcePlugin = plugin;
                        LogInfo(false, "Parsing '{0}' form", plugin);
                        Parse(plugin);
                        _context.Log.SourcePlugin = null;
                    }

                    if (!_context.Status.Advance())
                    {
                        break;
                    }
                }
            }

            _typeImporter = null;
        }

        public void Build()
        {
            using (_context.Status.BeginStep("Building forms...", _context.Forms.Count*2))
            {
                foreach (FormInfo form in _context.Forms.Values)
                {
                    if (!form.IsLegacy)
                    {
                        _context.Log.SourcePlugin = form.Plugin;
                        LogInfo(false, "Building '{0}' form", form);
                        Build(form);
                        _context.Log.SourcePlugin = null;
                    }

                    if (!_context.Status.Advance())
                    {
                        break;
                    }
                }

                foreach (FormInfo form in _context.Forms.Values)
                {
                    if (!form.IsLegacy)
                    {
                        _context.Log.SourcePlugin = form.Plugin;
                        LogInfo(false, "Post building '{0}' form", form);
                        PostBuild(form);
                        _context.Log.SourcePlugin = null;
                    }

                    if (!_context.Status.Advance())
                    {
                        break;
                    }
                }
            }
        }

        public void Persist()
        {
            using (_context.Status.BeginStep("Persisting forms...", _context.Forms.Count))
            {
                foreach (FormInfo form in _context.Forms.Values)
                {
                    if (!form.IsLegacy)
                    {
                        _context.Log.SourcePlugin = form.Plugin;
                        LogInfo(false, "Persisting '{0}' form", form);
                        Persist(form);
                        _context.Log.SetGeneratedItemMapping(form.Plugin, form.QuickForm.Url);
                        LogInfo(true, "Item successfully migrated");
                        _context.Log.SourcePlugin = null;
                    }

                    if (!_context.Status.Advance())
                    {
                        break;
                    }
                }
            }
        }

        #endregion

        private void Parse(Plugin plugin)
        {
            DelphiComponent component;

            using (DelphiBinaryReader binaryReader = new DelphiBinaryReader(new MemoryStream(BorlandUtils.ObjectTextToBinary(plugin.Blob.Data))))
            {
                component = binaryReader.ReadComponent(true);
            }

            _formSimplifier.Simplify(component);
            //_visibilityDeterminer.Determine(component); - don't
            //_formFlattener.Flatten(component, false);   - don't

            string baseTable = plugin.DataCode;
            bool isDataForm = !string.IsNullOrEmpty(baseTable);

            if (!isDataForm)
            {
                baseTable = _context.Settings.MainTable;
            }

            _dataPathTranslator.RegisterTable(baseTable);

            int width;
            int height;
            string dialogCaption;
            string tabCaption = (!string.IsNullOrEmpty(plugin.DisplayName)
                                     ? plugin.DisplayName
                                     : plugin.Name);
            component.TryGetPropertyValue("Width", out width);
            component.TryGetPropertyValue("Height", out height);
            component.TryGetPropertyValue("Caption", out dialogCaption);
            FormInfo form = new FormInfo(plugin, baseTable, isDataForm, dialogCaption, tabCaption, width, height);
            IDictionary<string, FormField> formFields = new Dictionary<string, FormField>(StringComparer.InvariantCultureIgnoreCase);

            foreach (DelphiComponent childComponent in component.Components)
            {
                ParseChildComponent(form, form.Controls, childComponent, isDataForm, formFields);
            }

            _context.Forms.Add(form.FullName, form);

            string script;

            if (_context.Settings.ProcessScripts && component.TryGetPropertyValue("Script", out script) && !string.IsNullOrEmpty(script))
            {
                ScriptInfo scriptInfo = new ScriptInfo(plugin, script, component.Name, formFields);
                _context.Scripts.Add(scriptInfo.PrefixedFullName, scriptInfo);
            }
        }

        private void BuildControl(IEntityQuickFormDefinition quickForm, ControlInfo control)
        {
            if (!control.IsExcluded)
            {
                if (control.QfControl == null) control.QfControl = control.Builder.Construct(); //could be already set

                QuickFormElement element = new QuickFormNotMappedElement(quickForm, control.QfControl);

                if (control.IsTool)
                {
                    if (quickForm.ToolElements.Count == 0)
                    {
                        quickForm.ToolElements.Add(new QuickFormNotMappedElement(quickForm, new QFElementSpacer()));
                        quickForm.ToolElements.Add(new QuickFormNotMappedElement(quickForm, new QFElementSpacer()));
                    }

                    quickForm.ToolElements.Add(element);
                }
                else
                {
                    quickForm.Elements.Add(element);
                }

                control.Builder.Build();

                if (!string.IsNullOrEmpty(control.Caption))
                {
                    control.QfControl.Caption = control.Caption;
                }

                if (!string.IsNullOrEmpty(control.Hint))
                {
                    control.QfControl.ToolTip = control.Hint;
                }

                if (control.QfControl is IQuickFormsControlContainer)
                {
                    bool hasVisibleControls = false;
                    foreach (ControlInfo childControl in control.Controls)
                    {
                        BuildControl(quickForm, childControl);
                        hasVisibleControls = hasVisibleControls || (!(childControl.IsExcluded || childControl.IsTool));
                    }
                    //now that we have processed all child controls, see if this container control even needs to be visible
                    control.IsExcluded = !hasVisibleControls;
                    if (control.IsExcluded)
                    {
                        quickForm.Elements.Remove(element); //remove self from the list
                        LogWarning("Container control {0} of type {1} is excluded since it does not contain any controls", new object[] { control.QfControl.ControlId, control.LegacyType });
                    }
                }
            }
        }

        private void PostBuildControl(IEntityQuickFormDefinition quickForm, ControlInfo control)
        {
            if (!control.IsExcluded)
            {
                control.Builder.PostBuild();
                foreach (ControlInfo childControl in control.Controls)
                {
                    PostBuildControl(quickForm, childControl);
                }
            }
        }
    

        private void Build(FormInfo form)
        {
            form.Entity = _context.Entities[form.BaseTable];
            Type entityType = EntityTypeService.GenerateEntityType(
                "Sage.Entity.Interfaces",
                "Sage.Entity.Interfaces",
                form.Entity.InterfaceName);

            IEntityQuickFormDefinition quickForm = QuickFormModel.CreateMainDetailViewDefinition(form.Entity, entityType, form.SafeFullName);
            quickForm.UseEntityNameAsTitle = false;
            quickForm.Elements.Clear();
            form.QuickForm = quickForm;

            _controlAligner.Align(form.Controls);

            //QFControlsList hiddenControls = new QFControlsList();

            //build teh controls
            foreach (ControlInfo control in form.Controls)
            {
                BuildControl(quickForm, control);
            }

            //give the builders a chnace to rearrange the controls
            foreach (ControlInfo control in form.Controls)
            {
                PostBuildControl(quickForm, control);
            }

            _formLayout.Process(form);

            //since a caption can be changed by an adjacent label merge
            foreach (QuickFormElement element in quickForm.Elements)
            {
                element.DisplayLabel = element.Control.Caption;
            }

            /*if (hiddenControls.Controls.Count > 0)
            {
                int row = quickForm.Rows.Count;
                quickForm.Rows.Add(new RowStyle(SizeType.Absolute, 35));
                QuickFormElement element = new QuickFormNotMappedElement(quickForm, hiddenControls);
                quickForm.Elements.Add(element);
                hiddenControls.Row = row;
                hiddenControls.Column = 0;
                hiddenControls.RowSpan = 1;
                hiddenControls.ColumnSpan = quickForm.Columns.Count + 1;
            }*/
        }

        private void PostBuild(FormInfo form)
        {
            if (form.HasDeleteButton)
            {
                CreateDeleteButton(form.QuickForm);
            }

            if (form.HasSaveButton)
            {
                CreateSaveButton(form.QuickForm);
            }

            if (form.HasGroupNavigator)
            {
                CreateGroupNavigator(form.QuickForm);
            }
        }

        private void Persist(FormInfo form)
        {
            QuickFormModel model = _projectContext.ActiveProject.Models.Get<QuickFormModel>();

            foreach (IQuickFormDefinition item in model.LoadDefinitions(form.Entity))
            {
                if (StringUtils.CaseInsensitiveEquals(item.Name, form.SafeFullName))
                {
                    //ensure that the new form manifest item will match the old one
                    //form.QuickForm.QuickFormsDefinitionData.Id = item.QuickFormsDefinitionData.Id;
                    item.Delete();
                }
            }

            form.QuickForm.Validate();
            form.QuickForm.Save();
            _hierarchyNodeService.InsertQuickFormNode(form.Entity, form.QuickForm);
        }

        private void ParseChildComponent(FormInfo form, IList<ControlInfo> formControls, DelphiComponent component, bool dataForm, IDictionary<string, FormField> formFields)
        {
            string cGuid;

            //we will set it below for the AX control, but there
            //cxan be cases when control will be null. 
            //E.g. when we want to skip the TTabSheetEx control and directly process the controls
            ControlInfo control = null; 

            if (component.TryGetPropertyValue("CGUID", out cGuid) && !string.IsNullOrEmpty(component.Name)) //AX controls only
            {
                Type legacyType = StandardControls.LookupType(cGuid);

                if (legacyType == null)
                {
                    if (_typeImporter == null)
                    {
                        _typeImporter = new ActiveXTypeImporter(
                            Path.Combine(_context.Settings.OutputDirectory, "lib"),
                            _context.KeyPair,
                            _context.References);
                    }

                    legacyType = _typeImporter.ImportCGuid(cGuid) ?? typeof (object);
                }

                IDictionary<object, DataPath> bindings = null;

                if (dataForm)
                {
                    byte[] dataBindings;

                    if (component.TryGetPropertyValue("DataBindings", out dataBindings))
                    {
                        bindings = ParseDataBindings(dataBindings);
                    }
                }

                int left;
                int top;
                int width;
                int height;
                string caption;
                string hint;
                component.TryGetPropertyValue("Left", out left);
                component.TryGetPropertyValue("Top", out top);
                component.TryGetPropertyValue("Width", out width);
                component.TryGetPropertyValue("Height", out height);
                bool visible = (!component.TryGetPropertyValue("Visible", out visible) || visible);
                component.TryGetPropertyValue("Caption", out caption);
                component.TryGetPropertyValue("Hint", out hint);
                control = new ControlInfo(legacyType.Name, bindings, left, top, width, height, visible, FormatUtils.ConvertCaption(caption), hint);
                ControlBuilder builder = ControlBuilder.CreateBuilder(legacyType.Name, component);

                if (builder != null)
                {
                    _workItem.BuildTransientItem(builder);
                    control.Builder = builder;
                    builder.Initialize(form, control);

                    if (!control.IsExcluded)
                    {
                        builder.ExtractSchemaHints();
                    }
                }
                else
                {
                    LogWarning("Legacy control type '{0}' not supported", legacyType);
                    control.IsExcluded = true;
                }

                formControls.Add(control);
                formFields.Add(component.Name, new FormField(component.Name, legacyType));
            }

            //process child component
            foreach (DelphiComponent childComponent in component.Components)
            {
                if (control == null)
                {
                    //we are skipping a level - e.g. when we are processing TTabSheetEx controls - all the chld controls will be
                    //added to the parent MultiTab control, which will then sort these controls into separate tabs.
                    ParseChildComponent(form, formControls, childComponent, dataForm, formFields);
                }
                else
                {
                    ParseChildComponent(form, control.Controls, childComponent, dataForm, formFields);
                }
            }
        }

        private IDictionary<object, DataPath> ParseDataBindings(byte[] data)
        {
            IDictionary<int, string> strings = ParseNumberedStringList(data);
            IDictionary<object, DataPath> bindings = new Dictionary<object, DataPath>(strings.Count);

            foreach (KeyValuePair<int, string> str in strings)
            {
                DataPath dataPath = null;

                try
                {
                    dataPath = DataPath.Parse(str.Value);
                }
                catch (FormatException)
                {
                    LogError("Unable to parse '{0}' binding string", str.Value);
                }

                if (dataPath != null)
                {
                    bindings.Add(str.Key, dataPath);
                }
            }

            return bindings;
        }

        private static IDictionary<int, string> ParseNumberedStringList(byte[] data)
        {
            using (DelphiBinaryReader binaryReader = new DelphiBinaryReader(data))
            {
                int count = binaryReader.ReadInteger();
                IDictionary<int, string> values = new Dictionary<int, string>(count);

                for (int i = 0; i < count; i++)
                {
                    values.Add(binaryReader.ReadInteger(), binaryReader.ReadValue().ToString());
                }

                Debug.Assert(binaryReader.BaseStream.Position == binaryReader.BaseStream.Length, "Expected end of stream");
                return values;
            }
        }

        private static void CreateGroupNavigator(IQuickFormDefinition quickForm)
        {
            CreateToolElement<QFSLXGroupNavigator>(quickForm, "GroupNavigator");
        }

        private static void CreateSaveButton(IQuickFormDefinition quickForm)
        {
            QFButton button = CreateToolElement<QFButton>(quickForm, "SaveButton");
            button.Caption = "Save";
            button.ToolTip = "Save";
            button.ButtonType = ButtonType.Icon;
            button.Image = "[Localization!Global_Images:Save_16x16]";

            BusinessRuleActionItem action = new BusinessRuleActionItem();
            button.OnClickAction.Action = action;
            action.BusinessRule = "Save";
            button.OnClickAction.IsDialogCloseAction = true;
        }

        private static void CreateDeleteButton(IQuickFormDefinition quickForm)
        {
            QFButton button = CreateToolElement<QFButton>(quickForm, "DeleteButton");
            button.Caption = "Delete";
            button.ToolTip = "Delete";
            button.ButtonType = ButtonType.Icon;
            button.Image = "[Localization!Global_Images:Delete_16x16]";

            BusinessRuleActionItem action = new BusinessRuleActionItem();
            button.OnClickAction.Action = action;
            action.BusinessRule = "Delete";

            RedirectActionItem redirectAction = new RedirectActionItem();
            action.OnCompleteActionItem = redirectAction;
            redirectAction.MainViewEntityName = action.ObjectName;
            redirectAction.EntityViewMode = enumEntityViewMode.List;
            redirectAction.UseCurrentIdInLink = false;
        }

        private static T CreateToolElement<T>(IQuickFormDefinition quickForm, string controlId)
            where T : IQuickFormsControl, new()
        {
            T button = new T {ControlId = controlId};
            QuickFormElement element = new QuickFormNotMappedElement(quickForm, button);

            if (quickForm.ToolElements.Count == 0)
            {
                quickForm.ToolElements.Add(new QuickFormNotMappedElement(quickForm, new QFElementSpacer()));
                quickForm.ToolElements.Add(new QuickFormNotMappedElement(quickForm, new QFElementSpacer()));
            }

            quickForm.ToolElements.Insert(2, element);
            return button;
        }

        private void LogInfo(bool persist, string text, params object[] args)
        {
            if (_context != null && _context.Log != null)
            {
                _context.Log.Info(persist, text, args);
            }
        }

        private void LogWarning(string text, params object[] args)
        {
            if (_context != null && _context.Log != null)
            {
                _context.Log.Warn(text, args);
            }
        }

        private void LogError(string text, params object[] args)
        {
            if (_context != null && _context.Log != null)
            {
                _context.Log.Error(text, args);
            }
        }
    }
}
