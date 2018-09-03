using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.U2D;
using UnityEngine.Experimental.U2D;

namespace Unity.VectorGraphics.Editor
{
    internal class SVGBoneWeightTransfer
    {
        private SVGDataProviderBase m_DataProvider;
        private Color[] m_BoneColors;

        public SVGBoneWeightTransfer(SVGDataProviderBase dataProvider)
        {
            m_DataProvider = dataProvider;

            var bones = m_DataProvider.GetSVGSpriteData().Bones;
            m_BoneColors = new Color[bones.Count];
            for (int i = 0; i < bones.Count; ++i)
                m_BoneColors[i] = ComputeUniqueColor(i, bones.Count);
        }

        public void TransferBoneWeights(Vertex2DMetaData[] srcData, int[] srcIndices, Vertex2DMetaData[] dstData)
        {
            if (srcData.Length == 0 || srcIndices.Length == 0)
                return;

            var weightMap = BuildWeightMap(srcData, srcIndices);

            // Resample bone weights for each vertex position
            var sprite = m_DataProvider.GetImportedSprite();
            float spriteWidth = sprite.rect.width;
            float spriteHeight = sprite.rect.height;

            var bones = m_DataProvider.GetSVGSpriteData().Bones;
            var boneDistances = new KeyValuePair<int, float>[bones.Count];

            for (int i = 0; i < dstData.Length; ++i)
            {
                var v = dstData[i];
                int x = (int)((v.position.x/spriteWidth)*weightMap.width);
                int y = (int)((v.position.y/spriteHeight)*weightMap.height);
                x = Math.Max(0, Math.Min(x, weightMap.width-1));
                y = Math.Max(0, Math.Min(y, weightMap.height-1));

                var texCol = weightMap.GetPixel(x, y);
                var texVector = new Vector4(texCol.r, texCol.g, texCol.b, texCol.a);

                for (int boneIndex = 0; boneIndex< bones.Count; ++boneIndex)
                {
                    var boneCol = m_BoneColors[boneIndex];
                    var boneVector = new Vector4(boneCol.r, boneCol.g, boneCol.b, boneCol.a);
                    float dist = (boneVector - texVector).magnitude;
                    boneDistances[boneIndex] = new KeyValuePair<int, float>(boneIndex, dist);
                }

                // Sort by distance
                Array.Sort(boneDistances, (a, b) => (a.Value > b.Value ? 1 : (a.Value < b.Value ? -1 : 0)));

                dstData[i].boneWeight = ComputeBoneWeight(boneDistances);                
            }

            Texture2D.DestroyImmediate(weightMap);
        }

        private BoneWeight ComputeBoneWeight(KeyValuePair<int, float>[] boneDistances)
        {
            if (boneDistances.Length == 0)
                return new BoneWeight();

            int size = Math.Min(4, boneDistances.Length);
            float sum = 0.0f;
            for (int i = 0; i < size; ++i)
            {
                float dist = boneDistances[i].Value;
                if (Mathf.Approximately(dist, 0.0f))
                    break;
                sum += 1.0f/dist;
            }

            var weights = new float[size];
            for (int i = 0; i < size; ++i)
            {
                float dist = boneDistances[i].Value;
                if (Mathf.Approximately(dist, 0.0f))
                {
                    // Special case, full weight assigned to first bone
                    weights[0] = 1.0f;
                    for (int j = 1; j < size; ++j)
                        weights[j] = 0.0f;
                    break;
                }
                weights[i] = (1.0f/dist) / sum;
            }

            var bw = new BoneWeight();
            bw.boneIndex0 = (size > 0) ? boneDistances[0].Key : 0;
            bw.weight0 = (size > 0) ? weights[0] : 0.0f;
            bw.boneIndex1 = (size > 1) ? boneDistances[1].Key : 0;
            bw.weight1 = (size > 1) ? weights[1] : 0.0f;
            bw.boneIndex2 = (size > 2) ? boneDistances[2].Key : 0;
            bw.weight2 = (size > 2) ? weights[2] : 0.0f;
            bw.boneIndex3 = (size > 3) ? boneDistances[3].Key : 0;
            bw.weight3 = (size > 3) ? weights[3] : 0.0f;

            return bw;
        }

        private Texture2D BuildWeightMap(Vertex2DMetaData[] srcData, int[] srcIndices)
        {
            const int kRtSize = 512;

            var shader = AssetDatabase.LoadMainAssetAtPath("Packages/com.unity.vectorgraphics/Editor/Shaders/VectorWeightMap.shader") as Shader;
            var mat = new Material(shader);
            mat.SetPass(0);

            var rt = new RenderTexture(kRtSize, kRtSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var oldRt = RenderTexture.active;
            RenderTexture.active = rt;

            RenderBoneWeights(srcData, srcIndices, true);

            // "Grow" the weights
            var rt2 = new RenderTexture(kRtSize, kRtSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var lastRt = rt;
            for (int i = 0; i < 16; ++i)
            {
                Graphics.Blit(rt, rt2, mat, 1);
                lastRt = rt2;
                rt2 = rt;
                rt = lastRt;
            }
            RenderTexture.active = lastRt;

            Texture2D copy = new Texture2D(kRtSize, kRtSize, TextureFormat.RGBA32, false, true);
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.ReadPixels(new Rect(0, 0, kRtSize, kRtSize), 0, 0);
            copy.Apply();

            RenderTexture.active = oldRt;

            rt.Release();
            rt2.Release();
            RenderTexture.DestroyImmediate(rt);
            RenderTexture.DestroyImmediate(rt2);

            Material.DestroyImmediate(mat);

            return copy;
        }

        private void RenderBoneWeights(Vertex2DMetaData[] srcData, int[] srcIndices, bool clear)
        {
            var sprite = m_DataProvider.GetImportedSprite();
            float spriteWidth = sprite.rect.width;
            float spriteHeight = sprite.rect.height;
            int boneCount = m_DataProvider.GetSVGSpriteData().Bones.Count;

            if (clear)
                GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < srcIndices.Length; ++i)
            {
                int index = srcIndices[i];
                var data = srcData[index];

                BoneWeight boneWeight = data.boneWeight;
                float weightSum = boneWeight.weight0 + boneWeight.weight1 + boneWeight.weight2 + boneWeight.weight3;

                var color =
                    ComputeUniqueColor(boneWeight.boneIndex0, boneCount) * boneWeight.weight0 +
                    ComputeUniqueColor(boneWeight.boneIndex1, boneCount) * boneWeight.weight1 +
                    ComputeUniqueColor(boneWeight.boneIndex2, boneCount) * boneWeight.weight2 +
                    ComputeUniqueColor(boneWeight.boneIndex3, boneCount) * boneWeight.weight3;
                color.a = 1f;

                color = Color.Lerp(Color.black, color, weightSum);
                GL.Color(color);

                Vector2 vertex = data.position;
                GL.Vertex3(vertex.x / spriteWidth, vertex.y / spriteHeight, 0);
            }
            GL.End();
            GL.PopMatrix();

        }

        private static Color ComputeUniqueColor(int index, int numColors)
        {
            numColors = Mathf.Clamp(numColors, 1, int.MaxValue);

            float hueAngleStep = 360f / (float)numColors;
            float hueLoopOffset = hueAngleStep * 0.5f;

            float hueAngle = index * hueAngleStep;
            float loops = (int)(hueAngle / 360f);
            float hue = ((hueAngle % 360f + (loops * hueLoopOffset % 360f)) / 360f);

            return Color.HSVToRGB(hue, 1f, 1f);
        }
    }
}