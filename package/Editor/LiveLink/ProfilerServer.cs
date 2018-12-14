using System;

namespace Unity.Tiny
{
    internal class ProfilerServer
    {
        public static int Port => 54997;
        public static Uri URL => new UriBuilder("ws", BasicServer.LocalIP, Port).Uri;
    }
}
