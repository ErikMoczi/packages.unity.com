

using UnityEngine;


namespace Unity.Tiny
{
    internal static class TinyObjectExtensions
    {
        public static void AssignIfDifferent<TValue>(this TinyObject tiny, string propertyName, TValue value)
        {
            var current = tiny.GetProperty<TValue>(propertyName);
            if (null == current && null == value)
            {
                return;
            }
            if (!current?.Equals(value) ?? true)
            {
                tiny.AssignPropertyFrom(propertyName, value);
            }
        }

        public static void AssignIfDifferent(this TinyObject tiny, string propertyName, Vector2 value)
        {
            var v = tiny[propertyName] as TinyObject;
            AssignIfDifferent(v, "x", value.x);
            AssignIfDifferent(v, "y", value.y);
        }

        public static void AssignIfDifferent(this TinyObject tiny, string propertyName, Vector2Int value)
        {
            var v = tiny[propertyName] as TinyObject;
            AssignIfDifferent(v, "x", (float)value.x);
            AssignIfDifferent(v, "y", (float)value.y);
        }

        public static void AssignIfDifferent(this TinyObject tiny, string propertyName, Vector3 value)
        {
            var v = tiny[propertyName] as TinyObject;
            AssignIfDifferent(v, "x", value.x);
            AssignIfDifferent(v, "y", value.y);
            AssignIfDifferent(v, "z", value.z);
        }

        public static void AssignIfDifferent(this TinyObject tiny, string propertyName, Vector3Int value)
        {
            var v = tiny[propertyName] as TinyObject;
            AssignIfDifferent(v, "x", (float)value.x);
            AssignIfDifferent(v, "y", (float)value.y);
            AssignIfDifferent(v, "z", (float)value.z);
        }

        public static void AssignIfDifferent(this TinyObject tiny, string propertyName, Vector4 value)
        {
            var v = tiny[propertyName] as TinyObject;
            AssignIfDifferent(v, "x", value.x);
            AssignIfDifferent(v, "y", value.y);
            AssignIfDifferent(v, "z", value.z);
            AssignIfDifferent(v, "w", value.w);
        }

        public static void AssignIfDifferent(this TinyObject tiny, string propertyName, Quaternion value)
        {
            var v = tiny[propertyName] as TinyObject;
            AssignIfDifferent(v, "x", value.x);
            AssignIfDifferent(v, "y", value.y);
            AssignIfDifferent(v, "z", value.z);
            AssignIfDifferent(v, "w", value.w);
        }

        public static void AssignIfDifferent(this TinyObject tiny, string propertyName, Rect value)
        {
            var v = tiny[propertyName] as TinyObject;
            AssignIfDifferent(v, "x", value.x);
            AssignIfDifferent(v, "y", value.y);
            AssignIfDifferent(v, "width", value.width);
            AssignIfDifferent(v, "height", value.height);
        }
    }
}

