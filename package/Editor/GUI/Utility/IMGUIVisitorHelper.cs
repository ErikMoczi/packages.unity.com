
using System;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class IMGUIVisitorHelper
    {
        public delegate bool GUIDelegate<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : IPropertyContainer;

        public static bool AsLeafItem<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context, GUIDelegate<TContainer, TValue> action)
            where TContainer : IPropertyContainer
        {
            using (IMGUIScopes.MakePrefabScopes(ref container, ref context))
            using (IMGUIScopes.MakeOverridenScope(ref container, ref context))
            using (IMGUIScopes.MakeMixedValueScope(ref container, ref context))
            using (IMGUIScopes.MakeListScope(context))
            {
                return action(ref container, ref context);
            }
        }

        public static bool AsStructItem<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context, GUIDelegate<TContainer, TValue> action)
            where TContainer : IPropertyContainer
        {
            using (IMGUIScopes.MakeOverridenScope(ref container, ref context))
            using (IMGUIScopes.MakeMixedValueScope(ref container, ref context))
            using (IMGUIScopes.MakeListScope(context))
            {
                EditorGUILayout.BeginVertical();
                try
                {
                    return action(ref container, ref context);
                }
                finally
                {
                    EditorGUILayout.EndVertical();
                }
            }
        }

        public static bool AsContainerItem<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context, GUIDelegate<TContainer, TValue> action)
            where TContainer : IPropertyContainer
        {
            using (IMGUIScopes.MakeOverridenScope(ref container, ref context))
            using (IMGUIScopes.MakeContainerListScope(context))
            {
                return action(ref container, ref context);
            }
        }

        public static bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<bool> context)
            where TContainer : IPropertyContainer
        {
            context.Value = EditorGUILayout.Toggle(context.Label, context.Value);
            return context.Visitor.StopVisit;
        }

        public static bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<sbyte> context)
            where TContainer : IPropertyContainer
        {
            context.Value = (sbyte) Mathf.Clamp(EditorGUILayout.IntField(context.Label, context.Value), sbyte.MinValue, sbyte.MaxValue);
            return context.Visitor.StopVisit;
        }

        public static bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<byte> context)
            where TContainer : IPropertyContainer
        {
            context.Value = (byte)Mathf.Clamp(EditorGUILayout.IntField(context.Label, context.Value), byte.MinValue, byte.MaxValue);
            return context.Visitor.StopVisit;
        }

        public static bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<short> context)
            where TContainer : IPropertyContainer
        {
            context.Value = (short) Mathf.Clamp(EditorGUILayout.IntField(context.Label, context.Value), short.MinValue, short.MaxValue);
            return context.Visitor.StopVisit;
        }

        public static bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<ushort> context)
            where TContainer : IPropertyContainer
        {
            context.Value = (ushort)Mathf.Clamp(EditorGUILayout.IntField(context.Label, context.Value), ushort.MinValue, ushort.MaxValue);
            return context.Visitor.StopVisit;
        }

        public static bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<int> context)
            where TContainer : IPropertyContainer
        {
            context.Value = EditorGUILayout.IntField(context.Label, context.Value);
            return context.Visitor.StopVisit;
        }

        public static bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<uint> context)
            where TContainer : IPropertyContainer
        {
            context.Value = (uint)Mathf.Clamp(EditorGUILayout.LongField(context.Label, context.Value), uint.MinValue, uint.MaxValue);
            return context.Visitor.StopVisit;
        }

        public static bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<long> context)
            where TContainer : IPropertyContainer
        {
            context.Value = EditorGUILayout.LongField(context.Label, context.Value);
            return context.Visitor.StopVisit;
        }

        public static bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<ulong> context)
            where TContainer : IPropertyContainer
        {
            var text = EditorGUILayout.TextField(context.Label, context.Value.ToString());
            ulong.TryParse(text, out var num);
            context.Value = num;
            return context.Visitor.StopVisit;
        }

        public static bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<float> context)
            where TContainer : IPropertyContainer
        {
            context.Value = EditorGUILayout.FloatField(context.Label, context.Value);
            return context.Visitor.StopVisit;
        }

        public static bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<double> context)
            where TContainer : IPropertyContainer
        {
            context.Value = EditorGUILayout.DoubleField(context.Label, context.Value);
            return context.Visitor.StopVisit;
        }

        public static bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<string> context)
            where TContainer : IPropertyContainer
        {
            EditorGUILayout.BeginHorizontal();
            var oldIndent = EditorGUI.indentLevel;
            try
            {
                EditorGUILayout.PrefixLabel(context.Label);
                EditorGUI.indentLevel = 0;
                context.Value = EditorGUILayout.TextArea(context.Value);
            }
            finally
            {
                EditorGUI.indentLevel = oldIndent;
                EditorGUILayout.EndHorizontal();
            }
            return context.Visitor.StopVisit;
        }

        public static bool PropertyField<TContainer, TObject>(ref TContainer container, ref UIVisitContext<TObject> context)
            where TContainer : IPropertyContainer
            where TObject : UnityEngine.Object
        {
            context.Value = (TObject) EditorGUILayout.ObjectField(context.Label, context.Value, typeof(TObject), false);
            return context.Visitor.StopVisit;
        }

        public static bool UnsupportedPropertyField<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : IPropertyContainer
        {
            EditorGUILayout.LabelField(context.Label, $"{typeof(TValue).Name} is not supported.");
            return context.Visitor.StopVisit;
        }
    }
}
