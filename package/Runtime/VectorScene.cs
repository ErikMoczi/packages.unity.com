using System.Collections.Generic;
using UnityEngine;

namespace Unity.VectorGraphics
{
    /// <summary>The gradient fill types.</summary>
    public enum GradientFillType
    {
        /// <summary>A linear gradient.</summary>
        Linear,

        /// <summary>A radial gradient, centered at the radial focus of the gradient fill.</summary>
        Radial
    }

    /// <summary>The path corner types, for joining path segments together.</summary>
    public enum PathCorner
    {
        /// <summary>A tipped corner with a sharp edge.</summary>
        Tipped,

        /// <summary>A rounded corner.</summary>
        Round,

        /// <summary>A beveled corner.</summary>
        Beveled
    }

    /// <summary>The path ending types.</summary>
    public enum PathEnding
    {
        /// <summary>Chop the path end with shar edges.</summary>
        Chop,

        /// <summary>A square path ending with a small extrusion.</summary>
        Square,

        /// <summary>A rounded path ending.</summary>
        Round
    }

    /// <summary>The fill mode types.</summary>
    public enum FillMode
    {
        /// <summary>Determines the "insideness" of the shape by evaluating the direction of the edges crossed.</summary>
        NonZero,

        /// <summary>Determines the "insideness" of the shape by counting the number of edges crossed.</summary>
        OddEven
    }

    /// <summary>The addressing mode, defining how textures or gradients behave when being addressed outside their unit range.</summary>
    public enum AddressMode
    {
        /// <summary>Textures/gradients are wrapping around with a repeating pattern.</summary>
        Wrap,

        /// <summary>Textures/gradients are clamped on the borders.</summary>
        Clamp,

        /// <summary>Textures/gradients are repeated with a mirroring pattern.</summary>
        Mirror
    }

    /// <summary>The gradient stops used for gradient fills.</summary>
    public struct GradientStop
    {
        /// <summary>The color of the stop.</summary>
        public Color color { get; set; }

        /// <summary>At which percentage this stop applies. Should be between 0 and 1, inclusively.</summary>
        public float stopPercentage { get; set; }
    }

    /// <summary>A bezier segment.</summary>
    /// <remarks>
    /// Cubic Bezier segment starts from P0, flies in tangent to direction from P0 to P1,
    /// then lands in direction from P2 to P3, to finally end exactly at P3.
    /// </remarks>
    public struct BezierSegment
    {
        /// <summary>Origin point of the segment.</summary>
        public Vector2 p0;

        /// <summary>First control point of the segment.</summary>
        public Vector2 p1;

        /// <summary>Second control point of the segment.</summary>
        public Vector2 p2;

        /// <summary>Ending point of the segment.</summary>
        public Vector2 p3;
    }

    /// <summary>A bezier path segment.</summary>
    /// <remarks>
    /// Like BezierSegment but implies connectivity of segments, where segments[0].P3 is actually segments[1].P0
    /// The last point of the path may only fill its P0, as P1 and P2 will be ignored.
    /// </remarks>
    public struct BezierPathSegment
    {
        /// <summary>Origin point of the segment.</summary>
        public Vector2 p0;

        /// <summary>First control point of the segment.</summary>
        public Vector2 p1;

        /// <summary>Second control point of the segment.</summary>
        public Vector2 p2;
    }

    /// <summary>A chain of bezier paths, optionnally closed.</summary>
    public struct BezierContour
    {
        /// <summary>An array of every path segments on the contour.</summary>
        /// <remarks>Closed paths should not add a dedicated closing segment. It is implied by the 'closed' property.</remarks>
        public BezierPathSegment[] segments { get; set; }

        /// <summary>A boolean indicating if the contour should be closed.</summary>
        public bool closed { get; set; }
    }

    /// <summary>The IDrawable interface is implemented by elements that displays something on screen.</summary>
    public interface IDrawable { }

    /// <summary>The IFill interface is implemented by filling techniques (solid, texture or gradient).</summary>
    public interface IFill
    {
        /// <summary>The filling method (non-zero or even-odd) of the fill.</summary>
        FillMode mode { get; set; }
    }

    /// <summary>Fills a shape with a single color.</summary>
    public class SolidFill : IFill
    {
        /// <summary>The color of the fill.</summary>        
        public Color color { get; set; }

        /// <summary>The filling method (non-zero or even-odd) of the fill.</summary>
        public FillMode mode { get; set; }
    }

    /// <summary>Fills a shape with a gradient.</summary>
    /// <remarks>
    /// Size of the fill is always assumed to cover the entire element's bounding box.
    /// Radial fills are centered in the element's bounding box. Its radii are half the bounding box dimensions in each direction.
    /// Linear fills start from the left edge to the right edge of the element's bounding box.
    /// </remarks>
    public class GradientFill : IFill
    {
        /// <summary>The fill type (linear or gradient).</summary>
        public GradientFillType type { get; set; }

        /// <summary>An array of stops defining the gradient colors.</summary>
        public GradientStop[] stops { get; set; }

        /// <summary>The filling method (non-zero or even-odd) of the fill.</summary>
        public FillMode mode { get; set; }

        /// <summary>The tint of the filling</summary>
        public Color tint { get { return m_Tint; } set { m_Tint = value; } }
        private Color m_Tint = Color.white;

