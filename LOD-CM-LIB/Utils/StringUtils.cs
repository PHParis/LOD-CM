using System;
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
            var rgx = new Regex("[^a-zA-Z0-9 -]");
            str = rgx.Replace(str, "");
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
    }
}