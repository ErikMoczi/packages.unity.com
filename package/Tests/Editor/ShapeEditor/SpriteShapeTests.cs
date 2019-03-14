using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Unity.Collections;
using UnityEngine.Experimental.U2D;
using Unity.Jobs;

namespace UnityEditor.U2D.SpriteShapeTest
{
    // ToDo: Once we migrate to ECS, migrate all native tests to this class too.
    public class SpriteShapeTests
    {
        private GameObject go;
        private SpriteShape spriteShape;
        private SpriteShapeController spriteShapeController;

        [SetUp]
        public void Setup()
        {
            go = new GameObject("TestObject");
            spriteShapeController = go.AddComponent<SpriteShapeController>();
            spriteShapeController.spriteShape = Resources.Load<SpriteShape>("Goo");
        }

        [TearDown]
        public void Teardown()
        {
            GameObject.DestroyImmediate(go);
        }

        private static int GetSegmentCount(NativeArray<SpriteShapeSegment> sa, ref int indexCount, ref int vertexCount)
        {
            int segmentCount = 0, successiveZeroes = 0;
            indexCount = vertexCount = 0;
            for (int i = 0; i < sa.Length && successiveZeroes < 2; ++i)
            {
                if (sa[i].spriteIndex != -1)
                    if (sa[i].indexCount == 0)
                        successiveZeroes++;            
                    else
                        segmentCount++;
                else
                    segmentCount++;
                indexCount += sa[i].indexCount;
                vertexCount += sa[i].vertexCount;
            }
            return segmentCount;
        }

        private static void VerifySegments(NativeArray<SpriteShapeSegment> sa, SpriteShapeSegment[] segments)
        {
            for (int i = 0; i < segments.Length; ++i)
            { 
                Assert.AreEqual(sa[i].geomIndex, segments[i].geomIndex);
                Assert.AreEqual(sa[i].indexCount, segments[i].indexCount);
                Assert.AreEqual(sa[i].vertexCount, segments[i].vertexCount);
                Assert.AreEqual(sa[i].spriteIndex, segments[i].spriteIndex);
            }
        }

        private static void VerifyNativeSliceVector3Data(NativeSlice<Vector3> naData, Vector3[] expectedData, float tolerance)
        {
            for (int i = 0; i < expectedData.Length; ++i)
            {
                Assert.True(Mathf.Abs(naData[i].x - expectedData[i].x) < tolerance);
                Assert.True(Mathf.Abs(naData[i].y - expectedData[i].y) < tolerance);
                Assert.True(Mathf.Abs(naData[i].z - expectedData[i].z) < tolerance);
            }
        }

        private static void VerifyNativeSliceVector2Data(NativeSlice<Vector2> naData, Vector2[] expectedData, float tolerance)
        {
            for (int i = 0; i < expectedData.Length; ++i)
            { 
                Assert.True(Mathf.Abs(naData[i].x - expectedData[i].x) < tolerance);
                Assert.True(Mathf.Abs(naData[i].y - expectedData[i].y) < tolerance);
            }
        }

        private static void VerifyGeneratedGeometry(SpriteShapeController spriteShapeController, SpriteShapeSegment[] segments, Vector3[] posArrayData, Vector2[] uv0ArrayData)
        {
            spriteShapeController.UpdateSpriteShapeParameters();
            JobHandle jobHandle = spriteShapeController.BakeMesh();
            jobHandle.Complete();

            int indexCount = 0, vertexCount = 0;
            NativeArray<SpriteShapeSegment> segmentArray = spriteShapeController.spriteShapeRenderer.GetSegments(spriteShapeController.spline.GetPointCount() * 8);
            int segmentCount = GetSegmentCount(segmentArray, ref indexCount, ref vertexCount);

            NativeSlice<Vector3> posArray;
            NativeSlice<Vector2> uv0Array;
            NativeArray<ushort> indexArray;
            int maxArrayCount = spriteShapeController.spline.GetPointCount() * 256 * spriteShapeController.splineDetail;
            spriteShapeController.spriteShapeRenderer.GetChannels(maxArrayCount, out indexArray, out posArray, out uv0Array);

            VerifySegments(segmentArray, segments);
            VerifyNativeSliceVector3Data(posArray, posArrayData, 0.1f);
            VerifyNativeSliceVector2Data(uv0Array, uv0ArrayData, 0.1f);
        }

        [Test, Description("Test Simple Strip")]
        public void SpriteShape_VerifyGeneratedGeometrySimpleStrip()
        {
            spriteShapeController.spline.Clear();
            spriteShapeController.spline.InsertPointAt(0, new Vector3(10.0f, 0, 0));
            spriteShapeController.spline.InsertPointAt(1, new Vector3(-10.0f, 0, 0));
            spriteShapeController.splineDetail = 4;

            SpriteShapeSegment[] segments = { new SpriteShapeSegment{ geomIndex = 0, indexCount = 0, vertexCount = 0, spriteIndex = -1 }, new SpriteShapeSegment{ geomIndex = 1, indexCount = 48, vertexCount = 32, spriteIndex = 0 } };
            Vector3[] posArrayData = { new Vector3(10.0f, 0.9f, 0.0f), new Vector3(6.7f, 0.9f, 0.0f), new Vector3(10.0f, -0.9f, 0.0f), new Vector3(6.7f, -0.9f, 0.0f), new Vector3(6.7f, 0.9f, 0.0f), new Vector3(4.8f, 0.9f, 0.0f), new Vector3(6.7f, -0.9f, 0.0f), new Vector3(4.8f, -0.9f, 0.0f), new Vector3(4.8f, 0.9f, 0.0f), new Vector3(3.3f, 0.9f, 0.0f), new Vector3(4.8f, -0.9f, 0.0f), new Vector3(3.3f, -0.9f, 0.0f), new Vector3(3.3f, 0.9f, 0.0f), new Vector3(0.0f, 0.9f, 0.0f), new Vector3(3.3f, -0.9f, 0.0f), new Vector3(0.0f, -0.9f, 0.0f), new Vector3(0.0f, 0.9f, 0.0f), new Vector3(-3.3f, 0.9f, 0.0f), new Vector3(0.0f, -0.9f, 0.0f), new Vector3(-3.3f, -0.9f, 0.0f), new Vector3(-3.3f, 0.9f, 0.0f), new Vector3(-4.8f, 0.9f, 0.0f), new Vector3(-3.3f, -0.9f, 0.0f), new Vector3(-4.8f, -0.9f, 0.0f), new Vector3(-4.8f, 0.9f, 0.0f), new Vector3(-6.7f, 0.9f, 0.0f), new Vector3(-4.8f, -0.9f, 0.0f), new Vector3(-6.7f, -0.9f, 0.0f), new Vector3(-6.7f, 0.9f, 0.0f), new Vector3(-10.0f, 0.9f, 0.0f), new Vector3(-6.7f, -0.9f, 0.0f), new Vector3(-10.0f, -0.9f, 0.0f) };
            Vector2[] uv0ArrayData = { new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.6f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.6f, 1.0f), new Vector2(0.6f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.6f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.4f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.4f, 1.0f), new Vector2(0.4f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.4f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f) };

            VerifyGeneratedGeometry(spriteShapeController, segments, posArrayData, uv0ArrayData);
        }

