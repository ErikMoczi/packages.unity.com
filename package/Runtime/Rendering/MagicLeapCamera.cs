using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.MagicLeap.Remote;
#endif
using UnityEngine.Experimental;
using UnityEngine.Experimental.XR;
using UnityEngine.Jobs;
using UnityEngine.Lumin;
#if !NETFX_CORE && !NET_4_6 && !NET_STANDARD_2_0
using UnityEngine.XR.MagicLeap.Compatibility;
#endif

using UnityObject = UnityEngine.Object;

namespace UnityEngine.XR.MagicLeap.Rendering
{
    public enum FrameTimingHint : int
    {
        Unspecified = 0,
        Maximum,
        Max_60Hz,
        Max_120Hz,
    }

    public enum StabilizationMode : byte
    {
        None,
        FarClip,
        FurthestObject,
        Custom
    }

    [AddComponentMenu("AR/Magic Leap/Camera")]
    [RequireComponent(typeof(Camera))]
    [UsesLuminPlatformLevel(2)]
    public sealed class MagicLeapCamera : MonoBehaviour
    {
        private Camera m_Camera;
#if ML_RENDERING_VALIDATION
        private Color m_PreviousClearColor;
#endif
        private List<Transform> _TransformList = new List<Transform>();
        private Unity.Jobs.JobHandle _Handle;

        [SerializeField]
        private Transform m_StereoConvergencePoint;
        [SerializeField]
        private FrameTimingHint m_FrameTimingHint;
        [SerializeField]
        private StabilizationMode m_StabilizationMode;
        [SerializeField]
        private float m_StabilizationDistance;

        public Transform stereoConvergencePoint
        {
            get { return m_StereoConvergencePoint; }
            set { m_StereoConvergencePoint = value; }
        }
        public FrameTimingHint frameTimingHint
        {
            get { return m_FrameTimingHint; }
            set { m_FrameTimingHint = value; }
        }
        public StabilizationMode stabilizationMode
        {
            get { return m_StabilizationMode; }
            set { m_StabilizationMode = value; }
        }
        public float stabilizationDistance
        {
            get { return m_StabilizationDistance; }
            set { m_StabilizationDistance = value; }
        }

#if PLATFORM_LUMIN && !UNITY_EDITOR
        private static Lazy<bool> _enforceNearClip = new Lazy<bool>(() => RenderingSettings.GetSystemProperty("persist.ml.render.min_clip") == "true");
        private static Lazy<bool> _enforceFarClip = new Lazy<bool>(() => RenderingSettings.GetSystemProperty("persist.ml.render.max_clip") == "true");
#endif // PLATFORM_LUMIN
        public static bool enforceNearClip
        {
            get
            {
#if PLATFORM_LUMIN && !UNITY_EDITOR
                return _enforceNearClip.Value;
#elif UNITY_EDITOR
                return true; // ML Remote needs near clip enforcement too!
#else
                return false;
#endif
            }
        }
        public static bool enforceFarClip
        {
            get
            {
#if PLATFORM_LUMIN && !UNITY_EDITOR
                return _enforceFarClip.Value;
#else
                return false;
#endif
            }
        }

        void Reset()
        {
            frameTimingHint = FrameTimingHint.Max_60Hz;
            stabilizationMode = StabilizationMode.FarClip;
            stabilizationDistance = (GetComponent<Camera>() != null) ? GetComponent<Camera>().farClipPlane : 1000.0f;
        }

        void OnDisable()
        {
            RenderingSettings.useLegacyFrameParameters = true;
        }

        void OnEnable()
        {
            RenderingSettings.useLegacyFrameParameters = false;
        }

        void Start()
        {
            m_Camera = GetComponent<Camera>();
            RenderingSettings.frameTimingHint = frameTimingHint;
            RenderingSettings.singlePassEnabled = XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced;
        }

