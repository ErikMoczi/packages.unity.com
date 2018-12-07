using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Unity.Tiny
{
    internal class WebSocketServer : BasicServer
    {
        private readonly Dictionary<int, Connection> m_Connections = new Dictionary<int, Connection>();
        private Client m_LastActiveClient;

        private struct SyncServerEvent
        {
            public ServerEvent Event;
            public Client Client;
            public Message Message;
        }
        private readonly List<SyncServerEvent> m_SyncServerEvents = new List<SyncServerEvent>();

        private class Connection
        {
            public Client Client { get; set; }
            public List<Protocol.Buffer> Buffers = new List<Protocol.Buffer>();
        }

        public class Client
        {
            public int Id { get; internal set; }
            public string Address { get; internal set; }
            public string Info { get; internal set; }
            public string Label
            {
                get
                {
                    var label = "";
                    if (!string.IsNullOrEmpty(Address))
                    {
                        const string IPv6Begin = "::ffff:";
                        label += Address.StartsWith(IPv6Begin) ? Address.Substring(IPv6Begin.Length) : Address;
                        if (!string.IsNullOrEmpty(Info))
                        {
                            label += $" ({Info})";
                        }
                    }
                    else
                    {
                        label += $"Client {Id}";
                    }
                    return label;
                }
            }
        }

        public class Message
        {
            public Message(Client client, IReadOnlyList<Protocol.Buffer> buffers)
            {
                if (buffers == null || buffers.Count <= 0)
                {
                    throw new ArgumentNullException("empty buffers");
                }

                Client = client;
                Name = buffers[0].ToString();
                Buffers = buffers.Select(b => b.Bytes).ToArray();
            }

            public Client Client { get; }
            public string Name { get; }
            public byte[] Data => Buffers.Count() > 1 ? Buffers[1] : null;
            public byte[][] Buffers { get; }
        }

        public static WebSocketServer Instance { get; private set; }
        protected override string[] ShellArgs => new string[]
        {
            $"-p {Port}",
            $"-w {Process.GetCurrentProcess().Id}"
        };
        public override Uri URL => new UriBuilder("ws", LocalIP, Port).Uri;

        public bool HasClients
        {
            get
            {
                lock (m_Connections)
                {
                    return m_Connections.Count > 0;
                }
            }
        }

        public Client[] Clients
        {
            get
            {
                lock (m_Connections)
                {
                    return m_Connections.Values.Select(c => c.Client).ToArray();
                }
            }
        }

        public Client ActiveClient { get; set; }
        public int ActiveClientIndex
        {
            get { return Clients.ToList().FindIndex(c => c == ActiveClient); }
            set { ActiveClient = value >= 0 && value < Clients.Count() ? Clients[value] : null; }
        }

        public delegate void ClientEventHandler(Client client);
        public delegate void MessageEventHandler(Message message);

        public event ClientEventHandler OnClientConnected;
        public event MessageEventHandler OnClientDataReceived;
        public event ClientEventHandler OnClientDisconnected;
        public event ClientEventHandler OnActiveClientChanged;

        [TinyInitializeOnLoad(100)]
        private static void Initialize()
        {
            Instance = new WebSocketServer();
            TinyEditorApplication.OnLoadProject += (project, context) =>
            {
                Bridge.EditorApplication.RegisterContextualUpdate(OnEditorUpdate);
                Instance.Listen(project.Settings.LocalWSServerPort);
            };
            TinyEditorApplication.OnCloseProject += (project, context) =>
            {
                Instance.Close();
                Bridge.EditorApplication.UnregisterContextualUpdate(OnEditorUpdate);
            };
        }

        private WebSocketServer() : base("WSServer", useIPC: true)
        {
            if (Listening)
            {
                SendToServer(ServerEvent.Reconnect);
            }
        }

        public override void Close()
        {
            base.Close();
            m_Connections.Clear();
            ActiveClient = null;
        }

        private static T[] GetItemsFromConcurrentList<T>(List<T> list)
        {
            T[] items = null;
            lock (list)
            {
                if (list.Count > 0)
                {
                    items = new T[list.Count];
                    for (var i = 0; i < list.Count; ++i)
                    {
                        items[i] = list[i];
                    }
                    list.Clear();
                }
            }
            return items;
        }

        private static void OnEditorUpdate()
        {
            var serverEvents = GetItemsFromConcurrentList(Instance.m_SyncServerEvents);
            if (serverEvents != null)
            {
                foreach (var serverEvent in serverEvents)
                {
                    switch (serverEvent.Event)
                    {
                        case ServerEvent.Connected:
                            Instance.OnClientConnected?.Invoke(serverEvent.Client);
                            break;
                        case ServerEvent.DataReceived:
                            Instance.OnClientDataReceived?.Invoke(serverEvent.Message);
                            break;
                        case ServerEvent.Disconnected:
                            Instance.OnClientDisconnected?.Invoke(serverEvent.Client);
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }

            if (Instance.ActiveClient != Instance.m_LastActiveClient)
            {
                Instance.OnActiveClientChanged?.Invoke(Instance.ActiveClient);
                Instance.m_LastActiveClient = Instance.ActiveClient;
            }
        }

        unsafe protected override void OnIPCDataReceived(object sender, byte[] buffer)
        {
            var read = 0;
            if (buffer.Length - read < sizeof(byte))
            {
                throw new Exception($"Cannot read server event header.");
            }
            var @event = (ServerEvent)buffer[0];
            read += sizeof(byte);

            if (buffer.Length - read < sizeof(int))
            {
                throw new Exception($"Cannot read connection id header.");
            }
            var id = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, read));
            read += sizeof(int);

            if (buffer.Length - read < sizeof(int))
            {
                throw new Exception($"Cannot read data size header.");
            }
            var dataLength = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, read));
            read += sizeof(int);

            fixed (byte* ptr = buffer)
            {
                var size = Math.Min(dataLength, buffer.Length - read);
                var bytes = size > 0 ? ptr + read : null;
                switch (@event)
                {
                    case ServerEvent.Connected:
                        ClientConnectedEvent(id, bytes, size);
                        break;
                    case ServerEvent.DataReceived:
                        ClientDataReceivedEvent(id, bytes, size);
                        break;
                    case ServerEvent.Disconnected:
                        ClientDisconnectedEvent(id);
                        break;
                    default:
                        throw new Exception($"invalid server event {@event}");
                }
            }
        }

        protected override void OnIPCClosed(object sender, EventArgs args)
        {
            UnityEngine.Debug.LogError($"IPC unexpectedly closed. Restarting {Name} server.");
            Close();
            Listen(Port);
        }

        unsafe private void ClientConnectedEvent(int id, byte* bytes, int size)
        {
            var address = bytes != null ? Encoding.ASCII.GetString(bytes, size) : null;
            var client = new Client() { Id = id, Address = address };

            lock (m_Connections)
            {
                if (m_Connections.Keys.Contains(id))
                {
                    return;
                }
                m_Connections.Add(id, new Connection { Client = client });

                if (ActiveClient == null || m_Connections.Values.FirstOrDefault(c => c.Client == ActiveClient) == null)
                {
                    ActiveClient = client;
                }
            }

            SendGetConnectionInfo(client, (message) =>
            {
                client.Info = Encoding.ASCII.GetString(message.Data);
                lock (m_SyncServerEvents)
                {
                    m_SyncServerEvents.Add(new SyncServerEvent { Event = ServerEvent.Connected, Client = client });
                }
            });
        }

        unsafe private void ClientDataReceivedEvent(int id, byte* bytes, int size)
        {
            Connection connection = null;
            lock (m_Connections)
            {
                if (!m_Connections.TryGetValue(id, out connection))
                {
                    return;
                }
            }

            Protocol.Decode(connection.Buffers, bytes, size, (buffers) =>
            {
                var message = new Message(connection.Client, buffers);
                lock (m_SyncServerEvents)
                {
                    m_SyncServerEvents.Add(new SyncServerEvent { Event = ServerEvent.DataReceived, Message = message });
                }
            });
        }

        private void ClientDisconnectedEvent(int id)
        {
            Connection connection = null;
            lock (m_Connections)
            {
                if (!m_Connections.TryGetValue(id, out connection))
                {
                    return;
                }
                m_Connections.Remove(id);

                if (m_Connections.Count == 0)
                {
                    ActiveClient = null;
                }
                else if (ActiveClient == connection.Client || m_Connections.Values.FirstOrDefault(c => c.Client == ActiveClient) == null)
                {
                    ActiveClient = m_Connections.Values.FirstOrDefault().Client;
                }
            }

            lock (m_SyncServerEvents)
            {
                m_SyncServerEvents.Add(new SyncServerEvent { Event = ServerEvent.Disconnected, Client = connection.Client });
            }
        }

        private void SendToServer(ServerEvent @event)
        {
            IPCStream.WriteAsync(Protocol.Encode((byte)@event));
        }

        private void SendToClients(params byte[][] buffers)
        {
            var @event = Protocol.Encode((byte)ServerEvent.Broadcast);
            var data = Protocol.EncodeMessage(buffers);
            foreach (var connection in m_Connections.Values)
            {
                var id = Protocol.Encode(connection.Client.Id);
                IPCStream.WriteAsync(Protocol.Combine(@event, id, data));
            }
        }

        private void SendToClient(Client client, params byte[][] buffers)
        {
            var @event = Protocol.Encode((byte)ServerEvent.Broadcast);
            var id = Protocol.Encode(client.Id);
            var data = Protocol.EncodeMessage(buffers);
            IPCStream.WriteAsync(Protocol.Combine(@event, id, data));
        }

        private void SendToClients(params string[] strings)
        {
            var @event = Protocol.Encode((byte)ServerEvent.Broadcast);
            var data = Protocol.EncodeMessage(strings);
            foreach (var connection in m_Connections.Values)
            {
                var id = Protocol.Encode(connection.Client.Id);
                IPCStream.WriteAsync(Protocol.Combine(@event, id, data));
            }
        }

        private void SendToClient(Client client, params string[] strings)
        {
            var @event = Protocol.Encode((byte)ServerEvent.Broadcast);
            var id = Protocol.Encode(client.Id);
            var data = Protocol.EncodeMessage(strings);
            IPCStream.WriteAsync(Protocol.Combine(@event, id, data));
        }

        private void MessageHandler(Client client, Action<Message> callback, string name, Message message, MessageEventHandler handler)
        {
            if (message.Client == client && message.Name == name)
            {
                callback(message);
                OnClientDataReceived -= handler;
            }
        }

        public void SendReload()
        {
            SendToClients("reload");
        }

        public void SendGetConnectionInfo(Client client, Action<Message> callback)
        {
            void handler(Message message)
            {
                MessageHandler(client, callback, "connectionInfo", message, handler);
            }
            OnClientDataReceived += handler;
            SendToClient(client, "getConnectionInfo");
        }

        public void SendGetWorldState(Client client, Action<Message> callback)
        {
            void handler(Message message)
            {
                MessageHandler(client, callback, "worldState", message, handler);
            }
            OnClientDataReceived += handler;
            SendToClient(client, "getWorldState");
        }

        public void SendSetWorldState(Client client, string worldState, Action<Message> callback)
        {
            void handler(Message message)
            {
                MessageHandler(client, callback, "worldStateLoaded", message, handler);
            }
            OnClientDataReceived += handler;
            SendToClient(client, "setWorldState", worldState);
        }

        public void SendPause(Client client, Action<Message> callback)
        {
            void handler(Message message)
            {
                MessageHandler(client, callback, "pauseState", message, handler);
            }
            OnClientDataReceived += handler;
            SendToClient(client, "pause");
        }

        public void SendIsPaused(Client client, Action<Message> callback)
        {
            void handler(Message message)
            {
                MessageHandler(client, callback, "pauseState", message, handler);
            }
            OnClientDataReceived += handler;
            SendToClient(client, "isPaused");
        }

        public void SendStep(Client client)
        {
            SendToClient(client, "step");
        }

        public void SendResume()
        {
            SendToClients("resume");
        }

        public void SendResume(Client client)
        {
            SendToClient(client, "resume");
        }
    }
}
