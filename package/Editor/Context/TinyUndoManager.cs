

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    [ContextManager(ContextUsage.Edit)]
    internal class TinyUndoManager : ContextManager, IUndoManager
    {
        private class TinyUndoObject : ScriptableObject
        {
            private const string k_UndoName = TinyConstants.ApplicationName + " Operation";
            private int m_Current = 0;

            [SerializeField]
            private int m_Version = 0;

            public delegate void UndoHandler();
            public delegate void RedoHandler();

            public UndoHandler OnUndo;
            public UndoHandler OnRedo;
            
            public int Version => m_Version;

            public void IncrementVersion()
            {
                Undo.RecordObject(this, k_UndoName);
                m_Version++;
                m_Current = m_Version;
                EditorUtility.SetDirty(this);
            }

            private void OnEnable()
            {
                Undo.undoRedoPerformed += HandleUndoRedoPerformed;
            }

            private void OnDisable()
            {
                Undo.undoRedoPerformed -= HandleUndoRedoPerformed;
            }
            
            public void Flush()
            {
                Undo.FlushUndoRecordObjects();
            }

            private void HandleUndoRedoPerformed()
            {
                if (m_Current != m_Version)
                {
                    if (m_Current > m_Version)
                    {
                        OnUndo?.Invoke();
                    }
                    else
                    {
                        OnRedo?.Invoke();
                    }

                    m_Current = m_Version;
                }
            }
        }

        public class ChangeComparer : IEqualityComparer<Change>
        {
            public bool Equals(Change x, Change y)
            {
                return x.Id.Equals(y.Id);
            }

            public int GetHashCode(Change obj)
            {
                return obj.Id.GetHashCode();
            }
        }

        public event Action OnBeginUndo;
        public event Action OnEndUndo;
        
        public event Action OnBeginRedo;
        public event Action OnEndRedo;
        
        public event UndoPerformed OnUndoPerformed;
        public event RedoPerformed OnRedoPerformed;

        private readonly List<HashSet<Change>> m_UndoableChanges;
        private readonly List<HashSet<Change>> m_RedoableChanges;
        private readonly HashSet<Change> m_FrameChanges;

        private readonly TinyUndoObject m_Undo;
        private readonly TinyCaretaker m_Caretaker;
        private IBindingsManager m_Bindings { get; set; }
        private int m_CurrentIndex;

        public TinyUndoManager(TinyContext context)
            :base(context)
        {
            m_Caretaker = context.Caretaker;
            m_Caretaker.OnGenerateMemento += HandleUndoableChange;
            m_Undo = ScriptableObject.CreateInstance<TinyUndoObject>();
            m_Undo.hideFlags |= HideFlags.HideAndDontSave;
            AddCallbacks();
            m_UndoableChanges = new List<HashSet<Change>>();
            m_RedoableChanges = new List<HashSet<Change>>();
            m_FrameChanges = new HashSet<Change>();
            AssemblyReloadEvents.beforeAssemblyReload += Unload;
        }

        public override void Load()
        {
            Bridge.EditorApplication.RegisterContextualUpdate(Update);
            m_Bindings = Context.GetManager<IBindingsManager>();
        }

        private void AddCallbacks()
        {
            m_Undo.OnUndo += HandleUndoOperation;
            m_Undo.OnRedo += HandleRedoOperation;
        }

        private void RemoveCallbacks()
        {
            m_Undo.OnUndo -= HandleUndoOperation;
            m_Undo.OnRedo -= HandleRedoOperation;
        }

        private void HandleRedoOperation()
        {
            using (new GameObjectTracker.DontTrackScope())
            {
                m_Bindings.SetAllDirty();
                int version = int.MaxValue;
                
                try
                {
                    do
                    {
                        if (m_RedoableChanges.Count > 0)
                        {
                            //Debug.Log("Redo operation");
                            var changes = m_RedoableChanges.Last();
                            foreach (var change in changes)
                            {
                                var originator = Registry.FindById<IRegistryObject>(change.Id) as IOriginator;
                                version = change.Version;
                                if (null == originator)
                                {
                                    originator = change.RegistryObject as IOriginator;
                                }

                                // We have no next version, which means that the originator must have been created.
                                // Knowing this, we shall remove it from the registry.
                                if (null == change.NextVersion)
                                {
                                    // Dont unregister if this is a persistent object
                                    // @NOTE There is one case missing where the persistentId is not null but the asset is actually gone. (i.e. The user deleted the meta) this would cause unexpected issues
                                    var persistentObject = originator as IPersistentObject;
                                    if (null == persistentObject || string.IsNullOrEmpty(persistentObject.PersistenceId))
                                    {
                                        Registry.Unregister(originator as IRegistryObject);
                                    }
                                }
                                // Otherwise, restore the next version.
                                else
                                {
                                    originator.Restore(change.NextVersion);
                                }
                            }

                            m_RedoableChanges.RemoveAt(m_RedoableChanges.Count - 1);
                            m_UndoableChanges.Add(changes);
                        }
                        else
                        {
                            break;
                        }
                    } while (version < m_Undo.Version - 1);
                }
                finally
                {
                    RefreshAll();
                    OnRedoPerformed?.Invoke(m_UndoableChanges.Last());
                    m_Caretaker.OnGenerateMemento -= HandleUndoableChange;
                    OnBeginRedo?.Invoke();
                    using (Serialization.SerializationContext.Scope(Serialization.SerializationContext.UndoRedo))
                    {
                        m_Caretaker.Update();
                    }
                    Registry.ClearUnregisteredObjects();
                    m_Caretaker.OnGenerateMemento += HandleUndoableChange;
                    OnEndRedo?.Invoke();
                    m_Bindings.TransferAll();
                }
            }
        }

        private void HandleUndoOperation()
        {
            // Workaround for a very strange issue i've encountering with prefabs
            // It seems that when we call `Undo.RecordObject` during a DragDrop operation
            // Unity will invoke the Undo callback...
            // This is a quick patch to make sure we are not dragging
            if (null != DragAndDrop.objectReferences && DragAndDrop.objectReferences.Length > 0)
            {
                return;
            }

            using (new GameObjectTracker.DontTrackScope())
            {
                m_Bindings.SetAllDirty();
                int version = -1;

                // 0 is basically you loaded the project
                var hasChanges = m_UndoableChanges.Count > 1;

                try
                {
                    do
                    {
                        if (m_UndoableChanges.Count > 1)
                        {
                            //Debug.Log("Undo operation");
                            var changes = m_UndoableChanges.Last();
                            foreach (var change in changes)
                            {
                                var originator = Registry.FindById<IRegistryObject>(change.Id) as IOriginator;
                                version = change.Version;
                                if (null == originator)
                                {
                                    originator = change.RegistryObject as IOriginator;
                                }

                                // We have no previous version, which means that the originator must have been created.
                                // Knowing this, we shall remove it from the registry.
                                if (null == change.PreviousVersion)
                                {
                                    // Dont unregister if this is a persistent object
                                    // @NOTE There is one case missing where the persistentId is not null but the asset is actually gone. (i.e. The user deleted the meta) this would cause unexpected issues
                                    var persistentObject = originator as IPersistentObject;
                                    if (null == persistentObject || string.IsNullOrEmpty(persistentObject.PersistenceId))
                                    {
                                        Registry.Unregister(originator as IRegistryObject);
                                    }
                                }
                                // Otherwise, restore the previous version.
                                else
                                {
                                    originator.Restore(change.PreviousVersion);
                                }
                            }

                            m_UndoableChanges.RemoveAt(m_UndoableChanges.Count - 1);
                            m_RedoableChanges.Add(changes);
                        }
                        else
                        {
                            break;
                        }
                    } while (version > m_Undo.Version);
                }
                finally
                {
                    RefreshAll();
                    if (hasChanges)
                    {
                        OnUndoPerformed?.Invoke(m_RedoableChanges.Last());
                    }

                    m_Caretaker.OnGenerateMemento -= HandleUndoableChange;
                    OnBeginUndo?.Invoke();
                    
                    using (Serialization.SerializationContext.Scope(Serialization.SerializationContext.UndoRedo))
                    {
                        m_Caretaker.Update();
                    }
                    
                    Registry.ClearUnregisteredObjects();
                    m_Caretaker.OnGenerateMemento += HandleUndoableChange;
                    OnEndUndo?.Invoke();
                    m_Bindings.TransferAll();
                }
            }
        }

        public void RefreshAll()
        {
            if (!m_Caretaker.HasChanges)
            {
                return;
            }

            foreach (var type in Registry.FindAllByType<Tiny.TinyType>())
            {
                type.Refresh();
            }

            foreach (var entity in Registry.FindAllByType<Tiny.TinyEntity>())
            {
                foreach (var component in entity.Components)
                {
                    component.Refresh();
                }
            }
        }

        public void Update()
        {
            if (!m_Undo || null == m_Undo)
            {
                return;
            }

            RefreshAll();

            m_FrameChanges.Clear();
            
            using (Serialization.SerializationContext.Scope(Serialization.SerializationContext.UndoRedo))
            {
                m_Caretaker.Update();
            }

            // Handle deleted objects
            var pooled = ListPool<IRegistryObject>.Get();
            try
            {
                pooled.AddRange(Registry.AllUnregistered());
                for (int i = 0; i < pooled.Count; ++i)
                {
                    var unregistered = pooled[i];
                    m_FrameChanges.Add(new Change { Id = unregistered.Id, Version = m_Undo.Version, RegistryObject = unregistered, NextVersion = null, PreviousVersion = GetPreviousValue(unregistered) });
                }
            }
            finally
            {
                ListPool<IRegistryObject>.Release(pooled);
            }
            Registry.ClearUnregisteredObjects();

            if (m_FrameChanges.Count > 0)
            {
                if (TryMergeChanges())
                {
                    return;
                }

                //Debug.Log("UndoRedoable operation registered");
                // We didn't / couldn't merge the changes, push a new changeset.
                m_UndoableChanges.Add(new HashSet<Change>(m_FrameChanges, new ChangeComparer()));
                m_Undo.IncrementVersion();
                m_RedoableChanges.Clear();
                ++m_CurrentIndex;
            }
        }

        private bool TryMergeChanges()
        {
            // [MP] @TODO: If the target(s) of the current changeset is/are the same as the last changeset and we are inside a change delta time,
            //             merge the changes together instead of pushing a new changeset.

            return false;
        }

        public void HandleUndoableChange(IOriginator originator, IMemento memento)
        {
            var change = new Change { Id = originator.Id, Version = m_Undo.Version, RegistryObject = originator as IRegistryObject, NextVersion = memento, PreviousVersion = GetPreviousValue(originator) };
            m_FrameChanges.Remove(change);
            m_FrameChanges.Add(change);
        }

        /// <summary>
        /// When flushing an originator, we will push the current state at the index 0 (pretty much the initial state) and then remove it from
        /// the undo/redo stack.
        /// </summary>
        public void FlushChanges(params IOriginator[] originators)
        {
            FlushChanges((IEnumerable <IOriginator>) originators);
        }

        public void FlushChanges(IEnumerable<IOriginator> originators)
        {
            if (null == m_Undo)
            {
                return; 
            }
            
            foreach(var originator in originators)
            {
                using (Serialization.SerializationContext.Scope(Serialization.SerializationContext.UndoRedo))
                {
                    m_UndoableChanges[0].Add(new Change { Id = originator.Id, Version = m_Undo.Version, RegistryObject = originator as IRegistryObject, NextVersion = originator.Save(), PreviousVersion = null });
                }

                foreach(var changeSet in m_RedoableChanges)
                {
                    changeSet.RemoveWhere(change => change.Id.Equals(originator.Id));
                }

                for(int i = 1; i < m_UndoableChanges.Count; ++i)
                {
                    var changeSet = m_UndoableChanges[i];
                    changeSet.RemoveWhere(change => change.Id.Equals(originator.Id));
                }
            }
        }

        public override void Unload()
        {
            Bridge.EditorApplication.UnregisterContextualUpdate(Update);
            m_Undo.Flush();
            RemoveCallbacks();
            Object.DestroyImmediate(m_Undo, false);
        }

        private IMemento GetPreviousValue(IIdentified<TinyId> originator)
        {
            // We skip the current changes
            for (int i = m_UndoableChanges.Count - 1; i >= 0; --i)
            {
                var changeset = m_UndoableChanges[i];
                var previous = default(Change);

                foreach (var cs in changeset)
                {
                    if (cs.Id == originator.Id)
                    {
                        previous = cs;
                        break;
                    }
                }

                if (previous.Equals(default(Change)))
                {
                    continue;
                }

                return previous.NextVersion;
            }

            return null;
        }
    }
}

