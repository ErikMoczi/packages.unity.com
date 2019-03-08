using System;
using UnityEditor;

namespace Unity.Tiny.Test
{
    internal static class FlowTestHelper
    {
        internal class ProjectContext : PersistentObjectContext
        {
            public ProjectContext(string path = "Assets/TestProject") : base(path) { }
            
            protected override void CreatePersistentObject(string path)
            {
                TinyEditorApplication.NewProject(path);
            }
        }
        
        internal class ModuleContext : PersistentObjectContext
        {
            public ModuleContext(string path = "Assets/TestModule") : base(path) { }

            protected override void CreatePersistentObject(string path)
            {
                TinyEditorApplication.NewModule(path);
            }
        }
        
        internal abstract class PersistentObjectContext : IDisposable
        {
            private string m_Path;
            protected PersistentObjectContext(string path)
            {
                m_Path = path;
                CreatePersistentObject(m_Path);
            }

            public void Dispose() {
                TinyEditorApplication.Close();
                
                const string separator = "/";
                const int firstSeparatorKnownPosition = 8;
                var index = m_Path.IndexOf(separator, firstSeparatorKnownPosition, StringComparison.Ordinal);
                if (index != -1)
                {
                    AssetDatabase.DeleteAsset(m_Path.Substring(0, index));
                }
                else
                {
                    AssetDatabase.DeleteAsset(m_Path);
                }
            }
            
            protected abstract void CreatePersistentObject(string path);
        }
        
       
    }
}