        [Test, Description("Test Closed Shape")]
        public void SpriteShape_VerifyGeneratedGeometryClosedShape()
        {
            spriteShapeController.spline.Clear();
            spriteShapeController.spline.InsertPointAt(0, new Vector3(-10.0f, -10.0f, 0));
            spriteShapeController.spline.SetCorner(0, false);
            spriteShapeController.spline.InsertPointAt(1, new Vector3(-10.0f,  10.0f, 0));
            spriteShapeController.spline.SetCorner(1, false);
            spriteShapeController.spline.InsertPointAt(2, new Vector3( 10.0f,  10.0f, 0));
            spriteShapeController.spline.SetCorner(2, false);
            spriteShapeController.spline.InsertPointAt(3, new Vector3( 10.0f, -10.0f, 0));
            spriteShapeController.spline.SetCorner(3, false);
            spriteShapeController.spline.isOpenEnded = false;
            spriteShapeController.splineDetail = 4;

            SpriteShapeSegment[] segments = { new SpriteShapeSegment { geomIndex = 0, indexCount = 12, vertexCount = 6, spriteIndex = -1 }, new SpriteShapeSegment { geomIndex = 1, indexCount = 48, vertexCount = 32, spriteIndex = 0 }, new SpriteShapeSegment { geomIndex = 2, indexCount = 48, vertexCount = 32, spriteIndex = 1 }, new SpriteShapeSegment { geomIndex = 3, indexCount = 48, vertexCount = 32, spriteIndex = 1 }, new SpriteShapeSegment { geomIndex = 4, indexCount = 48, vertexCount = 32, spriteIndex = 0 } };
            Vector3[] posArrayData = { new Vector3(4.8f, -10.0f, 0.0f), new Vector3(10.0f, 10.0f, 0.0f), new Vector3(10.0f, -10.0f, 0.0f), new Vector3(-4.8f, -10.0f, 0.0f), new Vector3(-10.0f, -10.0f, 0.0f), new Vector3(-10.0f, 10.0f, 0.0f), new Vector3(-9.2f, -10.0f, 0.0f), new Vector3(-9.1f, -6.7f, 0.0f), new Vector3(-10.9f, -10.0f, 0.0f), new Vector3(-10.9f, -6.7f, 0.0f), new Vector3(-9.1f, -6.7f, 0.0f), new Vector3(-9.1f, -4.8f, 0.0f), new Vector3(-10.9f, -6.7f, 0.0f), new Vector3(-10.9f, -4.8f, 0.0f), new Vector3(-9.1f, -4.8f, 0.0f), new Vector3(-9.1f, -3.3f, 0.0f), new Vector3(-10.9f, -4.8f, 0.0f), new Vector3(-10.9f, -3.3f, 0.0f), new Vector3(-9.1f, -3.3f, 0.0f), new Vector3(-9.1f, 0.0f, 0.0f), new Vector3(-10.9f, -3.3f, 0.0f), new Vector3(-10.9f, 0.0f, 0.0f), new Vector3(-9.1f, 0.0f, 0.0f), new Vector3(-9.2f, 3.3f, 0.0f), new Vector3(-10.9f, 0.0f, 0.0f), new Vector3(-10.9f, 3.3f, 0.0f), new Vector3(-9.2f, 3.3f, 0.0f), new Vector3(-9.2f, 4.8f, 0.0f), new Vector3(-10.9f, 3.3f, 0.0f), new Vector3(-10.9f, 4.8f, 0.0f), new Vector3(-9.2f, 4.8f, 0.0f), new Vector3(-9.2f, 6.7f, 0.0f), new Vector3(-10.9f, 4.8f, 0.0f), new Vector3(-10.9f, 6.7f, 0.0f), new Vector3(-9.2f, 6.7f, 0.0f), new Vector3(-9.2f, 10.0f, 0.0f), new Vector3(-10.9f, 6.7f, 0.0f), new Vector3(-10.9f, 10.0f, 0.0f), new Vector3(-10.0f, 9.2f, 0.0f), new Vector3(-6.7f, 9.2f, 0.0f), new Vector3(-10.0f, 10.8f, 0.0f), new Vector3(-6.7f, 10.8f, 0.0f), new Vector3(-6.7f, 9.2f, 0.0f), new Vector3(-4.8f, 9.2f, 0.0f), new Vector3(-6.7f, 10.8f, 0.0f), new Vector3(-4.8f, 10.8f, 0.0f), new Vector3(-4.8f, 9.2f, 0.0f), new Vector3(-3.3f, 9.2f, 0.0f), new Vector3(-4.8f, 10.8f, 0.0f), new Vector3(-3.3f, 10.8f, 0.0f), new Vector3(-3.3f, 9.2f, 0.0f), new Vector3(0.0f, 9.2f, 0.0f), new Vector3(-3.3f, 10.8f, 0.0f), new Vector3(0.0f, 10.8f, 0.0f), new Vector3(0.0f, 9.2f, 0.0f), new Vector3(3.3f, 9.2f, 0.0f), new Vector3(0.0f, 10.8f, 0.0f), new Vector3(3.3f, 10.8f, 0.0f), new Vector3(3.3f, 9.2f, 0.0f), new Vector3(4.8f, 9.2f, 0.0f), new Vector3(3.3f, 10.8f, 0.0f), new Vector3(4.8f, 10.8f, 0.0f), new Vector3(4.8f, 9.2f, 0.0f), new Vector3(6.7f, 9.2f, 0.0f), new Vector3(4.8f, 10.8f, 0.0f), new Vector3(6.7f, 10.8f, 0.0f), new Vector3(6.7f, 9.2f, 0.0f), new Vector3(10.0f, 9.2f, 0.0f), new Vector3(6.7f, 10.8f, 0.0f), new Vector3(10.0f, 10.8f, 0.0f), new Vector3(9.2f, 10.0f, 0.0f), new Vector3(9.2f, 6.7f, 0.0f), new Vector3(10.8f, 10.0f, 0.0f), new Vector3(10.8f, 6.7f, 0.0f), new Vector3(9.2f, 6.7f, 0.0f), new Vector3(9.2f, 4.8f, 0.0f), new Vector3(10.8f, 6.7f, 0.0f), new Vector3(10.8f, 4.8f, 0.0f), new Vector3(9.2f, 4.8f, 0.0f), new Vector3(9.2f, 3.3f, 0.0f), new Vector3(10.8f, 4.8f, 0.0f), new Vector3(10.8f, 3.3f, 0.0f), new Vector3(9.2f, 3.3f, 0.0f), new Vector3(9.2f, 0.0f, 0.0f), new Vector3(10.8f, 3.3f, 0.0f), new Vector3(10.8f, 0.0f, 0.0f), new Vector3(9.2f, 0.0f, 0.0f), new Vector3(9.2f, -3.3f, 0.0f), new Vector3(10.8f, 0.0f, 0.0f), new Vector3(10.8f, -3.3f, 0.0f), new Vector3(9.2f, -3.3f, 0.0f), new Vector3(9.2f, -4.8f, 0.0f), new Vector3(10.8f, -3.3f, 0.0f), new Vector3(10.8f, -4.8f, 0.0f), new Vector3(9.2f, -4.8f, 0.0f), new Vector3(9.2f, -6.7f, 0.0f), new Vector3(10.8f, -4.8f, 0.0f), new Vector3(10.8f, -6.7f, 0.0f), new Vector3(9.2f, -6.7f, 0.0f), new Vector3(9.2f, -10.0f, 0.0f), new Vector3(10.8f, -6.7f, 0.0f), new Vector3(10.8f, -10.0f, 0.0f), new Vector3(10.0f, -9.2f, 0.0f), new Vector3(6.7f, -9.1f, 0.0f), new Vector3(10.0f, -10.9f, 0.0f), new Vector3(6.7f, -10.9f, 0.0f), new Vector3(6.7f, -9.1f, 0.0f), new Vector3(4.8f, -9.1f, 0.0f), new Vector3(6.7f, -10.9f, 0.0f), new Vector3(4.8f, -10.9f, 0.0f), new Vector3(4.8f, -9.1f, 0.0f), new Vector3(3.3f, -9.1f, 0.0f), new Vector3(4.8f, -10.9f, 0.0f), new Vector3(3.3f, -10.9f, 0.0f), new Vector3(3.3f, -9.1f, 0.0f), new Vector3(0.0f, -9.1f, 0.0f), new Vector3(3.3f, -10.9f, 0.0f), new Vector3(0.0f, -10.9f, 0.0f), new Vector3(0.0f, -9.1f, 0.0f), new Vector3(-3.3f, -9.2f, 0.0f), new Vector3(0.0f, -10.9f, 0.0f), new Vector3(-3.3f, -10.9f, 0.0f), new Vector3(-3.3f, -9.2f, 0.0f), new Vector3(-4.8f, -9.2f, 0.0f), new Vector3(-3.3f, -10.9f, 0.0f), new Vector3(-4.8f, -10.9f, 0.0f), new Vector3(-4.8f, -9.2f, 0.0f), new Vector3(-6.7f, -9.2f, 0.0f), new Vector3(-4.8f, -10.9f, 0.0f), new Vector3(-6.7f, -10.9f, 0.0f), new Vector3(-6.7f, -9.2f, 0.0f), new Vector3(-10.0f, -9.2f, 0.0f), new Vector3(-6.7f, -10.9f, 0.0f), new Vector3(-10.0f, -10.9f, 0.0f) };
            Vector2[] uv0ArrayData = { new Vector2(1.9f, -3.9f), new Vector2(3.9f, 3.9f), new Vector2(3.9f, -3.9f), new Vector2(-1.9f, -3.9f), new Vector2(-3.9f, -3.9f), new Vector2(-3.9f, 3.9f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.6f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.6f, 1.0f), new Vector2(0.6f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.6f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.4f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.4f, 1.0f), new Vector2(0.4f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.4f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.6f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.6f, 0.2f), new Vector2(0.6f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.6f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.4f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.4f, 0.2f), new Vector2(0.4f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.4f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.6f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.6f, 0.2f), new Vector2(0.6f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.6f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.4f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.4f, 0.2f), new Vector2(0.4f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.4f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.6f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.6f, 1.0f), new Vector2(0.6f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.6f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.4f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.4f, 1.0f), new Vector2(0.4f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.4f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f) };

            VerifyGeneratedGeometry(spriteShapeController, segments, posArrayData, uv0ArrayData);
        }

