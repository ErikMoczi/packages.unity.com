

using System;
using System.Collections.Generic;

namespace Unity.Tiny
{
    internal delegate void TinyEventHandler<TEvent, in TValue>(TEvent @event, TValue @object)
        where TEvent : struct
        where TValue : class;

    internal interface IEvent
    {
        void AddListener<TEvent, TValue>(TinyEventHandler<TEvent, TValue> @delegate)
            where TEvent : struct
            where TValue : class;

        void RemoveListener<TEvent, TValue>(TinyEventHandler<TEvent, TValue> @delegate)
            where TEvent : struct
            where TValue : class;
    }

    internal class Event<TEvent, TValue> : IEvent
        where TEvent : struct
        where TValue : class
    {
        private TinyEventHandler<TEvent, TValue> m_Delegate;

        public void AddListener<TEventType, TObject>(TinyEventHandler<TEventType, TObject> @delegate)
            where TEventType : struct
            where TObject : class
        {
            m_Delegate += (TinyEventHandler<TEvent, TValue>)(object)@delegate;
        }

        public void RemoveListener<TEventType, TObject>(TinyEventHandler<TEventType, TObject> @delegate)
            where TEventType : struct
            where TObject : class
        {
            m_Delegate -= (TinyEventHandler<TEvent, TValue>)(object)@delegate;
        }

        public void Dispatch(TEvent type, TValue value)
        {
            if (null != m_Delegate)
            {
                m_Delegate.Invoke(type, value);
            }
        }
    }

    internal class TinyEventDispatcher
    {
        public static void AddListener<TEvent, TValue>(TEvent type, TinyEventHandler<TEvent, TValue> @event)
            where TEvent : struct
            where TValue : class
        {
            TinyEventDispatcher<TEvent>.AddListener(type, @event);
        }

        public static void RemoveListener<TEvent, TValue>(TEvent type, TinyEventHandler<TEvent, TValue> @event)
            where TEvent : struct
            where TValue : class
        {
            TinyEventDispatcher<TEvent>.RemoveListener(type, @event);
        }

        public static void Dispatch<TEvent, TValue>(TEvent type, TValue @event)
            where TEvent : struct
            where TValue : class
        {
            TinyEventDispatcher<TEvent>.Dispatch(type, @event);
        }
    }

    internal class TinyEventDispatcher<TEvent>
        where TEvent : struct
    {
        internal class EventMap
        {
            private readonly Dictionary<Type, IEvent> m_Events = new Dictionary<Type, IEvent>();

            public void AddListener<TValue>(TinyEventHandler<TEvent, TValue> @delegate) 
                where TValue : class
            {
                IEvent @event;

                if (!m_Events.TryGetValue(typeof(TValue), out @event))
                {
                    @event = new Event<TEvent, TValue>();
                    m_Events.Add(typeof(TValue), @event);
                }

                @event.AddListener(@delegate);
            }

            public void RemoveListener<TValue>(TinyEventHandler<TEvent, TValue> @delegate)
                where TValue : class
            {
                IEvent @event;

                if (!m_Events.TryGetValue(typeof(TValue), out @event))
                {
                    return;
                }

                @event.RemoveListener(@delegate);
            }

            public void Dispatch<TValue>(TEvent type, TValue value, Type objectType) 
                where TValue : class
            {
                IEvent @event;

                if (!m_Events.TryGetValue(objectType, out @event))
                {
                    return;
                }
                var typeEvent = @event as Event<TEvent, TValue>;
                typeEvent.Dispatch(type, value);
            }
        }

        private static readonly Dictionary<TEvent, EventMap> Events = new Dictionary<TEvent, EventMap>();

        public static void AddListener<TValue>(TEvent type, TinyEventHandler<TEvent, TValue> @delegate)
            
            where TValue : class
        {
            EventMap map = null;
            if (!Events.TryGetValue(type, out map))
            {
                map = Events[type] = new EventMap();
            }
            map.AddListener(@delegate);
        }

        public static void RemoveListener<TValue>(TEvent type, TinyEventHandler<TEvent, TValue> @delegate)
            where TValue : class
        {
            EventMap map = null;
            if (!Events.TryGetValue(type, out map))
            {
                map = Events[type] = new EventMap();
            }
            map.RemoveListener(@delegate);
        }

        public static void Dispatch<T>(TEvent type, T value = default(T)) where T : class
        {
            EventMap map = null;
            if (!Events.TryGetValue(type, out map))
            {
                return;
            }

            map.Dispatch(type, value, typeof(T));
        }

        public static void Dispatch(TEvent type)
        {
            Dispatch<object>(type);
        }
    }
}

