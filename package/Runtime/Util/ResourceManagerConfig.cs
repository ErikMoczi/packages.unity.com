using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Interface for objects that support post construction initialization via an id and byte array.
    /// </summary>
    public interface IInitializableObject
    {
        /// <summary>
        /// Initialize a constructed object.
        /// </summary>
        /// <param name="id">The id of the object.</param>
        /// <param name="data">Serialized data for the object.</param>
        /// <returns>The result of the initialization.</returns>
        bool Initialize(string id, string data);
    }


    /// <summary>
    /// Interface for objects that can create object initialization data.
    /// </summary>
    public interface IObjectInitializationDataProvider
    {
        string Name { get; }
        ObjectInitializationData CreateObjectInitializationData();
    }



    /// <summary>
    /// Attribute for restricting which types can be assigned to a SerializedType
    /// </summary>
    public class SerializedTypeRestrictionAttribute : Attribute
    {
        public Type type;
    }

    /// <summary>
    /// Wrapper for serializing types for runtime.
    /// </summary>
    [Serializable]
    public struct SerializedType
    {
        [SerializeField]
        string m_assemblyName;
        /// <summary>
        /// The assembly name of the type.
        /// </summary>
        public string AssemblyName { get { return m_assemblyName; } }

        [SerializeField]
        string m_className;
        /// <summary>
        /// The name of the type.
        /// </summary>
        public string ClassName { get { return m_className; } }

        Type _cachedType;

        /// <inheritdoc/>
        public override string ToString()
        {
            return Value.Name;
        }

        /// <summary>
        /// Get and set the serialized type.
        /// </summary>
        public Type Value
        {
            get
            {
                if (string.IsNullOrEmpty(m_assemblyName) || string.IsNullOrEmpty(m_className))
                    return null;

                if (_cachedType == null)
                {
                    var assembly = Assembly.Load(m_assemblyName);
                    if(assembly != null)
                        _cachedType = assembly.GetType(m_className);
                }
                return _cachedType;
            }
            set
            {
                if (value != null)
                {
                    m_assemblyName = value.Assembly.FullName;
                    m_className = value.FullName;
                }
                else
                {
                    m_assemblyName = m_className = null;
                }
            }
        }
    }

    /// <summary>
    /// Contains data used to construct and initialize objects at runtime.
    /// </summary>
    [Serializable]
    public struct ObjectInitializationData
    {
        [SerializeField]
        string m_id;
        /// <summary>
        /// The object id.
        /// </summary>
        public string Id { get { return m_id; } }

        [SerializeField]
        SerializedType m_objectType;
        /// <summary>
        /// The object type that will be created.
        /// </summary>
        public SerializedType ObjectType { get { return m_objectType; } }

        [SerializeField]
        string m_data;
        /// <summary>
        /// String representation of the data that will be passed to the IInitializableObject.Initialize method of the created object.  This is usually a JSON string of the serialized data object.
        /// </summary>
        public string Data { get { return m_data; } }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("ObjectInitializationData: id={0}, type={1}", m_id, m_objectType);
        }

        /// <summary>
        /// Create an instance of the defined object.  Initialize will be called on it with the id and data if it implements the IInitializableObject interface.
        /// </summary>
        /// <returns>Constructed object.  This object will already be initialized with its serialized data and id.</returns>
        public TObject CreateInstance<TObject>()
        {
            try
            {
                var obj = Activator.CreateInstance(m_objectType.Value);
                var serObj = obj as IInitializableObject;
                if (serObj != null)
                {
                    if (!serObj.Initialize(m_id, m_data))
                        return default(TObject);
                }
                return (TObject)obj;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return default(TObject);
            }
        }

#if UNITY_EDITOR
        Type[] m_runtimeTypes;
        /// <summary>
        /// Construct a serialized data for the object.
        /// </summary>
        /// <param name="objectType">The type of object to create.</param>
        /// <param name="id">The object id.</param>
        /// <param name="dataObject">The serializable object that will be saved into the Data string via the JSONUtility.ToJson method.</param>
        /// <returns>Contains data used to construct and initialize an object at runtime.</returns>
        public static ObjectInitializationData CreateSerializedInitializationData(Type objectType, string id = null, object dataObject = null)
        {
            return new ObjectInitializationData()
            {
                m_objectType = new SerializedType() { Value = objectType },
                m_id = string.IsNullOrEmpty(id) ? objectType.FullName : id,
                m_data = dataObject == null ? null : JsonUtility.ToJson(dataObject),
                m_runtimeTypes = dataObject == null ? null : new Type[] { dataObject.GetType() }
            };
        }

        /// <summary>
        /// Construct a serialized data for the object.
        /// </summary>
        /// <typeparam name="T">The type of object to create.</typeparam>
        /// <param name="dataObject">The serializable object that will be saved into the Data string via the JSONUtility.ToJson method.</param>
        /// <returns>Contains data used to construct and initialize an object at runtime.</returns>
        public static ObjectInitializationData CreateSerializedInitializationData<T>(string id = null, object dataObject = null)
        {
            return CreateSerializedInitializationData(typeof(T), id, dataObject);
        }

        /// <summary>
        /// Get the set of runtime types need to deserialized this object.  This is used to ensure that types are not stripped from player builds.
        /// </summary>
        /// <returns></returns>
        public Type[] GetRuntimeTypes()
        {
            return m_runtimeTypes;
        }
#endif
    }

    internal static class ResourceManagerConfig
    {
        public static bool IsInstance<T1, T2>()
        {
            var tA = typeof(T1);
            var tB = typeof(T2);
#if !UNITY_EDITOR && UNITY_WSA_10_0 && ENABLE_DOTNET
            return tB.GetTypeInfo().IsAssignableFrom(tA.GetTypeInfo());
#else
            return tB.IsAssignableFrom(tA);
#endif
        }

    }
}
