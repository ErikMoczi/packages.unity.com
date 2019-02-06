

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.Properties;

using static Unity.Tiny.EditorInspectorAttributes;

namespace Unity.Tiny
{
    internal static class Wrapper
    {
        public static Wrapper<T> Make<T>(T t)
            where T : class, IPropertyContainer
        {
            return new Wrapper<T>(t);
        }

        public static Wrapper<TinyEntity> Make(TinyEntity entity)
        {
            entity = entity.Ref.Dereference(entity.Registry); 
            return new Wrapper<TinyEntity>(entity);
        }

        public static Wrapper<TinyEntity> Make(TinyConfigurationViewer viewer)
        {
            var entity = viewer.Entity.Ref.Dereference(viewer.Entity.Registry);
            return new Wrapper<TinyEntity>(entity);
        }
    }

    internal class Wrapper<T> : IPropertyContainer
        where T : class, IPropertyContainer
    {
        public static ClassValueClassProperty<Wrapper<T>, T> TypeIdProperty { get; private set; }

        public readonly T Value;

        public IVersionStorage VersionStorage => Value.VersionStorage;
        public IPropertyBag PropertyBag => s_PropertyBag;
        private static ClassPropertyBag<Wrapper<T>> s_PropertyBag { get; set; }

        public Wrapper(T value)
        {
            TypeIdProperty = new ClassValueClassProperty<Wrapper<T>, T>(
                $"{typeof(T).Name} Wrapper",
                c => c.Value,
                null
            );

            Value = value;
            s_PropertyBag = new ClassPropertyBag<Wrapper<T>>
            (
                TypeIdProperty
            );
        }
    }


    internal sealed class IMGUIBackend : InspectorBackend
    {
        #region Static
        private TinyContext m_Context;
        private IBindingsManager BindingsManager { get; }
        private ICustomEditorManagerInternal Editors { get; }
        private IGUIManagerInternal GUIManager { get; }
        #endregion

        #region Fields
        private Vector2 m_BodyScroll = Vector2.zero;
        #endregion

        public IMGUIBackend(TinyInspector inspector, TinyContext context)
            : base(inspector)
        {
            m_Context = context;
            BindingsManager = m_Context.GetManager<IBindingsManager>();
            Editors = m_Context.GetManager<ICustomEditorManagerInternal>();
            GUIManager = m_Context.GetManager<IGUIManagerInternal>();
        }

        #region Implementation

        protected override void ValidateTargets()
        {
            Targets = Targets.NotNull().ToList();
        }

        protected override void Inspect()
        {
            Editors.Mode = Mode;
            var firstTarget = Targets[0];
            var v = GUIManager.GetVisitor(firstTarget.GetType(), Mode, ShowFamilies);

            m_BodyScroll = EditorGUILayout.BeginScrollView(m_BodyScroll);
            try
            {
                // TODO: Create automatic dispatching by using delegates created through reflection.
                switch (firstTarget)
                {
                    case TinyEntity entity:
                        v.SetTargets(Targets.OfType<TinyEntity>().Select(Wrapper.Make).ToList());
                        break;
                    case TinyConfigurationViewer config:
                        v.SetTargets(Targets.OfType<TinyConfigurationViewer>().Select(Wrapper.Make).ToList());
                        break;
                    default:
                        v.SetTargets(Targets.Select(Wrapper.Make).ToList());
                        break;
                }

                v.VisitTargets();

                foreach (var entity in Targets.OfType<TinyEntity>())
                {
                    foreach (var component in entity.Components)
                    {
                        var type = component.Type.Dereference(m_Context.Registry);
                        if (type.HasAttribute<ComponentCallbackAttribute>())
                        {
                            var bindings = type.GetAttribute<ComponentCallbackAttribute>().Callback;
                            bindings.Run(ComponentCallbackType.OnValidate, entity, component);
                        }
                    }
                }
                
                if (v.ChangeTracker.HasChanges)
                {
                    v.ChangeTracker.PropagateChanges();
                    v.ChangeTracker.ClearChanges();
                    
                    foreach (var entity in Targets.OfType<TinyEntity>())
                    {
                        BindingsManager.Transfer(entity);
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
                var rect = m_Inspector.Window.position;
                rect.x = rect.y = 0.0f;
                EntityDragAndDropUtility.HandleComponentDragAndDrop(rect, m_Context.Registry, Targets.OfType<TinyEntity>().ToList(), -1);
            }
        }

        protected override void ShowDifferentTypes(Dictionary<Type, int> types)
        {
            EditorGUILayout.LabelField("Narrow the selection:");
            foreach (var kvp in types)
            {
                EditorGUILayout.BeginHorizontal();
                try
                {
                    // I would use EditorGUI.indent, but it is ignored with Buttons..
                    GUILayout.Space(20);
                    if (GUILayout.Button($"{kvp.Value} {kvp.Key.Name}", GUI.skin.label))
                    {
                        RestrictToType(kvp.Key);
                        return;
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        #endregion
    }
}

