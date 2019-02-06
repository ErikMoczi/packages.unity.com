using System;
using System.Collections.Generic;
using System.IO;
using PDNWrapper;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using Unity.Collections;
using System.Linq;
using UnityEditor.Experimental.U2D.Animation;
using UnityEditor.Experimental.U2D.Common;
using UnityEngine.Assertions;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.U2D.Animation;

namespace UnityEditor.Experimental.U2D.PSD
{
    [ScriptedImporter(1, "psb")]
    public class PSDImporter : ScriptedImporter, ISpriteEditorDataProvider, IAnimationAssetPostProcess
    {
        [Serializable]
        struct BoneGO
        {
            public GameObject go;
            public int index;
        }

        [SerializeField]
        TextureImporterSettings m_TextureImporterSettings;
        [SerializeField]
        List<SpriteMetaData> m_SpriteImportData = new List<SpriteMetaData>(); // we use index 0 for single sprite and the rest for multiple sprites
        [SerializeField]
        List<SpriteMetaData> m_MosaicSpriteImportData = new List<SpriteMetaData>();
        [SerializeField]
        List<SpriteMetaData> m_RigSpriteImportData = new List<SpriteMetaData>();

        [SerializeField]
        List<TextureImporterPlatformSettings> m_PlatformSettings = new List<TextureImporterPlatformSettings>();
        [SerializeField]
        bool m_MosaicLayers = true;
        [SerializeField]
        Vector2 m_DocumentPivot = Vector2.zero;
        [SerializeField]
        SpriteAlignment m_DocumentAlignment = SpriteAlignment.BottomCenter;
        [SerializeField]
        bool m_ImportHiddenLayers = false;
        [SerializeField]
        int m_ImportedTextureWidth;
        [SerializeField]
        int m_ImportedTextureHeight;
        [SerializeField]
        Vector2Int m_DocumentSize;

        [SerializeField]
        int m_TextureActualWidth;
        public int textureActualWidth
        {
            get { return m_TextureActualWidth; }
            private set { m_TextureActualWidth = value; }
        }

        [SerializeField]
        int m_TextureActualHeight;
        public int textureActualHeight
        {
            get { return m_TextureActualHeight; }
            private set { m_TextureActualHeight = value; }
        }

        [SerializeField]
        string m_SpritePackingTag = "";

        [SerializeField]
        bool m_ResliceFromLayer = false;
        [SerializeField]
        bool m_CharacterMode = true;

        [SerializeField]
        List<PSDLayer> m_MosaicPSDLayers = new List<PSDLayer>();
        [SerializeField]
        List<PSDLayer> m_RigPSDLayers = new List<PSDLayer>();

        [SerializeField]
        CharacterData m_CharacterData = new CharacterData();

        [SerializeField]
        BoneGO[] m_BoneGOs;

        [SerializeField]
        bool m_GenerateGOHierarchy = false;

