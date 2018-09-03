#if (NET_4_6 || NET_STANDARD_2_0)

using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Properties.Serialization
{
    public static class JsonPropertyContainerWriter
    {
        private static readonly Visitor s_DefaultVisitor = new Visitor();
        
        /// <summary>
        /// Override the visitor to append type information for polymorphic serialization
        /// </summary>
        private class Visitor : JsonPropertyVisitor
        {
            public override bool BeginContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            {
                if (!base.BeginContainer(container, context))
                {
                    return false;
                }
                WriteTypeName(typeof(TValue));
                return true;
            }

            public override bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            {
                if (!base.BeginContainer(ref container, context))
                {
                    return false;
                }
                WriteTypeName(typeof(TValue));
                return true;
            }

            private void WriteTypeName(Type type)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append($"\"$Type\": \"{type.AssemblyQualifiedName}\",\n");
            }
        }

        public static string Write<TContainer>(TContainer container, JsonPropertyVisitor visitor = null)
            where TContainer : class, IPropertyContainer
        {
            if (null == visitor)
            {
                visitor = s_DefaultVisitor;
            }

            WritePrefix(visitor);
            container.Visit(visitor);
            WriteSuffix(visitor);

            return visitor.ToString();
        }

        public static string Write<TContainer>(ref TContainer container, JsonPropertyVisitor visitor = null)
            where TContainer : struct, IPropertyContainer
        {
            if (null == visitor)
            {
                visitor = s_DefaultVisitor;
            }

            WritePrefix(visitor);
            PropertyContainer.Visit(ref container, visitor);
            WriteSuffix(visitor);

            return visitor.ToString();
        }

        private static void WritePrefix(JsonPropertyVisitor visitor)
        {
            Assert.IsNotNull(visitor);

            visitor.StringBuffer.Clear();
            visitor.StringBuffer.Append(' ', JsonPropertyVisitor.Style.Space * visitor.Indent);
            visitor.StringBuffer.Append("{\n");

            visitor.Indent++;
        }

        private static void WriteSuffix(JsonPropertyVisitor visitor)
        {
            Debug.Assert(visitor != null);

            visitor.Indent--;

            if (visitor.StringBuffer[visitor.StringBuffer.Length - 2] == '{')
            {
                visitor.StringBuffer.Length -= 1;
            }
            else
            {
                visitor.StringBuffer.Length -= 2;
            }
            
            visitor.StringBuffer.Append("\n");
            visitor.StringBuffer.Append(' ', JsonPropertyVisitor.Style.Space * visitor.Indent);
            visitor.StringBuffer.Append("}");
        }
    }
}
#endif // NET_4_6
