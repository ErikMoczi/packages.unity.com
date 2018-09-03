using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Properties.Serialization
{
    public static class JsonPropertyContainerWriter
    {
        private static readonly StringBuffer s_StringBuffer = new StringBuffer(1024);
        private static readonly JsonPropertyVisitor s_DefaultVisitor = new JsonPropertyVisitor { StringBuffer = s_StringBuffer };
        
        public static string Write<TContainer>(TContainer container, JsonPropertyVisitor visitor = null) 
            where TContainer : class, IPropertyContainer
        {
            if (null == visitor)
            {
                visitor = s_DefaultVisitor;
            }

            WritePrefix(visitor);
            container.PropertyBag.Visit(container, visitor);
            WriteSuffix(visitor);

            return s_StringBuffer.ToString();
        }
        
        public static string WriteStruct<TContainer>(ref TContainer container, JsonPropertyVisitor visitor = null) 
            where TContainer : struct, IPropertyContainer
        {
            if (null == visitor)
            {
                visitor = s_DefaultVisitor;
            }

            WritePrefix(visitor);
            container.PropertyBag.VisitStruct(ref container, visitor);
            WriteSuffix(visitor);

            return s_StringBuffer.ToString();
        }

        private static void WritePrefix(JsonPropertyVisitor visitor)
        {
            Assert.IsNotNull(visitor);

            visitor.StringBuffer = s_StringBuffer;

            s_StringBuffer.Clear();
            s_StringBuffer.Append(' ', JsonPropertyVisitor.Style.Space * visitor.Indent);
            s_StringBuffer.Append("{\n");

            visitor.Indent++;
        }
        
        private static void WriteSuffix(JsonPropertyVisitor visitor)
        {
            Debug.Assert(visitor != null);

            visitor.Indent--;

            s_StringBuffer.Length -= 2;
            s_StringBuffer.Append("\n");
            s_StringBuffer.Append(' ', JsonPropertyVisitor.Style.Space * visitor.Indent);
            s_StringBuffer.Append("}");
        }
    }
}