using UnityEngine;

namespace Unity.Tiny
{
    internal abstract class TinyScriptableObject : ScriptableObject
    {
        /// <summary>
        /// Set of tiny objects that exist as part of this asset
        /// </summary>
        [SerializeField] private string[] m_Objects;
        [SerializeField] private string m_Hash;
        [SerializeField] private Texture2D m_Icon;
        
        public string[] Objects
        {
            get => m_Objects;
            set => m_Objects = value;
        }

        public string Hash
        {
            get => m_Hash;
            set => m_Hash = value;
        }

        public Texture2D Icon
        {
            get => m_Icon;
            set => m_Icon = value;
        }
    }
}

