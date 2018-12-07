using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class CharacterCache : SkinningObject, IEnumerable<CharacterPartCache>
    {
        [SerializeField]
        private SkeletonCache m_Skeleton;
        [SerializeField]
        private List<CharacterPartCache> m_Parts = new List<CharacterPartCache>();
        [SerializeField]
        private Vector2Int m_Dimension;

        public SkeletonCache skeleton
        {
            get { return m_Skeleton; }
            set { m_Skeleton = value; }
        }

        public virtual CharacterPartCache[] parts
        {
            get { return m_Parts.ToArray(); }
            set { m_Parts = new List<CharacterPartCache>(value); }
        }

        public Vector2Int dimension
        {
            get { return m_Dimension; }
            set { m_Dimension = value; }
        }

        public IEnumerator<CharacterPartCache> GetEnumerator()
        {
            return ((IEnumerable<CharacterPartCache>)m_Parts).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<CharacterPartCache>)m_Parts).GetEnumerator();
        }
    }
}
