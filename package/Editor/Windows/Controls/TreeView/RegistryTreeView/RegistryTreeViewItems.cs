
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Tiny
{
    internal partial class RegistryTreeView
    {
        internal abstract class ItemBase : TreeViewItem
        {
            public ItemBase(RegistryTreeView treeView)
            {
                TreeView = treeView;
            }

            public override int depth => parent?.depth + 1 ?? -1;

            public virtual void OnGUI(GUIArgs args)
            {
                args.DefaultOnGUI(args);
            }

            public RegistryTreeView TreeView { get; }

            public virtual void OnSingleClick() { }

            public virtual void OnDoubleClick() { }

            public virtual float GetItemHeight(TinyModule module) => EditorGUIUtility.singleLineHeight;
            public virtual void OnContextClicked(GenericMenu menu) { }

            public abstract object GetValue();

            protected Rect IndentRect(Rect rect, float indent)
            {
                rect.x += indent;
                rect.width -= indent;
                return rect;
            }
        }

        internal class LabelItem : ItemBase
        {
            public override int id => parent.id * 17 ^ displayName.GetHashCode();
            public override string displayName { get; set; }

            public LabelItem(string label, RegistryTreeView treeView)
                :base(treeView)
            {
                displayName = label;
            }

            public override object GetValue()
            {
                return displayName;
            }
        }

        public sealed class Root : ItemBase
        {
            public override int id => -1;
            public override int depth => -1;

            public Root(RegistryTreeView treeView)
                :base(treeView)
            {
                children = new List<TreeViewItem>();
            }

            public override object GetValue()
            {
                throw null;
            }
        }

        internal abstract class Item<TItem> : ItemBase
            where TItem : IRegistryObject
        {
            private readonly TItem m_Value;
            public TItem Value => m_Value;
            protected IRegistry Registry => m_Value.Registry;

            protected Item(TItem value, RegistryTreeView treeView)
                : base(treeView)
            {
                m_Value = value;
            }

            public override string displayName => Value.Name;
            public override int id => Value.Id.GetHashCode();

            public override void OnSingleClick() { }

            public override void OnDoubleClick() { }

            public override object GetValue()
            {
                return Value;
            }
        }

        internal class ProjectItem : Item<TinyProject>
        {
            public ProjectItem(TinyProject value, RegistryTreeView treeView)
                : base(value, treeView) { }

            public override Texture2D icon => TinyIcons.Project;
        }

        internal class ModuleItem : Item<TinyModule>
        {
            public ModuleItem(TinyModule value, RegistryTreeView treeView)
                : base(value, treeView)
            {
            }

            public override Texture2D icon => TinyIcons.Module;
        }

        internal class EntityGroupItem : Item<TinyEntityGroup>
        {
            public EntityGroupItem(TinyEntityGroup value, RegistryTreeView treeView)
                : base(value, treeView) { }

            public override void OnGUI(GUIArgs args)
            {
                if (TinyEditorApplication.Module.StartupEntityGroup.Equals((TinyEntityGroup.Reference)Value))
                {
                    var rect = args.rect;
                    rect.width -= 15.0f;
                    EditorGUI.LabelField(rect, "(Startup)", TinyStyles.RightAlignedLabel);
                }

                args.DefaultOnGUI(args);
            }

            public override Texture2D icon => TinyIcons.EntityGroup;
        }

        internal class TypeItem : Item<TinyType>
        {
            public TypeItem(TinyType value, RegistryTreeView treeView)
                : base(value, treeView) { }

            public override Texture2D icon => GetIconForType(Value);

            private static Texture2D GetIconForType(TinyType type)
            {
                if (type.IsComponent)
                {
                    return TinyIcons.Component;
                }

                if (type.IsStruct)
                {
                    return TinyIcons.Struct;
                }

                if (type.IsEnum)
                {
                    return TinyIcons.Enum;
                }

                return null;
            }
        }

        internal class EntityItem : Item<TinyEntity>
        {
            public EntityItem(TinyEntity value, RegistryTreeView treeView)
                : base(value, treeView) { }
        }

        internal class AssetItem : ItemBase
        {
            private readonly TinyAssetInfo m_AssetInfo;

            public TinyAssetInfo Value => m_AssetInfo;

            public AssetItem(TinyAssetInfo info, RegistryTreeView treeView)
                :base(treeView)
            {
                m_AssetInfo = info;
                foreach (var child in m_AssetInfo.Children)
                {
                    AddChild(CreateSubAssetItem(child, treeView));
                }
            }

            protected virtual AssetItem CreateSubAssetItem(TinyAssetInfo assetInfo, RegistryTreeView treeView)
            {
                return new AssetItem(assetInfo, treeView);
            }

            public override string displayName => m_AssetInfo.Name;
            public override int id => m_AssetInfo.Object.GetInstanceID();
            public override Texture2D icon => (Texture2D)EditorGUIUtility.ObjectContent(Value.Object, Value.Object.GetType()).image;

            public override object GetValue()
            {
                return Value;
            }

            public override float GetItemHeight(TinyModule module)
            {
                var asset = module.GetAsset(Value.Object);
                if (Value.Object is Texture2D)
                {
                    var settings = asset?.ExportSettings as TinyTextureSettings;
                    if (null == settings)
                    {
                        return base.GetItemHeight(module);
                    }

                    if (settings.FormatType == TextureFormatType.JPG || settings.FormatType == TextureFormatType.WebP)
                    {
                        return EditorGUIUtility.singleLineHeight * 4.0f;
                    }

                    return EditorGUIUtility.singleLineHeight * 3.0f;
                }
                else if (Value.Object is AudioClip)
                {
                    var settings = asset?.ExportSettings as TinyAudioClipSettings;
                    if (null == settings)
                    {
                        return base.GetItemHeight(module);
                    }

                    return EditorGUIUtility.singleLineHeight * 2.0f;
                }
                else if (Value.Object is AnimationClip)
                {
                    var settings = asset?.ExportSettings as TinyAnimationClipSettings;
                    if (null == settings)
                    {
                        return base.GetItemHeight(module);
                    }

                    return EditorGUIUtility.singleLineHeight * 2.0f;
                }
                else if (Value.Object is Sprite)
                {
                    var settings = asset?.ExportSettings as TinyGenericAssetExportSettings;
                    if (null == settings)
                    {
                        return base.GetItemHeight(module);
                    }

                    return EditorGUIUtility.singleLineHeight * 2.0f;
                }

                return base.GetItemHeight(module);
            }

            public override void OnGUI(GUIArgs args)
            {
                var asset = args.MainModule.GetAsset(Value.Object);
                if (Value.Object is Texture2D)
                {
                    var settings = asset?.ExportSettings as TinyTextureSettings;
                    if (null != settings)
                    {
                        var oldLabelWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 100;
                        var formatRect = args.rect;
                        formatRect.x += 100.0f;
                        formatRect.width -= 100.0f;
                        formatRect.height = EditorGUIUtility.singleLineHeight;
                        formatRect.y += formatRect.height;
                        EditorGUI.BeginChangeCheck();
                        settings.FormatType = (TextureFormatType)EditorGUI.EnumPopup(formatRect, "Format", settings.FormatType);
                        if (EditorGUI.EndChangeCheck())
                        {
                            TreeView.Reload();
                        }
                        formatRect.y += formatRect.height;
                        if (settings.FormatType == TextureFormatType.JPG)
                        {
                            settings.JpgCompressionQuality = EditorGUI.IntSlider(formatRect, "Compression", settings.JpgCompressionQuality, 1, 100);
                            formatRect.y += formatRect.height;
                        }
                        else if (settings.FormatType == TextureFormatType.WebP)
                        {
                            settings.WebPCompressionQuality = EditorGUI.IntSlider(formatRect, "Compression", settings.WebPCompressionQuality, 1, 100);
                            formatRect.y += formatRect.height;
                        }

                        settings.Embedded = EditorGUI.Toggle(formatRect, "Embedded", settings.Embedded);
                        EditorGUIUtility.labelWidth = oldLabelWidth;
                    }
                }
                else if (Value.Object is AudioClip)
                {
                    var settings = asset?.ExportSettings as TinyAudioClipSettings;

                    if (null != settings)
                    {
                        var oldLabelWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 100;
                        var formatRect = args.rect;
                        formatRect.x += 100.0f;
                        formatRect.width -= 100.0f;
                        formatRect.height = EditorGUIUtility.singleLineHeight;
                        formatRect.y += formatRect.height;
                        settings.Embedded = EditorGUI.Toggle(formatRect, "Embedded", settings.Embedded);
                        EditorGUIUtility.labelWidth = oldLabelWidth;
                    }
                }
                else if (Value.Object is AnimationClip)
                {
                    var settings = asset?.ExportSettings as TinyAnimationClipSettings;

                    if (null != settings)
                    {
                        var oldLabelWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 100;
                        var formatRect = args.rect;
                        formatRect.x += 100.0f;
                        formatRect.width -= 100.0f;
                        formatRect.height = EditorGUIUtility.singleLineHeight;
                        formatRect.y += formatRect.height;
                        settings.Embedded = EditorGUI.Toggle(formatRect, "Embedded", settings.Embedded);
                        EditorGUIUtility.labelWidth = oldLabelWidth;
                    }
                }

                else if (Value.Object is Sprite)
                {
                    var settings = asset?.ExportSettings as TinyGenericAssetExportSettings;
                    if (null != settings)
                    {
                        var oldLabelWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 100;
                        var formatRect = args.rect;
                        formatRect.x += 100.0f;
                        formatRect.width -= 100.0f;
                        formatRect.height = EditorGUIUtility.singleLineHeight;
                        formatRect.y += formatRect.height;
                        settings.Embedded = EditorGUI.Toggle(formatRect, "Embedded", settings.Embedded);
                        EditorGUIUtility.labelWidth = oldLabelWidth;
                    }
                }

                args.DefaultOnGUI(args);
            }
        }

        internal class ScriptItem : ItemBase
        {
            public IScriptObject Value { get; private set; }
            
            public ScriptItem(IScriptObject value, RegistryTreeView treeView) : base(treeView)
            {
                Value = value;
            }
            
            public override string displayName => Value.Name;
            public override int id => Value.QualifiedName.GetHashCode();
            public override Texture2D icon => TinyIcons.System;

            public override object GetValue()
            {
                return Value;
            }
        }
    }
}