        public PSDImporter()
        {
            m_TextureImporterSettings = new TextureImporterSettings();
            m_TextureImporterSettings.mipmapEnabled = true;
            m_TextureImporterSettings.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            m_TextureImporterSettings.sRGBTexture = true;
            m_TextureImporterSettings.borderMipmap = false;
            m_TextureImporterSettings.mipMapsPreserveCoverage = false;
            m_TextureImporterSettings.alphaTestReferenceValue = 0.5f;
            m_TextureImporterSettings.readable = false;

#if ENABLE_TEXTURE_STREAMING
            m_TextureImporterSettings.streamingMipmaps = true;
#endif

            m_TextureImporterSettings.fadeOut = false;
            m_TextureImporterSettings.mipmapFadeDistanceStart = 1;
            m_TextureImporterSettings.mipmapFadeDistanceEnd = 3;

            m_TextureImporterSettings.convertToNormalMap = false;
            m_TextureImporterSettings.heightmapScale = 0.25F;
            m_TextureImporterSettings.normalMapFilter = 0;

            m_TextureImporterSettings.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
            m_TextureImporterSettings.cubemapConvolution = 0;

            m_TextureImporterSettings.seamlessCubemap = false;

            m_TextureImporterSettings.npotScale = TextureImporterNPOTScale.ToNearest;

            m_TextureImporterSettings.spriteMode = (int)SpriteImportMode.Multiple;
            m_TextureImporterSettings.spriteExtrude = 1;
            m_TextureImporterSettings.spriteMeshType = SpriteMeshType.Tight;
            m_TextureImporterSettings.spriteAlignment = (int)SpriteAlignment.Center;
            m_TextureImporterSettings.spritePivot = new Vector2(0.5f, 0.5f);
            m_TextureImporterSettings.spritePixelsPerUnit = 100.0f;
            m_TextureImporterSettings.spriteBorder = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            m_TextureImporterSettings.alphaSource = TextureImporterAlphaSource.FromInput;
            m_TextureImporterSettings.alphaIsTransparency = true;
            m_TextureImporterSettings.spriteTessellationDetail = -1.0f;

            m_TextureImporterSettings.textureType = TextureImporterType.Sprite;
            m_TextureImporterSettings.textureShape = TextureImporterShape.Texture2D;

            m_TextureImporterSettings.filterMode = FilterMode.Bilinear;
            m_TextureImporterSettings.aniso = 1;
            m_TextureImporterSettings.mipmapBias = 0.0f;
            m_TextureImporterSettings.wrapModeU = TextureWrapMode.Repeat;
            m_TextureImporterSettings.wrapModeV = TextureWrapMode.Repeat;
            m_TextureImporterSettings.wrapModeW = TextureWrapMode.Repeat;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            string ext = System.IO.Path.GetExtension(ctx.assetPath).ToLower();
            if (ext != ".psb")
                throw new Exception("File does not have psb extension");

            m_BoneGOs = null;

            FileStream fileStream = new FileStream(ctx.assetPath, FileMode.Open, FileAccess.Read);
            Document doc = null;
            try
            {
                UnityEngine.Profiling.Profiler.BeginSample("OnImportAsset");

                UnityEngine.Profiling.Profiler.BeginSample("PsdLoad");
                doc = PaintDotNet.Data.PhotoshopFileType.PsdLoad.Load(fileStream);
                UnityEngine.Profiling.Profiler.EndSample();

                // Is layer id truely unique?
                for (int i = 0; i < doc.Layers.Count; ++i)
                {
                    for (int j = 0; j < doc.Layers.Count; ++j)
                    {
                        if (i == j)
                            continue;
                        if (doc.Layers[i].LayerID == doc.Layers[j].LayerID)
                        {
                            Debug.LogWarning("File's Layer ID is not unique. Please report to developer. " + doc.Layers[i].LayerID + " " + doc.Layers[i].Name + "::" + doc.Layers[j].Name);
                            doc.Layers[i].LayerID = doc.Layers[i].Name.GetHashCode();
                        }
                    }
                }

                m_DocumentSize = new Vector2Int(doc.width, doc.height);
                bool singleSpriteMode = m_TextureImporterSettings.textureType == TextureImporterType.Sprite && m_TextureImporterSettings.spriteMode != (int)SpriteImportMode.Multiple;
                EnsureSingleSpriteExist();

                if (m_TextureImporterSettings.textureType != TextureImporterType.Sprite ||
                    m_MosaicLayers == false || singleSpriteMode)
                {
                    var image = new NativeArray<Color32>(doc.width * doc.height, Allocator.Temp);
                    try
                    {
                        var spriteImportData = GetSpriteImportData();
                        FlattenImageTask.Execute(doc.Layers, m_ImportHiddenLayers, doc.width, doc.height, image);

                        int spriteCount = spriteDataCount;
                        int spriteIndexStart = 1;

                        if (spriteImportData.Count <= 0 || spriteImportData[0] == null)
                        {
                            spriteImportData.Add(new SpriteMetaData());
                        }
                        spriteImportData[0].name = System.IO.Path.GetFileNameWithoutExtension(ctx.assetPath) +"_1";
                        spriteImportData[0].alignment = (SpriteAlignment)m_TextureImporterSettings.spriteAlignment;
                        spriteImportData[0].border = m_TextureImporterSettings.spriteBorder;
                        spriteImportData[0].pivot = m_TextureImporterSettings.spritePivot;
                        spriteImportData[0].rect = new Rect(0, 0, doc.width, doc.height);
                        if (singleSpriteMode)
                        {
                            spriteCount = 1;
                            spriteIndexStart = 0;
                        }
                        textureActualWidth = doc.width;
                        textureActualHeight = doc.height;
                        var output = ImportTexture(ctx, image, doc.width, doc.height, spriteIndexStart, spriteCount);
                        RegisterAssets(ctx, output);
                    }
                    finally
                    {
                        image.Dispose();
                    }
                }
                else
                {
                    ImportFromLayers(ctx, doc);
                }
            }
            finally
            {
                fileStream.Close();
                if (doc != null)
                    doc.Dispose();
                UnityEngine.Profiling.Profiler.EndSample();
            }
        }

