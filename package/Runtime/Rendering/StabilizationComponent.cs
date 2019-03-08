using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.XR.MagicLeap.Rendering
{
    public class StabilizationComponent : MonoBehaviour
    {
        private void OnBecameInvisible()
        {
            enabled = false;
        }
        private void OnBecameVisible()
        {
            enabled = true;
        }
        void Update()
        {
            // normally, we'd want to cache the camera reference to save a couple cycles,
            // but since it can change, we need to sample it every frame.
            var camera = Camera.main;
            if (camera)
                camera.SendMessage("UpdateTransformList", transform);
        }
    }
}
