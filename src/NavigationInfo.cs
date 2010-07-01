using System.Drawing;
using System.Reflection;
using Sage.Platform.WebPortal.Design;
using Sage.SalesLogix.Plugins;

namespace Sage.SalesLogix.Migration
{
    public sealed class NavigationInfo : BasePluginInfo
    {
        //private static readonly char[] _illegalChars;

        /*static NavigationInfo()
        {
            FieldInfo field = typeof (PortalUtil).GetField("IllegalFileNameCharacters", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (field != null)
            {
                _illegalChars = (char[]) field.GetValue(null);
            }
        }*/

        private readonly string _caption;
        private readonly string _groupName;
        private readonly string _action;
        private readonly string _argument;
        private readonly Image _glyph;
        private string _safeCaption;
        private string _id;
        private string _safeGroupCaption;
        private string _groupId;
        private string _navUrl;
        private NavigationItem _item;

        public NavigationInfo(Plugin plugin, string caption, string groupName, string action, string argument, Image icon)
            : base(plugin)
        {
            _caption = caption;
            _groupName = groupName;
            _action = action;
            _argument = argument;
            _glyph = icon;
        }

        public string Caption
        {
            get { return _caption; }
        }

        public string GroupName
        {
            get { return _groupName; }
        }

        public string Action
        {
            get { return _action; }
        }

        public string Argument
        {
            get { return _argument; }
        }

        public Image Glyph
        {
            get { return _glyph; }
        }

        public string SafeCaption
        {
            get { return _safeCaption ?? (_safeCaption = StringUtils.ReplaceIllegalChars(_caption)); }
        }

        public string Id
        {
            get { return _id ?? (_id = "nav" + SafeCaption); }
        }

        public string SafeGroupCaption
        {
            get { return _safeGroupCaption ?? (_safeGroupCaption = StringUtils.ReplaceIllegalChars(_groupName)); }
        }

        public string GroupId
        {
            get { return _groupId ?? (_groupId = "nav" + SafeGroupCaption); }
        }

        public string NavUrl
        {
            get { return _navUrl; }
            set { _navUrl = value; }
        }

        public NavigationItem Item
        {
            get { return _item; }
            set { _item = value; }
        }
    }
}