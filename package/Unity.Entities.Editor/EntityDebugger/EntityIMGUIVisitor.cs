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
            var property = context.Value;
            GUILayout.Label(property.value.ToString());
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
