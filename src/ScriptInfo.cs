using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration
{
    public sealed class ScriptInfo : BasePluginInfo
    {
        private readonly bool _isForm;
        private string _code;
        private readonly string _formName;
        private readonly IDictionary<string, FormField> _formControls;
        private readonly IDictionary<string, ScriptInfo> _dependencies;
        private readonly string _prefix;
        private CodeTypeDeclaration _typeDeclaration;
        private bool _isInvalid;
        private string _namespaceName;
        private string _className;
        private string _prefixedFullName;

        public ScriptInfo(Plugin plugin)
            : this(plugin, false, Encoding.GetEncoding("iso-8859-1").GetString(plugin.Blob.Data), null, null, "Script") {}

        public ScriptInfo(Plugin plugin, string code)
            : this(plugin, true, code, null, null, "MainView") {}

        public ScriptInfo(Plugin plugin, string code, string formName, IDictionary<string, FormField> formControls)
            : this(plugin, true, code, formName, formControls, "Form") {}

        private ScriptInfo(Plugin plugin, bool isForm, string code, string formName, IDictionary<string, FormField> formControls, string prefix)
            : base(plugin)
        {
            _isForm = isForm;
            _code = code;
            _formName = formName;
            _formControls = formControls;
            _dependencies = new Dictionary<string, ScriptInfo>(StringComparer.InvariantCultureIgnoreCase);
            _prefix = prefix;
        }

        public bool IsForm
        {
            get { return _isForm; }
        }

        public string Code
        {
            get { return _code; }
            set { _code = value; }
        }

        public string FormName
        {
            get { return _formName; }
        }

        public IDictionary<string, FormField> FormControls
        {
            get { return _formControls; }
        }

        public IDictionary<string, ScriptInfo> Dependencies
        {
            get { return _dependencies; }
        }

        public CodeTypeDeclaration TypeDeclaration
        {
            get { return _typeDeclaration; }
            set { _typeDeclaration = value; }
        }

        public bool IsInvalid
        {
            get { return _isInvalid; }
            set { _isInvalid = value; }
        }

        public string NamespaceName
        {
            get { return _namespaceName ?? (_namespaceName = StringUtils.UnderscoreInvalidChars(Family)); }
        }

        public string ClassName
        {
            get { return _className ?? (_className = StringUtils.UnderscoreInvalidChars(Name)); }
        }

        public string PrefixedFullName
        {
            get { return _prefixedFullName ?? (_prefixedFullName = FormatPrefixedFullName(_prefix, Family, Name)); }
        }

        public static string FormatPrefixedFullName(string prefix, string family, string name)
        {
            return string.Format("{0}_{1}", prefix, FormatFullName(family, name));
        }
    }
}