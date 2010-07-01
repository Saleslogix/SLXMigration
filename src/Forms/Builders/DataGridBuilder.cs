using System;
using System.Collections.Generic;
using Sage.Platform.Application;
using Sage.Platform.Controls;
using Sage.Platform.QuickForms.ActionItems;
using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.Elements;
using Sage.Platform.QuickForms.QFControls;
using Sage.SalesLogix.LegacyBridge.Delphi;
using Sage.SalesLogix.Migration.Forms.Builders.Columns;
using Sage.SalesLogix.QuickForms.QFControls;
using Sage.SalesLogix.QuickForms.QFControls.DataGrid;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMapping("AxDataGrid")]
    public sealed class DataGridBuilder : ControlBuilder
    {
        private const int BindIdBindingCode = 3;

        private WorkItem _workItem;

        [ServiceDependency]
        public WorkItem WorkItem
        {
            set { _workItem = value; }
        }

        private string _tableName;
        private DataPath _bindIdBindingPath;
        private DataPath _bindConditionPath;
        private DataPath _columnPrefixPath;
        private IList<ColumnBuilder> _columnBuilders;

        protected override void OnInitialize()
        {
            //don't! - if it is not bound, it is the user's problem, but let 'em fix it later
            Control.IsExcluded = (Control.Bindings == null || !Control.Bindings.TryGetValue(BindIdBindingCode, out _bindIdBindingPath));
            if (Control.IsExcluded)
            {
                LogWarning("Grid {0} is excluded since it is not bound", new object[] { _component.Name});
            }
        }

        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFDataGrid();
        }

        protected override void OnExtractSchemaHints()
        {
            if (Component.TryGetPropertyValue("TableName", out _tableName) && !string.IsNullOrEmpty(_tableName))
            {
                DataPathTranslator.RegisterTable(_tableName);
            }

            string bindCondition;

            if (Component.TryGetPropertyValue("BindCondition", out bindCondition) && !string.IsNullOrEmpty(bindCondition))
            {
                try
                {
                    _bindConditionPath = DataPath.Parse(bindCondition);
                }
                catch (FormatException)
                {
                    LogError("Unable to parse '{0}' bind condition", bindCondition);
                }

                if (_bindConditionPath != null)
                {
                    if (_bindIdBindingPath != null)
                    {
                        DataPathTranslator.RegisterJoin(_bindIdBindingPath, _bindConditionPath);
                    }

                    _columnPrefixPath = _bindConditionPath.Reverse();
                    DataPathTranslator.RegisterField(_columnPrefixPath);
                }
            }

            _columnBuilders = new List<ColumnBuilder>(Component.Components.Count);

            foreach (DelphiComponent columnComponent in Component.Components)
            {
                bool visible;

                if (!columnComponent.TryGetPropertyValue("Visible", out visible) || visible)
                {
                    ColumnBuilder builder = ColumnBuilder.CreateBuilder(columnComponent);

                    if (builder != null)
                    {
                        _workItem.BuildTransientItem(builder);
                        builder.ExtractSchemaHints();
                        _columnBuilders.Add(builder);
                    }
                    else
                    {
                        LogWarning("Legacy column type '{0}' not supported", columnComponent.Type);
                    }
                }
            }
        }

        protected override void OnBuild()
        {
            QFDataGrid dataGrid = (QFDataGrid) QfControl;
            dataGrid.EmptyTableRowText = "No records match the selection criteria.";

            foreach (ColumnBuilder builder in _columnBuilders)
            {
                IQFDataGridCol col = builder.Construct();
                dataGrid.Columns.Add(col);
                builder.Build(_columnPrefixPath);
            }

            bool sortable;
            dataGrid.EnableSorting = (Component.TryGetPropertyValue("Sortable", out sortable) && sortable);

            string editView;
            FormInfo editForm;

            if (Component.TryGetPropertyValue("EditView", out editView) &&
                Context.Forms.TryGetValue(editView.Replace(':', '_'), out editForm))
            {
                QFEditCol editCol = new QFEditCol();
                dataGrid.Columns.Add(editCol);
                editCol.Text = "Edit";

                DialogActionItem editAction = editCol.DialogSpecs;
                editAction.SmartPart = editForm.SmartPartId;
                editAction.Width = editForm.Width;
                editAction.Height = editForm.Height;
                editAction.TitleOverride = "Edit " + editForm.DialogCaption;

                editForm.HasSaveButton = true;

                if (Form.QuickForm.ToolElements.Count == 0)
                {
                    Form.QuickForm.ToolElements.Add(new QuickFormNotMappedElement(Form.QuickForm, new QFElementSpacer()));
                    Form.QuickForm.ToolElements.Add(new QuickFormNotMappedElement(Form.QuickForm, new QFElementSpacer()));
                }

                QFButton addButton = new QFButton();
                addButton.ControlId = Component.Name + "AddButton";
                Form.QuickForm.ToolElements.Add(new QuickFormNotMappedElement(Form.QuickForm, addButton));
                addButton.ButtonType = ButtonType.Icon;
                addButton.Image = "[Localization!Global_Images:plus_16x16]";

                InsertChildDialogActionItem addAction = new InsertChildDialogActionItem();
                addButton.OnClickAction.Action = addAction;
                addAction.SmartPart = editForm.SmartPartId;
                addAction.Width = editForm.Width;
                addAction.Height = editForm.Height;
                addAction.TitleOverride = "Add " + editForm.DialogCaption;
                addAction.DataSource = Component.Name + "DS";

                if (_bindIdBindingPath != null)
                {
                    string propertyString = null;

                    try
                    {
                        propertyString = DataPathTranslator.TranslateField(_bindIdBindingPath);
                    }
                    catch (MigrationException ex)
                    {
                        LogError(ex.Message);
                    }

                    addAction.ParentRelationshipPropertyName = propertyString;
                }

                Form.DialogForms.Add(editForm);
            }

            bool showDelete;

            if (Component.TryGetPropertyValue("EditViewShowDelete", out showDelete) && showDelete)
            {
                QFDeleteCol deleteCol = new QFDeleteCol();
                dataGrid.Columns.Add(deleteCol);

                string deleteCaption;

                if (!Component.TryGetPropertyValue("EditViewDeleteCaption", out deleteCaption))
                {
                    deleteCaption = "Delete";
                }

                deleteCol.Text = deleteCaption;
                deleteCol.ConfirmationMessage = "Are you sure you want to remove this item?";
            }

            QFDataSource dataSource = new QFDataSource();
            dataSource.ControlId = Component.Name + "DS";
            Form.QuickForm.Elements.Add(new QuickFormNotMappedElement(Form.QuickForm, dataSource));

            try
            {
                dataSource.EntityTypeName = DataPathTranslator.TranslateTable(_tableName);
            }
            catch (MigrationException ex)
            {
                LogError(ex.Message);
            }

            string getByProperty = null;

            if (_bindIdBindingPath != null && _bindConditionPath != null)
            {
                try
                {
                    getByProperty = DataPathTranslator.TranslateCollection(_bindIdBindingPath, _bindConditionPath.TargetTable, _bindConditionPath.TargetField);
                }
                catch (MigrationException ex)
                {
                    LogError(ex.Message);
                }
            }

            //workaround: cannot persist a data grid without this property set
            if (string.IsNullOrEmpty(getByProperty))
            {
                getByProperty = "__dummy";
                LogError("Unable to translate BindId '{0}' into GetByProperty", _bindIdBindingPath);
            }

            dataSource.GetByProperty = getByProperty;
            dataSource.IsCollection = true;
            dataGrid.DataSource = dataSource.ControlId;
        }
    }
}