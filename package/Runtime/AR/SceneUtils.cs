namespace UnityEngine.XR.ARFoundation
{
    internal static class SceneUtils
    {
        public static void EnsureARSessionExists()
        {
            var sessions = UnityEngine.Object.FindObjectsOfType<ARSession>();
            if (sessions.Length == 0)
            {
                new GameObject("AR Session").AddComponent<ARSession>();
            }
        }
    }
}
