
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal class IMGUIAdapter :
        IClassStructContainerGenericUIAdapter
        , IGenericCollectionAdapter
        , IGenericUnsupportedAdapter
    {
        #region IContainerGenericUIAdapter

        public bool BeginClassContainer<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : class, IPropertyContainer
            where TValue : IPropertyContainer
            => BeginContainer(ref container, ref context);

        public void EndClassContainer<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : class, IPropertyContainer
            where TValue : IPropertyContainer
            => EndContainer(ref container, ref context);

        public bool BeginStructContainer<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer
            where TValue : IPropertyContainer
            => BeginContainer(ref container, ref context);

        public void EndStructContainer<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : struct, IPropertyContainer
            where TValue : IPropertyContainer
            => EndContainer(ref container, ref context);

        #endregion // IContainerGenericUIAdapter

        #region ICollectionValueUIAdapter

        public bool BeginClassCollection<TContainer, TValue>(TContainer container, ref UIVisitContext<IList<TValue>> context)
            where TContainer : class, IPropertyContainer
            => BeginCollection(ref container, ref context);

        public void EndClassCollection<TContainer, TValue>(TContainer container, ref UIVisitContext<IList<TValue>> context) where TContainer : class, IPropertyContainer
        {
            var property = context.Property as IListClassProperty<TContainer, TValue>;
            var cRef = container;
            EndCollection(ref container, ref context, () => property?.AddNew(cRef));
        }

        public bool BeginStructCollection<TContainer, TValue>(TContainer container, ref UIVisitContext<IList<TValue>> context)
            where TContainer : struct, IPropertyContainer
            => BeginCollection(ref container, ref context);

        public void EndStructCollection<TContainer, TValue>(TContainer container, ref UIVisitContext<IList<TValue>> context) where TContainer : struct, IPropertyContainer
        {
            var property = context.Property as IListStructProperty<TContainer, TValue>;
            var cRef = container;
            EndCollection(ref container, ref context, () => property?.AddNew(ref cRef));
        }

        #endregion // ICollectionValueUIAdapter

        #region IUnsupportedValueAdapter

        public bool UnsupportedClass<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context) where TContainer : class, IPropertyContainer
        {
            if (context.Value is IPropertyContainer)
            {
                return context.Visitor.ContinueVisit;
            }

            if (context.Property is ValueClassProperty<TContainer, TValue> ||
                context.Property is IListTypedItemClassProperty<TValue>)
            {
                return IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.UnsupportedPropertyField);
            }
            return context.Visitor.ContinueVisit;
        }

        public bool UnsupportedStruct<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context) where TContainer : struct, IPropertyContainer
        {
            if (context.Value is IPropertyContainer)
            {
                return context.Visitor.ContinueVisit;
            }

            if (context.Property is ValueStructProperty<TContainer, TValue> ||
                context.Property is IListTypedItemStructProperty<TValue>)
            {
                return IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.UnsupportedPropertyField);
            }
            return context.Visitor.ContinueVisit;
        }

        #endregion // IUnsupportedValueAdapter

        #region Implementation

        private static bool BeginContainer<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : IPropertyContainer
            where TValue : IPropertyContainer
        {
            if (context.IsListItem)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(context.Label, GUILayout.MaxWidth(16.0f));
                EditorGUILayout.BeginVertical();
            }
            else
            {
                using (IMGUIScopes.MakePrefabScopes(ref container, ref context))
                {
                    EditorGUILayout.LabelField(context.Label);
                }

                ++EditorGUI.indentLevel;
            }
            return true;
        }

        private static void EndContainer<TContainer, TValue>(ref TContainer container, ref UIVisitContext<TValue> context)
            where TContainer : IPropertyContainer
            where TValue : IPropertyContainer
        {
            if (context.IsListItem)
            {
                EditorGUILayout.EndVertical();
                if (GUILayout.Button(TinyIcons.Remove, GUILayout.Width(16.0f), GUILayout.Height(16.0f)))
                {
                    context.Visitor.RemoveAtIndex = context.Index;
                }
                EditorGUILayout.EndHorizontal();
                TinyGUILayout.Separator(TinyColors.Inspector.Separator, 2.0f);
            }
            else
            {
                --EditorGUI.indentLevel;
            }
        }

        private static bool BeginCollection<TContainer, TValue>(ref TContainer container, ref UIVisitContext<IList<TValue>> context)
            where TContainer : IPropertyContainer
        {
            if (context.Targets.Count > 1)
            {
                EditorGUILayout.HelpBox("Editing an array with multiple targets is not supported.", MessageType.Info);
                return false;
            }

            using (IMGUIScopes.MakePrefabScopes(ref container, ref context, 21))
            {
                EditorGUILayout.LabelField(context.Label);
            }
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15.0f);
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical(GUI.skin.box);

            return true;
        }

        private static void EndCollection<TContainer, TValue>(ref TContainer container, ref UIVisitContext<IList<TValue>> context, Action addDel)
            where TContainer : IPropertyContainer
        {
            if (context.Targets.Count > 1)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            try
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add", GUILayout.Width(32.0f), GUILayout.Height(16.0f)))
                {
                    addDel();
                }

                GUILayout.Space(15.0f);
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5.0f);
            EditorGUILayout.EndHorizontal();
        }

        #endregion // Implementation
    }
}
