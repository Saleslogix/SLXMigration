using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sage.Platform.Controls;
using Sage.Platform.Orm.CodeGen;
using Sage.Platform.Orm.Entities;
using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.HighLevelTypes;
using Sage.SalesLogix.Migration.Orm;
using Sage.SalesLogix.QuickForms.QFControls;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    [BuilderMapping("AxLookupEdit")]
    public sealed class LookupBuilder : ControlBuilder
    {
        private const int LookupIdBindingCode = 30;
        private new const int TextBindingCode = 43;

        private OrmLookup _lookup;
        private IList<ColumnDefinition> _columns;
        private IList<DataPath> _dataPaths;
        private DataPath _lookupIdBindingPath;
        private DataPath _idDataPath;
        private DataPath _nameDataPath;
        private DataPath _columnPrefixPath;
        private bool _isObject;

        protected override QuickFormsControlBase OnConstruct()
        {
            return new QFSLXLookup();
        }

        protected override void OnExtractSchemaHints()
        {
            DataPath textBindingPath;

            if (Control.Bindings != null)
            {
                Control.Bindings.TryGetValue(LookupIdBindingCode, out _lookupIdBindingPath);
                Control.Bindings.TryGetValue(TextBindingCode, out textBindingPath);
            }
            else
            {
                textBindingPath = null;
            }

            string lookupDef;

            if (Component.TryGetPropertyValue("Lookup", out lookupDef))
            {
                _lookup = EntityLoader.LoadLookup(lookupDef);

                if (_lookup != null)
                {
                    try
                    {
                        _idDataPath = DataPath.Parse(_lookup.IdField);
                    }
                    catch (FormatException)
                    {
                        LogError("Unable to parse '{0}' lookup id field", _lookup.IdField);
                    }

                    try
                    {
                        _nameDataPath = DataPath.Parse(_lookup.NameField);
                    }
                    catch (FormatException)
                    {
                        LogError("Unable to parse '{0}' lookup name field", _lookup.NameField);
                    }

                    DataPathTranslator.RegisterTable(_lookup.MainTable);

                    if (_idDataPath != null)
                    {
                        _columnPrefixPath = _idDataPath.Reverse();
                        DataPathTranslator.RegisterField(_idDataPath);
                        DataPathTranslator.RegisterField(_columnPrefixPath);
                    }

                    if (_nameDataPath != null)
                    {
                        DataPathTranslator.RegisterField(_nameDataPath);
                    }

                    _columns = new List<ColumnDefinition>();
                    _dataPaths = new List<DataPath>();

                    if (_lookup.Layout != null)
                    {
                        foreach (string columnString in _lookup.Layout.Split(new char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries))
                        {
                            ColumnDefinition column = ColumnDefinition.Parse(columnString);

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
                else
                {
                    LogWarning("Unable to find '{0}' lookup", lookupDef);
                }
            }

            if (_lookupIdBindingPath != null && _idDataPath != null)
            {
                if (textBindingPath != null &&
                    _nameDataPath != null &&
                    textBindingPath.Joins.Count == 0)
                {
                    Debug.Assert(_lookupIdBindingPath.Joins.Count == 0);
                    Debug.Assert(_idDataPath.Joins.Count == 0);
                    Debug.Assert(_nameDataPath.Joins.Count == 0);
                    DataPathTranslator.RegisterJoin(_lookupIdBindingPath, _idDataPath);
                    Context.SecondaryJoins[
                        new DataPathJoin(_lookupIdBindingPath.TargetTable, _lookupIdBindingPath.TargetField, _idDataPath.TargetTable, _idDataPath.TargetField)] =
                        new DataPathJoin(textBindingPath.TargetTable, textBindingPath.TargetField, _nameDataPath.TargetTable, _nameDataPath.TargetField);
                    _isObject = true;
                }

                if (!_isObject)
                {
                    OrmEntity entity = EntityLoader.LoadEntity(_lookupIdBindingPath.TargetTable);

                    if (entity != null)
                    {
                        string targetField = _lookupIdBindingPath.TargetField;

                        if (targetField.StartsWith("@"))
                        {
                            targetField = targetField.Substring(1);
                        }

                        OrmEntityProperty property = entity.Properties.GetFieldPropertyByFieldName(targetField);

                        if (property != null)
                        {
                            _isObject = !property.Include;
                        }
                    }
                }

                if (_isObject)
                {
                    DataPathTranslator.RegisterJoin(_idDataPath, _lookupIdBindingPath);
                }
            }
        }

        protected override void OnBuild()
        {
            QFSLXLookup qfLookup = (QFSLXLookup) QfControl;

            if (_lookupIdBindingPath != null)
            {
                string propertyString = null;

                try
                {
                    propertyString = (_isObject
                                          ? DataPathTranslator.TranslateReference(_lookupIdBindingPath, _idDataPath.TargetTable, _idDataPath.TargetField)
                                          : DataPathTranslator.TranslateField(_lookupIdBindingPath));
                }
                catch (MigrationException ex)
                {
                    LogError(ex.Message);
                }

                if (propertyString != null)
                {
                    QfControl.DataBindings.Add(new QuickFormPropertyDataBindingDefinition(propertyString, "LookupResultValue"));
                }
            }

            qfLookup.LookupBindingMode = (_isObject ? LookupBindingModeEnum.Object : LookupBindingModeEnum.String);

            if (_lookup != null)
            {
                OrmEntity entity = Context.Entities[_lookup.MainTable];

                if (entity != null)
                {
                    qfLookup.LookupEntityName = entity.Name;
                    qfLookup.LookupEntityTypeName = string.Format("Sage.Entity.Interfaces.{0}, Sage.Entity.Interfaces, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", entity.InterfaceName);

                    if (_nameDataPath != null)
                    {
                        if (entity.DisplayProperty == null && _nameDataPath.Joins.Count == 0)
                        {
                            string targetField = _nameDataPath.TargetField;

                            if (targetField.StartsWith("@"))
                            {
                                targetField = targetField.Substring(1);
                            }

                            entity.DisplayProperty = entity.Properties.GetFieldPropertyByFieldName(targetField);
                            entity.Validate();
                            entity.Save();
                        }

                        string nameString = null;

                        try
                        {
                            nameString = DataPathTranslator.TranslateField(_nameDataPath);
                        }
                        catch (MigrationException ex)
                        {
                            LogError(ex.Message);
                        }

                        if (!string.IsNullOrEmpty(nameString))
                        {
                            nameString = string.Format("${{{0}}}", nameString);
                            var stringExpression = entity.GetStringExpression();

                            if (stringExpression != nameString)
                            {
                                if (string.IsNullOrEmpty(stringExpression) ||
                                    nameString.Length < stringExpression.Length)
                                {
                                    entity.SetStringExpression(nameString);
                                    entity.Validate();
                                    entity.Save();
                                }
                                else
                                {
                                    LogWarning("Multiple string expressions for entity '{0}' found", entity.Name);
                                }
                            }
                        }
                    }
                }
                else
                {
                    LogWarning("Unable to resolve the entity for '{0}' table", _lookup.MainTable);
                }

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
                        LookupProperty prop = new LookupProperty(propertyString, column.Caption);

                        switch (column.FormatType)
                        {
                            case FormatType.Phone:
                                prop.PropertyFormat = PropertyFormatEnum.Phone;
                                break;
                            case FormatType.User:
                                prop.PropertyFormat = PropertyFormatEnum.User;
                                break;
                            case FormatType.PickListItem:
                                prop.PropertyFormat = PropertyFormatEnum.PickList;
                                break;
                        }

                        qfLookup.LookupProperties.Add(prop);
                    }
                }
            }
        }
    }
}