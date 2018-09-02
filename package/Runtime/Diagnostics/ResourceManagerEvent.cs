using UnityEngine;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace ResourceManagement.Diagnostics
{
    [System.Serializable]
    public struct ResourceManagerEvent
    {
        public enum Type
        {
            None,
            FrameCount,
            LoadAsyncRequest,
            LoadAsyncCompletion,
            Release,
            InstantiateAsyncRequest,
            InstantiateAsyncCompletion,
            ReleaseInstance,
            LoadSceneAsyncRequest,
            LoadSceneAsyncCompletion,
            ReleaseSceneAsyncRequest,
            ReleaseSceneAsyncCompletion,
            CacheEntryRefCount,
            CacheEntryLoadPercent,
            PoolCount,
        }
        public Type m_type;
        public string m_parent;
        public string m_id;
        public string m_path;
        public string m_provider;
        public int m_data;
        public int m_frame;

        public ResourceManagerEvent(Type t, string provider, string address, string path, int data, string parent)
        {
            m_parent = parent;
            m_id = address;
            m_provider = provider;
            m_path = path;
            m_frame = Time.frameCount;
            m_type = t;
            m_data = data;
        }

        public override string ToString()
        {
            return m_type + ": " + m_id;
        }

        public static byte[] Serialize(ResourceManagerEvent e)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(ms, e);
            ms.Flush();
            return ms.ToArray();
        }

        public static ResourceManagerEvent Deserialize(byte[] d)
        {
            ResourceManagerEvent evt = default(ResourceManagerEvent);
            try
            {
                if (d == null || d.Length < 8)
                    return evt;
                System.IO.MemoryStream ms = new System.IO.MemoryStream(d);
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                evt = (ResourceManagerEvent)formatter.Deserialize(ms);
                ms.Close();
            }
            catch (Exception) {}
            return evt;
        }
    }
}
