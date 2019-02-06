
using UnityEditor.IMGUI.Controls;

namespace Unity.Tiny
{
    internal partial class RegistryTreeView
    {
        internal class RootItemFactory<T>
        { 
            public delegate TreeViewItem CreateHandler(T item, RegistryTreeView treeView);
            public delegate bool ShouldCreateHandler(T item);
            public delegate bool MatchesFilterHandler(T value, Filters filter);

            public CreateHandler Create { get; set; }
            public ShouldCreateHandler ShouldCreateRootOfType { get; set; }
            public MatchesFilterHandler MatchesFilter { get; set; }
        }

        internal static class Defaults
        {
            public static TreeViewItem CreateItem(TinyProject project, RegistryTreeView treeView)     => new ProjectItem(project, treeView);
            public static TreeViewItem CreateItem(TinyModule module, RegistryTreeView treeView)       => new ModuleItem(module, treeView);
            public static TreeViewItem CreateItem(TinyType type, RegistryTreeView treeView)           => new TypeItem(type, treeView);
            public static TreeViewItem CreateItem(TinyEntityGroup group, RegistryTreeView treeView)   => new EntityGroupItem(group, treeView);
            public static TreeViewItem CreateItem(TinyEntity entity, RegistryTreeView treeView)       => new EntityItem(entity, treeView);
            public static TreeViewItem CreateItem(TinyAssetInfo assetInfo, RegistryTreeView treeView) => new AssetItem(assetInfo, treeView);
            public static TreeViewItem CreateItem(IScriptObject scriptObject, RegistryTreeView treeView) => new ScriptItem(scriptObject, treeView);

            public static bool AlwaysCreate<T>(T item) => true;
            public static bool NeverCreate<T>(T item) => false;

            public static bool MatchesFilter(TinyProject project, Filters filter) => filter.HasFlag(Filters.Project);
            public static bool MatchesFilter(TinyModule module, Filters filter) => filter.HasFlag(Filters.Module);
            public static bool MatchesFilter(TinyType type, Filters filter) => FilterType(type, filter);
            public static bool MatchesFilter(TinyEntityGroup group, Filters filter) => filter.HasFlag(Filters.EntityGroup);
            public static bool MatchesFilter(TinyEntity entity, Filters filter) => filter.HasFlag(Filters.Entity);
            public static bool MatchesFilter(TinyAssetInfo assetInfo, Filters filter) => filter.HasFlag(Filters.Asset);
            public static bool MatchesFilter(IScriptObject scriptObject, Filters filter) => FilterScriptObject(scriptObject, filter);

            private static bool FilterType(TinyType type, Filters filter)
            {
                var isEnum = type.TypeCode == TinyTypeCode.Enum;
                var isStruct  = type.TypeCode == TinyTypeCode.Struct;
                var isComponent = type.TypeCode == TinyTypeCode.Component;
                var isType = isEnum || isStruct || isComponent;

                return (filter.HasFlag(Filters.Type) && isType)
                    || (filter.HasFlag(Filters.Enum) && isEnum)
                    || (filter.HasFlag(Filters.Struct) && isStruct)
                    || (filter.HasFlag(Filters.Component) && isComponent);
            }
            
            private static bool FilterScriptObject(IScriptObject obj, Filters filter)
            {
                var isComponentSystem = obj is ScriptComponentSystem;
                var isEntityFilter = obj is ScriptEntityFilter;
                var isComponentBehaviour = obj is ScriptComponentBehaviour;

                return filter.HasFlag(Filters.Script)
                    || (isComponentSystem && filter.HasFlag(Filters.ComponentSystem))
                    || (isEntityFilter && filter.HasFlag(Filters.EntityFilter))
                    || (isComponentBehaviour && filter.HasFlag(Filters.ComponentBehaviour));
            }
        }

    }
}
