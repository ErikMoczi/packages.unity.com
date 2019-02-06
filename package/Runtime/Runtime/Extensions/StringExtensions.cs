

using System.IO;

namespace Unity.Tiny
{
    internal static class StringExtensions
    {
        public static string SingleQuoted(this string str)
        {
            return $"'{str}'";
        }

        public static string DoubleQuoted(this string str)
        {
            return $"\"{str}\"";
        }

        public static string Braced(this string str)
        {
            return $"{{{str}}}";
        }

        public static string ToForwardSlash(this string str)
        {
            return str.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
