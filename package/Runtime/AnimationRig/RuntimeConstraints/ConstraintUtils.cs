using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging.RuntimeConstraints
{
    public struct WeightCache
    {
        private bool dirty;
        private float[] cache;

        public float[] GetWeights(List<WeightedJobTransform> jobTransforms)
        {
            if (dirty || cache == null)
            {
                ConstraintDataUtils.GetWeights(jobTransforms, ref cache);
                dirty = false;
            }
            return cache;
        }

        public void MarkDirty() { dirty = true; }
    }

    public static class ConstraintDataUtils
    {
        public static Transform[] GetTransforms(List<JobTransform> jobTransforms)
        {
            if (jobTransforms.Count == 0)
                return null;

            Transform[] ret = new Transform[jobTransforms.Count];
            for (int i = 0; i < jobTransforms.Count; ++i)
                ret[i] = jobTransforms[i].transform;

            return ret;
        }

        public static Transform[] GetTransforms(List<WeightedJobTransform> jobTransforms)
        {
            if (jobTransforms.Count == 0)
                return null;

            Transform[] ret = new Transform[jobTransforms.Count];
            for (int i = 0; i < jobTransforms.Count; ++i)
                ret[i] = jobTransforms[i].transform;

            return ret;
        }

        public static void GetWeights(List<WeightedJobTransform> jobTransforms, ref float[] outWeights)
        {
            if (jobTransforms.Count == 0)
                outWeights = null;
            else
            {
                if (outWeights == null || jobTransforms.Count != outWeights.Length)
                    outWeights = new float[jobTransforms.Count];

                for (int i = 0; i < jobTransforms.Count; ++i)
                    outWeights[i] = jobTransforms[i].weight;
            }
        }
    }
}
