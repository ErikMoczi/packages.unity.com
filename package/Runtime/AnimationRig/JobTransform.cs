namespace UnityEngine.Animations.Rigging
{
    [System.Serializable]
    public class JobTransform
    {
        public Transform transform;
        public bool sync;

        public JobTransform(Transform transform, bool sync)
        {
            this.transform = transform;
            this.sync = sync;
        }

        public static JobTransform defaultSync => new JobTransform(null, true);
        public static JobTransform defaultNoSync => new JobTransform(null, false);
    }

    [System.Serializable]
    public class WeightedJobTransform : JobTransform
    {
        [Range(0f, 1f)]
        public float weight;

        public WeightedJobTransform(Transform transform, bool sync, float weight)
            : base(transform, sync)
        {
            this.weight = Mathf.Clamp01(weight);
        }

        public WeightedJobTransform(JobTransform jobTransform, float weight)
            : base(jobTransform.transform, jobTransform.sync)
        {
            this.weight = Mathf.Clamp01(weight);
        }

        public new static WeightedJobTransform defaultSync(float w) =>
            new WeightedJobTransform(JobTransform.defaultSync, w);

        public new static WeightedJobTransform defaultNoSync(float w) =>
            new WeightedJobTransform(JobTransform.defaultNoSync, w);
    }
}