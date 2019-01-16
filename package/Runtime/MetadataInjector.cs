using System;
using UnityEngine;
using UnityEngine.Profiling.Memory.Experimental;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityMemoryProfiler = UnityEngine.Profiling.Memory.Experimental.MemoryProfiler;

namespace Unity.MemoryProfiler
{

#if !MEMPROFILER_DISABLE_METADATA_INJECTOR
    internal static class MetadataInjector
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void EditorInitMetadata()
        {
            InitializeMetadataCollection();
        }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void PlayerInitMetadata()
        {
#if !UNITY_EDITOR
            InitializeMetadataCollection();
#endif
        }

        static void InitializeMetadataCollection()
        {
            var foundTypes = ReflectionUtility.GetConcreteDerivedTypes(typeof(IMetadataCollect), Assembly.GetCallingAssembly());
            if (foundTypes.Count > 0)
            {
                for(int i = 0; i < foundTypes.Count; ++i)
                {
                    var metaCollector = Activator.CreateInstance(foundTypes[i]) as IMetadataCollect;
                    UnityMemoryProfiler.createMetaData += metaCollector.CollectMetadata;
                }
            }
            else
            {
                UnityMemoryProfiler.createMetaData += DefaultCollect;
            }
        }

        static void DefaultCollect(MetaData data)
        {
            data.content = "Project name: " + Application.productName;
#if UNITY_EDITOR
            data.content += "\nScripting Version: " + EditorApplication.scriptingRuntimeVersion.ToString();
#endif
            data.platform = Application.platform.ToString();

            // TODO: Allow screenshot-ing once we have added the capture operation to EndOfFrame callbacks
            //if (Application.isPlaying)
            //{
            //    int width = Screen.width;
            //    int height = Screen.height;

            //    Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            //    //Read pixels from the currently active render target into the newly created image
            //    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0, true);

            //    int divider = 0;

            //    while (width > 480 || height > 240)
            //    {
            //        width /= 2;
            //        height /= 2;
            //        ++divider;
            //    }

            //    data.screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            //    data.screenshot.SetPixels(tex.GetPixels(divider));
            //    data.screenshot.Apply();
            //}
        }
    }
#endif
    public interface IMetadataCollect
    {
        void CollectMetadata(MetaData data);
    }
}
