using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.U2D.Common;

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
        NativeArray<Vector3> m_MinMax;
        JobHandle? m_BoundsHandle = null;
        int m_TransformHash = 0;

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

        void UpdateBoundsIfNeeded()
        {
            if (m_BoundsHandle.HasValue)
            {
                m_BoundsHandle.Value.Complete();
                if (boneTransforms != null)
                {
                    Bounds bounds = new Bounds();
                    bounds.SetMinMax(m_MinMax[0], m_MinMax[1]);
                    InternalEngineBridge.SetLocalAABB(spriteRenderer, bounds);
                }
                m_MinMax.Dispose();
                m_BoundsHandle = null;
            }
        }

        void OnDisable()
        {
            if (m_BoundsHandle.HasValue)
                m_BoundsHandle.Value.Complete();
            if (m_MinMax.IsCreated)
                m_MinMax.Dispose();
        }

        void Update()
        {

            UpdateBoundsIfNeeded();

            if (rootBone != null && boneTransforms != null && spriteRenderer.sprite != null)
            {
                int hashCode = SpriteBoneUtility.BoneTransformsHash(m_BoneTransforms);
                if (hashCode != m_TransformHash)
                {
                    try
                    {
                        var deformedVertices = spriteRenderer.GetDeformableVertices();
                        JobHandle deformJobHandle = SpriteBoneUtility.Deform(spriteRenderer.sprite, deformedVertices, gameObject.transform.worldToLocalMatrix, boneTransforms);
                        m_MinMax = new NativeArray<Vector3>(2, Allocator.Persistent);
                        m_BoundsHandle = SpriteBoneUtility.CalculateBounds(deformedVertices, m_MinMax, deformJobHandle);
                        spriteRenderer.UpdateDeformableBuffer(m_BoundsHandle.Value);
                        m_TransformHash = hashCode;
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
}
