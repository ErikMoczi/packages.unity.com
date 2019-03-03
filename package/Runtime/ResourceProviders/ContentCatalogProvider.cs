using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.AddressableAssets.ResourceProviders
{
    /// <summary>
    /// Provider for content catalogs.  This provider makes use of a hash file to determine if a newer version of the catalog needs to be downloaded.
    /// </summary>
    public class ContentCatalogProvider : ResourceProviderBase
    {
        public ContentCatalogProvider()
        {
            m_BehaviourFlags = ProviderBehaviourFlags.CanProvideWithFailedDependencies;
        }
        internal class InternalOp<TObject> : AsyncOperationBase<TObject> where TObject : class
        {
            int m_StartFrame;
            string m_LocalDataPath;
            string m_HashValue;

            public IAsyncOperation<TObject> Start(IResourceLocation location, IList<object> deps)
            {
                Validate();
                m_LocalDataPath = null;
                m_HashValue = null;
                m_StartFrame = Time.frameCount;
                m_Result = null;
                Context = location;
                if (deps == null || deps.Count != 2)
                {
                    Addressables.LogWarningFormat("Addressables -Invalid dependencies for content catalog at location {0}", location);
                    SetResult(default(TObject));
                    DelayedActionManager.AddAction((Action)InvokeCompletionEvent);
                }
                else
                {
                    var remoteHash = deps[0] as string;
                    var localHash = deps[1] as string;
                    Addressables.LogFormat("Addressables - ContentCatalogProvider LocalHash = {0}, RemoteHash = {1}.", localHash, remoteHash);

                    if (remoteHash == localHash || string.IsNullOrEmpty(remoteHash))
                    {
                        if (string.IsNullOrEmpty(localHash))
                            Addressables.LogFormat("Addressables - Unable to load localHash catalog hash: {0}.", OperationException);
                        if (location.Dependencies != null)
                        {
                            var localDataPath = location.Dependencies[1].InternalId.Replace(".hash", ".json");
                            Addressables.LogFormat("Addressables - Using content catalog from {0}.", localDataPath);
                            Addressables.ResourceManager.ProvideResource<ContentCatalogData>(new ResourceLocationBase(localDataPath, localDataPath, typeof(JsonAssetProvider).FullName)).Completed += OnCatalogLoaded;
                        }
                    }
                    else
                    {

                        if (location.Dependencies != null)
                        {
                            var remoteDataPath = location.Dependencies[0].InternalId.Replace(".hash", ".json");
                            m_LocalDataPath = location.Dependencies[1].InternalId.Replace(".hash", ".json");
                            m_HashValue = remoteHash;
                            Addressables.LogFormat("Addressables - Using content catalog from {0}.", remoteDataPath);
                            Addressables.ResourceManager.ProvideResource<ContentCatalogData>(new ResourceLocationBase(remoteDataPath, remoteDataPath, typeof(JsonAssetProvider).FullName)).Completed += OnCatalogLoaded;
                        }
                    }
                }
                return this;
            }

            void OnCatalogLoaded(IAsyncOperation<ContentCatalogData> op)
            {
                Addressables.LogFormat("Addressables - Content catalog load result = {0}.", op.Result);
                Validate();
                SetResult(op.Result as TObject);
                ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncCompletion, Context, Time.frameCount - m_StartFrame);
                InvokeCompletionEvent();
                if (op.Result != null && !string.IsNullOrEmpty(m_HashValue) && !string.IsNullOrEmpty(m_LocalDataPath))
                {
                    var dir = Path.GetDirectoryName(m_LocalDataPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    var localCachePath = m_LocalDataPath;
                    Addressables.LogFormat("Addressables - Saving cached content catalog to {0}.", localCachePath);
                    File.WriteAllText(localCachePath, JsonUtility.ToJson(op.Result));
                    File.WriteAllText(localCachePath.Replace(".json", ".hash"), m_HashValue);
                }
            }
        }

        ///<inheritdoc/>
        public override IAsyncOperation<TObject> Provide<TObject>(IResourceLocation location, IList<object> deps)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            var operation = AsyncOperationCache.Instance.Acquire<InternalOp<TObject>>();
            return operation.Start(location, deps);
        }
    }
}