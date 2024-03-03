// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS.Archives
{
    using System.Linq;
    using System.Text.RegularExpressions;

    internal static class PathExtensions
    {
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
    }
}
