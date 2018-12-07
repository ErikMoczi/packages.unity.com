using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Unity.Tiny
{
    internal class WebSocketServer : BasicServer
    {
        private Dictionary<int, Connection> m_Connections = new Dictionary<int, Connection>();
        private List<Client> m_ClientsConnected = new List<Client>();
        private List<Message> m_ClientsDataReceived = new List<Message>();
        private List<Client> m_ClientsDisconnected = new List<Client>();

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

        public event EventHandler<Client> ClientConnected;
        public event EventHandler<Message> ClientDataReceived;
        public event EventHandler<Client> ClientDisconnected;

        [TinyInitializeOnLoad]
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

        private WebSocketServer() : base("wsserver", useIPC: true)
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
            var connectedClients = GetItemsFromConcurrentList(Instance.m_ClientsConnected);
            if (connectedClients != null)
            {
                foreach (var client in connectedClients)
                {
                    Instance.ClientConnected?.Invoke(Instance, client);
                }
            }

            var messages = GetItemsFromConcurrentList(Instance.m_ClientsDataReceived);
            if (messages != null)
            {
                foreach (var message in messages)
                {
                    Instance.ClientDataReceived?.Invoke(Instance, message);
                }
            }

            var disconnectedClients = GetItemsFromConcurrentList(Instance.m_ClientsDisconnected);
            if (disconnectedClients != null)
            {
                foreach (var client in disconnectedClients)
                {
                    Instance.ClientDisconnected?.Invoke(Instance, client);
                }
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
                        OnClientConnected(id, bytes, size);
                        break;
                    case ServerEvent.DataReceived:
                        OnClientDataReceived(id, bytes, size);
                        break;
                    case ServerEvent.Disconnected:
                        OnClientDisconnected(id);
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

        unsafe private void OnClientConnected(int id, byte* bytes, int size)
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
            }

            SendGetConnectionInfo(client, (message) =>
            {
                client.Info = message.Data.ToString();
                lock (m_ClientsConnected)
                {
                    m_ClientsConnected.Add(client);
                }
            });
        }

        unsafe private void OnClientDataReceived(int id, byte* bytes, int size)
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
                lock (m_ClientsDataReceived)
                {
                    m_ClientsDataReceived.Add(message);
                }
            });
        }

        private void OnClientDisconnected(int id)
        {
            Connection connection = null;
            lock (m_Connections)
            {
                if (!m_Connections.TryGetValue(id, out connection))
                {
                    return;
                }
                m_Connections.Remove(id);
            }

            lock (m_ClientsDisconnected)
            {
                m_ClientsDisconnected.Add(connection.Client);
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

        public void SendReload()
        {
            SendToClients("reload");
        }

        public void SendGetConnectionInfo(Client client, Action<Message> callback)
        {
            void messageHandler(object sender, Message message)
            {
                if (message.Client.Id == client.Id && message.Name == "connectionInfo")
                {
                    callback(message);
                    ClientDataReceived -= messageHandler;
                }
            }
            ClientDataReceived += messageHandler;
            SendToClient(client, "getConnectionInfo");
        }

        public void SendGetWorldState(Client client, Action<Message> callback)
        {
            void messageHandler(object sender, Message message)
            {
                if (message.Client.Id == client.Id && message.Name == "worldState")
                {
                    callback(message);
                    ClientDataReceived -= messageHandler;
                }
            }
            ClientDataReceived += messageHandler;
            SendToClient(client, "getWorldState");
        }

        public void SendSetWorldState(Client client, string worldState, Action<Message> callback)
        {
            void messageHandler(object sender, Message message)
            {
                if (message.Client.Id == client.Id && message.Name == "worldStateLoaded")
                {
                    callback(message);
                    ClientDataReceived -= messageHandler;
                }
            }
            ClientDataReceived += messageHandler;
            SendToClient(client, "setWorldState", worldState);
        }
    }
}
