using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LOD_CM_CLI.Utils
{
    public static class StringUtils
    {
        /// <summary>
        /// Utility function to get the fragment of an URI (after a '/' or '#')
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static string GetUriFragment(this string uri)
        {
            var index = Math.Max(uri.LastIndexOf("/"), uri.LastIndexOf("#"));
            if (index == -1)
                throw new Exception($"{uri} doesn't contain any '/' or '#'");
            return uri.Substring(index + 1);
        }

        public static string ToCamelCaseAlphaNum(this string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return string.Empty;
            str = str.RemoveDiacritics();
            str = str.KeepOnlyAlphaNumeric();
            var array = str.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (array.Length == 1)
            {
                return array[0];
            }
            if (array.Length == 0)
            {
                return string.Empty;
            }
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = Char.ToUpperInvariant(array[i][0]) + array[i].Substring(1);
            }
            return string.Join(string.Empty, array);
        }

        public static string KeepOnlyAlphaNumeric(this string str)
        {
            var rgx = new Regex("[^a-zA-Z0-9 -]");
            str = rgx.Replace(str, "");
            return str;
        }
        public static string RemoveDiacritics(this string text) 
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}