#if (NET_4_6 || NET_STANDARD_2_0)

using System.Collections.Generic;

namespace Unity.Properties.Editor.Serialization
{
    public class CSharpGenerationCache
    {
        public class CodeInfo
        {
            public string Code { get; set; } = string.Empty;

            public PropertyTypeNode TypeNode { get; set; }

            public List<string> GeneratedPropertyFieldNames { get; set; } = new List<string>();

            public void Clear()
            {
                Code = string.Empty;
                TypeNode = null;
                GeneratedPropertyFieldNames = new List<string>();
            }
        }

        public void Clear()
        {
            Cache = new Dictionary<string, CodeInfo>();
        }

        public Dictionary<string, CodeInfo> Cache { get; internal set; } = new Dictionary<string, CodeInfo>();
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
