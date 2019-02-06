using UnityEngine;
using UnityEngine.Experimental.U2D.Common;

namespace UnityEngine.Experimental.U2D.Animation
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(SpriteSkinEntity))]
    public class SpriteSkin : MonoBehaviour
    {
        [SerializeField]
        private Transform m_RootBone;
        [SerializeField]
        private Transform[] m_BoneTransforms;
        [SerializeField]
        private Bounds m_Bounds;
        private SpriteRenderer m_SpriteRenderer;
        private SpriteSkinEntity m_SpriteSkinEntity;
        internal bool ForceSkinning;

#if UNITY_EDITOR
        internal static Events.UnityEvent onDrawGizmos = new Events.UnityEvent();
        private void OnDrawGizmos() { onDrawGizmos.Invoke(); }
#endif
        SpriteSkinEntity spriteSkinEntity
        {
            get
            {
                if (m_SpriteSkinEntity == null)
                    m_SpriteSkinEntity = GetComponent<SpriteSkinEntity>();

                return m_SpriteSkinEntity;
            }
        }

        internal SpriteRenderer spriteRenderer
        {
            get
            {
                if (m_SpriteRenderer == null)
                    m_SpriteRenderer = GetComponent<SpriteRenderer>();
                return m_SpriteRenderer;
            }
        }

        internal Transform[] boneTransforms
        {
            get { return m_BoneTransforms; }
            set { m_BoneTransforms = value; }
        }

        internal Transform rootBone
        {
            get { return m_RootBone; }
            set { m_RootBone = value; }
        }

        internal Bounds bounds
        {
            get { return m_Bounds; }
            set { m_Bounds = value; }
        }

        internal bool isValid
        {
            get { return this.Validate() == SpriteSkinValidationResult.Ready; }
        }

        protected virtual void OnDestroy()
        {
            DeactivateSkinning();
        }

        internal void DeactivateSkinning()
        {
            var sprite = spriteRenderer.sprite;

            if (sprite != null)
                InternalEngineBridge.SetLocalAABB(spriteRenderer, sprite.bounds);

            SpriteRendererDataAccessExtensions.DeactivateDeformableBuffer(spriteRenderer);
        }
    }
}
