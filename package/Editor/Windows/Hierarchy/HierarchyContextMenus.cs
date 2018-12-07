using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Unity.Tiny
{
    internal static class HierarchyContextMenus
    {
        public static void ShowEntityGroupContextMenu(this HierarchyTree tree, TinyEntityGroup.Reference entityGroupRef)
        {
            if (TinyEntityGroup.Reference.None.Id == entityGroupRef.Id)
            {
                entityGroupRef = tree.EntityGroupManager.ActiveEntityGroup;
            }

            var menu = new GenericMenu();
            if (tree.IsEntityGroupActive(entityGroupRef))
            {
                menu.AddDisabledItem(new GUIContent("Set Active EntityGroup"));
            }
            else
            {
                menu.AddItem(new GUIContent("Set Active EntityGroup"), false, () =>
                {
                    tree.SetEntityGroupActive(entityGroupRef);
                });
            }

            if (tree.EntityGroupManager.LoadedEntityGroups.IndexOf(entityGroupRef) > 0)
            {
                menu.AddItem(new GUIContent("Move EntityGroup Up"), false, () =>
                {
                    tree.EntityGroupManager.MoveUp(entityGroupRef);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Move EntityGroup Up"));
            }

            if (tree.EntityGroupManager.LoadedEntityGroups.IndexOf(entityGroupRef) < tree.EntityGroupManager.LoadedEntityGroupCount - 1)
            {
                menu.AddItem(new GUIContent("Move EntityGroup Down"), false, () =>
                {
                    tree.EntityGroupManager.MoveDown(entityGroupRef);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Move EntityGroup Down"));
            }

            menu.AddSeparator("");

            if (tree.EntityGroupManager.LoadedEntityGroupCount == 1)
            {
                menu.AddDisabledItem(new GUIContent("Unload EntityGroup"));
                menu.AddDisabledItem(new GUIContent("Unload Other EntityGroups"));
            }
            else
            {
                menu.AddItem(new GUIContent("Unload EntityGroup"), false, () =>
                {
                    tree.EntityGroupManager.UnloadEntityGroup(entityGroupRef);
                });
                menu.AddItem(new GUIContent("Unload Other EntityGroups"), false, () =>
                {
                    tree.EntityGroupManager.UnloadAllEntityGroupsExcept(entityGroupRef);
                });
            }

            menu.AddItem(new GUIContent("New EntityGroup"), false, () =>
            {
                var context = tree.Context;
                if (null == context)
                {
                    return;
                }
                var registry = context.Registry;
                var project = registry.FindAllByType<TinyProject>().First();
                tree.CreateNewEntityGroup(project.Module.Dereference(registry));
            });

            menu.AddSeparator("");

            PopulateEntityTemplate(menu, tree.GetRegistryObjectSelection());

            menu.ShowAsContext();
        }

        public static void ShowEntityContextMenu(this HierarchyTree tree, ISceneGraphNode node, TinyEntity.Reference entityRef)
        {
            if (TinyEntity.Reference.None.Id == entityRef.Id)
            {
                return;
            }

            var menu = new GenericMenu();


            menu.AddItem(new GUIContent("Rename"), false, () =>
            {
                tree.Rename(entityRef);
            });

            menu.AddItem(new GUIContent("Duplicate"), false, tree.DuplicateSelection);

            menu.AddItem(new GUIContent("Delete"), false, tree.DeleteSelection);

            menu.AddSeparator("");

            PopulatePrefab(tree, node, menu);

            PopulateEntityTemplate(menu, tree.GetRegistryObjectSelection());

            menu.ShowAsContext();
        }

        public static void PopulatePrefab(HierarchyTree tree, ISceneGraphNode node, GenericMenu menu)
        {
            var hasAnyPrefabOptions = false;
            
            var entities = node.GetDescendants()
                .OfType<EntityNode>()
                .Select(n => n.EntityRef.Dereference(tree.Registry))
                .ToList();

            var selectedEntity = entities.First();
            
            if (!selectedEntity.HasEntityInstanceComponent(false))
            {
                hasAnyPrefabOptions = true;
                
                menu.AddItem(new GUIContent("Make Prefab"), false, () =>
                {
                    
                    TinyAction.CreatePrefab(tree.Context.Registry, tree.Registry.AnyByType<TinyProject>().Module, selectedEntity.Name, group =>
                    {
                        // Don't let this operation be undoable
                        // Since we are creating an asset, undoing this would leave a floating asset.
                        // Instead prefer that the user deletes the asset and manually break instances
                        // using (tree.Registry.DontTrackChanges())
                        {
                            var prefabManager = tree.Context.GetManager<PrefabManager>();
                            prefabManager.CreatePrefabAndConvertToInstance(group, entities);
    
                            foreach (var entity in group.Entities)
                            {
                                tree.Context.Registry.ChangeSource(entity.Id, group.PersistenceId); 
                            }
                        }
                    });
                });
            }

            if (PrefabTransformUtility.IsPrefabInstanceRootTransform(selectedEntity))
            {
                hasAnyPrefabOptions = true;
                
                menu.AddItem(new GUIContent("Unpack Prefab"), false, () =>
                {
                    var instance = entities.Select(e => e.Instance.PrefabInstance).First();

                    foreach (var entity in instance.Dereference(tree.Registry).Entities.Deref(tree.Registry))
                    {
                        entity.Instance = null;

                        foreach (var component in entity.Components)
                        {
                            PrefabAttributeUtility.RemovePrefabComponentAttributes(component);
                        }
                    }
                    
                    tree.Registry.Unregister(instance.Id);

                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                });
            }
            else if (selectedEntity.HasEntityInstanceComponent())
            {
                hasAnyPrefabOptions = true;

                menu.AddItem(new GUIContent("Prefab Entity/Remove from Prefab"), false, () =>
                {
                    // Treat this as a standard delete
                    node.Graph.Delete(node);
                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.SceneGraph);
                    
                    var prefabManager = tree.Context.GetManager<PrefabManager>();
                    var instance = selectedEntity.Instance.PrefabInstance.Dereference(tree.Context.Registry);
                    prefabManager.ApplyRemovedEntityToPrefab(instance, selectedEntity);
                });
            }

            if (selectedEntity.IsAddedEntityOverride())
            {
                menu.AddItem(new GUIContent("Added Entity/Apply to Prefab"), false, () =>
                {
                    var instance = node.GetAncestors().OfType<EntityNode>().Select(e => e.EntityRef.Dereference(tree.Registry)).First(e => e.HasEntityInstanceComponent());
                    var prefabManager = tree.Context.GetManager<PrefabManager>();
                    prefabManager.ApplyAddedEntityToPrefab(instance.Instance.PrefabInstance.Dereference(tree.Context.Registry), selectedEntity);
                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                });
            }

            if (hasAnyPrefabOptions)
            {
                menu.AddSeparator("");
            }
        }
        
        public static void PopulateEntityTemplate(GenericMenu menu, params IRegistryObject[] contextObjects )
        {
            foreach (var methodAttribute in TinyAttributeScanner.GetMethodAttributes<TinyEntityTemplateMenuItemAttribute>())
            {
                menu.AddItem(new GUIContent(methodAttribute.Attribute.ItemNamePrefix.Replace(EntityTemplateMenuItems.k_Prefix, "") + methodAttribute.Method.Name.Replace("_", " ")), false, () =>
                {
                    methodAttribute.Method.Invoke(null, new object[]{ contextObjects });
                });
            }
        }

        #region Implementation
        private static bool IsEntityGroupActive(this HierarchyTree tree, TinyEntityGroup.Reference entityGroupRef)
        {
            return tree.EntityGroupManager.ActiveEntityGroup.Equals(entityGroupRef);
        }

        private static void SetEntityGroupActive(this HierarchyTree tree, TinyEntityGroup.Reference entityGroupRef)
        {
            tree.EntityGroupManager.SetActiveEntityGroup(entityGroupRef, true);
        }

        private static void CreateNewEntityGroup(this HierarchyTree tree, TinyModule module)
        {
            tree.EntityGroupManager.CreateNewEntityGroup();
        }
        #endregion
    }
}

