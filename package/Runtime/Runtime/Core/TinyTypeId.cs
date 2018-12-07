

namespace Unity.Tiny
{
    /// <summary>
    /// TypeId is used as an optimization when consuming data
    /// It is used to inform consumers of the type so the object can be created and loaded upfront
    /// </summary>
    internal enum TinyTypeId : ushort
    {
        Unknown = 0,
        Project = 1,
        Module = 2,
        Type = 3,
        EntityGroup = 4,
        Entity = 5,
        Script = 6,
        System = 7,
        EnumReference = 8,
        EntityReference = 9,
        UnityObject = 10,
        EntityGroupReference = 11,
        PrefabInstance = 12,
        PrefabInstanceReference = 13
    }
}