        TextureGenerationOutput ImportTexture(AssetImportContext ctx, NativeArray<Color32> imageData, int textureWidth, int textureHeight, int spriteStart, int spriteCount)
        {
            UnityEngine.Profiling.Profiler.BeginSample("ImportTexture");
            var platformSettings = GetPlatformTextureSettings(ctx.selectedBuildTarget);

            var textureSettings = m_TextureImporterSettings.ExtractTextureSettings();
            textureSettings.assetPath = ctx.assetPath;
            textureSettings.enablePostProcessor = false;
            textureSettings.containsAlpha = true;
            textureSettings.hdr = false;

            var textureAlphaSettings = m_TextureImporterSettings.ExtractTextureAlphaSettings();
            var textureMipmapSettings = m_TextureImporterSettings.ExtractTextureMipmapSettings();
            var textureCubemapSettings = m_TextureImporterSettings.ExtractTextureCubemapSettings();
            var textureWrapSettings = m_TextureImporterSettings.ExtractTextureWrapSettings();

            TextureGenerationOutput output;
            switch (m_TextureImporterSettings.textureType)
            {
                case TextureImporterType.Default:
                    output = TextureGeneratorHelper.GenerateTextureDefault(imageData, textureWidth, textureHeight, textureSettings, platformSettings, textureAlphaSettings, textureMipmapSettings, textureCubemapSettings, textureWrapSettings);
                    break;
                case TextureImporterType.NormalMap:
                    var textureNormalSettings = m_TextureImporterSettings.ExtractTextureNormalSettings();
                    output = TextureGeneratorHelper.GenerateNormalMap(imageData, textureWidth, textureHeight, textureSettings, platformSettings, textureNormalSettings, textureMipmapSettings, textureCubemapSettings, textureWrapSettings);
                    break;
                case TextureImporterType.GUI:
                    output = TextureGeneratorHelper.GenerateTextureGUI(imageData, textureWidth, textureHeight, textureSettings, platformSettings, textureAlphaSettings, textureMipmapSettings, textureWrapSettings);
                    break;
                case TextureImporterType.Sprite:
                    var textureSpriteSettings = m_TextureImporterSettings.ExtractTextureSpriteSettings();
                    textureSpriteSettings.packingTag = m_SpritePackingTag;
                    textureSpriteSettings.qualifyForPacking = !string.IsNullOrEmpty(m_SpritePackingTag);
                    textureSpriteSettings.spriteSheetData = new UnityEditor.Experimental.AssetImporters.SpriteImportData[spriteCount];
                    textureSettings.npotScale = TextureImporterNPOTScale.None;
                    var spriteImportData = GetSpriteImportData();
                    for (int i = 0; i < spriteCount; ++i)
                    {
                        //AutoGenerateSpriteSkinData(m_SpriteImportData[spriteStart + i]);
                        textureSpriteSettings.spriteSheetData[i] = spriteImportData[spriteStart + i];
                    }
                    output = TextureGeneratorHelper.GenerateTextureSprite(imageData, textureWidth, textureHeight, textureSettings, platformSettings, textureSpriteSettings, textureAlphaSettings, textureMipmapSettings, textureWrapSettings);
                    break;
                case TextureImporterType.Cursor:
                    output = TextureGeneratorHelper.GenerateTextureCursor(imageData, textureWidth, textureHeight, textureSettings, platformSettings, textureAlphaSettings, textureMipmapSettings, textureWrapSettings);
                    break;
                case TextureImporterType.Cookie:
                    output = TextureGeneratorHelper.GenerateCookie(imageData, textureWidth, textureHeight, textureSettings, platformSettings, textureAlphaSettings, textureMipmapSettings, textureCubemapSettings, textureWrapSettings);
                    break;
                case TextureImporterType.Lightmap:
                    output = TextureGeneratorHelper.GenerateLightmap(imageData, textureWidth, textureHeight, textureSettings, platformSettings, textureMipmapSettings, textureWrapSettings);
                    break;
                case TextureImporterType.SingleChannel:
                    output = TextureGeneratorHelper.GenerateTextureSingleChannel(imageData, textureWidth, textureHeight, textureSettings, platformSettings, textureAlphaSettings, textureMipmapSettings, textureCubemapSettings, textureWrapSettings);
                    break;
                default:
                    Debug.LogAssertion("Unknown texture type for import");
                    output = default(TextureGenerationOutput);
                    break;
            }
            UnityEngine.Profiling.Profiler.EndSample();
            return output;
        }

        void AutoGenerateSpriteSkinData(SpriteMetaData metaData)
        {
            //If bone data exist but skinning data doesn't exist, auto generate them
            if (metaData.spriteBone != null && metaData.spriteBone.Count > 0 &&
                (metaData.vertices == null || metaData.vertices.Count == 0))
            {
                var spriteMeshDataController = new SpriteMeshDataController();
                var smd = new SpriteMeshData();
                smd.spriteID = metaData.spriteID;
                smd.frame = metaData.rect;
                smd.pivot = metaData.pivot;
                smd.bones = ModuleUtility.CreateSpriteBoneData(metaData.spriteBone.ToArray(), Matrix4x4.TRS(metaData.rect.position, Quaternion.identity, Vector3.one));
                spriteMeshDataController.spriteMeshData = smd;
                spriteMeshDataController.OutlineFromAlpha(new OutlineGenerator(), GetDataProvider<ITextureDataProvider>(), 0.05f, 200);
                spriteMeshDataController.Triangulate(new Triangulator());
                spriteMeshDataController.Subdivide(new Triangulator(), 0.25f);
                spriteMeshDataController.CalculateWeights(new BoundedBiharmonicWeightsGenerator(), null, 0.01f);
                spriteMeshDataController.SortTrianglesByDepth();

                List<Vertex2DMetaData> vmd = new List<Vertex2DMetaData>(smd.vertices.Count);
                foreach (var v in smd.vertices)
                    vmd.Add(new Vertex2DMetaData() { position = v.position - smd.frame.position, boneWeight = v.editableBoneWeight.ToBoneWeight(true) });
                List<Vector2Int> emd = new List<Vector2Int>(smd.edges.Count);
                foreach (var e in smd.edges)
                    emd.Add(new Vector2Int(e.index1, e.index2));

                metaData.vertices = vmd;
                metaData.indices = smd.indices.ToArray();
                metaData.edges = emd.ToArray();
            }
        }

