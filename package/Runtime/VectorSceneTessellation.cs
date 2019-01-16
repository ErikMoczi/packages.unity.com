using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.VectorGraphics.External.LibTessDotNet;

namespace Unity.VectorGraphics
{
    public static partial class VectorUtils
    {
        /// <summary>Holds the tessellated Scene geometry and associated data.</summary>
        public class Geometry
        {
            /// <summary>The vertices of the geometry.</summary>
            public Vector2[] Vertices;

            /// <summary>The UV coordinates of the geometry.</summary>
            public Vector2[] UVs;

            /// <summary>The triangle indices of the geometry.</summary>
            public UInt16[] Indices;

            /// <summary>The color of the geometry.</summary>
            public Color Color;

            /// <summary>The world transform of the geometry.</summary>
            public Matrix2D WorldTransform;

            /// <summary>The fill of the geometry. May be null.</summary>
            public IFill Fill;

            /// <summary>The filling transform of the geometry.</summary>
            public Matrix2D FillTransform;

            /// <summary>The unclipped bounds of the geometry.</summary>
            public Rect UnclippedBounds;

            /// <summary>The setting index of the geometry.</summary>
            /// <remarks>
            /// This is used to refer to the proper texture/gradient settings inside the texture atlas.
            /// This should be set to 0 for geometries without texture or gradients.
            /// </remarks>
            public int SettingIndex;
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
            var geoms = TessellateNodeHierarchyRecursive(scene.Root, tessellationOptions, scene.Root.Transform, 1.0f, nodeOpacities);

            UnityEngine.Profiling.Profiler.EndSample();

            return geoms;
        }

        #pragma warning disable 612, 618 // Silence use of deprecated IDrawable
        private static List<Geometry> TessellateNodeHierarchyRecursive(SceneNode node, TessellationOptions tessellationOptions, Matrix2D worldTransform, float worldOpacity, Dictionary<SceneNode, float> nodeOpacities)
        {
            if (node.Clipper != null)
                VectorClip.PushClip(TraceNodeHierarchyShapes(node.Clipper, tessellationOptions), worldTransform);

            var geoms = new List<Geometry>();

            if (node.Drawables != null)
            {
                // We process the drawables even though they are obsolete, until we remove the IDrawable interface entirely
                foreach (var drawable in node.Drawables)
                {
                    var vectorShape = drawable as Shape;
                    if (vectorShape != null)
                    {
                        bool isConvex = vectorShape.IsConvex && vectorShape.Contours.Length == 1;
                        TessellateShape(vectorShape, geoms, tessellationOptions, isConvex);
                        continue;
                    }

                    var vectorPath = drawable as Path;
                    if (vectorPath != null)
                    {
                        TessellatePath(vectorPath.Contour, vectorPath.PathProps, geoms, tessellationOptions);
                        continue;
                    }

                    var vectorRect = drawable as Rectangle;
                    if (vectorRect != null)
                    {
                        TessellateRectangle(vectorRect, geoms, tessellationOptions);
                        continue;
                    }
                }
            }

            if (node.Shapes != null)
            {
                foreach (var shape in node.Shapes)
                {
                    bool isConvex = shape.IsConvex && shape.Contours.Length == 1;
                    TessellateShape(shape, geoms, tessellationOptions, isConvex);
                }
            }

            foreach (var g in geoms)
            {
                g.Color.a *= worldOpacity;
                g.WorldTransform = worldTransform;
                g.UnclippedBounds = Bounds(g.Vertices);

                VectorClip.ClipGeometry(g);
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    var childOpacity = 1.0f;
                    if (nodeOpacities == null || !nodeOpacities.TryGetValue(child, out childOpacity))
                        childOpacity = 1.0f;

                    var transform = worldTransform * child.Transform;
                    var opacity = worldOpacity * childOpacity;
                    var childGeoms = TessellateNodeHierarchyRecursive(child, tessellationOptions, transform, opacity, nodeOpacities);

                    geoms.AddRange(childGeoms);
                }
            }

            if (node.Clipper != null)
                VectorClip.PopClip();

            return geoms;
        }

