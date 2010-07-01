using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Sage.SalesLogix.Migration.Forms
{
    public static class FormatUtils
    {
        private static Regex _regex;

        public static string ConvertNumberFormat(string legacyFormatString, string defaultFormatString)
        {
            if (string.IsNullOrEmpty(legacyFormatString))
            {
                return defaultFormatString;
            }

            if (_regex == null)
            {
                _regex = new Regex(@"%(?<alignment>[0-9]+)?\.?(?<padding>[0-9]+)?(?<format>[A-Za-z])", RegexOptions.Compiled);
            }

            legacyFormatString = legacyFormatString.Replace("%%", "\0");
            MatchCollection matches = _regex.Matches(legacyFormatString);
            StringBuilder builder = new StringBuilder();
            int index = 0;

            foreach (Match match in matches)
            {
                builder
                    .Append(legacyFormatString.Substring(index, match.Index - index))
                    .Append("{0");
                string alignment = match.Groups["alignment"].Value;

                if (alignment.Length > 0)
                {
                    builder
                        .Append(",")
                        .Append(alignment);
                }

                builder
                    .Append(":")
                    .Append(match.Groups["format"].Value)
                    .Append(match.Groups["padding"].Value)
                    .Append("}");
                index = match.Index + match.Length;
            }

            builder.Append(legacyFormatString.Substring(index));
            return builder.ToString().Replace("\0", "%");
        }

        public static string ConvertDateFormat(string legacyFormatString, string defaultFormatString)
        {
            if (string.IsNullOrEmpty(legacyFormatString))
            {
                return defaultFormatString;
            }

            legacyFormatString = legacyFormatString.Trim();
            string lowerStr = legacyFormatString.ToLower();

            switch (lowerStr)
            {
                case "dddddd":
                    legacyFormatString = "D";
                    break;
                case "tt":
                    legacyFormatString = "T";
                    break;
                case "c":
                    legacyFormatString = "G";
                    break;
                default:
                    {
                        DateTimeFormatInfo info = DateTimeFormatInfo.CurrentInfo;

                        string[] slxCodes = new string[]
                            {
                                "dddddd",
                                "am/pm",
                                "ddddd",
                                "ampm",
                                "dddd",
                                "mmmm",
                                "yyyy",
                                "a/p",
                                "ddd",
                                "mmm",
                                "dd",
                                "hh",
                                "mm",
                                "nn",
                                "ss",
                                "tt",
                                "yy",
                                "c",
                                "d",
                                "h",
                                "m",
                                "n",
                                "s",
                                "t"
                            };

                        string[] netCodes = new string[]
                            {
                                info.LongDatePattern,
                                "tt",
                                "d",
                                "tt",
                                "dddd",
                                "MMMM",
                                "yyyy",
                                "%t",
                                "ddd",
                                "MMM",
                                "dd",
                                "HH",
                                "MM",
                                "mm",
                                "ss",
                                info.LongTimePattern,
                                "yy",
                                info.FullDateTimePattern,
                                "%d",
                                "%H",
                                "%M",
                                "%m",
                                "%s",
                                "t"
                            };

                        for (int i = 0; i < slxCodes.Length; i++)
                        {
                            string slxCode = slxCodes[i];
                            string netCode = netCodes[i];
                            int slxCodeLen = slxCode.Length;
                            int netCodeLen = netCode.Length;

                            while (true)
                            {
                                int pos = lowerStr.IndexOf(slxCode);

                                if (pos < 0)
                                {
                                    break;
                                }

                                lowerStr = lowerStr.Substring(0, pos) + new string('\0', netCodeLen) + lowerStr.Substring(pos + slxCodeLen);
                                legacyFormatString = legacyFormatString.Substring(0, pos) + netCode + legacyFormatString.Substring(pos + slxCodeLen);
                            }
                        }
                    }
                    break;
            }

            return string.Format("{{0:{0}}}", legacyFormatString);
        }

        public static string ConvertCaption(string caption)
        {
            return (string.IsNullOrEmpty(caption)
                        ? caption
                        : caption.Replace("&&", "\0").Replace("&", "").Replace("\0", "&"));
        }
    }
}