        void ImportFromLayers(AssetImportContext ctx, Document doc)
        {
            NativeArray<Color32> output = default(NativeArray<Color32>);

            List<int> layerIndex = new List<int>();
            List<int> spriteNameHash = new List<int>();
            var oldPsdLayers = GetPSDLayers();
            try
            {
                var psdLayers = new List<PSDLayer>();
                ExtractLayerTask.Execute(psdLayers, doc.Layers, m_ImportHiddenLayers);
                var removedLayersSprite = oldPsdLayers.Where(x => psdLayers.FirstOrDefault(y => y.layerID == x.layerID) == null).Select(z => z.spriteID).ToArray();
                for (int i = 0; i < psdLayers.Count; ++i)
                {
                    int j = 0;
                    var psdLayer = psdLayers[i];
                    for (; j < oldPsdLayers.Count; ++j)
                    {
                        if (psdLayer.layerID == oldPsdLayers[j].layerID)
                        {
                            psdLayer.spriteID = oldPsdLayers[j].spriteID;
                            psdLayer.spriteName = oldPsdLayers[j].spriteName;
                            psdLayer.mosaicPosition = oldPsdLayers[j].mosaicPosition;
                            break;
                        }
                    }
                }

                int expectedBufferLength = doc.width * doc.height;
                var layerBuffers = new List<NativeArray<Color32>>();
                for (int i = 0; i < psdLayers.Count; ++i)
                {
                    var l = psdLayers[i];
                    if (l.texture.IsCreated && l.texture.Length == expectedBufferLength)
                    {
                        layerBuffers.Add(l.texture);
                        layerIndex.Add(i);
                    }
                }

                RectInt[] spritedata;
                int width, height;
                int padding = 4;
                Vector2Int[] uvTransform;
                ImagePacker.Pack(layerBuffers.ToArray(), doc.width, doc.height, padding, out output, out width, out height, out spritedata, out uvTransform);
                var spriteImportData = GetSpriteImportData();
                if (spriteImportData.Count <= 0 || shouldResliceFromLayer)
                {
                    var newSpriteMeta = new List<SpriteMetaData>();

                    for (int i = 0; i < spritedata.Length && i < layerIndex.Count; ++i)
                    {
                        var spriteSheet = spriteImportData.FirstOrDefault(x => x.spriteID == psdLayers[layerIndex[i]].spriteID);
                        if (spriteSheet == null)
                        {
                            spriteSheet = new SpriteMetaData();
                            spriteSheet.border = Vector4.zero;
                            spriteSheet.alignment = (SpriteAlignment)m_TextureImporterSettings.spriteAlignment;
                            spriteSheet.pivot = m_TextureImporterSettings.spritePivot;
                        }

                        psdLayers[layerIndex[i]].spriteName = GetUniqueName(psdLayers[layerIndex[i]].name, spriteNameHash);
                        spriteSheet.name = psdLayers[layerIndex[i]].spriteName;
                        spriteSheet.rect = new Rect(spritedata[i].x, spritedata[i].y, spritedata[i].width, spritedata[i].height);
                        spriteSheet.uvTransform = uvTransform[i];

                        psdLayers[layerIndex[i]].spriteID = spriteSheet.spriteID;
                        psdLayers[layerIndex[i]].mosaicPosition = spritedata[i].position;
                        newSpriteMeta.Add(spriteSheet);
                    }
                    spriteImportData.Clear();
                    spriteImportData.AddRange(newSpriteMeta);
                }
                else
                {
                    spriteImportData.RemoveAll(x => removedLayersSprite.Contains(x.spriteID));

                    foreach (var spriteData in spriteImportData)
                    {
                        var psdLayer = psdLayers.FirstOrDefault(x => x.spriteID == spriteData.spriteID);
                        if (psdLayer == null)
                            spriteData.uvTransform = new Vector2Int((int)spriteData.rect.position.x, (int)spriteData.rect.position.y);
                        // If it is user created rect or the name has been changed before
                        // add it into the spriteNameHash and we don't copy it over from the layer
                        if (psdLayer == null || psdLayer.spriteName != spriteData.name)
                            spriteNameHash.Add(spriteData.name.GetHashCode());

                        // If the sprite name has not been changed, we ensure the new
                        // layer name is still unique and use it as the sprite name
                        if (psdLayer != null && psdLayer.spriteName == spriteData.name)
                        {
                            psdLayer.spriteName = GetUniqueName(psdLayer.name, spriteNameHash);
                            spriteData.name = psdLayer.spriteName;
                        }
                    }

                    //Update names for those user has not changed and add new sprite rect based on PSD file.
                    for (int k = 0; k < layerIndex.Count; ++k)
                    {
                        int i = layerIndex[k];
                        var spriteSheet = spriteImportData.FirstOrDefault(x => x.spriteID == psdLayers[i].spriteID);
                        var inOldLayer = oldPsdLayers.FindIndex(x => x.layerID == psdLayers[i].layerID) != -1;
                        if (spriteSheet == null && !inOldLayer)
                        {
                            spriteSheet = new SpriteMetaData();
                            spriteImportData.Add(spriteSheet);
                            spriteSheet.rect = new Rect(spritedata[k].x, spritedata[k].y, spritedata[k].width, spritedata[k].height);
                            spriteSheet.border = Vector4.zero;
                            spriteSheet.alignment = (SpriteAlignment)m_TextureImporterSettings.spriteAlignment;
                            spriteSheet.pivot = m_TextureImporterSettings.spritePivot;
                            psdLayers[i].spriteName = GetUniqueName(psdLayers[i].name, spriteNameHash);
                            spriteSheet.name = psdLayers[i].spriteName;
                        }
                        else if (spriteSheet != null)
                        {
                            var r = spriteSheet.rect;
                            r.position = spriteSheet.rect.position - psdLayers[i].mosaicPosition + spritedata[k].position;
                            spriteSheet.rect = r;
                        }

                        if (spriteSheet != null)
                        {
                            spriteSheet.uvTransform = uvTransform[k];
                            psdLayers[i].spriteID = spriteSheet.spriteID;
                            psdLayers[i].mosaicPosition = spritedata[k].position;
                        }
                    }
                }
                oldPsdLayers.Clear();
                oldPsdLayers.AddRange(psdLayers);
                m_ResliceFromLayer = false;
                m_ImportedTextureHeight = textureActualHeight = height;
                m_ImportedTextureWidth = textureActualWidth = width;
                var generatedTexture = ImportTexture(ctx, output, width, height, 0, spriteImportData.Count);
                m_ImportedTextureHeight = generatedTexture.texture.height;
                m_ImportedTextureWidth = generatedTexture.texture.width;
                RegisterAssets(ctx, generatedTexture);
            }
            finally
            {
                if (output.IsCreated)
                    output.Dispose();
                foreach (var l in oldPsdLayers)
                    l.Dispose();
            }
        }

