using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Tiny.Runtime.Audio;
using Unity.Tiny.Runtime.Core2D;
using Unity.Tiny.Runtime.Tilemap2D;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Tilemaps;
using Unity.Tiny.Serialization;
using Unity.Tiny.Runtime.EditorExtensions;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    /// <summary>
    /// Contains information on how an asset was exported
    /// </summary>
    internal class TinyExportInfo
    {
        public readonly TinyAssetInfo AssetInfo;
        public readonly List<FileInfo> ExportedFiles = new List<FileInfo>();

        public TinyExportInfo(TinyAssetInfo assetInfo)
        {
            AssetInfo = assetInfo;
        }
    }

    /// <summary>
    /// Replacement for the Filtering API
    /// </summary>
    internal static class AssetIterator
    {
        private static readonly Dictionary<Object, TinyAssetInfo> s_Assets = new Dictionary<Object, TinyAssetInfo>();
        private static readonly Dictionary<Texture2D, float> s_TexturePixelsPerUnit = new Dictionary<Texture2D, float>();

        public static TinyAssetInfo GetAssetInfo(Object @object)
        {
            TinyAssetInfo assetInfo = null;
            s_Assets.TryGetValue(@object, out assetInfo);
            return assetInfo;
        }

        public static float GetTexturePixelsPerUnit(Texture2D texture)
        {
            if (!s_TexturePixelsPerUnit.TryGetValue(texture, out var pixelsPerUnit))
            {
                pixelsPerUnit = 1;
            }
            return pixelsPerUnit;
        }

        public static IEnumerable<TinyAssetInfo> EnumerateAllAssets(TinyModule module, bool export)
        {
            s_Assets.Clear();
            s_TexturePixelsPerUnit.Clear();

            foreach (var m in module.EnumerateDependencies())
            {
                // Enumerate explicit references
                foreach (var asset in m.Assets)
                {
                    if (!asset.Object || null == asset.Object)
                    {
                        continue;
                    }

                    var assetInfo = GetOrAddAssetInfo(asset.Object, asset.Name);
                    if (assetInfo != null)
                    {
                        assetInfo.AddExplicitReference((TinyModule.Reference)module);
                    }
                }
            }

            // Enumerate implicit assets references
            foreach (var entity in module.EnumerateDependencies().Entities())
            {
                foreach (var component in entity.Components)
                {
                    foreach (var @object in EnumerateUnityEngineObjects(component))
                    {
                        if (!@object || null == @object || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(@object)))
                        {
                            continue;
                        }

                        var assetInfo = GetOrAddAssetInfo(@object, @object.name);
                        if (assetInfo != null)
                        {
                            assetInfo.AddImplicitReference((TinyEntity.Reference)entity);
                        }
                    }
                }
            }

            // Generate export assets
            if (export)
            {
                // TEMPORARY PATCH: Unity mono stacktrace will crash when a sprite is found in more than one
                // SpriteAtlas. Abort export if this is the case to prevent a crash when packing sprite atlases until
                // this is fixed in Unity.
                {
                    var spriteCache = new Dictionary<Sprite, SpriteAtlas>();
                    var spriteAtlases = AssetDatabase.FindAssets("t:SpriteAtlas")
                        .Select(a => AssetDatabase.GUIDToAssetPath(a))
                        .Select(a => AssetDatabase.LoadAssetAtPath<SpriteAtlas>(a));
                    foreach (var spriteAtlas in spriteAtlases)
                    {
                        var packedSprites = TinyEditorBridge.GetAtlasPackedSprites(spriteAtlas);
                        foreach (var packedSprite in packedSprites)
                        {
                            if (spriteCache.TryGetValue(packedSprite, out var packingSpriteAtlas))
                            {
                                throw new InvalidOperationException(
                                    $"Sprite {packedSprite.name} found in more than one SpriteAtlas:\n" +
                                    $"Found in: {AssetDatabase.GetAssetPath(packingSpriteAtlas)}\n" +
                                    $"Found in: {AssetDatabase.GetAssetPath(spriteAtlas)}\n" +
                                    $"This is not supported yet, aborting export.");
                            }
                            else
                            {
                                spriteCache.Add(packedSprite, spriteAtlas);
                            }
                        }
                    }
                }

                // Make sure all sprite atlases are packed before proceeding
                TinyEditorBridge.PackAllSpriteAtlases();

                // BUG: packing sprite atlases clears the progress bar
                TinyEditorUtility.ProgressBarScope.Restore();

                // Gather all sprite atlas referenced by sprites
                var sprites = s_Assets.Keys.OfType<Sprite>().Distinct().ToList();
                var usedSpriteTextures = new HashSet<Texture2D>();
                var usedSpriteAtlases = new HashSet<SpriteAtlas>();
                foreach (var sprite in sprites)
                {
                    var texture = sprite.texture;
                    var spriteAtlas = TinyEditorBridge.GetSpriteActiveAtlas(sprite);
                    var spriteAtlasTexture = TinyEditorBridge.GetSpriteActiveAtlasTexture(sprite);
                    if (spriteAtlas != null && spriteAtlasTexture != null)
                    {
                        texture = spriteAtlasTexture;
                        usedSpriteAtlases.Add(spriteAtlas);
                    }
                    if (texture != null)
                    {
                        usedSpriteTextures.Add(texture);
                        if (!s_TexturePixelsPerUnit.Keys.Contains(texture))
                        {
                            s_TexturePixelsPerUnit.Add(texture, sprite.pixelsPerUnit);
                        }
                    }
                }

                // Generate sprite atlas assets
                foreach (var spriteAtlas in usedSpriteAtlases)
                {
                    GetOrAddAssetInfo(spriteAtlas, spriteAtlas.name);
                    var atlasSprites = TinyEditorBridge.GetAtlasPackedSprites(spriteAtlas);
                    foreach (var atlasSprite in atlasSprites)
                    {
                        GetOrAddAssetInfo(atlasSprite, atlasSprite.name);
                    }
                }

                // Generate sprite texture assets
                foreach (var spriteTexture in usedSpriteTextures)
                {
                    GetOrAddAssetInfo(spriteTexture, spriteTexture.name);
                }

                // Remove sprite textures that are no longer referenced by sprites
                var spriteTextures = sprites.Select(s => s.texture).NotNull().Distinct().ToList();
                foreach (var spriteTexture in spriteTextures)
                {
                    if (!usedSpriteTextures.Contains(spriteTexture))
                    {
                        s_Assets.Remove(spriteTexture);
                    }
                }
            }

            return s_Assets.Values;
        }

        public static IEnumerable<TinyAssetInfo> EnumerateRootAssets(TinyModule module, bool export = false)
        {
            return EnumerateAllAssets(module, export).Where(asset => asset.Parent == null);
        }

        /// <summary>
        /// @TODO Move to filtering API
        /// </summary>
        private static IEnumerable<Object> EnumerateUnityEngineObjects(TinyObject @object)
        {
            return @object.EnumerateProperties().SelectMany(property => Filter(property.Value));
        }

        private static IEnumerable<Object> Filter(object @object)
        {
            if (@object is Object)
            {
                yield return (Object)@object;
            }
            else if (@object is TinyObject)
            {
                foreach (var v in EnumerateUnityEngineObjects((TinyObject)@object))
                {
                    yield return v;
                }
            }
            else if (@object is TinyList)
            {
                foreach (var item in (TinyList)@object)
                {
                    foreach (var o in Filter(item))
                    {
                        yield return o;
                    }
                }
            }
        }

        private static TinyAssetInfo GetOrAddAssetInfo(Object @object, string name)
        {
            // First check if asset info exist
            TinyAssetInfo assetInfo = null;
            if (s_Assets.TryGetValue(@object, out assetInfo))
            {
                return assetInfo;
            }

            // Create new asset info
            assetInfo = new TinyAssetInfo(@object, name);

            // If its a tile, add its sprite
            if (@object is Tile)
            {
                var tile = (Tile)@object;
                var sprite = tile.sprite;
                GetOrAddAssetInfo(sprite, sprite.name);
            }

            // Set asset info parent
            if (AssetDatabase.IsSubAsset(@object))
            {
                var path = AssetDatabase.GetAssetPath(@object);
                var parentObject = AssetDatabase.LoadMainAssetAtPath(path);
                var parentAssetInfo = GetOrAddAssetInfo(parentObject, parentObject.name);
                if (parentAssetInfo != null)
                {
                    assetInfo.Parent = parentAssetInfo;
                }
            }

            s_Assets.Add(@object, assetInfo);
            return assetInfo;
        }
    }

    internal static class TinyAssetEntityGroupGenerator
    {
        public static Dictionary<Object, TinyEntity> ObjectToEntityMap = new Dictionary<Object, TinyEntity>();

        public static string GetAssetEntityPath(Object obj)
        {
            if (obj is Texture2D)
            {
                return "assets/textures/";
            }

            if (obj is Sprite)
            {
                var atlas = TinyEditorBridge.GetSpriteActiveAtlas((Sprite) obj);
                if (atlas != null)
                {
                    return $"assets/sprites/{atlas.name}/";
                }

                return "assets/sprites/";
            }

            if (obj is SpriteAtlas)
            {
                return "assets/atlases/";
            }

            if (obj is Tile)
            {
                return "assets/tiles/";
            }

            if (obj is TMPro.TMP_FontAsset)
            {
                return "assets/fonts/";
            }

            if (obj is AudioClip)
            {
                return "assets/audioclips/";
            }

            if (obj is AnimationClip)
            {
                return "assets/animationclips/";
            }

            return string.Empty;
        }

        public static void Generate(IRegistry registry, TinyProject project, TinyEntityGroup entityGroup)
        {
            ObjectToEntityMap.Clear();

            // Enumerate all assets for export
            var assets = AssetIterator.EnumerateAllAssets(project.Module.Dereference(project.Registry), export: true);

            // Generate entities
            foreach (var asset in assets)
            {
                GetOrCreateEntityForAsset(registry, project, entityGroup, asset);
            }
        }

        private static TinyEntity GetOrCreateEntityForAsset(IRegistry registry, TinyProject project,
            TinyEntityGroup entityGroup, TinyAssetInfo assetInfo)
        {
            var @object = assetInfo.Object;

            TinyEntity entity = null;
            if (ObjectToEntityMap.TryGetValue(@object, out entity))
            {
                return entity;
            }

            var entityPathName = $"{GetAssetEntityPath(@object)}{assetInfo.Name}";

            var baseInfo = new EntityExporterInfo
            {
                AssetInfo = assetInfo,
                Group = entityGroup,
                PathName = entityPathName,
                Project = project,
                Registry = registry,
            };

            if (@object is Texture2D t)
            {
                ExportEntity(baseInfo, t);
            }
            else if (@object is Sprite s)
            {
                ExportEntity(baseInfo, s);
            }
            else if (@object is SpriteAtlas atlas)
            {
                ExportEntity(baseInfo, atlas);
            }
            else if (@object is Tile tile)
            {
                ExportEntity(baseInfo, tile);
            }
            else if (@object is AudioClip audio)
            {
                ExportEntity(baseInfo, audio);
            }
            else if (@object is AnimationClip animationClip)
            {
                ExportEntity(baseInfo, animationClip);
            }
            else if (@object is TMPro.TMP_FontAsset font)
            {
                ExportEntity(baseInfo, font);
            }

            foreach (var child in assetInfo.Children)
            {
                GetOrCreateEntityForAsset(registry, project, entityGroup, child);
            }

            return entity;
        }

        private struct EntityExporterInfo
        {
            public string PathName;
            public IRegistry Registry;
            public TinyAssetInfo AssetInfo;
            public TinyProject Project;
            public TinyEntityGroup Group;

            public TinyEntity CreateEntity()
            {
                var entity = Registry.CreateEntity(TinyId.New(), PathName);
                Group.AddEntityReference(entity.Ref);
                return entity;

            }
        }

        private static TinyEntity ExportEntity(EntityExporterInfo info, Texture2D texture)
        {
            var entity = info.CreateEntity();
            var image2DFile = entity.AddComponent<Runtime.Core2D.TinyImage2DLoadFromFile>();
            image2DFile.imageFile = $"ut-asset:{info.AssetInfo.Name}";

            var settings = TinyUtility.GetAssetExportSettings(info.Project, texture) as TinyTextureSettings;
            if (settings != null && settings.FormatType == TextureFormatType.JPG &&
                TinyAssetExporter.TextureExporter.ReallyHasAlpha(texture))
            {
                image2DFile.maskFile = $"ut-asset:{info.AssetInfo.Name}_a";
            }

            var image2D = entity.AddComponent<Runtime.Core2D.TinyImage2D>();
            image2D.disableSmoothing = texture.filterMode == FilterMode.Point;
            image2D.pixelsToWorldUnits = 1.0f / AssetIterator.GetTexturePixelsPerUnit(texture);

            AddAssetReferenceComponent(entity, texture);
            ObjectToEntityMap.Add(texture, entity);
            return entity;
        }

        private static TinyEntity ExportEntity(EntityExporterInfo info, Sprite sprite)
        {
            var entity = info.CreateEntity();
            var sprite2d = entity.AddComponent(TypeRefs.Core2D.Sprite2D);

            if (TinyEditorBridge.GetSpriteActiveAtlasTexture(sprite) == null)
            {
                // Standalone sprite (not part of an atlas)

                var rect = sprite.rect;
                var texture = sprite.texture;

                var region = new Rect(
                    rect.x / texture.width,
                    rect.y / texture.height,
                    rect.width / texture.width,
                    rect.height / texture.height);

                var pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);

                sprite2d.AssignIfDifferent("image", texture);
                sprite2d.AssignIfDifferent("imageRegion", region);
                sprite2d.AssignIfDifferent("pivot", pivot);
            }
            else
            {
                // This sprite is part of an atlas

                var atlasTexture = TinyEditorBridge.GetSpriteActiveAtlasTexture(sprite);
                var atlasRect = TinyEditorBridge.GetSpriteActiveAtlasTextureRect(sprite);

                var region = new Rect(
                    atlasRect.x / atlasTexture.width,
                    atlasRect.y / atlasTexture.height,
                    atlasRect.width / atlasTexture.width,
                    atlasRect.height / atlasTexture.height);

                // The original pivot is based on the full rect
                // Here we are computing the new pivot based on the `textureRectOffset` field to account for `tight` packing
                var pivot = new Vector2((sprite.pivot.x - sprite.textureRectOffset.x) / atlasRect.width,
                    (sprite.pivot.y - sprite.textureRectOffset.y) / atlasRect.height);

                sprite2d.AssignIfDifferent("image", atlasTexture);
                sprite2d.AssignIfDifferent("imageRegion", region);
                sprite2d.AssignIfDifferent("pivot", pivot);
            }

            if (sprite.border != Vector4.zero)
            {
                var border = entity.AddComponent(TypeRefs.Core2D.Sprite2DBorder);
                border.AssignIfDifferent("bottomLeft",
                    new Vector2(sprite.border.x / sprite.rect.width, sprite.border.y / sprite.rect.height));
                border.AssignIfDifferent("topRight",
                    new Vector2((sprite.rect.width - sprite.border.z) / sprite.rect.width,
                        (sprite.rect.height - sprite.border.w) / sprite.rect.height));
            }

            AddAssetReferenceComponent(entity, sprite);
            ObjectToEntityMap.Add(sprite, entity);
            return entity;
        }

        private static TinyEntity ExportEntity(EntityExporterInfo info, SpriteAtlas spriteAtlas)
        {
            var entity = info.CreateEntity();

            var settings = spriteAtlas.GetPackingSettings();

            if (settings.enableRotation)
            {
                Debug.LogError($"{TinyConstants.ApplicationName}: SpriteAtlas.enableRotation is not supported!");
            }

            if (settings.enableTightPacking)
            {
                Debug.LogError(
                    $"{TinyConstants.ApplicationName}: SpriteAtlas.enableTightPacking is not supported!");
            }

            var atlas = entity.AddComponent(TypeRefs.Core2D.SpriteAtlas);
            atlas["sprites"] = new TinyList(info.Registry, (TinyType.Reference)TinyType.SpriteEntity);
            var spriteList = atlas["sprites"] as TinyList;

            var sprites = TinyEditorBridge.GetAtlasPackedSprites(spriteAtlas);
            foreach (var sprite in sprites)
            {
                var spriteAssetInfo = AssetIterator.GetAssetInfo(sprite);
                if (spriteAssetInfo != null)
                {
                    var spriteEntity = GetOrCreateEntityForAsset(info.Registry, info.Project, info.Group, spriteAssetInfo);
                    spriteList.Add((TinyEntity.Reference)spriteEntity);
                }
            }

			AddAssetReferenceComponent(entity, spriteAtlas);
            ObjectToEntityMap.Add(spriteAtlas, entity);
            return entity;
        }

        private static TinyEntity ExportEntity(EntityExporterInfo info, Tile tile)
        {
            var entity = info.CreateEntity();

            var tinyTile = entity.AddComponent<TinyTile>();
            tinyTile.color = tile.color;
            tinyTile.sprite = tile.sprite;
            tinyTile.colliderType = tile.colliderType;

            AddAssetReferenceComponent(entity, tile);
            ObjectToEntityMap.Add(tile, entity);
            return entity;
        }

        private static TinyEntity ExportEntity(EntityExporterInfo info, AudioClip audioClip)
        {
            var entity = info.CreateEntity();

            var audioFile = entity.AddComponent<TinyAudioClipLoadFromFile>();
            audioFile.fileName = $"ut-asset:{info.AssetInfo.Name}";

            entity.AddComponent<TinyAudioClip>();

            AddAssetReferenceComponent(entity, audioClip);
            ObjectToEntityMap.Add(audioClip, entity);
            return entity;
        }

        private static TinyEntity ExportEntity(EntityExporterInfo info, AnimationClip animationClip)
        {
            var entity = info.CreateEntity();

            TinyAnimationExportUtilities.PopulateAnimationClipEntity(info.Group, ObjectToEntityMap, entity,
                animationClip);

            AddAssetReferenceComponent(entity, animationClip);
            ObjectToEntityMap.Add(animationClip, entity);
            return entity;
        }

        private static void AddAssetReferenceComponent<TValue>(TinyEntity entity, TValue @object)
            where TValue : Object
        {
            var objHandle = UnityObjectSerializer.ToObjectHandle(@object);
            if (string.IsNullOrEmpty(objHandle.Guid))
            {
                return;
            }

            var assetReferenceType = TinyAssetReference.GetType<TValue>();
            var assetReference = entity.AddComponent(assetReferenceType);
            assetReference.AssignIfDifferent("guid", objHandle.Guid);
            assetReference.AssignIfDifferent("fileId", objHandle.FileId);
            assetReference.AssignIfDifferent("type", objHandle.Type);
        }

        private static TinyEntity ExportEntity(EntityExporterInfo info, TMPro.TMP_FontAsset font)
        {
            var textureInfo = info;
            textureInfo.PathName = textureInfo.PathName.Replace("assets/fonts", "assets/font-textures");
            var texture = font.material.mainTexture as Texture2D;
            var textureEntity = ExportEntity(textureInfo, texture);

            var entity = info.CreateEntity();
            var bitmapFont = entity.AddComponent<Runtime.Text.TinyBitmapFont>();
            bitmapFont.ascent = font.fontInfo.Ascender;
            bitmapFont.descent = font.fontInfo.Descender;
            bitmapFont.size = font.creationSettings.pointSize;
            bitmapFont.textureAtlas = textureEntity.Ref;
            var data = bitmapFont.data;
            foreach (var kvp in font.characterDictionary)
            {
                var ci = kvp.Value;
                var tinyCI = new Runtime.Text.TinyCharacterInfo(entity.Registry);
                tinyCI.value = (uint) ci.id;
                
                tinyCI.advance = ci.xAdvance;
                
                tinyCI.bearingX = ci.xOffset;
                tinyCI.bearingY = ci.yOffset;
                tinyCI.width = ci.width;
                tinyCI.height = ci.height;

                
                var min = new Vector2(ci.x / font.atlas.width, (font.atlas.height - ci.y - ci.height) / font.atlas.height);
                var off = new Vector2(ci.width/ font.atlas.width, ci.height/ font.atlas.height);
                tinyCI.characterRegion = new Rect(min.x, min.y, off.x, off.y);
                data.Add(tinyCI.Tiny);
            }

            ObjectToEntityMap.Add(font, entity);
            AddAssetReferenceComponent(entity, font);
            return entity;
        }
    }
    
    internal static class TinyAssetExporter
    {
        public static string GetAssetName(TinyProject project, Object @object)
        {
            return GetAssetName(project.Module.Dereference(project.Registry), @object);
        }

        public static string GetAssetName(TinyModule module, Object @object)
        {
            if (!@object)
            {
                return string.Empty;
            }

            var asset = module.EnumerateDependencies().Select(m => m.GetAsset(@object)).FirstOrDefault();

            if (!string.IsNullOrEmpty(asset?.Name))
            {
                return asset.Name;
            }

            return @object.name;
        }

        public static IList<TinyExportInfo> Export(TinyBuildOptions options, DirectoryInfo assetsFolder)
        {
            var module = options.Project.Module.Dereference(options.Registry);
            return AssetIterator.EnumerateRootAssets(module, export: true).Select(asset => Export(options, assetsFolder.FullName, asset)).NotNull().ToList();
        }

        private static TinyExportInfo Export(TinyBuildOptions options, string path, TinyAssetInfo assetInfo)
        {
            // Skip asset types that should not be exported
            if (assetInfo.Object is SpriteAtlas ||
                assetInfo.Object is Tilemap ||
                assetInfo.Object is Tile ||
				TinyScriptUtility.EndsWithTinyScriptExtension(assetInfo.AssetPath))
            {
                return null;
            }

            var project = options.Project;
            var export = new TinyExportInfo(assetInfo);
            var assetName = GetAssetName(project, assetInfo.Object);
            var isRelease = options.Configuration == TinyBuildConfiguration.Release;

            var texture = assetInfo.Object as Texture2D;
            if (texture != null)
            {
                var settings = TinyUtility.GetAssetExportSettings(project, texture) as TinyTextureSettings;
                TextureExporter.Export(path, assetName, texture, isRelease, settings, export.ExportedFiles);
                return export;
            }

            var audioClip = assetInfo.Object as AudioClip;
            if (audioClip != null)
            {
                FileExporter.Export(path, assetName, audioClip, export.ExportedFiles);
                return export;
            }

            var font = assetInfo.Object as TMPro.TMP_FontAsset;
            if (font != null)
            {
                FontExporter.Export(path, assetName, font, export.ExportedFiles);
                return export;
            }

            // Export the object as is
            FileExporter.Export(path, assetName, assetInfo.Object, export.ExportedFiles);
            return export;
        }

        public static class FileExporter
        {
            public static void Export(string path, string name, Object @object, ICollection<FileInfo> output)
            {
                var assetPath = AssetDatabase.GetAssetPath(@object);
                if (string.IsNullOrEmpty(assetPath))
                {
                    throw new ArgumentException($"Asset path is empty for object {@object.name}", "assetPath");
                }

                var srcFile = new FileInfo(Path.Combine(Path.Combine(Application.dataPath, ".."), assetPath));
                if (!File.Exists(srcFile.FullName))
                {
                    throw new FileNotFoundException(srcFile.FullName);
                }

                var dstFile = new FileInfo(Path.Combine(path, name + Path.GetExtension(srcFile.Name)));
                srcFile.CopyTo(dstFile.FullName, true);
                output.Add(dstFile);
            }
        }

        public static class TextureExporter
        {
            public static void Export(string path, string name, Texture2D texture, bool forRelease, TinyTextureSettings settings, List<FileInfo> output)
            {
                // If the texture doesn't exist in asset database, we can't use "Source" format type. Default to PNG in that case.
                var format = settings.FormatType;
                if (format == TextureFormatType.Source && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture)))
                {
                    format = TextureFormatType.PNG;
                }

                switch (format)
                {
                    case TextureFormatType.Source:
                        // Use the basic file exporter
                        FileExporter.Export(path, name, texture, output);
                        break;
                    case TextureFormatType.PNG:
                        ExportPng(path, name, texture, output);
                        break;
                    case TextureFormatType.JPG:
                        if (forRelease)
                            ExportJpgOptimized(path, name, texture, settings.JpgCompressionQuality, output);
                        else
                            ExportJpg(path, name, texture, settings.JpgCompressionQuality, output);
                        break;
                    case TextureFormatType.WebP:
                        ExportWebP(path, name, texture, settings.WebPCompressionQuality, output);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private static void ExportPng(string path, string name, Texture2D texture, ICollection<FileInfo> output)
            {
                var hasAlpha = ReallyHasAlpha(texture);
                var outputTexture = CopyTexture(texture, hasAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24);
                var bytes = outputTexture.EncodeToPNG();
                var dstFile = $"{Path.Combine(path, name)}.png";

                using (var stream = new FileStream(dstFile, FileMode.Create))
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                output.Add(new FileInfo(dstFile));
            }
            
            internal static void ExportFontAtlas(string path, string name, Texture2D texture, ICollection<FileInfo> output)
            {
                var outputTexture = CopyAlphaTexture(texture, TextureFormat.RGBA32);
                var bytes = outputTexture.EncodeToPNG();
                var dstFile = $"{Path.Combine(path, name)}.png";

                using (var stream = new FileStream(dstFile, FileMode.Create))
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                output.Add(new FileInfo(dstFile));
            }            

            private static void ExportJpg(string path, string name, Texture2D texture, int quality, ICollection<FileInfo> output)
            {
                var outputColorTexture = CopyTexture(texture, TextureFormat.RGB24);
                var colorFilePath = $"{Path.Combine(path, name)}.jpg";

                using (var stream = new FileStream(colorFilePath, FileMode.Create))
                {
                    var bytes = outputColorTexture.EncodeToJPG(quality);
                    stream.Write(bytes, 0, bytes.Length);
                }

                output.Add(new FileInfo(colorFilePath));

                if (ReallyHasAlpha(texture))
                {
                    // @TODO Optimization by reusing the texture above
                    var outputAlphaTexture = CopyTexture(texture, TextureFormat.RGBA32);

                    var pixels = outputAlphaTexture.GetPixels32();

                    // broadcast alpha to color
                    for (var i = 0; i < pixels.Length; i++)
                    {
                        pixels[i].r = pixels[i].a;
                        pixels[i].g = pixels[i].a;
                        pixels[i].b = pixels[i].a;
                        pixels[i].a = 255;
                    }


                    outputAlphaTexture.SetPixels32(pixels);
                    outputAlphaTexture.Apply();
                    // kill alpha channel
                    outputAlphaTexture = CopyTexture(outputAlphaTexture, TextureFormat.RGB24);


                    var alphaFilePath = $"{Path.Combine(path, name)}_a.png";

                    using (var stream = new FileStream(alphaFilePath, FileMode.Create))
                    {
                        var bytes = outputAlphaTexture.EncodeToPNG();
                        stream.Write(bytes, 0, bytes.Length);
                    }

                    output.Add(new FileInfo(alphaFilePath));
                }
            }

            private static string ImageUtilsPath()
            {
                var path = TinyRuntimeInstaller.GetToolDirectory("images");
#if UNITY_EDITOR_WIN
                path = Path.Combine(path, "win");
#else
		        path = Path.Combine(path, "osx");
#endif
                return new DirectoryInfo(path).FullName;
            }

            private static void ExportJpgOptimized(string path, string name, Texture2D texture, int quality, ICollection<FileInfo> output)
            {
                var colorFilePath = $"{Path.Combine(path, name)}.jpg";

                var outputTexture = CopyTexture(texture, TextureFormat.RGB24);
                //var tempPngInputPath = Path.Combine(Application.temporaryCachePath, "webp-input.png");
                var tempPngInputPath = $"{Path.Combine(path, name)}-input.png";
                File.WriteAllBytes(tempPngInputPath, outputTexture.EncodeToPNG());

                quality = Math.Max(0, Math.Min(100, quality));

                // this will build progressive jpegs by default; -baseline stops this.
                // progressive results in better compression
                TinyBuildUtilities.RunInShell(
                    $"moz-cjpeg -quality {quality} -outfile \"{colorFilePath}\" \"{tempPngInputPath}\"",
                    new ShellProcessArgs()
                    {
                        ExtraPaths = ImageUtilsPath().AsEnumerable()
                    });

                File.Delete(tempPngInputPath);

                output.Add(new FileInfo(colorFilePath));

                if (ReallyHasAlpha(texture))
                {
                    // @TODO Optimization by reusing the texture above
                    var outputAlphaTexture = CopyTexture(texture, TextureFormat.RGBA32);

                    var pixels = outputAlphaTexture.GetPixels32();

                    // broadcast alpha to color
                    for (var i = 0; i < pixels.Length; i++)
                    {
                        pixels[i].r = pixels[i].a;
                        pixels[i].g = pixels[i].a;
                        pixels[i].b = pixels[i].a;
                        pixels[i].a = 255;
                    }

                    outputAlphaTexture.SetPixels32(pixels);
                    outputAlphaTexture.Apply();

                    //var pngCrushInputPath = Path.Combine(Application.temporaryCachePath, "alpha-input.png");
                    var pngCrushInputPath = $"{Path.Combine(path, name)}_a-input.png";
                    var alphaFilePath = $"{Path.Combine(path, name)}_a.png";

                    using (var stream = new FileStream(pngCrushInputPath, FileMode.Create))
                    {
                        var bytes = outputAlphaTexture.EncodeToPNG();
                        stream.Write(bytes, 0, bytes.Length);
                    }

                    // convert to 8-bit grayscale png
                    TinyBuildUtilities.RunInShell(
                        $"pngcrush -s -c 0 \"{pngCrushInputPath}\" \"{alphaFilePath}\"",
                        new ShellProcessArgs()
                        {
                            ExtraPaths = ImageUtilsPath().AsEnumerable()
                        });

                    output.Add(new FileInfo(alphaFilePath));
                }
            }


            internal static void ExportWebP(string path, string name, Texture2D texture, int quality, ICollection<FileInfo> output)
            {
                var hasAlpha = ReallyHasAlpha(texture);
                var outputTexture = CopyTexture(texture, hasAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24);

                var tempPngInputPath = Path.Combine(Application.temporaryCachePath, "webp-input.png");
                File.WriteAllBytes(tempPngInputPath, outputTexture.EncodeToPNG());

                quality = Math.Max(0, Math.Min(100, quality));

                var dstFile = new FileInfo(Path.Combine(path, name + ".webp"))
                    .FullName;

                TinyBuildUtilities.RunInShell(
                    $"cwebp -quiet -q {quality} \"{tempPngInputPath}\" -o \"{dstFile}\"",
                    new ShellProcessArgs()
                    {
                        ExtraPaths = ImageUtilsPath().AsEnumerable()
                    });

                File.Delete(tempPngInputPath);

                output.Add(new FileInfo(dstFile));
            }

            public static bool ReallyHasAlpha(Texture2D texture)
            {
                bool hasAlpha = HasAlpha(texture.format);
                if (!hasAlpha)
                    return false;

                if (texture.format == TextureFormat.ARGB4444 ||
                    texture.format == TextureFormat.ARGB32 ||
                    texture.format == TextureFormat.RGBA32)
                {
                    var copy = CopyTexture(texture, TextureFormat.ARGB32);
                    Color32[] pix = copy.GetPixels32();
                    for (int i = 0; i < pix.Length; ++i)
                    {
                        if (pix[i].a != 255)
                        {
                            return true;
                        }
                    }

                    // image has alpha channel, but every alpha value is opaque
                    return false;
                }

                return true;
            }

            public static bool HasAlpha(TextureFormat format)
            {
                return format == TextureFormat.Alpha8 ||
                       format == TextureFormat.ARGB4444 ||
                       format == TextureFormat.ARGB32 ||
                       format == TextureFormat.RGBA32 ||
                       format == TextureFormat.DXT5 ||
                       format == TextureFormat.PVRTC_RGBA2 ||
                       format == TextureFormat.PVRTC_RGBA4 ||
                       format == TextureFormat.ETC2_RGBA8;
            }

            private static Texture2D CopyTexture(Texture texture, TextureFormat format)
            {
                // Create a temporary RenderTexture of the same size as the texture
                var tmp = RenderTexture.GetTemporary(
                    texture.width,
                    texture.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.sRGB);

                // Blit the pixels on texture to the RenderTexture
                Graphics.Blit(texture, tmp);

                // Backup the currently set RenderTexture
                var previous = RenderTexture.active;

                // Set the current RenderTexture to the temporary one we created
                RenderTexture.active = tmp;

                // Create a new readable Texture2D to copy the pixels to it
                var result = new Texture2D(texture.width, texture.height, format, false);
                
                // Copy the pixels from the RenderTexture to the new Texture
                result.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                result.Apply();

                // Reset the active RenderTexture
                RenderTexture.active = previous;

                // Release the temporary RenderTexture
                RenderTexture.ReleaseTemporary(tmp);

                return result;
            }
            
            private static Texture2D CopyAlphaTexture(Texture2D texture, TextureFormat format)
            {
                // Create a temporary RenderTexture of the same size as the texture
                var tmp = RenderTexture.GetTemporary(
                    texture.width,
                    texture.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.sRGB);

                // Blit the pixels on texture to the RenderTexture
                Graphics.Blit(texture, tmp);

                // Backup the currently set RenderTexture
                var previous = RenderTexture.active;

                // Set the current RenderTexture to the temporary one we created
                RenderTexture.active = tmp;

                // Create a new readable Texture2D to copy the pixels to it
                var result = new Texture2D(texture.width, texture.height, format, false);
                
                // Copy the pixels from the RenderTexture to the new Texture
                result.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                result.Apply();
                var pixels = result.GetPixels();
                for (var i = 0; i < pixels.Length; ++i)
                {
                    var color = pixels[i];
                    color.r = color.a;
                    color.g = color.a;
                    color.b = color.a;
                    pixels[i] = color;
                }
                result.SetPixels(pixels);
                result.Apply();

                // Reset the active RenderTexture
                RenderTexture.active = previous;

                // Release the temporary RenderTexture
                RenderTexture.ReleaseTemporary(tmp);

                return result;
            }
        }

        private static class FontExporter
        {
            /// <summary>
            /// @HACK This should be platform specific
            /// </summary>
            private static readonly List<string> s_WebFonts = new List<string>
            {
                "Arial",
            };

            private static bool IncludedByTargetPlatform(TMPro.TMP_FontAsset font)
            {
                return s_WebFonts.Intersect(font.name.AsEnumerable()).Any();
            }

            public static void Export(string path, string name, TMPro.TMP_FontAsset font, ICollection<FileInfo> output)
            {
                TextureExporter.ExportFontAtlas(path, name, font.atlas, output);
                
                
                
                
                if (IncludedByTargetPlatform(font))
                {
                    return;
                }

                //FileExporter.Export(path, name, font, output);
            }
        }
    }
}

