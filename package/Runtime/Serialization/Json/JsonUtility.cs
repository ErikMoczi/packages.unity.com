#if NET_4_6
using System.Text;

namespace Unity.Properties.Serialization
{
    public static class JsonUtility
    {
        private static readonly StringBuilder s_Builder = new StringBuilder(64);

        public static string EncodeJsonString(string s)
        {
            if (s == null)
            {
                return "null";
            }

            var b = s_Builder;
            b.Clear();
            b.Append("\"");
            
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\': b.Append("\\\\"); break; // TODO: Unicode look-ahead \u1234
                    case '\"': b.Append("\\\""); break;
                    case '\t': b.Append("\\t"); break;
                    case '\r': b.Append("\\r"); break;
                    case '\n': b.Append("\\n"); break;
                    case '\b': b.Append("\\b"); break;
                    default: b.Append(c); break;
                }
            }
            b.Append("\"");
            return s_Builder.ToString();
        }

        public static string DecodeJsonString(string s)
        {
            if (s == null || s == "null")
            {
                return null;
            }

            var b = s_Builder;
            b.Clear();
            var escaped = false;
            
            foreach (var c in s)
            {
                if (escaped)
                {
                    switch (c)
                    {
                        case '\\': b.Append('\\'); break; // TODO: handle Unicode look-ahead \u1234
                        case '\"': b.Append('\"'); break;
                        case 't': b.Append('\t'); break;
                        case 'r': b.Append('\r'); break;
                        case 'n': b.Append('\n'); break;
                        case 'b': b.Append('\b'); break;
                        default: break; // TODO: error
                    }
                    escaped = false;
                }
                else
                {
                    switch (c)
                    {
                        case '\\': escaped = true; break;
                        default: b.Append(c); break;
                    }
                }
            }

            return b.ToString();
        }

    }
}
#endif // NET_4_6
