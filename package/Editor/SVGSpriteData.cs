using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.U2D;
using UnityEngine.Experimental.U2D;

namespace Unity.VectorGraphics.Editor
{
    [Serializable]
    internal class SVGSpriteData
    {
        public float tessellationDetail;
        public SpriteRect spriteRect;
        public List<Vector2> physicsOutlineVectors;
        public List<int> physicsOutlineLengths;
        public List<SpriteBone> bones;
        public Vertex2DMetaData[] skinVertices;
        public int[] skinIndices;
        public Vector2Int[] skinEdges;

        public SVGSpriteData()
        {
            tessellationDetail = 0.0f;
            spriteRect = new SpriteRect();
            physicsOutlineLengths = new List<int>();
            physicsOutlineVectors = new List<Vector2>();
            bones = new List<SpriteBone>();
            skinVertices = new Vertex2DMetaData[] { };
            skinIndices = new int[] { };
            skinEdges = new Vector2Int[] { };
        }

        public void Load(SerializedObject so)
        {
            var importer = so.targetObject as SVGImporter;
            var sprite = SVGImporter.GetImportedSprite(importer.assetPath);
            spriteRect.name = sprite.name;

            int targetWidth;
            int targetHeight;
            importer.TextureSizeForSpriteEditor(out targetWidth, out targetHeight);
            spriteRect.rect = new Rect(0, 0, targetWidth, targetHeight);
            var textureSize = new Vector2(targetWidth, targetHeight);

            var baseSP = so.FindProperty("m_SpriteData");
            var spriteRectSP = baseSP.FindPropertyRelative("spriteRect");
            spriteRect.border = Vector4.zero;
            spriteRect.pivot = sprite.pivot / textureSize;
            var guidSP = spriteRectSP.FindPropertyRelative("m_SpriteID");
            spriteRect.spriteID = new GUID(guidSP.stringValue);

            SVGPhysicsOutlineDataProvider.Load(baseSP, out physicsOutlineVectors, out physicsOutlineLengths);

            if (importer.enableAnimationTools)
            {
                SVGBoneDataProvider.Load(baseSP, out bones);
                SVGMeshDataProvider.Load(baseSP, out skinVertices, out skinIndices, out skinEdges);
            }
        }

        public void Apply(SerializedObject so)
        {
            var baseSP = so.FindProperty("m_SpriteData");
            var spriteRectSP = baseSP.FindPropertyRelative("spriteRect");
            spriteRectSP.FindPropertyRelative("m_Rect").rectValue = spriteRect.rect;
            spriteRectSP.FindPropertyRelative("m_Name").stringValue = spriteRect.name;
            spriteRectSP.FindPropertyRelative("m_Border").vector4Value = spriteRect.border;
            spriteRectSP.FindPropertyRelative("m_Alignment").intValue = (int)spriteRect.alignment;
            spriteRectSP.FindPropertyRelative("m_Pivot").vector2Value = spriteRect.pivot;
            spriteRectSP.FindPropertyRelative("m_SpriteID").stringValue = spriteRect.spriteID.ToString();

            baseSP.FindPropertyRelative("tessellationDetail").floatValue = tessellationDetail;

            SVGPhysicsOutlineDataProvider.Apply(baseSP, physicsOutlineVectors, physicsOutlineLengths);
            SVGBoneDataProvider.Apply(baseSP, bones);
            SVGMeshDataProvider.Apply(baseSP, skinVertices, skinIndices, skinEdges);
        }

        public static Vector2 ConvertToTextureSpace(Vector2 v, Rect spriteBBox, Vector2 textureSize)
        {
            v -= spriteBBox.position;
            v /= spriteBBox.size;
            v *= textureSize;
            return v;
        }

        public static Vector2 ConvertToSpriteSpace(Vector2 v, Rect spriteBBox, Vector2 textureSize)
        {
            v /= textureSize;
            v *= spriteBBox.size;
            v += spriteBBox.position;
            return v;
        }
    }

    internal class SVGDataProviderBase
    {
        private SVGImporter m_Importer;

        public SVGDataProviderBase(SVGImporter importer)
        {
            m_Importer = importer;
        }

        public SVGSpriteData GetSVGSpriteData()
        {
            return m_Importer.GetSVGSpriteData();
        }

        public SVGImporter GetImporter()
        {
            return m_Importer;
        }

        public Sprite GetImportedSprite()
        {
            return SVGImporter.GetImportedSprite(GetImporter().assetPath);
        }

        public Rect GetImportedSpriteBBox()
        {
            return VectorUtils.Bounds(GetImportedSprite().vertices);
        }

        public Vector2 GetTextureSize()
        {
            int targetWidth;
            int targetHeight;
            GetImporter().TextureSizeForSpriteEditor(out targetWidth, out targetHeight);
            return new Vector2(targetWidth, targetHeight);
        }
    }

