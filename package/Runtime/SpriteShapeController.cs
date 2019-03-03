using System.Collections.Generic;
using UnityEngine.Experimental.U2D;
using ShapeControlPointExperimental = UnityEngine.Experimental.U2D.ShapeControlPoint;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor.U2D;
#endif

namespace UnityEngine.U2D
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(SpriteShapeRenderer))]
    [DisallowMultipleComponent]
    [HelpURLAttribute("https://docs.unity3d.com/Packages/com.unity.2d.spriteshape@1.0/manual/index.html")]
    public class SpriteShapeController : MonoBehaviour
    {
        const float s_DistanceTolerance = 0.001f;

        PolygonCollider2D m_PolygonCollider2D;
        EdgeCollider2D m_EdgeCollider2D;

        Sprite[] m_EdgeSpriteArray;
        Sprite[] m_CornerSpriteArray;
        AngleRangeInfo[] m_AngleRangeInfoArray;

        private bool m_DynamicOcclusionLocal;
        private bool m_DynamicOcclusionOverriden;

        SpriteShape m_CurrentSpriteShape;
        SpriteShapeRenderer m_SpriteShapeRenderer;
        SpriteShapeParameters m_CurrentShapeParameters;
        List<AngleRange> m_CurrentAngleRanges = new List<AngleRange>();
        List<CornerSprite> m_CurrentCornerSprites = new List<CornerSprite>();
        NativeArray<float2> m_ColliderData;

        int m_CurrentSplineHashCode = 0;
        public bool m_LegacyGenerator = false;

        [SerializeField]
        Spline m_Spline = new Spline();
        [SerializeField]
        SpriteShape m_SpriteShape;

        [SerializeField]
        float m_FillPixelPerUnit = 100.0f;
        [SerializeField]
        float m_StretchTiling = 1.0f;
        [SerializeField]
        int m_SplineDetail;
        [SerializeField]
        bool m_AdaptiveUV;
        [SerializeField]
        bool m_StretchUV;
        [SerializeField]
        bool m_WorldSpaceUV;


        [SerializeField]
        int m_ColliderDetail;
        [SerializeField, Range(-0.5f, 0.5f)]
        float m_ColliderOffset;
        [SerializeField]
        bool m_UpdateCollider = true;
        [SerializeField]
        bool m_OptimizeCollider = true;

        public bool worldSpaceUVs
        {
            get { return m_WorldSpaceUV; }
            set { m_WorldSpaceUV = value; }
        }
        public float fillPixelsPerUnit
        {
            get { return m_FillPixelPerUnit; }
            set { m_FillPixelPerUnit = value; }
        }
        public float stretchTiling
        {
            get { return m_StretchTiling; }
            set { m_StretchTiling = value; }
        }
        public Spline spline
        {
            get { return m_Spline; }
        }

        public SpriteShape spriteShape
        {
            get { return m_SpriteShape; }
            set { m_SpriteShape = value; }
        }

        public SpriteShapeRenderer spriteShapeRenderer
        {
            get
            {
                if (!m_SpriteShapeRenderer)
                    m_SpriteShapeRenderer = GetComponent<SpriteShapeRenderer>();

                return m_SpriteShapeRenderer;
            }
        }

        public PolygonCollider2D polygonCollider
        {
            get
            {
                if (!m_PolygonCollider2D)
                    m_PolygonCollider2D = GetComponent<PolygonCollider2D>();

                return m_PolygonCollider2D;
            }
        }

        public EdgeCollider2D edgeCollider
        {
            get
            {
                if (!m_EdgeCollider2D)
                    m_EdgeCollider2D = GetComponent<EdgeCollider2D>();

                return m_EdgeCollider2D;
            }
        }

        public int splineDetail
        {
            get { return m_SplineDetail; }
            set { m_SplineDetail = Mathf.Max(0, value); }
        }

        public int colliderDetail
        {
            get { return m_ColliderDetail; }
            set { m_ColliderDetail = Mathf.Max(0, value); }
        }

        public float colliderOffset
        {
            get { return m_ColliderOffset; }
            set { m_ColliderOffset = value; }
        }

        public bool autoUpdateCollider
        {
            get
            {
                bool hasCollider = (edgeCollider != null) || (polygonCollider != null);
                return hasCollider && m_UpdateCollider;
            }
            set { m_UpdateCollider = value; }
        }
        public bool optimizeCollider
        {
            get { return m_OptimizeCollider; }
        }

        private void DisposeNativeArrays()
        {
            if (m_ColliderData.IsCreated)
                m_ColliderData.Dispose();
        }

        private void OnApplicationQuit()
        {
            DisposeNativeArrays();
        }

        static void SmartDestroy(UnityEngine.Object o)
        {
            if (o == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(o);
            else
#endif
                Destroy(o);
        }

        void Reset()
        {
            m_SplineDetail = (int)QualityDetail.High;
            m_AdaptiveUV = true;
            m_StretchUV = false;
            m_FillPixelPerUnit = 100f;

            m_Spline.InsertPointAt(0, Vector2.left + Vector2.down);
            m_Spline.InsertPointAt(1, Vector2.left + Vector2.up);
            m_Spline.InsertPointAt(2, Vector2.right + Vector2.up);
            m_Spline.InsertPointAt(3, Vector2.right + Vector2.down);

            m_ColliderDetail = (int)QualityDetail.High;
        }

        void OnEnable()
        {
            spriteShapeRenderer.enabled = true;
            m_DynamicOcclusionOverriden = true;
            m_DynamicOcclusionLocal = spriteShapeRenderer.allowOcclusionWhenDynamic;
            spriteShapeRenderer.allowOcclusionWhenDynamic = false;
        }

        void OnDisable()
        {
            spriteShapeRenderer.enabled = false;
            DisposeNativeArrays();
        }

        void OnDestroy()
        {

        }

        void ValidateSpriteShapeData()
        {
            if (spriteShape == null)
            {
                if (m_EdgeSpriteArray != null)
                    m_EdgeSpriteArray = null;
                if (m_CornerSpriteArray != null)
                    m_CornerSpriteArray = null;
                if (m_AngleRangeInfoArray != null)
                    m_AngleRangeInfoArray = null;
                m_CurrentAngleRanges.Clear();
                m_CurrentCornerSprites.Clear();
            }
        }

        bool SpriteShapeChanged()
        {
            return m_CurrentSpriteShape != spriteShape;
        }

        bool NeedUpdateSpriteArrays()
        {
            if (m_EdgeSpriteArray == null || m_CornerSpriteArray == null || m_AngleRangeInfoArray == null)
                return true;

            if (spriteShape)
            {
                if (m_CurrentAngleRanges.Count != spriteShape.angleRanges.Count)
                    return true;

                if (m_CurrentCornerSprites.Count != spriteShape.cornerSprites.Count)
                {
                    for (int i = 0; i < spriteShape.cornerSprites.Count; ++i)
                    {
                        var cornerSprite = spriteShape.cornerSprites[i].sprites[0];
                        if (cornerSprite != null)
                            return true;
                    }
                }

                for (int i = 0; i < m_CurrentAngleRanges.Count; i++)
                {
                    if (!m_CurrentAngleRanges[i].Equals(spriteShape.angleRanges[i]))
                        return true;
                }

                for (int i = 0; i < m_CurrentCornerSprites.Count; i++)
                {
                    if (!m_CurrentCornerSprites[i].Equals(spriteShape.cornerSprites[i]))
                        return true;
                }
            }

            return SpriteShapeChanged();
        }

        bool HasSplineChanged()
        {
            int hashCode = m_Spline.GetHashCode();

            if (m_CurrentSplineHashCode != hashCode)
            {
                m_CurrentSplineHashCode = hashCode;
                return true;
            }
            return false;
        }

        void OnWillRenderObject()
        {
            ValidateSpriteShapeData();
            bool needUpdateSpriteArrays = NeedUpdateSpriteArrays();
            bool spriteShapeParametersChanged = UpdateSpriteShapeParameters();
            bool splineChanged = HasSplineChanged();

            BakeCollider();
            if (needUpdateSpriteArrays || spriteShapeParametersChanged || splineChanged)
                BakeMesh(needUpdateSpriteArrays);

            m_CurrentSpriteShape = spriteShape;

        }

        public void RefreshSpriteShape()
        {
            m_CurrentSplineHashCode = 0;
        }

        public JobHandle BakeMesh()
        {
            UpdateSpriteShapeParameters();
            return BakeMesh(NeedUpdateSpriteArrays());
        }

        // Ensure Neighbor points are not too close to each other.
        private bool ValidatePoints(List<ShapeControlPointExperimental> shapePoints)
        {
            for (int i = 0; i < shapePoints.Count - 1; ++i)
            {
                var vec = shapePoints[i].position - shapePoints[i + 1].position;
                if (vec.sqrMagnitude < s_DistanceTolerance)
                {
                    Debug.LogWarningFormat("Control points {0} & {1} are too close to each other. SpriteShape will not be generated.", i, i + 1);
                    return false;
                }
            }
            return true;
        }

        JobHandle BakeMesh(bool needUpdateSpriteArrays)
        {
            JobHandle jobHandle = default;
            if (needUpdateSpriteArrays)
                UpdateSpriteArrays();

            List<ShapeControlPointExperimental> shapePoints = new List<ShapeControlPointExperimental>();
            List<SpriteShapeMetaData> shapeMetaData = new List<SpriteShapeMetaData>();
            int pointCount = m_Spline.GetPointCount();
            for (int i = 0; i < pointCount; ++i)
            {
                ShapeControlPointExperimental shapeControlPoint;
                shapeControlPoint.position = m_Spline.GetPosition(i);
                shapeControlPoint.leftTangent = m_Spline.GetLeftTangent(i);
                shapeControlPoint.rightTangent = m_Spline.GetRightTangent(i);
                shapeControlPoint.mode = (int)m_Spline.GetTangentMode(i);
                shapePoints.Add(shapeControlPoint);

                SpriteShapeMetaData metaData;
                metaData.corner = m_Spline.GetCorner(i);
                metaData.height = m_Spline.GetHeight(i);
                metaData.spriteIndex = (uint)m_Spline.GetSpriteIndex(i);
                metaData.bevelCutoff = 0;
                metaData.bevelSize = 0;
                shapeMetaData.Add(metaData);
            }

            if (spriteShapeRenderer != null && ValidatePoints(shapePoints))
            {
                if (m_LegacyGenerator)
                {
                    SpriteShapeUtility.GenerateSpriteShape(spriteShapeRenderer, m_CurrentShapeParameters,
                        shapePoints.ToArray(), shapeMetaData.ToArray(), m_AngleRangeInfoArray, m_EdgeSpriteArray,
                        m_CornerSpriteArray);
                }
                else
                {
                    // Allow max quads for each segment to 128.
                    int maxArrayCount = (int)(pointCount * 256 * m_CurrentShapeParameters.splineDetail);
                    if (m_ColliderData.IsCreated)
                        m_ColliderData.Dispose();
                    m_ColliderData = new NativeArray<float2>(maxArrayCount, Allocator.Persistent);

                    NativeArray<ushort> indexArray;
                    NativeSlice<Vector3> posArray;
                    NativeSlice<Vector2> uv0Array;
                    spriteShapeRenderer.GetChannels(maxArrayCount, out indexArray, out posArray, out uv0Array);
                    NativeArray<Bounds> bounds = spriteShapeRenderer.GetBounds();
                    NativeArray<SpriteShapeSegment> geomArray = spriteShapeRenderer.GetSegments(shapePoints.Count * 8);

                    var spriteShapeJob = new SpriteShapeGenerator()
                    {
                        m_GeomArray = geomArray,
                        m_IndexArray = indexArray,
                        m_PosArray = posArray,
                        m_Uv0Array = uv0Array,
                        m_ColliderPoints = m_ColliderData,
                        m_Bounds = bounds
                    };
                    spriteShapeJob.Prepare(this, m_CurrentShapeParameters, maxArrayCount, shapePoints.ToArray(), shapeMetaData.ToArray(), m_AngleRangeInfoArray, m_EdgeSpriteArray, m_CornerSpriteArray);

                    List<Sprite> sprites = new List<Sprite>();
                    sprites.AddRange(m_EdgeSpriteArray);
                    sprites.AddRange(m_CornerSpriteArray);
                    jobHandle = spriteShapeJob.Schedule();
                    spriteShapeRenderer.Prepare(jobHandle, m_CurrentShapeParameters, sprites.ToArray());
                    JobHandle.ScheduleBatchedJobs();
                }
            }

            if (m_DynamicOcclusionOverriden)
            {
                spriteShapeRenderer.allowOcclusionWhenDynamic = m_DynamicOcclusionLocal;
                m_DynamicOcclusionOverriden = false;
            }
            return jobHandle;
        }

        public void BakeCollider()
        {
            if (m_ColliderData.IsCreated && autoUpdateCollider)
            {
                int maxCount = short.MaxValue - 1;
                float2 last = (float2)0;
                List<Vector2> m_ColliderSegment = new List<Vector2>();
                for (int i = 0; i < maxCount; ++i)
                {
                    float2 now = m_ColliderData[i];
                    if (!math.any(last) && !math.any(now))
                        break;
                    m_ColliderSegment.Add(new Vector2(now.x, now.y));
                }

                EdgeCollider2D edge = GetComponent<EdgeCollider2D>();
                if (edge != null)
                    edge.points = m_ColliderSegment.ToArray();
                PolygonCollider2D poly = GetComponent<PolygonCollider2D>();
                if (poly != null)
                    poly.points = m_ColliderSegment.ToArray();

                m_ColliderData.Dispose();
#if UNITY_EDITOR
                if (UnityEditor.SceneView.lastActiveSceneView != null)
                    UnityEditor.SceneView.lastActiveSceneView.Repaint();
#endif
            }
        }

        public bool UpdateSpriteShapeParameters()
        {
            Matrix4x4 transformMatrix = Matrix4x4.identity;
            Texture2D fillTexture = null;
            uint fillScale = 0;
            uint splineDetail = (uint)m_SplineDetail;
            float angleThreshold = 30.0f;
            float borderPivot = 0f;
            bool smartSprite = true;
            bool carpet = !m_Spline.isOpenEnded;
            bool adaptiveUV = m_AdaptiveUV;
            bool stretchUV = m_StretchUV;
            bool spriteBorders = false;

            if (spriteShape)
            {
                if (worldSpaceUVs)
                    transformMatrix = transform.localToWorldMatrix;

                fillTexture = spriteShape.fillTexture;
                fillScale = stretchUV ? (uint)stretchTiling : (uint)fillPixelsPerUnit;
                borderPivot = spriteShape.fillOffset;
                spriteBorders = spriteShape.useSpriteBorders;
                // If Corners are enabled, set smart-sprite to false.
                if (spriteShape.cornerSprites.Count > 0)
                    smartSprite = false;
            }
            else
            {
#if UNITY_EDITOR
                if (fillTexture == null)
                    fillTexture = UnityEditor.EditorGUIUtility.whiteTexture;
                fillScale = 100;
#endif
            }

            bool changed = m_CurrentShapeParameters.adaptiveUV != adaptiveUV ||
                m_CurrentShapeParameters.angleThreshold != angleThreshold ||
                m_CurrentShapeParameters.borderPivot != borderPivot ||
                m_CurrentShapeParameters.carpet != carpet ||
                m_CurrentShapeParameters.fillScale != fillScale ||
                m_CurrentShapeParameters.fillTexture != fillTexture ||
                m_CurrentShapeParameters.smartSprite != smartSprite ||
                m_CurrentShapeParameters.splineDetail != splineDetail ||
                m_CurrentShapeParameters.spriteBorders != spriteBorders ||
                m_CurrentShapeParameters.transform != transformMatrix ||
                m_CurrentShapeParameters.stretchUV != stretchUV;

            m_CurrentShapeParameters.adaptiveUV = adaptiveUV;
            m_CurrentShapeParameters.stretchUV = stretchUV;
            m_CurrentShapeParameters.angleThreshold = angleThreshold;
            m_CurrentShapeParameters.borderPivot = borderPivot;
            m_CurrentShapeParameters.carpet = carpet;
            m_CurrentShapeParameters.fillScale = fillScale;
            m_CurrentShapeParameters.fillTexture = fillTexture;
            m_CurrentShapeParameters.smartSprite = smartSprite;
            m_CurrentShapeParameters.splineDetail = splineDetail;
            m_CurrentShapeParameters.spriteBorders = spriteBorders;
            m_CurrentShapeParameters.transform = transformMatrix;

            return changed;
        }

        void UpdateSpriteArrays()
        {
            List<Sprite> edgeSpriteList = new List<Sprite>();
            List<Sprite> cornerSpriteList = new List<Sprite>();
            List<AngleRangeInfo> angleRangeInfoList = new List<AngleRangeInfo>();

            m_CurrentAngleRanges.Clear();
            m_CurrentCornerSprites.Clear();

            if (spriteShape)
            {
                List<AngleRange> sortedAngleRanges = new List<AngleRange>(spriteShape.angleRanges);
                sortedAngleRanges.Sort((a, b) => a.order.CompareTo(b.order));

                for (int i = 0; i < sortedAngleRanges.Count; i++)
                {
                    bool validSpritesFound = false;
                    AngleRange angleRange = sortedAngleRanges[i];
                    foreach (Sprite edgeSprite in angleRange.sprites)
                    {
                        if (edgeSprite != null)
                        {
                            validSpritesFound = true;
                            break;
                        }
                    }

                    if (validSpritesFound)
                    {
                        AngleRangeInfo angleRangeInfo = new AngleRangeInfo();
                        angleRangeInfo.start = angleRange.start;
                        angleRangeInfo.end = angleRange.end;
                        angleRangeInfo.order = (uint)angleRange.order;
                        List<int> spriteIndices = new List<int>();
                        foreach (Sprite edgeSprite in angleRange.sprites)
                        {
                            edgeSpriteList.Add(edgeSprite);
                            spriteIndices.Add(edgeSpriteList.Count - 1);
                        }
                        angleRangeInfo.sprites = spriteIndices.ToArray();
                        angleRangeInfoList.Add(angleRangeInfo);
                    }
                }

                bool validCornerSpritesFound = false;
                foreach (CornerSprite cornerSprite in spriteShape.cornerSprites)
                {
                    if (cornerSprite.sprites[0] != null)
                    {
                        validCornerSpritesFound = true;
                        break;
                    }
                }

                if (validCornerSpritesFound)
                {
                    for (int i = 0; i < spriteShape.cornerSprites.Count; i++)
                    {
                        CornerSprite cornerSprite = spriteShape.cornerSprites[i];
                        cornerSpriteList.Add(cornerSprite.sprites[0]);
                        m_CurrentCornerSprites.Add(cornerSprite.Clone() as CornerSprite);
                    }
                }

                for (int i = 0; i < spriteShape.angleRanges.Count; i++)
                {
                    AngleRange angleRange = spriteShape.angleRanges[i];
                    m_CurrentAngleRanges.Add(angleRange.Clone() as AngleRange);
                }
            }

            m_EdgeSpriteArray = edgeSpriteList.ToArray();
            m_CornerSpriteArray = cornerSpriteList.ToArray();
            m_AngleRangeInfoArray = angleRangeInfoList.ToArray();
        }

        Texture2D GetTextureFromIndex(int index)
        {
            if (index == 0)
                return spriteShape ? spriteShape.fillTexture : null;

            --index;
            if (index < m_EdgeSpriteArray.Length)
                return GetSpriteTexture(m_EdgeSpriteArray[index]);

            index -= m_EdgeSpriteArray.Length;
            return GetSpriteTexture(m_CornerSpriteArray[index]);
        }

        Texture2D GetSpriteTexture(Sprite sprite)
        {
            if (sprite)
            {
#if UNITY_EDITOR
                return UnityEditor.Sprites.SpriteUtility.GetSpriteTexture(sprite, sprite.packed);
#else
                return sprite.texture;
#endif
            }

            return null;
        }
    }
}
