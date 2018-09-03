using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.U2D;
using UnityEngine.Experimental.U2D;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.Experimental.Rendering;

namespace Unity.VectorGraphics.Editor
{
    [Serializable]
    [ScriptedImporter(1, "svg")]
    public class SVGImporter : ScriptedImporter, ISpriteEditorDataProvider
    {
        internal static readonly string kPackagePath = "Packages/com.unity.vectorgraphics";

        /// <summary>The number of pixels per Unity units.</summary>
        public float svgPixelsPerUnit = 100.0f;

        /// <summary>Maximum resolution for gradient texture.</summary>
        public UInt16 gradientResolution = 128;

        /// <summary>The SVG sprite alignement.</summary>
        public VectorUtils.Alignment alignment;

        /// <summary>The custom pivot, when alignement is "Custom".</summary>
        public Vector2 customPivot;

        /// <summary>Use advanced settings.</summary>
        public bool advancedMode = false;

        /// <summary>The predefined resolution used, when not in advanced mode.</summary>
        public int predefinedResolutionIndex = 1;

        /// <summary>The target resolution on which this SVG is displayed.</summary>
        public int targetResolution = 1080;

        /// <summary>An additional scale factor on the target resolution.</summary>
        public float resolutionMultiplier = 1.0f;

        /// <summary>The uniform step distance used for tessellation.</summary>
        public float stepDistance = 10.0f;

        /// <summary>Number of samples evaluated on paths.</summary>
        public float samplingStepDistance = 100.0f;

        /// <summary>Enables the "max coord deviation" constraint.</summary>
        public bool maxCordDeviationEnabled = false;

        /// <summary>Distance on the cord to a straight line between two points after which more tessellation will be generated.</summary>
        public float maxCordDeviation = 1.0f;

        /// <summary>Enables the "max tangent angle" constraint.</summary>
        public bool maxTangentAngleEnabled = false;

        /// <summary>Max tangent angle (in degrees) after which more tessellation will be generated.</summary>
        public float maxTangentAngle = 5.0f;

        /// <summary>Enables the animation tools for bones and skin weights in the Sprite Editor.</summary>
        public bool enableAnimationTools = false;

        [SerializeField]
        private SVGSpriteData m_SpriteData = new SVGSpriteData();
        internal SVGSpriteData GetSVGSpriteData() { return m_SpriteData; }

        internal enum PredefinedResolution
        {
            Res_2160p,
            Res_1080p,
            Res_720p,
            Res_480p,
            Custom
        }

        /// <summary>Imports an SVG asset</summary>
        /// <param name="ctx">The asset import context of the scripted importer</param>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var sceneInfo = SVGParser.ImportSVG(System.IO.Path.GetFullPath(ctx.assetPath));
            if (sceneInfo.scene == null || sceneInfo.scene.root == null)
                throw new Exception("Wowzers!");

            float stepDist = stepDistance;
            float samplingStepDist = samplingStepDistance;
            float maxCord = maxCordDeviationEnabled ? maxCordDeviation : float.MaxValue;
            float maxTangent = maxTangentAngleEnabled ? maxTangentAngle : Mathf.PI * 0.5f;

            if (!advancedMode)
            {
                // Automatically compute sensible tessellation options from the
                // vector scene's bouding box and target resolution
                ComputeTessellationOptions(sceneInfo, targetResolution, resolutionMultiplier, out stepDist, out maxCord, out maxTangent);
            }

            var tessOptions = new VectorUtils.TessellationOptions();
            tessOptions.maxCordDeviation = maxCord;
            tessOptions.maxTanAngleDeviation = maxTangent;
            tessOptions.samplingStepSize = 1.0f / (float)samplingStepDist;
            tessOptions.stepDistance = stepDist;

            var geometry = VectorUtils.TessellateNodeHierarchy(sceneInfo.scene.root, tessOptions, sceneInfo.nodeOpacity);

            var sprite = VectorUtils.BuildSprite(geometry, svgPixelsPerUnit, alignment, customPivot, gradientResolution);

            GenerateAsset(ctx, sprite);
        }

        private void GenerateAsset(AssetImportContext ctx, Sprite sprite)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(ctx.assetPath);
            sprite.name = name + "Sprite";

            // Apply GUID from SpriteRect
            var so = new SerializedObject(sprite);
            so.FindProperty("m_SpriteID").stringValue = m_SpriteData.spriteRect.spriteID.ToString();
            so.ApplyModifiedPropertiesWithoutUndo();

            ctx.AddObjectToAsset("sprite", sprite);

            Material mat = null;

            if (sprite.texture != null)
            {
                // When texture is present, use the VectorGradient shader
                mat = AssetDatabase.LoadMainAssetAtPath(kPackagePath + "/Runtime/Materials/Unlit_VectorGradient.mat") as Material;
                sprite.texture.name = name + "Atlas";
            }
            else
            {
                mat = AssetDatabase.LoadMainAssetAtPath(kPackagePath + "/Runtime/Materials/Unlit_Vector.mat") as Material;                
            }

