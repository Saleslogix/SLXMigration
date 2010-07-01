using System.IO;
using Sage.Platform.Application;
using Sage.Platform.Orm.Entities;
using Sage.SalesLogix.LegacyBridge.Delphi;
using Sage.SalesLogix.Migration.Forms.Services;
using Sage.SalesLogix.Migration.Services;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration.Forms
{
    public sealed class MainViewMigrationSupport : IMigrationSupport
    {
        private MigrationContext _context;
        private IFormSimplificationService _componentSimplifier;

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
        public IFormSimplificationService ComponentSimplifier
        {
            set { _componentSimplifier = value; }
        }

        #region IMigrationSupport Members

        public void Parse()
        {
            using (_context.Status.BeginStep("Parsing main views...", _context.Plugins.Count))
            {
                foreach (Plugin plugin in _context.Plugins)
                {
                    if (plugin.Type == PluginType.MainView)
                    {
                        _context.Log.SourcePlugin = plugin;
                        LogInfo(false, "Parsing '{0}' main view", plugin);
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
            using (_context.Status.BeginStep("Building main views...", _context.MainViews.Count))
            {
                foreach (MainViewInfo mainView in _context.MainViews.Values)
                {
                    _context.Log.SourcePlugin = mainView.Plugin;
                    LogInfo(false, "Building '{0}' main view", mainView);
                    Build(mainView);
                    _context.Log.SourcePlugin = null;

                    if (!_context.Status.Advance())
                    {
                        break;
                    }
                }
            }
        }

        public void Persist()
        {
            using (_context.Status.BeginStep("Persisting main views...", _context.MainViews.Count))
            {
                foreach (MainViewInfo mainView in _context.MainViews.Values)
                {
                    _context.Log.SourcePlugin = mainView.Plugin;
                    LogInfo(false, "Persisting '{0}' main view", mainView);
                    Persist(mainView);
                    //TODO: add generated-item mapping to portal page
                    LogInfo(true, "Item successfully migrated");
                    _context.Log.SourcePlugin = null;

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

            _componentSimplifier.Simplify(component);

            string baseTable;
            string detailsViewName;
            component.TryGetPropertyValue("BaseTable", out baseTable);
            component.TryGetPropertyValue("DetailsViewName", out detailsViewName);
            MainViewInfo mainView = new MainViewInfo(plugin, baseTable, detailsViewName);
            _context.MainViews.Add(mainView.FullName, mainView);

            //TODO: might need EntityNameSingular, EntityNamePlural, EntityFieldName

            string script;

            if (component.TryGetPropertyValue("ScriptText", out script) && !string.IsNullOrEmpty(script))
            {
                //TODO: need to add proper script support for main views
                ScriptInfo scriptInfo = new ScriptInfo(plugin, script);
                _context.Scripts.Add(scriptInfo.PrefixedFullName, scriptInfo);
            }
        }

        private void Build(MainViewInfo mainView)
        {
            if (!string.IsNullOrEmpty(mainView.MainTable))
            {
                OrmEntity entity;

                if (_context.Entities.TryGetValue(mainView.MainTable, out entity))
                {
                    mainView.Entity = entity;
                }
                else
                {
                    LogWarning("Unable to resolve the entity for '{0}' table", mainView.MainTable);
                }
            }

            if (!string.IsNullOrEmpty(mainView.DetailFormName))
            {
                FormInfo form;

                if (_context.Forms.TryGetValue(mainView.DetailFormName.Replace(':', '_'), out form))
                {
                    mainView.DetailForm = form;
                    form.IsDetail = true;
                    form.HasGroupNavigator = true;
                    form.HasSaveButton = true;
                    form.HasDeleteButton = true;
                }
                else
                {
                    LogWarning("Unable to resolve the '{0}' form", mainView.DetailFormName);
                }
            }
        }

        private void Persist(MainViewInfo mainView) {}

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
    }
}