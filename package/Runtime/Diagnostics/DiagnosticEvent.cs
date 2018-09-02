using System;

namespace UnityEngine.ResourceManagement.Diagnostics
{
    /// <summary>
    /// Diagnostic event data.
    /// </summary>
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
        private byte[] m_data;   //this is up to the sender/receiver to serialize/deserialize

        /// <summary>
        /// Gets the graph id that this event is intended for
        /// </summary>
        /// <value>The graph Id</value>
        public string Graph { get { return m_graph; } }
        /// <summary>
        /// Optional id of the parent event.  This is used to structure the tree view of the event viewer
        /// </summary>
        /// <value>Parent Id</value>
        public string Parent { get { return m_parent; } }
        /// <summary>
        /// The id of this event.  Ids are used to combine multiple events into a single graph
        /// </summary>
        /// <value>Event Id</value>
        public string EventId { get { return m_id; } }
        /// <summary>
        /// The stream id for the event.  Each graph may display multiple streams of data for the same event Id
        /// </summary>
        /// <value>Stream Id</value>
        public int Stream { get { return m_stream; } }
        /// <summary>
        /// The frame that the event occurred 
        /// </summary>
        /// <value>Frame number</value>
        public int Frame { get { return m_frame; } }
        /// <summary>
        /// The value of the event. This value depends on the event type
        /// </summary>
        /// <value>Event value</value>
        public int Value { get { return m_value; } }
        /// <summary>
        /// Serialized data for the event.  The contents depend on the event type
        /// </summary>
        /// <value>Event data</value>
        public byte[] Data { get { return m_data; } }

        /// <summary>
        /// DiagnosticEvent constructor
        /// </summary>
        /// <param name="graph">Graph id</param>
        /// <param name="parent">Parent event id</param>
        /// <param name="id">Event id</param>
        /// <param name="stream">Stream index</param>
        /// <param name="frame">Frame number</param>
        /// <param name="value">Event value</param>
        /// <param name="data">Event data</param>
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

        /// <summary>
        /// Serializes the event into JSON and then encodes with System.Text.Encoding.ASCII.GetBytes
        /// </summary>
        /// <returns>Byte array containing serialized version of the event</returns>
        internal byte[] Serialize()
        {
            return System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
        }

        /// <summary>
        /// Deserializes event from a byte array created by the <cref="Serialize"/> method
        /// </summary>
        /// <returns>Deserialized DiagnosticEvent struct</returns>
        /// <param name="data">Serialized data</param>
        public static DiagnosticEvent Deserialize(byte[] data)
        {
            return JsonUtility.FromJson<DiagnosticEvent>(System.Text.Encoding.ASCII.GetString(data));
        }
    }
}