using System.Diagnostics;
using Sage.Platform.Application;

namespace Sage.SalesLogix.Migration
{
    public sealed class ColumnDefinition
    {
        private readonly string _binding;
        private readonly string _alias;
        private readonly string _caption;
        private readonly int? _width;
        private readonly string _formatString;
        private readonly FormatType _formatType;
        private readonly HorizontalAlignment _textAlign;
        private readonly HorizontalAlignment _captionAlign;
        private readonly bool _visible;

        private ColumnDefinition(
            string binding,
            string alias,
            string caption,
            int? width,
            string formatString,
            FormatType formatType,
            HorizontalAlignment textAlign,
            HorizontalAlignment captionAlign,
            bool visible)
        {
            _binding = binding;
            _alias = alias;
            _caption = caption;
            _width = width;
            _formatString = formatString;
            _formatType = formatType;
            _textAlign = textAlign;
            _captionAlign = captionAlign;
            _visible = visible;
        }

        public string Binding
        {
            get { return _binding; }
        }

        public string Alias
        {
            get { return _alias; }
        }

        public string Caption
        {
            get { return _caption; }
        }

        public int? Width
        {
            get { return _width; }
        }

        public string FormatString
        {
            get { return _formatString; }
        }

        public FormatType FormatType
        {
            get { return _formatType; }
        }

        public HorizontalAlignment TextAlign
        {
            get { return _textAlign; }
        }

        public HorizontalAlignment CaptionAlign
        {
            get { return _captionAlign; }
        }

        public bool Visible
        {
            get { return _visible; }
        }

        public static ColumnDefinition Parse(string text)
        {
            Guard.ArgumentNotNullOrEmptyString(text, "text");
            string[] parts = text.Split('|');
            int value;
            Debug.Assert(parts[0].Length == 0);

            string binding = (parts.Length > 1
                                  ? parts[1]
                                  : null);
            string alias = (parts.Length > 2
                                ? parts[2]
                                : null);
            string caption = (parts.Length > 3
                                  ? parts[3]
                                  : null);
            int? width = (parts.Length > 4 && int.TryParse(parts[4], out value)
                              ? (int?) value
                              : null);
            string formatString = (parts.Length > 5
                                       ? parts[5]
                                       : null);
            FormatType formatType = (parts.Length > 6 && int.TryParse(parts[6], out value)
                                         ? (FormatType) value
                                         : FormatType.None);
            HorizontalAlignment textAlign = (parts.Length > 7 && int.TryParse(parts[7], out value)
                                                 ? (HorizontalAlignment) value
                                                 : HorizontalAlignment.Left);
            HorizontalAlignment captionAlign = (parts.Length > 8 && int.TryParse(parts[8], out value)
                                                    ? (HorizontalAlignment) value
                                                    : HorizontalAlignment.Left);

            if (parts.Length > 9)
            {
                //TODO: figure out what this is
                Debug.Assert(parts[9].Length == 0 || parts[9] == "0");
            }

            bool visible = !(parts.Length > 10) ||
                           StringUtils.CaseInsensitiveEquals(parts[10], "T");

            if (parts.Length > 11)
            {
                //TODO: figure out what this is
                Debug.Assert(parts[11].Length == 0 || parts[11] == "F");
            }

            if (parts.Length > 12)
            {
                //TODO: figure out what this is
            }

            if (parts.Length > 13)
            {
                //TODO: figure out what this is
                Debug.Assert(parts[13].Length == 0);
            }

            Debug.Assert(parts.Length <= 14);
            return new ColumnDefinition(binding, alias, caption, width, formatString, formatType, textAlign, captionAlign, visible);
        }
    }
}