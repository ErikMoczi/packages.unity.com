

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    /// <summary>
    /// Shared version storage
    /// </summary>
    internal sealed class TinyVersionStorage : IVersionStorage
    {
        private class DontTrackChange : IDisposable
        {
            private readonly TinyVersionStorage m_Storage;
            private readonly bool m_Value;

            public DontTrackChange(TinyVersionStorage storage)
            {
                m_Storage = storage;
                m_Value = m_Storage.m_DontTrackChange;
                m_Storage.m_DontTrackChange = true;
            }

            public void Dispose()
            {
                m_Storage.m_DontTrackChange = m_Value;
            }
        }
        
        private bool m_DontTrackChange;
        
        private readonly HashSet<IPropertyContainer> m_ChangeSet = new HashSet<IPropertyContainer>();
        private readonly List<IPropertyContainer> m_ChangeList = new List<IPropertyContainer>();

        public IReadOnlyList<IPropertyContainer> Changed => m_ChangeList.AsReadOnly();

        public void IncrementVersion<TContainer>(IProperty property, TContainer container) 
            where TContainer : IPropertyContainer
        {
            if (m_DontTrackChange)
            {
                return;
            }
            
            // Assert.IsTrue(container is IRegistryObject, $"{UTinyConstants.ApplicationName}: VersionStorage.IncrementVersion should only be called with a IRegistryObject container. Actual type is { container.GetType() }");

            if (m_ChangeSet.Add(container))
            {
                m_ChangeList.Add(container);
            }
        }

        public void MarkAsChanged(IPropertyContainer container)
        {
            if (m_DontTrackChange)
            {
                return;
            }

            if (m_ChangeSet.Add(container))
            {
                m_ChangeList.Add(container);
            }
            
        }

        public void ResetChanged()
        {
            m_ChangeSet.Clear();
            m_ChangeList.Clear();
        }

        internal IDisposable DontTrackChangeScopeInternal()
        {
            return new DontTrackChange(this);
        }
    }
}

