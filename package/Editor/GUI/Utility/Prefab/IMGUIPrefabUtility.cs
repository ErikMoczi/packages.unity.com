using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal static class IMGUIPrefabUtility
    {
        

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

        internal static void ShowEntityPrefabHeader(IRegistry registry, IEnumerable<TinyEntity> entities)
        {
            if (!entities.All(PrefabTransformUtility.IsPrefabInstanceRootTransform))
            {
                return;
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                var prefabManager = registry.Context.GetManager<IPrefabManager>();

                var multiple = entities.Count() > 1;
                
                EditorGUILayout.LabelField(multiple ? "Multiple" : "Prefab", GUILayout.Width(50));

                // Disable multi apply for now...
                GUI.enabled = !multiple;
                
                if (GUILayout.Button("Apply", GUILayout.Height(16f)))
                {
                    foreach (var entity in entities)
                    {
                        var prefabInstance = entity.Instance.PrefabInstance.Dereference(entity.Registry);
                        prefabManager.ApplyInstanceToPrefab(entity.Instance.PrefabInstance.Dereference(entity.Registry));
                        var group = prefabInstance.PrefabEntityGroup.Dereference(entity.Registry);
                        Persistence.SaveObjectsAs(entity.Registry, group.AsEnumerable(), Persistence.GetAssetPath(group));
                    }
                            
                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                }

                GUI.enabled = true;

                if (GUILayout.Button("Revert", GUILayout.Height(16f)))
                {
                    foreach (var entity in entities)
                    {
                        prefabManager.RevertInstanceToPrefab(entity.Instance.PrefabInstance.Dereference(entity.Registry));
                    }
                            
                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                }
                
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }
        }
    }
}