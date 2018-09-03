using System;
using System.Globalization;

namespace Unity.Properties.Serialization
{
    public class JsonPropertyVisitor : PropertyVisitor,
        ICustomVisit<bool>,
        ICustomVisit<float>,
        ICustomVisit<double>,
        ICustomVisit<string>
    {
        public static class Style
        {
            public const int Space = 4;
        }
        
        private static readonly StringBuffer s_StringBuffer = new StringBuffer(1024);

        public StringBuffer StringBuffer = s_StringBuffer;
        public int Indent;

        public override string ToString()
        {
            return StringBuffer?.ToString() ?? string.Empty;
        }

        protected void AppendPrimitive(string value)
        {
            if (IsListItem)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(value);
                StringBuffer.Append(",\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(Property.Name);
                StringBuffer.Append("\": ");
                StringBuffer.Append(value);
                StringBuffer.Append(",\n");
            }
        }

        protected override void Visit<TValue>(TValue value)
        {
            if (typeof(TValue).IsEnum)
            {
                AppendPrimitive(Convert.ToInt32(value).ToString());
            }
            else
            {
                AppendPrimitive(value.ToString());
            }
        }

        void ICustomVisit<bool>.CustomVisit(bool value)
        {
            AppendPrimitive(value ? "true" : "false");
        }

        void ICustomVisit<float>.CustomVisit(float value)
        {
            AppendPrimitive(value.ToString(CultureInfo.InvariantCulture));
        }

        void ICustomVisit<double>.CustomVisit(double value)
        {
            AppendPrimitive(value.ToString(CultureInfo.InvariantCulture));
        }

        void ICustomVisit<string>.CustomVisit(string value)
        {
            AppendPrimitive(JsonUtility.EncodeJsonString(value));
        }

        public override bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            base.BeginContainer(ref container, context);
            if (IsListItem)
            {
                Indent--;
                StringBuffer.Length -= 1;
                StringBuffer.Append(StringBuffer[StringBuffer.Length - 1] == ',' ? " {\n" : "{\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append("\"");
                StringBuffer.Append(Property.Name);
                StringBuffer.Append("\": {\n");
            }

            Indent++;

            return true;
        }

        public override void EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            base.EndContainer(ref container, context);
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

            if (IsListItem)
            {
                Indent++;
            }

            StringBuffer.Append("},\n");
        }

        public override bool BeginList<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            base.BeginList(ref container, context);
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.Append('\"');
            StringBuffer.Append(Property.Name);
            StringBuffer.Append("\": [\n");
            Indent++;
            return true;
        }

        public override void EndList<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            base.EndList(ref container, context);
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

            var skipNewline = StringBuffer[StringBuffer.Length - 1] == '}' &&
                StringBuffer[StringBuffer.Length - 3] == ' ';
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