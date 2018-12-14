
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    [System.Flags]
    internal enum EditorContextType
    {
        /// <summary>
        /// No project is open
        /// </summary>
        None = 0,
        
        /// <summary>
        /// The editor is setup to work on a user project
        /// </summary>
        Project = 1,
        
        /// <summary>
        /// The editor is setup to work on standalone modules
        /// </summary>
        Module = 2
    }

    internal class TinyEditorContext
    {
        public readonly TinyContext Context;
        private TinyProject.Reference m_Project;

        public TinyProject Project => m_Project.Dereference(Registry);
        public TinyModule Module => m_Project.Dereference(Registry)?.Module.Dereference(Registry);
        public TinyRegistry Registry => Context.Registry;
        public TinyCaretaker Caretaker => Context.Caretaker;
        public TinyVersionStorage VersionStorage => Context.VersionStorage;
        public TinyEditorWorkspace Workspace { get; }
        
        internal EditorContextType ContextType { get; }

        public IEnumerable<IPersistentObject> GetPersistentObjects()
        {
            Assert.IsNotNull(Registry);
            return GetPersistentObjectsImpl();
        }

        private IEnumerable<IPersistentObject> GetPersistentObjectsImpl()
        {
            yield return ContextType == EditorContextType.Project 
                ? Project 
                : (IPersistentObject) Module;
            
            foreach (var e in Module.EntityGroups)
            {
                var o = e.Dereference(Registry);
                if (null == o) continue;
                yield return o;
            }
            
            foreach (var e in Module.Types)
            {
                var o = e.Dereference(Registry);
                if (null == o) continue;
                yield return o;
            }
        }

        public TinyEditorContext(TinyProject.Reference project, EditorContextType type, TinyContext context, TinyEditorWorkspace workspace)
        {
            m_Project = project;
            ContextType = type;
            Context = context ?? new TinyContext(ContextUsage.Edit);
            Workspace = workspace ?? new TinyEditorWorkspace();
        }
        
        internal void Load()
        {
            Context.LoadManagers();
        }
        
        internal void Unload()
        {
            Context.UnloadManagers();
            Bridge.EditorApplication.ClearContextualUpdates();
        }
    }
}


