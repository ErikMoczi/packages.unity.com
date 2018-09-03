using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using LibTessDotNet;

namespace Unity.VectorGraphics
{
    public static partial class VectorUtils
    {
        /// <summary>Holds the tessellated Scene geometry and associated data.</summary>
        public class Geometry
        {
            /// <summary>The vertices of the geometry.</summary>
            public Vector2[] vertices;

            /// <summary>The UV coordinates of the geometry.</summary>
            public Vector2[] uvs;

            /// <summary>The triangle indices of the geometry.</summary>
            public UInt16[] indices;

            /// <summary>The color of the geometry.</summary>
            public Color color;

            /// <summary>The world transform of the geometry.</summary>
            public Matrix2D worldTransform;

            /// <summary>The fill of the geometry. May be null.</summary>
            public IFill fill;

            /// <summary>The filling transform of the geometry.</summary>
            public Matrix2D fillTransform;

            /// <summary>The unclipped bounds of the geometry.</summary>
            public Rect unclippedBounds;

            /// <summary>The setting index of the geometry.</summary>
            /// <remarks>
            /// This is used to refer to the proper texture/gradient settings inside the texture atlas.
            /// This should be set to 0 for geometries without texture or gradients.
            /// </remarks>
            public int settingIndex;
        }

        /// <summary>Tessellates a Scene object into triangles.</summary>
        /// <param name="node">The root node of the hierarchy to tessellate</param>
        /// <param name="tessellationOptions">The tessellation options</param>
        /// <param name="nodeOpacities">If provided, the resulting node opacities</param>
        /// <returns>A list of tesselated geometry</returns>
        public static List<Geometry> TessellateScene(Scene scene, TessellationOptions tessellationOptions, Dictionary<SceneNode, float> nodeOpacities = null)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TessellateVectorScene");

            VectorClip.ResetClip();
            var geoms = TessellateNodeHierarchyRecursive(scene.root, tessellationOptions, Matrix2D.identity, 1.0f, nodeOpacities);

            UnityEngine.Profiling.Profiler.EndSample();

            return geoms;
        }

