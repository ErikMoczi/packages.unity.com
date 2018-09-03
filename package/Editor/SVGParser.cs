using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Linq;
using UnityEngine;

namespace Unity.VectorGraphics.Editor
{
    /// <summary>Reads an SVG document and builds a vector scene.</summary>
    public class SVGParser
    {
        /// <summary>A structure containing the SVG scene data.</summary>
        public struct SceneInfo
        {
            internal SceneInfo(Scene scene, Dictionary<SceneNode, float> nodeOpacities) { this.scene = scene; this.nodeOpacity = nodeOpacities; }
        
            /// <summary>The vector scene.</summary>
            public Scene scene { get; }
    
            /// <summary>A dictionary containing the opacity of the scene nodes.</summary>
            public Dictionary<SceneNode, float> nodeOpacity { get; }
        }

        /// <summary>Kicks off an SVG file import.</summary>
        /// <param name="fullPath">The full pathname of the SVG file</param>
        /// <param name="dpi">The DPI of the SVG file, or 0 to use the device's DPI</param>
        /// <param name="pixelsPerUnit">How many SVG units fit in a Unity unit</param>
        /// <param name="windowWidth">The default with of the viewport, may be 0</param>
        /// <param name="windowHeight">The default height of the viewport, may be 0</param>
        /// <returns>A SceneInfo object containing the scene data</returns>
        public static SceneInfo ImportSVG(string fullPath, float dpi = 0.0f, float pixelsPerUnit = 1.0f, int windowWidth = 0, int windowHeight = 0)
        {
            var scene = new Scene();
            var settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;

            // Validation and resolving can reach through HTTP to fetch and validate against schemas/DTDs, which could take ages
            //settings.DtdProcessing = DtdProcessing.Ignore;
            settings.ProhibitDtd = false;
            settings.ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.None;
            settings.ValidationType = ValidationType.None;
            settings.XmlResolver = null;

            if (dpi == 0.0f)
                dpi = Screen.dpi;

            Dictionary<SceneNode, float> nodeOpacities;
            using (var reader = XmlReader.Create(fullPath, settings))
            {
                var doc = new SVGDocument(reader, dpi, scene, windowWidth, windowHeight);
                doc.Import();
                nodeOpacities = doc.NodeOpacities;
            }

            float scale = 1.0f / pixelsPerUnit;
            if ((scale != 1.0f) && (scene != null) && (scene.root != null))
                scene.root.transform = scene.root.transform * Matrix2D.Scale(new Vector2(scale, scale));
            return new SceneInfo(scene, nodeOpacities);
        }
    }

    internal class XmlReaderIterator
    {
        internal class Node
        {
            public Node(XmlReader reader) { this.reader = reader; name = reader.Name; depth = reader.Depth; }
            public string Name { get { return name; } }
            public string this[string attrib] { get { return reader.GetAttribute(attrib); } }
            public SVGPropertySheet GetAttributes()
            {
                var atts = new SVGPropertySheet();
                for (int i = 0; i < reader.AttributeCount; ++i)
                {
                    reader.MoveToAttribute(i);
                    atts[reader.Name] = reader.Value;
                }
                reader.MoveToElement();
                return atts;
            }
            public SVGFormatException GetException(string message) { return new SVGFormatException(reader, message); }
            public SVGFormatException GetUnsupportedAttribValException(string attrib)
            {
                return new SVGFormatException(reader, "Value '" + this[attrib] + "' is invalid for attribute '" + attrib + "'");
            }

            public int Depth { get { return depth; } }
            XmlReader reader;
            int depth;
            string name;
        }

        public XmlReaderIterator(XmlReader reader) { this.reader = reader; }
        public bool GoToRoot(string tagName) { return reader.ReadToFollowing(tagName) && reader.Depth == 0; }
        public Node VisitCurrent() { currentElementVisited = true; return new Node(reader); }
        public bool IsEmptyElement() { return reader.IsEmptyElement; }

        public bool GoToNextChild(Node node)
        {
            if (!currentElementVisited)
                return reader.Depth == node.Depth + 1;

            reader.Read();
            while ((reader.NodeType != XmlNodeType.None) && (reader.NodeType != XmlNodeType.Element))
                reader.Read();
            if (reader.NodeType != XmlNodeType.Element)
                return false;

            currentElementVisited = false;
            return reader.Depth == node.Depth + 1;
        }

        public void SkipCurrentChildTree(Node node)
        {
            while (GoToNextChild(node))
                SkipCurrentChildTree(VisitCurrent());
        }

        public string ReadTextWithinElement()
        {
            if (reader.IsEmptyElement)
                return "";
            
            var text = "";
            while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
                text += reader.Value;

            return text;
        }

        XmlReader reader;
        bool currentElementVisited;
    }

    internal class SVGFormatException : Exception
    {
        public SVGFormatException() {}
        public SVGFormatException(string message) : base(ComposeMessage(null, message)) {}
        public SVGFormatException(XmlReader reader, string message) : base(ComposeMessage(reader, message)) {}

        public static SVGFormatException StackError { get { return new SVGFormatException("Vector scene construction mismatch"); } }

        static string ComposeMessage(XmlReader reader, string message)
        {
            IXmlLineInfo li = reader as IXmlLineInfo;
            if (li != null)
                return "SVG Error (line " + li.LineNumber + ", character " + li.LinePosition + "): " + message;
            return "SVG Error: " + message;
        }
    }

    internal class SVGDictionary : Dictionary<string, object> {}

    internal class SVGDocument
    {
        public SVGDocument(XmlReader docReader, float dpi, Scene scene, int windowWidth, int windowHeight)
        {
            allElems = new ElemHandler[]
            { circle, defs, ellipse, g, image, line, linearGradient, path, polygon, polyline, radialGradient, clipPath, pattern, mask, rect, symbol, use, style };

            // These elements excluded below should not be immediatelly part of the hierarchy and can only be referenced
            elemsToAddToHierarchy = new HashSet<ElemHandler>(new ElemHandler[]
                    { circle, /*defs,*/ ellipse, g, image, line, path, polygon, polyline, rect, /*symbol,*/ svg, use });

            this.docReader = new XmlReaderIterator(docReader);
            this.scene = scene;
            this.dpiScale = dpi / 90.0f; // SVG specs assume 90DPI but this machine might use something else
            this.windowWidth = windowWidth;
            this.windowHeight = windowHeight;
            this.svgObjects[StockBlackNonZeroFillName] = new SolidFill() { color = new Color(0, 0, 0), mode = FillMode.NonZero };
            this.svgObjects[StockBlackOddEvenFillName] = new SolidFill() { color = new Color(0, 0, 0), mode = FillMode.OddEven };
        }

        public void Import()
        {
            if (scene == null) throw new ArgumentNullException();
            if (!docReader.GoToRoot("svg"))
                throw new SVGFormatException("Document doesn't have 'svg' root");
            svg();

            PostProcess();
        }

        public Dictionary<SceneNode, float> NodeOpacities { get { return nodeOpacity; } }

        internal const float SVGLengthFactor = 1.41421356f; // Used when calculating relative lengths. See http://www.w3.org/TR/SVG/coords.html#Units
        static internal string StockBlackNonZeroFillName { get { return "unity_internal_black_nz"; } }
        static internal string StockBlackOddEvenFillName { get { return "unity_internal_black_oe"; } }

        void ParseChildren(XmlReaderIterator.Node node, string nodeName)
        {
            var sceneNode = currentSceneNode.Peek();

            var supportedChildren = subTags[nodeName];
            while (docReader.GoToNextChild(node))
            {
                var child = docReader.VisitCurrent();

                ElemHandler handler;
                if (!supportedChildren.TryGetValue(child.Name, out handler))
                {
                    System.Diagnostics.Debug.WriteLine("Skipping over unsupported child (" + child.Name + ") of a (" + node.Name + ")");
                    docReader.SkipCurrentChildTree(child);
                    continue;
                }

                bool addToSceneHierarchy = elemsToAddToHierarchy.Contains(handler);
                SceneNode childVectorNode = null;
                if (addToSceneHierarchy)
                {
                    if (sceneNode.children == null)
                        sceneNode.children = new List<SceneNode>();
                    childVectorNode = new SceneNode();
                    nodeGlobalSceneState[childVectorNode] = new NodeGlobalSceneState() { containerSize = currentContainerSize.Peek() };
                    sceneNode.children.Add(childVectorNode);
                    currentSceneNode.Push(childVectorNode);
                }

                styleResolver.PushNode(child);

                handler();
                ParseChildren(child, child.Name); // Recurse

                styleResolver.PopNode();

                if (addToSceneHierarchy && currentSceneNode.Pop() != childVectorNode)
                    throw SVGFormatException.StackError;
            }
        }

        #region Tag handling
        void circle()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseOpacity(sceneNode);
            sceneNode.transform = SVGAttribParser.ParseTransform(node);
            var fill = SVGAttribParser.ParseFill(node, svgObjects, styleResolver);
            PathCorner strokeCorner;
            PathEnding strokeEnding;
            var stroke = ParseStrokeAttributeSet(node, out strokeCorner, out strokeEnding);

            float cx = AttribLengthVal(node, "cx", 0.0f, DimType.Width);
            float cy = AttribLengthVal(node, "cy", 0.0f, DimType.Height);
            float r = AttribLengthVal(node, "r", 0.0f, DimType.Length);

            var circle = VectorUtils.MakeCircle(new Vector2(cx, cy), r);
            circle.pathProps = new PathProperties() { stroke = stroke, head = strokeEnding, tail = strokeEnding, corners = strokeCorner };
            circle.fill = fill;

            sceneNode.drawables = new List<IDrawable>(1);
            sceneNode.drawables.Add(circle);

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void defs()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = new SceneNode(); // A new scene node instead of one precreated for us
            ParseOpacity(sceneNode);
            sceneNode.transform = SVGAttribParser.ParseTransform(node);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);

