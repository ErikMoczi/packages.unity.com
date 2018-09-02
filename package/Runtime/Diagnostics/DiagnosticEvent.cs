using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement.Diagnostics
{
    [Serializable]
    public struct DiagnosticEvent
    {
        [SerializeField]
        private string m_graph;  //id of graph definition to use
        [SerializeField]
        private string m_parent; //used to nest datagraphs
        [SerializeField]
        private string m_id;     //id of a set of data streams
        [SerializeField]
        private int m_stream;    //data stream
        [SerializeField]
        private int m_frame;     //frame of the event
        [SerializeField]
        private int m_value;      //data value of event
        [SerializeField]
        private byte[] m_data;   //this is up to the ender/receiver to serialize/deserialize

        public string Graph { get { return m_graph; } }
        public string Parent { get { return m_parent; } }
        public string EventId { get { return m_id; } }
        public int Stream { get { return m_stream; } }
        public int Frame { get { return m_frame; } }
        public int Value { get { return m_value; } }
        public byte[] Data { get { return m_data; } }

        public DiagnosticEvent(string graph, string parent, string id, int stream, int frame, int value, byte[] data)
        {
            m_graph = graph;
            m_parent = parent;
            m_id = id;
            m_stream = stream;
            m_frame = frame;
            m_value = value;
            m_data = data;
        }

        public byte[] Serialize()
        {
            return System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
        }

        public static DiagnosticEvent Deserialize(byte[] data)
        {
            return JsonUtility.FromJson<DiagnosticEvent>(System.Text.Encoding.ASCII.GetString(data));
        }
    }
}