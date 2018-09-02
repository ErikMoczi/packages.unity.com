using System;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.SceneManagement;

namespace UnityEditor.Build.Utilities
{
    public class SceneStateCleanup : IDisposable
    {
        SceneSetup[] m_Scenes;

        bool m_Disposed = false;

        public SceneStateCleanup()
        {
            m_Scenes = EditorSceneManager.GetSceneManagerSetup();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            m_Disposed = true;

            if (disposing)
            {
                if (!m_Scenes.IsNullOrEmpty())
                {
                    SceneSetup[] current = EditorSceneManager.GetSceneManagerSetup();
                    bool scenesChanged = false;
                    if (current.Length == m_Scenes.Length)
                    {
                        for (int i = 0; i < current.Length; i++)
                            scenesChanged |= current[i].isActive != m_Scenes[i].isActive || current[i].isLoaded != m_Scenes[i].isLoaded || current[i].path != m_Scenes[i].path;
                    }
                    if (scenesChanged)
                        EditorSceneManager.RestoreSceneManagerSetup(m_Scenes);
                }
                else
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            }
        }
    }
}
