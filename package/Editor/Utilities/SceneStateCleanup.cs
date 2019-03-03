using System;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.SceneManagement;
using System.Linq;

namespace UnityEditor.Build.Utilities
{
    public class SceneStateCleanup : IDisposable
    {
        SceneSetup[] m_Scenes;

        bool m_Disposed;

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
                if (!m_Scenes.IsNullOrEmpty() && m_Scenes.All(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(m_Scenes);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            }
        }
    }
}
