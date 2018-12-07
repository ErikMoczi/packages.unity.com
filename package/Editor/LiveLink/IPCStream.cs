
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Unity.Tiny
{
    internal class IPCStream
    {
        private TcpClient m_Client;
        private NetworkStream m_Stream;

        public bool IsConnected => m_Client != null && m_Stream != null && m_Client.Connected;
        public event EventHandler<byte[]> DataReceived;
        public event EventHandler<EventArgs> Closed;

        public bool Connect(int port)
        {
            m_Client = new TcpClient();
            m_Client.Connect("localhost", port);
            m_Stream = m_Client.Connected ? m_Client.GetStream() : null;
            return IsConnected;
        }

        public void Close()
        {
            m_Client.Close();
            m_Client.Dispose();
            m_Client = null;

            // Closing the TcpClient instance does not close the network stream
            m_Stream.Close();
            m_Stream.Dispose();
            m_Stream = null;
        }

        public void StartReadAsync()
        {
            ReadAsync(4, (dataLength) =>
            {
                var length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(dataLength, 0));
                ReadAsync(length, (data) =>
                {
                    if (data != null && data.Length > 0)
                    {
                        if (data.Length == 1 && data[0] == 255)
                        {
                            // ping, just drop packet
                        }
                        else
                        {
                            DataReceived?.Invoke(this, data);
                        }
                    }
                    StartReadAsync();
                });
            });
        }

        private void ReadAsync(int length, Action<byte[]> callback)
        {
            if (m_Client == null || m_Stream == null)
            {
                throw new ArgumentException();
            }

            if (m_Client.Connected && m_Stream.CanRead)
            {
                var buffer = new byte[length];
                m_Stream.ReadAsync(buffer, 0, length).ContinueWith(task =>
                {
                    if (task.Result == 0)
                    {
                        Closed?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        callback(buffer);
                    }
                });
            }
            else
            {
                Closed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void WriteAsync(byte[] data)
        {
            if (m_Client == null || m_Stream == null)
            {
                throw new ArgumentException();
            }

            if (m_Client.Connected && m_Stream.CanWrite)
            {
                m_Stream.WriteAsync(data, 0, data.Length);
            }
            else
            {
                Closed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
