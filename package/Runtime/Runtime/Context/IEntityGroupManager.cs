
using System.Collections.ObjectModel;

namespace Unity.Tiny
{
    internal interface IEntityGroupManager : IContextManager
    {
        ReadOnlyCollection<TinyEntityGroup.Reference> LoadedEntityGroups { get; }
        int LoadedEntityGroupCount { get; }
        TinyEntityGroup.Reference ActiveEntityGroup { get; }

        void LoadEntityGroup(TinyEntityGroup.Reference entityGroupRef);
        void LoadEntityGroupAtIndex(TinyEntityGroup.Reference entityGroupRef, int index);
        void UnloadEntityGroup(TinyEntityGroup.Reference entityGroupRef);
        void UnloadAllEntityGroups();
    }
}