        internal static List<Vector2[]> TraceNodeHierarchyShapes(SceneNode root, TessellationOptions tessellationOptions)
        {
            var shapes = new List<Vector2[]>();

            foreach (var nodeInfo in WorldTransformedSceneNodes(root, null))
            {
                var node = nodeInfo.Node;

                // We process the drawables even though they are obsolete, until we remove the IDrawable interface entirely
                if (node.Drawables != null)
                {
                    foreach (var drawable in node.Drawables)
                    {
                        var vectorShape = drawable as Shape;
                        if (vectorShape != null)
                        {
                            foreach (var c in vectorShape.Contours)
                            {
                                var shape = VectorUtils.TraceShape(c, vectorShape.PathProps.Stroke, tessellationOptions);
                                if (shape.Length > 0)
                                    shapes.Add(shape.Select(v => nodeInfo.WorldTransform * v).ToArray());
                            }
                            continue;
                        }

                        var vectorPath = drawable as Path;
                        if (vectorPath != null)
                        {
                            var shape = VectorUtils.TraceShape(vectorPath.Contour, vectorPath.PathProps.Stroke, tessellationOptions);
                            if (shape.Length > 0)
                                shapes.Add(shape.Select(v => nodeInfo.WorldTransform * v).ToArray());
                            continue;
                        }

                        var vectorRect = drawable as Rectangle;
                        if (vectorRect != null)
                        {
                            var shape = VectorUtils.TraceRectangle(vectorRect, vectorRect.PathProps.Stroke, tessellationOptions);
                            if (shape.Length > 0)
                                shapes.Add(shape.Select(v => nodeInfo.WorldTransform * v).ToArray());
                            continue;
                        }
                    }
                }

                if (node.Shapes != null)
                {
                    foreach (var shape in node.Shapes)
                    {
                        foreach (var c in shape.Contours)
                        {
                            var tracedShape = VectorUtils.TraceShape(c, shape.PathProps.Stroke, tessellationOptions);
                            if (tracedShape.Length > 0)
                                shapes.Add(tracedShape.Select(v => nodeInfo.WorldTransform * v).ToArray());
                        }
                    }
                }
            }

            return shapes;
        }
        #pragma warning restore 612, 618

        private static void TessellateShape(Shape vectorShape, List<Geometry> geoms, TessellationOptions tessellationOptions, bool isConvex)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TessellateShape");

            // Don't generate any geometry for pattern fills since these are generated from another SceneNode
            if (vectorShape.Fill != null && !(vectorShape.Fill is PatternFill))
            {
                Color shapeColor = Color.white;
                if (vectorShape.Fill is SolidFill)
                    shapeColor = ((SolidFill)vectorShape.Fill).Color;

                shapeColor.a *= vectorShape.Fill.Opacity;

                if (isConvex && vectorShape.Contours.Length == 1)
                {
                    TessellateConvexContour(vectorShape, vectorShape.PathProps.Stroke, shapeColor, geoms, tessellationOptions);
                }
                else
                {
                    TessellateShapeLibTess(vectorShape, shapeColor, geoms, tessellationOptions);
                }
            }