        void LateUpdate()
        {
            NativeArray<float>? distances = null;
            if (stabilizationMode == StabilizationMode.FurthestObject)
            {
                var taa = new TransformAccessArray(_TransformList.ToArray());
                distances = new NativeArray<float>(taa.length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var job = new RenderingJobs.CalculateDistancesJob(distances.Value, transform.position);
                _Handle = job.Schedule(taa);
                _TransformList.Clear();
            }

            var scale = GetCameraScale();
            RenderingSettings.cameraScale = scale;
            // only perform clipping plane validation if the user has added the requisite components.
            ValidateFarClip(scale);
            ValidateNearClip(scale);

            RenderingSettings.farClipDistance = GetFarClippingPlane(scale);
            RenderingSettings.nearClipDistance = GetNearClippingPlane(scale);
            RenderingSettings.focusDistance = actualStereoConvergence / scale;
#if ML_RENDERING_VALIDATION
            CheckClearColor();
#endif
            switch (stabilizationMode)
            {
                case StabilizationMode.Custom:
                    RenderingSettings.stabilizationDistance = ClampToClippingPlanes(stabilizationDistance);
                    break;
                case StabilizationMode.FarClip:
                    RenderingSettings.stabilizationDistance = GetFarClippingPlane(scale);
                    break;
                case StabilizationMode.FurthestObject:
                    _Handle.Complete();
                    RenderingSettings.stabilizationDistance = distances.Max();
                    distances.Value.Dispose();
                    distances = null;
                    break;
            }
        }

        public void ValidateFarClip(float scale)
        {
            if (!m_Camera) return;
            var farClip = m_Camera.farClipPlane / scale;
            var max = RenderingSettings.maxFarClipDistance;
            if (enforceFarClip && farClip > max)
            {
                MLWarnings.WarnedAboutFarClippingPlane.Trigger(farClip, max);
                m_Camera.farClipPlane = max * scale;
            }
        }

        public void ValidateNearClip(float scale)
        {
            if (!m_Camera) return;
            var nearClip = m_Camera.nearClipPlane / scale;
            var max = RenderingSettings.maxNearClipDistance;
            if (enforceNearClip && nearClip < max)
            {
                MLWarnings.WarnedAboutNearClippingPlane.Trigger(nearClip, max);
                m_Camera.nearClipPlane = max * scale;
            }
        }

        private float actualStereoConvergence
        {
            get
            {
                // Get Focus Distance and log warnings if not within the allowed value bounds.
                float focusDistance = m_Camera.stereoConvergence;
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
                m_Camera.stereoConvergence = focusDistance;

                return focusDistance;
            }
        }
        public float ClampToClippingPlanes(float value)
        {
            return Mathf.Clamp(value,
                RenderingSettings.maxNearClipDistance,
                RenderingSettings.maxFarClipDistance);
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
            var scale = Vector3.one;
            var parent = transform.parent;
            if (parent)
                scale = parent.lossyScale;
#if ML_RENDERING_VALIDATION
            if (!(Mathf.Approximately(scale.x, scale.y) && Mathf.Approximately(scale.x, scale.z)))
            {
                MLWarnings.WarnedAboutNonUniformScale.Trigger();
                return (scale.x + scale.y + scale.z) / 3;
            }
#endif
            // Avoid precision error caused by averaging x, y and z components.
            return scale.x;
        }

        private float GetFarClippingPlane(float scale)
        {
            return m_Camera.farClipPlane / scale;
        }

        private float GetNearClippingPlane(float scale)
        {
            return m_Camera.nearClipPlane / scale;
        }

        private void UpdateTransformList(Transform transform)
        {
            if (stabilizationMode == StabilizationMode.FurthestObject)
                _TransformList.Add(transform);
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(MagicLeapCamera))]
    class MagicLeapCameraEditor : Editor
    {
        private const string kDefineRenderingValidation = "ML_RENDERING_VALIDATION";

        private static GUIContent kRenderingValidationText = new GUIContent("Runtime Rendering Validation");
        private static GUIContent kFarClipEnforcementText = new GUIContent("Far Clip Enforcement");
        private static GUIContent kNearClipEnforcementText = new GUIContent("Near Clip Enforcement");
        private static GUIContent kStereoConvergencePointText = new GUIContent("Stereo Convergence Point");
        private static GUIContent kFrameTimingHintText = new GUIContent("Frame Timing Hint");
        private static GUIContent kStabilizationModeText = new GUIContent("Stabilization Mode");
        private static GUIContent kStabilizationDistanceText = new GUIContent("Stabilization Distance");

#if ML_RENDERING_VALIDATION
        SerializedProperty previousClearColorProp;
#endif
        SerializedProperty stereoConvergenceProp;
        SerializedProperty stereoConvergencePointProp;
        SerializedProperty frameTimingHintProp;
        SerializedProperty stabilizationModeProp;
        SerializedProperty stabilizationDistanceProp;

        private bool renderingValidationEnabled
        {
            get { return IsDefineSet(kDefineRenderingValidation); }
            set { ToggleDefine(kDefineRenderingValidation, value); }
        }

        private GameObject gameObject { get { return (target as MagicLeapCamera).gameObject; } }

        void OnEnable()
        {
#if ML_RENDERING_VALIDATION
            previousClearColorProp = serializedObject.FindProperty("m_PreviousClearColor");
#endif
            stereoConvergencePointProp = serializedObject.FindProperty("m_StereoConvergencePoint");

            frameTimingHintProp = serializedObject.FindProperty("m_FrameTimingHint");
            stabilizationModeProp = serializedObject.FindProperty("m_StabilizationMode");
            stabilizationDistanceProp = serializedObject.FindProperty("m_StabilizationDistance");
        }

        public override void OnInspectorGUI()
        {
            //var rect = GUILayoutUtility.GetRect(kRenderingValidationText, EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).label);
            renderingValidationEnabled = EditorGUILayout.Toggle(kRenderingValidationText, renderingValidationEnabled, GUILayout.ExpandWidth(true));
            // TODO :: For now, we're always doing enforcement, but we might not always want to.
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle(kFarClipEnforcementText, true);
                EditorGUILayout.Toggle(kNearClipEnforcementText, true);
            }

            serializedObject.Update();

            EditorGUILayout.ObjectField(stereoConvergencePointProp, typeof(Transform), kStereoConvergencePointText);
            EditorGUILayout.PropertyField(frameTimingHintProp, kFrameTimingHintText);
            EditorGUILayout.PropertyField(stabilizationModeProp, kStabilizationModeText);
            using (new EditorGUI.DisabledScope(stabilizationModeProp.enumValueIndex != (int)StabilizationMode.Custom))
                EditorGUILayout.PropertyField(stabilizationDistanceProp, kStabilizationDistanceText);

            serializedObject.ApplyModifiedProperties();
        }

        private bool IsDefineSet(string define)
        {
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Lumin).Contains(define);
        }

        private void ToggleDefine(string define, bool active)
        {
            if (active)
            {
                if (IsDefineSet(define)) return;
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Lumin);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Lumin, string.Format("{0};{1}", defines, define));
            }
            else
            {
                if (!IsDefineSet(define)) return;
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Lumin).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Lumin, string.Join(";", defines.Where(d => d.Trim() != define).ToArray()));
            }
        }
    }
#endif
}