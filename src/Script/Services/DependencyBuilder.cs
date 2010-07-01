using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration.Script.Services
{
    public sealed class DependencyBuilder
    {
        private static readonly Regex _regex = new Regex(@"^(?<family>[\w\s-]+)\:(?<name>[\w\s-]+)$", RegexOptions.Compiled);

        private readonly IExtendedLog _log;
        private readonly IDictionary<string, ScriptInfo> _scripts;

        public DependencyBuilder(IExtendedLog log, IDictionary<string, ScriptInfo> scripts)
        {
            _log = log;
            _scripts = scripts;
        }

        public void Build(ScriptInfo script)
        {
            using (TextReader reader = new StringReader(script.Code))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    MatchCollection matches = _regex.Matches(line);

                    if (matches.Count == 1)
                    {
                        Match match = matches[0];
                        string family = match.Groups["family"].Value;
                        string name = match.Groups["name"].Value;
                        BuildDependency(script, family, name);
                    }
                    else
                    {
                        while (line == "|")
                        {
                            line = reader.ReadLine();
                        }

                        break;
                    }
                }

                script.Code = string.Format("{1}{0}{2}{0}", Environment.NewLine, line, reader.ReadToEnd());
            }
        }

        private void BuildDependency(ScriptInfo script, string family, string name)
        {
            string fullName = ScriptInfo.FormatPrefixedFullName("Script", family, name);

            if (script.IsForm || !StringUtils.CaseInsensitiveEquals(script.FullName, fullName))
            {
                ScriptInfo dependencyScript;

                if (!_scripts.TryGetValue(fullName, out dependencyScript))
                {
                    Plugin plugin = Plugin.LoadByName(name, family, PluginType.ActiveScript);

                    if (plugin == null)
                    {
                        LogWarning("Include script '{0}:{1}' not found", family, name);
                    }
                    else
                    {
                        dependencyScript = new ScriptInfo(plugin);
                        Debug.Assert(!_scripts.ContainsKey(dependencyScript.PrefixedFullName));
                        _scripts.Add(dependencyScript.PrefixedFullName, dependencyScript);
                        Build(dependencyScript);
                    }
                }

                if (dependencyScript != null && !dependencyScript.IsInvalid)
                {
                    Debug.Assert(_scripts.ContainsKey(dependencyScript.PrefixedFullName));
                    script.Dependencies[dependencyScript.PrefixedFullName] = dependencyScript;

                    foreach (ScriptInfo item in dependencyScript.Dependencies.Values)
                    {
                        if (item != script && !item.IsInvalid)
                        {
                            Debug.Assert(_scripts.ContainsKey(item.PrefixedFullName));
                            script.Dependencies[item.PrefixedFullName] = item;
                        }
                    }
                }
            }
        }

        private void LogWarning(string text, params object[] args)
        {
            if (_log != null)
            {
                _log.Warn(text, args);
            }
        }
    }
}