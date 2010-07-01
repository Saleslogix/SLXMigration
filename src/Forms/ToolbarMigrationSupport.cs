using System.Diagnostics;
using System.Drawing;
using System.IO;
using Sage.Platform.Application;
using Sage.SalesLogix.LegacyBridge.Delphi;
using Sage.SalesLogix.Migration.Forms.Services;
using Sage.SalesLogix.Migration.Services;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration.Forms
{
    public sealed class ToolbarMigrationSupport : IMigrationSupport
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
            using (_context.Status.BeginStep("Parsing toolbars...", _context.Plugins.Count))
            {
                foreach (Plugin plugin in _context.Plugins)
                {
                    if (plugin.Type == PluginType.Toolbar)
                    {
                        _context.Log.SourcePlugin = plugin;
                        LogInfo(false, "Parsing '{0}' toolbar", plugin);
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
            using (_context.Status.BeginStep("Building toolbars...", _context.Navigation.Count))
            {
                foreach (NavigationInfo navigation in _context.Navigation)
                {
                    _context.Log.SourcePlugin = navigation.Plugin;
                    LogInfo(false, "Building '{0}' navigation", navigation);
                    Build(navigation);
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
            using (_context.Status.BeginStep("Persisting toolbars...", _context.Navigation.Count))
            {
                foreach (NavigationInfo navigation in _context.Navigation)
                {
                    _context.Log.SourcePlugin = navigation.Plugin;
                    LogInfo(false, "Persisting '{0}' navigation", navigation);
                    Persist(navigation);
                    //TODO: add generated-item mapping to something
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

            using (MemoryStream stream = new MemoryStream(plugin.Blob.Data))
            {
                stream.Position = 0xE;

                using (DelphiBinaryReader binaryReader = new DelphiBinaryReader(stream))
                {
                    component = binaryReader.ReadComponent(true);
                }
            }

            _componentSimplifier.Simplify(component);
            byte[] data;

            if (component.TryGetPropertyValue("Toolbars.WhenItems", out data))
            {
                using (DelphiBinaryReader binaryReader = new DelphiBinaryReader(data))
                {
                    component = ParseItems(binaryReader, true);
                }

                foreach (DelphiComponent groupComponent in component.Components)
                {
                    string group;
                    string defaultDock;

                    if (groupComponent.TryGetPropertyValue("DefaultDock", out defaultDock) && defaultDock == "wtdLeft")
                    {
                        groupComponent.TryGetPropertyValue("Caption", out group);
                    }
                    else
                    {
                        group = null;
                    }

                    foreach (DelphiComponent itemComponent in groupComponent.Components)
                    {
                        string caption;

                        if (itemComponent.TryGetPropertyValue("Caption", out caption) && !string.IsNullOrEmpty(caption))
                        {
                            string action;
                            string argument;
                            itemComponent.TryGetPropertyValue("Caption", out caption);
                            itemComponent.TryGetPropertyValue("WhenClick.Action", out action);
                            itemComponent.TryGetPropertyValue("WhenClick.Argument", out argument);
                            Image glyph = (itemComponent.TryGetPropertyValue("Glyph.Data", out data)
                                               ? BorlandUtils.ParseGlyphData(data)
                                               : null);
                            _context.Navigation.Add(new NavigationInfo(plugin, caption, group, action, argument, glyph));
                        }
                    }
                }
            }
        }

        private void Build(NavigationInfo navigation)
        {
            switch (navigation.Action)
            {
                case "MainView":
                    {
                        string[] parts = navigation.Argument.Split(':');

                        if (parts.Length == 2)
                        {
                            string name = BasePluginInfo.FormatFullName(parts[0], parts[1]);
                            MainViewInfo mainView;

                            if (_context.MainViews.TryGetValue(name, out mainView))
                            {
                                navigation.NavUrl = mainView.MainTable + ".aspx";
                            }
                            else
                            {
                                LogWarning("Unable to find the '{0}' main view", name);
                            }
                        }
                        else
                        {
                            LogWarning("Unable to parse argument: '{0}'", navigation.Argument);
                        }
                    }
                    break;
                default:
                    LogWarning("Legacy action '{0}' not supported", navigation.Action);
                    break;
            }
        }

        private void Persist(NavigationInfo navigation) {}

        private DelphiComponent ParseItems(DelphiBinaryReader binaryReader, bool isPrefixed)
        {
            DelphiComponent component = binaryReader.ReadComponent(isPrefixed);
            _componentSimplifier.Simplify(component);
            byte b = binaryReader.ReadByte();
            Debug.Assert(b == 1);

            while (binaryReader.PeekChar() != 0)
            {
                component.Components.Add(ParseItems(binaryReader, false));
            }

            binaryReader.ReadByte();
            return component;
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
    }
}