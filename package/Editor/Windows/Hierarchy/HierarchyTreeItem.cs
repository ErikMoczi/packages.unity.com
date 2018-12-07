using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Tiny
{
    /// <summary>
    /// An item that is backed by a scene graph item
    /// </summary>
    internal interface ISceneGraphNodeTreeItem
    {
        ISceneGraph Graph { get; }
        ISceneGraphNode Node { get; }
    }

    internal class HierarchyTreeItemBase : TreeViewItem
    {
        
    }
    
    internal class HierarchyTreeItem<T> : HierarchyTreeItemBase
    {
        public T Value { get; set; }
    }

    internal class EntityGroupTreeItem : HierarchyTreeItem<EntityGroupGraph>
    {
        public override string displayName
        {
            get
            {
                var name = Value.EntityGroup.Name;
                var suffix = Value.EntityGroup.PersistenceId != null && Persistence.IsPersistentObjectChanged(Value.EntityGroup) ? "*" : "";
                return $"{name}{suffix}";
            }
        }

        public override int id => Value.EntityGroupRef.Id.GetHashCode();
    }

    internal class EntityTreeItem : HierarchyTreeItem<EntityNode>, ISceneGraphNodeTreeItem
    {
        public ISceneGraph Graph => Value.Graph;
        public ISceneGraphNode Node => Value;
        public IRegistry Registry { get; set; }
        
        public TinyEntity.Reference EntityRef
        {
            get => Value.EntityRef;
            set => Value.EntityRef = value;
        }

        public Texture2D OverlayIcon { get; set; }

        public override string displayName => EntityRef.Dereference(Registry).Name;

        public override int id
        {
            get
            {
                var entity = Value.EntityRef.Dereference(Registry);

                if (null == entity)
                {
                    return -1;
                }

                if (null == entity.View || !entity.View)
                {
                    return -1;
                }
            
                return entity.View.gameObject.GetInstanceID();
            }
        }
    }

    internal class PrefabInstanceTreeItem : HierarchyTreeItem<PrefabInstanceNode>, ISceneGraphNodeTreeItem
    {
        public ISceneGraph Graph => Value.Graph;
        public ISceneGraphNode Node => Value;
        public IRegistry Registry { get; set; }
        public Texture2D OverlayIcon { get; set; }
        public override string displayName { get; set; }
        public override int id => Value.PrefabInstanceRef.Id.GetHashCode();
    }

    internal class FolderTreeItem : HierarchyTreeItem<FolderNode>, ISceneGraphNodeTreeItem
    {
        public ISceneGraph Graph => Value.Graph;
        public ISceneGraphNode Node => Value;
        public override string displayName => (Node as FolderNode)?.Name;
        public override int id => displayName.GetHashCode();
    }
}
