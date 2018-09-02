using System;
using UnityEditor.SceneManagement;

namespace UnityEditor.Build.Utilities
{
    public class SceneStateCleanup : IDisposable
    {
        SceneSetup[] m_Scenes;

        public SceneStateCleanup()
        {
            m_Scenes = EditorSceneManager.GetSceneManagerSetup();
        }

        public void Dispose()
        {
            if (!m_Scenes.IsNullOrEmpty())
                EditorSceneManager.RestoreSceneManagerSetup(m_Scenes);
            else
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
        }
    }
}
