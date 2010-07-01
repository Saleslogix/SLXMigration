using System;
using System.IO;
using System.Collections.Generic;

namespace Sage.SalesLogix.Migration
{
    public static class StringUtils
    {
        public static bool CaseInsensitiveEquals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.InvariantCultureIgnoreCase);
        }

        public static string UnderscoreInvalidChars(string str)
        {
            if (str == null)
            {
                return str;
            }

            char[] chars = str.ToCharArray();

            for (int i = 0; i < str.Length; i++)
            {
                char c = chars[i];

                if (!char.IsLetter(c) && !char.IsDigit(c) && c != '_')
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }

        public static string ReplaceAny(string str, char[] illegalChars, char replacement)
        {
            if (str == null)
            {
                return str;
            }

            char[] chars = str.ToCharArray();
            int pos = 0;

            while ((pos = str.IndexOfAny(illegalChars, pos)) >= 0)
            {
                chars[pos] = replacement;
                pos++;
            }

            return new string(chars);
        }

        //replaces characters illegal in file names as well as " "
        public static string ReplaceIllegalChars(string str)
        {
            if (str == null)
            {
                return str;
            }

            List<char> lstInvalidChars = new List<char>(Path.GetInvalidFileNameChars());
            lstInvalidChars.Add(' ');
            lstInvalidChars.Add('-');
            lstInvalidChars.Add('.');

            return ReplaceAny(str, lstInvalidChars.ToArray(), '_');
        }

    }
}