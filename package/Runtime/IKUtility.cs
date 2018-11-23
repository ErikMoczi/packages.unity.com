namespace UnityEngine.Experimental.U2D.IK
{
    public class IKUtility
    {
        public static bool IsDescendentOf(Transform transform, Transform ancestor)
        {
            Debug.Assert(transform != null, "Transform is null");

            Transform currentParent = transform.parent;

            while (currentParent)
            {
                if (currentParent == ancestor)
                    return true;

                currentParent = currentParent.parent;
            }

            return false;
        }

        public static int GetAncestorCount(Transform transform)
        {
            Debug.Assert(transform != null, "Transform is null");

            int ancestorCount = 0;

            while (transform.parent)
            {
                ++ancestorCount;

                transform = transform.parent;
            }

            return ancestorCount;
        }

        public static int GetMaxChainCount(IKChain2D chain)
        {
            int maxChainCount = 0;

            if (chain.effector)
                maxChainCount = GetAncestorCount(chain.effector) + 1;

            return maxChainCount;
        }
    }
}