        [Test, Description("Test Closed Shape With Corners")]
        public void SpriteShape_VerifyGeneratedGeometryClosedShapeWithCorners()
        {
            spriteShapeController.spline.Clear();
            spriteShapeController.spline.InsertPointAt(0, new Vector3(-10.0f, -10.0f, 0));
            spriteShapeController.spline.SetCorner(0, true);
            spriteShapeController.spline.InsertPointAt(1, new Vector3(-10.0f, 10.0f, 0));
            spriteShapeController.spline.SetCorner(1, true);
            spriteShapeController.spline.InsertPointAt(2, new Vector3(10.0f, 10.0f, 0));
            spriteShapeController.spline.SetCorner(2, true);
            spriteShapeController.spline.InsertPointAt(3, new Vector3(10.0f, -10.0f, 0));
            spriteShapeController.spline.SetCorner(3, true);
            spriteShapeController.spline.isOpenEnded = false;
            spriteShapeController.splineDetail = 4;

            SpriteShapeSegment[] segments = { new SpriteShapeSegment { geomIndex = 0, indexCount = 18, vertexCount = 8, spriteIndex = -1 }, new SpriteShapeSegment { geomIndex = 1, indexCount = 48, vertexCount = 32, spriteIndex = 0 }, new SpriteShapeSegment { geomIndex = 2, indexCount = 48, vertexCount = 32, spriteIndex = 1 }, new SpriteShapeSegment { geomIndex = 3, indexCount = 48, vertexCount = 32, spriteIndex = 1 }, new SpriteShapeSegment { geomIndex = 4, indexCount = 48, vertexCount = 32, spriteIndex = 0 }, new SpriteShapeSegment { geomIndex = 5, indexCount = 6, vertexCount = 4, spriteIndex = 3 }, new SpriteShapeSegment { geomIndex = 6, indexCount = 6, vertexCount = 4, spriteIndex = 4 } };
            Vector3[] posArrayData = { new Vector3(10.0f, -10.0f, 0.0f), new Vector3(9.3f, 10.0f, 0.0f), new Vector3(10.0f, 9.3f, 0.0f), new Vector3(5.0f, -10.0f, 0.0f), new Vector3(-4.3f, -10.0f, 0.0f), new Vector3(-9.3f, -10.0f, 0.0f), new Vector3(-10.0f, -9.3f, 0.0f), new Vector3(-10.0f, 10.0f, 0.0f), new Vector3(-9.2f, -9.3f, 0.0f), new Vector3(-9.1f, -6.1f, 0.0f), new Vector3(-10.9f, -9.3f, 0.0f), new Vector3(-10.9f, -6.1f, 0.0f), new Vector3(-9.1f, -6.1f, 0.0f), new Vector3(-9.1f, -4.3f, 0.0f), new Vector3(-10.9f, -6.1f, 0.0f), new Vector3(-10.9f, -4.3f, 0.0f), new Vector3(-9.1f, -4.3f, 0.0f), new Vector3(-9.1f, -2.9f, 0.0f), new Vector3(-10.9f, -4.3f, 0.0f), new Vector3(-10.9f, -2.9f, 0.0f), new Vector3(-9.1f, -2.9f, 0.0f), new Vector3(-9.1f, 0.4f, 0.0f), new Vector3(-10.9f, -2.9f, 0.0f), new Vector3(-10.9f, 0.3f, 0.0f), new Vector3(-9.1f, 0.4f, 0.0f), new Vector3(-9.2f, 3.6f, 0.0f), new Vector3(-10.9f, 0.3f, 0.0f), new Vector3(-10.9f, 3.6f, 0.0f), new Vector3(-9.2f, 3.6f, 0.0f), new Vector3(-9.2f, 5.0f, 0.0f), new Vector3(-10.9f, 3.6f, 0.0f), new Vector3(-10.9f, 5.0f, 0.0f), new Vector3(-9.2f, 5.0f, 0.0f), new Vector3(-9.2f, 6.8f, 0.0f), new Vector3(-10.9f, 5.0f, 0.0f), new Vector3(-10.9f, 6.8f, 0.0f), new Vector3(-9.2f, 6.8f, 0.0f), new Vector3(-9.2f, 10.0f, 0.0f), new Vector3(-10.9f, 6.8f, 0.0f), new Vector3(-10.9f, 10.0f, 0.0f), new Vector3(-10.0f, 9.2f, 0.0f), new Vector3(-6.8f, 9.2f, 0.0f), new Vector3(-10.0f, 10.8f, 0.0f), new Vector3(-6.8f, 10.8f, 0.0f), new Vector3(-6.8f, 9.2f, 0.0f), new Vector3(-5.0f, 9.2f, 0.0f), new Vector3(-6.8f, 10.8f, 0.0f), new Vector3(-5.0f, 10.8f, 0.0f), new Vector3(-5.0f, 9.2f, 0.0f), new Vector3(-3.6f, 9.2f, 0.0f), new Vector3(-5.0f, 10.8f, 0.0f), new Vector3(-3.6f, 10.8f, 0.0f), new Vector3(-3.6f, 9.2f, 0.0f), new Vector3(-0.4f, 9.2f, 0.0f), new Vector3(-3.6f, 10.8f, 0.0f), new Vector3(-0.4f, 10.8f, 0.0f), new Vector3(-0.4f, 9.2f, 0.0f), new Vector3(2.9f, 9.2f, 0.0f), new Vector3(-0.4f, 10.8f, 0.0f), new Vector3(2.9f, 10.8f, 0.0f), new Vector3(2.9f, 9.2f, 0.0f), new Vector3(4.3f, 9.2f, 0.0f), new Vector3(2.9f, 10.8f, 0.0f), new Vector3(4.3f, 10.8f, 0.0f), new Vector3(4.3f, 9.2f, 0.0f), new Vector3(6.1f, 9.2f, 0.0f), new Vector3(4.3f, 10.8f, 0.0f), new Vector3(6.1f, 10.8f, 0.0f), new Vector3(6.1f, 9.2f, 0.0f), new Vector3(9.3f, 9.2f, 0.0f), new Vector3(6.1f, 10.8f, 0.0f), new Vector3(9.3f, 10.8f, 0.0f), new Vector3(9.2f, 9.3f, 0.0f), new Vector3(9.2f, 6.1f, 0.0f), new Vector3(10.8f, 9.3f, 0.0f), new Vector3(10.8f, 6.1f, 0.0f), new Vector3(9.2f, 6.1f, 0.0f), new Vector3(9.2f, 4.3f, 0.0f), new Vector3(10.8f, 6.1f, 0.0f), new Vector3(10.8f, 4.3f, 0.0f), new Vector3(9.2f, 4.3f, 0.0f), new Vector3(9.2f, 2.9f, 0.0f), new Vector3(10.8f, 4.3f, 0.0f), new Vector3(10.8f, 2.9f, 0.0f), new Vector3(9.2f, 2.9f, 0.0f), new Vector3(9.2f, -0.4f, 0.0f), new Vector3(10.8f, 2.9f, 0.0f), new Vector3(10.8f, -0.3f, 0.0f), new Vector3(9.2f, -0.4f, 0.0f), new Vector3(9.2f, -3.6f, 0.0f), new Vector3(10.8f, -0.3f, 0.0f), new Vector3(10.8f, -3.6f, 0.0f), new Vector3(9.2f, -3.6f, 0.0f), new Vector3(9.2f, -5.0f, 0.0f), new Vector3(10.8f, -3.6f, 0.0f), new Vector3(10.8f, -5.0f, 0.0f), new Vector3(9.2f, -5.0f, 0.0f), new Vector3(9.2f, -6.8f, 0.0f), new Vector3(10.8f, -5.0f, 0.0f), new Vector3(10.8f, -6.8f, 0.0f), new Vector3(9.2f, -6.8f, 0.0f), new Vector3(9.2f, -10.0f, 0.0f), new Vector3(10.8f, -6.8f, 0.0f), new Vector3(10.8f, -10.0f, 0.0f), new Vector3(10.0f, -9.2f, 0.0f), new Vector3(6.8f, -9.1f, 0.0f), new Vector3(10.0f, -10.9f, 0.0f), new Vector3(6.8f, -10.9f, 0.0f), new Vector3(6.8f, -9.1f, 0.0f), new Vector3(5.0f, -9.1f, 0.0f), new Vector3(6.8f, -10.9f, 0.0f), new Vector3(5.0f, -10.9f, 0.0f), new Vector3(5.0f, -9.1f, 0.0f), new Vector3(3.6f, -9.1f, 0.0f), new Vector3(5.0f, -10.9f, 0.0f), new Vector3(3.6f, -10.9f, 0.0f), new Vector3(3.6f, -9.1f, 0.0f), new Vector3(0.4f, -9.1f, 0.0f), new Vector3(3.6f, -10.9f, 0.0f), new Vector3(0.4f, -10.9f, 0.0f), new Vector3(0.4f, -9.1f, 0.0f), new Vector3(-2.9f, -9.2f, 0.0f), new Vector3(0.4f, -10.9f, 0.0f), new Vector3(-2.9f, -10.9f, 0.0f), new Vector3(-2.9f, -9.2f, 0.0f), new Vector3(-4.3f, -9.2f, 0.0f), new Vector3(-2.9f, -10.9f, 0.0f), new Vector3(-4.3f, -10.9f, 0.0f), new Vector3(-4.3f, -9.2f, 0.0f), new Vector3(-6.1f, -9.2f, 0.0f), new Vector3(-4.3f, -10.9f, 0.0f), new Vector3(-6.1f, -10.9f, 0.0f), new Vector3(-6.1f, -9.2f, 0.0f), new Vector3(-9.3f, -9.2f, 0.0f), new Vector3(-6.1f, -10.9f, 0.0f), new Vector3(-9.3f, -10.9f, 0.0f), new Vector3(10.7f, 10.7f, 0.0f), new Vector3(10.7f, 9.3f, 0.0f), new Vector3(9.3f, 10.7f, 0.0f), new Vector3(9.3f, 9.3f, 0.0f), new Vector3(-10.7f, -10.7f, 0.0f), new Vector3(-10.7f, -9.3f, 0.0f), new Vector3(-9.3f, -10.7f, 0.0f), new Vector3(-9.3f, -9.3f, 0.0f) };
            Vector2[] uv0ArrayData = { new Vector2(3.9f, -3.9f), new Vector2(3.6f, 3.9f), new Vector2(3.9f, 3.6f), new Vector2(2.0f, -3.9f), new Vector2(-1.7f, -3.9f), new Vector2(-3.6f, -3.9f), new Vector2(-3.9f, -3.6f), new Vector2(-3.9f, 3.9f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.6f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.6f, 1.0f), new Vector2(0.6f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.6f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.4f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.4f, 1.0f), new Vector2(0.4f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.4f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.6f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.6f, 0.2f), new Vector2(0.6f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.6f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.4f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.4f, 0.2f), new Vector2(0.4f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.4f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.6f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.6f, 0.2f), new Vector2(0.6f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.6f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.4f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.4f, 0.2f), new Vector2(0.4f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.4f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.6f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.6f, 1.0f), new Vector2(0.6f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.6f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.4f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.4f, 1.0f), new Vector2(0.4f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.4f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.1f), new Vector2(1.0f, 0.0f), new Vector2(0.0f, 0.1f), new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 0.1f) };

