using System.Collections;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.MagicLeap.Rendering;

namespace Rendering
{
    public class StabilizationTests
    {
        [UnitySetUp]
        public void Setup()
        {
            Debug.Log("TestStarted");
        }

        [UnityTearDown]
        public void Teardown()
        {
            Debug.Log("TestEnded");
        }

        [UnityTest]
        public IEnumerator CanCreateABunchOfStabilizerObjects()
        {
            var box = new Bounds(Vector3.zero, Vector3.one * 10);
            for (int i = 0; i < 10; i++)
            {
                var go = new GameObject(string.Format("Stabilizer {0}", i));
                go.AddComponent<StabilizationComponent>();
                go.transform.position = GetRandomPointFromBoundingBox(box);
                yield return null;
            }
        }

        private Vector3 GetRandomPointFromBoundingBox(Bounds bounds)
        {
            var r = Random.insideUnitSphere;
            var max = bounds.max;
            var min = bounds.min;
            return new Vector3
            {
                x = Mathf.Lerp(min.x, max.x, r.x),
                y = Mathf.Lerp(min.y, max.y, r.y),
                z = Mathf.Lerp(min.z, max.z, r.z)
            };
        }
    }
}
