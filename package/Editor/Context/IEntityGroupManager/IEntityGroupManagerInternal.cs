
using UnityEngine.SceneManagement;

namespace Unity.Tiny
{
    internal interface IEntityGroupManagerInternal : IEntityGroupManager
    {
        Scene UnityScratchPad { get; }

        TinyEntityGroupManager.EntityGroupEventHandler OnWillLoadEntityGroup { get; set; }
        TinyEntityGroupManager.EntityGroupEventHandler OnEntityGroupLoaded { get; set; }
        TinyEntityGroupManager.EntityGroupEventHandler OnWillUnloadEntityGroup { get; set; }
        TinyEntityGroupManager.EntityGroupEventHandler OnEntityGroupUnloaded { get; set; }
        TinyEntityGroupManager.OnEntityGroupsReorderedHandler OnEntityGroupsReordered { get; set; }

        void SetActiveEntityGroup(TinyEntityGroup.Reference entityGroupRef);
        void SetActiveEntityGroup(TinyEntityGroup.Reference entityGroupRef, bool rebuildWorkspace);
        void MoveUp(TinyEntityGroup.Reference entityGroupRef);
        void MoveDown(TinyEntityGroup.Reference entityGroupRef);
        void UnloadAllEntityGroupsExcept(TinyEntityGroup.Reference entityGroupRef);
        void CreateNewEntityGroup();
        void InitialGroupLoading();
        void ShowOpenEntityGroupMenu();
        void RecreateEntityGroupGraph(TinyEntityGroup.Reference groupRef);
        void RecreateEntityGroupGraphs();
        EntityGroupGraph GetSceneGraph(TinyEntityGroup.Reference entityGroupRef);
    }
}
