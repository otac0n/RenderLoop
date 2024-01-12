namespace RenderLoop
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
