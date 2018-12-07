using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    using Experimental.Animations;

    public static class RigUtils
    {
        public static IRigConstraint[] GetConstraints(Rig rig)
        {
            IRigConstraint[] constraints = rig.GetComponentsInChildren<IRigConstraint>();
            if (constraints.Length == 0)
                return null;

            List<IRigConstraint> tmp = new List<IRigConstraint>(constraints.Length);
            foreach (var constraint in constraints)
            {
                if (constraint.IsValid())
                    tmp.Add(constraint);
            }

            return tmp.Count == 0 ? null : tmp.ToArray();
        }

        public static JobTransform[] GetAllConstraintReferences(Animator animator, IRigConstraint[] constraints)
        {
            if (constraints == null || constraints.Length == 0)
                return null;

            List<JobTransform> allReferences = new List<JobTransform>(constraints.Length);
            foreach (var constraint in constraints)
            {
                var data = constraint.data;
                if (!(data is IRigReferenceSync))
                    continue;

                var references = ((IRigReferenceSync)data).allReferences;
                foreach (var reference in references)
                {
                    if (reference.transform.IsChildOf(animator.transform))
                        allReferences.Add(reference);
                }
            }

            return allReferences.Count == 0 ? null : allReferences.ToArray();
        }

        public static Dictionary<int, List<int>> BuildUniqueReferenceMap(JobTransform[] references)
        {
            if (references == null || references.Length == 0)
                return null;

            Dictionary<int, List<int>> uniqueReferences = new Dictionary<int, List<int>>();
            for (int i = 0; i < references.Length; ++i)
            {
                int key = references[i].transform.GetInstanceID();
                if (!uniqueReferences.TryGetValue(key, out List<int> indices))
                {
                    indices = new List<int>();
                    uniqueReferences[key] = indices;
                }
                indices.Add(i);
            }

            return uniqueReferences;
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

        private struct SyncSceneToStreamData : IAnimationJobData, ISyncSceneToStreamData
        {
            public SyncSceneToStreamData(JobTransform[] references)
            {
                if (references == null || references.Length == 0)
                {
                    objects = null;
                    sync = null;
                    return;
                }

                var uniqueReferences = BuildUniqueReferenceMap(references);
                var keys = uniqueReferences.Keys;

                objects = new Transform[keys.Count];
                sync = new bool[keys.Count];

                int index = 0;
                foreach (var key in keys)
                {
                    var values = uniqueReferences[key];
                    objects[index] = references[values[0]].transform;

                    bool state = false;
                    foreach (var val in values)
                    {
                        if ((state |= references[val].sync))
                            break;
                    }

                    sync[index] = state;
                    ++index;
                }
            }

            public Transform[] objects { get; private set; }
            public bool[] sync { get; private set; }

            bool IAnimationJobData.IsValid() => objects != null && objects.Length > 0 && sync.Length == objects.Length;

            void IAnimationJobData.SetDefaultValues()
            {
                sync = null;
                objects = null;
            }
        }

        public static IAnimationJobData CreateSyncSceneToStreamData(JobTransform[] references)
        {
            return new SyncSceneToStreamData(references);
        }

        public static IAnimationJobBinder syncSceneToStreamBinder { get; } = new SyncSceneToStreamJobBinder<SyncSceneToStreamData>();
    }
}