using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.U2D.Common;
using UnityEngine.Experimental.Rendering;

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
        private Transform m_RootBone;
        [SerializeField]
        private Transform[] m_BoneTransforms;
        private SpriteRenderer m_SpriteRenderer;
        private Sprite m_CurrentSprite;
        private int m_TransformHash = 0;
        private JobHandle m_BoundsHandle;
        private NativeArray<Vector3> m_MinMax;
        private bool m_NeedsUpdateBounds;

#if UNITY_EDITOR
        private SpriteBone[] m_SpriteBones;
        internal SpriteBone[] spriteBones
        {
            get
            {
                if (m_SpriteBones == null)
                    m_SpriteBones = spriteRenderer.sprite.GetBones();

                return m_SpriteBones;
            }
        }

        private ulong m_AssetTimeStamp = 0;

        internal static UnityEvent onDrawGizmos = new UnityEvent();
#endif

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

        internal bool isValid
        {
            get { return this.Validate() == SpriteSkinValidationResult.Ready; }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            ValidateTransformArray();
        }

        private void OnValidate()
        {
            ValidateTransformArray();
        }

        private void ValidateTransformArray()
        {
            if (spriteRenderer.sprite)
            {
                var bindPose = spriteRenderer.sprite.GetBindPoses();

                if (m_BoneTransforms == null || m_BoneTransforms.Length != bindPose.Length)
                    m_BoneTransforms = new Transform[bindPose.Length];
            }
        }

        private void OnDrawGizmos()
        {
            onDrawGizmos.Invoke();
        }

#endif
        private void OnEnable()
        {
            CreatePersistentNativeArrays();

            var min = transform.InverseTransformPoint(spriteRenderer.bounds.min);
            var max = transform.InverseTransformPoint(spriteRenderer.bounds.max);

            UpdateBounds(min, max);
        }

        private void OnDisable()
        {
            DisposePersistentNativeArrays();
            DeactivateSkinning();
        }

        private void OnApplicationQuit()
        {
            DisposePersistentNativeArrays();
        }

        private void CreatePersistentNativeArrays()
        {
            m_MinMax = new NativeArray<Vector3>(2, Allocator.Persistent);
        }

        private void DisposePersistentNativeArrays()
        {
            m_BoundsHandle.Complete();
            m_BoundsHandle = default(JobHandle);
            if (m_MinMax.IsCreated)
                m_MinMax.Dispose();
        }

        private void UpdateBounds(Vector3 min, Vector3 max)
        {
            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            InternalEngineBridge.SetLocalAABB(spriteRenderer, bounds);
        }

        private void UpdateBoundsIfNeeded()
        {
            if (m_NeedsUpdateBounds)
            {
                m_BoundsHandle.Complete();
                m_BoundsHandle = default(JobHandle);
                UpdateBounds(m_MinMax[0], m_MinMax[1]);
                m_NeedsUpdateBounds = false;
            }
        }

        private void DeactivateSkinning()
        {
            m_TransformHash = 0;
            SpriteRendererDataAccessExtensions.DeactivateDeformableBuffer(spriteRenderer);

            if (spriteRenderer.sprite != null)
                InternalEngineBridge.SetLocalAABB(spriteRenderer, spriteRenderer.sprite.bounds);
        }

        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && spriteRenderer.sprite != null)
            {
                var assetTimeStamp = UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(spriteRenderer.sprite)).assetTimeStamp;
                if (m_AssetTimeStamp != assetTimeStamp)
                {
                    m_AssetTimeStamp = assetTimeStamp;
                    DeactivateSkinning();
                }
            }
#endif

            if (m_CurrentSprite != spriteRenderer.sprite)
            {
                m_CurrentSprite = spriteRenderer.sprite;
                DeactivateSkinning();
            }

            if (!spriteRenderer.enabled || !isValid)
            {
                DeactivateSkinning();
                return;
            }

            UpdateBoundsIfNeeded();

            int hashCode = this.CalculateTransformHash();
            if (hashCode == m_TransformHash)
                return;

            var bindPoses = m_CurrentSprite.GetBindPoses();
            var boneWeights = m_CurrentSprite.GetBoneWeights();
            var outputVertices = spriteRenderer.GetDeformableVertices();
            var transformMatrices = new NativeArray<Matrix4x4>(m_BoneTransforms.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < m_BoneTransforms.Length; ++i)
                transformMatrices[i] = m_BoneTransforms[i].localToWorldMatrix;

            var deformJobHandle = SpriteSkinUtility.Deform(m_CurrentSprite.GetVertexAttribute<Vector3>(VertexAttribute.Position), boneWeights, transform.worldToLocalMatrix, bindPoses, transformMatrices, outputVertices);

            m_BoundsHandle = SpriteSkinUtility.CalculateBounds(outputVertices, m_MinMax, deformJobHandle);
            spriteRenderer.UpdateDeformableBuffer(m_BoundsHandle);

            m_TransformHash = hashCode;
            m_NeedsUpdateBounds = true;

#if UNITY_EDITOR
            if (m_SpriteBones != null && m_SpriteBones.Length != bindPoses.Length)
                m_SpriteBones = null;
#endif
        }
    }
}
