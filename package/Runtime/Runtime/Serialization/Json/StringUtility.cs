

namespace Unity.Tiny.Serialization.Json
{
    internal static class StringUtility
    {
        public static string Escape(char c)
        {
            switch (c)
            {
                case '\b':
                    return "\\b";
                case '\n':
                    return "\\n";
                case '\t':
                    return "\\t";
                case '\r':
                    return "\\r";
                case '\f':
                    return "\\f";
                default:
                    return c.ToString();
            }
        }
    }
}

