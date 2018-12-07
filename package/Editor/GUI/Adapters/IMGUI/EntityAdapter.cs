using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    internal class EntityAdapter : TinyAdapter
        ,ICollectionAdapter<TinyEntity, TinyObject>
        ,IVisitValueAdapter<TinyObject>
        ,IVisitValueAdapter<TinyEntity>
    {
        private IRegistry Registry { get; }
        private TinyModule.Reference MainModule { get; }
        private BindingsManager Bindings { get; }
        private ICustomEditorManagerInternal CustomEditors { get; }

        public EntityAdapter(TinyContext tinyContext)
            :base(tinyContext)
        {
            Registry = TinyContext.Registry;
            Bindings = TinyContext.GetManager<BindingsManager>();
            CustomEditors = TinyContext.GetManager<ICustomEditorManagerInternal>();
        }

        public bool BeginCollection(ref TinyEntity container, ref UIVisitContext<IList<TinyObject>> context)
        {
            return true;
        }

        public void EndCollection(ref TinyEntity container, ref UIVisitContext<IList<TinyObject>> context)
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var content = new GUIContent("Add Tiny Component");

            var rect = GUILayoutUtility.GetRect(content, TinyStyles.AddComponentStyle);
            if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, TinyStyles.AddComponentStyle))
            {
                var targets = context.Targets.Cast<TinyEntity>();
                AddComponentWindow.Show(rect, TinyContext.Registry, targets.ToArray());
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyObject> context) where TContainer : class, IPropertyContainer
        {
            return VisitTinyObject(ref container, ref context);
        }

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyObject> context) where TContainer : struct, IPropertyContainer
        {
            return VisitTinyObject(ref container, ref context);
        }

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyEntity> context)
            where TContainer : class, IPropertyContainer
            => VisitEntity(ref container, ref context);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<TinyEntity> context)
            where TContainer : struct, IPropertyContainer
            => VisitEntity(ref container, ref context);

        #region Implementation

        private static bool VisitEntity<TContainer>(ref TContainer container, ref UIVisitContext<TinyEntity> context) where TContainer : IPropertyContainer
        {
            EntityAdapterUtility.DrawEntityHeader(ref context);
            context.Value.Visit(context.Visitor);
            
            if (context.Value.Instance != null)
            {
                IMGUIPrefabUtility.ShowRemovedPrefabComponents(context.Value);
            }

            return context.Visitor.StopVisit;
        }

        private bool VisitTinyObject<TContainer>(ref TContainer container, ref UIVisitContext<TinyObject> context) where TContainer : IPropertyContainer
        {
            var tiny = context.Value;
            var version = tiny.Version;
            var type = tiny.Type.Dereference(tiny.Registry);

            // Make sure the type  exists.
            if (null == type)
            {
                ShowTypeNotFound(ref context);
                return true;
            }

            type.Refresh();

            // Make sure the type is included in the project.
            var editorContext = TinyEditorApplication.EditorContext;
            var mainModuleRef = editorContext.Project.Module;
            var mainModule = mainModuleRef.Dereference(editorContext.Registry);

            var moduleContainingType = editorContext.Registry.FindAllByType<TinyModule>().First(m => m.Types.Contains(tiny.Type));

            if (!moduleContainingType.Equals(mainModule) && !mainModule.EnumerateDependencies().Contains(moduleContainingType))
            {
                if (type.IsConfiguration)
                {
                    // Silently exit. This is a design choice, we want to preserve any configuration data on the entity but not show or remove the component
                    // This may be changed in the future as the number of configuration components increases.
                    return true;
                }

                ShowTypeMissing(ref context, mainModule, moduleContainingType);
                return true;
            }

            if (type.IsComponent)
            {
                return VisitComponent(ref container, ref context);
            }
            else if (type.IsConfiguration)
            {
                return VisitConfiguration(ref container, ref context);
            }
            else
            {
                return VisitStructOrEnum(ref container, ref context);

            }
        }

        private bool VisitComponent<TContainer>(ref TContainer container, ref UIVisitContext<TinyObject> context)
            where TContainer : IPropertyContainer
        {
            var tiny = context.Value;
            var type = tiny.Type.Dereference(TinyContext.Registry);
            var componentRect = EditorGUILayout.BeginVertical();
            try
            {
                var components = ListPool<TinyObject>.Get();

                try
                {
                    components.AddRange(context.Targets.OfType<TinyEntity>().Select(e => e.GetComponent(type.Ref))
                        .NotNull());
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
                
                var prefabScope = new IMGUIPrefabComponentScope<TinyObject>(ref context);
                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                var showProperties = Foldout(ref context);
                ShowRemoveComponent(tiny, context.Targets.OfType<TinyEntity>().ToList(), (TinyType.Reference) type);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5.0f);

                try
                {
                    if (showProperties)
                    {
                        ++EditorGUI.indentLevel;
                        try
                        {
                            var editor = CustomEditors.GetEditor(tiny.Type);
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
                    TinyGUILayout.Separator(TinyColors.Inspector.Separator, TinyGUIUtility.ComponentSeperatorHeight);
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();

                var target = context.MainTarget<TinyEntity>();
                EntityDragAndDropUtility.HandleComponentDragAndDrop(componentRect, Registry, context.Targets.OfType<TinyEntity>().ToList(), target.GetComponentIndex(type.Ref));
            }

            return context.Visitor.StopVisit;
        }

        private bool VisitConfiguration<TContainer>(ref TContainer container, ref UIVisitContext<TinyObject> context)  where TContainer : IPropertyContainer
        {
            return VisitComponent(ref container, ref context);
        }

        private bool StructField<TContainer>(ref TContainer container, ref UIVisitContext<TinyObject> context) where TContainer : IPropertyContainer
        {
            var tiny = context.Value;
            var drawer = CustomEditors.GetDrawer(tiny.Type);
            return drawer.Visit(ref context);
        }

        private bool VisitStructOrEnum<TContainer>(ref TContainer container, ref UIVisitContext<TinyObject> context) where TContainer : IPropertyContainer
        {
            using (IMGUIScopes.MakePrefabScopes(ref container, ref context))
            {
                return IMGUIVisitorHelper.AsStructItem(ref container, ref context, StructField);
            }
        }

        private bool Foldout(ref UIVisitContext<TinyObject> context, bool isMissing = false, string missingString = "")
        {
            var tiny = context.Value;
            var type = tiny.Type.Dereference(tiny.Registry);

            var foldout = false;
            var showArrow = tiny.Properties.PropertyBag.PropertyCount > 0 && type.Fields.Any(f => !f.Visibility.HasFlag(TinyVisibility.HideInInspector));
            if (showArrow)
            {
                if (!context.Visitor.FolderCache.TryGetValue(tiny, out foldout))
                {
                    foldout = true;
                }
                foldout = EditorGUILayout.Foldout(foldout, "  " + type.Name + (isMissing ? $" ({missingString})" :""), true, TinyStyles.ComponenHeaderFoldout);
                context.Visitor.FolderCache[tiny] = foldout;
            }
            else
            {
                GUILayout.Space(24);
                EditorGUILayout.LabelField(type.Name + (isMissing ? $" ({missingString})" :""), TinyStyles.ComponenHeaderLabel);
            }

            var rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && Event.current.button == 1 && null != tiny)
            {
                ShowContextMenu(tiny, context.Targets.OfType<TinyEntity>().ToList(), tiny.Type);
            }
            return foldout;
        }

        private void ShowContextMenu(TinyObject component, List<TinyEntity> entities, TinyType.Reference typeRef)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Remove"), false, () =>
            {
                var targets = entities;
                EditorApplication.delayCall += () =>
                {
                    foreach (var target in targets)
                    {
                        target.RemoveComponent(typeRef);
                    }

                    // This is called manually because we want the scene graphs to be recreated.
                    TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
                };
            });

            if (entities.Count == 1 && null != entities[0].Instance)
            {
                IMGUIPrefabUtility.AddComponentOverrideMenuItems(menu, entities[0], component.Type);
            }
            
            menu.AddSeparator(string.Empty);

            // TODO: Select type asset.
            //menu.AddItem(new GUIContent("Inspect Initial Values.."), false, () => TinyTypeViewer.SetType(tiny.Type.Dereference(tiny.Registry)));
            menu.AddItem(new GUIContent("Reset Initial Values.."), false, () =>
            {
                foreach(var target in entities)
                {
                    target.GetComponent(typeRef)?.Reset();
                    Bindings.Transfer(target);
                }
            });
            menu.AddSeparator(string.Empty);
            var entity = entities[0] as TinyEntity;
            var index = entity.GetComponentIndex(component.Type);
            if (index > 0)
            {
                menu.AddItem(new GUIContent("Move Up"), false, () => { MoveTypeUp(entities, component.Type); });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Move Up"));
            }

            if (index < entity.Components.Count - 1)
            {
                menu.AddItem(new GUIContent("Move Down"), false, () => { MoveTypeDown(entities, component.Type); });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Move Down"));
            }
            
            menu.AddSeparator(string.Empty);
            
            menu.AddItem(new GUIContent("Select Asset"), false, () =>
            {
                var asset = AssetDatabase.LoadAssetAtPath<UTType>(Persistence.GetAssetPath(typeRef.Dereference(Registry)));
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            });

            menu.ShowAsContext();
        }

        private void ShowRemoveComponent(TinyObject tiny, List<TinyEntity> entities, TinyType.Reference typeRef)
        {
            var oldEnabled = GUI.enabled;
            GUI.enabled = true;
            try
            {
                var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(16.0f));
                if (GUI.Button(rect, EditorGUIUtility.IconContent("_Popup"), TinyStyles.MiddleCenteredLabel))
                {
                    ShowContextMenu(tiny, entities, typeRef);
                }
            }
            finally
            {
                GUI.enabled = oldEnabled;
            }

            GUILayout.Space(5.0f);
        }

        private void MoveTypeUp(List<TinyEntity> entities, TinyType.Reference typeRef)
        {
            foreach (var entity in entities)
            {
                var index = entity.GetComponentIndex(typeRef);
                if (index < 0)
                {
                    continue;
                }

                var component = entity.Components[index];
                entity.Components.RemoveAt(index);
                entity.Components.Insert(Mathf.Max(index - 1, 0), component);
            }
        }

        private void MoveTypeDown(List<TinyEntity> entities, TinyType.Reference typeRef)
        {
            foreach (var entity in entities)
            {
                var index = entity.GetComponentIndex(typeRef);
                if (index < 0)
                {
                    continue;
                }
                var component = entity.Components[index];
                entity.Components.RemoveAt(index);
                entity.Components.Insert(Mathf.Min(index+1, entity.Components.Count), component);
            }
        }

        private void ShowTypeNotFound(ref UIVisitContext<TinyObject> context)
        {
            var tinyObject = context.Value;
            var typeRef = tinyObject.Type;
            using (new EditorGUILayout.HorizontalScope(TinyStyles.TypeNotFoundStyle))
            {
                EditorGUILayout.LabelField($"Type '{typeRef.Name}' is missing.");
                ShowRemoveComponent(tinyObject, context.Targets.OfType<TinyEntity>().ToList(), tinyObject.Type);
            }
            GUILayout.Space(5.0f);
        }

        private void ShowTypeMissing(ref UIVisitContext<TinyObject> context, TinyModule mainModule, TinyModule moduleContainingType)
        {
            var tinyObject = context.Value;
            var type = tinyObject.Type.Dereference(tinyObject.Registry);
            using (new EditorGUILayout.HorizontalScope(TinyStyles.TypeMissingStyle))
            {
                Foldout(ref context, true, "Missing");
                if (type.IsComponent)
                {
                    ShowRemoveComponent(tinyObject, context.Targets.OfType<TinyEntity>().ToList(), tinyObject.Type);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button($"Add '{moduleContainingType.Name}' module"))
                {
                    mainModule.AddExplicitModuleDependency((TinyModule.Reference)moduleContainingType);
                    mainModule.Registry.Context.GetManager<TinyScriptingManager>().Refresh();
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(5.0f);
        }


        #endregion // Implementation
    }
}
