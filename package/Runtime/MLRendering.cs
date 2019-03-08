using System;
#if UNITY_EDITOR || PLATFORM_LUMIN
using System.Runtime.InteropServices;
#endif
#if PLATFORM_LUMIN
using System.Text;
#endif
#if !NETFX_CORE && !NET_4_6 && !NET_STANDARD_2_0
using UnityEngine.XR.MagicLeap.Compatibility;
#endif

using UnityEngine;
using UnityEngine.XR;

namespace UnityEngine.XR.MagicLeap
{
    public enum FrameTimingHint : int
    {
        Unspecified = 0,
        Maximum,
        Max_60Hz,
        Max_120Hz,
    }

    [RequireComponent(typeof(Camera))]
    public class MLRendering : MonoBehaviour
    {
#if PLATFORM_LUMIN && ML_RENDERING_VALIDATION
#if ML_RENDERING_ENFORCE_FAR_CLIP
        private static Lazy<bool> _enforceFarClip = new Lazy<bool>(() => RenderingAPI.GetSystemProperty("persist.ml.render.max_clip") == "true");
#endif
#if ML_RENDERING_ENFORCE_NEAR_CLIP
        private static Lazy<bool> _enforceNearClip = new Lazy<bool>(() => RenderingAPI.GetSystemProperty("persist.ml.render.min_clip") == "true");
#endif
#endif // PLATFORM_LUMIN && ML_RENDERING_VALIDATION
        private static Lazy<float> _maxFarClipPlane = new Lazy<float>(() => RenderingAPI.UnityMagicLeap_RenderingGetMaxFarClipDistance());
        private static Lazy<float> _maxNearClipPlane = new Lazy<float>(() => RenderingAPI.UnityMagicLeap_RenderingGetMaxNearClipDistance());
        private Camera m_Camera;
#if ML_RENDERING_VALIDATION
        private Color m_PreviousClearColor;
#endif

        public float stereoConvergence;
        public Transform stereoConvergencePoint;

        public FrameTimingHint FrameTimingHint;

        void Start()
        {
            m_Camera = GetComponent<Camera>();
            RenderingAPI.UnityMagicLeap_RenderingSetFrameTimingHint(FrameTimingHint);
            RenderingAPI.UnityMagicLeap_RenderingSetSinglePassEnabled(XRSettings.stereoRenderingMode != XRSettings.StereoRenderingMode.MultiPass);
        }

        void LateUpdate()
        {
            var scale = GetCameraScale();
            RenderingAPI.UnityMagicLeap_RenderingSetCameraScale(scale);
            RenderingAPI.UnityMagicLeap_RenderingSetFarClipDistance(GetFarClippingPlane(scale));
            RenderingAPI.UnityMagicLeap_RenderingSetNearClipDistance(GetNearClippingPlane(scale));
            RenderingAPI.UnityMagicLeap_RenderingSetFocusDistance(actualStereoConvergence / scale);
#if ML_RENDERING_VALIDATION
            CheckClearColor();
#endif
        }

        public static bool enforceFarClip
        {
            get
            {
#if PLATFORM_LUMIN && ML_RENDERING_VALIDATION && ML_RENDERING_ENFORCE_FAR_CLIP
                return _enforceFarClip.Value;
#else
                return false;
#endif
            }
        }

        public static bool enforceNearClip
        {
            get
            {
#if PLATFORM_LUMIN && ML_RENDERING_VALIDATION && ML_RENDERING_ENFORCE_NEAR_CLIP
                return _enforceNearClip.Value;
#else
                return false;
#endif
            }
        }

        public static float maxFarClipPlane
        {
            get
            {
                return _maxFarClipPlane.Value;
            }
        }

        public static float maxNearClipPlane
        {
            get
            {
                return _maxNearClipPlane.Value;
            }
        }

        private float actualStereoConvergence
        {
            get
            {
                // Get Focus Distance and log warnings if not within the allowed value bounds.
                float focusDistance = stereoConvergence;
                bool hasStereoConvergencePoint = stereoConvergencePoint != null;
                if (hasStereoConvergencePoint)
                {
                    // From Unity documentation:
                    // Note that camera space matches OpenGL convention: camera's forward is the negative Z axis.
                    // This is different from Unity's convention, where forward is the positive Z axis.
                    Vector3 worldForward = new Vector3(0.0f, 0.0f, -1.0f);
                    Vector3 camForward = m_Camera.cameraToWorldMatrix.MultiplyVector(worldForward);
                    camForward = camForward.normalized;

                    // We are only interested in the focus object's distance to the camera forward tangent plane.
                    focusDistance = Vector3.Dot(stereoConvergencePoint.position - transform.position, camForward);
                }
#if ML_RENDERING_VALIDATION
                float nearClip = m_Camera.nearClipPlane;
                if (focusDistance < nearClip)
                {
                    MLWarnings.WarnedAboutSteroConvergence.Trigger(hasStereoConvergencePoint);
                    focusDistance = nearClip;
                }
#endif
                stereoConvergence = focusDistance;

                return stereoConvergence;
            }
        }
#if ML_RENDERING_VALIDATION
        private void CheckClearColor()
        {
            bool isClearingCorrectly = false;
            if (m_Camera.clearFlags == CameraClearFlags.SolidColor)
            {
                Color color = m_Camera.backgroundColor;
                if (m_PreviousClearColor != color)
                {
                    MLWarnings.WarnedAboutClearColor.Reset();
                    isClearingCorrectly = color == Color.clear;
                    m_PreviousClearColor = color;
                }
            }
            if (!isClearingCorrectly)
            {
                MLWarnings.WarnedAboutClearColor.Trigger();
            }
        }
#endif

