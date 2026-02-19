// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    internal static class PathExtensions
    {
        public static readonly char[] Separators = ['/', '\\'];

        public static string CombineIgnoringAbsolute(params string[] paths) =>
            string.Join("/", paths.Where(p => !string.IsNullOrEmpty(p)).Select(p => p.Trim(Separators)));

        internal static string GetDirectoryName(string path)
        {
            var i = path.LastIndexOfAny(Separators);
            return i >= 0 ? path[..i] : string.Empty;
        }

        public static Regex GlobToRegex(string searchPattern) =>
            new Regex(
                "^" +
                string.Concat(
                    Regex.Split(searchPattern, @"(\?|\*+)")
                        .Select(p =>
                            p == ""  ? "" :
                            p[0] == '?' ? "." :
                            p[0] == '*' ? ".*" :
                            Regex.Escape(p))) +
                "$",
                RegexOptions.Singleline);

        public static bool PrefixMatch(string[] prefix, string[] subject)
        {
            if (prefix.Length > subject.Length)
            {
                return false;
            }

            for (var i = 0; i < prefix.Length; i++)
            {
                if (prefix[i] != subject[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
