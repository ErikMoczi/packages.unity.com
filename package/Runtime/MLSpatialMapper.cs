using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental;
using UnityEngine.Experimental.XR;
using UnityEngine.Lumin;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.MagicLeap.Remote;
#endif

namespace UnityEngine.XR.MagicLeap
{
    [AddComponentMenu("AR/Magic Leap/ML Spatial Mapper")]
    [UsesLuminPrivilege("WorldReconstruction")]
    public sealed class MLSpatialMapper : MonoBehaviour
    {
        /// <summary>
        /// What type of mesh to generate: a triangle mesh or a point cloud
        /// </summary>
        public enum MeshType
        {
            /// <summary>
            /// Generate triangle meshes
            /// </summary>
            Triangles,

            /// <summary>
            /// Generate a point cloud (a mesh with <c>MeshTopology.Points</c>)
            /// </summary>
            PointCloud
        }

        /// <summary>
        /// Describes the level of detail (LOD) to request from the generated meshes
        /// </summary>
        public enum LevelOfDetail
        {
            /// <summary>
            /// Minimum level of detail. Meshes will render faster, but be less accurate.
            /// </summary>
            Minimum,

            /// <summary>
            /// Medium level of detail. A good balance between accuracy and render performance.
            /// </summary>
            Medium,

            /// <summary>
            /// Maximum level of detail. This will take more time to render, but the meshes will be more accurate.
            /// </summary>
            Maximum
        }

        [SerializeField]
        GameObject m_MeshPrefab;

        /// <summary>
        /// Get or set the prefab which should be instantiated to create individual mesh instances.
        /// May have a mesh renderer and an optional mesh collider for physics.
        /// </summary>
        public GameObject meshPrefab
        {
            get { return m_MeshPrefab; }
            set { m_MeshPrefab = value; }
        }

        [SerializeField]
        bool m_ComputeNormals = Defaults.computeNormals;

        /// <summary>
        /// When enabled, the system will compute the normals for the triangle vertices.
        /// </summary>
        public bool computeNormals
        {
            get { return m_ComputeNormals; }
            set
            {
                if (m_ComputeNormals != value)
                {
                    m_ComputeNormals = value;
                    m_SettingsDirty = true;
                }
            }
        }

        [SerializeField]
        LevelOfDetail m_LevelOfDetail = Defaults.levelOfDetail;

        public LevelOfDetail levelOfDetail
        {
            get { return m_LevelOfDetail; }
            set
            {
                if (m_LevelOfDetail != value)
                {
                    m_LevelOfDetail = value;
                    SetLod();
                }
            }
        }

        [SerializeField]
        Transform m_MeshParent;

        /// <summary>
        /// The parent transform for generated meshes.
        /// </summary>
        public Transform meshParent
        {
            get { return m_MeshParent; }
            set { m_MeshParent = value; }
        }

        [SerializeField]
        MeshType m_MeshType = Defaults.meshType;

        /// <summary>
        /// Whether to generate a triangle mesh or point cloud points.
        /// </summary>
        public MeshType meshType
        {
            get { return m_MeshType; }
            set
            {
                if (m_MeshType != value)
                {
                    m_MeshType = value;
                    m_SettingsDirty = true;
                }
            }
        }

        [SerializeField]
        float m_FillHoleLength = Defaults.fillHoleLength;

        /// <summary>
        /// Boundary distance (in meters) of holes you wish to have filled.
        /// </summary>
        public float fillHoleLength
        {
            get { return m_FillHoleLength; }
            set
            {
                if (m_FillHoleLength != value)
                {
                    m_FillHoleLength = value;
                    m_SettingsDirty = true;
                }
            }
        }

        [SerializeField]
        bool m_Planarize = Defaults.planarize;

        /// <summary>
        /// When enabled, the system will planarize the returned mesh (planar regions will be smoothed out).
        /// </summary>
        public bool planarize
        {
            get { return m_Planarize; }
            set
            {
                if (m_Planarize != value)
                {
                    m_Planarize = value;
                    m_SettingsDirty = true;
                }
            }
        }

        [SerializeField]
        float m_DisconnectedComponentArea = Defaults.disconnectedComponentArea;

