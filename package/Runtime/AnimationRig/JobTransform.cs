namespace UnityEngine.Animations.Rigging
{
    public interface ITransformProvider
    {
        Transform transform { get; set; }
    }

    public interface IWeightProvider
    {
        float weight { get; set; }
    }

    [System.Serializable]
    public class JobTransform : ITransformProvider
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

        Transform ITransformProvider.transform { get => transform; set => transform = value; }
    }

    [System.Serializable]
    public class WeightedJobTransform : JobTransform, IWeightProvider
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

        float IWeightProvider.weight { get => weight; set => weight = value; }
    }
}