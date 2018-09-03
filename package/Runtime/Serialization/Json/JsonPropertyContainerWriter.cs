namespace Unity.Properties.Serialization
{
    public static class JsonPropertyContainerWriter
    {
        private static readonly StringBuffer s_StringBuffer = new StringBuffer(1024);
        private static readonly JsonPropertyVisitor s_DefaultVisitor = new JsonPropertyVisitor { StringBuffer = s_StringBuffer };
        
        public static string Write<TContainer>(TContainer container, JsonPropertyVisitor visitor = null) 
            where TContainer : class, IPropertyContainer
        {
            visitor = WritePrefix(visitor);
            container.PropertyBag.Visit(container, visitor);
            WriteSuffix(visitor);

            return s_StringBuffer.ToString();
        }
        
        public static string WriteStruct<TContainer>(ref TContainer container, JsonPropertyVisitor visitor = null) 
            where TContainer : struct, IPropertyContainer
        {
            visitor = WritePrefix(visitor);
            container.PropertyBag.VisitStruct(ref container, visitor);
            WriteSuffix(visitor);

            return s_StringBuffer.ToString();
        }

        private static JsonPropertyVisitor WritePrefix(JsonPropertyVisitor visitor)
        {
            if (null == visitor)
            {
                visitor = s_DefaultVisitor;
            }

            visitor.StringBuffer = s_StringBuffer;

            s_StringBuffer.Clear();
            s_StringBuffer.Append(' ', JsonPropertyVisitor.Style.Space * visitor.Indent);
            s_StringBuffer.Append("{\n");

            visitor.Indent++;
            return visitor;
        }
        
        private static void WriteSuffix(JsonPropertyVisitor visitor)
        {
            visitor.Indent--;

            s_StringBuffer.Length -= 2;
            s_StringBuffer.Append("\n");
            s_StringBuffer.Append(' ', JsonPropertyVisitor.Style.Space * visitor.Indent);
            s_StringBuffer.Append("}");
        }
    }
}