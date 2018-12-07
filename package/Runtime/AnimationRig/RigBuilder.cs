using System;
using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    using Animations;
    using Playables;
    using Experimental.Animations;

    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Animation Rigging/Setup/Rig Builder")]
    public class RigBuilder : MonoBehaviour
    {
        [Serializable]
        public class RigLayer
        {
            public Rig rig;
            public bool active = true;

            [NonSerialized]
            public int data;

            public RigLayer(Rig rig, bool active = true)
            {
                this.rig = rig;
                this.active = active;
                data = -1;
            }

            public void Reset()
            {
                data = -1;
                if (rig != null)
                    rig.Destroy();
            }

            public bool IsValid() => rig != null && data != -1;
        }

        struct LayerData
        {
            public AnimationPlayableOutput output;
            public AnimationScriptPlayable[] playables;
        }

        [SerializeField]
        private List<RigLayer> m_RigLayers;
        private List<LayerData> m_RigLayerData;

        private IAnimationJob m_SyncSceneToStreamJob;
        private IAnimationJobData m_SyncSceneToStreamJobData;

        void OnEnable()
        {
            Build();
        }

        void OnDisable()
        {
            Clear();
        }

        void Update()
        {
            if (!graph.IsValid())
                return;

            foreach (var layer in layers)
            {
                if (layer.IsValid())
                    layer.rig.UpdateConstraints(
                        m_RigLayerData[layer.data].playables, layer.active
                        );
            }
        }

        public bool Build()
        {
            Clear();
            var animator = GetComponent<Animator>();
            if (animator == null || layers.Count == 0)
                return false;

            string graphName = gameObject.transform.name + "_Rigs";
            graph = PlayableGraph.Create(graphName);
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            // Create sync scene to stream layer
            var syncLayerOutput = AnimationPlayableOutput.Create(graph, "syncSceneToStreamOutput", animator);
            syncLayerOutput.SetAnimationStreamSource(AnimationStreamSource.PreviousInputs);
            
            // Create all rig layers
            m_RigLayerData = new List<LayerData>(layers.Count);
            List<JobTransform> allRigReferences = new List<JobTransform>();
            foreach (var layer in layers)
            {
                if (layer.rig == null || !layer.rig.Initialize(animator))
                    continue;

                LayerData data = new LayerData();
                data.output = AnimationPlayableOutput.Create(graph, "rigOutput", animator);
                data.output.SetAnimationStreamSource(AnimationStreamSource.PreviousInputs);
                data.playables = BuildRigPlayables(graph, layer.rig, ref data.output);

                layer.data = m_RigLayerData.Count;
                m_RigLayerData.Add(data);

                // Gather all references used by rig
                allRigReferences.AddRange(RigUtils.GetAllConstraintReferences(animator, layer.rig.constraints));
            }

            // Create sync to stream job with all rig references
            m_SyncSceneToStreamJobData = RigUtils.CreateSyncSceneToStreamData(allRigReferences.ToArray());
            if (m_SyncSceneToStreamJobData.IsValid())
            {
                m_SyncSceneToStreamJob = RigUtils.syncSceneToStreamBinder.Create(animator, m_SyncSceneToStreamJobData);
                syncLayerOutput.SetSourcePlayable(RigUtils.syncSceneToStreamBinder.CreatePlayable(graph, m_SyncSceneToStreamJob));
            }
            graph.Play();

            return true;
        }

        public void Clear()
        {
            if (graph.IsValid())
                graph.Destroy();

            foreach (var layer in layers)
                layer.Reset();

            if (m_RigLayerData != null)
                m_RigLayerData.Clear();

            if (m_SyncSceneToStreamJobData != null && m_SyncSceneToStreamJobData.IsValid())
            {
                RigUtils.syncSceneToStreamBinder.Destroy(m_SyncSceneToStreamJob);
                m_SyncSceneToStreamJobData = null;
            }
        }

        AnimationScriptPlayable[] BuildRigPlayables(PlayableGraph graph, Rig rig, ref AnimationPlayableOutput output)
        {
            if (rig == null || rig.jobs == null || rig.jobs.Length == 0)
                return null;

            var count = rig.jobs.Length;
            var playables = new AnimationScriptPlayable[count];
            for (int i = 0; i < count; ++i)
            {
                var binder = rig.constraints[i].binder;
                playables[i] = binder.CreatePlayable(graph, rig.jobs[i]);
            }

            // Set null input on first rig playable in order to use inputWeight
            // to set job constraint weight
            playables[0].AddInput(Playable.Null, 0, 1);

            // Connect rest of rig playables serially
            for (int i = 1; i < count; ++i)
                playables[i].AddInput(playables[i - 1], 0, 1);

            // Connect last rig playable to output
            output.SetSourcePlayable(playables[playables.Length - 1]);

            return playables;
        }

        public List<RigLayer> layers
        {
            get
            {
                if (m_RigLayers == null)
                    m_RigLayers = new List<RigLayer>();

                return m_RigLayers;
            }

            set => m_RigLayers = value;
        }

        public PlayableGraph graph { get; private set; }
    }
}
