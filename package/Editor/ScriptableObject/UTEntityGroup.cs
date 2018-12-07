

using UnityEditor;
using UnityEditor.Callbacks;

namespace Unity.Tiny
{
    internal class UTEntityGroup : TinyScriptableObject
    {
        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (TinyEditorApplication.ContextType == EditorContextType.None)
            {
                return false;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return true;
            }

            var obj = Selection.activeObject;

            if (obj is UTEntityGroup && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long id))
            {
                var assets = Persistence.GetRegistryObjectIdsForAssetGuid(guid);
                var main = assets[0];

                var registry = TinyEditorApplication.Registry;
                var group = registry?.FindById<TinyEntityGroup>(new TinyId(main));
                if (null == group)
                {
                    // Entity group is from another project.
                    return true;
                }
                var currentContext = TinyEditorApplication.EditorContext.Context;

                var groupManager = currentContext.GetManager<IEntityGroupManager>();

                var groupRef = group.Ref;
                if (!groupManager.LoadedEntityGroups.Contains(groupRef))
                {
                    groupManager.LoadEntityGroup(groupRef);
                }
                return true;
            }

            return false;
        }
    }
}