    internal class SVGTextureDataProvider : SVGDataProviderBase, ITextureDataProvider
    {
        public SVGTextureDataProvider(SVGImporter importer) : base(importer)
        { }

        private Texture2D m_Texture;
        public Texture2D texture
        {
            get
            {
                if (m_Texture == null)
                {
                    int width;
                    int height;
                    GetImporter().TextureSizeForSpriteEditor(out width, out height);

                    var asset = AssetDatabase.LoadMainAssetAtPath(GetImporter().assetPath);
                    var sprite = SVGImporter.GetImportedSprite(asset);
                    m_Texture = VectorUtils.RenderSpriteToTexture2D(sprite, width, height, 4);
                }
                return m_Texture;
            }
        }

        public void GetTextureActualWidthAndHeight(out int width, out int height)
        {
            width = texture.width;
            height = texture.height;            
        }

        public Texture2D GetReadableTexture2D()
        {
            return texture;
        }
    }

    internal class SVGPhysicsOutlineDataProvider : SVGDataProviderBase, ISpritePhysicsOutlineDataProvider
    {
        public SVGPhysicsOutlineDataProvider(SVGImporter importer) : base(importer)
        { }

        List<Vector2[]> ISpritePhysicsOutlineDataProvider.GetOutlines(GUID guid)
        {
            return DecodeOutlines(GetSVGSpriteData().physicsOutlineVectors, GetSVGSpriteData().physicsOutlineLengths);
        }

        void ISpritePhysicsOutlineDataProvider.SetOutlines(GUID guid, List<Vector2[]> data)
        {
            EncodeOutlines(data, ref GetSVGSpriteData().physicsOutlineVectors, ref GetSVGSpriteData().physicsOutlineLengths);
        }

        float ISpritePhysicsOutlineDataProvider.GetTessellationDetail(GUID guid)
        {
            return GetSVGSpriteData().tessellationDetail;
        }

        void ISpritePhysicsOutlineDataProvider.SetTessellationDetail(GUID guid, float value)
        {
            GetSVGSpriteData().tessellationDetail = value;
        }

        public static void Load(SerializedProperty baseSP, out List<Vector2> vectors, out List<int> lengths)
        {
            var vectorsSP = baseSP.FindPropertyRelative("physicsOutlineVectors");
            vectors = new List<Vector2>(vectorsSP.arraySize);
            for (int i = 0; i < vectorsSP.arraySize; ++i)
                vectors.Add(vectorsSP.GetArrayElementAtIndex(i).vector2Value);

            var lengthsSP = baseSP.FindPropertyRelative("physicsOutlineLengths");
            lengths = new List<int>(lengthsSP.arraySize);
            for (int i = 0; i < lengthsSP.arraySize; ++i)
                lengths.Add(lengthsSP.GetArrayElementAtIndex(i).intValue);
        }

        public static void Apply(SerializedProperty baseSP, List<Vector2> vectors, List<int> lengths)
        {
            var outlinesSP = baseSP.FindPropertyRelative("physicsOutlineVectors");
            outlinesSP.arraySize = vectors.Count;
            for (int i = 0; i < vectors.Count; ++i)
            {
                outlinesSP.GetArrayElementAtIndex(i).vector2Value = vectors[i];
            }

            var lengthsSP = baseSP.FindPropertyRelative("physicsOutlineLengths");
            lengthsSP.arraySize = lengths.Count;
            for (int i = 0; i < lengths.Count; ++i)
            {
                lengthsSP.GetArrayElementAtIndex(i).intValue = lengths[i];
            }
        }

        private static List<Vector2[]> DecodeOutlines(List<Vector2> vectors, List<int> lengths)
        {
            int i = 0;
            var o = new List<Vector2[]>(lengths.Count);
            foreach (int length in lengths)
            {
                var a = new Vector2[length];
                for (int j = 0; j < length; ++j)
                    a[j] = vectors[i + j];
                o.Add(a);
                i += length;
            }
            return o;
        }

        private static void EncodeOutlines(List<Vector2[]> outlines, ref List<Vector2> vectors, ref List<int> lengths)
        {
            vectors.Clear();
            lengths.Clear();

            foreach (var a in outlines)
            {
                lengths.Add(a.Length);
                foreach (var v in a)
                    vectors.Add(v);
            }
        }
    }

    internal class SVGBoneDataProvider : SVGDataProviderBase, ISpriteBoneDataProvider
    {
        public SVGBoneDataProvider(SVGImporter importer) : base(importer)
        { }

        public List<SpriteBone> GetBones(GUID guid)
        {
            return GetSVGSpriteData().bones;
        }

        public void SetBones(GUID guid, List<SpriteBone> bones)
        {
            GetSVGSpriteData().bones = bones;
        }