            var gameObject = new GameObject(name);
            var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.material = mat;

            if (sprite.texture != null)
                ctx.AddObjectToAsset("texAtlas", sprite.texture);

            ctx.AddObjectToAsset("gameObject", gameObject);
            ctx.SetMainObject(gameObject);
        }

        private void ComputeTessellationOptions(SVGParser.SceneInfo sceneInfo, int targetResolution, float multiplier, out float stepDist, out float maxCord, out float maxTangent)
        {
            var bbox = VectorUtils.ApproximateSceneNodeBounds(sceneInfo.scene.root);
            float maxDim = Mathf.Max(bbox.width, bbox.height);

            // The scene ratio gives a rough estimate of coverage % of the vector scene on the screen.
            // Higher values should result in a more dense tessellation.
            float sceneRatio = (targetResolution * multiplier) / maxDim;

            stepDist = float.MaxValue; // No need for uniform step distance
            maxCord = 0.5f / sceneRatio;
            maxTangent = 1.0f / sceneRatio;
        }

        internal static Sprite GetImportedSprite(string assetPath)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            return GetImportedSprite(asset);
        }

        internal static Sprite GetImportedSprite(UnityEngine.Object asset)
        {
            var sprite = asset as Sprite;
            if (sprite == null)
            {
                var go = asset as GameObject;
                var sr = go != null ? go.GetComponent<SpriteRenderer>() : null;
                sprite = sr != null ? sr.sprite : null;
            }
            return sprite;
        }

        internal void TextureSizeForSpriteEditor(out int width, out int height)
        {
            var sprite = GetImportedSprite(assetPath);
            var size = (Vector2)sprite.bounds.size;
            size.x *= svgPixelsPerUnit;
            size.y *= svgPixelsPerUnit;
            width = (int)size.x;
            height = (int)size.y;
        }

        // ISpriteEditorDataProvider methods

        /// <summary>Returns the sprite import mode</summary>
        /// <remarks>For SVG files, the import mode is always "single"</remarks>
        public SpriteImportMode spriteImportMode
        {
             get { return SpriteImportMode.Single; }
        }

        /// <summary>Returns pixels per unit of the imported SVG</summary>
        public float pixelsPerUnit
        {
            get { return svgPixelsPerUnit; }
        }
        
        /// <summary>Returns imported sprite</summary>
        public UnityEngine.Object targetObject
        {
            get { return GetImportedSprite(assetPath); }
        }

        /// <summary>Returns the sprite rectangles</summary>
        SpriteRect[] ISpriteEditorDataProvider.GetSpriteRects()
        {
            return new SpriteRect[] { m_SpriteData.spriteRect };
        }

        /// <summary>Sets the sprite rectangles</summary>
        /// <param name="rects">The new sprite rectangles to use</param>
        void ISpriteEditorDataProvider.SetSpriteRects(SpriteRect[] rects)
        {
            if (rects.Length > 0)
                m_SpriteData.spriteRect = rects[0];
        }

        /// <summary>Applies the modified SVG data</summary>
        void ISpriteEditorDataProvider.Apply()
        {
            var so = new SerializedObject(this);
            m_SpriteData.Apply(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Initializes the sprite editor data</summary>
        void ISpriteEditorDataProvider.InitSpriteEditorDataProvider()
        {
            var so = new SerializedObject(this);
            m_SpriteData.Load(so);
        }

        /// <summary>Gets the data provider for a given type</summary>
        T ISpriteEditorDataProvider.GetDataProvider<T>()
        {
            if (enableAnimationTools && typeof(T) == typeof(ISpriteBoneDataProvider))
            {
                return new SVGBoneDataProvider(this) as T;                
            }
            if (enableAnimationTools && typeof(T) == typeof(ISpriteMeshDataProvider))
            {
                return new SVGMeshDataProvider(this) as T;
            }
            if (typeof(T) == typeof(ISpritePhysicsOutlineDataProvider))
            {
                return new SVGPhysicsOutlineDataProvider(this) as T;
            }
            if (typeof(T) == typeof(ITextureDataProvider))
            {
                return new SVGTextureDataProvider(this) as T;
            }
            else if (typeof(T) == typeof(ISpriteEditorDataProvider))
            {
                return this as T;
            }
            return null;
        }

        /// <summary>Gets the data provider for a given type</summary>
        /// <param name="type">The type</param>
        /// <returns>True if a data provider is available for the type, or false otherwise</returns>
        bool ISpriteEditorDataProvider.HasDataProvider(Type type)
        {
            if (enableAnimationTools)
            {
                if (type == typeof(ISpriteBoneDataProvider) ||
                    type == typeof(ISpriteMeshDataProvider))
                {
                    return true;
                }
            }
            if (type == typeof(ISpritePhysicsOutlineDataProvider) ||
                type == typeof(ITextureDataProvider))
            {
                return true;
            }
            return false;
        }
    }
}
