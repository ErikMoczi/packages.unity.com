using System;
using System.Collections.Generic;
using Unity.Entities.Properties;
using Unity.Properties;
using Unity.Properties.Serialization;

namespace Unity.AI.Planner.Utility
{
    class ToStringPropertyVisitor : PropertyVisitor, ICustomVisitPrimitives, IPrimitivePropertyVisitor
    {
        public static class Style
        {
            public const int Space = 4;
        }

        static readonly StringBuffer s_StringBuffer = new StringBuffer(1024);
        static HashSet<Type> s_SupportedPrimitiveTypes;

        public StringBuffer StringBuffer = s_StringBuffer;
        public int Indent;

        public ToStringPropertyVisitor()
        {
            Reset();
        }

        public void Reset()
        {
            StringBuffer.Clear();
        }

        public override string ToString()
        {
            return StringBuffer?.ToString() ?? string.Empty;
        }

        public HashSet<Type> SupportedPrimitiveTypes()
        {
            if (s_SupportedPrimitiveTypes == null)
            {
                s_SupportedPrimitiveTypes = new HashSet<Type>();
                foreach (var it in typeof(ICustomVisitPrimitives).GetInterfaces())
                {
                    if (it.IsGenericType && typeof(ICustomVisit<>) == it.GetGenericTypeDefinition())
                    {
                        var genArgs = it.GetGenericArguments();
                        if (genArgs.Length == 1)
                        {
                            s_SupportedPrimitiveTypes.Add(genArgs[0]);
                        }
                    }
                }
            }

            return s_SupportedPrimitiveTypes;
        }

        protected void AppendPrimitive(string value)
        {
            if (IsListItem)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(value);
                StringBuffer.Append("\n");
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                var propertyName = Property.Name;
                var isTypeId = propertyName.IndexOf("$TypeId", StringComparison.OrdinalIgnoreCase) >= 0;
                if (isTypeId)
                {
                    if (Indent <= 1)
                    {
                        StringBuffer.Append("\n");
                        StringBuffer.Append(' ', Style.Space * Indent);
                    }

                    StringBuffer.Append("<i>");
                }
                else
                {
                    StringBuffer.Append(propertyName);
                    StringBuffer.Append(": ");
                }

                StringBuffer.Append(value);
                StringBuffer.Append("\n");

                if (isTypeId)
                    StringBuffer.Append("</i>");
            }
        }

        protected void AppendPrefix()
        {
            if (IsListItem)
            {
                StringBuffer.Append(' ', Style.Space * Indent);
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(Property.Name);
                StringBuffer.Append(": ");
            }
        }

        protected void AppendSuffix()
        {
            StringBuffer.Append("\n");
        }

        protected override void Visit<TValue>(TValue value)
        {
            if (typeof(TValue).IsEnum)
                AppendPrimitive(value.ToString());
            else
                AppendPrimitive(value?.ToString());
        }

        void ICustomVisit<bool>.CustomVisit(bool value)
        {
            AppendPrimitive(value ? "true" : "false");
        }

        void ICustomVisit<byte>.CustomVisit(byte value)
        {
            AppendPrefix();
            StringBuffer.Append(value);
            AppendSuffix();
        }

        void ICustomVisit<sbyte>.CustomVisit(sbyte value)
        {
            AppendPrefix();
            StringBuffer.Append(value);
            AppendSuffix();
        }

        void ICustomVisit<ushort>.CustomVisit(ushort value)
        {
            AppendPrefix();
            StringBuffer.Append(value);
            AppendSuffix();
        }

        void ICustomVisit<short>.CustomVisit(short value)
        {
            AppendPrefix();
            StringBuffer.Append(value);
            AppendSuffix();
        }

        void ICustomVisit<uint>.CustomVisit(uint value)
        {
            AppendPrefix();
            StringBuffer.Append(value);
            AppendSuffix();
        }

        void ICustomVisit<int>.CustomVisit(int value)
        {
            AppendPrefix();
            StringBuffer.Append(value);
            AppendSuffix();
        }

        void ICustomVisit<ulong>.CustomVisit(ulong value)
        {
            AppendPrefix();
            StringBuffer.Append(value);
            AppendSuffix();
        }

        void ICustomVisit<long>.CustomVisit(long value)
        {
            AppendPrefix();
            StringBuffer.Append(value);
            AppendSuffix();
        }

        void ICustomVisit<char>.CustomVisit(char value)
        {
            AppendPrimitive(value.ToString());
        }

        void ICustomVisit<float>.CustomVisit(float value)
        {
            AppendPrefix();
            StringBuffer.Append(value.ToString("F3"));
            AppendSuffix();
        }

        void ICustomVisit<double>.CustomVisit(double value)
        {
            AppendPrefix();
            StringBuffer.Append(value.ToString("F3"));
            AppendSuffix();
        }

        void ICustomVisit<string>.CustomVisit(string value)
        {
            AppendPrimitive(value);
        }

        protected override bool BeginContainer()
        {
            if (IsListItem)
            {
                Indent--;
            }
            else
            {
                StringBuffer.Append(' ', Style.Space * Indent);
                StringBuffer.Append(Property.Name);
                StringBuffer.Append(": ");
            }

            Indent++;
            return true;
        }

        protected override void EndContainer()
        {
            Indent--;

            // Remove the trailing comma
            if (IsListItem)
                Indent++;

            if (Indent == 0)
                StringBuffer.Append("\n");
        }

        protected override bool BeginCollection()
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.Append(Property.Name);
            Indent++;
            return true;
        }

        protected override void EndCollection()
        {
            Indent--;
//            StringBuffer.Append("\n");
        }
    }
}