        private static List<Geometry> TessellateNodeHierarchyRecursive(SceneNode node, TessellationOptions tessellationOptions, Matrix2D worldTransform, float worldOpacity, Dictionary<SceneNode, float> nodeOpacities)
        {
            if (node.clipper != null)
                VectorClip.PushClip(TraceNodeHierarchyShapes(node.clipper, tessellationOptions), worldTransform);

            var geoms = new List<Geometry>();

            if (node.drawables != null)
            {
                foreach (var drawable in node.drawables)
                {
                    var vectorShape = drawable as Shape;
                    if (vectorShape != null)
                    {
                        TessellateShape(vectorShape, geoms, tessellationOptions);
                        continue;
                    }

                    var vectorPath = drawable as Path;
                    if (vectorPath != null)
                    {
                        TessellatePath(vectorPath.contour, vectorPath.pathProps, geoms, tessellationOptions);
                        continue;
                    }

                    var vectorRect = drawable as Rectangle;
                    if (vectorRect != null)
                    {
                        TessellateRectangle(vectorRect, geoms, tessellationOptions);
                        continue;
                    }
                }

                foreach (var g in geoms)
                {
                    g.color.a *= worldOpacity;
                    g.worldTransform = worldTransform;
                    g.unclippedBounds = Bounds(g.vertices);

                    VectorClip.ClipGeometry(g);
                }
            }

            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    var childOpacity = 1.0f;
                    if (nodeOpacities == null || !nodeOpacities.TryGetValue(child, out childOpacity))
                        childOpacity = 1.0f;

                    var transform = worldTransform * child.transform;
                    var opacity = worldOpacity * childOpacity;
                    var childGeoms = TessellateNodeHierarchyRecursive(child, tessellationOptions, transform, opacity, nodeOpacities);

                    geoms.AddRange(childGeoms);
                }
            }

            if (node.clipper != null)
                VectorClip.PopClip();

            return geoms;
        }

        internal static List<Vector2[]> TraceNodeHierarchyShapes(SceneNode root, TessellationOptions tessellationOptions)
        {
            var shapes = new List<Vector2[]>();

            foreach (var nodeInfo in WorldTransformedSceneNodes(root, null))
            {
                var node = nodeInfo.node;
                if (node.drawables == null || node.drawables.Count == 0)
                    continue;

                foreach (var drawable in node.drawables)
                {
                    var vectorShape = drawable as Shape;
                    if (vectorShape != null)
                    {
                        foreach (var c in vectorShape.contours)
                        {
                            var shape = VectorUtils.TraceShape(c, vectorShape.pathProps.stroke, tessellationOptions);
                            if (shape.Length > 0)
                                shapes.Add(shape.Select(v => nodeInfo.worldTransform * v).ToArray());
                        }
                        continue;
                    }

                    var vectorRect = drawable as Rectangle;
                    if (vectorRect != null)
                    {
                        var shape = VectorUtils.TraceRectangle(vectorRect, vectorRect.pathProps.stroke, tessellationOptions);
                        if (shape.Length > 0)
                            shapes.Add(shape.Select(v => nodeInfo.worldTransform * v).ToArray());
                        continue;
                    }
                }
            }

            return shapes;
        }

        internal static void TessellateShape(Shape vectorShape, List<Geometry> geoms, TessellationOptions tessellationOptions)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TessellateShape");

            // Don't generate any geometry for pattern fills since these are generated from another SceneNode
            if (vectorShape.fill != null && !(vectorShape.fill is PatternFill))
            {
                UnityEngine.Profiling.Profiler.BeginSample("LibTess");

                Color shapeColor = Color.white;
                if (vectorShape.fill is SolidFill)
                    shapeColor = ((SolidFill)vectorShape.fill).color;

                if (vectorShape.fill is GradientFill)
                    shapeColor *= (vectorShape.fill as GradientFill).tint;
                if (vectorShape.fill is TextureFill)
                    shapeColor *= (vectorShape.fill as TextureFill).tint;

                var tess = new Tess();

                var angle = 45.0f * Mathf.Deg2Rad;
                var mat = Matrix2D.Rotate(angle);
                var invMat = Matrix2D.Rotate(-angle);

                foreach (var c in vectorShape.contours)
                {
                    var contour = new List<ContourVertex>(100);
                    foreach (var v in VectorUtils.TraceShape(c, vectorShape.pathProps.stroke, tessellationOptions))
                    {
                        var tv = mat.MultiplyPoint(v);
                        contour.Add(new ContourVertex() { Position = new Vec3() { X = tv.x, Y = tv.y, Z = 0.0f }});
                    }
                    tess.AddContour(contour.ToArray(), ContourOrientation.Original);
                }

                var windingRule = (vectorShape.fill.mode == FillMode.OddEven) ? WindingRule.EvenOdd : WindingRule.NonZero; 
                tess.Tessellate(windingRule, ElementType.Polygons, 3);

                var indices = tess.Elements.Select(i => (UInt16)i);
                var vertices = tess.Vertices.Select(v => invMat.MultiplyPoint(new Vector2(v.Position.X, v.Position.Y)));

                if (indices.Count() > 0)
                {
                    geoms.Add(new Geometry() { vertices = vertices.ToArray(), indices = indices.ToArray(), color = shapeColor, fill = vectorShape.fill, fillTransform = vectorShape.fillTransform });
                }
                UnityEngine.Profiling.Profiler.EndSample();
            }

            var stroke = vectorShape.pathProps.stroke;
            if (stroke != null && stroke.halfThickness > VectorUtils.Epsilon)
            {
                foreach (var c in vectorShape.contours)
                {
                    Vector2[] strokeVerts;
                    UInt16[] strokeIndices;
                    VectorUtils.TessellatePath(c, vectorShape.pathProps, tessellationOptions, out strokeVerts, out strokeIndices);
                    if (strokeIndices.Length > 0)
                    {
                        geoms.Add(new Geometry() { vertices = strokeVerts, indices = strokeIndices, color = vectorShape.pathProps.stroke.color });
                    }
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private static void TessellatePath(BezierContour contour, PathProperties pathProps, List<Geometry> geoms, TessellationOptions tessellationOptions)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TessellatePath");

            if (pathProps.stroke != null)
            {
                Vector2[] vertices;
                UInt16[] indices;
                VectorUtils.TessellatePath(contour, pathProps, tessellationOptions, out vertices, out indices);

                var color = pathProps.stroke.color;
                if (indices.Length > 0)
                {
                    geoms.Add(new Geometry() { vertices = vertices, indices = indices, color = color });
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        internal static Vector2[] GenerateShapeUVs(Vector2[] verts, Rect bounds, Matrix2D uvTransform)
        {
            UnityEngine.Profiling.Profiler.BeginSample("GenerateShapeUVs");

            uvTransform =
                Matrix2D.Translate(new Vector2(0, 1)) * Matrix2D.Scale(new Vector2(1.0f, -1.0f)) * // Do 1-uv.y
                uvTransform *
                Matrix2D.Scale(new Vector2(1.0f / bounds.width, 1.0f / bounds.height)) * Matrix2D.Translate(-bounds.position);
            var uvs = new Vector2[verts.Length];
            int vertCount = verts.Length;
            for (int i = 0; i < vertCount; i++)
                uvs[i] = uvTransform * verts[i];

            UnityEngine.Profiling.Profiler.EndSample();

            return uvs;
        }

        static void SwapXY(ref Vector2 v)
        {
            float t = v.x;
            v.x = v.y;
            v.y = t;
        }

        struct RawTexture
        {
            public Color32[] rgba;
            public int width;
            public int height;
        }

        class AtlasEntry
        {
            public RawTexture texture;
            public PackRectItem atlasLocation;
        }

        /// <summary>A struct to hold packed atlas entries.</summary>
        public class TextureAtlas
        {
            /// <summary>The texture atlas.</summary>
            public Texture2D texture { get; set; }

            /// <summary>The atlas entries.</summary>
            public List<PackRectItem> entries { get; set; }
        };

        /// <summary>Generates a Texture2D atlas containing the textures and gradients for the vector geometry, and fill the UVs of the geometry.</summary>
        /// <param name="geoms">The list of Geometry objects, probably created with TessellateNodeHierarchy</param>
        /// <param name="rasterSize">Maximum size of the generated texture</param>
        /// <returns>The generated texture atlas</returns>
        public static TextureAtlas GenerateAtlasAndFillUVs(IEnumerable<Geometry> geoms, uint rasterSize)
        {
            var atlas = GenerateAtlas(geoms, rasterSize);
            if (atlas != null)
                FillUVs(geoms, atlas);
            return atlas;
        }

        /// <summary>Generates a Texture2D atlas containing the textures and gradients for the vector geometry.</summary>
        /// <param name="geoms">The list of Geometry objects, probably created with TessellateNodeHierarchy</param>
        /// <param name="rasterSize">Maximum size of the generated texture</param>
        /// <returns>The generated texture atlas</returns>
        public static TextureAtlas GenerateAtlas(IEnumerable<Geometry> geoms, uint rasterSize)
        {
            UnityEngine.Profiling.Profiler.BeginSample("GenerateAtlas");

            var fills = new Dictionary<IFill, AtlasEntry>();
            int texturedGeomCount = 0;
            foreach (var g in geoms)
            {
                RawTexture tex;
                if (g.fill is GradientFill)
                {
                    tex = new RawTexture() { width = (int)rasterSize, height = 1, rgba = RasterizeGradientStripe((GradientFill)g.fill, (int)rasterSize) };
                    ++texturedGeomCount;
                }
                else if (g.fill is TextureFill)
                {
                    var fillTex = ((TextureFill)g.fill).texture;
                    tex = new RawTexture() { rgba = fillTex.GetPixels32(), width = fillTex.width, height = fillTex.height };                    
                    ++texturedGeomCount;
                }
                else
                {
                    continue;
                }
                fills[g.fill] = new AtlasEntry() { texture = tex };
            }

            if (fills.Count == 0)
                return null;

            Vector2 atlasSize;
            var rectsToPack = fills.Select(x => new KeyValuePair<IFill, Vector2>(x.Key, new Vector2(x.Value.texture.width, x.Value.texture.height))).ToList();
            rectsToPack.Add(new KeyValuePair<IFill, Vector2>(null, new Vector2(2, 2))); // White fill
            var pack = PackRects(rectsToPack, out atlasSize);

            // The first row of the atlas is reserved for the gradient settings
            for (int packIndex = 0; packIndex < pack.Count; ++packIndex)
            {
                var item = pack[packIndex];
                item.position.y += 1;
                pack[packIndex] = item;
            }
            atlasSize.y += 1;

            // Need enough space on first row for texture settings
            int maxSettingIndex = 0;
            foreach (var item in pack)
                maxSettingIndex = Math.Max(maxSettingIndex, item.settingIndex);
            int minWidth = (maxSettingIndex+1) * 3;
            atlasSize.x = Math.Max(minWidth, (int)atlasSize.x);

            int atlasWidth = (int)atlasSize.x;
            int atlasHeight = (int)atlasSize.y;
            var atlasColors = new Color32[atlasWidth * atlasHeight]; // Comes out all black transparent
            Vector2 atlasInvSize = new Vector2(1.0f / (float)atlasWidth, 1.0f / (float)atlasHeight);
            Vector2 whiteTexelsScreenPos = pack[pack.Count - 1].position;
            Vector2 whiteTexelsPos = (whiteTexelsScreenPos + Vector2.one) * atlasInvSize;

            int i = 0;
            RawTexture rawAtlasTex = new RawTexture() { rgba = atlasColors, width = atlasWidth, height = atlasHeight };
            foreach (var entry in fills.Values)
            {
                var packItem = pack[i++];
                entry.atlasLocation = packItem;
                BlitRawTexture(entry.texture, rawAtlasTex, (int)packItem.position.x, (int)packItem.position.y, packItem.rotated);
            }

            RawTexture whiteTex = new RawTexture() { width = 2, height = 2, rgba = new Color32[4] };
            for (i = 0; i < whiteTex.rgba.Length; i++)
                whiteTex.rgba[i] = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
            BlitRawTexture(whiteTex, rawAtlasTex, (int)whiteTexelsScreenPos.x, (int)whiteTexelsScreenPos.y, false);

            // Setting 0 is reserved for the white texel
            WriteRawInt2Packed(rawAtlasTex, (int)whiteTexelsScreenPos.x, (int)whiteTexelsScreenPos.y, 0, 0);
            WriteRawInt2Packed(rawAtlasTex, (int)whiteTexelsScreenPos.x, (int)whiteTexelsScreenPos.y, 1, 0);

            var writtenSettings = new HashSet<int>();
            writtenSettings.Add(0);

            foreach (var g in geoms)
            {
                AtlasEntry entry;
                int vertsCount = g.vertices.Length;
                if ((g.fill != null) && fills.TryGetValue(g.fill, out entry))
                {
                    int setting = entry.atlasLocation.settingIndex;
                    if (writtenSettings.Contains(setting))
                        continue;

                    writtenSettings.Add(setting);

                    // There are 3 consecutive pixels to store the settings
                    int destX = setting * 3;

                    var gradientFill = g.fill as GradientFill;
                    if (gradientFill != null)
                    {
                        var focus = gradientFill.radialFocus;
                        focus += Vector2.one;
                        focus /= 2.0f;
                        focus.y = 1.0f - focus.y;
                        WriteRawFloat4Packed(rawAtlasTex, ((float)gradientFill.type)/255, ((float)gradientFill.addressing)/255, focus.x, focus.y, destX++, 0);
                    }

                    var textureFill = g.fill as TextureFill;
                    if (textureFill != null)
                    {
                        WriteRawFloat4Packed(rawAtlasTex, 0.0f, ((float)textureFill.addressing)/255, 0.0f, 0.0f, destX++, 0);
                    }

                    var pos = entry.atlasLocation.position;
                    var size = new Vector2(entry.texture.width-1, entry.texture.height-1);
                    WriteRawInt2Packed(rawAtlasTex, (int)pos.x, (int)pos.y, destX++, 0);
                    WriteRawInt2Packed(rawAtlasTex, (int)size.x, (int)size.y, destX++, 0);
                }
                else
                {
                    g.uvs = new Vector2[vertsCount];
                    for (i = 0; i < vertsCount; i++)
                        g.uvs[i] = whiteTexelsPos;
                }
            }

            var atlasTex = new Texture2D(atlasWidth, atlasHeight, TextureFormat.ARGB32, false, true);
            atlasTex.wrapModeU = TextureWrapMode.Clamp;
            atlasTex.wrapModeV = TextureWrapMode.Clamp;
            atlasTex.wrapModeW = TextureWrapMode.Clamp;
            atlasTex.SetPixels32(atlasColors);

            UnityEngine.Profiling.Profiler.EndSample();

            return new TextureAtlas() { texture = atlasTex, entries = pack };
        }

        /// <summary>Fill the UVs of the geometry using the provided texture atlas.</summary>
        /// <param name="geoms">The geometry that will have its UVs filled</param>
        /// <param name="texAtlas">The texture atlas used for the UV generation</param>
        public static void FillUVs(IEnumerable<Geometry> geoms, TextureAtlas texAtlas)
        {
            UnityEngine.Profiling.Profiler.BeginSample("FillUVs");

            var fills = new Dictionary<IFill, PackRectItem>();
            foreach (var entry in texAtlas.entries)
            {
                if (entry.fill != null)
                    fills[entry.fill] = entry;
            }

            var item = new PackRectItem();
            foreach (var g in geoms)
            {
                int settingIndex = 0;
                if ((g.fill != null) && fills.TryGetValue(g.fill, out item))
                    settingIndex = item.settingIndex;

                g.uvs = GenerateShapeUVs(g.vertices, g.unclippedBounds, g.fillTransform);
                g.settingIndex = settingIndex;
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}
