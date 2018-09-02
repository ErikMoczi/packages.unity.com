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

            if (disposing)
            {
                if (!m_Scenes.IsNullOrEmpty())
                    EditorSceneManager.RestoreSceneManagerSetup(m_Scenes);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            }

            m_Disposed = true;
        }
    }
}
