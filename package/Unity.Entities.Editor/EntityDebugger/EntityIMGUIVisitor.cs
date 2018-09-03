using System;
using UnityEngine;
using Unity.Properties;
using Unity.Mathematics;
using UnityEditor;

namespace Unity.Entities.Editor
{

    public class EntityIMGUIVisitor : IPropertyVisitor
        , IBuiltInPropertyVisitor
        , IPropertyVisitor<Unity.Mathematics.quaternion>
        , IPropertyVisitor<Unity.Mathematics.float2>
        , IPropertyVisitor<Unity.Mathematics.float3>
        , IPropertyVisitor<Unity.Mathematics.float4>
        , IPropertyVisitor<Unity.Mathematics.float4x4>
        , IPropertyVisitor<Unity.Mathematics.float3x3>
        , IPropertyVisitor<Unity.Mathematics.float2x2>

    {

        public void Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : IPropertyContainer
        {
            var property = context.Value;
            GUILayout.Label(property.ToString());
        }

        public void VisitEnum<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context) where TContainer : IPropertyContainer where TValue : struct
        {
        }

        public bool BeginContainer<TContainer, TValue>(ref TContainer container, SubtreeContext<TValue> context) where TContainer : IPropertyContainer
        {
            EditorGUI.indentLevel++;
            return true;
        }

        public void EndContainer<TContainer, TValue>(ref TContainer container, SubtreeContext<TValue> context) where TContainer : IPropertyContainer
        {
            EditorGUI.indentLevel--;
        }

        public bool BeginList<TContainer, TValue>(ref TContainer container, ListContext<TValue> context) where TContainer : IPropertyContainer
        {
            return true;
        }

        public void EndList<TContainer, TValue>(ref TContainer container, ListContext<TValue> context) where TContainer : IPropertyContainer
        {
        }
        
        public void Visit<TContainer>(ref TContainer container, VisitContext<Unity.Mathematics.quaternion> context) where TContainer : IPropertyContainer
        {
            var value = context.Value.value;
            EditorGUILayout.Vector4Field(context.Property.Name, new Vector4(value.x, value.y, value.z, value.w) );
        }
        
        public void Visit<TContainer>(ref TContainer container, VisitContext<float2> context) where TContainer : IPropertyContainer
        {
            EditorGUILayout.Vector2Field(context.Property.Name, (Vector2) context.Value);
        }
        
        public void Visit<TContainer>(ref TContainer container, VisitContext<float3> context) where TContainer : IPropertyContainer
        {
            EditorGUILayout.Vector3Field(context.Property.Name, (Vector3) context.Value);
        }
        
        public void Visit<TContainer>(ref TContainer container, VisitContext<float4> context) where TContainer : IPropertyContainer
        {
            EditorGUILayout.Vector4Field(context.Property.Name, (Vector4) context.Value);
        }
        
        public void Visit<TContainer>(ref TContainer container, VisitContext<float2x2> context) where TContainer : IPropertyContainer
        {
            var value = context.Value;
            GUILayout.Label(context.Property.Name);
            EditorGUILayout.Vector2Field("", (Vector2) value.m0);
            EditorGUILayout.Vector2Field("", (Vector2) value.m1);
        }
        
        public void Visit<TContainer>(ref TContainer container, VisitContext<float3x3> context) where TContainer : IPropertyContainer
        {
            var value = context.Value;
            GUILayout.Label(context.Property.Name);
            EditorGUILayout.Vector3Field("", (Vector3) value.m0);
            EditorGUILayout.Vector3Field("", (Vector3) value.m1);
            EditorGUILayout.Vector3Field("", (Vector3) value.m2);
        }
        
        public void Visit<TContainer>(ref TContainer container, VisitContext<float4x4> context) where TContainer : IPropertyContainer
        {
            var value = context.Value;
            GUILayout.Label(context.Property.Name);
            EditorGUILayout.Vector4Field("", (Vector4) value.m0);
            EditorGUILayout.Vector4Field("", (Vector4) value.m1);
            EditorGUILayout.Vector4Field("", (Vector4) value.m2);
            EditorGUILayout.Vector4Field("", (Vector4) value.m3);
        }

        #region IBuiltInPropertyVisitor
        public void Visit<TContainer>(ref TContainer container, VisitContext<sbyte> context) where TContainer : IPropertyContainer
        {
            DoField(ref container, context, (label, val) => (sbyte)Mathf.Clamp(EditorGUILayout.IntField(label, val), sbyte.MinValue, sbyte.MaxValue));
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<short> context) where TContainer : IPropertyContainer
        {
            DoField(ref container, context, (label, val) => (short)Mathf.Clamp(EditorGUILayout.IntField(label, val), short.MinValue, short.MaxValue));
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<int> context) where TContainer : IPropertyContainer
        {
            DoField(ref container, context, (label, val) => EditorGUILayout.IntField(label, val));
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<long> context) where TContainer : IPropertyContainer
        {
            DoField(ref container, context, (label, val) => EditorGUILayout.LongField(label, val));
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<byte> context) where TContainer : IPropertyContainer
        {
            DoField(ref container, context, (label, val) => (byte)Mathf.Clamp(EditorGUILayout.IntField(label, val), byte.MinValue, byte.MaxValue));
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<ushort> context) where TContainer : IPropertyContainer
        {
            DoField(ref container, context, (label, val) => (ushort)Mathf.Clamp(EditorGUILayout.IntField(label, val), ushort.MinValue, ushort.MaxValue));
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<uint> context) where TContainer : IPropertyContainer
        {
            DoField(ref container, context, (label, val) => (uint)Mathf.Clamp(EditorGUILayout.LongField(label, val), uint.MinValue, uint.MaxValue));
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<ulong> context) where TContainer : IPropertyContainer
        {
            DoField(ref container, context, (label, val) =>
            {
                var text = EditorGUILayout.TextField(label, val.ToString());
                ulong num;
                ulong.TryParse(text, out num);
                return num;
            });
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<float> context) where TContainer : IPropertyContainer
        {
            DoField(ref container, context, (label, val) => EditorGUILayout.FloatField(label, val));
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<double> context) where TContainer : IPropertyContainer
        {
            DoField(ref container, context, (label, val) => EditorGUILayout.DoubleField(label, val));
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<bool> context) where TContainer : IPropertyContainer
        {
            DoField(ref container, context, (label, val) => EditorGUILayout.Toggle(label, val));
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<char> context) where TContainer : IPropertyContainer
        {
            DoField(ref container, context, (label, val) =>
            {
                var text = EditorGUILayout.TextField(label, val.ToString());
                var c = (string.IsNullOrEmpty(text) ? '\0' : text[0]);
                return c;
            });
        }

        public void Visit<TContainer>(ref TContainer container, VisitContext<string> context) where TContainer : IPropertyContainer
        {
            var property = context.Property;

            if (property == null)
            {
                return;
            }

            GUILayout.Label(context.Value, EditorStyles.boldLabel);
        }
        #endregion

        private void DoField<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context, Func<GUIContent, TValue, TValue> onGUI)
            where TContainer : IPropertyContainer
        {
            var property = context.Property;

            if (property == null)
            {
                return;
            }

            var previous = context.Value;
            onGUI(new GUIContent(property.Name), previous);
        }
    }
}