        /// <summary>The adressing mode (wrap, clamp or mirror) of this fill.</summary>
        public AddressMode addressing { get; set; }

        /// <summary>A position within the unit circle (-1,1) where 0 falls in the middle of the fill.</summary>
        public Vector2 radialFocus { get; set; }
    }

    /// <summary>Fills a shape with a texture.</summary>
    public class TextureFill : IFill 
    {
        /// <summary>The texture to fill the shape with.</summary>
        public Texture2D texture { get; set; }

        /// <summary>The filling method (non-zero or even-odd) of the fill.</summary>
        public FillMode mode { get; set; }

        /// <summary>The tint of the filling</summary>
        public Color tint { get { return m_Tint; } set { m_Tint = value; } }
        private Color m_Tint = Color.white;

        /// <summary>The adressing mode (wrap, clamp or mirror) of this fill.</summary>
        public AddressMode addressing { get; set; }
    }

    /// <summary>Fills a shape with a pattern.</summary>
    public class PatternFill : IFill
    {
        /// <summary>The filling method (non-zero or even-odd) of the fill.</summary>
        public FillMode mode { get; set; }

        /// <summary>The root node of the pattern</summary>
        public SceneNode pattern { get; set; }

        /// <summary>The rectangle that is repeated</summary>
        public Rect rect { get; set; }
    }

    /// <summary>Defines how strokes should be rendered.</summary>
    public class Stroke
    {
        /// <summary>The stroke color.</summary>
        public Color color { get; set; }

        /// <summary>The stroke half-thickness.</summary>
        public float halfThickness { get; set; }

        /// <summary>The stroke pattern (dashes).</summary>
        /// <remarks>Even entries mark a fill and odd entries mark void</remarks>
        public float[] pattern { get; set; }

        /// <summary>An offset to where the pattern should start.</summary>
        public float patternOffset { get; set; }

        /// <summary>How far the tipped corners may extrude.</summary>
        public float tippedCornerLimit { get; set; }
    }

    /// <summary>Defines properties of paths.</summary>
    public struct PathProperties
    {
        /// <summary>The stroke used to render the path.</summary>
        public Stroke stroke { get; set; }

        /// <summary>How the beginning of the path should be displayed.</summary>
        public PathEnding head { get; set; }

        /// <summary>How the end of the path should be displayed.</summary>
        public PathEnding tail { get; set; }

        /// <summary>How the corners of the path should be displayed.</summary>
        public PathCorner corners { get; set; }
    }

    /// <summary>A path definition.</summary>
    public class Path : IDrawable
    {
        /// <summary>The bezier contour defining the path.</summary>
        public BezierContour contour { get; set; }

        /// <summary>The path properties.</summary>
        public PathProperties pathProps { get; set; }
    }

    /// <summary>Filled shape representation.</summary>
    public abstract class Filled : IDrawable
    {
        /// <summary>The fill used on the shape.</summary>
        public IFill fill { get; set; }

        /// <summary>A transformation specific to the fill.</summary>
        public Matrix2D fillTransform { get { return m_FillTransform; } set { m_FillTransform = value; } }
        private Matrix2D m_FillTransform = Matrix2D.identity;

        /// <summary>The path properties.</summary>
        public PathProperties pathProps { get; set; }
    }

    /// <summary>Defines a rectangle shape, which may be rounded.</summary>
    public class Rectangle : Filled
    {
        /// <summary>The position of the rectangle.</summary>
        /// <remarks>Rectangles are anchored on their top-left corner, not their center.</remarks>
        public Vector2 position { get; set; }

        /// <summary>The size of the rectangle.</summary>        
        public Vector2 size { get; set; }

        /// <summary>The top-left radius of the rectangle.</summary>
        public Vector2 radiusTL { get; set; }

        /// <summary>The top-right radius of the rectangle.</summary>
        public Vector2 radiusTR { get; set; }

        /// <summary>The bottom-left radius of the rectangle.</summary>
        public Vector2 radiusBL { get; set; }

        /// <summary>The bottom-right radius of the rectangle.</summary>
        public Vector2 radiusBR { get; set; }
    }

    /// <summary>A generic filled shape.</summary>
    public class Shape : Filled
    {
        /// <summary>All the contours defining the shape.</summary>
        /// <remarks>Some of these coutours may be holes in the shape, depending on the fill mode used <see cref="FillMode"/>.</remarks>
        public BezierContour[] contours { get; set; }
    }

    /// <summary>A node inside a hierarchy.</summary>
    public class SceneNode
    {
        /// <summary>The list of children nodes.</summary>
        public List<SceneNode> children { get; set; }

        /// <summary>The list drawable elements.</summary>
        public List<IDrawable> drawables { get; set; }

        /// <summary>The transform of the node.</summary>
        public Matrix2D transform { get { return m_Transform; } set { m_Transform = value; } }
        private Matrix2D m_Transform = Matrix2D.identity;

        /// <summary>A clipper hierarchy that will clip this node.</summary>
        public SceneNode clipper { get; set; }
    }

    /// <summary>A scene contains the whole node hierarchy.</summary>
    public class Scene
    {
        /// <summary>The root of the node hierarchy.</summary>
        public SceneNode root { get; set; }
    }
} // namespace