            var stroke = vectorShape.PathProps.Stroke;
            if (stroke != null && stroke.HalfThickness > VectorUtils.Epsilon)
            {
                foreach (var c in vectorShape.Contours)
                {
                    Vector2[] strokeVerts;
                    UInt16[] strokeIndices;
                    VectorUtils.TessellatePath(c, vectorShape.PathProps, tessellationOptions, out strokeVerts, out strokeIndices);
                    if (strokeIndices.Length > 0)
                    {
                        geoms.Add(new Geometry() { Vertices = strokeVerts, Indices = strokeIndices, Color = vectorShape.PathProps.Stroke.Color });
                    }
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private static void TessellateConvexContour(Shape shape, Stroke stroke, Color color, List<Geometry> geoms, TessellationOptions tessellationOptions)
        {
            if (shape.Contours.Length != 1 || shape.Contours[0].Segments.Length == 0)
                return;

            UnityEngine.Profiling.Profiler.BeginSample("TessellateConvexContour");

            // Compute geometric mean
            var contour = shape.Contours[0];
            var mean = Vector2.zero;
            foreach (var seg in contour.Segments)
                mean += seg.P0;
            mean /= contour.Segments.Length;

            // Trace the shape and build triangle fan
            var tracedShape = VectorUtils.TraceShape(contour, stroke, tessellationOptions);
            var vertices = new Vector2[tracedShape.Length + 1];
            var indices = new UInt16[tracedShape.Length * 3];

            vertices[0] = mean;
            for (int i = 0; i < tracedShape.Length; ++i)
            {
                vertices[i + 1] = tracedShape[i];
                indices[i * 3] = 0;
                indices[i * 3 + 1] = (UInt16)(i + 1);
                indices[i * 3 + 2] = ((i + 2) >= vertices.Length) ? (UInt16)1 : (UInt16)(i + 2);
            }

            geoms.Add(new Geometry() { Vertices = vertices, Indices = indices, Color = color, Fill = shape.Fill, FillTransform = shape.FillTransform });

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private static void TessellateShapeLibTess(Shape vectorShape, Color color, List<Geometry> geoms, TessellationOptions tessellationOptions)
        {
            UnityEngine.Profiling.Profiler.BeginSample("LibTess");

            var tess = new Tess();

            var angle = 45.0f * Mathf.Deg2Rad;
            var mat = Matrix2D.RotateLH(angle);
            var invMat = Matrix2D.RotateLH(-angle);

            foreach (var c in vectorShape.Contours)
            {
                var contour = new List<Vector2>(100);
                foreach (var v in VectorUtils.TraceShape(c, vectorShape.PathProps.Stroke, tessellationOptions))
                    contour.Add(mat.MultiplyPoint(v));

                tess.AddContour(contour.Select(v => new ContourVertex() { Position = new Vec3() { X = v.x, Y = v.y } }).ToArray(), ContourOrientation.Original);
            }

            var windingRule = (vectorShape.Fill.Mode == FillMode.OddEven) ? WindingRule.EvenOdd : WindingRule.NonZero;
            try
            {
                tess.Tessellate(windingRule, ElementType.Polygons, 3);
            }
            catch (System.Exception)
            {
                Debug.LogWarning("Shape tessellation failed, skipping...");
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }

            var indices = tess.Elements.Select(i => (UInt16)i);
            var vertices = tess.Vertices.Select(v => invMat.MultiplyPoint(new Vector2(v.Position.X, v.Position.Y)));

            if (indices.Count() > 0)
            {
                geoms.Add(new Geometry() { Vertices = vertices.ToArray(), Indices = indices.ToArray(), Color = color, Fill = vectorShape.Fill, FillTransform = vectorShape.FillTransform });
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private static void TessellatePath(BezierContour contour, PathProperties pathProps, List<Geometry> geoms, TessellationOptions tessellationOptions)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TessellatePath");

            if (pathProps.Stroke != null)
            {
                Vector2[] vertices;
                UInt16[] indices;
                VectorUtils.TessellatePath(contour, pathProps, tessellationOptions, out vertices, out indices);

                var color = pathProps.Stroke.Color;
                if (indices.Length > 0)
                {
                    geoms.Add(new Geometry() { Vertices = vertices, Indices = indices, Color = color });
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
            public Color32[] Rgba;
            public int Width;
            public int Height;
        }

        class AtlasEntry
        {
            public RawTexture Texture;
            public PackRectItem AtlasLocation;
        }

        /// <summary>A struct to hold packed atlas entries.</summary>
        public class TextureAtlas
        {
            /// <summary>The texture atlas.</summary>
            public Texture2D Texture { get; set; }

            /// <summary>The atlas entries.</summary>
            public List<PackRectItem> Entries { get; set; }
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
                if (g.Fill is GradientFill)
                {
                    tex = new RawTexture() { Width = (int)rasterSize, Height = 1, Rgba = RasterizeGradientStripe((GradientFill)g.Fill, (int)rasterSize) };
                    ++texturedGeomCount;
                }
                else if (g.Fill is TextureFill)
                {
                    var fillTex = ((TextureFill)g.Fill).Texture;
                    tex = new RawTexture() { Rgba = fillTex.GetPixels32(), Width = fillTex.width, Height = fillTex.height };                    
                    ++texturedGeomCount;
                }
                else
                {
                    continue;
                }
                fills[g.Fill] = new AtlasEntry() { Texture = tex };
            }

            if (fills.Count == 0)
                return null;

            Vector2 atlasSize;
            var rectsToPack = fills.Select(x => new KeyValuePair<IFill, Vector2>(x.Key, new Vector2(x.Value.Texture.Width, x.Value.Texture.Height))).ToList();
            rectsToPack.Add(new KeyValuePair<IFill, Vector2>(null, new Vector2(2, 2))); // White fill
            var pack = PackRects(rectsToPack, out atlasSize);

            // The first row of the atlas is reserved for the gradient settings
            for (int packIndex = 0; packIndex < pack.Count; ++packIndex)
            {
                var item = pack[packIndex];
                item.Position.y += 1;
                pack[packIndex] = item;
            }
            atlasSize.y += 1;

            // Need enough space on first row for texture settings
            int maxSettingIndex = 0;
            foreach (var item in pack)
                maxSettingIndex = Math.Max(maxSettingIndex, item.SettingIndex);
            int minWidth = (maxSettingIndex+1) * 3;
            atlasSize.x = Math.Max(minWidth, (int)atlasSize.x);

            int atlasWidth = (int)atlasSize.x;
            int atlasHeight = (int)atlasSize.y;
            var atlasColors = new Color32[atlasWidth * atlasHeight];
            for (int k = 0; k < atlasWidth * atlasHeight; ++k)
                atlasColors[k] = Color.black;
            Vector2 atlasInvSize = new Vector2(1.0f / (float)atlasWidth, 1.0f / (float)atlasHeight);
            Vector2 whiteTexelsScreenPos = pack[pack.Count - 1].Position;
            Vector2 whiteTexelsPos = (whiteTexelsScreenPos + Vector2.one) * atlasInvSize;

            int i = 0;
            RawTexture rawAtlasTex = new RawTexture() { Rgba = atlasColors, Width = atlasWidth, Height = atlasHeight };
            foreach (var entry in fills.Values)
            {
                var packItem = pack[i++];
                entry.AtlasLocation = packItem;
                BlitRawTexture(entry.Texture, rawAtlasTex, (int)packItem.Position.x, (int)packItem.Position.y, packItem.Rotated);
            }

            RawTexture whiteTex = new RawTexture() { Width = 2, Height = 2, Rgba = new Color32[4] };
            for (i = 0; i < whiteTex.Rgba.Length; i++)
                whiteTex.Rgba[i] = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
            BlitRawTexture(whiteTex, rawAtlasTex, (int)whiteTexelsScreenPos.x, (int)whiteTexelsScreenPos.y, false);

            // Setting 0 is reserved for the white texel
            WriteRawInt2Packed(rawAtlasTex, (int)whiteTexelsScreenPos.x, (int)whiteTexelsScreenPos.y, 0, 0);
            WriteRawInt2Packed(rawAtlasTex, (int)whiteTexelsScreenPos.x, (int)whiteTexelsScreenPos.y, 1, 0);

            var writtenSettings = new HashSet<int>();
            writtenSettings.Add(0);

            foreach (var g in geoms)
            {
                AtlasEntry entry;
                int vertsCount = g.Vertices.Length;
                if ((g.Fill != null) && fills.TryGetValue(g.Fill, out entry))
                {
                    int setting = entry.AtlasLocation.SettingIndex;
                    if (writtenSettings.Contains(setting))
                        continue;

                    writtenSettings.Add(setting);

                    // There are 3 consecutive pixels to store the settings
                    int destX = setting * 3;

                    var gradientFill = g.Fill as GradientFill;
                    if (gradientFill != null)
                    {
                        var focus = gradientFill.RadialFocus;
                        focus += Vector2.one;
                        focus /= 2.0f;
                        focus.y = 1.0f - focus.y;
                        WriteRawFloat4Packed(rawAtlasTex, ((float)gradientFill.Type)/255, ((float)gradientFill.Addressing)/255, focus.x, focus.y, destX++, 0);
                    }

                    var textureFill = g.Fill as TextureFill;
                    if (textureFill != null)
                    {
                        WriteRawFloat4Packed(rawAtlasTex, 0.0f, ((float)textureFill.Addressing)/255, 0.0f, 0.0f, destX++, 0);
                    }

                    var pos = entry.AtlasLocation.Position;
                    var size = new Vector2(entry.Texture.Width-1, entry.Texture.Height-1);
                    WriteRawInt2Packed(rawAtlasTex, (int)pos.x, (int)pos.y, destX++, 0);
                    WriteRawInt2Packed(rawAtlasTex, (int)size.x, (int)size.y, destX++, 0);
                }
                else
                {
                    g.UVs = new Vector2[vertsCount];
                    for (i = 0; i < vertsCount; i++)
                        g.UVs[i] = whiteTexelsPos;
                }
            }

            var atlasTex = new Texture2D(atlasWidth, atlasHeight, TextureFormat.ARGB32, false, true);
            atlasTex.wrapModeU = TextureWrapMode.Clamp;
            atlasTex.wrapModeV = TextureWrapMode.Clamp;
            atlasTex.wrapModeW = TextureWrapMode.Clamp;
            atlasTex.SetPixels32(atlasColors);
            atlasTex.Apply();

            UnityEngine.Profiling.Profiler.EndSample();

            return new TextureAtlas() { Texture = atlasTex, Entries = pack };
        }

        /// <summary>Fill the UVs of the geometry using the provided texture atlas.</summary>
        /// <param name="geoms">The geometry that will have its UVs filled</param>
        /// <param name="texAtlas">The texture atlas used for the UV generation</param>
        public static void FillUVs(IEnumerable<Geometry> geoms, TextureAtlas texAtlas)
        {
            UnityEngine.Profiling.Profiler.BeginSample("FillUVs");

            var fills = new Dictionary<IFill, PackRectItem>();
            foreach (var entry in texAtlas.Entries)
            {
                if (entry.Fill != null)
                    fills[entry.Fill] = entry;
            }

            var item = new PackRectItem();
            foreach (var g in geoms)
            {
                int settingIndex = 0;
                if ((g.Fill != null) && fills.TryGetValue(g.Fill, out item))
                    settingIndex = item.SettingIndex;

                g.UVs = GenerateShapeUVs(g.Vertices, g.UnclippedBounds, g.FillTransform);
                g.SettingIndex = settingIndex;
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}
