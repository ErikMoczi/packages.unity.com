using UnityEngine;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEditor.Experimental.U2D.Common.Tests
{
    internal class FindTightRectTests
    {
        private static IEnumerable<TestCaseData> TrimAlphaTestCases()
        {
            byte[] buffer = new byte[64 * 64 * 4];
            yield return new TestCaseData(buffer, 64, 64, new RectInt(64, 64, 0, 0));

            buffer = new byte[64 * 64 * 4];
            buffer[16 * 64 * 4 + 16 * 4 + 3] = 255;
            buffer[16 * 64 * 4 + 17 * 4 + 3] = 255;
            yield return new TestCaseData(buffer, 64, 64, new RectInt(16, 16, 2, 1));

            buffer = new byte[64 * 64 * 4];
            buffer[16 * 64 * 4 + 16 * 4 + 3] = 255;
            buffer[31 * 64 * 4 + 31 * 4 + 3] = 255;
            yield return new TestCaseData(buffer, 64, 64, new RectInt(16, 16, 16, 16));

            buffer = new byte[64 * 64 * 4];
            buffer[16 * 64 * 4 + 16 * 4 + 3] = 255;
            buffer[18 * 64 * 4 + 17 * 4 + 3] = 255;
            buffer[18 * 64 * 4 + 18 * 4 + 3] = 255;
            buffer[31 * 64 * 4 + 31 * 4 + 3] = 255;
            yield return new TestCaseData(buffer, 64, 64, new RectInt(16, 16, 16, 16));
        }

        [Test, TestCaseSource("TrimAlphaTestCases")]
        public void TrimAlphaParametricTests(byte[] buffer, int width, int height, RectInt expectedOutput)
        {
            var nativeArrayBuffer = new NativeArray<byte>(buffer, Allocator.Temp);
            var rectOut = FindTightRectJob.Execute(new[] { nativeArrayBuffer }, width, height);
            nativeArrayBuffer.Dispose();
            Assert.AreEqual(expectedOutput, rectOut[0]);
        }
    }
}
