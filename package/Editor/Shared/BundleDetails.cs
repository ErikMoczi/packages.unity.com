using System;
using UnityEngine;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Struct containing detailed information about a built asset bundle
    /// </summary>
    [Serializable]
    public struct BundleDetails
    {
        /// <summary>
        /// Specific file name on disk of the asset bundle.
        /// </summary>
        public string fileName;

        /// <summary>
        /// Cyclic redundancy check of the content contained inside of the asset bundle.
        /// This value will not change between identical asset bundles with different compression options.
        /// </summary>
        public uint crc;

        /// <summary>
        /// The hash version of the content contained inside of the asset bundle.
        /// This value will not change between identical asset bundles with different compression options.
        /// </summary>
        public Hash128 hash;
    }
}