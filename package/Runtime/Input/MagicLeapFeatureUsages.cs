using UnityEngine.XR;

namespace UnityEngine.Experimental.XR.MagicLeap
{
    public static class MagicLeapHeadUsages
    {
        public static InputFeatureUsage<float> confidence = new InputFeatureUsage<float>("MLHeadConfidence");
    }
}