        void EnsureSingleSpriteExist()
        {
            if (m_SpriteImportData.Count <= 0)
                m_SpriteImportData.Add(new SpriteMetaData()); // insert default for single sprite mode
        }

        public TextureImporterPlatformSettings GetPlatformTextureSettings(BuildTarget buildTarget)
        {
            var buildTargetName = TexturePlatformSettingsModal.kValidBuildPlatform.FirstOrDefault(x => x.buildTarget.Contains(buildTarget));
            var defaultTargetName = TexturePlatformSettingsModal.kValidBuildPlatform.FirstOrDefault(x => x.buildTarget.Contains(BuildTarget.NoTarget));
            TextureImporterPlatformSettings platformSettings = null;
            platformSettings = m_PlatformSettings.SingleOrDefault(x => x.name == buildTargetName.buildTargetName);
            platformSettings =  platformSettings ?? m_PlatformSettings.SingleOrDefault(x => x.name == defaultTargetName.buildTargetName);

            if (platformSettings == null)
            {
                platformSettings = new TextureImporterPlatformSettings();
                platformSettings.name = name;
                platformSettings.overridden = false;
            }
            return platformSettings;
        }

        void RegisterAssets(AssetImportContext ctx, TextureGenerationOutput output)
        {
            List<int> assetNameHash = new List<int>();
            if (!string.IsNullOrEmpty(output.importInspectorWarnings))
            {
                Debug.LogWarning(output.importInspectorWarnings);
            }
            if (output.importWarnings != null && output.importWarnings.Length != 0)
            {
                foreach (var warning in output.importWarnings)
                    Debug.LogWarning(warning);
            }
            if (output.thumbNail == null)
                Debug.LogWarning("Thumbnail generation fail");
            if (output.texture == null)
            {
                throw new Exception("Texture import fail");
            }
            var assetName = GetUniqueName(System.IO.Path.GetFileNameWithoutExtension(ctx.assetPath), assetNameHash, true);
            output.texture.name = assetName;
            ctx.AddObjectToAsset(assetName, output.texture, output.thumbNail);
            UnityEngine.Object mainAsset = output.texture;

            
            if (output.sprites != null)
            {
                foreach (var s in output.sprites)
                {
                    assetName = GetUniqueName(s.name, assetNameHash, true, s);
                    ctx.AddObjectToAsset(assetName, s);
                }


                if (shouldProduceGameObject)
                {
                    var prefab = OnProducePrefab(assetName, output.sprites);
                    if (prefab != null)
                    {
                        assetName = GetUniqueName(prefab.name, assetNameHash, true, prefab);
                        ctx.AddObjectToAsset(assetName, prefab);
                        mainAsset = prefab;
                    }
                }
            }
            ctx.SetMainObject(mainAsset);
        }

        void BuildGroupGameObject(List<PSDLayer> psdGroup, int index, Transform root)
        {
            var spriteData = GetSpriteImportData().FirstOrDefault(x => x.spriteID == psdGroup[index].spriteID);
            if (psdGroup[index].gameObject == null)
            {
                if (m_GenerateGOHierarchy || !psdGroup[index].spriteID.Empty())
                    psdGroup[index].gameObject = new GameObject(spriteData  != null ? spriteData.name : psdGroup[index].name);
                if (psdGroup[index].parentIndex >= 0 && m_GenerateGOHierarchy)
                {
                    BuildGroupGameObject(psdGroup, psdGroup[index].parentIndex, root);
                    psdGroup[index].gameObject.transform.SetParent(psdGroup[psdGroup[index].parentIndex].gameObject.transform);
                }
                else if (psdGroup[index].gameObject != null)
                    psdGroup[index].gameObject.transform.SetParent(root);
            }
        }

        bool shouldProduceGameObject
        {
            get { return m_CharacterMode && m_MosaicLayers && spriteImportMode == SpriteImportMode.Multiple; }
        }

        bool shouldResliceFromLayer
        {
            get { return m_ResliceFromLayer && m_MosaicLayers && spriteImportMode == SpriteImportMode.Multiple; }
        }

        bool characterMode
        {
            get { return mosaicMode && m_CharacterMode == true; }
        }

        float definitionScale
        {
            get
            {
                float definitionScaleW = m_ImportedTextureWidth / (float)textureActualWidth;
                float definitionScaleH = m_ImportedTextureHeight / (float)textureActualHeight;
                return Mathf.Min(definitionScaleW, definitionScaleH);
            }
        }

        private Vector2 GetPivotPoint(Rect rect, SpriteAlignment alignment)
        {
            switch (alignment)
            {
                case SpriteAlignment.TopLeft:
                    return new Vector2(rect.xMin, rect.yMax);

                case SpriteAlignment.TopCenter:
                    return new Vector2(rect.center.x, rect.yMax);

                case SpriteAlignment.TopRight:
                    return new Vector2(rect.xMax, rect.yMax);

                case SpriteAlignment.LeftCenter:
                    return new Vector2(rect.xMin, rect.center.y);

                case SpriteAlignment.Center:
                    return new Vector2(rect.center.x, rect.center.y);

                case SpriteAlignment.RightCenter:
                    return new Vector2(rect.xMax, rect.center.y);

                case SpriteAlignment.BottomLeft:
                    return new Vector2(rect.xMin, rect.yMin);

                case SpriteAlignment.BottomCenter:
                    return new Vector2(rect.center.x, rect.yMin);

                case SpriteAlignment.BottomRight:
                    return new Vector2(rect.xMax, rect.yMin);

                case SpriteAlignment.Custom:
                    return new Vector2(m_DocumentPivot.x * rect.width, m_DocumentPivot.y * rect.height);
            }
            return Vector2.zero;
        }

