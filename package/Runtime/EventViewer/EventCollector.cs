using System;
using System.Collections.Generic;
using UnityEngine;

//this is used by code surrounded by #if !UNITY_EDITOR, so dont remove even if it appears that it can be removed
using UnityEngine.Networking.PlayerConnection;

namespace EditorDiagnostics
{
    [Serializable]
    public struct DiagnosticEvent
    {
        public string m_graph;  //id of graph definition to use
        public string m_parent; //used to nest datagraphs
        public string m_id;     //id of a set of data streams
        public int m_stream;    //data stream
        public int m_frame;     //frame of the event
        public int m_value;      //data value of event
        public object m_data;   //this must be serializable

        public DiagnosticEvent(string graph, string parent, string id, int stream, int frame, int val, object data)
        {
            m_graph = graph;
            m_parent = parent;
            m_id = id;
            m_stream = stream;
            m_frame = frame;
            m_value = val;
            m_data = data;
        }

        public byte[] Serialize()
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(ms, this);
            ms.Flush();
            return ms.ToArray();
        }

        public static DiagnosticEvent Deserialize(byte[] d)
        {
            DiagnosticEvent evt = default(DiagnosticEvent);
            try
            {
                if (d == null || d.Length < 8)
                    return evt;
                System.IO.MemoryStream ms = new System.IO.MemoryStream(d);
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                evt = (DiagnosticEvent)formatter.Deserialize(ms);
                ms.Close();
            }
            catch (Exception) {}
            return evt;
        }
    }

    public class EventCollector : MonoBehaviour
    {
        static public Guid eventGUID = new Guid(1, 2, 3, new byte[] { 20, 1, 32, 32, 4, 9, 6, 44 });
        static readonly List<DiagnosticEvent> unhandledEvents = new List<DiagnosticEvent>();
        static Action<DiagnosticEvent> eventHandlers;
        static public bool profileEvents = false;
        static bool initialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void SendFirstFrameEvent()
        {
            PostEvent(new DiagnosticEvent("EventCount", "", "Events", 0, 0, 0, null));
        }

        public static void Initialize()
        {
            if (profileEvents)
            {
                var ec = FindObjectOfType<EventCollector>();
                if (ec == null)
                    new GameObject("EventCollector", typeof(EventCollector));
            }
            initialized = true;
        }

        public static void RegisterEventHandler(Action<DiagnosticEvent> handler)
        {
            eventHandlers += handler;
            foreach (var e in unhandledEvents)
                handler(e);
            unhandledEvents.Clear();
        }

        public static void UnregisterEventHandler(Action<DiagnosticEvent> handler)
        {
            eventHandlers -= handler;
        }

        static int m_startFrame = -1;
        static List<int> m_frameEventCounts = new List<int>();
        static void CountFrameEvent(int frame)
        {
            if (frame < m_startFrame)
                return;
            var index = frame - m_startFrame;
            while (index >= m_frameEventCounts.Count)
                m_frameEventCounts.Add(0);
            m_frameEventCounts[index]++;
        }

        public static void PostEvent(DiagnosticEvent e)
        {
            if (!initialized)
                Initialize();

            if (!profileEvents)
                return;

            if (eventHandlers != null)
                eventHandlers(e);
            else
                unhandledEvents.Add(e);

            if (e.m_id != "EventCount")
                CountFrameEvent(e.m_frame);
        }

        private void Awake()
        {
#if !UNITY_EDITOR
            RegisterEventHandler((DiagnosticEvent e) => {PlayerConnection.instance.Send(eventGUID, e.Serialize()); });
#endif
            SendEventCounts();
            DontDestroyOnLoad(gameObject);
            InvokeRepeating("SendEventCounts", 0, .25f);
        }

        void SendEventCounts()
        {
            int latestFrame = Time.frameCount;

            if (m_startFrame >= 0)
            {
                while (m_frameEventCounts.Count < latestFrame - m_startFrame)
                    m_frameEventCounts.Add(0);
                for (int i = 0; i < m_frameEventCounts.Count; i++)
                    PostEvent(new DiagnosticEvent("EventCount", "", "Events", 0, m_startFrame + i, m_frameEventCounts[i], null));
            }
            m_startFrame = latestFrame;
            m_frameEventCounts.Clear();
        }
    }
}