        /// <summary>
        /// Any component that is disconnected from the main mesh and which has an area less than this size will be removed.
        /// </summary>
        public float disconnectedComponentArea
        {
            get { return m_DisconnectedComponentArea; }
            set
            {
                if (m_DisconnectedComponentArea != value)
                {
                    m_DisconnectedComponentArea = value;
                    m_SettingsDirty = true;
                }
            }
        }

        [SerializeField]
        uint m_MeshQueueSize = Defaults.meshQueueSize;

        /// <summary>
        /// Controls the number of meshes to queue for generation at once. Larger numbers will lead to higher CPU usage.
        /// </summary>
        public uint meshQueueSize
        {
            get { return m_MeshQueueSize; }
            set { m_MeshQueueSize = value; }
        }

        [SerializeField]
        float m_PollingRate = Defaults.pollingRate;

        /// <summary>
        /// How often to check for updates, in seconds. More frequent updates will increase CPU usage.
        /// </summary>
        public float pollingRate
        {
            get { return m_PollingRate; }
            set { m_PollingRate = value; }
        }

        [SerializeField]
        int m_BatchSize = Defaults.batchSize;

        /// <summary>
        /// How many meshes to update per batch. Larger values are more efficient, but have higher latency.
        /// </summary>
        public int batchSize
        {
            get { return m_BatchSize; }
            set
            {
                if (m_BatchSize != value)
                {
                    m_BatchSize = value;
                    m_SettingsDirty = true;
                }
            }
        }

        [SerializeField]
        bool m_RequestVertexConfidence = Defaults.requestVertexConfidence;

        /// <summary>
        /// When enabled, the system will generate confidence values for each vertex, ranging from 0-1.
        /// </summary>
        /// <seealso cref="TryGetConfidence(TrackableId, List{float})"/>
        public bool requestVertexConfidence
        {
            get { return m_RequestVertexConfidence; }
            set
            {
                if (m_RequestVertexConfidence != value)
                {
                    m_RequestVertexConfidence = value;
                    m_SettingsDirty = true;
                }
            }
        }

        [SerializeField]
        bool m_RemoveMeshSkirt = Defaults.removeMeshSkirt;

        /// <summary>
        /// When enabled, the mesh skirt (overlapping area between two mesh blocks) will be removed. This field is only valid when the Mesh Type is Blocks.
        /// </summary>
        public bool removeMeshSkirt
        {
            get { return m_RemoveMeshSkirt; }
            set
            {
                if (m_RemoveMeshSkirt != value)
                {
                    m_RemoveMeshSkirt = value;
                    m_SettingsDirty = true;
                }
            }
        }

        Vector3 boundsExtents
        {
            get { return transform.localScale; }
        }

        /// <summary>
        /// A <c>Dictionary</c> which maps mesh ids to their <c>GameObject</c>s.
        /// </summary>
        public Dictionary<TrackableId, GameObject> meshIdToGameObjectMap { get; private set; }

        /// <summary>
        /// An event which is invoked whenever a new mesh is added
        /// </summary>
        public event Action<TrackableId> meshAdded;

        /// <summary>
        /// An event which is invoked whenever an existing mesh is updated (regenerated).
        /// </summary>
        public event Action<TrackableId> meshUpdated;

        /// <summary>
        /// An event which is invoked whenever an existing mesh is removed.
        /// </summary>
        public event Action<TrackableId> meshRemoved;

        /// <summary>
        /// Retrieve the confidence values associated with a mesh. Confidence values
        /// range from 0..1. <see cref="requestVertexConfidence"/> must be enabled.
        /// </summary>
        /// <seealso cref="requestVertexConfidence"/>
        /// <param name="meshId">The unique <c>TrackableId</c> of the mesh.</param>
        /// <param name="confidenceOut">A <c>List</c> of floats, one for each vertex in the mesh.</param>
        /// <returns>True if confidence values were successfully retrieved for the mesh with id <paramref name="meshId"/>.</returns>
        public bool TryGetConfidence(TrackableId meshId, List<float> confidenceOut)
        {
            if (confidenceOut == null)
                throw new ArgumentNullException("confidenceOut");

            if (s_MeshSubsystem == null)
                return false;

            int count = 0;
            var floatPtr = Api.UnityMagicLeap_MeshingAcquireConfidence(meshId, out count);
            if (floatPtr == IntPtr.Zero)
                return false;

            confidenceOut.Clear();
            if (count > 0)
            {
                if (s_FloatBuffer == null || count > s_FloatBufferCapacity)
                {
                    s_FloatBuffer = new float[count];
                    s_FloatBufferCapacity = count;
                }

                Marshal.Copy(floatPtr, s_FloatBuffer, 0, count);
                for (int i = 0; i < count; ++i)
                    confidenceOut.Add(s_FloatBuffer[i]);
            }
            Api.UnityMagicLeap_MeshingReleaseConfidence(meshId);

            return true;
        }

