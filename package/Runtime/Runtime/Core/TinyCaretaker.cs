

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine;

namespace Unity.Tiny
{
    /// <summary>
    /// Originator, this interface is implemented by any objects that require state save/restore functionality
    /// </summary>
    internal interface IOriginator : IVersioned, IIdentified<TinyId>
    {
        /// <summary>
        /// Saves the internal state of the object and returns a memento object
        /// </summary>
        /// <returns>Current state as a memento</returns>
        IMemento Save();

        /// <summary>
        /// Restores the internal object state based on the given memento
        /// </summary>
        /// <param name="memento">Previous state as a memento</param>
        void Restore(IMemento memento);
    }

    /// <summary>
    /// Memento interface. The structure is internal the the object itself
    /// </summary>
    internal interface IMemento
    {
        int Version { get; }
    }

    public interface ICaretaker
    {
        void Update();
        bool HasObjectChanged(IPropertyContainer @object);
    }

    internal delegate void CaretakerEventHandler(IOriginator originator, IMemento memento);
    internal delegate void CaretakerUpdateEventHandler();

    internal sealed class TinyCaretaker
    {
        private readonly TinyVersionStorage m_VersionStorage;
        
        /// <summary>
        /// Tracked versions for ALL objects
        /// </summary>
        private readonly Dictionary<TinyId, int> m_BaselineVersion = new Dictionary<TinyId, int>();

        public bool HasChanges => m_VersionStorage.Changed.Any();
        
        /// <summary>
        /// Invoked just before the memento is generated
        /// This callback can be used to make modifications to the object before memento generation
        /// </summary>
        public event Action<IOriginator> OnWillGenerateMemento;
        
        /// <summary>
        /// Called once with the generated memento
        /// This callback can be used to persist the generated memento
        /// </summary>
        public event Action<IOriginator, IMemento> OnGenerateMemento;
        
        public event CaretakerUpdateEventHandler OnBeginUpdate;
        public event CaretakerUpdateEventHandler OnEndUpdate;

        public TinyCaretaker(TinyVersionStorage versionStorage)
        {
            m_VersionStorage = versionStorage;
        }

        public void Update()
        {
            OnBeginUpdate?.Invoke();
            Update(m_VersionStorage.Changed);
            m_VersionStorage.ResetChanged();
            OnEndUpdate?.Invoke();
        }

        public void SetBaselineVersion(TinyId id, int version)
        {
            m_BaselineVersion[id] = version;
        }

        public bool HasObjectChanged(IPropertyContainer @object)
        {
            return m_VersionStorage.Changed.Contains(@object);
        }

        private void Update(IReadOnlyList<IPropertyContainer> objects)
        {
            if (null != OnWillGenerateMemento)
            {
                for (var i = 0; i < objects.Count; i++)
                {
                    var registryObject = objects[i] as IRegistryObject;

                    if (!(registryObject is IOriginator originator) || registryObject.Registry == null)
                    {
                        continue;
                    }

                    var id = originator.Id;

                    if (!m_BaselineVersion.TryGetValue(id, out var version) || version == originator.Version)
                    {
                        continue;
                    }
                
                    OnWillGenerateMemento?.Invoke(originator);
                }
            }

            for (var i = 0; i < objects.Count; i++)
            {
                var registryObject = objects[i] as IRegistryObject;

                if (!(objects[i] is IOriginator originator) || null == registryObject?.Registry)
                {
                    continue;
                }

                var id = originator.Id;

                if (!m_BaselineVersion.TryGetValue(id, out var baselineVersion))
                {
                    m_BaselineVersion.Add(id, originator.Version);
                }

                if (baselineVersion == originator.Version || OnGenerateMemento == null)
                {
                    continue;
                }

                var memento = originator.Save();
                OnGenerateMemento.Invoke(originator, memento);
            }
        }
    }
}

