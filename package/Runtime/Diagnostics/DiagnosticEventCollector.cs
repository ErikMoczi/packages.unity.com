using System;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_EDITOR
using UnityEngine.Networking.PlayerConnection;
#endif

namespace UnityEngine.ResourceManagement.Diagnostics
{
    public class DiagnosticEventCollector : MonoBehaviour
    {
        static public Guid EventGuid { get { return new Guid(1, 2, 3, new byte[] { 20, 1, 32, 32, 4, 9, 6, 44 }); } }
        static readonly List<DiagnosticEvent> s_unhandledEvents = new List<DiagnosticEvent>();
        static Action<DiagnosticEvent> s_eventHandlers;
        static public bool ProfileEvents { get; set; }
        static bool s_initialized = false;
        static int s_startFrame = -1;
        static List<int> s_frameEventCounts = new List<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void SendFirstFrameEvent()
        {
            if (ProfileEvents)
                PostEvent(new DiagnosticEvent("EventCount", "", "Events", 0, 0, 0, null));
        }

        public static void Initialize()
        {
            if (ProfileEvents)
            {
                var ec = FindObjectOfType<DiagnosticEventCollector>();
                if (ec == null)
                    new GameObject("EventCollector", typeof(DiagnosticEventCollector));
            }
            s_initialized = true;
        }

        public static void RegisterEventHandler(Action<DiagnosticEvent> handler)
        {
            Debug.Assert(s_unhandledEvents != null, "DiagnosticEventCollector.RegisterEventHandler - s_unhandledEvents == null.");
            if (handler == null)
                throw new ArgumentNullException("handler");
            s_eventHandlers += handler;
            foreach (var e in s_unhandledEvents)
                handler(e);
            s_unhandledEvents.Clear();
        }

        public static void UnregisterEventHandler(Action<DiagnosticEvent> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");
            s_eventHandlers -= handler;
        }

        static void CountFrameEvent(int frame)
        {
            Debug.Assert(s_frameEventCounts != null, "DiagnosticEventCollector.CountFrameEvent - s_frameEventCounts == null.");
            if (frame < s_startFrame)
                return;
            var index = frame - s_startFrame;
            while (index >= s_frameEventCounts.Count)
                s_frameEventCounts.Add(0);
            s_frameEventCounts[index]++;
        }

        public static void PostEvent(DiagnosticEvent diagnosticEvent)
        {
            if (!s_initialized)
                Initialize();

            if (!ProfileEvents)
                return;

            Debug.Assert(s_unhandledEvents != null, "DiagnosticEventCollector.PostEvent - s_unhandledEvents == null.");

            if (s_eventHandlers != null)
                s_eventHandlers(diagnosticEvent);
            else
                s_unhandledEvents.Add(diagnosticEvent);

            if (diagnosticEvent.EventId != "EventCount")
                CountFrameEvent(diagnosticEvent.Frame);
        }

        private void Awake()
        {
#if !UNITY_EDITOR
            RegisterEventHandler((DiagnosticEvent diagnosticEvent) => {PlayerConnection.instance.Send(EventGuid, diagnosticEvent.Serialize()); });
#endif
            SendEventCounts();
            DontDestroyOnLoad(gameObject);
            InvokeRepeating("SendEventCounts", 0, .25f);
        }

        void SendEventCounts()
        {
            Debug.Assert(s_frameEventCounts != null, "DiagnosticEventCollector.SendEventCounts - s_frameEventCounts == null.");

            int latestFrame = Time.frameCount;

            if (s_startFrame >= 0)
            {
                while (s_frameEventCounts.Count < latestFrame - s_startFrame)
                    s_frameEventCounts.Add(0);
                for (int i = 0; i < s_frameEventCounts.Count; i++)
                    PostEvent(new DiagnosticEvent("EventCount", "", "Events", 0, s_startFrame + i, s_frameEventCounts[i], null));
            }
            s_startFrame = latestFrame;
            s_frameEventCounts.Clear();
        }
    }
}
