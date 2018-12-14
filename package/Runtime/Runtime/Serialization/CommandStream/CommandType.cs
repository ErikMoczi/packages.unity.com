

using System;

namespace Unity.Tiny.Serialization.CommandStream
{
    /// <summary>
    /// Command Types
    /// 
    /// The format of a command is 
    /// 
    /// [2 bytes ] [2 bytes ] [n bytes]
    /// 
    /// [CMD_TYPE] [DATA_LEN] [DATA...]
    /// 
    /// We reserve 2 bytes for built in command types
    /// </summary>
    internal static class CommandType
    {
        public const byte None = 0;
        public const byte User = 1;
        
        public const byte CreateProject = 2;
        public const byte CreateModule = 3;
        public const byte CreateType = 4;
        public const byte CreateEntityGroup = 5;
        public const byte CreateEntity = 6;
        public const byte CreateScript = 7; // DEPRECATED
        public const byte CreateSystem = 8; // DEPRECATED
        public const byte Unregister = 9;
        
        public const byte PushSourceIdentifierScope = 10;
        public const byte PopSourceIdentifierScope = 11;
        
        public const byte CreatePrefabInstance = 12;

        public static byte GetCreateCommandType(TinyTypeId typeId)
        {
            switch (typeId)
            {
                case TinyTypeId.Unknown:
                    return None;
                case TinyTypeId.Project:
                    return CreateProject;
                case TinyTypeId.Module:
                    return CreateModule;
                case TinyTypeId.Type:
                    return CreateType;
                case TinyTypeId.EntityGroup:
                    return CreateEntityGroup;
                case TinyTypeId.Entity:
                    return CreateEntity;
                case TinyTypeId.Script:
                    return CreateScript;
                case TinyTypeId.System:
                    return CreateSystem;
                case TinyTypeId.PrefabInstance:
                    return CreatePrefabInstance;
                case TinyTypeId.EnumReference:
                case TinyTypeId.EntityReference:
                case TinyTypeId.UnityObject:
                    return None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeId), typeId, null);
            }
        }
    }
}