        /// <summary>
        /// Destroy all mesh <c>GameObject</c>s created by this <see cref="MLSpatialMapper"/>.
        /// The <see cref="meshIdToGameObjectMap"/> will also be cleared.
        /// </summary>
        public void DestroyAllMeshes()
        {
            foreach (var kvp in meshIdToGameObjectMap)
            {
                var mesh = kvp.Value;
                Destroy(mesh);
            }

            meshIdToGameObjectMap.Clear();
        }

        /// <summary>
        /// 'Refresh' a single mesh. This forces the mesh to be regenerated with the current settings.
        /// </summary>
        /// <param name="meshId">The <c>TrackableId</c> of the mesh to regenerate.</param>
        public void RefreshMesh(TrackableId meshId)
        {
            if (m_MeshesBeingGenerated.ContainsKey(meshId))
                return;

            m_MeshesNeedingGeneration[meshId] = new MeshInfo
            {
                MeshId = meshId,
                ChangeState = MeshChangeState.Updated,
                PriorityHint = Time.frameCount
            };
        }

        /// <summary>
        /// 'Refresh' all known meshes (meshes that are in <see cref="meshIdToGameObjectMap"/>).
        /// This will force all meshes to be regenerated with the current settings.
        /// </summary>
        public void RefreshAllMeshes()
        {
            foreach (var kvp in meshIdToGameObjectMap)
            {
                var meshId = kvp.Key;
                RefreshMesh(meshId);
            }
        }

#if UNITY_EDITOR
        Api.MLMeshingSettings m_CachedSettings;
        LevelOfDetail m_CachedLod;

        bool hasLodChanged
        {
            get
            {
                return m_CachedLod != levelOfDetail;
            }
        }

        bool haveSettingsChanged
        {
            get
            {
                var currentSettings = GetMeshingSettings();
                return
                    (m_CachedSettings.fillHoleLength != currentSettings.fillHoleLength) ||
                    (m_CachedSettings.flags != currentSettings.flags) ||
                    (m_CachedSettings.disconnectedComponentArea != currentSettings.disconnectedComponentArea);
            }
        }
#endif

        Api.MLMeshingSettings GetMeshingSettings()
        {
            var flags = Api.MLMeshingFlags.IndexOrderCCW;

            if (computeNormals)
                flags |= Api.MLMeshingFlags.ComputeNormals;
            if (requestVertexConfidence)
                flags |= Api.MLMeshingFlags.ComputeConfidence;
            if (planarize)
                flags |= Api.MLMeshingFlags.Planarize;
            if (removeMeshSkirt)
                flags |= Api.MLMeshingFlags.RemoveMeshSkirt;
            if (meshType == MeshType.PointCloud)
                flags |= Api.MLMeshingFlags.PointCloud;

            var settings = new Api.MLMeshingSettings
            {
                flags = flags,
                fillHoleLength = fillHoleLength,
                disconnectedComponentArea = disconnectedComponentArea,
            };

            return settings;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, .5f, 0, .35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }

        // Create new GameObject and parent to ourself
        GameObject CreateGameObject(TrackableId meshId)
        {
            GameObject newGameObject = Instantiate(m_MeshPrefab, meshParent);
            newGameObject.name = string.Format("Mesh {0}", meshId);
            newGameObject.SetActive(true);
            return newGameObject;
        }

        GameObject GetOrCreateGameObject(TrackableId meshId)
        {
            GameObject go = null;
            if (!meshIdToGameObjectMap.TryGetValue(meshId, out go))
            {
                go = CreateGameObject(meshId);
                meshIdToGameObjectMap[meshId] = go;
            }

            return go;
        }

