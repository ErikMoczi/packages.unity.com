using System;
using Unity.Properties;
using Unity.Properties.Serialization;
using Unity.Tiny.Serialization.Json;
using Object = UnityEngine.Object;

namespace Unity.Tiny.Serialization
{
    /// <summary>
    /// Structure to capture UnityEngine.Object reference data
    /// </summary>
    internal struct UnityObjectHandle
    {
        public string Guid;
        public long FileId;
        public int Type;
    }

    /// <summary>
    /// @HACK
    /// This class is a workaround to access the internal guid, fileId and type of a UnityEngine.Object reference
    /// </summary>
    internal static class UnityObjectSerializer
    {
#if UNITY_EDITOR
        private static readonly string s_EmptyGuid = Guid.Empty.ToString();
        private static readonly string s_EmptyJsonObject = UnityEditor.EditorJsonUtility.ToJson(new Container { o = null });
#endif

#if UNITY_EDITOR
        private class Container
        {
            public Object o;
        }
#endif

        public static UnityObjectHandle ToObjectHandle(Object obj)
        {
            var handle = new UnityObjectHandle();

            if (!obj || null == obj)
            {
                return handle;
            }

#if UNITY_EDITOR
            var json = UnityEditor.EditorJsonUtility.ToJson(new Container { o = obj });
            if (string.IsNullOrEmpty(json) || json == s_EmptyJsonObject)
            {
                return handle;
            }
            json = json.Substring(5, json.Length - 6);

            var reader = new JsonObjectReader(json);
            reader.ReadBeginObject();
            reader.ReadPropertyNameSegment(); // fileID
            handle.FileId = reader.ReadInt64();
            reader.ReadValueSeparator();
            reader.ReadPropertyNameSegment(); // guid
            handle.Guid = reader.ReadString();
            reader.ReadValueSeparator();
            reader.ReadPropertyNameSegment(); // type
            handle.Type = reader.ReadInt32();
            reader.ReadValueSeparator();
#endif
            return handle;

        }

        public static Object FromObjectHandle(IPropertyContainer container)
        {
#if UNITY_EDITOR
            var c = new Container();
            var buffer = new StringBuffer(256);
            buffer.Append("{\"o\":{");
            buffer.Append("\"fileID\":");
            buffer.Append(container.GetValueOrDefault<long>("FileId"));
            buffer.Append(",\"guid\":\"");
            buffer.Append(container.GetValueOrDefault<string>("Guid", s_EmptyGuid));
            buffer.Append("\",\"type\": ");
            buffer.Append(container.GetValueOrDefault<int>("Type"));
            buffer.Append("}}");
            var json = buffer.ToString();
            UnityEditor.EditorJsonUtility.FromJsonOverwrite(json, c);
            return c.o;
#else
            return null;
#endif
        }

        public static Object FromObjectHandle(UnityObjectHandle handle)
        {
#if UNITY_EDITOR
            var c = new Container();
            var buffer = new StringBuffer(256);
            buffer.Append("{\"o\":{");
            buffer.Append("\"fileID\":");
            buffer.Append(handle.FileId);
            buffer.Append(",\"guid\":\"");
            buffer.Append(handle.Guid);
            buffer.Append("\",\"type\": ");
            buffer.Append(handle.Type);
            buffer.Append("}}");
            var json = buffer.ToString();
            UnityEditor.EditorJsonUtility.FromJsonOverwrite(json, c);
            return c.o;
#else
            return null;
#endif
        }
    }
}

