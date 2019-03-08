
using System.Runtime.InteropServices;
using System.Text;

namespace UnityEngine.XR.MagicLeap.Rendering
{
    public static class RenderingSettings
    {
        public static float cameraScale
        {
            get { return UnityMagicLeap_RenderingGetParameter("CameraScale", 1.0f); }
            internal set { UnityMagicLeap_RenderingSetParameter("CameraScale", value); }
        }
        public static float farClipDistance
        {
            get { return UnityMagicLeap_RenderingGetParameter("FarClipDistance", 100.0f); }
            internal set { UnityMagicLeap_RenderingSetParameter("FarClipDistance", value); }
        }
        public static float focusDistance
        {
            get { return UnityMagicLeap_RenderingGetParameter("FocusDistance", 100.0f); }
            internal set { UnityMagicLeap_RenderingSetParameter("FocusDistance", value); }
        }
        public static FrameTimingHint frameTimingHint
        {
            get { return UnityMagicLeap_RenderingGetFrameTimingHint(); }
            internal set { UnityMagicLeap_RenderingSetFrameTimingHint(value); }
        }
        public static float maxFarClipDistance
        {
            get { return UnityMagicLeap_RenderingGetParameter("MaxFarClipDistance", 1000.0f); }
        }
        public static float maxNearClipDistance
        {
            get { return UnityMagicLeap_RenderingGetParameter("MaxNearClipDistance", 0.42926f); }
        }
        public static float nearClipDistance
        {
            get { return UnityMagicLeap_RenderingGetParameter("NearClipDistance", 0.42926f); }
            internal set { UnityMagicLeap_RenderingSetParameter("NearClipDistance", value); }
        }
        public static bool singlePassEnabled
        {
            get { return UnityMagicLeap_RenderingGetParameter("SinglePassEnabled", 0.0f) != 0.0f; }
            internal set { UnityMagicLeap_RenderingSetParameter("SinglePassEnabled", value ? 1.0f : 0.0f); }
        }
        public static float stabilizationDistance
        {
            get { return UnityMagicLeap_RenderingGetParameter("StabilizationDistance", 100.0f); }
            internal set { UnityMagicLeap_RenderingSetParameter("StabilizationDistance", value); }
        }
#if PLATFORM_LUMIN
        [DllImport("UnityMagicLeap")]
        internal static extern FrameTimingHint UnityMagicLeap_RenderingGetFrameTimingHint();
        [DllImport("UnityMagicLeap")]
        internal static extern void UnityMagicLeap_RenderingSetFrameTimingHint(FrameTimingHint newValue);
        [DllImport("UnityMagicLeap", CharSet = CharSet.Ansi)]
        internal static extern void UnityMagicLeap_RenderingSetParameter(string key, float newValue);
        [DllImport("UnityMagicLeap", CharSet = CharSet.Ansi)]
        internal static extern float UnityMagicLeap_RenderingGetParameter(string key, float default_value);
#else
        internal static FrameTimingHint UnityMagicLeap_RenderingGetFrameTimingHint() { return FrameTimingHint.Unspecified; }
        internal static void UnityMagicLeap_RenderingSetFrameTimingHint(FrameTimingHint newValue) {}
        internal static void UnityMagicLeap_RenderingSetParameter(string key, float newValue) {}
        internal static float UnityMagicLeap_RenderingGetParameter(string key, float default_value) { return default_value; }
#endif

        // device-specific calls.
#if PLATFORM_LUMIN && !UNITY_EDITOR
        [DllImport("libc", EntryPoint = "__system_property_get")]
        private static extern int _GetSystemProperty(string name, StringBuilder @value);

        public static string GetSystemProperty(string name)
        {
            var sb = new StringBuilder(255);
            var ret = _GetSystemProperty(name, sb);
            return ret == 0 ? sb.ToString() : null;
        }
#endif
    }
}
