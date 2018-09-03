using System;

namespace Unity.Properties.Serialization
{
    public class JsonPropertyVisitor : IBuiltInPropertyVisitor
    {
        public static class Style
        {
            public const int Space = 4;
        }

        public StringBuffer StringBuffer;
        public int Indent;

        public override string ToString()
        {
            return StringBuffer?.ToString() ?? string.Empty;
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<bool> context) where TContainer : IPropertyContainer
        {
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(context.Value ? "true" : "false");
                StringBuffer.Append(",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": ");
                StringBuffer.Append(context.Value ? "true" : "false");
                StringBuffer.Append(",\n");
            }
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<char> context) where TContainer : IPropertyContainer
        {
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Value);
                StringBuffer.Append("\",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": \"");
                StringBuffer.Append(context.Value);
                StringBuffer.Append("\",\n");
            }
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<sbyte> context) where TContainer : IPropertyContainer
        {
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": ");
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<byte> context) where TContainer : IPropertyContainer
        {
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": ");
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<short> context) where TContainer : IPropertyContainer
        {
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": ");
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<int> context) where TContainer : IPropertyContainer
        {
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": ");
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<long> context) where TContainer : IPropertyContainer
        {
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": ");
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<ushort> context) where TContainer : IPropertyContainer
        {
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": ");
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<uint> context) where TContainer : IPropertyContainer
        {
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": ");
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<ulong> context) where TContainer : IPropertyContainer
        {
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": ");
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<float> context) where TContainer : IPropertyContainer
        {
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": ");
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<double> context) where TContainer : IPropertyContainer
        {
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": ");
                StringBuffer.Append(context.Value);
                StringBuffer.Append(",\n");
            }
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<string> context) where TContainer : IPropertyContainer
        {
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Value);
                StringBuffer.Append("\",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": \"");
                StringBuffer.Append(context.Value);
                StringBuffer.Append("\",\n");
            }
        }

        public void Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : IPropertyContainer
        {   
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Value);
                StringBuffer.Append("\",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": \"");
                StringBuffer.Append(context.Value);
                StringBuffer.Append("\",\n");
            }
        }
            
        public void VisitEnum<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            where TContainer : IPropertyContainer
            where TValue : struct
        {   
            if (context.Index != -1)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(Convert.ToInt32(context.Value));
                StringBuffer.Append(",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": ");
                StringBuffer.Append(Convert.ToInt32(context.Value));
                StringBuffer.Append(",\n");
            }
        }

        public bool BeginContainer<TContainer, TValue>(ref TContainer container, SubtreeContext<TValue> context) where TContainer : IPropertyContainer
        {
            if (context.Index != -1)
            {
                Indent--;
                StringBuffer.Length -= 1;
                StringBuffer.Append(StringBuffer[StringBuffer.Length - 1] == ',' ? " {\n" : "{\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(context.Property.Name);
                StringBuffer.Append("\": {\n");
            }
                
            Indent++;

            return true;
        }

        public void EndContainer<TContainer, TValue>(ref TContainer container, SubtreeContext<TValue> context) where TContainer : IPropertyContainer
        {
            Indent--;

            // Remove the trailing comma
            if (StringBuffer[StringBuffer.Length - 2] == ',')
            {
                StringBuffer.Length -= 2;
                StringBuffer.Append('\n');
                StringBuffer.Append(' ', Style.Space * Indent);
            }
            else
            {
                StringBuffer.Length -= 1;
            }

            if (context.Index != -1)
            {
                Indent++;
            }

            StringBuffer.Append("},\n");
        }

        public bool BeginList<TContainer, TValue>(ref TContainer container, ListContext<TValue> context) where TContainer : IPropertyContainer
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.Append('\"');
            StringBuffer.Append(context.Property.Name);
            StringBuffer.Append("\": [\n");
            Indent++;
            return true;
        }

        public void EndList<TContainer, TValue>(ref TContainer container, ListContext<TValue> context) where TContainer : IPropertyContainer
        {
            Indent--;
                
            // Remove the trailing comma
            if (StringBuffer[StringBuffer.Length - 2] == ',')
            {
                StringBuffer.Length -= 2;
            }
            else
            {
                StringBuffer.Length -= 1;
            }

            var skipNewline = StringBuffer[StringBuffer.Length - 1] == '}' && StringBuffer[StringBuffer.Length - 3] == ' ';
            skipNewline = skipNewline | StringBuffer[StringBuffer.Length - 1] == '[';

            if (!skipNewline)
            {
                StringBuffer.Append("\n");
                StringBuffer.Append(' ', Style.Space * Indent);
            }
                
            StringBuffer.Append("],\n");
        }
    }
}