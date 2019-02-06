
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal class ComponentFamilyAdapter : TinyAdapter
        ,IVisitValueAdapter<TinyEntity>
        ,IVisitAdapter<ComponentFamily, TinyObject>
        ,ICollectionAdapter<ComponentFamily, TinyObject>
    {
        private IRegistry Registry { get; }
        private IBindingsManager Bindings { get; }
        private ICustomEditorManagerInternal CustomEditors { get; }

        public ComponentFamilyAdapter(TinyContext tinyContext)
            : base(tinyContext)
        {
            Registry = TinyContext.Registry;
            Bindings = TinyContext.GetManager<IBindingsManager>();
            CustomEditors = TinyContext.GetManager<ICustomEditorManagerInternal>();
        }

        #region IVisitValueAdapter<TinyEntity>

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyEntity> context)
            where TContainer : class, IPropertyContainer
            => VisitEntity(ref container, ref context);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyEntity> context)
            where TContainer : struct, IPropertyContainer
            => VisitEntity(ref container, ref context);

        #endregion // IVisitValueAdapter<TinyEntity>

        private Rect AllFamilyRect = default;
        public bool BeginCollection(ref ComponentFamily family, ref UIVisitContext<IList<TinyObject>> context)
        {
            var cache = context.Visitor.FolderCache;

            if (!cache.TryGetValue(family, out var showProperties))
            {
                showProperties = true;
            }

            var showingComponents = false;
            foreach (var typeRef in family.GetTypes())
            {
                if (!family.SkipComponent(typeRef))
                {
                    showingComponents = true;
                    break;
                }
            }

            AllFamilyRect = EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            try
            {
                if (!showingComponents)
                {
                    EditorGUILayout.LabelField("     " + family.Name, TinyStyles.ComponenHeaderLabel);
                }
                else
                {
                    showProperties = cache[family] = EditorGUILayout.Foldout(showProperties, "  " + family.Name, true,
                        TinyStyles.ComponenHeaderFoldout);
                }

                var rect = GUILayoutUtility.GetLastRect();
                var entities = context.Targets.OfType<TinyEntity>().ToList();
                AddFamilyTypes(entities, family.Definition);
                ShowPopupMenu(entities, family);
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && Event.current.button == 1)
                {
                    CreateAndShowFamilyContextMenu(entities, family);
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            ++EditorGUI.indentLevel;
            return showProperties;
        }

        public void EndCollection(ref ComponentFamily container, ref UIVisitContext<IList<TinyObject>> context)
        {
            --EditorGUI.indentLevel;
            TinyGUILayout.Separator(TinyColors.Inspector.Separator, TinyGUIUtility.ComponentSeperatorHeight);
            EditorGUILayout.EndVertical();
            
            var target = context.MainTarget<TinyEntity>();
            EntityDragAndDropUtility.HandleComponentDragAndDrop(AllFamilyRect, Registry,
                context.Targets.OfType<TinyEntity>().ToList(), target.GetComponentIndex(container.GetRequiredTypes().First()));
        }

        #region Implementation

        private bool VisitEntity<TContainer>(ref TContainer container, ref UIVisitContext<TinyEntity> context) where TContainer : IPropertyContainer
        {
            EntityAdapterUtility.DrawEntityHeader(ref context);

            var entity = context.Value;
            var familyManager = TinyContext.GetManager<FamilyManager>();
            var visited = HashSetPool<TinyType.Reference>.Get();
            var visitor = context.Visitor as IPropertyVisitor;
            var families = ListPool<ComponentFamily>.Get();
            try
            {
                familyManager.GetFamilies(entity, families);

                foreach (var family in families)
                {
                    PropertyContainer.Visit(family, visitor);

                    foreach (var type in family.GetTypes())
                    {
                        visited.Add(type);
                    }
                }

                var itemVisitContext = new VisitContext<TinyObject>
                {
                    Property = entity.Components.Property
                };

                for (var i = 0; i < entity.Components.Count; i++)
                {
                    var component = entity.Components[i];

                    if (visited.Contains(component.Type))
                    {
                        continue;
                    }

                    itemVisitContext.Value = component;
                    itemVisitContext.Index = i;

                    if (visitor.ExcludeOrCustomVisit(entity, itemVisitContext))
                    {
                        continue;
                    }

                    visitor.Visit(entity, itemVisitContext);
                }
            
                if (context.Value.Instance != null)
                {
                    IMGUIPrefabUtility.ShowRemovedPrefabComponents(context.Value);
                }
                
                ShowAddComponentMenu(families, context.Targets.OfType<TinyEntity>().ToArray());
            }
            finally
            {
                ListPool<ComponentFamily>.Release(families);
                HashSetPool<TinyType.Reference>.Release(visited);
            }

            return context.Visitor.StopVisit;
        }

        private void ShowAddComponentMenu(List<ComponentFamily> families, TinyEntity[] targets)
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var content = new GUIContent("Add Tiny Component");

            var rect = GUILayoutUtility.GetRect(content, TinyStyles.AddComponentStyle);
            if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, TinyStyles.AddComponentStyle))
            {
                AddComponentFamilyWindow.Show(rect, TinyContext.Registry, families, targets.ToArray());
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        #endregion // Implementation

        public bool CustomVisit(ref ComponentFamily container, ref UIVisitContext<TinyObject> context)
        {
            var component = context.Value;
            var type = component.Type.Dereference(TinyContext.Registry);
            if (container.SkipComponent(type.Ref))
            {
                return context.Visitor.StopVisit;
            }

            var cache = context.Visitor.FolderCache;

            var components = ListPool<TinyObject>.Get();
            try
            {
                components.AddRange(
                    context.Targets.OfType<TinyEntity>().Select(e => e.GetComponent(type.Ref)).NotNull());
                if (components.Count != context.Targets.Count)
                {
                    // Not shared by all components, skip this component.
                    return context.Visitor.StopVisit;
                }

                context.Visitor.ChangeTracker.PushResolvers(components.Cast<IPropertyContainer>().ToList());
            }
            finally
            {
                ListPool<TinyObject>.Release(components);
            }

            if (!cache.TryGetValue(component, out var showProperties))
            {
                showProperties = true;
            }

            var prefabScope = new IMGUIPrefabComponentScope<TinyObject>(ref context);
    
            var rect = EditorGUILayout.BeginVertical();
            GUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            try
            {
                GUI.color = container.HeaderColor;
                GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
                GUI.color = Color.white;
                var entities = context.Targets.OfType<TinyEntity>().ToList();
                if (context.Visitor.StopVisit == container.Visit(ref context))
                {
                    showProperties = false;
                }
                else
                {
                    if (type.Fields.Count > 0)
                    {
                        showProperties = cache[component] = EditorGUILayout.Foldout(showProperties, component.Name);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(component.Name);
                    }

                    var menuPopupRect = GUILayoutUtility.GetLastRect();
                    if (Event.current.type == EventType.MouseDown &&
                        menuPopupRect.Contains(Event.current.mousePosition) &&
                        Event.current.button == 1)
                    {
                        CreateAndShowComponentContext(entities, component.Type);
                    }
                }

                ShowPopupMenu(entities, component.Type);
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
                EditorGUILayout.EndVertical();
            }

            try
            {
                if (showProperties)
                {
                    ++EditorGUI.indentLevel;
                    try
                    {
                        var editor = CustomEditors.GetEditor(component.Type);
                        return editor.Visit(ref context);
                    }
                    finally
                    {
                        GUILayout.Space(5);
                        --EditorGUI.indentLevel;
                    }
                }
            }
            finally
            {
                prefabScope.Dispose();
                context.Visitor.ChangeTracker.PopResolvers();
            }

            return context.Visitor.StopVisit;
        }

        private void AddFamilyTypes(List<TinyEntity> entities, FamilyDefinition family)
        {
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(16.0f));

            if (GUI.Button(rect, TinyIcons.Component, TinyStyles.MiddleCenteredLabel))
            {
                var sections = new List<SelectorSection>();
                var section = new SelectorSection {Header = "Components in family"};
                foreach (var required in family.Required)
                {
                    section.Options.Add(new SelectorOption(required.Name, () => true, b => { }, false));
                }

                foreach (var optional in family.Optional)
                {
                    section.Options.Add(new SelectorOption(
                            optional.Name,
                            () => entities.TrueForAll(e => null != e.GetComponent(optional)),
                            selected =>
                            {
                                var targets = entities;
                                foreach (var entity in targets)
                                {
                                    if (selected)
                                    {
                                        entity.GetOrAddComponent(optional);
                                    }
                                    else
                                    {
                                        entity.RemoveComponent(optional);
                                    }
                                }

                                // This is called manually because we want the scene graphs to be recreated.
                                TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                                GUIUtility.ExitGUI();
                            },
                            true
                        )
                    );
                }

                sections.Add(section);
                var window = new TinyMultiSelectorMenu(sections);
                PopupWindowBridge.Show(rect, window);
            }
        }

        private void ShowPopupMenu(List<TinyEntity> entities, ComponentFamily family)
        {
            var oldEnabled = GUI.enabled;
            GUI.enabled = true;
            try
            {
                var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(16.0f));
                if (GUI.Button(rect, EditorGUIUtility.IconContent("_Popup"), TinyStyles.MiddleCenteredLabel))
                {
                    CreateAndShowFamilyContextMenu(entities, family);
                }
            }
            finally
            {
                GUI.enabled = oldEnabled;
            }

            GUILayout.Space(5.0f);
        }

        private void CreateAndShowFamilyContextMenu(List<TinyEntity> entities, ComponentFamily family)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Remove Family"), false, () =>
            {
                var targets = entities;
                EditorApplication.delayCall += () =>
                {
                    foreach (var entity in targets)
                    {
                        foreach (var component in family.GetTypes())
                        {
                            entity.RemoveComponent(component);
                        }
                    }

                    // This is called manually because we want the scene graphs to be recreated.
                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                };
            });
                    
            menu.AddItem(new GUIContent("Reset Family Initial Values.."), false, () =>
            {
                family.ResetValues(entities);
            });
            menu.ShowAsContext();
        }

        private void ShowPopupMenu(List<TinyEntity> entities, TinyType.Reference typeRef)
        {
            var oldEnabled = GUI.enabled;
            GUI.enabled = true;
            try
            {
                var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(16.0f));
                if (GUI.Button(rect, EditorGUIUtility.IconContent("_Popup"), TinyStyles.MiddleCenteredLabel))
                {
                    CreateAndShowComponentContext(entities, typeRef);
                }
            }
            finally
            {
                GUI.enabled = oldEnabled;
            }

            GUILayout.Space(5.0f);
        }

        private void CreateAndShowComponentContext(List<TinyEntity> entities, TinyType.Reference typeRef)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Remove"), false, () =>
            {
                var targets = entities;
                EditorApplication.delayCall += () =>
                {
                    foreach (var entity in targets)
                    {
                        entity.RemoveComponent(typeRef);
                    }

                    // This is called manually because we want the scene graphs to be recreated.
                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                };
            });
            
            if (entities.Count == 1 && null != entities[0].Instance)
            {
                IMGUIPrefabUtility.AddComponentOverrideMenuItems(menu, entities[0], typeRef);
            }
                    
            menu.AddItem(new GUIContent("Reset Initial Values.."), false, () =>
            {
                foreach(var target in entities)
                {
                    target.GetComponent(typeRef)?.Reset();
                    Bindings.Transfer(target);
                }
            });
            
            menu.AddSeparator(string.Empty);
            
            menu.AddItem(new GUIContent("Select Asset"), false, () =>
            {
                var asset = AssetDatabase.LoadAssetAtPath<UTType>(Persistence.GetAssetPath(typeRef.Dereference(Registry)));
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            });
            
            menu.ShowAsContext();
        }
    }
}
