using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sage.Platform.Application;
using Sage.SalesLogix.LegacyBridge.Delphi;
using Sage.SalesLogix.Migration.Forms.Services;
using Sage.SalesLogix.Migration.Services;
using Sage.SalesLogix.QuickForms.QFControls;
using Sage.SalesLogix.QuickForms.QFControls.DataGrid;

namespace Sage.SalesLogix.Migration.Forms.Builders.Columns
{
    public abstract class ColumnBuilder
    {
        private static readonly IDictionary<BuilderMappingAttribute, Type> _builders;

        static ColumnBuilder()
        {
            _builders = new Dictionary<BuilderMappingAttribute, Type>();
            Type baseType = typeof (ColumnBuilder);

            foreach (Type builderType in baseType.Assembly.GetTypes())
            {
                if (!builderType.IsAbstract && baseType.IsAssignableFrom(builderType))
                {
                    foreach (BuilderMappingAttribute attr in builderType.GetCustomAttributes(typeof (BuilderMappingAttribute), false))
                    {
                        Debug.Assert(!_builders.ContainsKey(attr));
                        _builders.Add(attr, builderType);
                    }
                }
            }
        }

        public static ColumnBuilder CreateBuilder(DelphiComponent component)
        {
            Type builderType = null;

            foreach (KeyValuePair<BuilderMappingAttribute, Type> builder in _builders)
            {
                if (builderType != builder.Value && builder.Key.IsApplicable(component.Type, component.Properties))
                {
                    bool isSpecific = (builder.Key.GetType() != typeof (BuilderMappingAttribute));

                    if (builderType == null || isSpecific)
                    {
                        builderType = builder.Value;

                        if (isSpecific)
                        {
                            break;
                        }
                    }
                }
            }

            ColumnBuilder colBuilder;

            if (builderType != null)
            {
                colBuilder = (ColumnBuilder) Activator.CreateInstance(builderType);
                colBuilder._component = component;
                return colBuilder;
            }
            else
            {
                colBuilder = null;
            }

            return colBuilder;
        }

        private MigrationContext _context;
        private IDataPathTranslationService _dataPathTranslator;
        private IOrmEntityLoaderService _entityLoader;
        private DelphiComponent _component;
        private IQFDataGridCol _column;
        private DataPath _bindingPath;

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
            protected get { return _dataPathTranslator; }
            set { _dataPathTranslator = value; }
        }

        [ServiceDependency]
        public IOrmEntityLoaderService EntityLoader
        {
            protected get { return _entityLoader; }
            set { _entityLoader = value; }
        }

        protected DelphiComponent Component
        {
            get { return _component; }
        }

        protected IQFDataGridCol Column
        {
            get { return _column; }
        }

        protected DataPath BindingPath
        {
            get { return _bindingPath; }
        }

        public void ExtractSchemaHints()
        {
            string dataPath;

            if (_component.TryGetPropertyValue("DataPath", out dataPath) && !string.IsNullOrEmpty(dataPath))
            {
                try
                {
                    _bindingPath = DataPath.Parse(dataPath);
                }
                catch (FormatException)
                {
                    LogError("Unable to parse '{0}' colunn data path", dataPath);
                }

                if (_bindingPath != null)
                {
                    _dataPathTranslator.RegisterField(_bindingPath);
                }
            }

            OnExtractSchemaHints();
        }

        public IQFDataGridCol Construct()
        {
            _column = OnConstruct();
            return _column;
        }

        public void Build(DataPath prefixPath)
        {
            if (_bindingPath != null)
            {
                string propertyString = BuildDataField(prefixPath);
                _column.DataField = propertyString ?? string.Empty;
                QFDataGridCol col = _column as QFDataGridCol;

                if (col != null)
                {
                    col.IsSortable = !string.IsNullOrEmpty(propertyString);
                }
            }
            else
            {
                _column.DataField = string.Empty;
            }

            string caption;

            if (_component.TryGetPropertyValue("Caption", out caption) && !string.IsNullOrEmpty(caption))
            {
                _column.ColumnHeading = caption;
            }

            OnBuild();
        }

        protected virtual string BuildDataField(DataPath prefixPath)
        {
            try
            {
                return _dataPathTranslator.TranslateField(_bindingPath, prefixPath);
            }
            catch (MigrationException ex)
            {
                LogError(ex.Message);
                return null;
            }
        }

        protected virtual void OnExtractSchemaHints() {}
        protected abstract IQFDataGridCol OnConstruct();
        protected virtual void OnBuild() {}

        protected void LogError(string text, params object[] args)
        {
            if (_context != null && _context.Log != null)
            {
                _context.Log.Error(text, args);
            }
        }
    }
}