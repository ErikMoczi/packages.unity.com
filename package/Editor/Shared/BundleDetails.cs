using System;
using UnityEngine;

namespace UnityEditor.Build
{
    [Serializable]
    public struct BundleDetails
    {
        public string fileName;
        public uint crc;
        public Hash128 hash;
    }
}