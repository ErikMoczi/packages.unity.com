using UnityEngine;
using NUnit.Framework;
using System.Collections.Generic;
using System;

namespace UnityEditor.Experimental.U2D.Common.Tests
{
    public class ImagePackerTests
    {
        [TestCase(1, ExpectedResult = 1)]
        [TestCase(2, ExpectedResult = 2)]
        [TestCase(3, ExpectedResult = 4)]
        [TestCase(4, ExpectedResult = 4)]
        [TestCase(5, ExpectedResult = 8)]
        [TestCase(6, ExpectedResult = 8)]
        [TestCase(7, ExpectedResult = 8)]
        [TestCase(8, ExpectedResult = 8)]
        [TestCase(9, ExpectedResult = 16)]
        public int NextPowerOfTwoTest(int value)
        {
            return (int)ImagePacker.NextPowerOfTwo((ulong)value);
        }

        private static IEnumerable<TestCaseData> PackSpriteTestCases()
        {
            yield return new TestCaseData(new[] { new Vector2Int(64, 64) }, 0,
                new[]
            {
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 32, 32), index = 0} ,
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 16, 16), index = 1} ,
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 16, 16), index = 2} ,
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 16, 16), index = 3} ,
            },
                4, 0
                );

            yield return new TestCaseData(new[] { new Vector2Int(64, 64) }, 0,
                new[]
            {
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 32, 32), index = 0} ,
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 16, 16), index = 1} ,
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 16, 16), index = 2} ,
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 32, 32), index = 3} ,
            },
                4, 0
                );

            yield return new TestCaseData(new[] { new Vector2Int(64, 64), new Vector2Int(128, 64) }, 0,
                new[]
            {
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 64, 64), index = 0} ,
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 16, 16), index = 1} ,
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 16, 16), index = 2} ,
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 32, 32), index = 3} ,
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 32, 32), index = 4} ,
            },
                5, 1
                );

            yield return new TestCaseData(new[] { new Vector2Int(64, 64), new Vector2Int(128, 64) }, 0,
                new[]
            {
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 0, 0), index = 0} ,
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 16, 16), index = 1} ,
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 16, 16), index = 2} ,
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 32, 32), index = 3} ,
            new ImagePacker.ImagePackRect() {rect = new RectInt(0, 0, 32, 32), index = 4} ,
            },
                5, 0
                );
        }

        [Test, TestCaseSource("PackSpriteTestCases")]
        public void ImagePackRectSortTests(Vector2Int[] atlasSize, int padding, ImagePacker.ImagePackRect[] packRect, int expectedPass, int expectedFail)
        {
            Array.Sort(packRect);
            for (int i = 0, j = 1; i < packRect.Length - 1; ++i, ++j)
            {
                var area1 = packRect[i].rect.width * packRect[i].rect.height;
                var area2 = packRect[j].rect.width * packRect[j].rect.height;
                Assert.IsTrue(area1 > area2 || (area1 <= area2 && packRect[i].index < packRect[j].index));
            }
        }

        [Test, TestCaseSource("PackSpriteTestCases")]
        public void RectPackTests(Vector2Int[] atlasSize, int padding,
            ImagePacker.ImagePackRect[] packRect, int expectedPass, int expectedFail)
        {
            Array.Sort(packRect);
            var root = new ImagePackNode();
            root.rect = new RectInt(0, 0, atlasSize[0].x, atlasSize[0].y);
            int passed = 0, failed = 0;
            int atlasSizeIndex = 1;
            for (int i = 0; i < packRect.Length; ++i)
            {
                var rect = packRect[i];
                var result = root.Insert(rect, padding);
                if (result)
                    ++passed;
                else
                {
                    ++failed;
                    if (atlasSizeIndex < atlasSize.Length)
                    {
                        root.AdjustSize(atlasSize[atlasSizeIndex].x, atlasSize[atlasSizeIndex].y);
                        ++atlasSizeIndex;
                        --i;
                    }
                }
            }
            Assert.AreEqual(expectedPass, passed);
            Assert.AreEqual(expectedFail, failed);
        }
    }

}
