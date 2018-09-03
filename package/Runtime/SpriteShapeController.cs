using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using ShapeControlPointExperimental = UnityEngine.Experimental.U2D.ShapeControlPoint;
using UnityEngine.U2D.SpriteShapeClipperLib;
#if UNITY_EDITOR
using UnityEditor.U2D;
#endif

namespace UnityEngine.U2D
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(SpriteShapeRenderer))]
    public class SpriteShapeController : MonoBehaviour
    {
        const float s_ClipperScale = 100000.0f;
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
        int m_CurrentSplineHashCode = 0;

        [SerializeField]
        Spline m_Spline = new Spline();
        [SerializeField]
        SpriteShape m_SpriteShape;

        [SerializeField]
        int m_SplineDetail;
        [SerializeField]
        bool m_AdaptiveUV;

        [SerializeField]
        bool m_UpdateCollider;
        [SerializeField]
        int m_ColliderDetail;
        [SerializeField, Range(-1, 1)]
        float m_ColliderOffset;
        [SerializeField]
        ColliderCornerType m_ColliderCornerType;

        public Spline spline
        {
            get { return m_Spline; }
        }

        public bool autoUpdateCollider
        {
            get { return m_UpdateCollider; }
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

        public ColliderCornerType colliderCornerType
        {
            get { return m_ColliderCornerType; }
            set { m_ColliderCornerType = value; }
        }

        public float colliderOffset
        {
            get { return m_ColliderOffset; }
            set { m_ColliderOffset = value; }
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

            m_Spline.InsertPointAt(0, Vector2.left + Vector2.down);
            m_Spline.InsertPointAt(1, Vector2.left + Vector2.up);
            m_Spline.InsertPointAt(2, Vector2.right + Vector2.up);
            m_Spline.InsertPointAt(3, Vector2.right + Vector2.down);

            m_ColliderDetail = (int)QualityDetail.Low;
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
        }

        void OnDestroy()
        {

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
                    return true;

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

#if UNITY_EDITOR
        public void ResetHash()
        {
            m_CurrentSplineHashCode = 0;
        }
#endif

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
            bool needUpdateSpriteArrays = NeedUpdateSpriteArrays();
            bool spriteShapeParametersChanged = UpdateSpriteShapeParameters();
            bool splineChanged = HasSplineChanged();

            if (needUpdateSpriteArrays || spriteShapeParametersChanged || splineChanged)
                BakeMesh(needUpdateSpriteArrays);

            m_CurrentSpriteShape = spriteShape;
        }

        public void BakeMesh()
        {
            BakeMesh(NeedUpdateSpriteArrays());
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

        void BakeMesh(bool needUpdateSpriteArrays)
        {
            if (needUpdateSpriteArrays)
                UpdateSpriteArrays();

            List<ShapeControlPointExperimental> shapePoints = new List<ShapeControlPointExperimental>();
            List<SpriteShapeMetaData> shapeMetaData = new List<SpriteShapeMetaData>();

            for (int i = 0; i < m_Spline.GetPointCount(); ++i)
            {
                ShapeControlPointExperimental shapeControlPoint;
                shapeControlPoint.position = m_Spline.GetPosition(i);
                shapeControlPoint.leftTangent = m_Spline.GetLeftTangent(i);
                shapeControlPoint.rightTangent = m_Spline.GetRightTangent(i);
                shapeControlPoint.mode = (int)m_Spline.GetTangentMode(i);
                shapePoints.Add(shapeControlPoint);

                SpriteShapeMetaData metaData;
                metaData.bevelCutoff = m_Spline.GetBevelCutoff(i);
                metaData.bevelSize = m_Spline.GetBevelSize(i);
                metaData.corner = m_Spline.GetCorner(i);
                metaData.height = m_Spline.GetHeight(i);
                metaData.spriteIndex = (uint)m_Spline.GetSpriteIndex(i);
                shapeMetaData.Add(metaData);
            }

            if (spriteShapeRenderer != null && ValidatePoints(shapePoints))
            {
                SpriteShapeUtility.GenerateSpriteShape(spriteShapeRenderer, m_CurrentShapeParameters,
                    shapePoints.ToArray(), shapeMetaData.ToArray(), m_AngleRangeInfoArray, m_EdgeSpriteArray,
                    m_CornerSpriteArray);
            }

            if (m_DynamicOcclusionOverriden)
            {
                spriteShapeRenderer.allowOcclusionWhenDynamic = m_DynamicOcclusionLocal;
                m_DynamicOcclusionOverriden = false;
            }
        }

        public void BakeCollider()
        {
            List<IntPoint> path = new List<IntPoint>();

            int splinePointCount = m_Spline.GetPointCount();
            int pathPointCount = splinePointCount;

            if (m_Spline.isOpenEnded)
                pathPointCount--;

            for (int i = 0; i < pathPointCount; ++i)
            {
                int nextIndex = SplineUtility.NextIndex(i, splinePointCount);
                SampleCurve(m_Spline.GetPosition(i), m_Spline.GetRightTangent(i), m_Spline.GetPosition(nextIndex), m_Spline.GetLeftTangent(nextIndex), ref path);
            }

            if (m_ColliderOffset != 0f)
            {
                List<List<IntPoint>> solution = new List<List<IntPoint>>();
                ClipperOffset clipOffset = new ClipperOffset();

                EndType endType = EndType.etClosedPolygon;

                if (m_Spline.isOpenEnded)
                {
                    endType = EndType.etOpenSquare;

                    if (colliderCornerType == ColliderCornerType.Round)
                        endType = EndType.etOpenRound;

                    if (spriteShape && !spriteShape.useSpriteBorders)
                        endType = EndType.etOpenButt;
                }

                clipOffset.ArcTolerance = 200f / m_ColliderDetail;
                clipOffset.AddPath(path, (SpriteShapeClipperLib.JoinType)colliderCornerType, endType);
                clipOffset.Execute(ref solution, s_ClipperScale * m_ColliderOffset);

                if (solution.Count > 0)
                    path = solution[0];
            }

            List<Vector2> pathPoints = new List<Vector2>(path.Count);

            for (int i = 0; i < path.Count; ++i)
            {
                IntPoint ip = path[i];
                pathPoints.Add(new Vector2(ip.X / s_ClipperScale, ip.Y / s_ClipperScale));
            }

            if (polygonCollider)
                polygonCollider.SetPath(0, pathPoints.ToArray());

            if (edgeCollider)
            {
                if (m_ColliderOffset > 0f || m_ColliderOffset < 0f && !m_Spline.isOpenEnded)
                    pathPoints.Add(pathPoints[0]);

                edgeCollider.points = pathPoints.ToArray();
            }
        }

        void SampleCurve(Vector3 startPoint, Vector3 startTangent, Vector3 endPoint, Vector3 endTangent, ref List<IntPoint> path)
        {
            if (startTangent.sqrMagnitude > 0f || endTangent.sqrMagnitude > 0f)
            {
                for (int j = 0; j <= m_ColliderDetail; ++j)
                {
                    float t = j / (float)m_ColliderDetail;
                    Vector3 newPoint = BezierUtility.BezierPoint(startPoint, startTangent + startPoint, endTangent + endPoint, endPoint, t) * s_ClipperScale;

                    path.Add(new IntPoint((System.Int64)newPoint.x, (System.Int64)newPoint.y));
                }
            }
            else
            {
                Vector3 newPoint = startPoint * s_ClipperScale;
                path.Add(new IntPoint((System.Int64)newPoint.x, (System.Int64)newPoint.y));

                newPoint = endPoint * s_ClipperScale;
                path.Add(new IntPoint((System.Int64)newPoint.x, (System.Int64)newPoint.y));
            }
        }

        bool UpdateSpriteShapeParameters()
        {
            Matrix4x4 transformMatrix = Matrix4x4.identity;
            Texture2D fillTexture = null;
            uint fillScale = 0;
            float bevelCutoff = 0f;
            float bevelSize = 0f;
            bool smartSprite = true;
            float borderPivot = 0f;
            bool carpet = !m_Spline.isOpenEnded;
            bool adaptiveUV = m_AdaptiveUV;
            bool spriteBorders = false;
            uint splineDetail = (uint)m_SplineDetail;
            float angleThreshold = 30.0f;

            if (spriteShape)
            {
                if (spriteShape.worldSpaceUVs)
                    transformMatrix = transform.localToWorldMatrix;

                fillTexture = spriteShape.fillTexture;
                fillScale = (uint) spriteShape.fillPixelsPerUnit;
                bevelCutoff = spriteShape.bevelCutoff;
                bevelSize = spriteShape.bevelSize;
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
                m_CurrentShapeParameters.bevelCutoff != bevelCutoff ||
                m_CurrentShapeParameters.bevelSize != bevelSize ||
                m_CurrentShapeParameters.borderPivot != borderPivot ||
                m_CurrentShapeParameters.carpet != carpet ||
                m_CurrentShapeParameters.fillScale != fillScale ||
                m_CurrentShapeParameters.fillTexture != fillTexture ||
                m_CurrentShapeParameters.smartSprite != smartSprite ||
                m_CurrentShapeParameters.splineDetail != splineDetail ||
                m_CurrentShapeParameters.spriteBorders != spriteBorders ||
                m_CurrentShapeParameters.transform != transformMatrix;

            m_CurrentShapeParameters.adaptiveUV = adaptiveUV;
            m_CurrentShapeParameters.angleThreshold = angleThreshold;
            m_CurrentShapeParameters.bevelCutoff = bevelCutoff;
            m_CurrentShapeParameters.bevelSize = bevelSize;
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
                    AngleRange angleRange = sortedAngleRanges[i];
                    AngleRangeInfo angleRangeInfo = new AngleRangeInfo();
                    angleRangeInfo.start = angleRange.start;
                    angleRangeInfo.end = angleRange.end;
                    angleRangeInfo.order = (uint)angleRange.order;
                    List<int> spriteIndices = new List<int>();
                    bool validSpritesFound = false;
                    foreach (Sprite edgeSprite in angleRange.sprites)
                    {
                        edgeSpriteList.Add(edgeSprite);
                        spriteIndices.Add(edgeSpriteList.Count - 1);
                        if (edgeSprite != null)
                            validSpritesFound = true;
                    }
                    if (validSpritesFound)
                    {
                        angleRangeInfo.sprites = spriteIndices.ToArray();
                        angleRangeInfoList.Add(angleRangeInfo);
                    }
                }

                for (int i = 0; i < spriteShape.cornerSprites.Count; i++)
                {
                    CornerSprite cornerSprite = spriteShape.cornerSprites[i];
                    cornerSpriteList.Add(cornerSprite.sprites[0]);
                    m_CurrentCornerSprites.Add(cornerSprite.Clone() as CornerSprite);
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
