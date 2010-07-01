using System;
using System.Collections;
using System.Collections.Generic;
using Sage.Platform.Controls;
using Sage.Platform.QuickForms.ActionItems;
using Sage.Platform.QuickForms.Controls;
using Sage.Platform.QuickForms.Elements;
using Sage.Platform.QuickForms.QFControls;
using Sage.SalesLogix.LegacyBridge.Delphi;
using Sage.SalesLogix.QuickForms.QFControls;
using Sage.SalesLogix.QuickForms.QFControls.DataGrid;

namespace Sage.SalesLogix.Migration.Forms.LegacyBuilders
{
    [BuilderMapping("TSLDataGrid")]
    public sealed class DataGridBuilder : ControlBuilder
    {
        private string _tableName;
        private DataPath _bindIdBindingPath;
        private DataPath _bindConditionPath;
        private DataPath _columnPrefixPath;
        private IList<ColumnDefinition> _columns;
        private IList<DataPath> _dataPaths;

        protected override void OnInitialize()
        {
            Control.IsExcluded = (Control.Bindings == null ||
                                  !Control.Bindings.TryGetValue("BindID", out _bindIdBindingPath) ||
                                  Component.Components.Count == 0);
        }

        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFDataGrid();
        }

        protected override void OnExtractSchemaHints()
        {
            DelphiComponent query = Component.Components[0];

            if (query.TryGetPropertyValue("MainTable", out _tableName) && !string.IsNullOrEmpty(_tableName))
            {
                DataPathTranslator.RegisterTable(_tableName);
            }

            string bindCondition;

            if (query.TryGetPropertyValue("BindCondition", out bindCondition) && !string.IsNullOrEmpty(bindCondition))
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

            _columns = new List<ColumnDefinition>();
            _dataPaths = new List<DataPath>();

            IEnumerable layouts;

            if (query.TryGetPropertyValue("Layouts.Strings", out layouts))
            {
                foreach (string layout in layouts)
                {
                    ColumnDefinition column = ColumnDefinition.Parse(layout);

                    if (column.Visible)
                    {
                        DataPath dataPath = null;

                        try
                        {
                            dataPath = DataPath.Parse(column.Binding);
                        }
                        catch (FormatException)
                        {
                            LogError("Unable to parse '{0}' column binding string", column.Binding);
                        }

                        if (dataPath != null)
                        {
                            DataPathTranslator.RegisterField(dataPath);
                            _columns.Add(column);
                            _dataPaths.Add(dataPath);
                        }
                    }
                }
            }
        }

        protected override void OnBuild()
        {
            QFDataGrid dataGrid = (QFDataGrid) QfControl;
            dataGrid.EmptyTableRowText = "No records match the selection criteria.";

            for (int i = 0; i < _columns.Count; i++)
            {
                ColumnDefinition column = _columns[i];
                DataPath dataPath = _dataPaths[i];
                string propertyString = null;

                try
                {
                    propertyString = DataPathTranslator.TranslateField(dataPath, _columnPrefixPath);
                }
                catch (MigrationException ex)
                {
                    LogError(ex.Message);
                }

                if (propertyString != null)
                {
                    IQFDataGridCol col;

                    switch (column.FormatType)
                    {
                        case FormatType.DateTime:
                            col = new QFDateTimePickerCol();
                            break;
                        case FormatType.Currency:
                            col = new QFSLXCurrencyCol();
                            break;
                        case FormatType.User:
                            col = new QFSLXUserCol();
                            break;
                        case FormatType.Phone:
                            col = new QFPhoneCol();
                            break;
                        case FormatType.Owner:
                            col = new QFSLXOwnerCol();
                            break;
                        case FormatType.Boolean:
                            col = new QFCheckBoxCol();
                            break;
                        case FormatType.PickListItem:
                            col = new QFPickListCol();
                            break;
                        case FormatType.None:
                        case FormatType.Fixed:
                        case FormatType.Integer:
                        case FormatType.Percent:
                        case FormatType.PositiveInteger:
                        case FormatType.TimeZone:
                        default:
                            col = new QFDataGridCol();
                            break;
                    }

                    col.DataField = propertyString ?? string.Empty;
                    dataGrid.Columns.Add(col);
                    col.ColumnHeading = column.Caption;
                }
            }

            bool sortable;
            dataGrid.EnableSorting = (Component.TryGetPropertyValue("AllowResort", out sortable) && sortable);

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

            if (Component.TryGetPropertyValue("MenuDeleteVisible", out showDelete) && showDelete)
            {
                QFDeleteCol deleteCol = new QFDeleteCol();
                dataGrid.Columns.Add(deleteCol);

                string deleteCaption;

                if (!Component.TryGetPropertyValue("MenuDeleteCaption", out deleteCaption))
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