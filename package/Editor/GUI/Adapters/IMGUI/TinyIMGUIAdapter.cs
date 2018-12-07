
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal class TinyIMGUIAdapter : TinyAdapter
        ,IVisitValueAdapter<TinyId>
        ,IVisitValueAdapter<TinyTypeId>
        ,IVisitValueAdapter<TinyEnum.Reference>
        ,IVisitValueAdapter<TinyEntity.Reference>
        ,IVisitValueAdapter<TinyEntityGroup.Reference>
        ,IVisitValueAdapter<TinyType.Reference>
    {
        public TinyIMGUIAdapter(TinyContext tinyContext)
            :base(tinyContext)
        {
        }

        #region IVisitUIValueAdapter<TinyId>

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyId> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, PropertyField);


        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyId> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, PropertyField);

        #endregion // IVisitUIValueAdapter<TinyId>

        #region IVisitUIValueAdapter<TinyTypeId>

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyTypeId> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyTypeId> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, PropertyField);

        #endregion // IVisitUIValueAdapter<TinyTypeId>

        #region IVisitUIValueAdapter<TinyEnum.Reference>

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyEnum.Reference> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyEnum.Reference> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, PropertyField);

        #endregion // IVisitUIValueAdapter<TinyEnum.Reference>

        #region IVisitUIValueAdapter<TinyEntity.Reference>

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyEntity.Reference> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyEntity.Reference> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, PropertyField);

        #endregion // IVisitUIValueAdapter<TinyEntity.Reference>
        
        #region IVisitUIValueAdapter<TinyEntityGroup.Reference>

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyEntityGroup.Reference> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyEntityGroup.Reference> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, PropertyField);

        #endregion // IVisitUIValueAdapter<TinyEntity.Reference>

        #region IVisitUIValueAdapter<TinyType.Reference>

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyType.Reference> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyType.Reference> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, PropertyField);

        #endregion // IVisitUIValueAdapter<TinyType.Reference>

        #region Implementation

        private static bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<TinyId> context)
            where TContainer : IPropertyContainer
        {
            GUI.enabled = false;
            EditorGUILayout.LabelField(context.Label, context.Value.ToString());
            return context.Visitor.StopVisit;
        }

        private static bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<TinyTypeId> context)
            where TContainer : IPropertyContainer
        {
            GUI.enabled = false;
            EditorGUILayout.EnumPopup(context.Label, context.Value);
            return context.Visitor.StopVisit;
        }

        private bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<TinyEnum.Reference> context) where TContainer : IPropertyContainer
        {
            var value = context.Value;
            var type = value.Type.Dereference(TinyContext.Registry);
            var field = type.FindFieldById(value.Id);
            var names = type.Fields.Select(f => new GUIContent(f.Name)).ToArray();
            var index = Mathf.Clamp(type.Fields.IndexOf(field), 0, names.Length);

            if (names.Length == 0)
            {
                EditorGUILayout.Popup(new GUIContent(context.Label), -1, names);
                return context.Visitor.StopVisit;
            }

            EditorGUI.BeginChangeCheck();
            index = EditorGUILayout.Popup(new GUIContent(context.Label), index, names);
            if (EditorGUI.EndChangeCheck())
            {
                var newType = type.Fields[index];
                context.Value = new TinyEnum.Reference(type, newType.Id);
            }

            return context.Visitor.StopVisit;
        }

        private bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<TinyEntity.Reference> context) where TContainer : IPropertyContainer
        {
            var entityRef = context.Value;
            var entity = entityRef.Dereference(TinyContext.Registry);
            var view = entity?.View;
            view?.RefreshName();
            EditorGUI.BeginChangeCheck();

            var allowSceneObjects = false;

            var targets = context.Targets?.OfType<TinyEntity>().ToList();
            var entityGroup = targets?.FirstOrDefault()?.EntityGroup;
            allowSceneObjects = (targets?.All(e => e.EntityGroup == entityGroup) ?? false) &&
                DragAndDrop.objectReferences
                    .OfType<GameObject>()
                    .Select(go => go.GetComponent<TinyEntityView>())
                    .NotNull()
                    .Select(v => v.EntityRef.Dereference(v.Registry))
                    .NotNull()
                    .All(e => e.EntityGroup == entityGroup);

            var newView = (TinyEntityView)EditorGUILayout.ObjectField(context.Label, view, typeof(TinyEntityView), allowSceneObjects);
            if (newView && newView.EntityRef.Dereference(newView.Registry).EntityGroup != entityGroup)
            {
                EditorGUILayout.HelpBox($"Cross-references between multiple entity groups are not supported in {TinyConstants.ApplicationName}.", MessageType.Warning);
            }

            if (EditorGUI.EndChangeCheck())
            {
                context.Value = newView?.EntityRef ?? TinyEntity.Reference.None;
            }

            return context.Visitor.StopVisit;
        }
        
        private bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<TinyEntityGroup.Reference> context) where TContainer : IPropertyContainer
        {
            GUI.enabled = false;
            EditorGUILayout.LabelField(context.Label, context.Value.Name);
            return context.Visitor.StopVisit;
        }

        private bool PropertyField<TContainer>(ref TContainer container, ref UIVisitContext<TinyType.Reference> context)
            where TContainer : IPropertyContainer
        {
            var value = context.Value;
            var type = value.Dereference(TinyContext.Registry) ?? TinyType.BuiltInTypes.FirstOrDefault(t => t.Ref.Equals(value));

            var path = Persistence.GetAssetPath(type);
            var tType = AssetDatabase.LoadAssetAtPath<UTType>(path);

            GUI.enabled = false;
            if (null != tType)
            {
                EditorGUILayout.ObjectField(context.Label, tType, typeof(UTType), true);
            }
            else if (null != type)
            {
                EditorGUILayout.LabelField(context.Label, $"{value.Name} (Builtin Type)", EditorStyles.objectField);
            }
            else
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField(context.Label, $"{value.Name} (Missing Type)", EditorStyles.objectField);
                GUI.color = Color.white;
            }


            return context.Visitor.StopVisit;
        }

        #endregion // Implementation

    }
}
