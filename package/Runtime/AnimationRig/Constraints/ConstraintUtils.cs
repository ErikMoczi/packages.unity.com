using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging.RuntimeConstraints
{
    public struct WeightCache
    {
        private bool dirty;
        private float[] cache;

        public float[] GetWeights<T>(List<T> weights) where T : IWeightProvider
        {
            if (dirty || cache == null)
            {
                ConstraintDataUtils.GetWeights(weights, ref cache);
                dirty = false;
            }
            return cache;
        }

        public void MarkDirty() { dirty = true; }
    }

    public static class ConstraintDataUtils
    {
        public static Transform[] GetTransforms<T>(List<T> transforms) where T : ITransformProvider
        {
            if (transforms.Count == 0)
                return null;

            Transform[] ret = new Transform[transforms.Count];
            for (int i = 0; i < transforms.Count; ++i)
                ret[i] = transforms[i].transform;

            return ret;
        }

        public static void GetWeights<T>(List<T> weights, ref float[] outWeights) where T : IWeightProvider
        {
            if (weights.Count == 0)
                outWeights = null;
            else
            {
                if (outWeights == null || weights.Count != outWeights.Length)
                    outWeights = new float[weights.Count];

                for (int i = 0; i < weights.Count; ++i)
                    outWeights[i] = weights[i].weight;
            }
        }
    }
}
