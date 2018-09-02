using System;
using System.IO;

namespace UnityEditor.Build.Utilities
{
    public class BuildStateCleanup : IDisposable
    {
        string m_TempPath;

        public BuildStateCleanup(string tempBuildPath)
        {
            m_TempPath = tempBuildPath;
            Directory.CreateDirectory(m_TempPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(m_TempPath))
                Directory.Delete(m_TempPath, true);
        }
    }
}