        void Awake()
        {
            meshIdToGameObjectMap = new Dictionary<TrackableId, GameObject>();
            m_MeshesNeedingGeneration = new Dictionary<TrackableId, MeshInfo>();
            m_MeshesBeingGenerated = new Dictionary<TrackableId, MeshInfo>();
        }

        void Init()
        {
            CreateMeshSubsystemIfNeeded();
            if (s_MeshSubsystem == null)
            {
                enabled = false;
                return;
            }

            UpdateSettings();
            UpdateBounds();
            UpdateBatchSize();
            SetLod();
            s_MeshSubsystem.Start();
        }

        void OnEnable()
        {
#if UNITY_EDITOR && PLATFORM_LUMIN
            StartCoroutine(WaitForMagicLeapRemote());
#else
            Init();
#endif // UNITY_EDITOR && PLATFORM_LUMIN
        }

#if UNITY_EDITOR && PLATFORM_LUMIN
        IEnumerator WaitForMagicLeapRemote()
        {
            while (!MagicLeapRemoteManager.isInitialized)
                yield return null;
            Init();
        }
#endif // UNITY_EDITOR && PLATFORM_LUMIN

        void OnDisable()
        {
            if (s_MeshSubsystem != null)
                s_MeshSubsystem.Stop();
        }

        void OnDestroy()
        {
            if (s_MeshSubsystem != null)
            {
                s_MeshSubsystem.Destroy();
                s_MeshSubsystem = null;
            }
        }

        void AddToQueueIfNecessary(MeshInfo meshInfo)
        {
            if (m_MeshesNeedingGeneration.ContainsKey(meshInfo.MeshId))
                return;

            meshInfo.PriorityHint = Time.frameCount;
            m_MeshesNeedingGeneration[meshInfo.MeshId] = meshInfo;
        }

        void SetLod()
        {
            Api.UnityMagicLeap_MeshingSetLod(levelOfDetail);
#if UNITY_EDITOR
            m_CachedLod = levelOfDetail;
#endif
        }

        void UpdateSettings()
        {
            UpdateBatchSize();
            var settings = GetMeshingSettings();
            Api.UnityMagicLeap_MeshingUpdateSettings(settings);
            m_SettingsDirty = false;
#if UNITY_EDITOR
            m_CachedSettings = settings;
#endif
        }

        void UpdateBounds()
        {
            Api.UnityMagicLeap_MeshingSetBounds(transform.localPosition, transform.localRotation, boundsExtents);
            transform.hasChanged = false;
        }

        void UpdateBatchSize()
        {
            Api.UnityMagicLeap_MeshingSetBatchSize(batchSize);
        }

        void Reset()
        {
            transform.localScale = Defaults.boundsExtents;
            m_MeshParent = transform.parent;
        }

        // Every frame, poll the MeshSubsystem for mesh updates (Added, Updated, Removed)
        // If the mesh is Added or Updated, then add it to the generation queue.
        //
        // Create generation requests for each mesh needing it until all have
        // been added to the asynchronous queue, or the queue is full.
        void Update()
        {
            if (s_MeshSubsystem == null)
                return;

#if UNITY_EDITOR
            m_SettingsDirty |= haveSettingsChanged;

            if (hasLodChanged)
                SetLod();
#endif

            if (m_SettingsDirty)
                UpdateSettings();

            if (transform.hasChanged)
                UpdateBounds();

            float timeSinceLastUpdate = (float) (DateTime.Now - m_TimeLastUpdated).TotalSeconds;
            bool allowUpdate = (timeSinceLastUpdate > m_PollingRate);

            if (allowUpdate && s_MeshSubsystem.TryGetMeshInfos(s_MeshInfos))
            {
                foreach (var meshInfo in s_MeshInfos)
                {
                    switch (meshInfo.ChangeState)
                    {
                        case MeshChangeState.Added:
                        case MeshChangeState.Updated:
                            AddToQueueIfNecessary(meshInfo);
                            break;

                        case MeshChangeState.Removed:
                            RaiseMeshRemoved(meshInfo.MeshId);

                            // Remove from processing queue
                            m_MeshesNeedingGeneration.Remove(meshInfo.MeshId);

                            // Destroy the GameObject
                            GameObject meshGameObject;
                            if (meshIdToGameObjectMap.TryGetValue(meshInfo.MeshId, out meshGameObject))
                            {
                                Destroy(meshGameObject);
                                meshIdToGameObjectMap.Remove(meshInfo.MeshId);
                            }

                            break;

                        default:
                            break;
                    }
                }

                m_TimeLastUpdated = DateTime.Now;
            }

            if (meshPrefab != null)
            {
                TrackableId meshId;
                while (m_MeshesBeingGenerated.Count < meshQueueSize && GetNextMeshToGenerate(out meshId))
                {
                    GameObject meshGameObject = GetOrCreateGameObject(meshId);
                    var meshCollider = meshGameObject.GetComponent<MeshCollider>();
                    var meshFilter = meshGameObject.GetComponent<MeshFilter>();
                    var mesh = GetOrCreateMesh(meshFilter);
                    var meshAttributes = computeNormals ? MeshVertexAttributes.Normals : MeshVertexAttributes.None;
                    s_MeshSubsystem.GenerateMeshAsync(meshId, mesh, meshCollider, meshAttributes, OnMeshGenerated);
                    m_MeshesBeingGenerated.Add(meshId, m_MeshesNeedingGeneration[meshId]);
                    m_MeshesNeedingGeneration.Remove(meshId);
                }
            }
        }

