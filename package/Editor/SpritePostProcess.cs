using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEditor.Experimental.U2D.Common;
using Unity.Collections;
using System.Linq;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class SpritePostProcess : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            List<string> assetPathModified = new List<string>();
            foreach (var importedAsset in importedAssets)
            {
                ISpriteEditorDataProvider ai = InternalEditorBridge.GetISpriteEditorDataProviderFromPath(importedAsset);
                if (ai != null)
                {
                    ai.InitSpriteEditorDataProvider();
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(importedAsset).OfType<Sprite>().ToArray<Sprite>();
                    bool dataChanged = false;
                    dataChanged = PostProcessBoneData(ai,  sprites);
                    dataChanged |= PostProcessSpriteMeshData(ai, sprites);
                    if (dataChanged)
                        assetPathModified.Add(importedAsset);
                }
            }

            if (assetPathModified.Count > 0)
            {
                var originalValue = EditorPrefs.GetBool("VerifySavingAssets", false);
                EditorPrefs.SetBool("VerifySavingAssets", false);
                AssetDatabase.ForceReserializeAssets(assetPathModified, ForceReserializeAssetsOptions.ReserializeMetadata);
                EditorPrefs.SetBool("VerifySavingAssets", originalValue);
            }
        }

        static bool PostProcessBoneData(ISpriteEditorDataProvider spriteDataProvider, Sprite[] sprites)
        {
            var boneDataProvider = spriteDataProvider.GetDataProvider<ISpriteBoneDataProvider>();
            var textureDataProvider = spriteDataProvider.GetDataProvider<ITextureDataProvider>();

            if (sprites == null || sprites.Length == 0 || boneDataProvider == null || textureDataProvider == null)
                return false;

            bool dataChanged = false;

            float definitionScale = CalculateDefinitionScale(textureDataProvider);
            
            foreach (var sprite in sprites)
            {
                var guid = sprite.GetSpriteID();
                {
                    SpriteRect spriteRect = spriteDataProvider.GetSpriteRects().First(s => { return s.spriteID == guid; });
                    var spriteBone = boneDataProvider.GetBones(guid);
                    if (spriteBone == null)
                        continue;

                    var spriteBoneCount = spriteBone.Count;
                    var pivotPointInPixels = sprite.pivot;
                    var bindPose = new NativeArray<Matrix4x4>(spriteBoneCount, Allocator.Temp);
                    var outputSpriteBones = new SpriteBone[spriteBoneCount];// NativeArray<SpriteBone>(spriteBone.Count, Allocator.Temp);
                    for (int i = 0; i < spriteBoneCount; ++i)
                    {
                        // Convert position to world unit.
                        SpriteBone sp = spriteBone[i];
                        var isRoot = sp.parentId == -1;
                        var position = isRoot ? (spriteBone[i].position - Vector3.Scale(spriteRect.rect.size, spriteRect.pivot)) : spriteBone[i].position;
                        position.z = 0f;
                        sp.position =  position * definitionScale / sprite.pixelsPerUnit;
                        sp.length = spriteBone[i].length * definitionScale / sprite.pixelsPerUnit;
                        outputSpriteBones[i] = sp;

                        // Calculate bind poses
                        var worldPosition = Vector3.zero;
                        var worldRotation = Quaternion.identity;

                        if (sp.parentId == -1)
                        {
                            worldPosition = sp.position;
                            worldRotation = sp.rotation;
                        }
                        else
                        {
                            var parentBindPose = bindPose[sp.parentId];
                            var invParentBindPose = Matrix4x4.Inverse(parentBindPose);

                            worldPosition = invParentBindPose.MultiplyPoint(sp.position);
                            worldRotation = sp.rotation * invParentBindPose.rotation;
                        }

                        // Practically Matrix4x4.SetTRInverse
                        var rot = Quaternion.Inverse(worldRotation);
                        Matrix4x4 mat = Matrix4x4.identity;
                        mat = Matrix4x4.Rotate(rot);
                        mat = mat * Matrix4x4.Translate(-worldPosition);


                        bindPose[i] = mat;
                    }
                    sprite.SetBindPoses(bindPose);
                    sprite.SetBones(outputSpriteBones);
                    bindPose.Dispose();

                    dataChanged = true;
                }
            }

            return dataChanged;
        }

        static bool PostProcessSpriteMeshData(ISpriteEditorDataProvider spriteEditorDataProvider, Sprite[] sprites)
        {
            var spriteMeshDataProvider = spriteEditorDataProvider.GetDataProvider<ISpriteMeshDataProvider>();
            var textureDataProvider = spriteEditorDataProvider.GetDataProvider<ITextureDataProvider>();
            if (sprites == null || sprites.Length == 0 || spriteMeshDataProvider == null || textureDataProvider == null)
                return false;

            bool dataChanged = false;
            float definitionScale = CalculateDefinitionScale(textureDataProvider);

            foreach (var sprite in sprites)
            {
                var guid = sprite.GetSpriteID();
                SpriteRect spriteRect = spriteEditorDataProvider.GetSpriteRects().First(s => { return s.spriteID == guid; });

                Vertex2DMetaData[] vertices = spriteMeshDataProvider.GetVertices(guid);
                int[] indices = spriteMeshDataProvider.GetIndices(guid);

                if (vertices.Length > 2 && indices.Length > 2)
                {
                    NativeArray<Vector3> vertexArray = new NativeArray<Vector3>(vertices.Length, Allocator.Temp);
                    NativeArray<BoneWeight> boneWeightArray = new NativeArray<BoneWeight>(vertices.Length, Allocator.Temp);

                    for (int i = 0; i < vertices.Length; ++i)
                    {
                        vertexArray[i] = (Vector3)(vertices[i].position - Vector2.Scale(spriteRect.rect.size, spriteRect.pivot)) * definitionScale / sprite.pixelsPerUnit;
                        boneWeightArray[i] = vertices[i].boneWeight;
                    }

                    NativeArray<ushort> indicesArray = new NativeArray<ushort>(indices.Length, Allocator.Temp);

                    for (int i = 0; i < indices.Length; ++i)
                        indicesArray[i] = (ushort)indices[i];

                    sprite.SetVertexCount(vertices.Length);
                    sprite.SetVertexAttribute<Vector3>(VertexAttribute.Position, vertexArray);
                    sprite.SetIndices(indicesArray);
                    sprite.SetBoneWeights(boneWeightArray);
                    vertexArray.Dispose();
                    boneWeightArray.Dispose();
                    indicesArray.Dispose();

                    dataChanged = true;
                }
            }

            return dataChanged;
        }

        static float CalculateDefinitionScale(ITextureDataProvider dataProvider)
        {
            float definitionScale = 1;
            var texture = dataProvider.texture;
            if (texture != null)
            {
                int actualWidth = 0, actualHeight = 0;
                dataProvider.GetTextureActualWidthAndHeight(out actualWidth, out actualHeight);
                float definitionScaleW = texture.width / (float)actualWidth;
                float definitionScaleH = texture.height / (float)actualHeight;
                definitionScale = Mathf.Min(definitionScaleW, definitionScaleH);
            }
            return definitionScale;
        }
    }
}
