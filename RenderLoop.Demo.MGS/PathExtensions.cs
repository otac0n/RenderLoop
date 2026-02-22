// Copyright © John Gietzen. All Rights Reserved. This source is subject to the GPL license. Please see license.md for more information.

namespace RenderLoop.Demo.MGS
{
    using System;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    internal static partial class PathExtensions
    {
        public static readonly char[] Separators = ['/', '\\'];

        [GeneratedRegex(@"(?<=(?<![/\\])[/\\])[/\\]*(?!$)")]
        private static partial Regex SegmentSplitRegex();

        public static string CombineIgnoringAbsolute(this IPath path, string prefix, string suffix) =>
            path.Combine(prefix.TrimEnd(Separators), suffix.TrimStart(Separators));

        public static string CombineWithSeparator(this IPath path, char separator, string prefix, string suffix)
        {
            if (string.IsNullOrEmpty(prefix) || path.IsPathRooted(suffix))
            {
                return suffix ?? string.Empty;
            }

            if (string.IsNullOrEmpty(suffix))
            {
                return prefix ?? string.Empty;
            }

            return prefix.TrimEnd(Separators) + separator + suffix.TrimStart(Separators);
        }

        public static string GetRelativePath(this IPath path, char separator, string relativeTo, string destination)
        {
            ArgumentException.ThrowIfNullOrEmpty(relativeTo, nameof(relativeTo));
            ArgumentException.ThrowIfNullOrEmpty(destination, nameof(destination));

            if (path.IsPathRooted(relativeTo) != path.IsPathRooted(destination))
            {
                return destination;
            }

            var from = Split(relativeTo);
            var to = Split(destination);

            var common = 0;
            var max = Math.Min(from.Length, to.Length);

            while (common < max && SegmentEquals(from[common], to[common], StringComparison.Ordinal))
            {
                common++;
            }

            if (common == from.Length && common == to.Length)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            if (common < from.Length)
            {
                for (var i = from.Length - 1; i >= common; i--)
                {
                    sb.Append("..");
                    if (i >= 1)
                    {
                        sb.Append(from[i - 1][^1]);
                    }
                    else
                    {
                        sb.Append(separator);
                    }
                }
            }

            for (var i = common; i < to.Length; i++)
            {
                sb.Append(to[i]);
            }

            return sb.Length == 0 ? string.Empty : sb.ToString();
        }

        private static bool SegmentEquals(string a, string b, StringComparison comparison)
        {
            var aSpan = a[^1] is '/' or '\\' ? a.AsSpan()[..^1] : a.AsSpan();
            var bSpan = b[^1] is '/' or '\\' ? b.AsSpan()[..^1] : b.AsSpan();
            return aSpan.Equals(bSpan, comparison);
        }

        public static string[] Split(string path) => SegmentSplitRegex().Split(path);

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