        public static void Load(SerializedProperty baseSP, out List<SpriteBone> bones)
        {
            var bonesSP = baseSP.FindPropertyRelative("bones");
            bones = new List<SpriteBone>(bonesSP.arraySize);
            for (int i = 0; i < bonesSP.arraySize; ++i)
            {
                var bone = new SpriteBone();
                var sp = bonesSP.GetArrayElementAtIndex(i);
                bone.length = sp.FindPropertyRelative("m_Length").floatValue;
                bone.position = sp.FindPropertyRelative("m_Position").vector3Value;
                bone.rotation = sp.FindPropertyRelative("m_Rotation").quaternionValue;
                bone.parentId = sp.FindPropertyRelative("m_ParentId").intValue;
                bone.name = sp.FindPropertyRelative("m_Name").stringValue;
                bones.Add(bone);
            }
        }

        public static void Apply(SerializedProperty baseSP, List<SpriteBone> bones)
        {
            var bonesSP = baseSP.FindPropertyRelative("bones");
            bonesSP.arraySize = bones.Count;
            for (int i = 0; i < bones.Count; ++i)
            {
                var sp = bonesSP.GetArrayElementAtIndex(i);
                var bone = bones[i];
                sp.FindPropertyRelative("m_Length").floatValue = bone.length;
                sp.FindPropertyRelative("m_Position").vector3Value = bone.position;
                sp.FindPropertyRelative("m_Rotation").quaternionValue = bone.rotation;
                sp.FindPropertyRelative("m_ParentId").intValue = bone.parentId;
                sp.FindPropertyRelative("m_Name").stringValue = bone.name;
            }
        }
    }

    internal class SVGMeshDataProvider : SVGDataProviderBase, ISpriteMeshDataProvider
    {
        struct Triangle
        {
            public int a;
            public int b;
            public int c;
        }

        public SVGMeshDataProvider(SVGImporter importer) : base(importer)
        {
            if (GetSVGSpriteData().skinVertices.Length == 0)
            {
                BuildDefaultMesh();
            }
        }

        private void BuildDefaultMesh()
        {
            // Build default vertices/indices
            var bbox = GetImportedSpriteBBox();
            var textureSize = GetTextureSize();
            var sprite = GetImportedSprite();
            var verts = new Vertex2DMetaData[sprite.vertices.Length];
            for (int i = 0; i < sprite.vertices.Length; ++i)
            {
                var p = SVGSpriteData.ConvertToTextureSpace(sprite.vertices[i], bbox, textureSize);
                verts[i] = new Vertex2DMetaData() { position = p };
            }

            GetSVGSpriteData().skinVertices = verts;
            GetSVGSpriteData().skinIndices = sprite.triangles.Select(x => (int)x).ToArray();

            // Build triangle list for each edge
            var edgeTrianglesMap = new Dictionary<Vector2Int, HashSet<Triangle>>();
            var triangles = sprite.triangles;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                var a = triangles[t];
                var b = triangles[t + 1];
                var c = triangles[t + 2];
                var triangle = new Triangle() { a = a, b = b, c = c };

                var edge0 = new Vector2Int(Math.Min(a, b), Math.Max(a, b));
                var edge1 = new Vector2Int(Math.Min(b, c), Math.Max(b, c));
                var edge2 = new Vector2Int(Math.Min(c, a), Math.Max(c, a));

                if (!edgeTrianglesMap.ContainsKey(edge0))
                    edgeTrianglesMap.Add(edge0, new HashSet<Triangle>());
                if (!edgeTrianglesMap.ContainsKey(edge1))
                    edgeTrianglesMap.Add(edge1, new HashSet<Triangle>());
                if (!edgeTrianglesMap.ContainsKey(edge2))
                    edgeTrianglesMap.Add(edge2, new HashSet<Triangle>());

                edgeTrianglesMap[edge0].Add(triangle);
                edgeTrianglesMap[edge1].Add(triangle);
                edgeTrianglesMap[edge2].Add(triangle);
            }

            // Select edges that are associated with a single triangle (i.e., they are not shared)
            GetSVGSpriteData().skinEdges = edgeTrianglesMap.Where(x => x.Value.Count == 1).Select(x => x.Key).ToArray();
        }

        public Vertex2DMetaData[] GetVertices(GUID guid)
        {
            return GetSVGSpriteData().skinVertices;
        }

        public void SetVertices(GUID guid, Vertex2DMetaData[] vertices)
        {
            GetSVGSpriteData().skinVertices = vertices;
        }

        public int[] GetIndices(GUID guid)
        {
            return GetSVGSpriteData().skinIndices;
        }

        public void SetIndices(GUID guid, int[] indices)
        {
            GetSVGSpriteData().skinIndices = indices;
        }

