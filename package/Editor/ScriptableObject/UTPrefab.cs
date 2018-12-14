using UnityEditor;
using UnityEditor.Callbacks;

namespace Unity.Tiny
{
    internal class UTPrefab : TinyScriptableObject
    {
        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (TinyEditorApplication.ContextType == EditorContextType.None)
            {
                return false;
            }

            var obj = Selection.activeObject;

            if (!(obj is UTPrefab) || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long id))
            {
                return false;
            }
            
            var assets = Persistence.GetRegistryObjectIdsForAssetGuid(guid);
            var main = assets[0];

            var registry = TinyEditorApplication.Registry;
            var prefab = registry?.FindById<TinyEntityGroup>(new TinyId(main));
            
            if (null == prefab)
            {
                // Entity group is from another project.
                return true;
            }
            
            var currentContext = TinyEditorApplication.EditorContext.Context;
            var groupManager = currentContext.GetManager<IEntityGroupManager>();
            var prefabManager = currentContext.GetManager<IPrefabManager>();

            // Spawn the instance
            var instance = prefabManager.Instantiate(prefab);
            
            // Integrate it into the group
            var group = groupManager.ActiveEntityGroup.Dereference(currentContext.Registry);
            
            foreach (var entity in instance.Entities)
            {
                entity.Dereference(registry).EntityGroup = group;
                group.AddEntityReference(entity);
            }
            
            group.PrefabInstances.Add(instance.Ref);
            instance.EntityGroup = group.Ref;
            
            // Re-build the scene graph
            TinyEventDispatcher<ChangeSource>.Dispatch(ChangeSource.DataModel);
            
            return true;
        }
    }
}