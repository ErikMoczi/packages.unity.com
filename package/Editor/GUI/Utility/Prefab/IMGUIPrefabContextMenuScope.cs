﻿using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal struct IMGUIPrefabContextMenuScope<TValue> : IDisposable
    {
        private struct MenuItemContext
        {
            public IPrefabManager PrefabManager;
            public TinyEntity Entity;
            public IList<IPropertyModification> Modifications;
        }
        
        public IMGUIPrefabContextMenuScope(ref UIVisitContext<TValue> context, Rect rect, TinyEntity entity)
        {
            var evt = Event.current;
            
            if (rect.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.MouseDown && evt.button == 1 || evt.type == EventType.ContextClick)
                {
                    var menu = new GenericMenu();

                    var resolvers = context.Visitor.ChangeTracker.Resolvers;
                    var component = resolvers.Count > 1 ? resolvers[1][0] as TinyObject : null;

                    var modifications = new List<IPropertyModification>();
                    
                    foreach (var modification in entity.Instance.Modifications)
                    {
                        if (!Equals(modification.Target, component.Type))
                        {
                            // Filter by the component we are currently visiting
                            continue;
                        }
                
                        if (!PrefabManager.IsModified(modification.GetFullPath(), component, context.Property, context.Index))
                        {
                            continue;
                        }

                        modifications.Add(modification);
                    }

                    var prefabManager = entity.Registry.Context.GetManager<IPrefabManager>();
                    
                    menu.AddItem(new GUIContent("Apply to Prefab"), false, o =>
                    {
                        var menuItemContext = (MenuItemContext) o;
                        menuItemContext.PrefabManager.ApplyComponentModificationsToPrefab(menuItemContext.Modifications, menuItemContext.Entity);
                        TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                    }, new MenuItemContext
                    {
                        PrefabManager = prefabManager,
                        Entity = entity,
                        Modifications = modifications
                    });
                    
                    menu.AddItem(new GUIContent("Revert"), false, o =>
                    {
                        var menuItemContext = (MenuItemContext) o;
                        menuItemContext.PrefabManager.RevertComponentModificationsForInstance(menuItemContext.Modifications, menuItemContext.Entity);
                        TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                    }, new MenuItemContext
                    {
                        PrefabManager = prefabManager,
                        Entity = entity,
                        Modifications = modifications
                    });

                    menu.ShowAsContext();
                }
            }
        }

        public void Dispose()
        {
            
        }
    }
}