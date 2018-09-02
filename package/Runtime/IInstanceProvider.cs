using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public struct InstantiationParameters
    {
        private Vector3 m_position;
        private Quaternion m_rotation;
        private Transform m_parent;
        private bool m_instantiateInWorldPosition;
        private bool m_setPositionRotation;

        public Vector3 Position { get { return m_position; } }
        public Quaternion Rotation { get { return m_rotation; } }
        public Transform Parent { get { return m_parent; } }
        public bool InstantiateInWorldPosition { get { return m_instantiateInWorldPosition; } }
        public bool SetPositionRotation { get { return m_setPositionRotation; } }

        public InstantiationParameters(Transform parent, bool instantiateInWorldSpace)
        {
            m_position = Vector3.zero;
            m_rotation = Quaternion.identity;
            m_parent = parent;
            m_instantiateInWorldPosition = instantiateInWorldSpace;
            m_setPositionRotation = false;
        }
        public InstantiationParameters(Vector3 position, Quaternion rotation, Transform parent)
        {
            m_position = position;
            m_rotation = rotation;
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
        IAsyncOperation<TObject> ProvideInstanceAsync<TObject>(IResourceProvider loadProvider, IResourceLocation location, IAsyncOperation<IList<object>> loadDependencyOperation, InstantiationParameters instantiateParameters)
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