        public Vector2Int[] GetEdges(GUID guid)
        {
            return GetSVGSpriteData().skinEdges;
        }

        public void SetEdges(GUID guid, Vector2Int[] edges)
        {
            GetSVGSpriteData().skinEdges = edges;
        }

        public static void Load(SerializedProperty baseSP, out Vertex2DMetaData[] vertices, out int[] indices, out Vector2Int[] edges)
        {
            var skinVerticesSP = baseSP.FindPropertyRelative("skinVertices");
            vertices = new Vertex2DMetaData[skinVerticesSP.arraySize];
            for (int i = 0; i < skinVerticesSP.arraySize; ++i)
            {
                var v = new Vertex2DMetaData();
                var vSP = skinVerticesSP.GetArrayElementAtIndex(i);
                v.position = vSP.FindPropertyRelative("position").vector2Value;

                var wSP = vSP.FindPropertyRelative("boneWeight");
                var w = new BoneWeight();
                w.weight0 = wSP.FindPropertyRelative("m_Weight0").floatValue;
                w.weight1 = wSP.FindPropertyRelative("m_Weight1").floatValue;
                w.weight2 = wSP.FindPropertyRelative("m_Weight2").floatValue;
                w.weight3 = wSP.FindPropertyRelative("m_Weight3").floatValue;
                w.boneIndex0 = wSP.FindPropertyRelative("m_BoneIndex0").intValue;
                w.boneIndex1 = wSP.FindPropertyRelative("m_BoneIndex1").intValue;
                w.boneIndex2 = wSP.FindPropertyRelative("m_BoneIndex2").intValue;
                w.boneIndex3 = wSP.FindPropertyRelative("m_BoneIndex3").intValue;
                v.boneWeight = w;

                vertices[i] = v;
            }

            var skinIndicesSP = baseSP.FindPropertyRelative("skinIndices");
            indices = new int[skinIndicesSP.arraySize];
            for (int i = 0; i < skinIndicesSP.arraySize; ++i)
            {
                indices[i] = skinIndicesSP.GetArrayElementAtIndex(i).intValue;
            }

            var skinEdgesSP = baseSP.FindPropertyRelative("skinEdges");
            edges = new Vector2Int[skinEdgesSP.arraySize];
            for (int i = 0; i < skinEdgesSP.arraySize; ++i)
            {
                var e = new Vector2Int();
                var sp = skinEdgesSP.GetArrayElementAtIndex(i);
                e.x = sp.FindPropertyRelative("x").intValue;
                e.y = sp.FindPropertyRelative("y").intValue;
                edges[i] = e;
            }
        }

        public static void Apply(SerializedProperty baseSP, Vertex2DMetaData[] vertices, int[] indices, Vector2Int[] edges)
        {
            var skinVerticesSP = baseSP.FindPropertyRelative("skinVertices");
            skinVerticesSP.arraySize = vertices.Length;
            for (int i = 0; i < vertices.Length; ++i)
            {
                var v = vertices[i];
                var vSP = skinVerticesSP.GetArrayElementAtIndex(i);
                vSP.FindPropertyRelative("position").vector2Value = v.position;
                var wSP = vSP.FindPropertyRelative("boneWeight");
                wSP.FindPropertyRelative("m_Weight0").floatValue = v.boneWeight.weight0;
                wSP.FindPropertyRelative("m_Weight1").floatValue = v.boneWeight.weight1;
                wSP.FindPropertyRelative("m_Weight2").floatValue = v.boneWeight.weight2;
                wSP.FindPropertyRelative("m_Weight3").floatValue = v.boneWeight.weight3;
                wSP.FindPropertyRelative("m_BoneIndex0").intValue = v.boneWeight.boneIndex0;
                wSP.FindPropertyRelative("m_BoneIndex1").intValue = v.boneWeight.boneIndex1;
                wSP.FindPropertyRelative("m_BoneIndex2").intValue = v.boneWeight.boneIndex2;
                wSP.FindPropertyRelative("m_BoneIndex3").intValue = v.boneWeight.boneIndex3;
            }

            var skinIndicesSP = baseSP.FindPropertyRelative("skinIndices");
            skinIndicesSP.arraySize = indices.Length;
            for (int i = 0; i < indices.Length; ++i)
            {
                skinIndicesSP.GetArrayElementAtIndex(i).intValue = indices[i];
            }

            var skinEdgesSP = baseSP.FindPropertyRelative("skinEdges");
            skinEdgesSP.arraySize = edges.Length;
            for (int i = 0; i < edges.Length; ++i)
            {
                var e = edges[i];
                var sp = skinEdgesSP.GetArrayElementAtIndex(i);
                sp.FindPropertyRelative("x").intValue = e.x;
                sp.FindPropertyRelative("y").intValue = e.y;
            }
        }
    }
}