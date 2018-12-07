using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    using Playables;
    using Experimental.Animations;

    [AddComponentMenu("Runtime Rigging/Setup/Rig")]
    public class Rig : MonoBehaviour
    {
        [Range(0f, 1f)] public float weight = 1f;

        private IRigConstraint[] m_Constraints;
        private IAnimationJob[] m_Jobs;
        private bool m_IsInitialized;

        public void Initialize(Animator animator)
        {
            if (m_IsInitialized)
                return;

            m_Constraints = GatherRigConstraints(animator);
            m_Jobs = CreateAnimationJobs(animator, m_Constraints);
            m_IsInitialized = true;
        }

        public void Clear()
        {
            if (m_IsInitialized)
                DestroyAnimationJobs(m_Constraints, m_Jobs);

            m_IsInitialized = false;
        }

        public IRigConstraint[] constraints => m_IsInitialized ? m_Constraints : null;

        public IAnimationJob[] jobs => m_IsInitialized ? m_Jobs : null;

        public void UpdateConstraints(AnimationScriptPlayable[] playables, float weight)
        {
            if (!m_IsInitialized || playables == null || playables.Length != m_Constraints.Length)
                return;

            for (int i = 1; i < m_Constraints.Length; ++i)
            {
                var constraintWeight = m_Constraints[i].weight * weight;
                playables[i].SetInputWeight(0, constraintWeight);

                if (constraintWeight > 0f)
                    m_Constraints[i].UpdateJob(m_Jobs[i]);
            }
        }

        public IRigConstraint[] GatherRigConstraints(Animator animator)
        {
            IRigConstraint[] constraints = GetComponentsInChildren<IRigConstraint>();
            if (constraints.Length == 0)
                return null;

            List<IRigConstraint> tmp = new List<IRigConstraint>(constraints.Length + 1);
            SyncSceneToStream sceneToStream = new SyncSceneToStream();

            tmp.Add(sceneToStream);
            for (int i = 0; i != constraints.Length; ++i)
            {
                if (!constraints[i].IsValid())
                    continue;

                tmp.Add(constraints[i]);

                if (constraints[i].data is IRigReferenceSync)
                {
                    var references = ((IRigReferenceSync)constraints[i].data).allReferences;
                    for (int j = 0; j < references.Length; ++j)
                    {
                        if (references[j].transform.IsChildOf(animator.transform))
                            ((SyncSceneToStream.Data)sceneToStream.data).Add(references[j]);
                    }
                }
            }

            // If all constraints other than SyncSceneToStream were invalid then return null
            if (tmp.Count == 1 || !sceneToStream.IsValid())
                return null;

            return tmp.ToArray();
        }

        public static IAnimationJob[] CreateAnimationJobs(Animator animator, IRigConstraint[] constraints)
        {
            if (constraints == null || constraints.Length == 0)
                return null;

            IAnimationJob[] jobs = new IAnimationJob[constraints.Length];
            for (int i = 0; i < constraints.Length; ++i)
                jobs[i] = constraints[i].CreateJob(animator);

            return jobs;
        }

        public static void DestroyAnimationJobs(IRigConstraint[] constraints, IAnimationJob[] jobs)
        {
            if (jobs == null || jobs.Length != constraints.Length)
                return;

            for (int i = 0; i < constraints.Length; ++i)
                constraints[i].DestroyJob(jobs[i]);
        }

        // First rig constraint that is run in order to
        // feed the animation stream with scene values when needed
        internal class SyncSceneToStream : IRigConstraint
        {
            internal class Data : IAnimationJobData, ISyncSceneToStreamData
            {
                private List<Transform> m_Objects;
                private List<bool> m_Sync;

                static readonly SyncSceneToStreamJobBinder<Data> s_Binder =
                    new SyncSceneToStreamJobBinder<Data>();

                public Data()
                {
                    m_Objects = new List<Transform>();
                    m_Sync = new List<bool>();
                }

                public void Add(JobTransform jobTx)
                {
                    m_Objects.Add(jobTx.transform);
                    m_Sync.Add(jobTx.sync);
                }

                public void Clear()
                {
                    m_Objects.Clear();
                    m_Sync.Clear();
                }

                public Transform[] objects { get => m_Objects.ToArray(); }
                public bool[] sync { get => m_Sync.ToArray(); }
                public bool IsValid() => m_Objects.Count > 0 && m_Sync.Count == m_Objects.Count;
                public IAnimationJobBinder binder => s_Binder;
            }
            private Data m_Data;

            public SyncSceneToStream()
            {
                m_Data = new Data();
            }

            public IAnimationJobData data => m_Data;
            public float weight { set {} get => 1f; }
            public IAnimationJob CreateJob(Animator animator) => data.binder.Create(animator, data);
            public void DestroyJob(IAnimationJob job) => data.binder.Destroy(job);
            public void UpdateJob(IAnimationJob job) => data.binder.Update(data, job);
            public bool IsValid() => data.IsValid();
        }
    }
}
