using System.Runtime.CompilerServices;

// Allow internal visibility for testing purposes.
[assembly: InternalsVisibleTo("Unity.TextCore")]

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.TextCore.Editor")]
[assembly: InternalsVisibleTo("Unity.TextMeshPro.Editor")]
#endif