        void CreateBoneGO(int index, SpriteBone[] bones, BoneGO[] bonesGO, Transform defaultRoot)
        {
            if (bonesGO[index].go != null)
                return;
            var bone = bones[index];
            if (bone.parentId != -1 && bonesGO[bone.parentId].go == null)
                CreateBoneGO(bone.parentId, bones, bonesGO, defaultRoot);

            var go = new GameObject(bone.name);
            if (bone.parentId == -1)
                go.transform.SetParent(defaultRoot);
            else
                go.transform.SetParent(bonesGO[bone.parentId].go.transform);
            go.transform.localPosition = bone.position * 1 / pixelsPerUnit;
            go.transform.localRotation = bone.rotation;
            bonesGO[index] = new BoneGO()
            {
                go = go,
                index = index
            };
        }

        BoneGO[] CreateBonesGO(Transform root)
        {
            if (characterMode)
            {
                var characterSkeleton = GetDataProvider<ICharacterDataProvider>().GetCharacterData();
                var bones = characterSkeleton.bones;
                if (bones != null)
                {
                    var boneGOs = new BoneGO[bones.Length];
                    for (int i = 0; i < bones.Length; ++i)
                    {
                        CreateBoneGO(i, bones, boneGOs, root);
                    }
                    return boneGOs;
                }
            }
            return new BoneGO[0];
        }

        GameObject OnProducePrefab(string assetname, Sprite[] sprites)
        {
            GameObject root = null;
            CharacterData? characterSkeleton = characterMode ? new CharacterData ? (GetDataProvider<ICharacterDataProvider>().GetCharacterData()) : null;
            if (sprites != null && sprites.Length > 0)
            {
                var spriteImportData = GetSpriteImportData();
                root = new GameObject();
                root.name = assetname + "_GO";
                var psdLayers = GetPSDLayers();
                for (int i = 0; i < psdLayers.Count; ++i)
                {
                    BuildGroupGameObject(psdLayers, i, root.transform);
                    var l = psdLayers[i];
                    GUID layerSpriteID = l.spriteID;
                    var sprite = sprites.FirstOrDefault(x => x.GetSpriteID() == layerSpriteID);
                    var spriteMetaData = spriteImportData.FirstOrDefault(x => x.spriteID == layerSpriteID);
                    if (sprite != null && spriteMetaData != null)
                    {
                        var spriteRenderer = l.gameObject.AddComponent<SpriteRenderer>();
                        spriteRenderer.sprite = sprite;
                        spriteRenderer.sortingOrder = psdLayers.Count - i;
                        var uvTransform = spriteMetaData.uvTransform;
                        var outlineOffset = new Vector2(spriteMetaData.rect.x - uvTransform.x + (spriteMetaData.pivot.x * spriteMetaData.rect.width),
                                spriteMetaData.rect.y - uvTransform.y + (spriteMetaData.pivot.y * spriteMetaData.rect.height)) * definitionScale / sprite.pixelsPerUnit;
                        l.gameObject.transform.position = new Vector3(outlineOffset.x, outlineOffset.y, 0);

                        if (characterSkeleton != null)
                        {
                            var part = characterSkeleton.Value.parts.FirstOrDefault(x => x.spriteId == spriteMetaData.spriteID.ToString());
                            if (part.bones != null && part.bones.Length > 0)
                            {
                                l.gameObject.AddComponent<SpriteSkin>();
                            }
                        }
                    }
                }

                m_BoneGOs = CreateBonesGO(root.transform);

                var prefabBounds = new Rect(0 , 0, m_DocumentSize.x / pixelsPerUnit, m_DocumentSize.y / pixelsPerUnit);
                var documentPivot = (Vector3)GetPivotPoint(prefabBounds, m_DocumentAlignment);
                for (int i = 0; i < psdLayers.Count; ++i)
                {
                    var l = psdLayers[i];
                    if (l.gameObject == null || l.gameObject.GetComponent<SpriteRenderer>() == null)
                        continue;
                    var p = l.gameObject.transform.localPosition;
                    p -= documentPivot;
                    l.gameObject.transform.localPosition = p;
                }
                for (int i = 0; i < m_BoneGOs.Length; ++i)
                {
                    if (m_BoneGOs[i].go.transform.parent != root.transform)
                        continue;
                    var p = m_BoneGOs[i].go.transform.position;
                    p -= documentPivot;
                    m_BoneGOs[i].go.transform.position = p;
                }
            }

            return root;
        }

        Bounds? GetBoundingBox(GameObject root)
        {
            Bounds? prefabBounds1 = null;
            var sr = root.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                prefabBounds1 = sr.bounds;
            }
            for (int i = 0; i < root.transform.childCount; ++i)
            {
                var b = GetBoundingBox(root.transform.GetChild(i).gameObject);
                if (prefabBounds1 == null)
                    prefabBounds1 = b;
                else
                {
                    if (b.HasValue)
                    {
                        var bb = prefabBounds1.Value;
                        bb.Encapsulate(b.Value);
                        prefabBounds1 = bb;
                    }
                }
            }
            return prefabBounds1;
        }

