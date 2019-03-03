using System;
using System.Net;

using UnityEngine.Networking.Types;

namespace UnityEngine.Networking
{
    public class NetworkTransportHelper
    {
        static INetworkTransport s_ActiveTransport = new DefaultNetworkTransport();

        public static INetworkTransport Active
        {
            get
            {
                return s_ActiveTransport;
            }
            set
            {
                if (s_ActiveTransport != null && s_ActiveTransport.IsStarted)
                {
                    throw new InvalidOperationException("Cannot change network transport when current transport object is in use.");
                }

                if (value == null)
                    s_ActiveTransport = new DefaultNetworkTransport();
                else
                    s_ActiveTransport = value;
            }
        }

        public static void Init()
        {
            s_ActiveTransport.Init();
        }

        public static void Init(GlobalConfig config)
        {
            s_ActiveTransport.Init();
        }

        public static bool IsStarted
        {
            get
            {
                return s_ActiveTransport.IsStarted;
            }
        }

        public static void Shutdown()
        {
            s_ActiveTransport.Shutdown();
        }

        public static int AddHost(HostTopology topology, int port, string ip)
        {
            return s_ActiveTransport.AddHost(topology, port, ip);
        }

        public static int AddWebsocketHost(HostTopology topology, int port, string ip)
        {
            return s_ActiveTransport.AddWebsocketHost(topology, port, ip);
        }

        public static int ConnectWithSimulator(int hostId, string address, int port, int exceptionConnectionId, out byte error, ConnectionSimulatorConfig conf)
        {
            return s_ActiveTransport.ConnectWithSimulator(hostId, address, port, exceptionConnectionId, out error, conf);
        }

        public static int Connect(int hostId, string address, int port, int exceptionConnectionId, out byte error)
        {
            return s_ActiveTransport.Connect(hostId, address, port, exceptionConnectionId, out error);
        }

        public static void ConnectAsNetworkHost(int hostId, string address, int port, NetworkID network, SourceID source, NodeID node, out byte error)
        {
            s_ActiveTransport.ConnectAsNetworkHost(hostId, address, port, network, source, node, out error);
        }

        public static int ConnectToNetworkPeer(int hostId, string address, int port, int exceptionConnectionId, int relaySlotId, NetworkID network, SourceID source, NodeID node, out byte error)
        {
            return s_ActiveTransport.ConnectToNetworkPeer(hostId, address, port, exceptionConnectionId, relaySlotId, network, source, node, out error);
        }

        public static int ConnectEndPoint(int hostId, EndPoint endPoint, int exceptionConnectionId, out byte error)
        {
            return s_ActiveTransport.ConnectEndPoint(hostId, endPoint, exceptionConnectionId, out error);
        }

        public static bool DoesEndPointUsePlatformProtocols(EndPoint endPoint)
        {
            return s_ActiveTransport.DoesEndPointUsePlatformProtocols(endPoint);
        }

        public static int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, int port)
        {
            return s_ActiveTransport.AddHostWithSimulator(topology, minTimeout, maxTimeout, port);
        }

        public static bool RemoveHost(int hostId)
        {
            return s_ActiveTransport.RemoveHost(hostId);
        }

        public static bool Send(int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error)
        {
            return s_ActiveTransport.Send(hostId, connectionId, channelId, buffer, size, out error);
        }

        public static NetworkEventType ReceiveFromHost(int hostId, out int connectionId, out int channelId, byte[] buffer, int bufferSize, out int receivedSize, out byte error)
        {
            return s_ActiveTransport.ReceiveFromHost(hostId, out connectionId, out channelId, buffer, bufferSize, out receivedSize, out error);
        }

        public static NetworkEventType ReceiveRelayEventFromHost(int hostId, out byte error)
        {
            return s_ActiveTransport.ReceiveRelayEventFromHost(hostId, out error);
        }

        public static int GetCurrentRTT(int hostId, int connectionId, out byte error)
        {
            return s_ActiveTransport.GetCurrentRTT(hostId, connectionId, out error);
        }

        public static void GetConnectionInfo(int hostId, int connectionId, out string address, out int port, out NetworkID network, out NodeID dstNode, out byte error)
        {
            s_ActiveTransport.GetConnectionInfo(hostId, connectionId, out address, out port, out network, out dstNode, out error);
        }

        public static bool Disconnect(int hostId, int connectionId, out byte error)
        {
            return s_ActiveTransport.Disconnect(hostId, connectionId, out error);
        }

        public static void SetBroadcastCredentials(int hostId, int key, int version, int subversion, out byte error)
        {
            s_ActiveTransport.SetBroadcastCredentials(hostId, key, version, subversion, out error);
        }

        public static bool StartBroadcastDiscovery(int hostId, int broadcastPort, int key, int version, int subversion, byte[] buffer, int size, int timeout, out byte error)
        {
            return s_ActiveTransport.StartBroadcastDiscovery(hostId, broadcastPort, key, version, subversion, buffer, size, timeout, out error);
        }

        public static void GetBroadcastConnectionInfo(int hostId, out string address, out int port, out byte error)
        {
            s_ActiveTransport.GetBroadcastConnectionInfo(hostId, out address, out port, out error);
        }

        public static void GetBroadcastConnectionMessage(int hostId, byte[] buffer, int bufferSize, out int receivedSize, out byte error)
        {
            s_ActiveTransport.GetBroadcastConnectionMessage(hostId, buffer, bufferSize, out receivedSize, out error);
        }

        public static void StopBroadcastDiscovery()
        {
            s_ActiveTransport.StopBroadcastDiscovery();
        }

        public static void SetPacketStat(int direction, int packetStatId, int numMsgs, int numBytes)
        {
            s_ActiveTransport.SetPacketStat(direction, packetStatId, numMsgs, numBytes);
        }
    }
}
