using UnityEditor;

namespace Unity.Tiny.Bridge
{
    internal static class SceneView
    {
        public static void SetTinyMode()
        {
            foreach (UnityEditor.SceneView sceneView in UnityEditor.SceneView.sceneViews)
            {
                sceneView.in2DMode = true;
                sceneView.sceneLighting = false;
                sceneView.isRotationLocked = true;
            }
        }
    }
}