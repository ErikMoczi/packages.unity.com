

using System;
using UnityEngine;

namespace Unity.Tiny
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class AutoRepaintOnTypeChangeAttribute : TinyAttribute
    {
        public readonly Type TinyType;

        public AutoRepaintOnTypeChangeAttribute(Type type)
            :base(0)
        {
            TinyType = type;
        }
    }
}

