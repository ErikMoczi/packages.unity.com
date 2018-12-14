using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [Flags]
    public enum PrefabScopeOptions
    {
        None = 0,
        DrawOverride = 1,
        ContextMenu = 2,
            
        Default = DrawOverride | ContextMenu
    }

    internal struct IMGUIPrefabEntityScope : IDisposable
    {
        private readonly bool m_Overridden;
        private IMGUIPrefabOverrideScope m_Override;
        
        public IMGUIPrefabEntityScope(List<TinyEntity> entities) 
        {
            m_Overridden = false;

            var rect = EditorGUILayout.BeginVertical(GUIStyle.none);

            if (entities.Count > 1)
            {
                // No support for multi edit on prefab interaction YET...
                return;
            }

            var entity = entities.FirstOrDefault();

            if (entity?.Instance == null)
            {
                return;
            }

            var instance = entity.Instance;

            if (instance.EntityModificationFlags == EntityModificationFlags.None)
            {
                return;
            }
            
            var evt = Event.current;
            
            if (rect.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.MouseDown && evt.button == 1 || evt.type == EventType.ContextClick)
                {
                    ShowContextMenu(entity);
                }
            }

            m_Overridden = true;
            m_Override = new IMGUIPrefabOverrideScope(rect);
        }

        private static void ShowContextMenu(TinyEntity entity)
        {
            var menu = new GenericMenu();

            foreach (var flag in entity.Instance.EntityModificationFlags.EnumerateFlags())
            {
                menu.AddItem(new GUIContent($"{flag.ToString()}/Apply to Prefab"), false, o =>
                {
                    var e = (TinyEntity) o;
                    var manager = e.Registry.Context.GetManager<IPrefabManager>();
                    manager.ApplyEntityModificationsToPrefab(flag, e);
                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                }, entity);
                
                menu.AddItem(new GUIContent($"{flag.ToString()}/Revert"), false, o =>
                {
                    var e = (TinyEntity) o;
                    var manager = e.Registry.Context.GetManager<IPrefabManager>();
                    manager.RevertEntityModificationsForInstance(flag, e);
                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                }, entity);
            }  
  
            menu.ShowAsContext();
        }

        public void Dispose()
        {
            if (m_Overridden)
            {
                m_Override.Dispose();
            }
            
            EditorGUILayout.EndVertical();
        }
    }

    internal struct IMGUIPrefabComponentScope<TValue> : IDisposable
    {
        private IMGUIPrefabOverrideScope m_Override;
        private readonly bool m_Overridden;
        
        public IMGUIPrefabComponentScope(ref UIVisitContext<TValue> context)
        {
            var rect = EditorGUILayout.BeginVertical(GUIStyle.none);
            
            m_Overridden = false;
            
            if (context.Targets.Count > 1)
            {
                // No support for multi edit on prefab interaction YET...
                return;
            }
            
            var entity = context.MainTarget<TinyEntity>();

            if (null == entity || !entity.HasEntityInstanceComponent())
            {
                // Not a prefab instance OR the prefab instance is missing in some way
                return;
            }
            
            // Query the resolvers for the current component we are editing
            var resolvers = context.Visitor.ChangeTracker.Resolvers;
            var component = resolvers.Count > 1 ? resolvers[1][0] as TinyObject : null;

            if (null == component)
            {
                return;
            }
            
            var instance = entity.Instance;
            
            if (instance.Source.Dereference(entity.Registry).HasComponent(component.Type))
            {
                return;
            }
            
            m_Overridden = true;
            m_Override = new IMGUIPrefabOverrideScope(rect);
        }

        public void Dispose()
        {
            if (m_Overridden)
            {
                m_Override.Dispose();
            }
            
            EditorGUILayout.EndVertical();
        }
    }

    internal struct IMGUIPrefabValueScope<TContainer, TValue> : IDisposable
        where TContainer : IPropertyContainer
    {
        private readonly PrefabScopeOptions m_Options;
        private readonly bool m_Overridden;
        
        private IMGUIPrefabOverrideScope m_Override;
        private IMGUIPrefabContextMenuScope<TValue> m_ContextMenu;

        public IMGUIPrefabValueScope(ref TContainer container, ref UIVisitContext<TValue> context, int minHeight, PrefabScopeOptions options = PrefabScopeOptions.Default)
        {
            var rect = EditorGUILayout.BeginHorizontal(GUIStyle.none);
            rect.height = Mathf.Max(rect.height, minHeight);
            
            m_Overridden = false;
            m_Options = options;
            
            if (context.Targets.Count > 1)
            {
                // No support for multi edit on prefab interaction YET...
                return;
            }

            var entity = context.MainTarget<TinyEntity>();

            if (null == entity || !entity.HasEntityInstanceComponent())
            {
                // Not a prefab instance
                return;
            }
            
            // Query the resolvers for the current component we are editing
            var resolvers = context.Visitor.ChangeTracker.Resolvers;
            var component = resolvers.Count > 1 ? resolvers[1][0] as TinyObject : null;

            if (null == component)
            {
                return;
            }

            var instance = entity.Instance;
            if (!instance.Source.Dereference(entity.Registry).HasComponent(component.Type))
            {
                // This is not a prefab component...
                // Don't flag any properties as modified, this is handled by the ComponentPrefabScope
            }
            else
            {
                // @TODO [PREFAB] Optimization
                // We need a way faster way to detect changes
                foreach (var modification in instance.Modifications)
                {
                    if (!Equals(modification.Target, component.Type))
                    {
                        // Filter by the component we are currently visiting
                        continue;
                    }
                
                    // Does this property path match the currently visited container/property/index?
                    if (!PrefabManager.IsModified(modification.GetFullPath(), component, context.Property, context.Index))
                    {
                        continue;
                    }
                
                    m_Overridden = true;
                    break;
                }
            }

            if (!m_Overridden)
            {
                return;
            }
            
            if (m_Options.HasFlag(PrefabScopeOptions.DrawOverride))
            {
                m_Override = new IMGUIPrefabOverrideScope(rect);
            }

            if (m_Options.HasFlag(PrefabScopeOptions.ContextMenu))
            {
                m_ContextMenu = new IMGUIPrefabContextMenuScope<TValue>(ref context, rect, entity);
            }
        }
        
        public void Dispose()
        {
            if (m_Overridden)
            {
                if (m_Options.HasFlag(PrefabScopeOptions.DrawOverride))
                {
                    m_Override.Dispose();
                }
                
                if (m_Options.HasFlag(PrefabScopeOptions.ContextMenu))
                {
                    m_ContextMenu.Dispose();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}