            VerifyGeneratedGeometry(spriteShapeController, segments, posArrayData, uv0ArrayData);
        }

        [Test, Description("Test Closed Shape With Corners And Broken Tangents")]
        public void SpriteShape_VerifyGeneratedGeometryClosedShapeWithCornersAndBrokenTangents()
        {
            spriteShapeController.spline.Clear();
            spriteShapeController.spline.InsertPointAt(0, new Vector3(-10.0f, -10.0f, 0));
            spriteShapeController.spline.SetTangentMode(0, ShapeTangentMode.Broken);
            spriteShapeController.spline.InsertPointAt(1, new Vector3(-10.0f, 10.0f, 0));
            spriteShapeController.spline.SetTangentMode(1, ShapeTangentMode.Broken);
            spriteShapeController.spline.InsertPointAt(2, new Vector3(10.0f, 10.0f, 0));
            spriteShapeController.spline.SetTangentMode(2, ShapeTangentMode.Broken);
            spriteShapeController.spline.InsertPointAt(3, new Vector3(10.0f, -10.0f, 0));
            spriteShapeController.spline.SetTangentMode(3, ShapeTangentMode.Broken);
            spriteShapeController.spline.isOpenEnded = false;
            spriteShapeController.splineDetail = 4;

            SpriteShapeSegment[] segments = { new SpriteShapeSegment { geomIndex = 0, indexCount = 12, vertexCount = 6, spriteIndex = -1 }, new SpriteShapeSegment { geomIndex = 1, indexCount = 48, vertexCount = 32, spriteIndex = 0 }, new SpriteShapeSegment { geomIndex = 2, indexCount = 48, vertexCount = 32, spriteIndex = 1 }, new SpriteShapeSegment { geomIndex = 3, indexCount = 48, vertexCount = 32, spriteIndex = 1 }, new SpriteShapeSegment { geomIndex = 4, indexCount = 48, vertexCount = 32, spriteIndex = 0 } };
            Vector3[] posArrayData = { new Vector3(4.8f, -10.0f, 0.0f), new Vector3(10.0f, 10.0f, 0.0f), new Vector3(10.0f, -10.0f, 0.0f), new Vector3(-4.8f, -10.0f, 0.0f), new Vector3(-10.0f, -10.0f, 0.0f), new Vector3(-10.0f, 10.0f, 0.0f), new Vector3(-9.2f, -10.0f, 0.0f), new Vector3(-9.1f, -6.7f, 0.0f), new Vector3(-10.9f, -10.0f, 0.0f), new Vector3(-10.9f, -6.7f, 0.0f), new Vector3(-9.1f, -6.7f, 0.0f), new Vector3(-9.1f, -4.8f, 0.0f), new Vector3(-10.9f, -6.7f, 0.0f), new Vector3(-10.9f, -4.8f, 0.0f), new Vector3(-9.1f, -4.8f, 0.0f), new Vector3(-9.1f, -3.3f, 0.0f), new Vector3(-10.9f, -4.8f, 0.0f), new Vector3(-10.9f, -3.3f, 0.0f), new Vector3(-9.1f, -3.3f, 0.0f), new Vector3(-9.1f, 0.0f, 0.0f), new Vector3(-10.9f, -3.3f, 0.0f), new Vector3(-10.9f, 0.0f, 0.0f), new Vector3(-9.1f, 0.0f, 0.0f), new Vector3(-9.2f, 3.3f, 0.0f), new Vector3(-10.9f, 0.0f, 0.0f), new Vector3(-10.9f, 3.3f, 0.0f), new Vector3(-9.2f, 3.3f, 0.0f), new Vector3(-9.2f, 4.8f, 0.0f), new Vector3(-10.9f, 3.3f, 0.0f), new Vector3(-10.9f, 4.8f, 0.0f), new Vector3(-9.2f, 4.8f, 0.0f), new Vector3(-9.2f, 6.7f, 0.0f), new Vector3(-10.9f, 4.8f, 0.0f), new Vector3(-10.9f, 6.7f, 0.0f), new Vector3(-9.2f, 6.7f, 0.0f), new Vector3(-9.2f, 10.0f, 0.0f), new Vector3(-10.9f, 6.7f, 0.0f), new Vector3(-10.9f, 10.0f, 0.0f), new Vector3(-10.0f, 9.2f, 0.0f), new Vector3(-6.7f, 9.2f, 0.0f), new Vector3(-10.0f, 10.8f, 0.0f), new Vector3(-6.7f, 10.8f, 0.0f), new Vector3(-6.7f, 9.2f, 0.0f), new Vector3(-4.8f, 9.2f, 0.0f), new Vector3(-6.7f, 10.8f, 0.0f), new Vector3(-4.8f, 10.8f, 0.0f), new Vector3(-4.8f, 9.2f, 0.0f), new Vector3(-3.3f, 9.2f, 0.0f), new Vector3(-4.8f, 10.8f, 0.0f), new Vector3(-3.3f, 10.8f, 0.0f), new Vector3(-3.3f, 9.2f, 0.0f), new Vector3(0.0f, 9.2f, 0.0f), new Vector3(-3.3f, 10.8f, 0.0f), new Vector3(0.0f, 10.8f, 0.0f), new Vector3(0.0f, 9.2f, 0.0f), new Vector3(3.3f, 9.2f, 0.0f), new Vector3(0.0f, 10.8f, 0.0f), new Vector3(3.3f, 10.8f, 0.0f), new Vector3(3.3f, 9.2f, 0.0f), new Vector3(4.8f, 9.2f, 0.0f), new Vector3(3.3f, 10.8f, 0.0f), new Vector3(4.8f, 10.8f, 0.0f), new Vector3(4.8f, 9.2f, 0.0f), new Vector3(6.7f, 9.2f, 0.0f), new Vector3(4.8f, 10.8f, 0.0f), new Vector3(6.7f, 10.8f, 0.0f), new Vector3(6.7f, 9.2f, 0.0f), new Vector3(10.0f, 9.2f, 0.0f), new Vector3(6.7f, 10.8f, 0.0f), new Vector3(10.0f, 10.8f, 0.0f), new Vector3(9.2f, 10.0f, 0.0f), new Vector3(9.2f, 6.7f, 0.0f), new Vector3(10.8f, 10.0f, 0.0f), new Vector3(10.8f, 6.7f, 0.0f), new Vector3(9.2f, 6.7f, 0.0f), new Vector3(9.2f, 4.8f, 0.0f), new Vector3(10.8f, 6.7f, 0.0f), new Vector3(10.8f, 4.8f, 0.0f), new Vector3(9.2f, 4.8f, 0.0f), new Vector3(9.2f, 3.3f, 0.0f), new Vector3(10.8f, 4.8f, 0.0f), new Vector3(10.8f, 3.3f, 0.0f), new Vector3(9.2f, 3.3f, 0.0f), new Vector3(9.2f, 0.0f, 0.0f), new Vector3(10.8f, 3.3f, 0.0f), new Vector3(10.8f, 0.0f, 0.0f), new Vector3(9.2f, 0.0f, 0.0f), new Vector3(9.2f, -3.3f, 0.0f), new Vector3(10.8f, 0.0f, 0.0f), new Vector3(10.8f, -3.3f, 0.0f), new Vector3(9.2f, -3.3f, 0.0f), new Vector3(9.2f, -4.8f, 0.0f), new Vector3(10.8f, -3.3f, 0.0f), new Vector3(10.8f, -4.8f, 0.0f), new Vector3(9.2f, -4.8f, 0.0f), new Vector3(9.2f, -6.7f, 0.0f), new Vector3(10.8f, -4.8f, 0.0f), new Vector3(10.8f, -6.7f, 0.0f), new Vector3(9.2f, -6.7f, 0.0f), new Vector3(9.2f, -10.0f, 0.0f), new Vector3(10.8f, -6.7f, 0.0f), new Vector3(10.8f, -10.0f, 0.0f), new Vector3(10.0f, -9.2f, 0.0f), new Vector3(6.7f, -9.1f, 0.0f), new Vector3(10.0f, -10.9f, 0.0f), new Vector3(6.7f, -10.9f, 0.0f), new Vector3(6.7f, -9.1f, 0.0f), new Vector3(4.8f, -9.1f, 0.0f), new Vector3(6.7f, -10.9f, 0.0f), new Vector3(4.8f, -10.9f, 0.0f), new Vector3(4.8f, -9.1f, 0.0f), new Vector3(3.3f, -9.1f, 0.0f), new Vector3(4.8f, -10.9f, 0.0f), new Vector3(3.3f, -10.9f, 0.0f), new Vector3(3.3f, -9.1f, 0.0f), new Vector3(0.0f, -9.1f, 0.0f), new Vector3(3.3f, -10.9f, 0.0f), new Vector3(0.0f, -10.9f, 0.0f), new Vector3(0.0f, -9.1f, 0.0f), new Vector3(-3.3f, -9.2f, 0.0f), new Vector3(0.0f, -10.9f, 0.0f), new Vector3(-3.3f, -10.9f, 0.0f), new Vector3(-3.3f, -9.2f, 0.0f), new Vector3(-4.8f, -9.2f, 0.0f), new Vector3(-3.3f, -10.9f, 0.0f), new Vector3(-4.8f, -10.9f, 0.0f), new Vector3(-4.8f, -9.2f, 0.0f), new Vector3(-6.7f, -9.2f, 0.0f), new Vector3(-4.8f, -10.9f, 0.0f), new Vector3(-6.7f, -10.9f, 0.0f), new Vector3(-6.7f, -9.2f, 0.0f), new Vector3(-10.0f, -9.2f, 0.0f), new Vector3(-6.7f, -10.9f, 0.0f), new Vector3(-10.0f, -10.9f, 0.0f) };
            Vector2[] uv0ArrayData = { new Vector2(1.9f, -3.9f), new Vector2(3.9f, 3.9f), new Vector2(3.9f, -3.9f), new Vector2(-1.9f, -3.9f), new Vector2(-3.9f, -3.9f), new Vector2(-3.9f, 3.9f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.6f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.6f, 1.0f), new Vector2(0.6f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.6f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.4f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.4f, 1.0f), new Vector2(0.4f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.4f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.6f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.6f, 0.2f), new Vector2(0.6f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.6f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.4f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.4f, 0.2f), new Vector2(0.4f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.4f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.6f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.6f, 0.2f), new Vector2(0.6f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.6f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.4f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.4f, 0.2f), new Vector2(0.4f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.4f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.6f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.6f, 1.0f), new Vector2(0.6f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.6f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.4f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.4f, 1.0f), new Vector2(0.4f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.4f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f) };

            VerifyGeneratedGeometry(spriteShapeController, segments, posArrayData, uv0ArrayData);
        }

        [Test, Description("Test Closed Shape With Corners And Broken Tangents")]
        public void SpriteShape_VerifyGeneratedGeometryClosedShapeWithCornersAndContinousTangents()
        {
            spriteShapeController.spline.Clear();
            spriteShapeController.spline.InsertPointAt(0, new Vector3(-10.0f, -10.0f, 0));
            spriteShapeController.spline.SetTangentMode(0, ShapeTangentMode.Continuous);
            spriteShapeController.spline.InsertPointAt(1, new Vector3(-10.0f, 10.0f, 0));
            spriteShapeController.spline.SetTangentMode(1, ShapeTangentMode.Continuous);
            spriteShapeController.spline.InsertPointAt(2, new Vector3(10.0f, 10.0f, 0));
            spriteShapeController.spline.SetTangentMode(2, ShapeTangentMode.Continuous);
            spriteShapeController.spline.InsertPointAt(3, new Vector3(10.0f, -10.0f, 0));
            spriteShapeController.spline.SetTangentMode(3, ShapeTangentMode.Continuous);
            spriteShapeController.spline.isOpenEnded = false;
            spriteShapeController.splineDetail = 4;

            SpriteShapeSegment[] segments = { new SpriteShapeSegment { geomIndex = 0, indexCount = 12, vertexCount = 6, spriteIndex = -1 }, new SpriteShapeSegment { geomIndex = 1, indexCount = 48, vertexCount = 32, spriteIndex = 0 }, new SpriteShapeSegment { geomIndex = 2, indexCount = 96, vertexCount = 64, spriteIndex = 1 }, new SpriteShapeSegment { geomIndex = 3, indexCount = 48, vertexCount = 32, spriteIndex = 0 } };
            Vector3[] posArrayData = { new Vector3(4.8f, -10.0f, 0.0f), new Vector3(10.0f, 10.0f, 0.0f), new Vector3(10.0f, -10.0f, 0.0f), new Vector3(-4.8f, -10.0f, 0.0f), new Vector3(-10.0f, -10.0f, 0.0f), new Vector3(-10.0f, 10.0f, 0.0f), new Vector3(-9.2f, -10.0f, 0.0f), new Vector3(-9.1f, -6.7f, 0.0f), new Vector3(-10.9f, -10.0f, 0.0f), new Vector3(-10.9f, -6.7f, 0.0f), new Vector3(-9.1f, -6.7f, 0.0f), new Vector3(-9.1f, -4.8f, 0.0f), new Vector3(-10.9f, -6.7f, 0.0f), new Vector3(-10.9f, -4.8f, 0.0f), new Vector3(-9.1f, -4.8f, 0.0f), new Vector3(-9.1f, -3.3f, 0.0f), new Vector3(-10.9f, -4.8f, 0.0f), new Vector3(-10.9f, -3.3f, 0.0f), new Vector3(-9.1f, -3.3f, 0.0f), new Vector3(-9.1f, 0.0f, 0.0f), new Vector3(-10.9f, -3.3f, 0.0f), new Vector3(-10.9f, 0.0f, 0.0f), new Vector3(-9.1f, 0.0f, 0.0f), new Vector3(-9.2f, 3.3f, 0.0f), new Vector3(-10.9f, 0.0f, 0.0f), new Vector3(-10.9f, 3.3f, 0.0f), new Vector3(-9.2f, 3.3f, 0.0f), new Vector3(-9.2f, 4.8f, 0.0f), new Vector3(-10.9f, 3.3f, 0.0f), new Vector3(-10.9f, 4.8f, 0.0f), new Vector3(-9.2f, 4.8f, 0.0f), new Vector3(-9.2f, 6.7f, 0.0f), new Vector3(-10.9f, 4.8f, 0.0f), new Vector3(-10.9f, 6.7f, 0.0f), new Vector3(-9.2f, 6.7f, 0.0f), new Vector3(-9.2f, 10.0f, 0.0f), new Vector3(-10.9f, 6.7f, 0.0f), new Vector3(-10.9f, 10.0f, 0.0f), new Vector3(-10.0f, 9.2f, 0.0f), new Vector3(-6.7f, 9.2f, 0.0f), new Vector3(-10.0f, 10.8f, 0.0f), new Vector3(-6.7f, 10.8f, 0.0f), new Vector3(-6.7f, 9.2f, 0.0f), new Vector3(-4.8f, 9.2f, 0.0f), new Vector3(-6.7f, 10.8f, 0.0f), new Vector3(-4.8f, 10.8f, 0.0f), new Vector3(-4.8f, 9.2f, 0.0f), new Vector3(-3.3f, 9.2f, 0.0f), new Vector3(-4.8f, 10.8f, 0.0f), new Vector3(-3.3f, 10.8f, 0.0f), new Vector3(-3.3f, 9.2f, 0.0f), new Vector3(0.0f, 9.2f, 0.0f), new Vector3(-3.3f, 10.8f, 0.0f), new Vector3(0.0f, 10.8f, 0.0f), new Vector3(0.0f, 9.2f, 0.0f), new Vector3(3.3f, 9.2f, 0.0f), new Vector3(0.0f, 10.8f, 0.0f), new Vector3(3.3f, 10.8f, 0.0f), new Vector3(3.3f, 9.2f, 0.0f), new Vector3(4.8f, 9.2f, 0.0f), new Vector3(3.3f, 10.8f, 0.0f), new Vector3(4.8f, 10.8f, 0.0f), new Vector3(4.8f, 9.2f, 0.0f), new Vector3(6.7f, 9.2f, 0.0f), new Vector3(4.8f, 10.8f, 0.0f), new Vector3(6.7f, 10.8f, 0.0f), new Vector3(6.7f, 9.2f, 0.0f), new Vector3(9.4f, 9.4f, 0.0f), new Vector3(6.7f, 10.8f, 0.0f), new Vector3(10.6f, 10.6f, 0.0f), new Vector3(9.4f, 9.4f, 0.0f), new Vector3(9.2f, 6.7f, 0.0f), new Vector3(10.6f, 10.6f, 0.0f), new Vector3(10.8f, 6.7f, 0.0f), new Vector3(9.2f, 6.7f, 0.0f), new Vector3(9.2f, 4.8f, 0.0f), new Vector3(10.8f, 6.7f, 0.0f), new Vector3(10.8f, 4.8f, 0.0f), new Vector3(9.2f, 4.8f, 0.0f), new Vector3(9.2f, 3.3f, 0.0f), new Vector3(10.8f, 4.8f, 0.0f), new Vector3(10.8f, 3.3f, 0.0f), new Vector3(9.2f, 3.3f, 0.0f), new Vector3(9.2f, 0.0f, 0.0f), new Vector3(10.8f, 3.3f, 0.0f), new Vector3(10.8f, 0.0f, 0.0f), new Vector3(9.2f, 0.0f, 0.0f), new Vector3(9.2f, -3.3f, 0.0f), new Vector3(10.8f, 0.0f, 0.0f), new Vector3(10.8f, -3.3f, 0.0f), new Vector3(9.2f, -3.3f, 0.0f), new Vector3(9.2f, -4.8f, 0.0f), new Vector3(10.8f, -3.3f, 0.0f), new Vector3(10.8f, -4.8f, 0.0f), new Vector3(9.2f, -4.8f, 0.0f), new Vector3(9.2f, -6.7f, 0.0f), new Vector3(10.8f, -4.8f, 0.0f), new Vector3(10.8f, -6.7f, 0.0f), new Vector3(9.2f, -6.7f, 0.0f), new Vector3(9.2f, -10.0f, 0.0f), new Vector3(10.8f, -6.7f, 0.0f), new Vector3(10.8f, -10.0f, 0.0f), new Vector3(10.0f, -9.2f, 0.0f), new Vector3(6.7f, -9.1f, 0.0f), new Vector3(10.0f, -10.9f, 0.0f), new Vector3(6.7f, -10.9f, 0.0f), new Vector3(6.7f, -9.1f, 0.0f), new Vector3(4.8f, -9.1f, 0.0f), new Vector3(6.7f, -10.9f, 0.0f), new Vector3(4.8f, -10.9f, 0.0f), new Vector3(4.8f, -9.1f, 0.0f), new Vector3(3.3f, -9.1f, 0.0f), new Vector3(4.8f, -10.9f, 0.0f), new Vector3(3.3f, -10.9f, 0.0f), new Vector3(3.3f, -9.1f, 0.0f), new Vector3(0.0f, -9.1f, 0.0f), new Vector3(3.3f, -10.9f, 0.0f), new Vector3(0.0f, -10.9f, 0.0f), new Vector3(0.0f, -9.1f, 0.0f), new Vector3(-3.3f, -9.2f, 0.0f), new Vector3(0.0f, -10.9f, 0.0f), new Vector3(-3.3f, -10.9f, 0.0f), new Vector3(-3.3f, -9.2f, 0.0f), new Vector3(-4.8f, -9.2f, 0.0f), new Vector3(-3.3f, -10.9f, 0.0f), new Vector3(-4.8f, -10.9f, 0.0f), new Vector3(-4.8f, -9.2f, 0.0f), new Vector3(-6.7f, -9.2f, 0.0f), new Vector3(-4.8f, -10.9f, 0.0f), new Vector3(-6.7f, -10.9f, 0.0f), new Vector3(-6.7f, -9.2f, 0.0f), new Vector3(-9.2f, -10.0f, 0.0f), new Vector3(-6.7f, -10.9f, 0.0f), new Vector3(-10.9f, -10.0f, 0.0f) };
            Vector2[] uv0ArrayData = { new Vector2(1.9f, -3.9f), new Vector2(3.9f, 3.9f), new Vector2(3.9f, -3.9f), new Vector2(-1.9f, -3.9f), new Vector2(-3.9f, -3.9f), new Vector2(-3.9f, 3.9f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.6f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.6f, 1.0f), new Vector2(0.6f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.6f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.4f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.4f, 1.0f), new Vector2(0.4f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.4f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.6f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.6f, 0.2f), new Vector2(0.6f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.6f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.4f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.4f, 0.2f), new Vector2(0.4f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.4f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.6f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.6f, 0.2f), new Vector2(0.6f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.6f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(0.4f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(0.4f, 0.2f), new Vector2(0.4f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.4f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.1f), new Vector2(1.0f, 0.1f), new Vector2(0.0f, 0.2f), new Vector2(1.0f, 0.2f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.6f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.6f, 1.0f), new Vector2(0.6f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.6f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.4f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.4f, 1.0f), new Vector2(0.4f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.4f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f) };

            VerifyGeneratedGeometry(spriteShapeController, segments, posArrayData, uv0ArrayData);
        }

        [Test, Description("(case 1033772) Crash when changing Spline Control Points for a Sprite Shape Controller in debug Inspector")]
        public void InvalidShapeControlPoints_DoesNotGenerateSpriteShape()
        {
            SerializedObject splineSO = new SerializedObject(spriteShapeController);
            SerializedProperty sp = splineSO.FindProperty("m_Spline.m_ControlPoints"); ;

            // This will insert default points with (0,0,0) pos just like what happens in the Inspector for the case mentioned above.
            sp.arraySize = 10;
            splineSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(spriteShapeController);
            spriteShapeController.BakeMesh();
            LogAssert.Expect(LogType.Warning, "Control points 3 & 4 are too close to each other. SpriteShape will not be generated.");
        }

        [Test, Description("When NULL Sprite is used in AngleRange, Collider generates correctly.")]
        public void WithNullSpriteRange_ColliderIsGeneratedProperly()
        {
            spriteShapeController.spline.Clear();
            spriteShapeController.spriteShape = Resources.Load<SpriteShape>("Collider");

            spriteShapeController.spline.InsertPointAt(0, new Vector3(-10.0f, -10.0f, 0));
            spriteShapeController.spline.SetTangentMode(0, ShapeTangentMode.Linear);
            spriteShapeController.spline.SetSpriteIndex(0, 1);
            spriteShapeController.spline.InsertPointAt(1, new Vector3(-10.0f, 10.0f, 0));
            spriteShapeController.spline.SetTangentMode(1, ShapeTangentMode.Linear);
            spriteShapeController.spline.SetSpriteIndex(1, 1);
            spriteShapeController.spline.InsertPointAt(2, new Vector3(10.0f, 10.0f, 0));
            spriteShapeController.spline.SetTangentMode(2, ShapeTangentMode.Linear);
            spriteShapeController.spline.SetSpriteIndex(2, 1);
            spriteShapeController.spline.InsertPointAt(3, new Vector3(10.0f, -10.0f, 0));
            spriteShapeController.spline.SetTangentMode(3, ShapeTangentMode.Linear);
            spriteShapeController.spline.SetSpriteIndex(3, 1);
            spriteShapeController.spline.isOpenEnded = false;
            spriteShapeController.splineDetail = 4;

            go.AddComponent<PolygonCollider2D>();

            EditorUtility.SetDirty(spriteShapeController);
            spriteShapeController.UpdateSpriteShapeParameters();
            JobHandle jobHandle = spriteShapeController.BakeMesh();
            jobHandle.Complete();
            spriteShapeController.BakeCollider();

            Assert.AreEqual(go.GetComponent<PolygonCollider2D>().points.Length, 7);
        }

        static void ResetTangents(SpriteShapeController controller, int index)
        {

            var spline = controller.spline;
            Vector3 position = spline.GetPosition(index);
            Vector3 positionNext = spline.GetPosition(SplineUtility.NextIndex(index, spline.GetPointCount()));
            Vector3 positionPrev = spline.GetPosition(SplineUtility.PreviousIndex(index, spline.GetPointCount()));
            Vector3 forward = controller.gameObject.transform.forward;

            float scale = Mathf.Min((positionNext - position).magnitude, (positionPrev - position).magnitude) * 0.33f;

            Vector3 leftTangent = (positionPrev - position).normalized * scale;
            Vector3 rightTangent = (positionNext - position).normalized * scale;
            SplineUtility.CalculateTangents(position, positionPrev, positionNext, forward, scale, out rightTangent, out leftTangent);
            spline.SetLeftTangent(index, leftTangent);
            spline.SetRightTangent(index, rightTangent);

        }

        [Test, Description("Ensure Tangents are aligned for Start and End Points")]
        public void SpriteShape_VerifyTangentsareAligned()
        {
            spriteShapeController.spline.Clear();
            spriteShapeController.spline.InsertPointAt(0, new Vector3(10.0f, -10.0f, 0));
            spriteShapeController.spline.SetTangentMode(0, ShapeTangentMode.Continuous);
            spriteShapeController.spline.InsertPointAt(1, new Vector3(0.0f, 0, 0));
            spriteShapeController.spline.SetTangentMode(1, ShapeTangentMode.Continuous);
            spriteShapeController.spline.InsertPointAt(2, new Vector3(-10.0f, 10.0f, 0));
            spriteShapeController.spline.SetTangentMode(2, ShapeTangentMode.Continuous);
            spriteShapeController.splineDetail = 4;
            spriteShapeController.spline.isOpenEnded = true;

            ResetTangents(spriteShapeController, 0);
            ResetTangents(spriteShapeController, 1);
            ResetTangents(spriteShapeController, 2);

            SpriteShapeSegment[] segments = { new SpriteShapeSegment { geomIndex = 0, indexCount = 0, vertexCount = 0, spriteIndex = -1 }, new SpriteShapeSegment { geomIndex = 1, indexCount = 72, vertexCount = 48, spriteIndex = 0 } };
            Vector3[] posArrayData = { new Vector3(10.6f, -9.4f, 0.0f), new Vector3(8.1f, -6.9f, 0.0f), new Vector3(9.4f, -10.6f, 0.0f), new Vector3(6.9f, -8.1f, 0.0f), new Vector3(8.1f, -6.9f, 0.0f), new Vector3(7.3f, -6.1f, 0.0f), new Vector3(6.9f, -8.1f, 0.0f), new Vector3(6.1f, -7.3f, 0.0f), new Vector3(7.3f, -6.1f, 0.0f), new Vector3(5.6f, -4.4f, 0.0f), new Vector3(6.1f, -7.3f, 0.0f), new Vector3(4.4f, -5.6f, 0.0f), new Vector3(5.6f, -4.4f, 0.0f), new Vector3(3.9f, -2.7f, 0.0f), new Vector3(4.4f, -5.6f, 0.0f), new Vector3(2.7f, -3.9f, 0.0f), new Vector3(3.9f, -2.7f, 0.0f), new Vector3(3.1f, -1.9f, 0.0f), new Vector3(2.7f, -3.9f, 0.0f), new Vector3(1.9f, -3.1f, 0.0f), new Vector3(3.1f, -1.9f, 0.0f), new Vector3(0.6f, 0.6f, 0.0f), new Vector3(1.9f, -3.1f, 0.0f), new Vector3(-0.6f, -0.6f, 0.0f), new Vector3(0.6f, 0.6f, 0.0f), new Vector3(-1.9f, 3.1f, 0.0f), new Vector3(-0.6f, -0.6f, 0.0f), new Vector3(-3.1f, 1.9f, 0.0f), new Vector3(-1.9f, 3.1f, 0.0f), new Vector3(-2.7f, 3.9f, 0.0f), new Vector3(-3.1f, 1.9f, 0.0f), new Vector3(-3.9f, 2.7f, 0.0f), new Vector3(-2.7f, 3.9f, 0.0f), new Vector3(-4.4f, 5.6f, 0.0f), new Vector3(-3.9f, 2.7f, 0.0f), new Vector3(-5.6f, 4.4f, 0.0f), new Vector3(-4.4f, 5.6f, 0.0f), new Vector3(-6.1f, 7.3f, 0.0f), new Vector3(-5.6f, 4.4f, 0.0f), new Vector3(-7.3f, 6.1f, 0.0f), new Vector3(-6.1f, 7.3f, 0.0f), new Vector3(-6.9f, 8.1f, 0.0f), new Vector3(-7.3f, 6.1f, 0.0f), new Vector3(-8.1f, 6.9f, 0.0f), new Vector3(-6.9f, 8.1f, 0.0f), new Vector3(-9.4f, 10.6f, 0.0f), new Vector3(-8.1f, 6.9f, 0.0f), new Vector3(-10.6f, 9.4f, 0.0f) };
            Vector2[] uv0ArrayData = { new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.3f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.3f, 1.0f), new Vector2(0.3f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.3f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.7f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.7f, 1.0f), new Vector2(0.7f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.7f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.3f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.3f, 1.0f), new Vector2(0.3f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.3f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(0.7f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(0.7f, 1.0f), new Vector2(0.7f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.7f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.9f), new Vector2(1.0f, 0.9f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f) };
            VerifyGeneratedGeometry(spriteShapeController, segments, posArrayData, uv0ArrayData);
        }

    }
}