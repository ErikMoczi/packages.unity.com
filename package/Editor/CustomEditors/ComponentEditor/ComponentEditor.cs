
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal class ComponentEditor : TinyCustomEditor, IComponentEditor
    {
        internal static ComponentEditor CreateDefault(TinyContext context) => new ComponentEditor(context);

        protected ComponentEditor(TinyContext context)
            :base(context)
        {
        }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            foreach (var field in context.Value.Type.Dereference(TinyContext.Registry).Fields)
            {
                VisitField(ref context, field.Name);
            }
            return context.Visitor.StopVisit;
        }

        protected bool AddComponentToTargetButton(UIVisitContext<TinyObject> context, TinyType.Reference typeRef)
        {
            return AddComponentToTargetButton(context, typeRef, TinyType.Reference.None);
        }
        
        protected bool RemoveComponentToTargetButton(UIVisitContext<TinyObject> context, TinyType.Reference typeRef)
        {
            var buttonPressed = false;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            try
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button($"Remove {typeRef.Name}"))
                {
                    buttonPressed = true;
                    foreach (var target in context.Targets.OfType<TinyEntity>())
                    {
                        var component = target.GetComponent(typeRef);
                        if (null == component)
                        {
                            continue;
                        }

                        target.RemoveComponent(typeRef);
                        context.MainTarget<TinyEntity>().Registry.Context.GetManager<BindingsManager>().Transfer(target);
                    }
                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                }
                GUILayout.FlexibleSpace();
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            return buttonPressed;
        }

        protected TinyEntity GetRootEntity(TinyEntity target)
        {
            var entity = target;
            while (true)
            {
                if (!entity.HasTransformNode())
                {
                    return entity;
                }

                var parent = entity.Parent().Dereference(TinyContext.Registry);
                if (null == parent)
                {
                    return entity;
                }

                entity = parent;
            }
        }

        protected bool AddComponentToTargetButton(UIVisitContext<TinyObject> context, TinyType.Reference typeRef, TinyType.Reference afterTypeRef)
        {
            var buttonPressed = false;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            try
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button($"Add {typeRef.Name}"))
                {
                    buttonPressed = true;
                    foreach (var target in context.Targets.OfType<TinyEntity>())
                    {
                        var component = target.GetComponent(typeRef);
                        if (null != component)
                        {
                            continue;
                        }

                        var added = target.AddComponent(typeRef);

                        var index = -1;
                        for (var i = 0; i < target.Components.Count; ++i)
                        {
                            var c = target.Components[i];
                            if (c.Type.Equals(afterTypeRef))
                            {
                                index = i + 1;
                                break;
                            }
                        }

                        if (index >= 0)
                        {
                            target.Components.Remove(added);
                            target.Components.Insert(index, added);
                        }
                        context.MainTarget<TinyEntity>().Registry.Context.GetManager<BindingsManager>().Transfer(target);
                    }
                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                }
                GUILayout.FlexibleSpace();
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            return buttonPressed;
        }
    }
}