        Mesh GetOrCreateMesh(MeshFilter meshFilter)
        {
            if (meshFilter == null)
                return null;

            if (meshFilter.sharedMesh != null)
                return meshFilter.sharedMesh;

            return meshFilter.mesh;
        }

        // Find the oldest one. Prioritize new ones.
        bool GetNextMeshToGenerate(out TrackableId meshId)
        {
            Nullable<KeyValuePair<TrackableId, MeshInfo>> highestPriorityPair = null;
            foreach (var pair in m_MeshesNeedingGeneration)
            {
                // Skip meshes currently being generated
                if (m_MeshesBeingGenerated.ContainsKey(pair.Key))
                    continue;

                if (!highestPriorityPair.HasValue)
                {
                    highestPriorityPair = pair;
                    continue;
                }

                var consideredMeshInfo = pair.Value;
                var selectedMeshInfo = highestPriorityPair.Value.Value;

                // If the selected change type is less than this one,
                // then ignore entirely.
                if (consideredMeshInfo.ChangeState > selectedMeshInfo.ChangeState)
                    continue;

                // If this info has a higher priority change type
                // (e.g. Added rather than Updated) use it instead.
                if (consideredMeshInfo.ChangeState < selectedMeshInfo.ChangeState)
                {
                    highestPriorityPair = pair;
                    continue;
                }

                // If changeTypes are the same, but this one is older,
                // then use it.
                if (consideredMeshInfo.PriorityHint < selectedMeshInfo.PriorityHint)
                {
                    highestPriorityPair = pair;
                    continue;
                }
            }

            if (highestPriorityPair.HasValue)
            {
                meshId = highestPriorityPair.Value.Key;
                return true;
            }
            else
            {
                meshId = TrackableId.InvalidId;
                return false;
            }
        }

        void OnMeshGenerated(MeshGenerationResult result)
        {
            if (result.Status == MeshGenerationStatus.Success)
            {
                // The mesh may have been removed by external code
                MeshInfo meshInfo;
                if (!m_MeshesBeingGenerated.TryGetValue(result.MeshId, out meshInfo))
                    return;

                m_MeshesBeingGenerated.Remove(result.MeshId);
                switch (meshInfo.ChangeState)
                {
                    case MeshChangeState.Added:
                        RaiseMeshAdded(result.MeshId);
                        break;
                    case MeshChangeState.Updated:
                        RaiseMeshUpdated(result.MeshId);
                        break;

                    // Removed/unchanged meshes don't get generated.
                    default:
                        break;
                }
            }
            else
            {
                m_MeshesBeingGenerated.Remove(result.MeshId);
            }
        }

        void RaiseMeshAdded(TrackableId meshId)
        {
            if (meshAdded != null)
                meshAdded(meshId);
        }

        void RaiseMeshUpdated(TrackableId meshId)
        {
            if (meshUpdated != null)
                meshUpdated(meshId);
        }

        void RaiseMeshRemoved(TrackableId meshId)
        {
            if (meshRemoved != null)
                meshRemoved(meshId);
        }

