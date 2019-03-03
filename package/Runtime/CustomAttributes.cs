using System;

#if ENABLE_UNET

namespace UnityEngine.Networking
{
    [AttributeUsage(AttributeTargets.Class)]
    [Obsolete("The high level API classes are deprecated and will be removed in the future.")]
    public class NetworkSettingsAttribute : Attribute
    {
        public int channel = Channels.DefaultReliable;
        public float sendInterval = 0.1f;
    }

    [AttributeUsage(AttributeTargets.Field)]
    [Obsolete("The high level API classes are deprecated and will be removed in the future.")]
    public class SyncVarAttribute : Attribute
    {
        public string hook;
    }

    [AttributeUsage(AttributeTargets.Method)]
    [Obsolete("The high level API classes are deprecated and will be removed in the future.")]
    public class CommandAttribute : Attribute
    {
        public int channel = Channels.DefaultReliable; // this is zero
    }

    [AttributeUsage(AttributeTargets.Method)]
    [Obsolete("The high level API classes are deprecated and will be removed in the future.")]
    public class ClientRpcAttribute : Attribute
    {
        public int channel = Channels.DefaultReliable; // this is zero
    }


    [AttributeUsage(AttributeTargets.Method)]
    [Obsolete("The high level API classes are deprecated and will be removed in the future.")]
    public class TargetRpcAttribute : Attribute
    {
        public int channel = Channels.DefaultReliable; // this is zero
    }

    [AttributeUsage(AttributeTargets.Event)]
    [Obsolete("The high level API classes are deprecated and will be removed in the future.")]
    public class SyncEventAttribute : Attribute
    {
        public int channel = Channels.DefaultReliable;  // this is zero
    }

    [AttributeUsage(AttributeTargets.Method)]
    [Obsolete("The high level API classes are deprecated and will be removed in the future.")]
    public class ServerAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    [Obsolete("The high level API classes are deprecated and will be removed in the future.")]
    public class ServerCallbackAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    [Obsolete("The high level API classes are deprecated and will be removed in the future.")]
    public class ClientAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    [Obsolete("The high level API classes are deprecated and will be removed in the future.")]
    public class ClientCallbackAttribute : Attribute
    {
    }
}
#endif //ENABLE_UNET
