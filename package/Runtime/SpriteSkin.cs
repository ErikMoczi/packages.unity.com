using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.U2D;

#if ENABLE_MANAGED_JOBS
using Unity.Collections;
using Unity.Jobs;
#endif

namespace UnityEngine.Experimental.U2D.Animation
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteSkin : MonoBehaviour
    {
        [SerializeField]
        Transform m_RootBone;

        Transform[] m_BoneTransforms;
        SpriteRenderer m_SpriteRenderer;

        SpriteRenderer spriteRenderer
        {
            get
            {
                if (m_SpriteRenderer == null)
                    m_SpriteRenderer = GetComponent<SpriteRenderer>();
                return m_SpriteRenderer;
            }
        }

        public Transform[] boneTransforms
        {
            get { return m_BoneTransforms; }
        }

        public Transform rootBone
        {
            get { return m_RootBone; }
            set
            {
                m_RootBone = value;
                Rebind();
            }
        }

        void Rebind()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
                Debug.LogWarning("Rebind failure. Check spriteRenderer or spriteRenderer.sprite for null");
            if (rootBone != null)
                m_BoneTransforms = SpriteBoneUtility.Rebind(rootBone, spriteRenderer.sprite.GetBones());
        }

        void Awake()
        {
            Rebind();
        }

        void Reset()
        {
            Rebind();
        }

        void Update()
        {
            if (rootBone != null && boneTransforms != null && spriteRenderer.sprite != null)
            {
                try
                {
                    JobHandle jobHandle = SpriteBoneUtility.Deform(spriteRenderer.sprite, spriteRenderer.GetDeformableVertices(), gameObject.transform.worldToLocalMatrix, boneTransforms);
                    spriteRenderer.UpdateDeformableBuffer(jobHandle);
                }
                catch
                {
                    Debug.LogWarning("Deform failure, please ensure SpriteSkin.Rebind is successful.");
                    m_BoneTransforms = null;
                }
            }
        }
    }
}