        static void CreateMeshSubsystemIfNeeded()
        {
            if (s_MeshSubsystem != null)
                return;

            s_Descriptors.Clear();
            SubsystemManager.GetSubsystemDescriptors<XRMeshSubsystemDescriptor>(s_Descriptors);

            if (s_Descriptors.Count > 0)
            {
                var descriptorToUse = s_Descriptors[0];
                if (s_Descriptors.Count > 1)
                {
                    Type typeOfD = typeof(XRMeshSubsystemDescriptor);
                    Debug.LogWarningFormat("Found {0} {1}s. Using \"{2}\"",
                        s_Descriptors.Count, typeOfD.Name, descriptorToUse.id);
                }

                s_MeshSubsystem = descriptorToUse.Create();
            }
        }

        static class Defaults
        {
            public static Vector3 boundsExtents = Vector3.one * 10f;
            public static float fillHoleLength = 1f;
            public static bool computeNormals = true;
            public static MeshType meshType = MeshType.Triangles;
            public static bool planarize = false;
            public static float disconnectedComponentArea = .25f;
            public static uint meshQueueSize = 4;
            public static float pollingRate = 0.25f;
            public static int batchSize = 16;
            public static bool requestVertexConfidence = false;
            public static bool removeMeshSkirt = false;
            public static LevelOfDetail levelOfDetail = LevelOfDetail.Maximum;
        }

        bool m_SettingsDirty;

        DateTime m_TimeLastUpdated = DateTime.MinValue;

        Dictionary<TrackableId, MeshInfo> m_MeshesNeedingGeneration;

        Dictionary<TrackableId, MeshInfo> m_MeshesBeingGenerated;

        static List<MeshInfo> s_MeshInfos = new List<MeshInfo>();

        static XRMeshSubsystem s_MeshSubsystem;

        static List<XRMeshSubsystemDescriptor> s_Descriptors = new List<XRMeshSubsystemDescriptor>();

        static float[] s_FloatBuffer;

        static int s_FloatBufferCapacity;
    }

    internal static class Api
    {
        [Flags]
        public enum MLMeshingFlags
        {
            None = 0,
            PointCloud = 1 << 0,
            ComputeNormals = 1 << 1,
            ComputeConfidence = 1 << 2,
            Planarize = 1 << 3,
            RemoveMeshSkirt = 1 << 4,
            IndexOrderCCW = 1 << 5
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MLMeshingSettings
        {
            public MLMeshingFlags flags;
            public float fillHoleLength;
            public float disconnectedComponentArea;
        }

#if UNITY_EDITOR || PLATFORM_LUMIN
        [DllImport("UnityMagicLeap")]
        public static extern void UnityMagicLeap_MeshingUpdateSettings(MLMeshingSettings newSettings);

        [DllImport("UnityMagicLeap")]
        public static extern void UnityMagicLeap_MeshingSetLod(MLSpatialMapper.LevelOfDetail lod);

        [DllImport("UnityMagicLeap")]
        public static extern void UnityMagicLeap_MeshingSetBounds(Vector3 center, Quaternion rotation, Vector3 extents);

        [DllImport("UnityMagicLeap")]
        public static extern void UnityMagicLeap_MeshingSetBatchSize(int batchSize);

        [DllImport("UnityMagicLeap")]
        public static extern IntPtr UnityMagicLeap_MeshingAcquireConfidence(TrackableId meshId, out int count);

        [DllImport("UnityMagicLeap")]
        public static extern void UnityMagicLeap_MeshingReleaseConfidence(TrackableId meshId);
#else
        public static void UnityMagicLeap_MeshingUpdateSettings(MLMeshingSettings newSettings) { }

        public static void UnityMagicLeap_MeshingSetLod(MLSpatialMapper.LevelOfDetail lod) { }

        public static void UnityMagicLeap_MeshingSetBounds(Vector3 center, Quaternion rotation, Vector3 extents) { }

        public static void UnityMagicLeap_MeshingSetBatchSize(int batchSize) {}

        public static IntPtr UnityMagicLeap_MeshingAcquireConfidence(TrackableId meshId, out int count) { count = 0; return IntPtr.Zero; }

        public static void UnityMagicLeap_MeshingReleaseConfidence(TrackableId meshId) { }
#endif
    }
}