        bool CleanUpGameobjectsWithOutRig(GameObject root)
        {
            var sr = root.GetComponent<SpriteRenderer>();
            var canDelete = true;
            if (sr != null && sr.sprite != null)
            {
                try
                {
                    var bones = GetDataProvider<ISpriteBoneDataProvider>().GetBones(sr.sprite.GetSpriteID());
                    canDelete = bones == null || bones.Count == 0;
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }


            List<GameObject> cleanup = new List<GameObject>();
            for (int i = 0; i < root.transform.childCount; ++i)
            {
                var go = root.transform.GetChild(i);
                if (CleanUpGameobjectsWithOutRig(go.gameObject))
                    cleanup.Add(go.gameObject);
            }

            for (int i = 0; i < cleanup.Count; ++i)
                GameObject.DestroyImmediate(cleanup[i]);
            cleanup.Clear();

            if (root.transform.childCount == 0 && canDelete)
                return true;

            return false;
        }

        static string SanitizeName(string name)
        {
            string newName = null;
            // We can't create asset name with these name.
            if ((name.Length == 2 && name[0] == '.' && name[1] == '.')
                || (name.Length == 1 && name[0] == '.')
                || (name.Length == 1 && name[0] == '/'))
                newName += name + "_";

            if (!string.IsNullOrEmpty(newName))
            {
                Debug.LogWarning(string.Format("File contains layer with invalid name for generating asset. {0} is renamed to {1}", name, newName));
                return newName;
            }
            return name;
        }

        static string GetUniqueName(string name, List<int> stringHash, bool logNewNameGenerated = false, UnityEngine.Object context = null)
        {
            string uniqueName = string.Copy(SanitizeName(name));
            int index = 1;
            while (true)
            {
                int hash = uniqueName.GetHashCode();
                var p = stringHash.Where(x => x == hash);
                if (!p.Any())
                {
                    stringHash.Add(hash);
                    if (logNewNameGenerated && name != uniqueName)
                        Debug.Log(string.Format("Asset name {0} is changed to {1} to ensure uniqueness", name, uniqueName), context);
                    return uniqueName;
                }
                uniqueName = string.Format("{0}_{1}", name, index);
                ++index;
            }
        }

        // ISpriteEditorDataProvider interface
        public SpriteImportMode spriteImportMode
        {
            get
            {
                return m_TextureImporterSettings.textureType != TextureImporterType.Sprite ?
                    SpriteImportMode.None :
                    (SpriteImportMode)m_TextureImporterSettings.spriteMode;
            }
        }

        public int spriteDataCount
        {
            get
            {
                var spriteImportData = GetSpriteImportData();
                if (mosaicMode)
                    return spriteImportData.Count;
                if (spriteImportMode != SpriteImportMode.Multiple)
                    return 1;
                return spriteImportData.Count - 1;
            }
        }

        public UnityEngine.Object targetObject
        {
            get { return this; }
        }

        public float pixelsPerUnit
        {
            get { return m_TextureImporterSettings.spritePixelsPerUnit; }
        }

        public T GetDataProvider<T>() where T : class
        {
            if (typeof(T) == typeof(ISpriteBoneDataProvider))
            {
                return new SpriteBoneDataProvider { dataProvider = this } as T;
            }
            if (typeof(T) == typeof(ISpriteMeshDataProvider))
            {
                return new SpriteMeshDataProvider { dataProvider = this } as T;
            }
            if (typeof(T) == typeof(ISpriteOutlineDataProvider))
            {
                return new SpriteOutlineDataProvider { dataProvider = this } as T;
            }
            if (typeof(T) == typeof(ISpritePhysicsOutlineDataProvider))
            {
                return new SpritePhysicsOutlineProvider { dataProvider = this } as T;
            }
            if (typeof(T) == typeof(ITextureDataProvider))
            {
                return new TextureDataProvider { dataProvider = this } as T;
            }
            if (typeof(T) == typeof(ICharacterDataProvider))
            {
                return characterMode ? new CharacterDataProvider { dataProvider = this } as T : null;
            }
            else
                return this as T;
        }

        public bool HasDataProvider(Type type)
        {
            if (characterMode && type == typeof(ICharacterDataProvider))
                return true;
            if (type == typeof(ISpriteBoneDataProvider) ||
                type == typeof(ISpriteMeshDataProvider) ||
                type == typeof(ISpriteOutlineDataProvider) ||
                type == typeof(ISpritePhysicsOutlineDataProvider) ||
                type == typeof(ITextureDataProvider))
            {
                return true;
            }
            else
                return type.IsAssignableFrom(GetType());
        }

        public void AddSpriteData(SpriteRect spriteRect)
        {
            if (spriteImportMode != SpriteImportMode.Multiple)
                Debug.LogWarning("Can only add sprite data when import mode is multiple");
            else
            {
                GetSpriteImportData().Add(new SpriteMetaData(spriteRect));
            }
        }

        public void DeleteSpriteData(SpriteRect spriteRect)
        {
            if (spriteImportMode != SpriteImportMode.Multiple)
                Debug.LogWarning("Can only add sprite data when import mode is multiple");
            else
            {
                var spriteImportData = GetSpriteImportData();
                int index = spriteImportData.FindIndex(x => x.spriteID == spriteRect.spriteID);
                Assert.AreEqual(0, index, "Cannot delete Sprite from single sprite mode");
                spriteImportData.RemoveAt(index);
            }
        }

        public int GetSpriteDataIndex(GUID guid)
        {
            switch (spriteImportMode)
            {
                case SpriteImportMode.Single:
                case SpriteImportMode.Polygon:
                    return 0;
                case SpriteImportMode.Multiple:
                {
                    var spriteImportData = GetSpriteImportData();
                    return spriteImportData.FindIndex(x => x.spriteID == guid);
                }
                default:
                    throw new InvalidOperationException("GUID not found");
            }
        }

        public void Apply()
        {
            // Do this so that asset change save dialog will not show
            var originalValue = EditorPrefs.GetBool("VerifySavingAssets", false);
            EditorPrefs.SetBool("VerifySavingAssets", false);
            AssetDatabase.ForceReserializeAssets(new string[] { assetPath }, ForceReserializeAssetsOptions.ReserializeMetadata);
            EditorPrefs.SetBool("VerifySavingAssets", originalValue);
        }

        public void InitSpriteEditorDataProvider() {}

        public SpriteRect[] GetSpriteRects()
        {
            var spriteImportData = GetSpriteImportData();
            var skip = mosaicMode ? 0 : 1;
            return spriteImportMode == SpriteImportMode.Multiple ? spriteImportData.Skip(skip).Select(x => new SpriteMetaData(x) as SpriteRect).ToArray() : new[] {new SpriteMetaData(spriteImportData[0]) };
        }

        List<SpriteMetaData> GetSpriteImportData()
        {
            return mosaicMode ? (characterMode ? m_RigSpriteImportData : m_MosaicSpriteImportData) : m_SpriteImportData;
        }

        internal List<PSDLayer> GetPSDLayers()
        {
            return mosaicMode ? (characterMode ? m_RigPSDLayers : m_MosaicPSDLayers) : null;
        }

        internal SpriteMetaData[] GetSpriteMetaData()
        {
            var spriteImportData = GetSpriteImportData();
            var skip = mosaicMode ? 0 : 1;
            return spriteImportMode == SpriteImportMode.Multiple ? spriteImportData.Skip(skip).ToArray() : new[] { new SpriteMetaData(spriteImportData[0]) };
        }

        public SpriteRect GetSpriteData(GUID guid)
        {
            var spriteImportData = GetSpriteImportData();
            var skip = mosaicMode ? 0 : 1;
            return spriteImportMode == SpriteImportMode.Multiple ? spriteImportData.Skip(skip).FirstOrDefault(x => x.spriteID == guid) : spriteImportData[0];
        }

        public void SetSpriteRects(SpriteRect[] spriteRects)
        {
            var spriteImportData = GetSpriteImportData();
            if (spriteImportMode == SpriteImportMode.Multiple)
            {
                var singleSpriteID = mosaicMode ? new GUID() : spriteImportData[0].spriteID;
                spriteImportData.RemoveAll(data => data.spriteID != singleSpriteID && spriteRects.FirstOrDefault(x => x.spriteID == data.spriteID) == null);
                foreach (var sr in spriteRects)
                {
                    var importData = spriteImportData.FirstOrDefault(x => x.spriteID == sr.spriteID);
                    if (importData == null)
                        spriteImportData.Add(new SpriteMetaData(sr));
                    else
                    {
                        importData.name = sr.name;
                        importData.alignment = sr.alignment;
                        importData.border = sr.border;
                        importData.pivot = sr.pivot;
                        importData.rect = sr.rect;
                    }
                }
            }
            else if (spriteRects.Length == 1 && (spriteImportMode == SpriteImportMode.Single || spriteImportMode == SpriteImportMode.Polygon))
            {
                if (spriteImportData[0].spriteID == spriteRects[0].spriteID)
                {
                    spriteImportData[0].name = spriteRects[0].name;
                    spriteImportData[0].alignment = spriteRects[0].alignment;
                    m_TextureImporterSettings.spriteAlignment = (int)spriteRects[0].alignment;
                    m_TextureImporterSettings.spriteBorder = spriteImportData[0].border = spriteRects[0].border;
                    m_TextureImporterSettings.spritePivot = spriteImportData[0].pivot = spriteRects[0].pivot;
                    spriteImportData[0].rect = spriteRects[0].rect;
                }
                else
                {
                    spriteImportData[0] = new SpriteMetaData(spriteRects[0]);
                }
            }
        }

        bool mosaicMode
        {
            get { return spriteImportMode == SpriteImportMode.Multiple && m_MosaicLayers; }
        }

        bool IAnimationAssetPostProcess.OnAfterPostProcess()
        {
            var gameObjects = AssetDatabase.LoadAllAssetsAtPath(this.assetPath).OfType<GameObject>().ToArray();
            foreach (var go in gameObjects)
            {
                PostProcessPrefab(go);
            }

            return gameObjects.Length > 0;
        }

        internal void PostProcessPrefab(GameObject go)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null && sr.sprite.GetBindPoses().Length > 0)
            {
                var spriteSkin = go.GetComponent<SpriteSkin>();
                if (spriteSkin != null)
                {
                    var spriteBones = m_CharacterData.parts.FirstOrDefault(x => new GUID(x.spriteId) == sr.sprite.GetSpriteID()).bones.Where(x => x >= 0 && x < m_BoneGOs.Length).Select(x => m_BoneGOs[x]);
                    if (spriteBones.Any())
                    {
                        spriteSkin.rootBone = spriteBones.OrderBy(x => x.index).First().go.transform;
                        spriteSkin.boneTransforms = spriteBones.Select(x => x.go.transform).ToArray();
                        if (spriteSkin.isValid)
                            spriteSkin.CalculateBounds();
                    }
                }
            }
        }

        internal CharacterData characterData
        {
            get { return m_CharacterData; }
            set { m_CharacterData = value; }
        }

        internal Vector2Int documentSize
        {
            get { return m_DocumentSize; }
        }
    }
}
