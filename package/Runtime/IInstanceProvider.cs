using System.Collections.Generic;
using UnityEngine;

namespace ResourceManagement
{
    public struct InstantiationParams
    {
        public Vector3 m_position;
        public Quaternion m_rotation;
        public Transform m_parent;                
        public bool m_instantiateInWorldPosition;
        public bool m_setPositionRotation;
        public InstantiationParams(Transform parent, bool instantiateInWorldSpace)
        {
            m_position = Vector3.zero;
            m_rotation = Quaternion.identity;
            m_parent = parent;
            m_instantiateInWorldPosition = instantiateInWorldSpace;
            m_setPositionRotation = false;
        }
        public InstantiationParams(Vector3 pos, Quaternion rot, Transform parent)
        {
            m_position = pos;
            m_rotation = rot;
            m_parent = parent;
            m_instantiateInWorldPosition = false;
            m_setPositionRotation = true;
        }

        public TObject Instantiate<TObject>(TObject source) where TObject : Object
        {
            TObject result;
            if (m_parent == null)
            {
                if (m_setPositionRotation)
                    result = Object.Instantiate(source, m_position, m_rotation);
                else
                    result = Object.Instantiate(source);
            }
            else
            {
                if (m_setPositionRotation)
                    result = Object.Instantiate(source, m_position, m_rotation, m_parent);
                else
                    result = Object.Instantiate(source, m_parent, m_instantiateInWorldPosition);
            }
            return result;
        }
    }

    public interface IInstanceProvider
    {
        /// <summary>
        /// Determind whether or not this provider can provide for the given <paramref name="loadProvider"/> and <paramref name="location"/>
        /// </summary>
        /// <returns><c>true</c>, if provide instance was caned, <c>false</c> otherwise.</returns>
        /// <param name="loadProvider">Provider used to load the object prefab.</param>
        /// <param name="location">Location to instantiate.</param>
        /// <typeparam name="TObject">Object type.</typeparam>
        bool CanProvideInstance<TObject>(IResourceProvider loadProvider, IResourceLocation location)
        where TObject : UnityEngine.Object;

        /// <summary>
        /// Asynchronously nstantiate the given <paramref name="location"/>
        /// </summary>
        /// <returns>An async operation.</returns>
        /// <param name="loadProvider">Provider used to load the object prefab.</param>
        /// <param name="location">Location to instantiate.</param>
        /// <param name="loadDependencyOperation">Async operation for dependency loading.</param>
        /// <typeparam name="TObject">Instantiated object type.</typeparam>
        IAsyncOperation<TObject> ProvideInstanceAsync<TObject>(IResourceProvider loadProvider, IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, InstantiationParams instParams)
        where TObject : UnityEngine.Object;

        /// <summary>
        /// Releases the instance.
        /// </summary>
        /// <returns><c>true</c>, if instance was released, <c>false</c> otherwise.</returns>
        /// <param name="loadProvider">Provider used to load the object prefab.</param>
        /// <param name="location">Location to release.</param>
        /// <param name="instance">Object instance to release.</param>
        bool ReleaseInstance(IResourceProvider loadProvider, IResourceLocation location, UnityEngine.Object instance);
    }
}
