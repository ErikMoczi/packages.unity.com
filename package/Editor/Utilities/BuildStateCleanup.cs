using System;
using System.IO;
using UnityEditor.SceneManagement;

namespace UnityEditor.Build.Utilities
{
    public class BuildStateCleanup : IDisposable
    {
        private bool m_SceneBackup;
        private string m_TempPath;

        private SceneSetup[] m_Scenes;

        public BuildStateCleanup(bool sceneBackupAndRestore, string tempBuildPath)
        {
            m_SceneBackup = sceneBackupAndRestore;
            if (m_SceneBackup)
                m_Scenes = EditorSceneManager.GetSceneManagerSetup();

            m_TempPath = tempBuildPath;
            Directory.CreateDirectory(m_TempPath);
        }

        public void Dispose()
        {
            if (m_SceneBackup)
            {
                if (!m_Scenes.IsNullOrEmpty())
                    EditorSceneManager.RestoreSceneManagerSetup(m_Scenes);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            }

            if (Directory.Exists(m_TempPath))
                Directory.Delete(m_TempPath, true);
        }
    }
}