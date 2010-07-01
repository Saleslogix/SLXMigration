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
using Sage.SalesLogix.Migration.Forms.LegacyBuilders;
using Sage.SalesLogix.Migration.Forms.Services;
using Sage.SalesLogix.Migration.Services;
using Sage.SalesLogix.Plugins;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms
{
    public sealed class LegacyFormMigrationSupport : IMigrationSupport
    {
        private WorkItem _workItem;
        private IProjectContextService _projectContext;
        private MigrationContext _context;
        private IDataPathTranslationService _dataPathTranslator;
        private IFormSimplificationService _componentSimplifier;
        private IVisibilityDeterminationService _visibilityDeterminer;
        private IFormFlatteningService _formFlattener;
        private IControlAlignmentService _controlAligner;
        private IFormLayoutService _formLayout;
        private IHierarchyNodeService _hierarchyNodeService;

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
        public IFormSimplificationService ComponentSimplifier
        {
            set { _componentSimplifier = value; }
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
                    if (plugin.Type == PluginType.LegacyForm)
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
        }

        public void Build()
        {
            using (_context.Status.BeginStep("Building forms...", _context.Forms.Count*2))
            {
                foreach (FormInfo form in _context.Forms.Values)
                {
                    if (form.IsLegacy)
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
                    if (form.IsLegacy)
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
                    if (form.IsLegacy)
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

            using (DelphiBinaryReader binaryReader = new DelphiBinaryReader(new MemoryStream(plugin.Blob.Data)))
            {
                binaryReader.BaseStream.Position = 3;
                while (binaryReader.Read() != 0) {}
                binaryReader.BaseStream.Position += 6;
                component = binaryReader.ReadComponent(true);
                Debug.Assert(binaryReader.BaseStream.Position == binaryReader.BaseStream.Length, "Expected end of stream");
            }

            _componentSimplifier.Simplify(component);
            _visibilityDeterminer.Determine(component);
            _formFlattener.Flatten(component, true);

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

            foreach (DelphiComponent childComponent in component.Components)
            {
                ParseChildComponent(form, childComponent, isDataForm);
            }

            _context.Forms.Add(form.FullName, form);

            string systemButtons;
            byte[] whenItems;

            if (component.TryGetPropertyValue("Buttons.SystemButtons", out systemButtons))
            {
                switch (systemButtons)
                {
                    case "wbsOkCancelHelp":
                        //extra buttons
                        break;
                    case "wbsCloseHelp":
                        //extra buttons
                        break;
                    case "wbsNone":
                        //extra buttons
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
            }

            if (component.TryGetPropertyValue("Buttons.WhenItems", out whenItems))
            {
                DelphiComponent buttonsComponent;

                using (DelphiBinaryReader binaryReader = new DelphiBinaryReader(whenItems))
                {
                    buttonsComponent = binaryReader.ReadComponent(true);
                    Debug.Assert(binaryReader.BaseStream.Position == binaryReader.BaseStream.Length);
                }

                _componentSimplifier.Simplify(buttonsComponent);

                foreach (DelphiComponent buttonComponent in buttonsComponent.Components) {}
            }

            //TODO: add support for Enable Basic
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

            QFControlsList hiddenControls = new QFControlsList();

            foreach (ControlInfo control in form.Controls)
            {
                if (!control.IsExcluded)
                {
                    if (control.Builder != null)
                    {
                        control.QfControl = control.Builder.Construct();
                    }

                    if (!control.IsVisible)
                    {
                        hiddenControls.Controls.Add(control.QfControl);
                        control.QfControl.QuickFormDefinition = quickForm;
                    }
                    else
                    {
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
                    }

                    if (control.Builder != null)
                    {
                        control.Builder.Build();
                    }

                    if (!string.IsNullOrEmpty(control.Caption))
                    {
                        control.QfControl.Caption = control.Caption;
                    }

                    if (!string.IsNullOrEmpty(control.Hint))
                    {
                        control.QfControl.ToolTip = control.Hint;
                    }
                }
            }

            _formLayout.Process(form);

            //since a caption can be changed by an adjacent label merge
            foreach (QuickFormElement element in quickForm.Elements)
            {
                element.DisplayLabel = element.Control.Caption;
            }

            if (hiddenControls.Controls.Count > 0)
            {
                hiddenControls.ControlId = "HiddenControls";
                int row = quickForm.Rows.Count;
                quickForm.Rows.Add(new RowStyle(SizeType.Absolute, 35));
                QuickFormElement element = new QuickFormNotMappedElement(quickForm, hiddenControls);
                quickForm.Elements.Add(element);
                hiddenControls.Row = row;
                hiddenControls.ColumnSpan = quickForm.Columns.Count;
            }
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

        private void ParseChildComponent(FormInfo form, DelphiComponent component, bool dataForm)
        {
            string legacyType = component.Type;
            IDictionary<object, DataPath> bindings = null;

            if (dataForm)
            {
                DelphiList dataPaths;

                if (component.TryGetPropertyValue("DataPaths.Strings", out dataPaths))
                {
                    bindings = ParseDataBindings(dataPaths);
                }
            }

            int left;
            int top;
            int width;
            int height;
            string hint;
            string caption;
            component.TryGetPropertyValue("Left", out left);
            component.TryGetPropertyValue("Top", out top);
            component.TryGetPropertyValue("Width", out width);
            component.TryGetPropertyValue("Height", out height);
            component.TryGetPropertyValue("Hint", out hint);
            bool visible = (!component.TryGetPropertyValue("Visible", out visible) || visible);
            string labelCaption;

            if (!(component.TryGetPropertyValue("Caption", out caption) && !string.IsNullOrEmpty(caption)) &&
                (component.TryGetPropertyValue("LabelProps.Caption", out labelCaption) && !string.IsNullOrEmpty(labelCaption)))
            {
                int labelWidth;
                int labelHeight;
                int labelGap;
                string labelPosition;

                if (!component.TryGetPropertyValue("LabelProps.Width", out labelWidth))
                {
                    labelWidth = 64;
                }

                if (!component.TryGetPropertyValue("LabelProps.Height", out labelHeight))
                {
                    labelHeight = 15;
                }

                if (!component.TryGetPropertyValue("LabelProps.Gap", out labelGap))
                {
                    labelGap = 8;
                }

                if (visible && component.TryGetPropertyValue("LabelProps.Position", out labelPosition) && labelPosition != "lpLeft")
                {
                    int labelLeft = -1;
                    int labelTop = -1;

                    switch (labelPosition)
                    {
                        case "lpAbove":
                            labelLeft = left;
                            labelTop = top - labelHeight - labelGap;
                            break;
                        case "lpRight":
                            labelLeft = left + width + labelGap;
                            labelTop = top;
                            break;
                        case "lpBelow":
                            labelLeft = left;
                            labelTop = top + height + labelGap;
                            break;
                        default:
                            LogWarning("Unexpected '{0}' LabelProps.Position value", labelPosition);
                            break;
                    }

                    if (labelLeft >= 0 && labelTop >= 0)
                    {
                        ControlInfo labelControl = new ControlInfo("TSLLabel", null, labelLeft, labelTop, labelWidth, labelHeight, true, labelCaption, null)
                                                       {
                                                           QfControl = new QFLabel {ControlId = component.Name + "Label"}
                                                       };
                        form.Controls.Add(labelControl);
                    }
                }
                else
                {
                    caption = labelCaption;
                    int extraWidth = labelWidth + labelGap;
                    left -= extraWidth;
                    width += extraWidth;
                }
            }

            ControlInfo control = new ControlInfo(legacyType, bindings, left, top, width, height, visible, FormatUtils.ConvertCaption(caption), hint);
            ControlBuilder builder = ControlBuilder.CreateBuilder(legacyType, component);

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

            form.Controls.Add(control);
        }

        private IDictionary<object, DataPath> ParseDataBindings(DelphiList dataPaths)
        {
            IDictionary<object, DataPath> bindings = new Dictionary<object, DataPath>();

            foreach (string dataPath in dataPaths)
            {
                //unusual case
                string str = dataPath.Replace("\b", ":");

                string[] parts = str.Split(':');
                Debug.Assert(parts.Length == 5);
                Debug.Assert(parts[1] == "V");
                Debug.Assert(parts[4] == string.Empty);
                DataPath bindingPath = null;

                try
                {
                    bindingPath = DataPath.Parse(string.Format("{0}:{1}", parts[2], parts[3]));
                }
                catch (FormatException)
                {
                    LogError("Unable to parse '{0}' colunn data path", dataPath);
                }

                if (bindingPath != null)
                {
                    if (bindings.ContainsKey(parts[0]))
                    {
                        LogWarning("A binding for property '{0}' already exists", parts[0]);
                    }
                    else
                    {
                        bindings.Add(parts[0], bindingPath);
                    }
                }
            }

            return bindings;
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