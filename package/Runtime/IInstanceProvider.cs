using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Class that contains properties to apply to instantiated objects.
    /// </summary>
    public class InstantiationParameters
    {
        private Vector3 m_position;
        private Quaternion m_rotation;
        private Transform m_parent;
        private bool m_instantiateInWorldPosition;
        private bool m_setPositionRotation;

        /// <summary>
        /// Position in world space to instantiate object.
        /// </summary>
        public Vector3 Position { get { return m_position; } }
        /// <summary>
        /// Rotation in world space to instantiate object.
        /// </summary>
        public Quaternion Rotation { get { return m_rotation; } }
        /// <summary>
        /// Transform to set as the parent of the instantiated object.
        /// </summary>
        public Transform Parent { get { return m_parent; } }
        /// <summary>
        /// When setting the parent Transform, this sets whether to preserve instance transform relative to world space or relative to the parent.
        /// </summary>
        public bool InstantiateInWorldPosition { get { return m_instantiateInWorldPosition; } }
        /// <summary>
        /// Flag to tell the IInstanceProvider whether to set the position and rotation on new instances.
        /// </summary>
        public bool SetPositionRotation { get { return m_setPositionRotation; } }
        /// <summary>
        /// Create a new InstantationParameters class that will set the parent transform and use the prefab transform.
        /// <param name="parent">Transform to set as the parent of the instantiated object.</param>
        /// <param name="instantiateInWorldSpace">Flag to tell the IInstanceProvider whether to set the position and rotation on new instances.</param>
        /// </summary>
        public InstantiationParameters(Transform parent, bool instantiateInWorldSpace)
        {
            m_position = Vector3.zero;
            m_rotation = Quaternion.identity;
            m_parent = parent;
            m_instantiateInWorldPosition = instantiateInWorldSpace;
            m_setPositionRotation = false;
        }
        /// <summary>
        /// Create a new InstantationParameters class that will set the position, rotation, and Transform parent of the instance.
        /// <param name="position">Position relative to the parent to set on the instance.</param>
        /// <param name="rotation">Rotation relative to the parent to set on the instance.</param>
        /// <param name="parent">Transform to set as the parent of the instantiated object.</param>
        /// </summary>
        public InstantiationParameters(Vector3 position, Quaternion rotation, Transform parent)
        {
            m_position = position;
            m_rotation = rotation;
            m_parent = parent;
            m_instantiateInWorldPosition = false;
            m_setPositionRotation = true;
        }

        /// <summary>
        /// Instantiate an object with the parameters of this object.
        /// <param name="source">Object to instantiate.</param>
        /// <returns>Instantiated object.</returns>
        /// <typeparam name="TObject">Object type. This type must be of type UnityEngine.Object.</typeparam>
        /// </summary>
        virtual public TObject Instantiate<TObject>(TObject source) where TObject : Object
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

    /// <summary>
    /// Interface that provides instances of objects.  This is used in ResourceManager.Instantiate* calls.
    /// </summary>
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
        /// Asynchronously instantiate the given <paramref name="location"/>
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
