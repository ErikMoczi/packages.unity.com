using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class IMGUIPrefabUtility
    {
        /// <summary>
        /// Returns true if the currently visited (property, index) is contained in the given (path, root) 
        /// </summary>
        /// <param name="path">Expanded property path</param>
        /// <param name="root">Start point for the search (typically the component)</param>
        /// <param name="targetProperty"></param>
        /// <param name="targetListIndex"></param>
        /// <returns>True if the currently visited (container, property, index) is contained in the given (path, root)(</returns>
        internal static bool IsModified(PropertyPath path, IPropertyContainer root, IProperty targetProperty, int targetListIndex = -1)
        {
            var currentContainer = root;
            
            for (var i = 0; i < path.PartsCount; i++)
            {
                var part = path[i];
                
                var currentProperty = currentContainer?.PropertyBag.FindProperty(part.propertyName);
                
                if (currentProperty == null)
                {
                    break;
                }

                if (part.listIndex >= 0)
                {
                    if (!(currentProperty is IListClassProperty listProperty) || listProperty.Count(currentContainer) <= part.listIndex)
                    {
                        break;
                    }
                    
                    if (ReferenceEquals(currentProperty, targetProperty) && (targetListIndex == -1 || part.listIndex == targetListIndex))
                    {
                        return true;
                    }
                    
                    currentContainer = listProperty.GetObjectAt(currentContainer, part.listIndex) as IPropertyContainer;
                }
                else
                {
                    if (ReferenceEquals(currentProperty, targetProperty))
                    {
                        return true;
                    }

                    currentContainer = (currentProperty as IValueProperty)?.GetObjectValue(currentContainer) as IPropertyContainer;
                }
            }

            return false;
        }

        private struct MenuItemContext
        {
            public TinyEntity Entity { get; set; }
            public TinyType.Reference Component { get; set; }
        }
        
        internal static void ShowRemovedPrefabComponents(TinyEntity entity)
        {
            var instance = entity.Instance;
            
            foreach (var removedComponent in instance.RemovedComponents)
            {
                var showContext = false;

                var rect = EditorGUILayout.BeginHorizontal();
                using (new IMGUIPrefabOverrideScope(rect))
                using (new TinyGUIColorScope(Color.gray))
                {
                    GUILayout.Space(24);
                    EditorGUILayout.LabelField(removedComponent.Name + " (Removed)", TinyStyles.ComponenHeaderLabel);

                    var evt = Event.current;
                    if (rect.Contains(evt.mousePosition))
                    {
                        if (evt.type == EventType.MouseDown && evt.button == 1 || evt.type == EventType.ContextClick)
                        {
                            showContext = true;
                        }
                    }
                }

                if (GUI.Button(EditorGUILayout.GetControlRect(false, GUILayout.Width(16.0f)), EditorGUIUtility.IconContent("_Popup"), TinyStyles.MiddleCenteredLabel))
                {
                    showContext = true;
                }

                if (showContext)
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Removed Component/Apply to Prefab"), false, o =>
                    {
                        var ctx = (MenuItemContext) o;
                        var prefab = ctx.Entity.Instance.Source.Dereference(ctx.Entity.Registry);
                        prefab.RemoveComponent(ctx.Component);
                        ctx.Entity.Instance.RemovedComponents.Remove(ctx.Component);
                    }, new MenuItemContext
                    {
                        Entity = entity,
                        Component = removedComponent
                    });
                    
                    menu.AddItem(new GUIContent("Removed Component/Revert"), false, o =>
                    {
                        var ctx = (MenuItemContext) o;
                        ctx.Entity.AddComponent(ctx.Component);
                    }, new MenuItemContext
                    {
                        Entity = entity,
                        Component = removedComponent
                    });
                    menu.ShowAsContext();
                }

                EditorGUILayout.EndHorizontal();
                TinyGUILayout.Separator(TinyColors.Inspector.Separator, TinyGUIUtility.ComponentSeperatorHeight);
            }
        }

        internal static void AddComponentOverrideMenuItems(GenericMenu menu, TinyEntity entity, TinyType.Reference component)
        {
            if (null == entity.Instance)
            {
                return;
            }

            var prefab = entity.Instance.Source.Dereference(entity.Registry);

            if (prefab.HasComponent(component))
            {
                return;
            }
            
            menu.AddItem(new GUIContent("Added Component/Apply to Prefab"), false, () =>
            {
                prefab.AddComponent(component);
            });
                    
            menu.AddItem(new GUIContent("Added Component/Revert"), false, () =>
            {
                entity.RemoveComponent(component);
            });
        }

        internal static void ShowEntityPrefabHeader(IEnumerable<TinyEntity> entities)
        {
            if (!entities.All(PrefabTransformUtility.IsPrefabInstanceRootTransform))
            {
                return;
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                var multiple = entities.Count() > 1;
                
                EditorGUILayout.LabelField(multiple ? "Multiple" : "Prefab", GUILayout.Width(50));

                // Disable multi apply for now...
                GUI.enabled = !multiple;
                
                if (GUILayout.Button("Apply", GUILayout.Height(16f)))
                {
                    foreach (var entity in entities)
                    {
                        var manager = entity.Registry.Context.GetManager<IPrefabManager>();
                        var prefabInstance = entity.Instance.PrefabInstance.Dereference(entity.Registry);
                        
                        manager.ApplyInstanceToPrefab(entity.Instance.PrefabInstance.Dereference(entity.Registry));
                        
                        TinyEditorApplication.SaveObject(prefabInstance.PrefabEntityGroup.Dereference(entity.Registry), Persistence.PrefabFileExtension);
                    }
                            
                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                }

                GUI.enabled = true;

                if (GUILayout.Button("Revert", GUILayout.Height(16f)))
                {
                    foreach (var entity in entities)
                    {
                        var manager = entity.Registry.Context.GetManager<IPrefabManager>();
                        manager.RevertInstanceToPrefab(entity.Instance.PrefabInstance.Dereference(entity.Registry));
                    }
                            
                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                }
                
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }
        }
    }
}