using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sage.Platform.Application;
using Sage.Platform.QuickForms.Controls;
using Sage.SalesLogix.LegacyBridge.Delphi;
using Sage.SalesLogix.Migration.Forms.Services;
using Sage.SalesLogix.Migration.Services;

namespace Sage.SalesLogix.Migration.Forms.Builders
{
    public abstract class ControlBuilder : IControlBuilder
    {
        private static readonly IDictionary<BuilderMappingAttribute, Type> _builders;

        static ControlBuilder()
        {
            _builders = new Dictionary<BuilderMappingAttribute, Type>();
            Type baseType = typeof (ControlBuilder);

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

        public static ControlBuilder CreateBuilder(string name, DelphiComponent component)
        {
            //invisible controls are mapped to the HiddenControl control
            bool visible = (!component.TryGetPropertyValue("Visible", out visible) || visible);
            if (!visible)
            {
                ControlBuilder builder = new HiddenControlBuilder(component);
                builder._component = component;
                return builder;

            }

            Type builderType = null;

            foreach (KeyValuePair<BuilderMappingAttribute, Type> builder in _builders)
            {
                if (builderType != builder.Value && builder.Key.IsApplicable(name, component.Properties))
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

            ControlBuilder colBuilder;

            if (builderType != null)
            {
                colBuilder = (ControlBuilder) Activator.CreateInstance(builderType);
                colBuilder._component = component;
                return colBuilder;
            }
            else
            {
                colBuilder = null;
            }

            return colBuilder;
        }

        protected const int TextBindingCode = -517;

        private MigrationContext _context;
        private IDataPathTranslationService _dataPathTranslator;
        private IOrmEntityLoaderService _entityLoader;
        protected FormInfo _form;
        protected ControlInfo _control;
        protected DelphiComponent _component;
        private QuickFormsControlBase _qfControl;

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

        protected MigrationContext Context
        {
            get { return _context; }
        }

        protected FormInfo Form
        {
            get { return _form; }
        }

        protected ControlInfo Control
        {
            get { return _control; }
        }

        public DelphiComponent Component
        {
            get { return _component; }
        }

        protected IQuickFormsControl QfControl
        {
            get { return _qfControl; }
        }

        public void Initialize(
            FormInfo form,
            ControlInfo control)
        {
            _form = form;
            _control = control;
            OnInitialize();
        }

        public void ExtractSchemaHints()
        {
            if (Control.Bindings != null)
            {
                foreach (DataPath binding in Control.Bindings.Values)
                {
                    DataPathTranslator.RegisterField(binding);
                }
            }

            OnExtractSchemaHints();
        }

        #region IControlBuilder Members

        public QuickFormsControlBase Construct()
        {
            _qfControl = OnConstruct();
            _qfControl.ControlId = _component.Name;
            return _qfControl;
        }

        public void Build()
        {
            bool readOnly;
            bool enabled;
            _qfControl.IsReadOnly = ((Component.TryGetPropertyValue("ReadOnly", out readOnly) && readOnly) ||
                                     (Component.TryGetPropertyValue("Enabled", out enabled) && !enabled));
            AddDataBinding(TextBindingCode, "Text");
            OnBuild();
        }

        public void PostBuild()
        {
            OnPostBuild();
        }

        #endregion

        protected virtual void OnInitialize() {}
        protected virtual void OnExtractSchemaHints() {}
        protected abstract QuickFormsControlBase OnConstruct();
        protected virtual void OnBuild() {}
        protected virtual void OnPostBuild() { } //caled when the contro land all of its child controls are built

        protected void AddDataBinding(int bindingCode, string controlItem)
        {
            DataPath bindingPath;

            if (Control.Bindings != null && Control.Bindings.TryGetValue(bindingCode, out bindingPath))
            {
                string propertyString = null;

                try
                {
                    propertyString = DataPathTranslator.TranslateField(bindingPath);
                }
                catch (MigrationException ex)
                {
                    LogError(ex.Message);
                }

                if (propertyString != null)
                {
                    QfControl.DataBindings.Add(new QuickFormPropertyDataBindingDefinition(propertyString, controlItem));
                }
            }
        }

        protected void LogWarning(string text, params object[] args)
        {
            if (_context != null && _context.Log != null)
            {
                _context.Log.Warn(text, args);
            }
        }

        protected void LogError(string text, params object[] args)
        {
            if (_context != null && _context.Log != null)
            {
                _context.Log.Error(text, args);
            }
        }
    }
}