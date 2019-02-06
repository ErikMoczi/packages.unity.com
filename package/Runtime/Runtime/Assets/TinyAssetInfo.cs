using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Unity.Tiny
{
    /// <summary>
    /// Editor only representation of an asset.
    /// </summary>
    internal class TinyAssetInfo
    {
        private TinyAssetInfo m_Parent = null;
        private readonly List<TinyAssetInfo> m_Children = new List<TinyAssetInfo>();
        private readonly List<IReference> m_ExplicitReferences = new List<IReference>();
        private readonly List<IReference> m_ImplicitReferences = new List<IReference>();

        public string Name { get; }

        /// <summary>
        /// The object for this asset.
        /// </summary>
        public Object Object { get; }

        /// <summary>
        /// Unique asset path.
        /// </summary>
        public string AssetPath
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.AssetDatabase.GetAssetPath(Object);
#else
                return Object.name;
#endif
            }
        }

        /// <summary>
        /// List of modules that explicitly declare this asset.
        /// </summary>
        public ReadOnlyCollection<IReference> ExplicitReferences => m_ExplicitReferences.AsReadOnly();

        /// <summary>
        /// Parent for this asset (if any).
        /// </summary>
        public TinyAssetInfo Parent
        {
            get { return m_Parent; }
            set
            {
                // Remove previous parent
                if (m_Parent != null)
                {
                    m_Parent.RemoveChild(this);
                }

                // Set new parent
                m_Parent = value;

                // Add to parent's children list
                if (m_Parent != null)
                {
                    m_Parent.AddChild(this);
                }
            }
        }

        /// <summary>
        /// List of sub assets for this asset (e.g. Sprites for a Texture2D).
        /// </summary>
        public IEnumerable<TinyAssetInfo> Children => m_Children.AsReadOnly();

        public TinyAssetInfo(Object @object, string name)
        {
            Object = @object;
            Name = name;
        }

        public void AddExplicitReference(IReference @ref)
        {
            m_ExplicitReferences.Add(@ref);
        }

        public void AddImplicitReference(IReference @ref)
        {
            m_ImplicitReferences.Add(@ref);
        }

        private void AddChild(TinyAssetInfo assetInfo)
        {
            if (m_Children.Contains(assetInfo))
            {
                return;
            }
            m_Children.Add(assetInfo);
        }

        private void RemoveChild(TinyAssetInfo assetInfo)
        {
            m_Children.Remove(assetInfo);
        }

        public static bool operator ==(TinyAssetInfo a, TinyAssetInfo b)
        {
            // If both are null, or both are same instance, return true
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false
            if ((object) a == null || (object) b == null)
            {
                return false;
            }

            // Return true if the fields match
            return a.Object == b.Object;
        }

        public static bool operator !=(TinyAssetInfo a, TinyAssetInfo b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TinyAssetInfo))
            {
                return false;
            }

            var asset = (TinyAssetInfo) obj;

            return Object == asset.Object;
        }

        public override int GetHashCode()
        {
            return Object.GetHashCode();
        }
    }
}
