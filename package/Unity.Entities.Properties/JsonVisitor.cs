using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Properties;
using Unity.Properties.Serialization;

namespace Unity.Entities.Properties
{
    public interface IOptimizedVisitor : IBuiltInPropertyVisitor
        , IPropertyVisitor<float2>
        , IPropertyVisitor<float3>
        , IPropertyVisitor<float4>
        , IPropertyVisitor<float2x2>
        , IPropertyVisitor<float3x3>
        , IPropertyVisitor<float4x4>
    {
    }

    public static class OptimizedVisitor
    {
        public static bool Supports(Type t)
        {
            return s_OptimizedSet.Contains(t);
        }
        
        private static HashSet<Type> s_OptimizedSet;

        static OptimizedVisitor()
        {
            s_OptimizedSet = new HashSet<Type>();
            foreach (var it in typeof(IOptimizedVisitor).GetInterfaces())
            {
                if (typeof(IPropertyVisitor).IsAssignableFrom(it))
                {
                    var genArgs = it.GetGenericArguments();
                    if (genArgs.Length == 1)
                    {
                        s_OptimizedSet.Add(genArgs[0]);
                    }
                }
            }
        }
    }

    public static class StringBufferExtensions
    {
        public static void AppendPropertyName(this StringBuffer sb, string propertyName)
        {
            sb.EnsureCapacity(propertyName.Length + 4);

            var buffer = sb.Buffer;
            var position = sb.Length;

            buffer[position++] = '\"';

            var len = propertyName.Length;
            for (var i = 0; i < len; i++)
            {
                buffer[position + i] = propertyName[i];
            }
            position += len;

            buffer[position++] = '\"';
            buffer[position++] = ':';
            buffer[position++] = ' ';

            sb.Length = position;
        }

        public static void AppendFloat2(this StringBuffer sb, float2 value)
        {
            sb.Append(value.x);
            sb.Append(',');
            sb.Append(value.y);
        }

        public static void AppendFloat3(this StringBuffer sb, float3 value)
        {
            sb.Append(value.x);
            sb.Append(',');
            sb.Append(value.y);
            sb.Append(',');
            sb.Append(value.z);
        }

        public static void AppendFloat4(this StringBuffer sb, float4 value)
        {
            sb.Append(value.x);
            sb.Append(',');
            sb.Append(value.y);
            sb.Append(',');
            sb.Append(value.z);
            sb.Append(',');
            sb.Append(value.w);
        }
    }

    public class JsonVisitor : JsonPropertyVisitor, IOptimizedVisitor
    {
        public void Visit<TContainer>(ref TContainer container, VisitContext<float2> context) where TContainer : IPropertyContainer
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(context.Property.Name);
            StringBuffer.Append('[');
            StringBuffer.AppendFloat2(context.Value);
            StringBuffer.Append(']');
            StringBuffer.Append(",\n");
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<float3> context) where TContainer : IPropertyContainer
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(context.Property.Name);
            StringBuffer.Append('[');
            StringBuffer.AppendFloat3(context.Value);
            StringBuffer.Append(']');
            StringBuffer.Append(",\n");
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<float4> context) where TContainer : IPropertyContainer
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(context.Property.Name);
            StringBuffer.Append('[');
            StringBuffer.AppendFloat4(context.Value);
            StringBuffer.Append(']');
            StringBuffer.Append(",\n");
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<float2x2> context) where TContainer : IPropertyContainer
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(context.Property.Name);
            StringBuffer.Append('[');
            StringBuffer.AppendFloat2(context.Value.m0);
            StringBuffer.Append(',');
            StringBuffer.AppendFloat2(context.Value.m1);
            StringBuffer.Append(']');
            StringBuffer.Append(",\n");
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<float3x3> context) where TContainer : IPropertyContainer
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(context.Property.Name);
            StringBuffer.Append('[');
            StringBuffer.AppendFloat3(context.Value.m0);
            StringBuffer.Append(',');
            StringBuffer.AppendFloat3(context.Value.m1);
            StringBuffer.Append(',');
            StringBuffer.AppendFloat3(context.Value.m2);
            StringBuffer.Append(']');
            StringBuffer.Append(",\n");
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<float4x4> context) where TContainer : IPropertyContainer
        {
            StringBuffer.Append(' ', Style.Space * Indent);
            StringBuffer.AppendPropertyName(context.Property.Name);
            StringBuffer.Append('[');
            StringBuffer.AppendFloat4(context.Value.m0);
            StringBuffer.Append(',');
            StringBuffer.AppendFloat4(context.Value.m1);
            StringBuffer.Append(',');
            StringBuffer.AppendFloat4(context.Value.m2);
            StringBuffer.Append(',');
            StringBuffer.AppendFloat4(context.Value.m3);
            StringBuffer.Append(']');
            StringBuffer.Append(",\n");
        }
    }
}