        private float GetCameraScale()
        {
            var scale = transform.lossyScale;
#if ML_RENDERING_VALIDATION
            if (!(Mathf.Approximately(scale.x, scale.y) && Mathf.Approximately(scale.x, scale.z)))
            {
                MLWarnings.WarnedAboutNonUniformScale.Trigger();
                return (scale.x + scale.y + scale.z) / 3;
            }
            else
#else
            {
                // Avoid precision error caused by averaging x, y and z components.
                return scale.x;
            }
#endif
        }

        private float GetFarClippingPlane(float scale)
        {
            var farClip = m_Camera.farClipPlane / scale;
#if PLATFORM_LUMIN && ML_RENDERING_VALIDATION && ML_RENDERING_ENFORCE_NEAR_CLIP
            if (enforceFarClip && farClip > maxFarClipPlane)
            {
                MLWarnings.WarnedAboutFarClppingPlane.Trigger(farClip, maxFarClipPlane);
                m_Camera.farClipPlane = maxFarClipPlane * scale;
            }
#endif
            return farClip;
        }

        private float GetNearClippingPlane(float scale  )
        {
            var nearClip = m_Camera.nearClipPlane / scale;
#if PLATFORM_LUMIN && ML_RENDERING_VALIDATION && ML_RENDERING_ENFORCE_NEAR_CLIP
            if (enforceNearClip && nearClip < maxNearClipPlane)
            {
                MLWarnings.WarnedAboutNearClippingPlane.Trigger(nearClip, maxNearClipPlane);
                m_Camera.nearClipPlane = maxNearClipPlane * m_Scale;
            }
#endif
            return nearClip;
        }
    }

    internal static class RenderingAPI
    {
#if UNITY_EDITOR || PLATFORM_LUMIN
        [DllImport("UnityMagicLeap")]
        public static extern float UnityMagicLeap_RenderingGetCameraScale();
        [DllImport("UnityMagicLeap")]
        public static extern void UnityMagicLeap_RenderingSetCameraScale(float newScale);
        [DllImport("UnityMagicLeap")]
        public static extern float UnityMagicLeap_RenderingGetFarClipDistance();
        [DllImport("UnityMagicLeap")]
        public static extern void UnityMagicLeap_RenderingSetFarClipDistance(float newDistance);
        [DllImport("UnityMagicLeap")]
        public static extern float UnityMagicLeap_RenderingGetNearClipDistance();
        [DllImport("UnityMagicLeap")]
        public static extern void UnityMagicLeap_RenderingSetNearClipDistance(float newDistance);
        [DllImport("UnityMagicLeap")]
        public static extern float UnityMagicLeap_RenderingGetFocusDistance();
        [DllImport("UnityMagicLeap")]
        public static extern void UnityMagicLeap_RenderingSetFocusDistance(float newDistance);
        [DllImport("UnityMagicLeap")]
        public static extern FrameTimingHint UnityMagicLeap_RenderingGetFrameTimingHint();
        [DllImport("UnityMagicLeap")]
        public static extern void UnityMagicLeap_RenderingSetFrameTimingHint(FrameTimingHint newValue);
        [DllImport("UnityMagicLeap")]
        public static extern bool UnityMagicLeap_RenderingGetSinglePassEnabled();
        [DllImport("UnityMagicLeap")]
        public static extern void UnityMagicLeap_RenderingSetSinglePassEnabled(bool newValue);
#else
        public static float UnityMagicLeap_RenderingGetCameraScale() { return 1.0f; }
        public static void UnityMagicLeap_RenderingSetCameraScale(float newScale) {}
        public static float UnityMagicLeap_RenderingGetFarClipDistance() { return 100.0f; }
        public static void UnityMagicLeap_RenderingSetFarClipDistance(float newDistance) {}
        public static float UnityMagicLeap_RenderingGetNearClipDistance() { return 0.42926f; }
        public static void UnityMagicLeap_RenderingSetNearClipDistance(float newDistance) {}
        public static float UnityMagicLeap_RenderingGetFocusDistance() { return 100.0f; }
        public static void UnityMagicLeap_RenderingSetFocusDistance(float newDistance) {}
        public static FrameTimingHint UnityMagicLeap_RenderingGetFrameTimingHint() { return FrameTimingHint.Unspecified; }
        public static void UnityMagicLeap_RenderingSetFrameTimingHint(FrameTimingHint newValue) {}
        public static bool UnityMagicLeap_RenderingGetSinglePassEnabled() { return false; }
        public static void UnityMagicLeap_RenderingSetSinglePassEnabled(bool newValue) {}
#endif

        // device-specific calls.
#if PLATFORM_LUMIN

        [DllImport("libc", EntryPoint="__system_property_get")]
        private static extern int _GetSystemProperty(string name, StringBuilder @value);

        public static string GetSystemProperty(string name)
        {
            var sb = new StringBuilder(255);
            var ret = _GetSystemProperty(name, sb);
            return ret == 0 ? sb.ToString() : null;
        }

        [DllImport("UnityMagicLeap")]
        public static extern float UnityMagicLeap_RenderingGetMaxFarClipDistance();
        [DllImport("UnityMagicLeap")]
        public static extern float UnityMagicLeap_RenderingGetMaxNearClipDistance();
#else
        public static float UnityMagicLeap_RenderingGetMaxFarClipDistance()
        {
            return UnityMagicLeap_RenderingGetFarClipDistance();
        }
        public static float UnityMagicLeap_RenderingGetMaxNearClipDistance()
        {
            return UnityMagicLeap_RenderingGetNearClipDistance();
        }
#endif
    }
}