using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ResourceManagement.ResourceProviders;

namespace ResourceManagement.Samples
{
    /*
     * This data is intended to be serialized and passed to the player.  A build script would create an instance of this data and call the Save() method.
     * In the player initialization code, ResourceManagerRuntimeData.Initialize() will load and init the ResourceManager.
     */
    public class ResourceManagerRuntimeData
    {
        //This controls which providers are added to the ResourceManager
        public enum ProviderMode
        {
            FastMode,       //faster iteration mode, assets are loaded directly through the AssetDatabase
            VirtualBundles, //asset bundles are simulated, use this mode or AssetB
            AssetBundles    //run against asset bundles, even in play mode
        }

        //serializable data for resource locations
        [Serializable]
        public class ResourceLocationData
        {
            public string address;
            public string guid;
            public string id;
            public string provider;
            public string[] dependencies;
            public ResourceLocationData(string a, string g, string i, string p, string[] deps)
            {
                address = a;
                guid = g;
                id = i;
                provider = p;
                dependencies = deps;
            }
        }

        //runtime class for resource locator
        public class ResourceLocationMap : IResourceLocator<string>
        {
            Dictionary<string, IResourceLocation<string>> m_addressMap = new Dictionary<string, IResourceLocation<string>>();

            public ResourceLocationMap(List<ResourceLocationData> locationData)
            {
                var dataMap = new Dictionary<string, ResourceLocationData>();

                //create and collect locations
                for (int i = 0; i < locationData.Count; i++)
                {
                    var locData = locationData[i];
                    var loc = new ResourceLocationBase<string>(locData.address, locData.id, locData.provider);
                    m_addressMap.Add(locData.address, loc);
                    dataMap.Add(locData.address, locData);
                }

                //fix up dependencies between them
                foreach (var kvp in m_addressMap)
                {
                    var deps = kvp.Value.dependencies;
                    var data = dataMap[kvp.Key];
                    foreach (var d in data.dependencies)
                        kvp.Value.dependencies.Add(Locate(d));
                }
            }

            //IResourceLocator interface
            public IResourceLocation Locate(string address)
            {
                IResourceLocation<string> loc = null;
                m_addressMap.TryGetValue(address, out loc);
                return loc;
            }
        }

        //attach to RM profiler
        public bool profileEvents = true;

        //the mode determines the providers added to ResourceManager
        public ProviderMode resourceProviderMode = ProviderMode.VirtualBundles;

        //serialized locations used to initialize ResourceLocationMap
        public List<ResourceLocationData> locationData = new List<ResourceLocationData>();

        public static string PlayerLocation { get { return Path.Combine(Application.streamingAssetsPath, "ResourceManagerRuntimeData.json"); } }
        //to uses this, call this method from [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            try
            {
                if (File.Exists(PlayerLocation))
                {
                    var runtimeData = JsonUtility.FromJson<ResourceManagerRuntimeData>(File.ReadAllText(PlayerLocation));
                    if (runtimeData != null)
                        runtimeData.Init();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal void Init()
        {
            //add IResourceLocator that is initialized from locationData created in BuildScript
            ResourceManager.resourceLocators.Add(new ResourceLocationMap(locationData));

            //this provider used for all Instantiate calls
            ResourceManager.instanceProvider = new InstanceProvider();

            //set up different providers depending on the mode selected at build time
            switch (resourceProviderMode)
            {
                case ProviderMode.FastMode:
                    ResourceManager.resourceProviders.Insert(0, new AssetDatabaseProvider());
                    break;
                case ProviderMode.VirtualBundles:
                    ResourceProviders.Simulation.VirtualAssetBundleManager.AddProviders();
                    break;
                case ProviderMode.AssetBundles:
                {
                    ResourceManager.resourceProviders.Insert(0, new CachedProvider(new BundledAssetProvider()));
                    ResourceManager.resourceProviders.Insert(0, new CachedProvider(new LocalAssetBundleProvider()));
                    ResourceManager.resourceProviders.Insert(0, new CachedProvider(new RemoteAssetBundleProvider()));
                }
                break;
            }
            EditorDiagnostics.EventCollector.profileEvents = profileEvents;
        }

#if UNITY_EDITOR
        //Serialize this class in JSON format to a location for the player to load
        public void Save()
        {
            var data = JsonUtility.ToJson(this);
            if (!Directory.Exists(Path.GetDirectoryName(PlayerLocation)))
                Directory.CreateDirectory(Path.GetDirectoryName(PlayerLocation));
            File.WriteAllText(PlayerLocation, data);
        }
#endif

    }
}
