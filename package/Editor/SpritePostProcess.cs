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
                    var texture = AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(importedAsset);
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(importedAsset).OfType<Sprite>().ToArray<Sprite>();
                    bool dataChanged = false;
                    dataChanged = PostProcessBoneData(ai, texture, sprites);
                    dataChanged |= PostProcessSpriteMeshData(ai, texture, sprites);
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

        static bool PostProcessBoneData(ISpriteEditorDataProvider spriteDataProvider, UnityEngine.Texture2D texture, Sprite[] sprites)
        {
            var boneDataProvider = spriteDataProvider.GetDataProvider<ISpriteBoneDataProvider>();
            var textureDataProvider = spriteDataProvider.GetDataProvider<ITextureDataProvider>();

            if (texture == null || sprites == null || sprites.Length == 0 || boneDataProvider == null || textureDataProvider == null)
                return false;

            bool dataChanged = false;

            int actualWidth = 0, actualHeight = 0;
            textureDataProvider.GetTextureActualWidthAndHeight(out actualWidth, out actualHeight);
            float definitionScaleW = texture.width / (float)actualWidth;
            float definitionScaleH = texture.height / (float)actualHeight;
            float definitionScale = Mathf.Min(definitionScaleW, definitionScaleH);
            float scaledPTU = spriteDataProvider.pixelsPerUnit * definitionScale;

            foreach (var sprite in sprites)
            {
                var guid = sprite.GetSpriteID();
                {
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
                        sp.position = (spriteBone[i].position - new Vector3(pivotPointInPixels.x, pivotPointInPixels.y, 0.0f)) / scaledPTU;
                        sp.length = spriteBone[i].length / scaledPTU;
                        outputSpriteBones[i] = sp;

                        // Calculate bind poses
                        Matrix4x4 mat = Matrix4x4.identity;
                        mat = Matrix4x4.Rotate(Quaternion.Inverse(sp.rotation));
                        mat = mat * Matrix4x4.Translate(-sp.position);

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

        static bool PostProcessSpriteMeshData(ISpriteEditorDataProvider spriteEditorDataProvider, UnityEngine.Texture2D texture, Sprite[] sprites)
        {
            var spriteMeshDataProvider = spriteEditorDataProvider.GetDataProvider<ISpriteMeshDataProvider>();
            var textureDataProvider = spriteEditorDataProvider.GetDataProvider<ITextureDataProvider>();
            if (texture == null || sprites == null || sprites.Length == 0 || spriteMeshDataProvider == null || textureDataProvider == null)
                return false;

            bool dataChanged = false;

            int actualWidth = 0, actualHeight = 0;
            textureDataProvider.GetTextureActualWidthAndHeight(out actualWidth, out actualHeight);
            float definitionScaleW = texture.width / (float)actualWidth;
            float definitionScaleH = texture.height / (float)actualHeight;
            float definitionScale = Mathf.Min(definitionScaleW, definitionScaleH);
            float scaledPTU = spriteEditorDataProvider.pixelsPerUnit * definitionScale;

            foreach (var sprite in sprites)
            {
                var guid = sprite.GetSpriteID();
                Vertex2DMetaData[] vertices = spriteMeshDataProvider.GetVertices(guid);
                int[] indices = spriteMeshDataProvider.GetIndices(guid);

                if (vertices.Length > 2 && indices.Length > 2)
                {
                    NativeArray<Vector3> vertexArray = new NativeArray<Vector3>(vertices.Length, Allocator.Temp);
                    NativeArray<BoneWeight> boneWeightArray = new NativeArray<BoneWeight>(vertices.Length, Allocator.Temp);

                    for (int i = 0; i < vertices.Length; ++i)
                    {
                        vertexArray[i] = (Vector3)(vertices[i].position - sprite.pivot) / scaledPTU;
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
    }
}
