using System;

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
    }
}