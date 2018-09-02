using System;
using JetBrains.Annotations;
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
        public string FileName { get; set; }

        /// <summary>
        /// Cyclic redundancy check of the content contained inside of the asset bundle.
        /// This value will not change between identical asset bundles with different compression options.
        /// </summary>
        public uint Crc { get; set; }

        /// <summary>
        /// The hash version of the content contained inside of the asset bundle.
        /// This value will not change between identical asset bundles with different compression options.
        /// </summary>
        public Hash128 Hash { get; set; }

        public override bool Equals(object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || typeof(BundleDetails) != obj.GetType()) 
                return false;

            var rhs = (BundleDetails)obj;
            return this == rhs;
        }

        public override int GetHashCode()
        {
            return FileName.GetHashCode() ^ Crc.GetHashCode() ^ Hash.GetHashCode();
        }

        public static bool operator ==(BundleDetails a, BundleDetails b)
        {
            return a.FileName == b.FileName && a.Crc == b.Crc && a.Hash == b.Hash;
        }

        public static bool operator !=(BundleDetails a, BundleDetails b)
        {
            return !(a == b);
        }
    }
}