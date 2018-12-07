using System;
using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    using Animations;
    using Playables;
    using Experimental.Animations;

    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Runtime Rigging/Setup/Rig Builder")]
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
        }

        struct RigData
        {
            public AnimationPlayableOutput output;
            public AnimationScriptPlayable[] playables;
        }

        [SerializeField]
        private List<RigLayer> m_RigLayers;

        private List<RigData> m_Data;
        private PlayableGraph m_Graph;

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
            if (!m_Graph.IsValid())
                return;

            foreach (var layer in rigLayers)
            {
                if (layer.rig == null)
                    continue;

                layer.rig.UpdateConstraints(
                    m_Data[layer.data].playables,
                    layer.active ? layer.rig.weight : 0f
                    );
            }
        }

        public bool Build()
        {
            Clear();
            var animator = GetComponent<Animator>();
            if (animator == null || rigLayers.Count == 0)
                return false;

            string graphName = gameObject.transform.name + "_Rigs";
            m_Graph = PlayableGraph.Create(graphName);
            m_Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            m_Data = new List<RigData>(rigLayers.Count);
            foreach (var layer in rigLayers)
            {
                if (layer.rig == null)
                    continue;

                layer.rig.Initialize(animator);

                RigData data = new RigData();
                data.output = AnimationPlayableOutput.Create(m_Graph, "rigOutput", animator);
                data.output.SetAnimationStreamSource(AnimationStreamSource.PreviousInputs);

                data.playables = BuildRigPlayables(m_Graph, layer.rig);
                if (data.playables != null && data.playables.Length > 0)
                    data.output.SetSourcePlayable(data.playables[data.playables.Length - 1]);

                layer.data = m_Data.Count;
                m_Data.Add(data);
            }

            m_Graph.Play();
            return true;
        }

        public void Clear()
        {
            if (m_Graph.IsValid())
                m_Graph.Destroy();

            foreach (var layer in rigLayers)
                layer.rig.Clear();

            if (m_Data != null)
                m_Data.Clear();
        }

        AnimationScriptPlayable[] BuildRigPlayables(PlayableGraph graph, Rig rig)
        {
            if (rig == null || rig.jobs == null || rig.jobs.Length == 0)
                return null;

            var count = rig.jobs.Length;
            var playableArray = new AnimationScriptPlayable[count];
            for (int i = 0; i < count; ++i)
            {
                var binder = rig.constraints[i].data.binder;
                playableArray[i] = binder.CreatePlayable(graph, rig.jobs[i]);
            }

            for (int i = 1; i < count; ++i)
                playableArray[i].AddInput(playableArray[i - 1], 0, 1);

            return playableArray;
        }

        public List<RigLayer> rigLayers
        {
            get
            {
                if (m_RigLayers == null)
                    m_RigLayers = new List<RigLayer>();

                return m_RigLayers;
            }

            set => m_RigLayers = value;
        }
    }
}
