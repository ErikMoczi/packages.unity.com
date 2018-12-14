using System;
using System.Net;
using System.Net.Sockets;
using UnityEditor;

namespace Unity.Tiny
{
    internal class IPCStream
    {
        private TcpClient m_Client;
        private NetworkStream m_Stream;

        public bool IsConnected => m_Client != null && m_Stream != null && m_Client.Connected;
        public event EventHandler<byte[]> OnDataReceived;
        public event EventHandler<EventArgs> OnClosed;

        public IPCStream()
        {
            AssemblyReloadEvents.beforeAssemblyReload += () => Close();
            EditorApplication.quitting += () => Close();
        }

        public bool Connect(int port)
        {
            if (m_Client == null)
            {
                m_Client = new TcpClient();
                m_Client.Connect("localhost", port);
                m_Stream = m_Client.Connected ? m_Client.GetStream() : null;
            }
            return IsConnected;
        }

        public void Close()
        {
            if (m_Client != null)
            {
                m_Client.Close();
                m_Client.Dispose();
                m_Client = null;
            }

            // Closing the TcpClient instance does not close the network stream
            if (m_Stream != null)
            {
                m_Stream.Close();
                m_Stream.Dispose();
                m_Stream = null;
            }
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
                            OnDataReceived?.Invoke(this, data);
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
                        OnClosed?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        callback(buffer);
                    }
                });
            }
            else
            {
                OnClosed?.Invoke(this, EventArgs.Empty);
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
                OnClosed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
