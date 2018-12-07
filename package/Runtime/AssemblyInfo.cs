#if !UNITY_2018_3_OR_NEWER
#error "Unity 2018.3 is required by Tiny Mode"
#endif
                
#if UNITY_2019_2_OR_NEWER
#error "Unity 2019.2 or later is not supported by Tiny Mode yet"
#endif

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Tiny.Editor")]
[assembly: InternalsVisibleTo("Unity.Tiny.Editor.Internal")]
[assembly: InternalsVisibleTo("Unity.Tiny.Editor.Tests")]

