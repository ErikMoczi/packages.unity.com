using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Unity.VectorGraphics.Editor
{
    [CustomEditor(typeof(SVGImporter))]
    [CanEditMultipleObjects]
    internal class SVGImporterEditor : ScriptedImporterEditor
    {
        private enum SettingsType
        {
            Basic,
            Advanced
        }

        private SerializedProperty m_SVGType;
        private SerializedProperty m_TexturedSpriteMeshType;
        private SerializedProperty m_PixelsPerUnit;
        private SerializedProperty m_GradientResolution;
        private SerializedProperty m_Alignment;
        private SerializedProperty m_CustomPivot;
        private SerializedProperty m_GeneratePhysicsShape;
        private SerializedProperty m_PreserveViewport;
        private SerializedProperty m_AdvancedMode;
        private SerializedProperty m_StepDistance;
        private SerializedProperty m_SamplingStepDistance;
        private SerializedProperty m_PredefinedResolutionIndex;
        private SerializedProperty m_TargetResolution;
        private SerializedProperty m_ResolutionMultiplier;
        private SerializedProperty m_MaxCordDeviationEnabled;
        private SerializedProperty m_MaxCordDeviation;
        private SerializedProperty m_MaxTangentAngleEnabled;
        private SerializedProperty m_MaxTangentAngle;
        private SerializedProperty m_KeepTextureAspectRatio;
        private SerializedProperty m_TextureSize;
        private SerializedProperty m_TextureWidth;
        private SerializedProperty m_TextureHeight;
        private SerializedProperty m_WrapMode;
        private SerializedProperty m_FilterMode;
        private SerializedProperty m_SampleCount;

        private readonly GUIContent m_SVGTypeText = new GUIContent("Generated Asset Type", "How the SVG file will be imported.");
        private readonly GUIContent m_TexturedSpriteMeshTypeText = new GUIContent("Mesh Type", "Type of the sprite mesh to generate.");
        private readonly GUIContent m_PixelsPerUnitText = new GUIContent("Pixels Per Unit", "How many pixels in the SVG correspond to one unit in the world.");
        private readonly GUIContent m_GradientResolutionText = new GUIContent("Gradient Resolution", "Size of each rasterized gradient in pixels. Higher values consume memory but result in more accurate gradients.");
        private readonly GUIContent m_AlignmentText = new GUIContent("Pivot", "Sprite pivot point in its local space.");
        private readonly GUIContent m_CustomPivotText = new GUIContent("Custom Pivot");
        private readonly GUIContent m_GeneratePhysicsShapeText = new GUIContent("Generate Physics Shape");
        private readonly GUIContent m_PreserveViewportText = new GUIContent("Preserve Viewport", "Preserve the viewport defined in the SVG document");
        private readonly GUIContent m_SettingsText = new GUIContent("Tessellation Settings");
        private readonly GUIContent m_TargetResolutionText = new GUIContent("Target Resolution", "Target resolution below which the sprite will not look tessellated.");
        private readonly GUIContent m_CustomTargetResolutionText = new GUIContent("Custom Target Resolution");
        private readonly GUIContent m_ResolutionMultiplierText = new GUIContent("Zoom Factor", "Target zoom factor for which the SVG asset should not look tessellated.");
        private readonly GUIContent m_StepDistanceText = new GUIContent("Step Distance", "Distance at which vertices will be generated along the paths. Lower values will result in a more dense tessellation.");
        private readonly GUIContent m_SamplingStepDistanceText = new GUIContent("Sampling Steps", "Number of samples evaluated on paths. Higher values give more accurate results (but takes longer).");
        private readonly GUIContent m_MaxCordDeviationEnabledText = new GUIContent("Max Cord Enabled", "Enables the \"max cord deviation\" tessellation test.");
        private readonly GUIContent m_MaxCordDeviationText = new GUIContent("Max Cord Deviation", "Distance on the cord to a straight line between two points after which more tessellation will be generated.");
        private readonly GUIContent m_MaxTangentAngleEnabledText = new GUIContent("Max Tangent Enabled", "Enables the \"max tangent angle\" tessellation test.");
        private readonly GUIContent m_MaxTangentAngleText = new GUIContent("Max Tangent Angle", "Max tangent angle (in degrees) after which more tessellation will be generated.");
        private readonly GUIContent m_KeepTextureAspectRatioText = new GUIContent("Keep Aspect Ratio");
        private readonly GUIContent m_TextureSizeText = new GUIContent("Texture Size", "The size of the generated texture.");
        private readonly GUIContent m_WrapModeText = new GUIContent("Wrap Mode");
        private readonly GUIContent m_FilterModeText = new GUIContent("Filter Mode");
        private readonly GUIContent m_SampleCountText = new GUIContent("Sample Count");

        private readonly GUIContent[] svgTypeOptions =
        {
            new GUIContent("Vector Sprite", "A tessellated sprite with \"infinite\" resolution."),
            new GUIContent("Textured Sprite", "A textured sprite."),
            new GUIContent("Texture2D", "A normal texture."),
        };

        private readonly int[] svgTypeValues =
        {
            (int)SVGType.VectorSprite,
            (int)SVGType.TexturedSprite,
            (int)SVGType.Texture2D
        };

        private readonly GUIContent[] texturedSpriteMeshTypeOptions =
        {
            new GUIContent("Full Rect"),
            new GUIContent("Tight")
        };

        private readonly int[] texturedSpriteMeshTypeValues =
        {
            (int)SpriteMeshType.FullRect,
            (int)SpriteMeshType.Tight
        };

        private readonly GUIContent[] m_AlignmentOptions = new GUIContent[]
        {
            new GUIContent("Center"),
            new GUIContent("Top Left"),
            new GUIContent("Top Center"),
            new GUIContent("Top Right"),
            new GUIContent("Left Center"),
            new GUIContent("Right Center"),
            new GUIContent("Bottom Left"),
            new GUIContent("Bottom Center"),
            new GUIContent("Bottom Right"),
            new GUIContent("Custom"),
            new GUIContent("SVG Origin")
        };

        private readonly GUIContent[] m_SettingOptions = new GUIContent[]
        {
            new GUIContent("Basic"),
            new GUIContent("Advanced")
        };

        private readonly GUIContent[] m_TargetResolutionOptions = new GUIContent[]
        {
            new GUIContent("2160p (4K)"),
            new GUIContent("1080p"),
            new GUIContent("720p"),
            new GUIContent("480p"),
            new GUIContent("Custom")
        };

        public readonly GUIContent[] m_WrapModeContents =
        {
            new GUIContent("Repeat"),
            new GUIContent("Clamp"),
            new GUIContent("Mirror"),
            new GUIContent("Mirror Once")
        };

        public readonly int[] m_WrapModeValues =
        {
            (int)TextureWrapMode.Repeat,
            (int)TextureWrapMode.Clamp,
            (int)TextureWrapMode.Mirror,
            (int)TextureWrapMode.MirrorOnce
        };

        public readonly GUIContent[] m_FilterModeContents =
        {
            new GUIContent("Point"),
            new GUIContent("Bilinear"),
            new GUIContent("Trilinear")
        };

        public readonly int[] m_FilterModeValues =
        {
            (int)FilterMode.Point,
            (int)FilterMode.Bilinear,
            (int)FilterMode.Trilinear
        };

        public readonly GUIContent[] m_SampleCountContents =
        {
            new GUIContent("None"),
            new GUIContent("2 samples"),
            new GUIContent("4 samples"),
            new GUIContent("8 samples")
        };

        public readonly int[] m_SampleCountValues =
        {
            1,
            2,
            4,
            8
        };

        public override void OnEnable()
        {
            m_SVGType = serializedObject.FindProperty("m_SvgType");
            m_TexturedSpriteMeshType = serializedObject.FindProperty("m_TexturedSpriteMeshType");
            m_PixelsPerUnit = serializedObject.FindProperty("m_SvgPixelsPerUnit");
            m_GradientResolution = serializedObject.FindProperty("m_GradientResolution");
            m_Alignment = serializedObject.FindProperty("m_Alignment");
            m_CustomPivot = serializedObject.FindProperty("m_CustomPivot");
            m_GeneratePhysicsShape = serializedObject.FindProperty("m_GeneratePhysicsShape");
            m_PreserveViewport = serializedObject.FindProperty("m_PreserveViewport");
            m_AdvancedMode = serializedObject.FindProperty("m_AdvancedMode");
            m_PredefinedResolutionIndex = serializedObject.FindProperty("m_PredefinedResolutionIndex");
            m_TargetResolution = serializedObject.FindProperty("m_TargetResolution");
            m_ResolutionMultiplier = serializedObject.FindProperty("m_ResolutionMultiplier");
            m_StepDistance = serializedObject.FindProperty("m_StepDistance");
            m_SamplingStepDistance = serializedObject.FindProperty("m_SamplingStepDistance");
            m_MaxCordDeviationEnabled = serializedObject.FindProperty("m_MaxCordDeviationEnabled");
            m_MaxCordDeviation = serializedObject.FindProperty("m_MaxCordDeviation");
            m_MaxTangentAngleEnabled = serializedObject.FindProperty("m_MaxTangentAngleEnabled");
            m_MaxTangentAngle = serializedObject.FindProperty("m_MaxTangentAngle");
            m_KeepTextureAspectRatio = serializedObject.FindProperty("m_KeepTextureAspectRatio");
            m_TextureSize = serializedObject.FindProperty("m_TextureSize");
            m_TextureWidth = serializedObject.FindProperty("m_TextureWidth");
            m_TextureHeight = serializedObject.FindProperty("m_TextureHeight");
            m_WrapMode = serializedObject.FindProperty("m_WrapMode");
            m_FilterMode = serializedObject.FindProperty("m_FilterMode");
            m_SampleCount = serializedObject.FindProperty("m_SampleCount");
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_PixelsPerUnit, m_PixelsPerUnitText);
            PropertyField(m_GradientResolution, m_GradientResolutionText);
            IntPopup(m_Alignment, m_AlignmentText, m_AlignmentOptions);

            if (!m_Alignment.hasMultipleDifferentValues && m_Alignment.intValue == (int)VectorUtils.Alignment.Custom)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(m_CustomPivot, m_CustomPivotText);
                GUILayout.EndHorizontal();
            }

            using (new EditorGUI.DisabledScope(m_SVGType.hasMultipleDifferentValues || m_SVGType.intValue == (int)SVGType.Texture2D))
                IntToggle(m_GeneratePhysicsShape, m_GeneratePhysicsShapeText);

            IntToggle(m_PreserveViewport, m_PreserveViewportText);

            EditorGUILayout.Space();

            IntPopup(m_AdvancedMode, m_SettingsText, m_SettingOptions);

            ++EditorGUI.indentLevel;

            if (!m_AdvancedMode.hasMultipleDifferentValues)
            {
                if (m_AdvancedMode.boolValue)
                {
                    PropertyField(m_StepDistance, m_StepDistanceText);
                    PropertyField(m_SamplingStepDistance, m_SamplingStepDistanceText);

                    IntToggle(m_MaxCordDeviationEnabled, m_MaxCordDeviationEnabledText);
                    if (!m_MaxCordDeviationEnabled.hasMultipleDifferentValues)
                    {
                        using (new EditorGUI.DisabledScope(!m_MaxCordDeviationEnabled.boolValue))
                            PropertyField(m_MaxCordDeviation, m_MaxCordDeviationText);
                    }

                    IntToggle(m_MaxTangentAngleEnabled, m_MaxTangentAngleEnabledText);
                    if (!m_MaxTangentAngleEnabled.hasMultipleDifferentValues)
                    {
                        using (new EditorGUI.DisabledScope(!m_MaxTangentAngleEnabled.boolValue))
                            PropertyField(m_MaxTangentAngle, m_MaxTangentAngleText);
                    }
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.showMixedValue = m_PredefinedResolutionIndex.hasMultipleDifferentValues;
                    int resolutionIndex = EditorGUILayout.Popup(m_TargetResolutionText, m_PredefinedResolutionIndex.intValue, m_TargetResolutionOptions);
                    EditorGUI.showMixedValue = false;
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_PredefinedResolutionIndex.intValue = resolutionIndex;
                        if (m_PredefinedResolutionIndex.intValue != (int)SVGImporter.PredefinedResolution.Custom)
                            m_TargetResolution.intValue = TargetResolutionFromPredefinedValue((SVGImporter.PredefinedResolution)m_PredefinedResolutionIndex.intValue);
                    }

                    if (!m_PredefinedResolutionIndex.hasMultipleDifferentValues && m_PredefinedResolutionIndex.intValue == (int)SVGImporter.PredefinedResolution.Custom)
                        PropertyField(m_TargetResolution, m_CustomTargetResolutionText);

                    PropertyField(m_ResolutionMultiplier, m_ResolutionMultiplierText);
                }
            }

            --EditorGUI.indentLevel;

            EditorGUILayout.Space();

            IntPopup(m_SVGType, m_SVGTypeText, svgTypeOptions, svgTypeValues);

            if (!m_SVGType.hasMultipleDifferentValues && (m_SVGType.intValue == (int)SVGType.TexturedSprite || m_SVGType.intValue == (int)SVGType.Texture2D))
            {
                ++EditorGUI.indentLevel;

                if (m_SVGType.intValue == (int)SVGType.TexturedSprite)
                    IntPopup(m_TexturedSpriteMeshType, m_TexturedSpriteMeshTypeText, texturedSpriteMeshTypeOptions, texturedSpriteMeshTypeValues);

                PropertyField(m_KeepTextureAspectRatio, m_KeepTextureAspectRatioText);
                if (!m_KeepTextureAspectRatio.hasMultipleDifferentValues && m_KeepTextureAspectRatio.boolValue)
                {
                    PropertyField(m_TextureSize, m_TextureSizeText);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(m_TextureSizeText);
                    IntField(m_TextureWidth, GUIContent.none, GUILayout.MinWidth(40));
                    GUILayout.Label("x");
                    IntField(m_TextureHeight, GUIContent.none, GUILayout.MinWidth(40));
                    GUILayout.EndHorizontal();
                }

                IntPopup(m_WrapMode, m_WrapModeText, m_WrapModeContents, m_WrapModeValues);
                IntPopup(m_FilterMode, m_FilterModeText, m_FilterModeContents, m_FilterModeValues);
                IntPopup(m_SampleCount, m_SampleCountText, m_SampleCountContents, m_SampleCountValues);

                --EditorGUI.indentLevel;

                EditorGUILayout.Space();
            }

            if (!m_SVGType.hasMultipleDifferentValues && (m_SVGType.intValue == (int)SVGType.VectorSprite || m_SVGType.intValue == (int)SVGType.TexturedSprite))
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Sprite Editor"))
                {
                    InternalBridge.ShowSpriteEditorWindow();
                    // var spriteWindow = Type.GetType("UnityEditor.SpriteEditorWindow, UnityEditor", true);
                    // var getWindowMethod = spriteWindow.GetMethod("GetWindow", BindingFlags.Public | BindingFlags.Static);
                    // getWindowMethod.Invoke(null, null);
                }
                GUILayout.EndHorizontal();
            }

            ApplyRevertGUI();
        }

        protected override void Apply()
        {
            base.Apply();

            // Adjust every values to make sure they're in range
            foreach (var target in targets)
            {
                var svgImporter = target as SVGImporter;
                svgImporter.SvgPixelsPerUnit = Mathf.Max(0.001f, svgImporter.SvgPixelsPerUnit);
                svgImporter.GradientResolution = Math.Min((ushort)4096, Math.Max((ushort)16, svgImporter.GradientResolution));
                svgImporter.StepDistance = Mathf.Max(0.0f, svgImporter.StepDistance);
                svgImporter.SamplingStepDistance = Mathf.Clamp(svgImporter.SamplingStepDistance, 3.0f, 1000.0f);
                svgImporter.MaxCordDeviation = Mathf.Max(0.0f, svgImporter.MaxCordDeviation);
                svgImporter.MaxTangentAngle = Mathf.Clamp(svgImporter.MaxTangentAngle, 0.0f, 90.0f);
                svgImporter.TargetResolution = (int)Mathf.Max(1, svgImporter.TargetResolution);
                svgImporter.ResolutionMultiplier = Mathf.Clamp(svgImporter.ResolutionMultiplier, 1.0f, 100.0f);
                svgImporter.TextureSize = Math.Max(1, svgImporter.TextureSize);
                svgImporter.TextureWidth = Math.Max(1, svgImporter.TextureWidth);
                svgImporter.TextureHeight = Math.Max(1, svgImporter.TextureHeight);
            }
        }

        private void PropertyField(SerializedProperty prop, GUIContent label)
        {
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            EditorGUILayout.PropertyField(prop, label);
            EditorGUI.showMixedValue = false;
        }

        private void IntField(SerializedProperty prop, GUIContent label, params GUILayoutOption[] options)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            int value = EditorGUILayout.IntField(label, prop.intValue, options);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.intValue = value;
        }

        private void IntPopup(SerializedProperty prop, GUIContent label, GUIContent[] displayedOptions)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            int value = EditorGUILayout.Popup(label, prop.intValue, displayedOptions);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.intValue = value;
        }

        private void IntPopup(SerializedProperty prop, GUIContent label, GUIContent[] displayedOptions, int[] options)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            int value = EditorGUILayout.IntPopup(label, prop.intValue, displayedOptions, options);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.intValue = value;
        }

        private void IntToggle(SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            bool value = EditorGUILayout.Toggle(label, prop.boolValue);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.boolValue = value;
        }

        private int TargetResolutionFromPredefinedValue(SVGImporter.PredefinedResolution resolution)
        {
            switch (resolution)
            {
            case SVGImporter.PredefinedResolution.Res_2160p: return 2160;
            case SVGImporter.PredefinedResolution.Res_1080p: return 1080;
            case SVGImporter.PredefinedResolution.Res_720p:  return 720;
            case SVGImporter.PredefinedResolution.Res_480p:  return 480;
            default: return 1080;
            }
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {
            var sprite = SVGImporter.GetImportedSprite(assetTarget);
            if (sprite == null)
                return null;

            return BuildPreviewTexture(sprite, width, height);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            background.Draw(r, false, false, false, false);

            var sprite = SVGImporter.GetImportedSprite(assetTarget);
            if (sprite == null)
            {
                if (assetTarget is Texture2D)
                    EditorGUI.DrawTextureTransparent(r, (Texture2D)assetTarget, ScaleMode.ScaleToFit, 0.0f, 0);
                return;
            }

            float zoomLevel = Mathf.Min(r.width / sprite.rect.width, r.height / sprite.rect.height);
            Rect wantedRect = new Rect(r.x, r.y, sprite.rect.width * zoomLevel, sprite.rect.height * zoomLevel);
            wantedRect.center = r.center;

            var previewTex = BuildPreviewTexture(sprite, (int)wantedRect.width, (int)wantedRect.height);
            if (previewTex != null)
            {
                EditorGUI.DrawTextureTransparent(r, previewTex, ScaleMode.ScaleToFit);
                UnityEngine.Object.DestroyImmediate(previewTex);
            }
        }

        internal static Texture2D BuildPreviewTexture(Sprite sprite, int width, int height)
        {
            return VectorUtils.RenderSpriteToTexture2D(sprite, width, height, SVGImporter.GetSVGSpriteMaterial(sprite), 4);
        }

        public override string GetInfoString()
        {
            var sprite = SVGImporter.GetImportedSprite(assetTarget);
            if (sprite == null)
            {
                var tex = assetTarget as Texture2D;
                if (tex == null)
                    return "";
                return InternalBridge.GetTextureInfoString(tex);
            }
            
            int vertexCount = sprite.vertices.Length;
            int indexCount = sprite.triangles.Length;

            var stats = "" + vertexCount + " Vertices (Pos";

            int vertexSize = sizeof(float) * 2;
            if (sprite.HasVertexAttribute(VertexAttribute.Color))
            {
                stats += ", Col";
                vertexSize += 4;
            }
            if (sprite.HasVertexAttribute(VertexAttribute.TexCoord0))
            {
                stats += ", TexCoord0";
                vertexSize += sizeof(float) * 2;
            }
            if (sprite.HasVertexAttribute(VertexAttribute.TexCoord1))
            {
                stats += ", TexCoord1";
                vertexSize += sizeof(float) * 2;
            }
            if (sprite.HasVertexAttribute(VertexAttribute.TexCoord2))
            {
                stats += ", TexCoord2";
                vertexSize += sizeof(float) * 2;
            }

            stats += ") " + HumanReadableSize(vertexSize * vertexCount + indexCount * 2);

            return stats;
        }

        private static string HumanReadableSize(int bytes)
        {
            var units = new string[] { "B", "KB", "MB", "GB", "TB" };

            int order = 0;
            while (bytes >= 2014 && order < units.Length-1) {
                ++order;
                bytes /= 1024;
            }

            if (order >=  units.Length)
                return "" + bytes;

            return String.Format("{0:0.#} {1}", bytes, units[order]);
        }
    }
}
