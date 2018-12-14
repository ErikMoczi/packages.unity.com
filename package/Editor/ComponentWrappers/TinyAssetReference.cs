using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

namespace Unity.Tiny.Runtime.EditorExtensions
{
    internal partial struct TinyAssetReference
    {
        public static TinyType.Reference GetType<TValue>()
        {
            return GetType(typeof(TValue));
        }

        public static TinyType.Reference GetType(Type assetType)
        {
            switch (assetType)
            {
                case var type when type == typeof(Texture2D):
                    return TypeRefs.EditorExtensions.AssetReferenceTexture2D;
                case var type when type == typeof(Sprite):
                    return TypeRefs.EditorExtensions.AssetReferenceSprite;
                case var type when type == typeof(SpriteAtlas):
                    return TypeRefs.EditorExtensions.AssetReferenceSpriteAtlas;
                case var type when type == typeof(Tile):
                    return TypeRefs.EditorExtensions.AssetReferenceTile;
                case var type when type == typeof(AudioClip):
                    return TypeRefs.EditorExtensions.AssetReferenceAudioClip;
                case var type when type == typeof(TMPro.TMP_FontAsset):
                    return TypeRefs.EditorExtensions.AssetReferenceTMP_FontAsset;
                case var type when type == typeof(AnimationClip):
                    return TypeRefs.EditorExtensions.AssetReferenceAnimationClip;
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool IsAssetReference(TinyType.Reference type)
        {
            return
                type.Id == TypeRefs.EditorExtensions.AssetReferenceTexture2D.Id ||
                type.Id == TypeRefs.EditorExtensions.AssetReferenceSprite.Id ||
                type.Id == TypeRefs.EditorExtensions.AssetReferenceSpriteAtlas.Id ||
                type.Id == TypeRefs.EditorExtensions.AssetReferenceTile.Id ||
                type.Id == TypeRefs.EditorExtensions.AssetReferenceAudioClip.Id ||
                type.Id == TypeRefs.EditorExtensions.AssetReferenceTMP_FontAsset.Id ||
                type.Id == TypeRefs.EditorExtensions.AssetReferenceAnimationClip.Id;
        }
    }
}
