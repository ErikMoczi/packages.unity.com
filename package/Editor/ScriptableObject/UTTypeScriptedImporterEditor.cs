using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [CustomEditor(typeof(UTTypeScriptedImporter))]
    internal class UTTypeScriptedImporterEditor : TinyScriptedImporterEditorBase<UTTypeScriptedImporter, TinyType>
    {
        private readonly Dictionary<TinyField, RenamableLabel> Labels = new Dictionary<TinyField, RenamableLabel>();
        public override bool UseDefaultMargins() => true;
        private const float k_Spacing = 4.0f;
        private const float k_DoubleSpacing = k_Spacing * 2.0f;
        private const string k_DocumentationPrefix = "Tiny_ShowDocumentation_";

        private static bool GetFolded(IIdentified<TinyId> field)
        {
            return EditorPrefs.GetBool(field.Id.ToString(), true);
        }

        private static bool SetFolded(IIdentified<TinyId> field, bool show)
        {
            if (show)
            {
                EditorPrefs.DeleteKey(field.Id.ToString());
            }
            else
            {
                EditorPrefs.SetBool(field.Id.ToString(), false);
            }

            return show;
        }

        protected override void RefreshObject(ref TinyType type)
        {
            if (null == type)
            {
                return;
            }

            var typeRef = (TinyType.Reference) type;
            type = typeRef.Dereference(type.Registry);
        }

        protected override void Reload()
        {
            var context = TinyEditorApplication.EditorContext.Context;
            m_DefaultValueVisitor = new GUIVisitor(
                new EntityAdapter(context),
                new TinyIMGUIAdapter(context),
                new TinyVisibilityAdapter(context),
                new IMGUIUnityTypesAdapter(),
                new IMGUIPrimitivesAdapter(),
                new IMGUIAdapter());
            m_DefaultValueVisitor.NameResolver = new DefaultValueNameResolver();
        }

        private GUIVisitor m_DefaultValueVisitor;

        private float IconSpace(TinyType type)
        {
            return type.IsEnum ? 20.0f : 60.0f;
        }

        protected override void OnInspect(TinyType type)
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                GUILayout.Space(k_Spacing);
                DrawDocumentation(type);
                DrawUnlistedField(type);
                GUILayout.Space(k_Spacing);
                DrawSeparator();
                GUILayout.Space(k_Spacing);

                var defaultValue = type.DefaultValue as TinyObject;
                m_DefaultValueVisitor.SetTargets(new List<IPropertyContainer> { defaultValue });

                var mainModule = TinyEditorApplication.Module;
                var dependencies = new HashSet<TinyModule>(mainModule.EnumerateDependencies());

                foreach (var field in type.Fields)
                {
                    var showProperties = DrawField(type, field);

                    try
                    {
                        if (!showProperties)
                        {
                            continue;
                        }

                        GUILayout.Space(k_Spacing);
                        ++EditorGUI.indentLevel;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            using (new EditorGUILayout.VerticalScope())
                            {
                                if (EditorPrefs.GetBool(k_DocumentationPrefix + field.Id, false))
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        DrawDocumentation(field);
                                    }
                                }

                                var module = Registry.CacheManager.GetModuleOf(field.FieldType);
                                if (TinyType.BuiltInTypes.Contains(field.FieldType.Dereference(Registry)) || dependencies.Contains(module))
                                {
                                    VisitFieldDefaultValue(defaultValue, field.Name,
                                        field.FieldType.Dereference(defaultValue.Registry), m_DefaultValueVisitor,
                                        field.Array);
                                }
                                else
                                {
                                    ShowModuleMissing(mainModule, module, field);
                                }
                            }

                            GUILayout.Space(IconSpace(type));
                        }

                        --EditorGUI.indentLevel;
                    }
                    finally
                    {
                        GUILayout.Space(k_DoubleSpacing);
                    }
                }

                DrawAddNewField(type);
            }
        }

        protected override bool IsPartOfModule(TinyModule module, TinyId mainAssetId)
        {
            return module.Types.Any(t => t.Id == mainAssetId);
        }

        #region Implementation

        private static void DrawUnlistedField(TinyType type)
        {
            if (type.IsComponent)
            {
                type.Unlisted = EditorGUILayout.Toggle("Unlisted", type.Unlisted);
                type.Visibility = (TinyVisibility) EditorGUILayout.EnumPopup("Hide Flags", type.Visibility);
            }
        }

        private void DrawIcons(Rect rect, TinyType type, TinyField field)
        {
            const float iconSpacing = 3.0f;
            var iconRect = rect;
            var content = GUIContent.none;
            
            if (!type.IsEnum)
            {
                iconRect.x += iconSpacing;
                iconRect.y = iconRect.center.y - 6.0f;
                iconRect.width = TinyStyles.ArrayStyle.fixedWidth;
                iconRect.height = TinyStyles.ArrayStyle.fixedHeight;
                content.tooltip = field.Array ? "Change array to value" : "Change value to array";
                if (GUI.Button(iconRect, content, (field.Array ? TinyStyles.ArrayStyle : TinyStyles.NonArrayStyle)))
                {
                    field.Array = !field.Array;
                    type.Refresh();
                    GUIUtility.ExitGUI();
                }
            }

            if (!type.IsEnum)
            {
                var visible = field.Visibility.HasFlag(TinyVisibility.HideInInspector);
                
                iconRect.x += iconRect.width + iconSpacing;
                iconRect.width = TinyStyles.NonVisibleStyle.fixedWidth;
                iconRect.height = TinyStyles.NonVisibleStyle.fixedHeight;
                content.tooltip = visible ? "Hide field in inspector" : "Show field in inspector";
                if (GUI.Button(iconRect, content, visible ? TinyStyles.NonVisibleStyle : TinyStyles.VisibleStyle))
                {
                    field.Visibility ^= TinyVisibility.HideInInspector;
                    type.Refresh();
                    GUIUtility.ExitGUI();
                }
            }

            if (!type.IsEnum)
            {
                iconRect.x += iconRect.width + iconSpacing;
            }

            iconRect.width = TinyStyles.PaneOptionStyle.fixedWidth;
            iconRect.height = TinyStyles.PaneOptionStyle.fixedHeight;
            iconRect.y = iconRect.center.y - 5.5f;
            //iconRect.height = EditorGUIBridge.SingleLineHeight;

            content.tooltip = string.Empty;
            if (GUI.Button(iconRect, content, TinyStyles.PaneOptionStyle))
            {
                
                var index = type.Fields.IndexOf(field);
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent($"Remove"), false, () => RemoveField(type, field));
                menu.AddItem(new GUIContent($"Duplicate"), false, () => DuplicateField(type, field));
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent($"Move Up"), false, () => MoveUp(type, field, index));
                menu.AddItem(new GUIContent($"Move Down"), false, () => MoveDown(type, field, index));
                menu.AddSeparator(string.Empty);

                var id = k_DocumentationPrefix + field.Id;
                var hasDocumentation = EditorPrefs.GetBool(id, false);
                menu.AddItem(new GUIContent($"Documentation"), hasDocumentation, () =>
                {
                    if (!hasDocumentation)
                    {
                        EditorPrefs.SetBool(id, true);
                    }
                    else
                    {
                        EditorPrefs.DeleteKey(id);
                    }
                });
                menu.ShowAsContext();
                EndRename(null);
            }
        }

        private bool DrawField(TinyType type, TinyField field)
        {
            GUILayout.Space(5);

            // Since many IMGUI controls will eat the event, we need to draw the name field in reverse fashion:
            //   1- The context menu icon so that it doesn't get eaten up by the fold or the name.
            //   2- The name field so that it doesn't get eaten up by the fold.
            //   3- The fold.
            var foldRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.foldout);

            var iconsRect = foldRect;
            var iconSpace = IconSpace(type);
            iconsRect.x = foldRect.xMax - iconSpace;
            iconsRect.width = iconSpace;

            DrawIcons(iconsRect, type, field);

            var typeRect = foldRect;
            typeRect.x += EditorGUIUtility.labelWidth;
            typeRect.width -= EditorGUIUtility.labelWidth + iconSpace;
            DrawTypeField(typeRect, type, field);

            var label = GetRenamableLabel(type, field);
            label.RenameOnFirstClick = true;
            label.Delay = 0.0f;
            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.fixedHeight = 18;
            labelStyle.normal.background = TinyIcons.PillSprite;
            labelStyle.onActive.background = TinyIcons.PillSprite;
            labelStyle.focused.background = TinyIcons.PillSprite;
            labelStyle.border = new RectOffset(11, 9, 0, 0);
            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.padding = new RectOffset(15, 0, 0, 0);
            labelStyle.margin = new RectOffset(0, 0, 0, 0);

            var nameRect = foldRect;
            nameRect.x += 10.0f;
            var size = labelStyle.CalcSize(new GUIContent(label.CurrentName));
            nameRect.width =  Mathf.Min(size.x + 5.0f, EditorGUIUtility.labelWidth - 15.0f);

            var labelRect = nameRect;
            if (label.IsRenaming)
            {
                labelRect.width += 5.0f;
            }
            EditorGUI.LabelField(labelRect, GUIContent.none, GUIContent.none, labelStyle);
            EditorGUI.indentLevel += 1;
            label.OnGUI(nameRect, field.Name, labelStyle);
            EditorGUI.indentLevel -= 1;

            return SetFolded(field, EditorGUI.Foldout(foldRect, GetFolded(field), string.Empty, true));
        }

        private void DrawTypeField(Rect rect, TinyType type, TinyField field)
        {
            using (new EditorGUI.DisabledScope(type.IsEnum))
            {
                var c = new GUIContent(field.FieldType.Name);
                if (EditorGUI.DropdownButton(rect, c, FocusType.Passive))
                {
                    AddFieldWindow.Show(rect, type.Registry, type, field);
                }
            }
        }

        private void DrawAddNewField(TinyType type)
        {
            if (type.IsRuntimeIncluded)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var content = new GUIContent($"Add New {(type.IsEnum ? "Value" : "Field")}");

            var rect = GUILayoutUtility.GetRect(content, TinyStyles.AddComponentStyle);
            if (type.IsEnum)
            {
                if (GUI.Button(rect, content, TinyStyles.AddComponentStyle))
                {
                    var field = type.CreateField(TinyId.New(), TinyUtility.GetUniqueName(type.Fields, "NewElement"), type.BaseType);
                    
                    // @HACK
                    if (type.Fields.Count > 1)
                    {
                        var defaultEnum = type.DefaultValue as TinyObject;
                        defaultEnum[field.Name] = (int) defaultEnum[type.Fields[type.Fields.Count - 2].Name] + 1;
                    }

                    OnCreateField(field);
                }
            }
            else
            {
                if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, TinyStyles.AddComponentStyle))
                {
                    AddFieldWindow.Show(rect, type.Registry, type, null, OnCreateField);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private static void RemoveField(TinyType type, TinyField field)
        {
            type.RemoveField(field);
            type.Refresh();
        }

        private void DuplicateField(TinyType type, TinyField field)
        {
            var newField = type.CreateField(TinyId.New(), TinyUtility.GetUniqueName(type.Fields, field.Name), field.FieldType,
                field.Array);
            type.Refresh();
            newField.Documentation.Summary = field.Documentation.Summary;
            (type.DefaultValue as TinyObject)[newField.Name] = (type.DefaultValue as TinyObject)[field.Name];

            OnCreateField(newField);
        }

        private static void MoveUp(TinyType type, TinyField field, int index)
        {
            Move(type, field, Mathf.Max(index - 1, 0));
        }

        private static void MoveDown(TinyType type, TinyField field, int index)
        {
            Move(type, field, Mathf.Min(index + 1, type.Fields.Count - 1));
        }

        private static void Move(TinyType type, TinyField field, int index)
        {
            type.RemoveField(field);
            type.InsertField(index, field);
            type.Refresh();
        }

        private static void VisitFieldDefaultValue(TinyObject defaultValue, string name, TinyType fieldType,
            GUIVisitor visitor, bool isArray)
        {
            var oldEnable = GUI.enabled;
            try
            {
                if (isArray)
                {
                    GUI.enabled = false;
                    visitor.VisitList<TinyObject.PropertiesContainer, TinyList>(defaultValue.Properties, name);
                    return;
                }

                switch (fieldType.TypeCode)
                {
                    case TinyTypeCode.Unknown:
                        break;
                    case TinyTypeCode.Int8:
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, sbyte>(defaultValue.Properties,
                            name);
                        break;
                    case TinyTypeCode.Int16:
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, short>(defaultValue.Properties,
                            name);
                        break;
                    case TinyTypeCode.Int32:
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, int>(defaultValue.Properties,
                            name);
                        break;
                    case TinyTypeCode.Int64:
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, long>(defaultValue.Properties,
                            name);
                        break;
                    case TinyTypeCode.UInt8:
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, byte>(defaultValue.Properties,
                            name);
                        break;
                    case TinyTypeCode.UInt16:
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, ushort>(
                            defaultValue.Properties, name);
                        break;
                    case TinyTypeCode.UInt32:
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, uint>(defaultValue.Properties,
                            name);
                        break;
                    case TinyTypeCode.UInt64:
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, ulong>(defaultValue.Properties,
                            name);
                        break;
                    case TinyTypeCode.Float32:
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, float>(defaultValue.Properties,
                            name);
                        break;
                    case TinyTypeCode.Float64:
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, double>(
                            defaultValue.Properties, name);
                        break;
                    case TinyTypeCode.Boolean:
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, bool>(defaultValue.Properties,
                            name);
                        break;
                    case TinyTypeCode.String:
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, string>(
                            defaultValue.Properties, name);
                        break;
                    case TinyTypeCode.Component:
                        throw new InvalidOperationException("A field's default value cannot be of component type.");
                    case TinyTypeCode.Struct:
                        visitor.VisitContainer<TinyObject.PropertiesContainer, TinyObject>(defaultValue.Properties,
                            name);
                        break;
                    case TinyTypeCode.Enum:
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, TinyEnum.Reference>(
                            defaultValue.Properties, name);
                        break;
                    case TinyTypeCode.Configuration:
                        throw new InvalidOperationException("A field's default value cannot be of configuration type.");
                    case TinyTypeCode.EntityReference:
                        GUI.enabled = false;
                        visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, TinyEntity.Reference>(
                            defaultValue.Properties, name);
                        break;
                    case TinyTypeCode.UnityObject:
                        if (fieldType == TinyType.Texture2DEntity)
                        {
                            visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, Texture2D>(
                                defaultValue.Properties, name);
                        }
                        else if (fieldType == TinyType.SpriteEntity)
                        {
                            visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, Sprite>(
                                defaultValue.Properties, name);
                        }
                        else if (fieldType == TinyType.FontEntity)
                        {
                            visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, TMPro.TMP_FontAsset>(
                                defaultValue.Properties, name);
                        }
                        else if (fieldType == TinyType.AudioClipEntity)
                        {
                            visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, AudioClip>(
                                defaultValue.Properties, name);
                        }
                        else if (fieldType == TinyType.AnimationClipEntity)
                        {
                            visitor.VisitValueClassProperty<TinyObject.PropertiesContainer, AnimationClip>(
                                defaultValue.Properties, name);
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException();
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                GUI.enabled = oldEnable;
            }
        }
        
        private void OnCreateField(TinyField field)
        {
            GetRenamableLabel(field.FieldType.Dereference(Registry), field).RenameOnNextUpdate = true;
        }

        private RenamableLabel GetRenamableLabel(TinyType type, TinyField field)
        {
            if (!Labels.TryGetValue(field, out var label) || null == label)
            {
                var t = type;
                var f = field;
                Labels[field] = label = new RenamableLabel();
                label.OnRenamedEnded += (newName, originalName) =>
                {
                    if (TinyScriptUtility.IsReservedKeyword(newName))
                    {
                        Debug.LogError($"[{TinyConstants.ApplicationName}] FieldName=[{type.Name}] is a reserved keyword");
                        return;
                    }
                    
                    
                    if (TinyUtility.IsValidObjectName(newName) && !TinyUtility.ContainName(t.Fields, newName))
                    {
                        f.Name = newName;
                        GUIUtility.ExitGUI();
                    }
                };
                label.OnRenamedStarted += EndRename;
            }

            return label;
        }

        private void EndRename(RenamableLabel rl)
        {
            foreach (var other in Labels.Values)
            {
                if (other == rl)
                {
                    continue;
                }

                if (!other.IsRenaming)
                {
                    continue;
                }
                        
                other.EndRename(true);
            }
        }
        
        private void ShowModuleMissing(TinyModule mainModule, TinyModule moduleContainingType, TinyField field)
        {
            if (null == moduleContainingType)
            {
                using (new EditorGUILayout.HorizontalScope(TinyStyles.TypeNotFoundStyle))
                {
                    GUILayout.Space(24);
                    EditorGUILayout.LabelField(field.FieldType.Name + " Not Found", TinyStyles.ComponenHeaderLabel);
                }

                return;
            }
            
            using (new EditorGUILayout.HorizontalScope(TinyStyles.TypeMissingStyle))
            {
                GUILayout.Space(24);
                EditorGUILayout.LabelField(field.FieldType.Name + " Missing", TinyStyles.ComponenHeaderLabel);
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