            currentSceneNode.Push(sceneNode);
            ParseChildren(node, node.Name);
            if (currentSceneNode.Pop() != sceneNode)
                throw SVGFormatException.StackError;
        }

        void ellipse()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseOpacity(sceneNode);
            sceneNode.transform = SVGAttribParser.ParseTransform(node);
            var fill = SVGAttribParser.ParseFill(node, svgObjects, styleResolver);
            PathCorner strokeCorner;
            PathEnding strokeEnding;
            var stroke = ParseStrokeAttributeSet(node, out strokeCorner, out strokeEnding);

            float cx = AttribLengthVal(node, "cx", 0.0f, DimType.Width);
            float cy = AttribLengthVal(node, "cy", 0.0f, DimType.Height);
            float rx = AttribLengthVal(node, "rx", 0.0f, DimType.Length);
            float ry = AttribLengthVal(node, "ry", 0.0f, DimType.Length);

            var ellipse = VectorUtils.MakeEllipse(new Vector2(cx, cy), rx, ry);
            ellipse.pathProps = new PathProperties() { stroke = stroke, corners = strokeCorner, head = strokeEnding, tail = strokeEnding };
            ellipse.fill = fill;

            sceneNode.drawables = new List<IDrawable>(1);
            sceneNode.drawables.Add(ellipse);

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void g()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseOpacity(sceneNode);
            sceneNode.transform = SVGAttribParser.ParseTransform(node);

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);
        }

        void image()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            // Try to get the referenced image first, if we fail, we just ignore the whole thing
            var url = node["xlink:href"];
            if (url != null)
            {
                var textureFill = new TextureFill();
                textureFill.mode = FillMode.NonZero;
                textureFill.addressing = AddressMode.Clamp;

                if (url.StartsWith("data:"))
                {
                    textureFill.texture = DecodeTextureData(url);
                }
                else
                {
                    if (!url.Contains("://"))
                        url = "file://Assets/" + url;
                    using (WWW www = new WWW(url))
                    {
                        while (www.keepWaiting)
                            System.Threading.Thread.Sleep(10); // Progress bar please...
                        textureFill.texture = www.texture;
                    }
                }

                if (textureFill.texture == null)
                    return; // Unsupported texture...

                // Fills and strokes don't seem to apply to image despite what the specs say
                // All browsers and editing tools seem to ignore them, so we'll just do as well
                ParseOpacity(sceneNode);
                sceneNode.transform = SVGAttribParser.ParseTransform(node);

                var viewPort = ParseViewport(node, sceneNode, currentContainerSize.Peek());
                sceneNode.transform = sceneNode.transform * Matrix2D.Translate(viewPort.position);
                var viewBoxInfo = new ViewBoxInfo();
                viewBoxInfo.viewBox = new Rect(0, 0, textureFill.texture.width, textureFill.texture.height);
                ParseViewBoxAspectRatio(node, ref viewBoxInfo);
                ApplyViewBox(sceneNode, viewBoxInfo, viewPort);

                var rect = new Rectangle() { fill = textureFill, fillTransform = Matrix2D.identity };
                rect.position = Vector2.zero;
                rect.size = new Vector2(textureFill.texture.width, textureFill.texture.height);
                sceneNode.drawables = new List<IDrawable>(1);
                sceneNode.drawables.Add(rect);

                ParseClipAndMask(node, sceneNode);
            }

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void line()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseOpacity(sceneNode);
            sceneNode.transform = SVGAttribParser.ParseTransform(node);
            PathCorner strokeCorner;
            PathEnding strokeEnding;
            var stroke = ParseStrokeAttributeSet(node, out strokeCorner, out strokeEnding);

            float x1 = AttribLengthVal(node, "x1", 0.0f, DimType.Width);
            float y1 = AttribLengthVal(node, "y1", 0.0f, DimType.Height);
            float x2 = AttribLengthVal(node, "x2", 0.0f, DimType.Width);
            float y2 = AttribLengthVal(node, "y2", 0.0f, DimType.Height);

            var path = new Path();
            path.pathProps = new PathProperties() { stroke = stroke, head = strokeEnding, tail = strokeEnding };
            path.contour = new BezierContour() { segments = VectorUtils.PathSegments(VectorUtils.MakeLine(new Vector2(x1, y1), new Vector2(x2, y2))) };
            sceneNode.drawables = new List<IDrawable>(1);
            sceneNode.drawables.Add(path);

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void linearGradient()
        {
            var node = docReader.VisitCurrent();

            bool relativeToWorld;
            switch (node["gradientUnits"])
            {
                case null:
                case "objectBoundingBox":
                    relativeToWorld = false;
                    break;

                case "userSpaceOnUse":
                    relativeToWorld = true;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("gradientUnits");
            }

            AddressMode addressing;
            switch (node["spreadMethod"])
            {
                case null:
                case "pad":
                    addressing = AddressMode.Clamp;
                    break;

                case "reflect":
                    addressing = AddressMode.Mirror;
                    break;

                case "repeat":
                    addressing = AddressMode.Wrap;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("spreadMethod");
            }

            var gradientTransform = SVGAttribParser.ParseTransform(node, "gradientTransform");

            GradientFill fill = new GradientFill() { addressing = addressing, type = GradientFillType.Linear };
            LinearGradientExData fillExData = new LinearGradientExData() { worldRelative = relativeToWorld, fillTransform = gradientTransform };
            gradientExInfo[fill] = fillExData;

            // Fills are defined outside of a shape scope, so we can't resolve relative coordinates here.
            // We defer this entire operation to AdjustFills pass, but we still do value validation here
            // nonetheless to give meaningful error messages to the user if any.
            currentContainerSize.Push(Vector2.one);

            fillExData.x1 = node["x1"];
            fillExData.y1 = node["y1"];
            fillExData.x2 = node["x2"];
            fillExData.y2 = node["y2"];

            // The calls below are ineffective but they validate the inputs and throw an error if wrong values are specified, so don't remove them
            AttribLengthVal(fillExData.x1, node, "x1", 0.0f, DimType.Width);
            AttribLengthVal(fillExData.y1, node, "y1", 0.0f, DimType.Height);
            AttribLengthVal(fillExData.x2, node, "x2", 1.0f, DimType.Width);
            AttribLengthVal(fillExData.y2, node, "y2", 0.0f, DimType.Height);

            currentContainerSize.Pop();
            currentGradientFill = fill; // Children stops will register to this fill now

            AddToSVGDictionaryIfPossible(node, fill);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, stop);
        }

        void path()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseOpacity(sceneNode);
            sceneNode.transform = SVGAttribParser.ParseTransform(node);
            var fill = SVGAttribParser.ParseFill(node, svgObjects, styleResolver);
            PathCorner strokeCorner;
            PathEnding strokeEnding;
            var stroke = ParseStrokeAttributeSet(node, out strokeCorner, out strokeEnding);
            var pathProps = new PathProperties() { stroke = stroke, corners = strokeCorner, head = strokeEnding, tail = strokeEnding };

            // A path may have 1 or more sub paths. Each for us is an individual vector path.
            var contours = SVGAttribParser.ParsePath(node);
            if ((contours != null) && (contours.Count > 0))
            {
                //float pathLength = AttribFloatVal(node, "pathLength"); // This is useful for animation purposes mostly

                if (fill == null)
                {
                    sceneNode.drawables = new List<IDrawable>(contours.Count);
                    foreach (var contour in contours)
                        sceneNode.drawables.Add(new Path() { contour = contour, pathProps = pathProps });
                }
                else
                {
                    sceneNode.drawables = new List<IDrawable>(1);
                    sceneNode.drawables.Add(new Shape() { contours = contours.ToArray(), fill = fill, pathProps = pathProps });
                }

                AddToSVGDictionaryIfPossible(node, sceneNode);
            }

            ParseClipAndMask(node, sceneNode);

            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void polygon()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseOpacity(sceneNode);
            sceneNode.transform = SVGAttribParser.ParseTransform(node);
            var fill = SVGAttribParser.ParseFill(node, svgObjects, styleResolver);
            PathCorner strokeCorner;
            PathEnding strokeEnding;
            var stroke = ParseStrokeAttributeSet(node, out strokeCorner, out strokeEnding);

            var pointsAttribVal = node["points"];
            var pointsString = (pointsAttribVal != null) ? pointsAttribVal.Split(whiteSpaceNumberChars, StringSplitOptions.RemoveEmptyEntries) : null;
            if (pointsString != null)
            {
                if ((pointsString.Length & 1) == 1)
                    throw node.GetException("polygon 'points' must specify x,y for each coordinate");
                if (pointsString.Length < 4)
                    throw node.GetException("polygon 'points' do not even specify one triangle");

                var pathProps = new PathProperties() { stroke = stroke, head = strokeEnding, tail = strokeEnding };
                var contour = new BezierContour() { segments = new BezierPathSegment[pointsString.Length / 2], closed = true };
                var lastPoint = new Vector2(
                        AttribLengthVal(pointsString[0], node, "points", 0.0f, DimType.Width),
                        AttribLengthVal(pointsString[1], node, "points", 0.0f, DimType.Height));
                for (int i = 1; i < contour.segments.Length; i++)
                {
                    var newPoint = new Vector2(
                            AttribLengthVal(pointsString[i * 2 + 0], node, "points", 0.0f, DimType.Width),
                            AttribLengthVal(pointsString[i * 2 + 1], node, "points", 0.0f, DimType.Height));
                    var seg = VectorUtils.MakeLine(lastPoint, newPoint);
                    contour.segments[i - 1] = new BezierPathSegment() { p0 = seg.p0, p1 = seg.p1, p2 = seg.p2 };
                    lastPoint = newPoint;
                }
                contour.segments[contour.segments.Length - 1] = new BezierPathSegment() { p0 = lastPoint };

                var shape = new Shape() { contours = new BezierContour[] { contour }, pathProps = pathProps, fill = fill };
                sceneNode.drawables = new List<IDrawable>(1);
                sceneNode.drawables.Add(shape);
            }

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void polyline()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseOpacity(sceneNode);
            sceneNode.transform = SVGAttribParser.ParseTransform(node);
            PathCorner strokeCorner;
            PathEnding strokeEnding;
            var stroke = ParseStrokeAttributeSet(node, out strokeCorner, out strokeEnding);

            var pointsAttribVal = node["points"];
            var pointsString = (pointsAttribVal != null) ? pointsAttribVal.Split(whiteSpaceNumberChars, StringSplitOptions.RemoveEmptyEntries) : null;
            if (pointsString != null)
            {
                if ((pointsString.Length & 1) == 1)
                    throw node.GetException("polyline 'points' must specify x,y for each coordinate");
                if (pointsString.Length < 4)
                    throw node.GetException("polyline 'points' do not even specify one line");

                var path = new Path();
                path.pathProps = new PathProperties() { stroke = stroke, head = strokeEnding, tail = strokeEnding };
                path.contour = new BezierContour() { segments = new BezierPathSegment[pointsString.Length / 2] };
                var lastPoint = new Vector2(
                        AttribLengthVal(pointsString[0], node, "points", 0.0f, DimType.Width),
                        AttribLengthVal(pointsString[1], node, "points", 0.0f, DimType.Height));
                for (int i = 1; i < path.contour.segments.Length; i++)
                {
                    var newPoint = new Vector2(
                            AttribLengthVal(pointsString[i * 2 + 0], node, "points", 0.0f, DimType.Width),
                            AttribLengthVal(pointsString[i * 2 + 1], node, "points", 0.0f, DimType.Height));
                    var seg = VectorUtils.MakeLine(lastPoint, newPoint);
                    path.contour.segments[i - 1] = new BezierPathSegment() { p0 = seg.p0, p1 = seg.p1, p2 = seg.p2 };
                    lastPoint = newPoint;
                }
                path.contour.segments[path.contour.segments.Length - 1] = new BezierPathSegment() { p0 = lastPoint };
                sceneNode.drawables = new List<IDrawable>(1);
                sceneNode.drawables.Add(path);
            }

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void radialGradient()
        {
            var node = docReader.VisitCurrent();

            bool relativeToWorld;
            switch (node["gradientUnits"])
            {
                case null:
                case "objectBoundingBox":
                    relativeToWorld = false;
                    break;

                case "userSpaceOnUse":
                    relativeToWorld = true;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("gradientUnits");
            }

            AddressMode addressing;
            switch (node["spreadMethod"])
            {
                case null:
                case "pad":
                    addressing = AddressMode.Clamp;
                    break;

                case "reflect":
                    addressing = AddressMode.Mirror;
                    break;

                case "repeat":
                    addressing = AddressMode.Wrap;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("spreadMethod");
            }

            var gradientTransform = SVGAttribParser.ParseTransform(node, "gradientTransform");

            GradientFill fill = new GradientFill() { addressing = addressing, type = GradientFillType.Radial };
            RadialGradientExData fillExData = new RadialGradientExData() { worldRelative = relativeToWorld, fillTransform = gradientTransform };
            gradientExInfo[fill] = fillExData;

            // Fills are defined outside of a shape scope, so we can't resolve relative coordinates here.
            // We defer this entire operation to AdjustFills pass, but we still do value validation here
            // nonetheless to give meaningful error messages to the user if any.
            currentContainerSize.Push(Vector2.one);

            fillExData.cx = node["cx"];
            fillExData.cy = node["cy"];
            fillExData.fx = node["fx"];
            fillExData.fy = node["fy"];
            fillExData.r = node["r"];

            // The calls below are ineffective but they validate the inputs and throw an error if wrong values are specified, so don't remove them
            AttribLengthVal(fillExData.cx, node, "cx", 0.5f, DimType.Width);
            AttribLengthVal(fillExData.cy, node, "cy", 0.5f, DimType.Height);
            AttribLengthVal(fillExData.fx, node, "fx", 0.5f, DimType.Width);
            AttribLengthVal(fillExData.fy, node, "fy", 0.5f, DimType.Height);
            AttribLengthVal(fillExData.r, node, "r", 0.5f, DimType.Length);

            currentContainerSize.Pop();
            currentGradientFill = fill; // Children stops will register to this fill now

            AddToSVGDictionaryIfPossible(node, fill);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, stop);
        }

        void clipPath()
        {
            var node = docReader.VisitCurrent();

             // A new scene node instead of one precreated for us
            var clipRoot = new SceneNode() {
                transform = Matrix2D.identity
            };

            bool relativeToWorld;
            switch (node["clipPathUnits"])
            {
                case null:
                case "userSpaceOnUse":
                    relativeToWorld = true;
                    break;

                case "objectBoundingBox":
                    relativeToWorld = false;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("clipPathUnits");
            }

            clipData[clipRoot] = new ClipData() { worldRelative = relativeToWorld };

            AddToSVGDictionaryIfPossible(node, clipRoot);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);

            currentSceneNode.Push(clipRoot);
            ParseChildren(node, node.Name);
            if (currentSceneNode.Pop() != clipRoot)
                throw SVGFormatException.StackError;
        }

        void pattern()
        {
            var node = docReader.VisitCurrent();

            // A new scene node instead of one precreated for us
            var patternRoot = new SceneNode() {
                transform = Matrix2D.identity
            };

            bool relativeToWorld;
            switch (node["patternUnits"])
            {
                case null:
                case "userSpaceOnUse":
                    relativeToWorld = true;
                    break;

                case "objectBoundingBox":
                    relativeToWorld = false;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("patternUnits");
            }

            bool contentRelativeToWorld;
            switch (node["patternContentUnits"])
            {
                case null:
                case "userSpaceOnUse":
                    contentRelativeToWorld = true;
                    break;

                case "objectBoundingBox":
                    contentRelativeToWorld = false;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("patternContentUnits");
            }

            var x = AttribLengthVal(node["x"], node, "x", 0.0f, DimType.Width);
            var y = AttribLengthVal(node["y"], node, "y", 0.0f, DimType.Height);
            var w = AttribLengthVal(node["width"], node, "width", 0.0f, DimType.Width);
            var h = AttribLengthVal(node["height"], node, "height", 0.0f, DimType.Height);

            var patternTransform = SVGAttribParser.ParseTransform(node, "patternTransform");

            patternData[patternRoot] = new PatternData() {
                worldRelative = relativeToWorld,
                contentWorldRelative = contentRelativeToWorld,
                patternTransform = patternTransform
            };

            var fill = new PatternFill() { 
                pattern = patternRoot,
                rect = new Rect(x, y, w, h)
            };

            AddToSVGDictionaryIfPossible(node, fill);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);

            currentSceneNode.Push(patternRoot);
            ParseChildren(node, node.Name);
            if (currentSceneNode.Pop() != patternRoot)
                throw SVGFormatException.StackError;
        }

        void mask()
        {
            var node = docReader.VisitCurrent();

            // A new scene node instead of one precreated for us
            var maskRoot = new SceneNode() {
                transform = Matrix2D.identity
            };

            bool relativeToWorld;
            switch (node["maskUnits"])
            {
                case null:
                case "userSpaceOnUse":
                    relativeToWorld = true;
                    break;

                case "objectBoundingBox":
                    relativeToWorld = false;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("maskUnits");
            }

            bool contentRelativeToWorld;
            switch (node["maskContentUnits"])
            {
                case null:
                case "userSpaceOnUse":
                    contentRelativeToWorld = true;
                    break;

                case "objectBoundingBox":
                    contentRelativeToWorld = false;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("maskContentUnits");
            }

            maskData[maskRoot] = new MaskData() {
                worldRelative = relativeToWorld,
                contentWorldRelative = contentRelativeToWorld,
            };

            AddToSVGDictionaryIfPossible(node, maskRoot);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);

            currentSceneNode.Push(maskRoot);
            ParseChildren(node, node.Name);
            if (currentSceneNode.Pop() != maskRoot)
                throw SVGFormatException.StackError;
        }

        void rect()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseOpacity(sceneNode);
            sceneNode.transform = SVGAttribParser.ParseTransform(node);
            var fill = SVGAttribParser.ParseFill(node, svgObjects, styleResolver);
            PathCorner strokeCorner;
            PathEnding strokeEnding;
            var stroke = ParseStrokeAttributeSet(node, out strokeCorner, out strokeEnding);

            float x = AttribLengthVal(node, "x", 0.0f, DimType.Width);
            float y = AttribLengthVal(node, "y", 0.0f, DimType.Height);
            float rx = AttribLengthVal(node, "rx", -1.0f, DimType.Length);
            float ry = AttribLengthVal(node, "ry", -1.0f, DimType.Length);
            float width = AttribLengthVal(node, "width", 0.0f, DimType.Length);
            float height = AttribLengthVal(node, "height", 0.0f, DimType.Length);

            if ((rx < 0.0f) && (ry >= 0.0f))
                rx = ry;
            else if ((ry < 0.0f) && (rx >= 0.0f))
                ry = rx;
            else if ((ry < 0.0f) && (rx < 0.0f))
                rx = ry = 0.0f;
            rx = Mathf.Min(rx, width * 0.5f);
            ry = Mathf.Min(ry, height * 0.5f);

            var rect = new Rectangle() { fill = fill };
            rect.pathProps = new PathProperties() { stroke = stroke, head = strokeEnding, tail = strokeEnding, corners = strokeCorner };
            rect.position = new Vector2(x, y);
            rect.size = new Vector2(width, height);
            rect.radiusTL = rect.radiusTR = rect.radiusBL = rect.radiusBR = new Vector2(rx, ry);
            sceneNode.drawables = new List<IDrawable>(1);
            sceneNode.drawables.Add(rect);

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void stop()
        {
            var node = docReader.VisitCurrent();
            System.Diagnostics.Debug.Assert(currentGradientFill != null);

            GradientStop stop = new GradientStop();

            string stopColor = styleResolver.Evaluate("stop-color");
            Color color = stopColor != null ? SVGAttribParser.ParseColor(stopColor) : Color.black;

            color.a = AttribFloatVal("stop-opacity", 1.0f);
            stop.color = color;

            string offsetString = styleResolver.Evaluate("offset");
            if (!string.IsNullOrEmpty(offsetString))
            {
                bool percentage = offsetString.EndsWith("%");
                if (percentage)
                    offsetString = offsetString.Substring(0, offsetString.Length - 1);
                stop.stopPercentage = float.Parse(offsetString);
                if (percentage)
                    stop.stopPercentage /= 100.0f;

                stop.stopPercentage = Mathf.Max(0.0f, stop.stopPercentage);
                stop.stopPercentage = Mathf.Min(1.0f, stop.stopPercentage);
            }

            // I don't like this, but hopefully there aren't many stops in a gradient
            GradientStop[] newStops;
            if (currentGradientFill.stops == null || currentGradientFill.stops.Length == 0)
                newStops = new GradientStop[1];
            else
            {
                newStops = new GradientStop[currentGradientFill.stops.Length + 1];
                currentGradientFill.stops.CopyTo(newStops, 0);
            }
            newStops[newStops.Length - 1] = stop;
            currentGradientFill.stops = newStops;

            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void svg()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = new SceneNode();
            if (scene.root == null) // If this is the root SVG element, then we set the vector scene root as well
            {
                System.Diagnostics.Debug.Assert(currentSceneNode.Count == 0);
                scene.root = sceneNode;
            }

            styleResolver.PushNode(node);

            ParseOpacity(sceneNode);
            sceneNode.transform = SVGAttribParser.ParseTransform(node);

            var sceneViewport = ParseViewport(node, sceneNode, new Vector2(windowWidth, windowHeight));
            ApplyViewBox(sceneNode, ParseViewBox(node, sceneNode, sceneViewport), sceneViewport);

            currentContainerSize.Push(sceneViewport.size);
            currentSceneNode.Push(sceneNode);
            nodeGlobalSceneState[sceneNode] = new NodeGlobalSceneState() { containerSize = currentContainerSize.Peek() };

            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);
            ParseChildren(node, "svg");

            currentContainerSize.Pop();
            if (currentSceneNode.Pop() != sceneNode)
                throw SVGFormatException.StackError;
            
            styleResolver.PopNode();
        }

        void symbol()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = new SceneNode(); // A new scene node instead of one precreated for us
            ParseOpacity(sceneNode);
            sceneNode.transform = Matrix2D.identity;

            Rect viewportRect = new Rect(Vector2.zero, currentContainerSize.Peek());
            var viewBoxInfo = ParseViewBox(node, sceneNode, viewportRect);
            symbolViewBoxes[sceneNode] = viewBoxInfo;

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);

            currentSceneNode.Push(sceneNode);
            ParseChildren(node, node.Name);
            if (currentSceneNode.Pop() != sceneNode)
                throw SVGFormatException.StackError;

            ParseClipAndMask(node, sceneNode);
        }

        void use()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();
            ParseOpacity(sceneNode);

            var referencedNode = SVGAttribParser.ParseRelativeRef(node["xlink:href"], svgObjects) as SceneNode;
            if (referencedNode == null)
                throw node.GetException("Referencing non-existent element (" + node["xlink:href"] + ")");

            sceneNode.transform = SVGAttribParser.ParseTransform(node);
            var sceneViewport = ParseViewport(node, sceneNode, Vector2.zero);
            sceneNode.transform = sceneNode.transform * Matrix2D.Translate(sceneViewport.position);

            // Note we don't use the viewport size because the <use> element doesn't establish a viewport for its referenced elements
            ViewBoxInfo viewBoxInfo;
            if (symbolViewBoxes.TryGetValue(referencedNode, out viewBoxInfo))
                ApplyViewBox(sceneNode, viewBoxInfo, sceneViewport); // When using a symbol we need to apply the symbol's view box

            if (sceneNode.children == null)
                sceneNode.children = new List<SceneNode>();
            sceneNode.children.Add(referencedNode);

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void style()
        {
            var node = docReader.VisitCurrent();
            var text = docReader.ReadTextWithinElement();

            if (text.Length > 0)
                styleResolver.PushStyleSheet(SVGStyleSheetUtils.Parse(text), true);

            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }
        #endregion

        #region Simple Attribute Handling
        int AttribIntVal(string attribName) { return AttribIntVal(attribName, 0); }
        int AttribIntVal(string attribName, int defaultVal)
        {
            string val = styleResolver.Evaluate(attribName);
            return (val != null) ? int.Parse(val) : defaultVal;
        }

        float AttribFloatVal(string attribName) { return AttribFloatVal(attribName, 0.0f); }
        float AttribFloatVal(string attribName, float defaultVal)
        {
            string val = styleResolver.Evaluate(attribName);
            return (val != null) ? float.Parse(val) : defaultVal;
        }

        float AttribLengthVal(XmlReaderIterator.Node node, string attribName, DimType dimType) { return AttribLengthVal(node, attribName, 0.0f, dimType); }
        float AttribLengthVal(XmlReaderIterator.Node node, string attribName, float defaultUnitVal, DimType dimType)
        {
            var val = styleResolver.Evaluate(attribName);
            return AttribLengthVal(val, node, attribName, defaultUnitVal, dimType);
        }

        float AttribLengthVal(string val, XmlReaderIterator.Node node, string attribName, float defaultUnitVal, DimType dimType)
        {
            // For reference: http://www.w3.org/TR/SVG/coords.html#Units
            if (val == null) return defaultUnitVal;
            val = val.Trim();
            string unitType = "px";
            char lastChar = val[val.Length - 1];
            if (lastChar == '%')
            {
                float number = float.Parse(val.Substring(0, val.Length - 1));
                if (number < 0)
                    throw node.GetException("Number in " + attribName + " cannot be negative");
                number /= 100.0f;
                var vpSize = currentContainerSize.Peek();
                switch (dimType)
                {
                    case DimType.Width: return number * vpSize.x;
                    case DimType.Height: return number * vpSize.y;
                    case DimType.Length: return (number * vpSize.magnitude / SVGLengthFactor); // See http://www.w3.org/TR/SVG/coords.html#Units
                }
            }
            else if (val.Length >= 2)
            {
                unitType = val.Substring(val.Length - 2);
            }

            if (char.IsDigit(lastChar) || (lastChar == '.'))
                return float.Parse(val); // No unit specified.. assume pixels (one px unit is defined to be equal to one user unit)

            float length = float.Parse(val.Substring(0, val.Length - 2));
            switch (unitType)
            {
                case "em": throw new NotImplementedException();
                case "ex": throw new NotImplementedException();
                case "px": return length;
                case "in": return 90.0f * length * dpiScale;       // "1in" equals "90px" (and therefore 90 user units)
                case "cm": return 35.43307f * length * dpiScale;   // "1cm" equals "35.43307px" (and therefore 35.43307 user units)
                case "mm": return 3.543307f * length * dpiScale;   // "1mm" would be "3.543307px" (3.543307 user units)
                case "pt": return 1.25f * length * dpiScale;       // "1pt" equals "1.25px" (and therefore 1.25 user units)
                case "pc": return 15.0f * length * dpiScale;       // "1pc" equals "15px" (and therefore 15 user units)
                default:
                    throw new FormatException("Unknown length unit type (" + unitType + ")");
            }
        }

        #endregion

        #region Attribute Set Handling
        void AddToSVGDictionaryIfPossible(XmlReaderIterator.Node node, object vectorElement)
        {
            string id = node["id"];
            if (!string.IsNullOrEmpty(id))
                svgObjects[id] = vectorElement;
        }

        Rect ParseViewport(XmlReaderIterator.Node node, SceneNode sceneNode, Vector2 defaultViewportSize)
        {
            scenePos.x = AttribLengthVal(node, "x", DimType.Width);
            scenePos.y = AttribLengthVal(node, "y", DimType.Height);
            sceneSize.x = AttribLengthVal(node, "width", defaultViewportSize.x, DimType.Width);
            sceneSize.y = AttribLengthVal(node, "height", defaultViewportSize.y, DimType.Height);

            // The size could be all 0, in which case we should ignore the viewport sizing logic altogether
            return new Rect(scenePos, sceneSize);
        }

        enum ViewBoxAlign { Min, Mid, Max }
        enum ViewBoxAspectRatio { DontPreserve, FitLargestDim, FitSmallestDim }
        struct ViewBoxInfo { public Rect viewBox; public ViewBoxAspectRatio aspectRatio; public ViewBoxAlign alignX, alignY; }
        ViewBoxInfo ParseViewBox(XmlReaderIterator.Node node, SceneNode sceneNode, Rect sceneViewport)
        {
            var viewBoxInfo = new ViewBoxInfo();
            string viewBoxString = node["viewBox"];
            viewBoxString = viewBoxString != null ? viewBoxString.Trim() : null;
            if (string.IsNullOrEmpty(viewBoxString))
                return viewBoxInfo;

            var viewBoxValues = viewBoxString.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (viewBoxValues.Length != 4)
                throw node.GetException("Invalid viewBox specification");
            Vector2 viewBoxMin = new Vector2(
                    AttribLengthVal(viewBoxValues[0], node, "viewBox", 0.0f, DimType.Width),
                    AttribLengthVal(viewBoxValues[1], node, "viewBox", 0.0f, DimType.Height));
            Vector2 viewBoxSize = new Vector2(
                    AttribLengthVal(viewBoxValues[2], node, "viewBox", sceneViewport.width, DimType.Width),
                    AttribLengthVal(viewBoxValues[3], node, "viewBox", sceneViewport.height, DimType.Height));

            viewBoxInfo.viewBox = new Rect(viewBoxMin, viewBoxSize);
            ParseViewBoxAspectRatio(node, ref viewBoxInfo);

            return viewBoxInfo;
        }

        void ParseViewBoxAspectRatio(XmlReaderIterator.Node node, ref ViewBoxInfo viewBoxInfo)
        {
            viewBoxInfo.aspectRatio = ViewBoxAspectRatio.FitLargestDim;
            viewBoxInfo.alignX = ViewBoxAlign.Mid;
            viewBoxInfo.alignY = ViewBoxAlign.Mid;

            string preserveAspectRatioString = node["preserveAspectRatio"];
            preserveAspectRatioString = preserveAspectRatioString != null ? preserveAspectRatioString.Trim() : null;
            bool wantNone = false;
            if (!string.IsNullOrEmpty(preserveAspectRatioString))
            {
                var preserveAspectRatioValues = preserveAspectRatioString.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var value in preserveAspectRatioValues)
                {
                    switch (value)
                    {
                        case "defer": break; // This is only meaningful on <image> that references another SVG, we don't support that
                        case "none": wantNone = true; break;
                        case "xMinYMin": viewBoxInfo.alignX = ViewBoxAlign.Min; viewBoxInfo.alignY = ViewBoxAlign.Min; break;
                        case "xMidYMin": viewBoxInfo.alignX = ViewBoxAlign.Mid; viewBoxInfo.alignY = ViewBoxAlign.Min; break;
                        case "xMaxYMin": viewBoxInfo.alignX = ViewBoxAlign.Max; viewBoxInfo.alignY = ViewBoxAlign.Min; break;
                        case "xMinYMid": viewBoxInfo.alignX = ViewBoxAlign.Min; viewBoxInfo.alignY = ViewBoxAlign.Mid; break;
                        case "xMidYMid": viewBoxInfo.alignX = ViewBoxAlign.Mid; viewBoxInfo.alignY = ViewBoxAlign.Mid; break;
                        case "xMaxYMid": viewBoxInfo.alignX = ViewBoxAlign.Max; viewBoxInfo.alignY = ViewBoxAlign.Mid; break;
                        case "xMinYMax": viewBoxInfo.alignX = ViewBoxAlign.Min; viewBoxInfo.alignY = ViewBoxAlign.Max; break;
                        case "xMidYMax": viewBoxInfo.alignX = ViewBoxAlign.Mid; viewBoxInfo.alignY = ViewBoxAlign.Max; break;
                        case "xMaxYMax": viewBoxInfo.alignX = ViewBoxAlign.Max; viewBoxInfo.alignY = ViewBoxAlign.Max; break;
                        case "meet": viewBoxInfo.aspectRatio = ViewBoxAspectRatio.FitLargestDim; break;
                        case "slice": viewBoxInfo.aspectRatio = ViewBoxAspectRatio.FitSmallestDim; break;
                    }
                }
            }

            if (wantNone) // Override aspect ratio no matter what other modes are chosen (meet/slice)
                viewBoxInfo.aspectRatio = ViewBoxAspectRatio.DontPreserve;
        }

        void ApplyViewBox(SceneNode sceneNode, ViewBoxInfo viewBoxInfo, Rect sceneViewport)
        {
            if ((viewBoxInfo.viewBox.size == Vector2.zero) || (sceneViewport.size == Vector2.zero))
                return;

            Vector2 scale = Vector2.one, offset = -viewBoxInfo.viewBox.position;
            if (viewBoxInfo.aspectRatio == ViewBoxAspectRatio.DontPreserve)
            {
                scale = sceneViewport.size / viewBoxInfo.viewBox.size;
            }
            else
            {
                scale.x = scale.y = sceneViewport.width / viewBoxInfo.viewBox.width;
                bool fitsOnWidth;
                if (viewBoxInfo.aspectRatio == ViewBoxAspectRatio.FitLargestDim)
                    fitsOnWidth = viewBoxInfo.viewBox.height * scale.y <= sceneViewport.height;
                else fitsOnWidth = viewBoxInfo.viewBox.height * scale.y > sceneViewport.height;

                Vector2 alignOffset = Vector2.zero;
                if (fitsOnWidth)
                {
                    // We fit on the width, so apply the vertical alignment rules
                    if (viewBoxInfo.alignY == ViewBoxAlign.Mid)
                        alignOffset.y = (sceneViewport.height - viewBoxInfo.viewBox.height * scale.y) * 0.5f;
                    else if (viewBoxInfo.alignY == ViewBoxAlign.Max)
                        alignOffset.y = sceneViewport.height - viewBoxInfo.viewBox.height * scale.y;
                }
                else
                {
                    // We didn't fit on width, meaning we should fit on height and use the wiggle room on width
                    scale.x = scale.y = sceneViewport.height / viewBoxInfo.viewBox.height;

                    // Apply the horizontal alignment rules
                    if (viewBoxInfo.alignX == ViewBoxAlign.Mid)
                        alignOffset.x = (sceneViewport.width - viewBoxInfo.viewBox.width * scale.x) * 0.5f;
                    else if (viewBoxInfo.alignX == ViewBoxAlign.Max)
                        alignOffset.x = sceneViewport.width - viewBoxInfo.viewBox.width * scale.x;
                }

                offset += alignOffset / scale;
            }

            // Aaaaand finally, the transform
            sceneNode.transform = sceneNode.transform * Matrix2D.Scale(scale) * Matrix2D.Translate(offset);
        }

        Stroke ParseStrokeAttributeSet(XmlReaderIterator.Node node, out PathCorner strokeCorner, out PathEnding strokeEnding)
        {
            var stroke = SVGAttribParser.ParseStrokeAndOpacity(node, svgObjects, styleResolver);
            strokeCorner = PathCorner.Tipped;
            strokeEnding = PathEnding.Chop;
            if (stroke != null)
            {
                string strokeWidth = styleResolver.Evaluate("stroke-width", SVGResolveLimit.Hierarchy);
                stroke.halfThickness = AttribLengthVal(strokeWidth, node, "stroke-width", 1.0f, DimType.Length) * 0.5f;
                switch (styleResolver.Evaluate("stroke-linecap", SVGResolveLimit.Hierarchy))
                {
                    case "butt": strokeEnding = PathEnding.Chop; break;
                    case "square": strokeEnding = PathEnding.Square; break;
                    case "round": strokeEnding = PathEnding.Round; break;
                }
                switch (styleResolver.Evaluate("stroke-linejoin", SVGResolveLimit.Hierarchy))
                {
                    case "miter": strokeCorner = PathCorner.Tipped; break;
                    case "round": strokeCorner = PathCorner.Round; break;
                    case "bevel": strokeCorner = PathCorner.Beveled; break;
                }

                string pattern = styleResolver.Evaluate("stroke-dasharray", SVGResolveLimit.Hierarchy);
                if (pattern != null)
                {
                    string[] entries = pattern.Split(whiteSpaceNumberChars, StringSplitOptions.RemoveEmptyEntries);
                    // If the pattern is odd, then we duplicate it to make it even as per the spec
                    int totalCount = (entries.Length & 1) == 1 ? entries.Length * 2 : entries.Length;
                    stroke.pattern = new float[totalCount];
                    for (int i = 0; i < entries.Length; i++)
                        stroke.pattern[i] = AttribLengthVal(entries[i], node, "stroke-dasharray", 0.0f, DimType.Length);

                    // Duplicate the pattern
                    if (totalCount > entries.Length)
                    {
                        for (int i = 0; i < entries.Length; i++)
                            stroke.pattern[i + entries.Length] = stroke.pattern[i];
                    }

                    var dashOffset = styleResolver.Evaluate("stroke-dashoffset", SVGResolveLimit.Hierarchy);
                    stroke.patternOffset = AttribLengthVal(dashOffset, node, "stroke-dashoffset", 0.0f, DimType.Length);
                }

                var strokeMiterLimit = styleResolver.Evaluate("stroke-miterlimit", SVGResolveLimit.Hierarchy);
                stroke.tippedCornerLimit = AttribLengthVal(strokeMiterLimit, node, "stroke-miterlimit", 4.0f, DimType.Length);
                if (stroke.tippedCornerLimit < 1.0f)
                    throw node.GetException("'stroke-miterlimit' should be greater or equal to 1");
            } // If stroke is specified
            return stroke;
        }

        float ParseOpacity(SceneNode sceneNode)
        {
            float opacity = AttribFloatVal("opacity", 1.0f);
            if (opacity != 1.0f && sceneNode != null)
                nodeOpacity[sceneNode] = opacity;
            return opacity;
        }

        void ParseClipAndMask(XmlReaderIterator.Node node, SceneNode sceneNode)
        {
            ParseClip(node, sceneNode);
            ParseMask(node, sceneNode);
        }

        void ParseClip(XmlReaderIterator.Node node, SceneNode sceneNode)
        {
            string reference = null;
            string clipPath = node["clip-path"];
            if (clipPath != null)
                reference = SVGAttribParser.ParseURLRef(clipPath);

            if (reference == null)
                return;

            var clipper = SVGAttribParser.ParseRelativeRef(reference, svgObjects) as SceneNode;
            var clipperRoot = clipper;

            ClipData data;
            if (clipData.TryGetValue(clipper, out data) && !data.worldRelative)
            {
                // If the referenced clip path units is in bounding-box space, we add an intermediate
                // node to scale the content to the correct size.
                var rect = VectorUtils.SceneNodeBounds(sceneNode);
                var transform = Matrix2D.Translate(rect.position) * Matrix2D.Scale(rect.size);

                clipperRoot = new SceneNode() {
                    children = new List<SceneNode> { clipper },
                    transform = transform
                };
            }

            sceneNode.clipper = clipperRoot;
        }

        void ParseMask(XmlReaderIterator.Node node, SceneNode sceneNode)
        {
            string reference = null;
            string maskRef = node["mask"];
            if (maskRef != null)
                reference = SVGAttribParser.ParseURLRef(maskRef);

            if (reference == null)
                return;

            var maskPath = SVGAttribParser.ParseRelativeRef(reference, svgObjects) as SceneNode;
            var maskRoot = maskPath;

            MaskData data;
            if (maskData.TryGetValue(maskPath, out data) && !data.contentWorldRelative)
            {
                // If the referenced mask units is in bounding-box space, we add an intermediate
                // node to scale the content to the correct size.
                var rect = VectorUtils.SceneNodeBounds(sceneNode);
                var transform = Matrix2D.Translate(rect.position) * Matrix2D.Scale(rect.size);

                maskRoot = new SceneNode() {
                    children = new List<SceneNode> { maskPath },
                    transform = transform
                };
            }

            sceneNode.clipper = maskRoot;
        }

        #endregion

        #region Textures
        Texture2D DecodeTextureData(string dataURI)
        {
            int pos = 5; // Skip "data:"
            int length = dataURI.Length;

            int startPos = pos;
            while (pos < length && dataURI[pos] != ';' && dataURI[pos] != ',')
                ++pos;

            var mediaType = dataURI.Substring(startPos, pos-startPos);
            if (mediaType != "image/png" && mediaType != "image/jpeg")
                return null;

            while (pos < length && dataURI[pos] != ',')
                ++pos;

            ++pos; // Skip ','

            if (pos >= length)
                return null;

            var data = Convert.FromBase64String(dataURI.Substring(pos));

            var tex = new Texture2D(1, 1);
            if (tex.LoadImage(data))
                return tex;

            return null;
        }
        #endregion

        #region Post-processing

        struct HierarchyUpdate
        {
            public SceneNode parent;
            public SceneNode newNode;
            public SceneNode replaceNode;
        }

        void PostProcess()
        {
            var hierarchyUpdates = new List<HierarchyUpdate>();

            // Adjust fills on all objects
            foreach (var nodeInfo in VectorUtils.WorldTransformedSceneNodes(scene.root, nodeOpacity))
            {
                if (nodeInfo.node.drawables == null)
                    continue;
                foreach (var drawable in nodeInfo.node.drawables)
                {
                    Filled filled = drawable as Filled;
                    if (filled != null)
                    {
                        if (filled.fill is GradientFill)
                        {
                            AdjustGradientFill(nodeInfo.node, nodeInfo.worldTransform, filled);
                        }
                        else if (filled.fill is PatternFill)
                        {
                            var fillNode = AdjustPatternFill(nodeInfo.node, nodeInfo.worldTransform, filled);
                            if (fillNode != null)
                            {
                                hierarchyUpdates.Add(new HierarchyUpdate() {
                                    parent = nodeInfo.parent,
                                    newNode = fillNode,
                                    replaceNode = nodeInfo.node
                                });
                            }
                        }
                    }
                }
            }

            foreach (var update in hierarchyUpdates)
            {
                var index = update.parent.children.IndexOf(update.replaceNode);
                update.parent.children.RemoveAt(index);
                update.parent.children.Insert(index, update.newNode);
            }
        }

        void AdjustGradientFill(SceneNode node, Matrix2D worldTransform, Filled filledObj)
        {
            GradientFill fill = filledObj.fill as GradientFill;
            if (fill == null)
                return;

            Vector2 min = Vector2.zero, max = Vector2.zero;
            if (filledObj is Rectangle)
            {
                var r = (Rectangle)filledObj;
                min = r.position;
                max = r.position + r.size;
            }
            else if (filledObj is Shape)
            {
                min = new Vector2(float.MaxValue, float.MaxValue);
                max = new Vector2(-float.MaxValue, -float.MaxValue);
                foreach (var contour in ((Shape)filledObj).contours)
                {
                    var bbox = VectorUtils.Bounds(contour);
                    min = Vector2.Min(min, bbox.min);
                    max = Vector2.Max(max, bbox.max);
                }
            }
            else System.Diagnostics.Debug.Assert(false);

            Rect bounds = new Rect(min, max - min);

            GradientExData extInfo = (GradientExData)gradientExInfo[fill];
            var containerSize = nodeGlobalSceneState[node].containerSize;
            Matrix2D gradTransform = Matrix2D.identity;

            currentContainerSize.Push(extInfo.worldRelative ? containerSize : Vector2.one);

            // If the fill is object relative, then the dimensions will come to us in
            // a normalized space, we must adjust those to the object's dimensions
            if (extInfo is LinearGradientExData)
            {
                // In SVG, linear gradients are expressed using two vectors. A vector and normal. The vector determines
                // the direction where the gradient increases. The normal determines the slant of the gradient along the vector.
                // Due to transformations, it is possible that those two vectors (the gradient vector and its normal) are not
                // actually perpendicular. That's why a skew transformation is involved here.
                // VectorScene just maps linear gradients from 0 to 1 across the entire bounding box width, so we
                // need to figure out a super transformation that takes those simply-mapped UVs and have them express
                // the linear gradient with its slant and all the fun involved.
                var linGradEx = (LinearGradientExData)extInfo;
                Vector2 lineStart = new Vector2(
                        AttribLengthVal(linGradEx.x1, null, null, 0.0f, DimType.Width),
                        AttribLengthVal(linGradEx.y1, null, null, 0.0f, DimType.Height));
                Vector2 lineEnd = new Vector2(
                        AttribLengthVal(linGradEx.x2, null, null, currentContainerSize.Peek().x, DimType.Width),
                        AttribLengthVal(linGradEx.y2, null, null, 0.0f, DimType.Height));

                var gradientVector = lineEnd - lineStart;
                float gradientVectorInvLength = 1.0f / gradientVector.magnitude;
                var scale = Matrix2D.Scale(new Vector2(bounds.width * gradientVectorInvLength, bounds.height * gradientVectorInvLength));
                var rotation = Matrix2D.Rotate(Mathf.Atan2(gradientVector.y, gradientVector.x));
                var offset = Matrix2D.Translate(-lineStart);
                gradTransform = rotation * scale * offset;
            }
            else if (extInfo is RadialGradientExData)
            {
                // VectorScene positions radial gradiants at the center of the bbox, and picks the radii (not one radius, but two)
                // to fill the space between the center and the two edges (horizontal and vertical). So in the general case
                // the radial is actually an ellipsoid. So we need to do an SRT transformation to position the radial gradient according
                // to the SVG center point and radius
                var radGradEx = (RadialGradientExData)extInfo;
                Vector2 halfCurrentContainerSize = currentContainerSize.Peek() * 0.5f;
                Vector2 center = new Vector2(
                        AttribLengthVal(radGradEx.cx, null, null, halfCurrentContainerSize.x, DimType.Width),
                        AttribLengthVal(radGradEx.cy, null, null, halfCurrentContainerSize.y, DimType.Height));
                Vector2 focus = new Vector2(
                        AttribLengthVal(radGradEx.fx, null, null, center.x, DimType.Width),
                        AttribLengthVal(radGradEx.fy, null, null, center.y, DimType.Height));
                float radius = AttribLengthVal(radGradEx.r, null, null, halfCurrentContainerSize.magnitude / SVGLengthFactor, DimType.Length);

                // This block below tells that radial focus cannot change per object, but is realized correctly for the first object
                // that requests this gradient. If the gradient is using object-relative coordinates to specify the focus location,
                // then only the first object will look correct, and the rest will potentially not look right. The alternative is
                // to detect if it is necessary and generate a new atlas entry for it
                if (!radGradEx.parsed)
                {
                    // VectorGradientFill radialFocus is (-1,1) relative to the outer circle
                    fill.radialFocus = (focus - center) / radius;
                    if (fill.radialFocus.sqrMagnitude > 1.0f - VectorUtils.Epsilon)
                        fill.radialFocus = fill.radialFocus.normalized * (1.0f - VectorUtils.Epsilon); // Stick within the unit circle

                    radGradEx.parsed = true;
                }

                gradTransform =
                    Matrix2D.Scale(bounds.size * 0.5f / radius) *
                    Matrix2D.Translate(new Vector2(radius, radius) - center);
            }
            else
            {
                Debug.LogError("Unsupported gradient type: " + extInfo);
            }

            currentContainerSize.Pop();

            var uvToWorld = extInfo.worldRelative ? Matrix2D.Translate(bounds.min) * Matrix2D.Scale(bounds.size) : Matrix2D.identity;
            var boundsInv = new Vector2(1.0f / bounds.width, 1.0f / bounds.height);
            filledObj.fillTransform = Matrix2D.Scale(boundsInv) * gradTransform * extInfo.fillTransform.Inverse() * uvToWorld;
        }

        SceneNode AdjustPatternFill(SceneNode node, Matrix2D worldTransform, Filled filledObj)
        {
            PatternFill patternFill = filledObj.fill as PatternFill;
            if (patternFill == null ||
                Mathf.Abs(patternFill.rect.width) < VectorUtils.Epsilon ||
                Mathf.Abs(patternFill.rect.height) < VectorUtils.Epsilon)
            {
                return null;
            }
            
            var data = patternData[patternFill.pattern];

            var nodeBounds = VectorUtils.SceneNodeBounds(node);
            var patternRect = patternFill.rect;
            if (!data.worldRelative)
            {
                patternRect.position *= nodeBounds.size;
                patternRect.size *= nodeBounds.size;
            }

            // The pattern fill will create a new clipped node containing the repeating pattern
            // as well as a sibling containing the original node. This will replace the original node.
            var replacementNode = new SceneNode() {
                transform = node.transform,
                children = new List<SceneNode>(2)
            };
            node.transform = Matrix2D.identity;

            // The pattern node will be wrapped in a scaling node if content isn't world relative
            var patternNode = patternFill.pattern;
            if (!data.contentWorldRelative)
            {
                patternNode = new SceneNode() {
                    transform = Matrix2D.Scale(nodeBounds.size),
                    children = new List<SceneNode> { patternFill.pattern }
                };
            }

            // Duplicate the filling pattern
            var grid = new SceneNode() {
                transform = data.patternTransform,
                children = new List<SceneNode>(20)
            };

            var fill = new SceneNode() {
                transform = Matrix2D.identity,
                children = new List<SceneNode> { grid },
                clipper = node
            };

            // SVG patterns are clipped in their respective "boxes"
            var box = new SceneNode() {
                transform = Matrix2D.identity,
                drawables = new List<IDrawable> { new Rectangle() { size = patternRect.size } }
            };

            // Compute the bounds of the shape to be filled, taking into account the pattern transform
            var bounds = VectorUtils.SceneNodeBounds(node);
            var invPatternTransform = data.patternTransform.Inverse();
            var boundVerts = new Vector2[] {
                invPatternTransform * new Vector2(bounds.xMin, bounds.yMin),
                invPatternTransform * new Vector2(bounds.xMax, bounds.yMin),
                invPatternTransform * new Vector2(bounds.xMax, bounds.yMax),
                invPatternTransform * new Vector2(bounds.xMin, bounds.yMax)
            };
            bounds = VectorUtils.Bounds(boundVerts);

            // Start the pattern filling process
            var offset = patternRect.position;
            float xStart = (int)(bounds.x / patternRect.width) * patternRect.width - patternRect.width;
            float yStart = (int)(bounds.y / patternRect.height) * patternRect.height - patternRect.height;

            for (float y = yStart; y < bounds.yMax; y += patternRect.height)
            {
                for (float x = xStart; x < bounds.xMax; x += patternRect.width)
                {
                    var pattern = new SceneNode() {
                        transform = Matrix2D.Translate(new Vector2(x, y) + offset),
                        children = new List<SceneNode> { patternNode },
                        clipper = box
                    };
                    grid.children.Add(pattern);
                }
            }

            replacementNode.children.Add(fill);
            replacementNode.children.Add(node);

            return replacementNode;
        }

        #endregion

        delegate void ElemHandler();
        class Handlers : Dictionary<string, ElemHandler>
        {
            public Handlers(int capacity) : base(capacity) {}
        }
        bool ShouldDeclareSupportedChildren(XmlReaderIterator.Node node) { return !subTags.ContainsKey(node.Name); }
        void SupportElems(XmlReaderIterator.Node node, params ElemHandler[] handlers)
        {
            var elems = new Handlers(handlers.Length);
            foreach (var h in handlers)
                elems[h.Method.Name] = h;
            subTags[node.Name] = elems;
        }

        static char[] whiteSpaceNumberChars = " \r\n\t,".ToCharArray();
        enum DimType { Width, Height, Length };
        XmlReaderIterator docReader;
        Scene scene;
        float dpiScale;
        int windowWidth, windowHeight;
        Vector2 scenePos, sceneSize;
        SVGDictionary svgObjects = new SVGDictionary(); // Named elements are looked up in this
        Dictionary<string, Handlers> subTags = new Dictionary<string, Handlers>(); // For each element, the set of elements supported as its children
        Dictionary<GradientFill, GradientExData> gradientExInfo = new Dictionary<GradientFill, GradientExData>();
        Dictionary<SceneNode, ViewBoxInfo> symbolViewBoxes = new Dictionary<SceneNode, ViewBoxInfo>();
        Dictionary<SceneNode, NodeGlobalSceneState> nodeGlobalSceneState = new Dictionary<SceneNode, NodeGlobalSceneState>();
        Dictionary<SceneNode, float> nodeOpacity = new Dictionary<SceneNode, float>();
        Dictionary<SceneNode, ClipData> clipData = new Dictionary<SceneNode, ClipData>();
        Dictionary<SceneNode, PatternData> patternData = new Dictionary<SceneNode, PatternData>();
        Dictionary<SceneNode, MaskData> maskData = new Dictionary<SceneNode, MaskData>();
        Stack<Vector2> currentContainerSize = new Stack<Vector2>();
        Stack<SceneNode> currentSceneNode = new Stack<SceneNode>();
        GradientFill currentGradientFill;
        ElemHandler[] allElems;
        HashSet<ElemHandler> elemsToAddToHierarchy;
        SVGStyleResolver styleResolver = new SVGStyleResolver();

        struct NodeGlobalSceneState
        {
            public Vector2 containerSize;
        }

        class GradientExData
        {
            public bool worldRelative;
            public Matrix2D fillTransform;
        }

        class LinearGradientExData : GradientExData
        {
            public string x1, y1, x2, y2;
        }

        class RadialGradientExData : GradientExData
        {
            public bool parsed;
            public string cx, cy, fx, fy, r;
        }

        struct ClipData
        {
            public bool worldRelative;
        }

        struct PatternData
        {
            public bool worldRelative;
            public bool contentWorldRelative;
            public Matrix2D patternTransform;
        }

        struct MaskData
        {
            public bool worldRelative;
            public bool contentWorldRelative;
        }
    }

    internal enum SVGResolveLimit
    {
        Single,
        Hierarchy
    }

    internal class SVGStyleResolver
    {
        public SVGStyleResolver()
        {
            styleSheets = new List<SVGStyleSheet>();
            attributeSheets = new List<SVGPropertySheet>();
            nodes = new List<NodeData>();
            globalStyleSheet = new SVGStyleSheet();
        }

        public void PushNode(XmlReaderIterator.Node node)
        {
            var elem = new NodeData();
            elem.node = node;
            elem.name = node.Name;
            var klass = node["class"];
            if (klass != null)
                elem.classes = node["class"].Split(' ').Select(x => x.Trim()).ToList();
            else
                elem.classes = new List<string>();
            elem.classes = SortedClasses(elem.classes).ToList();
            elem.id = node["id"];

            nodes.Add(elem);

            attributeSheets.Add(node.GetAttributes());

            var cssText = node["style"];
            if (cssText != null)
            {
                var props = SVGStyleSheetUtils.ParseInline(cssText);
                var sheet = new SVGStyleSheet();
                sheet[node.Name] = props;
                PushStyleSheet(sheet);
            }
            else
            {
                PushStyleSheet(new SVGStyleSheet());
            }
        }

        public void PopNode()
        {
            if (nodes.Count == 0)
                throw SVGFormatException.StackError;

            nodes.RemoveAt(nodes.Count-1);
            attributeSheets.RemoveAt(attributeSheets.Count-1);
            styleSheets.RemoveAt(styleSheets.Count-1);
        }

        public void PushStyleSheet(SVGStyleSheet sheet, bool isGlobal = false)
        {
            if (!isGlobal)
                styleSheets.Add(sheet);
            else
            {
                foreach (var sel in sheet.selectors)
                    globalStyleSheet[sel] = sheet[sel];
            }
        }

        public void PopStyleSheet()
        {
            if (styleSheets.Count == 0)
                throw SVGFormatException.StackError;

            styleSheets.RemoveAt(styleSheets.Count-1);
        }

        public string Evaluate(string attribName, SVGResolveLimit limit = SVGResolveLimit.Single)
        {
            for (int i = nodes.Count-1; i >= 0; --i)
            {
                string attrib = null;
                if (LookupStyleOrAttribute(nodes[i], attribName, styleSheets[i], attributeSheets[i], limit, out attrib))
                    return attrib;
                
                if (limit == SVGResolveLimit.Single)
                    break;
            }
            return null;
        }

        private bool LookupStyleOrAttribute(NodeData nodeData, string attribName, SVGStyleSheet styleSheet, SVGPropertySheet propSheet, SVGResolveLimit limit, out string attrib)
        {
            // Try to match a CSS style first
            if (LookupProperty(nodeData, attribName, styleSheet, out attrib))
                return true;

            // Try to match a global CSS style
            if (LookupProperty(nodeData, attribName, globalStyleSheet, out attrib))
                return true;

            // Else, fallback on attribute
            if (propSheet.ContainsKey(attribName))
            {
                attrib = propSheet[attribName];
                return true;
            }

            return false;
        }

        private bool LookupProperty(NodeData nodeData, string attribName, SVGStyleSheet sheet, out string val)
        {
            var id = string.IsNullOrEmpty(nodeData.id) ? null : "#" + nodeData.id;
            var name = string.IsNullOrEmpty(nodeData.name) ? null : nodeData.name;

            if (LookupPropertyInSheet(sheet, attribName, id, out val))
                return true;

            foreach (var c in nodeData.classes)
            {
                var klass = "." + c;
                if (LookupPropertyInSheet(sheet, attribName, klass, out val))
                    return true;
            }

            if (LookupPropertyInSheet(sheet, attribName, name, out val))
                return true;

            val = null;
            return false;
        }

        private bool LookupPropertyInSheet(SVGStyleSheet sheet, string attribName, string selector, out string val)
        {
            if (selector == null)
            {
                val = null;
                return false;
            }

            if (sheet.selectors.Contains(selector))
            {
                var props = sheet[selector];
                if (props.ContainsKey(attribName))
                {
                    val = props[attribName];
                    return true;
                }
            }

            val = null;
            return false;
        }

        private IEnumerable<string> SortedClasses(List<string> classes)
        {
            // Classes should be matched by the inverse order they appeared in the sheet
            // (i.e., the last selector specified has higher precedence).
            foreach (var sel in globalStyleSheet.selectors.Reverse())
            {
                if (sel[0] != '.')
                    continue;
                var klass = sel.Substring(1);
                if (classes.Contains(klass))
                    yield return klass;
            }
        }

        struct NodeData
        {
            public XmlReaderIterator.Node node;
            public string name;
            public List<string> classes;
            public string id;
        }

        List<SVGStyleSheet> styleSheets;
        List<SVGPropertySheet> attributeSheets;
        List<NodeData> nodes;

        SVGStyleSheet globalStyleSheet;
    }

    internal class SVGAttribParser
    {
        public static List<BezierContour> ParsePath(XmlReaderIterator.Node node)
        {
            string path = node["d"];
            if (string.IsNullOrEmpty(path))
                throw node.GetException("'path' element missing 'd' attribute");
            try
            {
                return (new SVGAttribParser(path, AttribPath.Path)).contours;
            }
            catch (Exception e)
            {
                throw node.GetException(e.Message);
            }
        }

        public static Matrix2D ParseTransform(XmlReaderIterator.Node node)
        {
            return ParseTransform(node, "transform");
        }

        public static Matrix2D ParseTransform(XmlReaderIterator.Node node, string attribName)
        {
            // Transforms aren't part of styling and shouldn't be evaluated,
            // they have to be specified as node attributes
            string transform = node[attribName];
            if (string.IsNullOrEmpty(transform))
                return Matrix2D.identity;
            try
            {
                return (new SVGAttribParser(transform, attribName, AttribTransform.Transform)).transform;
            }
            catch (Exception e)
            {
                throw node.GetException(e.Message);
            }
        }

        public static IFill ParseFill(XmlReaderIterator.Node node, SVGDictionary dict, SVGStyleResolver styleResolver)
        {
            string opacityAttrib = styleResolver.Evaluate("fill-opacity", SVGResolveLimit.Hierarchy);
            float opacity = (opacityAttrib != null) ? float.Parse(opacityAttrib) : 1.0f;
            string fillMode = styleResolver.Evaluate("fill-rule", SVGResolveLimit.Hierarchy);
            FillMode mode = FillMode.NonZero;
            if (fillMode != null)
            {
                if (fillMode == "nonzero")
                    mode = FillMode.NonZero;
                else if (fillMode == "evenodd")
                    mode = FillMode.OddEven;
                else throw new Exception("Unknown fill-rule: " + fillMode);
            }

            try
            {
                var fill = styleResolver.Evaluate("fill", SVGResolveLimit.Hierarchy);
                return (new SVGAttribParser(fill, "fill", opacity, mode, dict)).fill;
            }
            catch (Exception e)
            {
                throw node.GetException(e.Message);
            }
        }

        public static Stroke ParseStrokeAndOpacity(XmlReaderIterator.Node node, SVGDictionary dict, SVGStyleResolver styleResolver)
        {
            string strokeAttrib = styleResolver.Evaluate("stroke", SVGResolveLimit.Hierarchy);
            if (string.IsNullOrEmpty(strokeAttrib))
                return null; // If stroke is not specified, no other stroke properties matter

            string opacityAttrib = styleResolver.Evaluate("stroke-opacity", SVGResolveLimit.Hierarchy);
            float opacity = (opacityAttrib != null) ? float.Parse(opacityAttrib) : 1.0f;

            IFill strokeFill = null;
            try
            {
                strokeFill = (new SVGAttribParser(strokeAttrib, "stroke", opacity, FillMode.NonZero, dict)).fill;
            }
            catch (Exception e)
            {
                throw node.GetException(e.Message);
            }

            if (strokeFill == null)
                return null;
            if (!(strokeFill is SolidFill))
                throw node.GetException("stroke fills other that a solid color are not supported yet");

            Stroke stroke = new Stroke();
            stroke.color = ((SolidFill)strokeFill).color;
            return stroke;
        }

        public static Color ParseColor(string colorString)
        {
            if (colorString[0] == '#')
            {
                // Hex format
                var hexVal = UInt32.Parse(colorString.Substring(1), NumberStyles.HexNumber);
                if (colorString.Length == 4)
                {
                    // #ABC >> #AABBCC
                    return new Color(
                        ((((hexVal >> 8) & 0xF) << 0) | (((hexVal >> 8) & 0xF) << 4)) / 255.0f,
                        ((((hexVal >> 4) & 0xF) << 0) | (((hexVal >> 4) & 0xF) << 4)) / 255.0f,
                        ((((hexVal >> 0) & 0xF) << 0) | (((hexVal >> 0) & 0xF) << 4)) / 255.0f);
                }
                else
                {
                    // #ABCDEF
                    return new Color(
                        ((hexVal >> 16) & 0xFF) / 255.0f,
                        ((hexVal >> 8) & 0xFF) / 255.0f,
                        ((hexVal >> 0) & 0xFF) / 255.0f);
                }
            }
            if (colorString.StartsWith("rgb(") && colorString.EndsWith(")"))
            {
                string[] numbers = colorString.Split(new char[] { ',', '%' }, StringSplitOptions.RemoveEmptyEntries);
                if (numbers.Length != 3)
                    throw new Exception("Invalid rgb() color specification");
                float divisor = colorString.Contains("%") ? 100.0f : 255.0f;
                return new Color(Byte.Parse(numbers[0]) / divisor, Byte.Parse(numbers[1]) / divisor, Byte.Parse(numbers[2]) / divisor);
            }

            // Named color
            if (namedColors == null)
                namedColors = new NamedWebColorDictionary();
            return namedColors[colorString.ToLower()];
        }

        public static string ParseURLRef(string url)
        {
            if (url.StartsWith("url(") && url.EndsWith(")"))
                return url.Substring(4, url.Length - 5);
            return null;
        }

        public static object ParseRelativeRef(string iri, SVGDictionary dict)
        {
            if (iri == null)
                return null;

            if (!iri.StartsWith("#"))
                throw new Exception("Unsupported reference type (" + iri + ")");
            iri = iri.Substring(1);
            object obj;
            dict.TryGetValue(iri, out obj);
            return obj;
        }

        SVGAttribParser(string attrib, AttribPath attribPath)
        {
            attribName = "path";
            attribString = attrib;
            NextPathCommand(true);
            if (pathCommand != 'm' && pathCommand != 'M')
                throw new Exception("Path must start with a MoveTo pathCommand");

            while (NextPathCommand() != (char)0)
            {
                bool relative = (pathCommand >= 'a') && (pathCommand <= 'z');
                char cmdNoCase = char.ToLower(pathCommand);
                if (cmdNoCase == 'm') // Move-to
                {
                    penPos = NextVector2(relative);
                    pathCommand = relative ? 'l' : 'L'; // After a move-to, we automatically switch to a line-to of the same relativity
                    ConcludePath(false);
                }
                else if (cmdNoCase == 'z') // ClosePath
                {
                    penPos = currentContour.First != null ? currentContour.First.Value.p0 : Vector2.zero;
                    ConcludePath(true);
                }
                else if (cmdNoCase == 'l') // Line-to
                {
                    var to = NextVector2(relative);
                    currentContour.AddLast(VectorUtils.MakeLine(penPos, to));
                    penPos = to;
                }
                else if (cmdNoCase == 'h') // Horizontal-line-to
                {
                    float x = relative ? penPos.x + NextFloat() : NextFloat();
                    var to = new Vector2(x, penPos.y);
                    currentContour.AddLast(VectorUtils.MakeLine(penPos, to));
                    penPos = to;
                }
                else if (cmdNoCase == 'v') // Vertical-line-to
                {
                    float y = relative ? penPos.y + NextFloat() : NextFloat();
                    var to = new Vector2(penPos.x, y);
                    currentContour.AddLast(VectorUtils.MakeLine(penPos, to));
                    penPos = to;
                }
                else if (cmdNoCase == 'c' || cmdNoCase == 'q') // Cubic-bezier-curve or quadratic-bezier-curve
                {
                    // If relative, the pen position is on P0 and is only moved to P3
                    BezierSegment bs = new BezierSegment();
                    bs.p0 = penPos;
                    bs.p1 = NextVector2(relative);
                    if (cmdNoCase == 'c')
                        bs.p2 = NextVector2(relative);
                    bs.p3 = NextVector2(relative);

                    if (cmdNoCase == 'q')
                    {
                        var p1 = bs.p1;
                        var t = 2.0f/3.0f;
                        bs.p1 = bs.p0 + t*(p1-bs.p0);
                        bs.p2 = bs.p3 + t*(p1-bs.p3);
                    }

                    penPos = bs.p3;
                    currentContour.AddLast(bs);
                }
                else if (cmdNoCase == 's' || cmdNoCase == 't') // Smooth cubic-bezier-curve or smooth quadratic-bezier-curve
                {
                    Vector2 reflectedP1 = penPos;
                    if (currentContour.Count > 0)
                        reflectedP1 += currentContour.Last.Value.p3 - currentContour.Last.Value.p2;

                    // If relative, the pen position is on P0 and is only moved to P3
                    BezierSegment bs = new BezierSegment();
                    bs.p0 = penPos;
                    bs.p1 = reflectedP1;
                    if (cmdNoCase == 's')
                        bs.p2 = NextVector2(relative);
                    else bs.p2 = bs.p1;
                    bs.p3 = NextVector2(relative);
                    penPos = bs.p3;
                    currentContour.AddLast(bs);
                }
                else if (cmdNoCase == 'a') // Elliptical-arc-to
                {
                    Vector2 radii = NextVector2();
                    float xAxisRotation = NextFloat();
                    bool largeArcFlag = NextBool();
                    bool sweepFlag = NextBool();
                    Vector2 to = NextVector2(relative);

                    if (radii.magnitude <= VectorUtils.Epsilon)
                    {
                        currentContour.AddLast(VectorUtils.MakeLine(penPos, to));
                    }
                    else
                    {
                        foreach (var seg in VectorUtils.BuildEllipsePath(penPos, to, -xAxisRotation * Mathf.Deg2Rad, radii.x, radii.y, largeArcFlag, sweepFlag))
                            currentContour.AddLast(seg);
                    }

                    penPos = to;
                }
            } // While commands exist in the string

            ConcludePath(false);
        }

        SVGAttribParser(string attrib, string attribNameVal, AttribTransform attribTransform)
        {
            attribString = attrib;
            attribName = attribNameVal;
            transform = Matrix2D.identity;
            while (stringPos < attribString.Length)
            {
                int cmdPos = stringPos;
                var trasformCommand = NextStringCommand();
                if (string.IsNullOrEmpty(trasformCommand))
                    return;
                SkipSymbol('(');

                if (trasformCommand == "matrix")
                {
                    Matrix2D mat = new Matrix2D();
                    mat.m00 = NextFloat();
                    mat.m10 = NextFloat();
                    mat.m01 = NextFloat();
                    mat.m11 = NextFloat();
                    mat.m02 = NextFloat();
                    mat.m12 = NextFloat();
                    transform *= mat;
                }
                else if (trasformCommand == "translate")
                {
                    float x = NextFloat();
                    float y = 0;
                    if (!PeekSymbol(')'))
                        y = NextFloat();
                    transform *= Matrix2D.Translate(new Vector2(x, y));
                }
                else if (trasformCommand == "scale")
                {
                    float x = NextFloat();
                    float y = x;
                    if (!PeekSymbol(')'))
                        y = NextFloat();
                    transform *= Matrix2D.Scale(new Vector2(x, y));
                }
                else if (trasformCommand == "rotate")
                {
                    float a = NextFloat() * Mathf.Deg2Rad;
                    float cx = 0, cy = 0;
                    if (!PeekSymbol(')'))
                    {
                        cx = NextFloat();
                        cy = NextFloat();
                    }
                    transform *= Matrix2D.Translate(new Vector2(-cx, -cy)) * Matrix2D.Rotate(-a) * Matrix2D.Translate(new Vector2(cx, cy));
                }
                else if ((trasformCommand == "skewX") || (trasformCommand == "skewY"))
                {
                    float a = Mathf.Tan(NextFloat() * Mathf.Deg2Rad);
                    Matrix2D mat = Matrix2D.identity;
                    if (trasformCommand == "skewY")
                        mat.m10 = a;
                    else mat.m01 = a;
                    transform *= mat;
                }
                else throw new Exception("Unknown transform command at " + cmdPos + " in trasform specification");

                SkipSymbol(')');
            }
        }

        SVGAttribParser(string attrib, string attribName, float opacity, FillMode mode, SVGDictionary dict, bool allowReference = true)
        {
            this.attribName = attribName;
            if (string.IsNullOrEmpty(attrib))
            {
                if (opacity < 1.0f)
                    fill = new SolidFill() { color = new Color(0, 0, 0, opacity) };
                else
                    fill = dict[mode == FillMode.NonZero ?
                                SVGDocument.StockBlackNonZeroFillName :
                                SVGDocument.StockBlackOddEvenFillName] as IFill;
                return;
            }

            if (attrib == "none" || attrib == "transparent")
                return;

            if (attrib == "currentColor")
                throw new NotSupportedException("currentColor is not supported as a " + attribName + " value");

            string[] paintParts = attrib.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (allowReference)
            {
                string reference = ParseURLRef(paintParts[0]);
                if (reference != null)
                {
                    fill = ParseRelativeRef(reference, dict) as IFill;
                    if (fill == null)
                    {
                        if (paintParts.Length > 1)
                            fill = (new SVGAttribParser(paintParts[1], attribName, opacity, mode, dict, false)).fill;
                        else Debug.LogWarning("Referencing non-existent paint (" + reference + ")");
                    }
                    return;
                }
            }

            var clr = ParseColor(paintParts[0]);
            clr.a = opacity;
            if (paintParts.Length > 1)
            {
                // TODO: Support ICC-Color
            }
            fill = new SolidFill() { color = clr, mode = mode };
        }

        void ConcludePath(bool joinEnds)
        {
            // No need to manually close the path with the last line. It is implied.
            //if (joinEnds && currentPath.Count >= 2)
            //{
            //    BezierSegment bs = new BezierSegment();
            //    bs.MakeLine(currentPath.Last.Value.P3, currentPath.First.Value.P0);
            //    currentPath.AddLast(bs);
            //}
            if (currentContour.Count > 0)
            {
                BezierContour contour = new BezierContour();
                contour.closed = joinEnds && (currentContour.Count >= 1);
                contour.segments = new BezierPathSegment[currentContour.Count + 1];
                int index = 0;
                foreach (var bs in currentContour)
                    contour.segments[index++] = new BezierPathSegment() { p0 = bs.p0, p1 = bs.p1, p2 = bs.p2  };
                contour.segments[index] = new BezierPathSegment() { p0 = currentContour.Last.Value.p3 };
                contours.Add(contour);
            }
            currentContour.Clear(); // Restart a new path
        }

        Vector2 NextVector2(bool relative = false)
        {
            var v = new Vector2(NextFloat(), NextFloat());
            return relative ? v + penPos : v;
        }

        float NextFloat()
        {
            SkipWhitespaces();
            if (stringPos >= attribString.Length)
                throw new Exception(attribName + " specification ended before sufficing numbers required by the last pathCommand");

            int startPos = stringPos;
            if (attribString[stringPos] == '-')
                stringPos++; // Skip over the negative sign if it exists

            bool gotPeriod = false;
            bool gotE = false;
            while (stringPos < attribString.Length)
            {
                char c = attribString[stringPos];
                if (!gotPeriod && (c == '.'))
                {
                    gotPeriod = true;
                    stringPos++;
                    continue;
                }
                if (!gotE && ((c == 'e') || (c == 'E')))
                {
                    gotE = true;
                    stringPos++;
                    if ((stringPos < attribString.Length) && (attribString[stringPos] == '-'))
                        stringPos++; // Skip over the negative sign if it exists for the e
                    continue;
                }
                if (!char.IsDigit(c))
                    break;
                stringPos++;
            }

            if ((stringPos - startPos == 0) ||
                ((stringPos - startPos == 1) && attribString[startPos] == '-'))
                throw new Exception("Missing number at " + startPos + " in " + attribName + " specification");

            return float.Parse(attribString.Substring(startPos, stringPos - startPos));
        }

        bool NextBool()
        {
            return Mathf.Abs(NextFloat()) > VectorUtils.Epsilon;
        }

        char NextPathCommand(bool noCommandInheritance = false)
        {
            SkipWhitespaces();
            if (stringPos >= attribString.Length)
                return (char)0;

            char newCmd = attribString[stringPos];
            if ((newCmd >= 'a' && newCmd <= 'z') || (newCmd >= 'A' && newCmd <= 'Z'))
            {
                pathCommand = newCmd;
                stringPos++;
                return newCmd;
            }

            if (!noCommandInheritance && (char.IsDigit(newCmd) || (newCmd == '.') || (newCmd == '-')))
                return pathCommand; // Stepped onto a number, which means we keep the last pathCommand
            throw new Exception("Unexpected character at " + stringPos + " in path specification");
        }

        string NextStringCommand()
        {
            SkipWhitespaces();
            if (stringPos >= attribString.Length)
                return null;

            int startPos = stringPos;
            while (stringPos < attribString.Length)
            {
                char c = attribString[stringPos];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    stringPos++;
                else break;
            }

            if (stringPos - startPos == 0)
                throw new Exception("Unexpected character at " + stringPos + " in " + attribName + " specification");

            return attribString.Substring(startPos, stringPos - startPos);
        }

        void SkipSymbol(char s)
        {
            SkipWhitespaces();
            if (stringPos >= attribString.Length || (attribString[stringPos] != s))
                throw new Exception("Expected " + s + " at " + stringPos + " of " + attribName + " specification");
            stringPos++;
        }

        bool PeekSymbol(char s)
        {
            SkipWhitespaces();
            return (stringPos < attribString.Length) && (attribString[stringPos] == s);
        }

        void SkipWhitespaces()
        {
            while (stringPos < attribString.Length)
            {
                switch (attribString[stringPos])
                {
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                    case ',':
                        stringPos++;
                        break;
                    default:
                        return;
                }
            }
        }

        enum AttribPath { Path };
        enum AttribTransform { Transform };
        enum AttribStroke { Stroke };

        // Path data
        LinkedList<BezierSegment> currentContour = new LinkedList<BezierSegment>();
        List<BezierContour> contours = new List<BezierContour>();
        Vector2 penPos;
        string attribString;
        char pathCommand;

        // Transform data
        Matrix2D transform;

        // Fill data
        IFill fill;

        // Parsing data
        string attribName;
        int stringPos;

        static NamedWebColorDictionary namedColors;
    }

    class NamedWebColorDictionary : Dictionary<string, Color>
    {
        public NamedWebColorDictionary()
        {
            this["aliceblue"] = new Color(240.0f / 255.0f, 248.0f / 255.0f, 255.0f / 255.0f);
            this["antiquewhite"] = new Color(250.0f / 255.0f, 235.0f / 255.0f, 215.0f / 255.0f);
            this["aqua"] = new Color(0.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
            this["aquamarine"] = new Color(127.0f / 255.0f, 255.0f / 255.0f, 212.0f / 255.0f);
            this["azure"] = new Color(240.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
            this["beige"] = new Color(245.0f / 255.0f, 245.0f / 255.0f, 220.0f / 255.0f);
            this["bisque"] = new Color(255.0f / 255.0f, 228.0f / 255.0f, 196.0f / 255.0f);
            this["black"] = new Color(0.0f / 255.0f, 0.0f / 255.0f, 0.0f / 255.0f);
            this["blanchedalmond"] = new Color(255.0f / 255.0f, 235.0f / 255.0f, 205.0f / 255.0f);
            this["blue"] = new Color(0.0f / 255.0f, 0.0f / 255.0f, 255.0f / 255.0f);
            this["blueviolet"] = new Color(138.0f / 255.0f, 43.0f / 255.0f, 226.0f / 255.0f);
            this["brown"] = new Color(165.0f / 255.0f, 42.0f / 255.0f, 42.0f / 255.0f);
            this["burlywood"] = new Color(222.0f / 255.0f, 184.0f / 255.0f, 135.0f / 255.0f);
            this["cadetblue"] = new Color(95.0f / 255.0f, 158.0f / 255.0f, 160.0f / 255.0f);
            this["chartreuse"] = new Color(127.0f / 255.0f, 255.0f / 255.0f, 0.0f / 255.0f);
            this["chocolate"] = new Color(210.0f / 255.0f, 105.0f / 255.0f, 30.0f / 255.0f);
            this["coral"] = new Color(255.0f / 255.0f, 127.0f / 255.0f, 80.0f / 255.0f);
            this["cornflowerblue"] = new Color(100.0f / 255.0f, 149.0f / 255.0f, 237.0f / 255.0f);
            this["cornsilk"] = new Color(255.0f / 255.0f, 248.0f / 255.0f, 220.0f / 255.0f);
            this["crimson"] = new Color(220.0f / 255.0f, 20.0f / 255.0f, 60.0f / 255.0f);
            this["cyan"] = new Color(0.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
            this["darkblue"] = new Color(0.0f / 255.0f, 0.0f / 255.0f, 139.0f / 255.0f);
            this["darkcyan"] = new Color(0.0f / 255.0f, 139.0f / 255.0f, 139.0f / 255.0f);
            this["darkgoldenrod"] = new Color(184.0f / 255.0f, 134.0f / 255.0f, 11.0f / 255.0f);
            this["darkgray"] = new Color(169.0f / 255.0f, 169.0f / 255.0f, 169.0f / 255.0f);
            this["darkgrey"] = new Color(169.0f / 255.0f, 169.0f / 255.0f, 169.0f / 255.0f);
            this["darkgreen"] = new Color(0.0f / 255.0f, 100.0f / 255.0f, 0.0f / 255.0f);
            this["darkkhaki"] = new Color(189.0f / 255.0f, 183.0f / 255.0f, 107.0f / 255.0f);
            this["darkmagenta"] = new Color(139.0f / 255.0f, 0.0f / 255.0f, 139.0f / 255.0f);
            this["darkolivegreen"] = new Color(85.0f / 255.0f, 107.0f / 255.0f, 47.0f / 255.0f);
            this["darkorange"] = new Color(255.0f / 255.0f, 140.0f / 255.0f, 0.0f / 255.0f);
            this["darkorchid"] = new Color(153.0f / 255.0f, 50.0f / 255.0f, 204.0f / 255.0f);
            this["darkred"] = new Color(139.0f / 255.0f, 0.0f / 255.0f, 0.0f / 255.0f);
            this["darksalmon"] = new Color(233.0f / 255.0f, 150.0f / 255.0f, 122.0f / 255.0f);
            this["darkseagreen"] = new Color(143.0f / 255.0f, 188.0f / 255.0f, 143.0f / 255.0f);
            this["darkslateblue"] = new Color(72.0f / 255.0f, 61.0f / 255.0f, 139.0f / 255.0f);
            this["darkslategray"] = new Color(47.0f / 255.0f, 79.0f / 255.0f, 79.0f / 255.0f);
            this["darkslategrey"] = new Color(47.0f / 255.0f, 79.0f / 255.0f, 79.0f / 255.0f);
            this["darkturquoise"] = new Color(0.0f / 255.0f, 206.0f / 255.0f, 209.0f / 255.0f);
            this["darkviolet"] = new Color(148.0f / 255.0f, 0.0f / 255.0f, 211.0f / 255.0f);
            this["deeppink"] = new Color(255.0f / 255.0f, 20.0f / 255.0f, 147.0f / 255.0f);
            this["deepskyblue"] = new Color(0.0f / 255.0f, 191.0f / 255.0f, 255.0f / 255.0f);
            this["dimgray"] = new Color(105.0f / 255.0f, 105.0f / 255.0f, 105.0f / 255.0f);
            this["dimgrey"] = new Color(105.0f / 255.0f, 105.0f / 255.0f, 105.0f / 255.0f);
            this["dodgerblue"] = new Color(30.0f / 255.0f, 144.0f / 255.0f, 255.0f / 255.0f);
            this["firebrick"] = new Color(178.0f / 255.0f, 34.0f / 255.0f, 34.0f / 255.0f);
            this["floralwhite"] = new Color(255.0f / 255.0f, 250.0f / 255.0f, 240.0f / 255.0f);
            this["forestgreen"] = new Color(34.0f / 255.0f, 139.0f / 255.0f, 34.0f / 255.0f);
            this["fuchsia"] = new Color(255.0f / 255.0f, 0.0f / 255.0f, 255.0f / 255.0f);
            this["gainsboro"] = new Color(220.0f / 255.0f, 220.0f / 255.0f, 220.0f / 255.0f);
            this["ghostwhite"] = new Color(248.0f / 255.0f, 248.0f / 255.0f, 255.0f / 255.0f);
            this["gold"] = new Color(255.0f / 255.0f, 215.0f / 255.0f, 0.0f / 255.0f);
            this["goldenrod"] = new Color(218.0f / 255.0f, 165.0f / 255.0f, 32.0f / 255.0f);
            this["gray"] = new Color(128.0f / 255.0f, 128.0f / 255.0f, 128.0f / 255.0f);
            this["grey"] = new Color(128.0f / 255.0f, 128.0f / 255.0f, 128.0f / 255.0f);
            this["green"] = new Color(0.0f / 255.0f, 128.0f / 255.0f, 0.0f / 255.0f);
            this["greenyellow"] = new Color(173.0f / 255.0f, 255.0f / 255.0f, 47.0f / 255.0f);
            this["honeydew"] = new Color(240.0f / 255.0f, 255.0f / 255.0f, 240.0f / 255.0f);
            this["hotpink"] = new Color(255.0f / 255.0f, 105.0f / 255.0f, 180.0f / 255.0f);
            this["indianred"] = new Color(205.0f / 255.0f, 92.0f / 255.0f, 92.0f / 255.0f);
            this["indigo"] = new Color(75.0f / 255.0f, 0.0f / 255.0f, 130.0f / 255.0f);
            this["ivory"] = new Color(255.0f / 255.0f, 255.0f / 255.0f, 240.0f / 255.0f);
            this["khaki"] = new Color(240.0f / 255.0f, 230.0f / 255.0f, 140.0f / 255.0f);
            this["lavender"] = new Color(230.0f / 255.0f, 230.0f / 255.0f, 250.0f / 255.0f);
            this["lavenderblush"] = new Color(255.0f / 255.0f, 240.0f / 255.0f, 245.0f / 255.0f);
            this["lawngreen"] = new Color(124.0f / 255.0f, 252.0f / 255.0f, 0.0f / 255.0f);
            this["lemonchiffon"] = new Color(255.0f / 255.0f, 250.0f / 255.0f, 205.0f / 255.0f);
            this["lightblue"] = new Color(173.0f / 255.0f, 216.0f / 255.0f, 230.0f / 255.0f);
            this["lightcoral"] = new Color(240.0f / 255.0f, 128.0f / 255.0f, 128.0f / 255.0f);
            this["lightcyan"] = new Color(224.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
            this["lightgoldenrodyellow"] = new Color(250.0f / 255.0f, 250.0f / 255.0f, 210.0f / 255.0f);
            this["lightgray"] = new Color(211.0f / 255.0f, 211.0f / 255.0f, 211.0f / 255.0f);
            this["lightgrey"] = new Color(211.0f / 255.0f, 211.0f / 255.0f, 211.0f / 255.0f);
            this["lightgreen"] = new Color(144.0f / 255.0f, 238.0f / 255.0f, 144.0f / 255.0f);
            this["lightpink"] = new Color(255.0f / 255.0f, 182.0f / 255.0f, 193.0f / 255.0f);
            this["lightsalmon"] = new Color(255.0f / 255.0f, 160.0f / 255.0f, 122.0f / 255.0f);
            this["lightseagreen"] = new Color(32.0f / 255.0f, 178.0f / 255.0f, 170.0f / 255.0f);
            this["lightskyblue"] = new Color(135.0f / 255.0f, 206.0f / 255.0f, 250.0f / 255.0f);
            this["lightslategray"] = new Color(119.0f / 255.0f, 136.0f / 255.0f, 153.0f / 255.0f);
            this["lightslategrey"] = new Color(119.0f / 255.0f, 136.0f / 255.0f, 153.0f / 255.0f);
            this["lightsteelblue"] = new Color(176.0f / 255.0f, 196.0f / 255.0f, 222.0f / 255.0f);
            this["lightyellow"] = new Color(255.0f / 255.0f, 255.0f / 255.0f, 224.0f / 255.0f);
            this["lime"] = new Color(0.0f / 255.0f, 255.0f / 255.0f, 0.0f / 255.0f);
            this["limegreen"] = new Color(50.0f / 255.0f, 205.0f / 255.0f, 50.0f / 255.0f);
            this["linen"] = new Color(250.0f / 255.0f, 240.0f / 255.0f, 230.0f / 255.0f);
            this["magenta"] = new Color(255.0f / 255.0f, 0.0f / 255.0f, 255.0f / 255.0f);
            this["maroon"] = new Color(128.0f / 255.0f, 0.0f / 255.0f, 0.0f / 255.0f);
            this["mediumaquamarine"] = new Color(102.0f / 255.0f, 205.0f / 255.0f, 170.0f / 255.0f);
            this["mediumblue"] = new Color(0.0f / 255.0f, 0.0f / 255.0f, 205.0f / 255.0f);
            this["mediumorchid"] = new Color(186.0f / 255.0f, 85.0f / 255.0f, 211.0f / 255.0f);
            this["mediumpurple"] = new Color(147.0f / 255.0f, 112.0f / 255.0f, 219.0f / 255.0f);
            this["mediumseagreen"] = new Color(60.0f / 255.0f, 179.0f / 255.0f, 113.0f / 255.0f);
            this["mediumslateblue"] = new Color(123.0f / 255.0f, 104.0f / 255.0f, 238.0f / 255.0f);
            this["mediumspringgreen"] = new Color(0.0f / 255.0f, 250.0f / 255.0f, 154.0f / 255.0f);
            this["mediumturquoise"] = new Color(72.0f / 255.0f, 209.0f / 255.0f, 204.0f / 255.0f);
            this["mediumvioletred"] = new Color(199.0f / 255.0f, 21.0f / 255.0f, 133.0f / 255.0f);
            this["midnightblue"] = new Color(25.0f / 255.0f, 25.0f / 255.0f, 112.0f / 255.0f);
            this["mintcream"] = new Color(245.0f / 255.0f, 255.0f / 255.0f, 250.0f / 255.0f);
            this["mistyrose"] = new Color(255.0f / 255.0f, 228.0f / 255.0f, 225.0f / 255.0f);
            this["moccasin"] = new Color(255.0f / 255.0f, 228.0f / 255.0f, 181.0f / 255.0f);
            this["navajowhite"] = new Color(255.0f / 255.0f, 222.0f / 255.0f, 173.0f / 255.0f);
            this["navy"] = new Color(0.0f / 255.0f, 0.0f / 255.0f, 128.0f / 255.0f);
            this["oldlace"] = new Color(253.0f / 255.0f, 245.0f / 255.0f, 230.0f / 255.0f);
            this["olive"] = new Color(128.0f / 255.0f, 128.0f / 255.0f, 0.0f / 255.0f);
            this["olivedrab"] = new Color(107.0f / 255.0f, 142.0f / 255.0f, 35.0f / 255.0f);
            this["orange"] = new Color(255.0f / 255.0f, 165.0f / 255.0f, 0.0f / 255.0f);
            this["orangered"] = new Color(255.0f / 255.0f, 69.0f / 255.0f, 0.0f / 255.0f);
            this["orchid"] = new Color(218.0f / 255.0f, 112.0f / 255.0f, 214.0f / 255.0f);
            this["palegoldenrod"] = new Color(238.0f / 255.0f, 232.0f / 255.0f, 170.0f / 255.0f);
            this["palegreen"] = new Color(152.0f / 255.0f, 251.0f / 255.0f, 152.0f / 255.0f);
            this["paleturquoise"] = new Color(175.0f / 255.0f, 238.0f / 255.0f, 238.0f / 255.0f);
            this["palevioletred"] = new Color(219.0f / 255.0f, 112.0f / 255.0f, 147.0f / 255.0f);
            this["papayawhip"] = new Color(255.0f / 255.0f, 239.0f / 255.0f, 213.0f / 255.0f);
            this["peachpuff"] = new Color(255.0f / 255.0f, 218.0f / 255.0f, 185.0f / 255.0f);
            this["peru"] = new Color(205.0f / 255.0f, 133.0f / 255.0f, 63.0f / 255.0f);
            this["pink"] = new Color(255.0f / 255.0f, 192.0f / 255.0f, 203.0f / 255.0f);
            this["plum"] = new Color(221.0f / 255.0f, 160.0f / 255.0f, 221.0f / 255.0f);
            this["powderblue"] = new Color(176.0f / 255.0f, 224.0f / 255.0f, 230.0f / 255.0f);
            this["purple"] = new Color(128.0f / 255.0f, 0.0f / 255.0f, 128.0f / 255.0f);
            this["rebeccapurple"] = new Color(102.0f / 255.0f, 51.0f / 255.0f, 153.0f / 255.0f);
            this["red"] = new Color(255.0f / 255.0f, 0.0f / 255.0f, 0.0f / 255.0f);
            this["rosybrown"] = new Color(188.0f / 255.0f, 143.0f / 255.0f, 143.0f / 255.0f);
            this["royalblue"] = new Color(65.0f / 255.0f, 105.0f / 255.0f, 225.0f / 255.0f);
            this["saddlebrown"] = new Color(139.0f / 255.0f, 69.0f / 255.0f, 19.0f / 255.0f);
            this["salmon"] = new Color(250.0f / 255.0f, 128.0f / 255.0f, 114.0f / 255.0f);
            this["sandybrown"] = new Color(244.0f / 255.0f, 164.0f / 255.0f, 96.0f / 255.0f);
            this["seagreen"] = new Color(46.0f / 255.0f, 139.0f / 255.0f, 87.0f / 255.0f);
            this["seashell"] = new Color(255.0f / 255.0f, 245.0f / 255.0f, 238.0f / 255.0f);
            this["sienna"] = new Color(160.0f / 255.0f, 82.0f / 255.0f, 45.0f / 255.0f);
            this["silver"] = new Color(192.0f / 255.0f, 192.0f / 255.0f, 192.0f / 255.0f);
            this["skyblue"] = new Color(135.0f / 255.0f, 206.0f / 255.0f, 235.0f / 255.0f);
            this["slateblue"] = new Color(106.0f / 255.0f, 90.0f / 255.0f, 205.0f / 255.0f);
            this["slategray"] = new Color(112.0f / 255.0f, 128.0f / 255.0f, 144.0f / 255.0f);
            this["slategrey"] = new Color(112.0f / 255.0f, 128.0f / 255.0f, 144.0f / 255.0f);
            this["snow"] = new Color(255.0f / 255.0f, 250.0f / 255.0f, 250.0f / 255.0f);
            this["springgreen"] = new Color(0.0f / 255.0f, 255.0f / 255.0f, 127.0f / 255.0f);
            this["steelblue"] = new Color(70.0f / 255.0f, 130.0f / 255.0f, 180.0f / 255.0f);
            this["tan"] = new Color(210.0f / 255.0f, 180.0f / 255.0f, 140.0f / 255.0f);
            this["teal"] = new Color(0.0f / 255.0f, 128.0f / 255.0f, 128.0f / 255.0f);
            this["thistle"] = new Color(216.0f / 255.0f, 191.0f / 255.0f, 216.0f / 255.0f);
            this["tomato"] = new Color(255.0f / 255.0f, 99.0f / 255.0f, 71.0f / 255.0f);
            this["turquoise"] = new Color(64.0f / 255.0f, 224.0f / 255.0f, 208.0f / 255.0f);
            this["violet"] = new Color(238.0f / 255.0f, 130.0f / 255.0f, 238.0f / 255.0f);
            this["wheat"] = new Color(245.0f / 255.0f, 222.0f / 255.0f, 179.0f / 255.0f);
            this["white"] = new Color(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
            this["whitesmoke"] = new Color(245.0f / 255.0f, 245.0f / 255.0f, 245.0f / 255.0f);
            this["yellow"] = new Color(255.0f / 255.0f, 255.0f / 255.0f, 0.0f / 255.0f);
            this["yellowgreen"] = new Color(154.0f / 255.0f, 205.0f / 255.0f, 50.0f / 255.0f);
        }
    } // The boring NamedWebColorDictionary class
} // namespace
