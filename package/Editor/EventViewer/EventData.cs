using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EditorDiagnostics
{
    [Serializable]
    public class EventDataCollection
    {
        [Serializable]
        public class PlayerSession
        {
            public DataSet rootStreamEntry = new DataSet("Root", "");
            public string m_name;
            public int m_playerId;
            public bool m_isActive;
            public int latestFrame = 0;
            public int startFrame = 0;
            public int frameCount = 300;

            Dictionary<int, List<DiagnosticEvent>> m_frameEvents = new Dictionary<int, List<DiagnosticEvent>>();
            public PlayerSession() {}
            public PlayerSession(string n, int i)
            {
                m_name = n;
                m_playerId = i;
                m_isActive = true;
            }

            DataSet GetDataSet(string parentName, string name, ref bool entryCreated, string graph)
            {
                DataSet parent = rootStreamEntry.GetDataSet(parentName, true, ref entryCreated, graph);
                if (parent == null)
                    parent = rootStreamEntry;
                return parent.GetDataSet(name, true, ref entryCreated, graph);
            }

            internal void Clear()
            {
                rootStreamEntry.Clear();
                m_frameEvents.Clear();
            }

            internal void Load()
            {
            }

            internal void Save()
            {
            }

            public List<DiagnosticEvent> GetFrameEvents(int frame)
            {
                List<DiagnosticEvent> frameEvents = null;
                if (m_frameEvents.TryGetValue(frame, out frameEvents))
                    return frameEvents;
                return null;
            }

            internal void AddSample(DiagnosticEvent evt, bool recordEvent, ref bool entryCreated)
            {
                latestFrame = evt.m_frame;
                startFrame = latestFrame - frameCount;

                if (recordEvent)
                {
                    List<DiagnosticEvent> frameEvents = null;
                    if (!m_frameEvents.TryGetValue(evt.m_frame, out frameEvents))
                        m_frameEvents.Add(evt.m_frame, frameEvents = new List<DiagnosticEvent>());
                    frameEvents.Add(evt);
                }

                var ds = GetDataSet(evt.m_parent, evt.m_id, ref entryCreated, evt.m_graph);
                ds.AddSample(evt.m_stream, evt.m_frame, evt.m_value);
            }

            [Serializable]
            public class DataSet
            {
                [Serializable]
                public class DataStream
                {
                    [Serializable]
                    public struct Sample
                    {
                        public int frame;
                        public int value;
                        public Sample(int f, int v) { frame = f; value = v; }
                    }
                    public int maxValue = 0;
                    public List<Sample> samples = new List<Sample>();
                    internal void AddSample(int frame, int val)
                    {
                        if (val > maxValue)
                            maxValue = val;
                        samples.Add(new Sample(frame, val));
                    }

                    internal int GetValue(int f)
                    {
                        if (samples.Count == 0 || f < samples[0].frame)
                            return 0;
                        if (f >= samples[samples.Count - 1].frame)
                            return samples[samples.Count - 1].value;
                        for (int i = 1; i < samples.Count; i++)
                        {
                            if (samples[i].frame > f)
                                return samples[i - 1].value;
                        }
                        return samples[0].value;
                    }
                }
                [SerializeField]
                List<DataStream> m_streams = new List<DataStream>();
                public string name;
                public string graph;
                public Dictionary<string, DataSet> m_children = null;
                public bool hasChildren { get { return m_children != null && m_children.Count > 0; } }
                public DataSet() {}
                public DataSet(string n, string g)
                {
                    name = n;
                    graph = g;
                }

                internal DataSet GetDataSet(string entryName, bool create, ref bool entryCreated, string graph)
                {
                    if (string.IsNullOrEmpty(entryName))
                        return null;
                    if (m_children == null)
                    {
                        if (!create)
                            return null;
                        m_children = new Dictionary<string, DataSet>();
                        entryCreated = true;
                    }
                    DataSet entry = null;
                    if (!m_children.TryGetValue(entryName, out entry) && create)
                    {
                        m_children.Add(entryName, entry = new DataSet(entryName, graph));
                        entryCreated = true;
                    }
                    return entry;
                }

                internal void AddSample(int stream, int frame, int val)
                {
                    while (stream >= m_streams.Count)
                        m_streams.Add(null);
                    if (m_streams[stream] == null)
                        m_streams[stream] = new DataStream();
                    m_streams[stream].AddSample(frame, val);
                }

                internal int GetStreamValue(int s, int frame)
                {
                    var stream = GetStream(s);
                    if (stream == null)
                        return 0;
                    return stream.GetValue(frame);
                }

                internal DataStream GetStream(int s)
                {
                    if (s >= m_streams.Count)
                        return null;
                    return m_streams[s];
                }

                internal int GetStreamMaxValue(int s)
                {
                    var stream = GetStream(s);
                    if (stream == null)
                        return 0;

                    return stream.maxValue;
                }

                internal void Clear()
                {
                    m_children.Clear();
                    m_streams.Clear();
                }
            }
        }

        List<PlayerSession> m_playerSessions = new List<PlayerSession>();
        public Action<PlayerSession, DiagnosticEvent, bool> onEvent;
        public Func<DiagnosticEvent, bool> onRecordEvent;

        bool RecordEvent(DiagnosticEvent e)
        {
            if (onRecordEvent != null)
                return onRecordEvent(e);
            return false;
        }

        public EventDataCollection()
        {
        }

        public void ProcessEvent(DiagnosticEvent evt, int sessionId)
        {
            var session = GetPlayerSession(sessionId, true);
            bool entryCreated = false;
            session.AddSample(evt, RecordEvent(evt), ref entryCreated);
            onEvent(session, evt, entryCreated);
        }

        public PlayerSession GetSessionByIndex(int index)
        {
            if (m_playerSessions.Count == 0 || m_playerSessions.Count <= index)
                return null;

            return m_playerSessions[index];
        }

        public PlayerSession GetPlayerSession(int playerId, bool create)
        {
            foreach (var c in m_playerSessions)
                if (c.m_playerId == playerId)
                    return c;
            if (create)
            {
                var c = new PlayerSession("Player " + playerId, playerId);
                m_playerSessions.Add(c);
                return c;
            }
            return null;
        }

        public string[] GetConnectionNames()
        {
            string[] names = new string[m_playerSessions.Count];// + 1];
            for (int i = 0; i < m_playerSessions.Count; i++)
                names[i] = m_playerSessions[i].m_name;
            return names;
        }

        public void AddSession(string name, int id)
        {
            m_playerSessions.Add(new PlayerSession(name, id));
        }
    }
}
