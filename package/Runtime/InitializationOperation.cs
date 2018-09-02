using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.Diagnostics;

namespace UnityEngine.AddressableAssets
{
    class InitializationOperation : AsyncOperationBase<bool>
    {
        public InitializationOperation()
        {
            AddResourceProviders();
            var playerSettingsLocation = AAConfig.ExpandPathWithGlobalVariables(ResourceManagerRuntimeData.PlayerSettingsLoadLocation);
            var runtimeDataLocation = new ResourceLocationBase("RuntimeData", playerSettingsLocation, typeof(JsonAssetProvider).FullName);
            Context = runtimeDataLocation;
            ResourceManager.ProvideResource<ResourceManagerRuntimeData>(runtimeDataLocation).Completed += OnDataLoaded;
        }

        void OnDataLoaded(IAsyncOperation<ResourceManagerRuntimeData> op)
        {
            if (op.Result == null)
                throw new Exception("Unable to load runtime data.");
            var rtd = op.Result;
            DiagnosticEventCollector.ProfileEvents = rtd.profileEvents;
            AAConfig.AddCachedValue("ContentVersion", rtd.contentVersion);
            if (rtd.usePooledInstanceProvider)
                ResourceManager.InstanceProvider = new PooledInstanceProvider("PooledInstanceProvider", 10);
            else
                ResourceManager.InstanceProvider = new InstanceProvider();

            Addressables.ResourceLocators.Add(rtd.catalogLocations.Create());
            LoadContentCatalog(rtd, 0);
        }

        void LoadContentCatalog(ResourceManagerRuntimeData rtd, int index)
        {
            while (index < rtd.catalogLocations.locations.Count && !rtd.catalogLocations.locations[index].m_isLoadable)
                index++;
            IList<IResourceLocation> locations;
            if (Addressables.GetResourceLocations(rtd.catalogLocations.locations[index].m_address, out locations))
            {
                ResourceManager.ProvideResource<ResourceLocationList>(locations[0]).Completed += (op) =>
                {
                    if (op.Result != null)
                    {
                        Debug.LogFormat("Loaded content catalog from {0}", op.Context);
                        Addressables.ResourceLocators.Clear();
                        Addressables.ResourceLocators.Add(op.Result.Create());
                        Addressables.ResourceLocators.Add(new AssetReferenceLocator());
                        SetResult(true);
                        InvokeCompletionEvent();
                    }
                    else
                    {
                        if (index + 1 >= rtd.catalogLocations.locations.Count)
                        {
                            Debug.LogError("Failed to load content catalog.");
                            SetResult(false);
                            InvokeCompletionEvent();
                        }
                        else
                        {
                            LoadContentCatalog(rtd, index + 1);
                        }
                    }
                };
            }
        }

        private void AddResourceProviders()
        {
            ResourceManager.SceneProvider = new SceneProvider();
            
            ResourceManager.ResourceProviders.Add(new JsonAssetProvider());
            ResourceManager.ResourceProviders.Add(new TextDataProvider());
            ResourceManager.ResourceProviders.Add(new ContentCatalogProvider());

            if (!Application.isEditor)
            {
                ResourceManager.ResourceProviders.Insert(0, new CachedProvider(new BundledAssetProvider()));
                ResourceManager.ResourceProviders.Insert(0, new CachedProvider(new LocalAssetBundleProvider()));
                ResourceManager.ResourceProviders.Insert(0, new CachedProvider(new RemoteAssetBundleProvider()));
            }
            else
            {
#if UNITY_EDITOR
                var playMode = (ResourceManagerRuntimeData.EditorPlayMode)PlayerPrefs.GetInt("AddressablesPlayMode", 0);
                switch (playMode)
                {
                    case ResourceManagerRuntimeData.EditorPlayMode.FastMode:
                        ResourceManager.ResourceProviders.Insert(0, new CachedProvider(new AssetDatabaseProvider()));
                        break;
                    case ResourceManagerRuntimeData.EditorPlayMode.VirtualMode:
                        VirtualAssetBundleManager.AddProviders(AAConfig.ExpandPathWithGlobalVariables);
                        break;
                    case ResourceManagerRuntimeData.EditorPlayMode.PackedMode:
                        {
                            ResourceManager.ResourceProviders.Insert(0, new CachedProvider(new BundledAssetProvider()));
                            ResourceManager.ResourceProviders.Insert(0, new CachedProvider(new LocalAssetBundleProvider()));
                            ResourceManager.ResourceProviders.Insert(0, new CachedProvider(new RemoteAssetBundleProvider()));
                        }
                        break;
                }
#endif
            }
        }
    }
}