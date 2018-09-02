#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    [Serializable]
    public class VirtualAssetBundleEntry
    {
        [SerializeField]
        string m_name;
        public string Name { get { return m_name; } }
        [SerializeField]
        long m_size;
        public long Size { get { return m_size; } }

        public VirtualAssetBundleEntry() { }
        public VirtualAssetBundleEntry(string name, long size)
        {
            m_name = name;
            m_size = size;
        }
    }

    [Serializable]
    public class VirtualAssetBundle : ISerializationCallbackReceiver
    {
        [SerializeField]
        string m_name;
        [SerializeField]
        bool m_isLocal;
        [SerializeField]
        long m_dataSize;
        [SerializeField]
        long m_headerSize;
        [SerializeField]
        float m_latency;

        [SerializeField]
        List<VirtualAssetBundleEntry> m_serializedAssets = new List<VirtualAssetBundleEntry>();

        long m_headerBytesLoaded;
        long m_dataBytesLoaded;

        LoadAssetBundleOp m_bundleLoadOperation;
        List<IVirtualLoadable> m_assetLoadOperations = new List<IVirtualLoadable>();
        Dictionary<string, VirtualAssetBundleEntry> m_assetMap;

        public string Name { get { return m_name; } }
        public List<VirtualAssetBundleEntry> Assets { get { return m_serializedAssets; } }

        public VirtualAssetBundle()
        {
        }

        public float PercentComplete
        {
            get
            {
                return (float)(m_headerBytesLoaded + m_dataBytesLoaded) / (m_headerSize + m_dataSize);
            }
        }

        public VirtualAssetBundle(string name, bool local)
        {
            m_latency = .1f;
            m_name = name;
            m_isLocal = local;
            m_headerBytesLoaded = 0;
            m_dataBytesLoaded = 0;
        }

        public void SetSize(long dataSize, long headerSize)
        {
            m_headerSize = headerSize;
            m_dataSize = dataSize;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            m_assetMap = new Dictionary<string, VirtualAssetBundleEntry>();
            foreach (var a in m_serializedAssets)
                m_assetMap.Add(a.Name, a);
        }

        class LoadAssetBundleOp : AsyncOperationBase<VirtualAssetBundle>
        {
            VirtualAssetBundle m_bundle;
            float m_lastUpdateTime = 0;
            public LoadAssetBundleOp(IResourceLocation location, VirtualAssetBundle bundle)
            {
                Context = location;
                Retain();
                m_bundle = bundle;
                m_lastUpdateTime = Time.unscaledTime + bundle.m_latency;
            }

            public override float PercentComplete
            {
                get
                {
                    if (IsDone)
                        return 1f;
                    return m_bundle.PercentComplete;
                }
            }
            public bool Update(long localBandwidth, long remoteBandwidth)
            {
                Validate();

                if (Time.unscaledTime > m_lastUpdateTime + .1f)
                {
                    long localBytes = (long)Math.Ceiling((Time.unscaledTime - m_lastUpdateTime) * localBandwidth);
                    long remoteBytes = (long)Math.Ceiling((Time.unscaledTime - m_lastUpdateTime) * remoteBandwidth);
                    m_lastUpdateTime = Time.unscaledDeltaTime;

                    if (m_bundle.LoadData(localBytes, remoteBytes))
                    {
                        SetResult(m_bundle);
                        InvokeCompletionEvent();
                    }
                }
                return true;
            }
        }

        private bool LoadData(long localBytes, long remoteBytes)
        {
            if (m_isLocal)
            {
                m_headerBytesLoaded += localBytes;
                return m_headerBytesLoaded > m_headerSize;
            }

            m_dataBytesLoaded += remoteBytes;
            if (m_dataBytesLoaded >= m_dataSize)
            {
                m_isLocal = true;
                m_headerBytesLoaded = 0;
            }
            return false;
        }

        internal bool Unload()
        {
            if (m_bundleLoadOperation == null)
                Debug.LogWarningFormat("Simulated assetbundle {0} is already unloaded.", m_name);
            m_headerBytesLoaded = 0;
            m_bundleLoadOperation = null;
            return true;
        }

        internal IAsyncOperation<VirtualAssetBundle> StartLoad(IResourceLocation location)
        {
            if (m_bundleLoadOperation != null)
            {
                if (m_bundleLoadOperation.IsDone)
                    Debug.LogWarningFormat("Simulated assetbundle {0} is already loaded.", m_name);
                else
                    Debug.LogWarningFormat("Simulated assetbundle {0} is already loading.", m_name);
                return m_bundleLoadOperation;
            }
            m_headerBytesLoaded = 0;
            return (m_bundleLoadOperation = new LoadAssetBundleOp(location, this));
        }


        public IAsyncOperation<TObject> LoadAssetAsync<TObject>(IResourceLocation location) where TObject : class
        {
            if (location == null)
                throw new ArgumentException("IResourceLocation location cannot be null.");
            if (m_bundleLoadOperation == null)
                return new EmptyOperation<TObject>().Start(location, location, default(TObject), new ResourceManagerException("LoadAssetAsync called on unloaded bundle " + m_name));

            if (!m_bundleLoadOperation.IsDone)
                return new EmptyOperation<TObject>().Start(location, location, default(TObject), new ResourceManagerException("LoadAssetAsync called on loading bundle " + m_name));

            VirtualAssetBundleEntry assetInfo;
            if (!m_assetMap.TryGetValue(location.InternalId, out assetInfo))
                return new EmptyOperation<TObject>().Start(location, location, default(TObject), new ResourceManagerException(string.Format("Unable to load asset {0} from simulated bundle {1}.", location.InternalId, Name)));

            LoadAssetOp<TObject> op = new LoadAssetOp<TObject>(location, assetInfo);
            m_assetLoadOperations.Add(op);
            return op;
        }

        internal void CountBandwidthUsage(ref long localCount, ref long remoteCount)
        {
            if (m_bundleLoadOperation != null && m_bundleLoadOperation.IsDone)
            {
                localCount += (long)m_assetLoadOperations.Count;
                return;
            }

            if (m_isLocal)
                localCount++;
            else
                remoteCount++;
        }

        interface IVirtualLoadable
        {
            bool Load(long localBandwidth, long remoteBandwidth);
        }

        class LoadAssetOp<TObject> : AsyncOperationBase<TObject>, IVirtualLoadable where TObject : class
        {
            long m_bytesLoaded;
            float m_lastUpdateTime;
            VirtualAssetBundleEntry m_assetInfo;
            public LoadAssetOp(IResourceLocation location, VirtualAssetBundleEntry assetInfo)
            {
                Context = location;
                m_assetInfo = assetInfo;
                m_lastUpdateTime = Time.realtimeSinceStartup;
            }

            public override float PercentComplete { get { return Mathf.Clamp01((float)(m_bytesLoaded / m_assetInfo.Size)); } }
            public bool Load(long localBandwidth, long remoteBandwidth)
            {
                if (Time.unscaledTime > m_lastUpdateTime)
                {
                    m_bytesLoaded += (long)Math.Ceiling((Time.unscaledTime - m_lastUpdateTime) * localBandwidth);
                    m_lastUpdateTime = Time.unscaledDeltaTime;
                }
                if (m_bytesLoaded < m_assetInfo.Size)
                    return true;
                var assetPath = (Context as IResourceLocation).InternalId;
                SetResult(UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(TObject)) as TObject);
                InvokeCompletionEvent();
                return false;
            }
        }

        //return true until complete
        public bool UpdateAsyncOperations(long localBandwidth, long remoteBandwidth)
        {
            if (m_bundleLoadOperation == null)
                return false;

            if (!m_bundleLoadOperation.IsDone)
                return m_bundleLoadOperation.Update(localBandwidth, remoteBandwidth);

            foreach (var o in m_assetLoadOperations)
            {
                if (!o.Load(localBandwidth, remoteBandwidth))
                {
                    m_assetLoadOperations.Remove(o);
                    break;
                }
            }
            return m_assetLoadOperations.Count > 0;
        }

    }
}
#endif
