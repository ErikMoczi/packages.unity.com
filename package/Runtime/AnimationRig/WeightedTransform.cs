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
    public class WeightedTransform : ITransformProvider, IWeightProvider
    {
        public Transform transform;

        [Range(0f, 1f)]
        public float weight;

        public WeightedTransform(Transform transform, float weight)
        {
            this.transform = transform;
            this.weight = Mathf.Clamp01(weight);
        }

        public static WeightedTransform Default(float weight) => new WeightedTransform(null, weight);

        Transform ITransformProvider.transform { get => transform; set => transform = value; }
        float IWeightProvider.weight { get => weight; set => weight = Mathf.Clamp01(value); }
    }
}