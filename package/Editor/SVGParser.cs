using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Linq;
using UnityEngine;

namespace Unity.VectorGraphics.Editor
{
    class SVGParser
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

    class XmlReaderIterator
    {
        public class Node
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

    class SVGFormatException : Exception
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

    class SVGDictionary : Dictionary<string, object> {}

    class SVGDocument
    {
        public SVGDocument(XmlReader docReader, float dpi, Scene scene, int windowWidth, int windowHeight)
        {
            allElems = new ElemHandler[]
            { circle, defs, ellipse, g, image, line, linearGradient, path, polygon, polyline, radialGradient, clipPath, rect, symbol, use, style };

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

            var pathProps = new PathProperties() { stroke = stroke, head = strokeEnding, tail = strokeEnding, corners = strokeCorner };
            var rect = new Rectangle() { pathProps = pathProps, fill = fill };
            VectorUtils.MakeCircle(rect, new Vector2(cx, cy), r);
            sceneNode.drawables = new List<IDrawable>(1);
            sceneNode.drawables.Add(rect);

            ParseClip(node, sceneNode);

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

            var pathProps = new PathProperties() { stroke = stroke, corners = strokeCorner, head = strokeEnding, tail = strokeEnding };
            var rect = new Rectangle() { pathProps = pathProps, fill = fill };
            VectorUtils.MakeEllipse(rect, new Vector2(cx, cy), rx, ry);
            sceneNode.drawables = new List<IDrawable>(1);
            sceneNode.drawables.Add(rect);

            ParseClip(node, sceneNode);

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

            ParseClip(node, sceneNode);

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

                ParseClip(node, sceneNode);
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

            ParseClip(node, sceneNode);

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

            ParseClip(node, sceneNode);

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

            ParseClip(node, sceneNode);

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

            ParseClip(node, sceneNode);

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
            var clipRoot = new SceneNode(); // A new scene node instead of one precreated for us
            clipRoot.transform = Matrix2D.identity;

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

            AddToSVGDictionaryIfPossible(node, clipRoot);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);

            currentSceneNode.Push(clipRoot);
            ParseChildren(node, node.Name);
            if (currentSceneNode.Pop() != clipRoot)
                throw SVGFormatException.StackError;
            
            clipData[clipRoot] = new ClipData() { worldRelative = relativeToWorld };
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

            ParseClip(node, sceneNode);

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

            ParseClip(node, sceneNode);
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

            ParseClip(node, sceneNode);

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

        void ParseClip(XmlReaderIterator.Node node, SceneNode sceneNode)
        {
            string reference = null;
            string clipPath = node["clip-path"];
            if (clipPath != null)
                reference = SVGAttribParser.ParseURLRef(clipPath);

            if (reference != null)
            {
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
        void PostProcess()
        {
            // Adjust gradient fills on all objects
            foreach (var nodeInfo in VectorUtils.WorldTransformedSceneNodes(scene.root, nodeOpacity))
            {
                if (nodeInfo.node.drawables == null)
                    continue;
                foreach (var drawable in nodeInfo.node.drawables)
                {
                    Filled filled = drawable as Filled;
                    if (filled != null)
                        AdjustFill(nodeInfo.node, nodeInfo.worldTransform, filled);
                }
            }
        }

        void AdjustFill(SceneNode node, Matrix2D worldTransform, Filled filledObj)
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

        void AdjustPath(SceneNode node, BezierContour contour, PathProperties pathProps)
        {
            if ((pathProps.stroke == null) || (pathProps.stroke.pattern == null) ||
                (pathProps.stroke.pattern.Length == 0) || (pathProps.stroke.pattern[0] >= 0.0f))
                return;
            int patternCount = pathProps.stroke.pattern.Length;
            float pathLength = VectorUtils.SegmentsLength(contour.segments, contour.closed);
            for (int i = 0; i < patternCount; i++)
                pathProps.stroke.pattern[i] = -pathProps.stroke.pattern[i] * pathLength;
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

    class SVGAttribParser
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
            return namedColors[colorString];
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
            this["snow"] = new Color(255 / 255.0f, 250 / 255.0f, 250 / 255.0f);
            this["ghost white"] = new Color(248 / 255.0f, 248 / 255.0f, 255 / 255.0f);
            this["GhostWhite"] = new Color(248 / 255.0f, 248 / 255.0f, 255 / 255.0f);
            this["white smoke"] = new Color(245 / 255.0f, 245 / 255.0f, 245 / 255.0f);
            this["WhiteSmoke"] = new Color(245 / 255.0f, 245 / 255.0f, 245 / 255.0f);
            this["gainsboro"] = new Color(220 / 255.0f, 220 / 255.0f, 220 / 255.0f);
            this["floral white"] = new Color(255 / 255.0f, 250 / 255.0f, 240 / 255.0f);
            this["FloralWhite"] = new Color(255 / 255.0f, 250 / 255.0f, 240 / 255.0f);
            this["old lace"] = new Color(253 / 255.0f, 245 / 255.0f, 230 / 255.0f);
            this["OldLace"] = new Color(253 / 255.0f, 245 / 255.0f, 230 / 255.0f);
            this["linen"] = new Color(250 / 255.0f, 240 / 255.0f, 230 / 255.0f);
            this["antique white"] = new Color(250 / 255.0f, 235 / 255.0f, 215 / 255.0f);
            this["AntiqueWhite"] = new Color(250 / 255.0f, 235 / 255.0f, 215 / 255.0f);
            this["papaya whip"] = new Color(255 / 255.0f, 239 / 255.0f, 213 / 255.0f);
            this["PapayaWhip"] = new Color(255 / 255.0f, 239 / 255.0f, 213 / 255.0f);
            this["blanched almond"] = new Color(255 / 255.0f, 235 / 255.0f, 205 / 255.0f);
            this["BlanchedAlmond"] = new Color(255 / 255.0f, 235 / 255.0f, 205 / 255.0f);
            this["bisque"] = new Color(255 / 255.0f, 228 / 255.0f, 196 / 255.0f);
            this["peach puff"] = new Color(255 / 255.0f, 218 / 255.0f, 185 / 255.0f);
            this["PeachPuff"] = new Color(255 / 255.0f, 218 / 255.0f, 185 / 255.0f);
            this["navajo white"] = new Color(255 / 255.0f, 222 / 255.0f, 173 / 255.0f);
            this["NavajoWhite"] = new Color(255 / 255.0f, 222 / 255.0f, 173 / 255.0f);
            this["moccasin"] = new Color(255 / 255.0f, 228 / 255.0f, 181 / 255.0f);
            this["cornsilk"] = new Color(255 / 255.0f, 248 / 255.0f, 220 / 255.0f);
            this["ivory"] = new Color(255 / 255.0f, 255 / 255.0f, 240 / 255.0f);
            this["lemon chiffon"] = new Color(255 / 255.0f, 250 / 255.0f, 205 / 255.0f);
            this["LemonChiffon"] = new Color(255 / 255.0f, 250 / 255.0f, 205 / 255.0f);
            this["seashell"] = new Color(255 / 255.0f, 245 / 255.0f, 238 / 255.0f);
            this["honeydew"] = new Color(240 / 255.0f, 255 / 255.0f, 240 / 255.0f);
            this["mint cream"] = new Color(245 / 255.0f, 255 / 255.0f, 250 / 255.0f);
            this["MintCream"] = new Color(245 / 255.0f, 255 / 255.0f, 250 / 255.0f);
            this["azure"] = new Color(240 / 255.0f, 255 / 255.0f, 255 / 255.0f);
            this["alice blue"] = new Color(240 / 255.0f, 248 / 255.0f, 255 / 255.0f);
            this["AliceBlue"] = new Color(240 / 255.0f, 248 / 255.0f, 255 / 255.0f);
            this["lavender"] = new Color(230 / 255.0f, 230 / 255.0f, 250 / 255.0f);
            this["lavender blush"] = new Color(255 / 255.0f, 240 / 255.0f, 245 / 255.0f);
            this["LavenderBlush"] = new Color(255 / 255.0f, 240 / 255.0f, 245 / 255.0f);
            this["misty rose"] = new Color(255 / 255.0f, 228 / 255.0f, 225 / 255.0f);
            this["MistyRose"] = new Color(255 / 255.0f, 228 / 255.0f, 225 / 255.0f);
            this["white"] = new Color(255 / 255.0f, 255 / 255.0f, 255 / 255.0f);
            this["black"] = new Color(0 / 255.0f, 0 / 255.0f, 0 / 255.0f);
            this["dark slate"] = new Color(47 / 255.0f, 79 / 255.0f, 79 / 255.0f);
            this["DarkSlateGray"] = new Color(47 / 255.0f, 79 / 255.0f, 79 / 255.0f);
            this["dark slate"] = new Color(47 / 255.0f, 79 / 255.0f, 79 / 255.0f);
            this["DarkSlateGrey"] = new Color(47 / 255.0f, 79 / 255.0f, 79 / 255.0f);
            this["dim gray"] = new Color(105 / 255.0f, 105 / 255.0f, 105 / 255.0f);
            this["DimGray"] = new Color(105 / 255.0f, 105 / 255.0f, 105 / 255.0f);
            this["dim grey"] = new Color(105 / 255.0f, 105 / 255.0f, 105 / 255.0f);
            this["DimGrey"] = new Color(105 / 255.0f, 105 / 255.0f, 105 / 255.0f);
            this["slate gray"] = new Color(112 / 255.0f, 128 / 255.0f, 144 / 255.0f);
            this["SlateGray"] = new Color(112 / 255.0f, 128 / 255.0f, 144 / 255.0f);
            this["slate grey"] = new Color(112 / 255.0f, 128 / 255.0f, 144 / 255.0f);
            this["SlateGrey"] = new Color(112 / 255.0f, 128 / 255.0f, 144 / 255.0f);
            this["light slate"] = new Color(119 / 255.0f, 136 / 255.0f, 153 / 255.0f);
            this["LightSlateGray"] = new Color(119 / 255.0f, 136 / 255.0f, 153 / 255.0f);
            this["light slate"] = new Color(119 / 255.0f, 136 / 255.0f, 153 / 255.0f);
            this["LightSlateGrey"] = new Color(119 / 255.0f, 136 / 255.0f, 153 / 255.0f);
            this["gray"] = new Color(190 / 255.0f, 190 / 255.0f, 190 / 255.0f);
            this["grey"] = new Color(190 / 255.0f, 190 / 255.0f, 190 / 255.0f);
            this["light grey"] = new Color(211 / 255.0f, 211 / 255.0f, 211 / 255.0f);
            this["LightGrey"] = new Color(211 / 255.0f, 211 / 255.0f, 211 / 255.0f);
            this["light gray"] = new Color(211 / 255.0f, 211 / 255.0f, 211 / 255.0f);
            this["LightGray"] = new Color(211 / 255.0f, 211 / 255.0f, 211 / 255.0f);
            this["midnight blue"] = new Color(25 / 255.0f, 25 / 255.0f, 112 / 255.0f);
            this["MidnightBlue"] = new Color(25 / 255.0f, 25 / 255.0f, 112 / 255.0f);
            this["navy"] = new Color(0 / 255.0f, 0 / 255.0f, 128 / 255.0f);
            this["navy blue"] = new Color(0 / 255.0f, 0 / 255.0f, 128 / 255.0f);
            this["NavyBlue"] = new Color(0 / 255.0f, 0 / 255.0f, 128 / 255.0f);
            this["cornflower blue"] = new Color(100 / 255.0f, 149 / 255.0f, 237 / 255.0f);
            this["CornflowerBlue"] = new Color(100 / 255.0f, 149 / 255.0f, 237 / 255.0f);
            this["dark slate"] = new Color(72 / 255.0f, 61 / 255.0f, 139 / 255.0f);
            this["DarkSlateBlue"] = new Color(72 / 255.0f, 61 / 255.0f, 139 / 255.0f);
            this["slate blue"] = new Color(106 / 255.0f, 90 / 255.0f, 205 / 255.0f);
            this["SlateBlue"] = new Color(106 / 255.0f, 90 / 255.0f, 205 / 255.0f);
            this["medium slate"] = new Color(123 / 255.0f, 104 / 255.0f, 238 / 255.0f);
            this["MediumSlateBlue"] = new Color(123 / 255.0f, 104 / 255.0f, 238 / 255.0f);
            this["light slate"] = new Color(132 / 255.0f, 112 / 255.0f, 255 / 255.0f);
            this["LightSlateBlue"] = new Color(132 / 255.0f, 112 / 255.0f, 255 / 255.0f);
            this["medium blue"] = new Color(0 / 255.0f, 0 / 255.0f, 205 / 255.0f);
            this["MediumBlue"] = new Color(0 / 255.0f, 0 / 255.0f, 205 / 255.0f);
            this["royal blue"] = new Color(65 / 255.0f, 105 / 255.0f, 225 / 255.0f);
            this["RoyalBlue"] = new Color(65 / 255.0f, 105 / 255.0f, 225 / 255.0f);
            this["blue"] = new Color(0 / 255.0f, 0 / 255.0f, 255 / 255.0f);
            this["dodger blue"] = new Color(30 / 255.0f, 144 / 255.0f, 255 / 255.0f);
            this["DodgerBlue"] = new Color(30 / 255.0f, 144 / 255.0f, 255 / 255.0f);
            this["deep sky"] = new Color(0 / 255.0f, 191 / 255.0f, 255 / 255.0f);
            this["DeepSkyBlue"] = new Color(0 / 255.0f, 191 / 255.0f, 255 / 255.0f);
            this["sky blue"] = new Color(135 / 255.0f, 206 / 255.0f, 235 / 255.0f);
            this["SkyBlue"] = new Color(135 / 255.0f, 206 / 255.0f, 235 / 255.0f);
            this["light sky"] = new Color(135 / 255.0f, 206 / 255.0f, 250 / 255.0f);
            this["LightSkyBlue"] = new Color(135 / 255.0f, 206 / 255.0f, 250 / 255.0f);
            this["steel blue"] = new Color(70 / 255.0f, 130 / 255.0f, 180 / 255.0f);
            this["SteelBlue"] = new Color(70 / 255.0f, 130 / 255.0f, 180 / 255.0f);
            this["light steel"] = new Color(176 / 255.0f, 196 / 255.0f, 222 / 255.0f);
            this["LightSteelBlue"] = new Color(176 / 255.0f, 196 / 255.0f, 222 / 255.0f);
            this["light blue"] = new Color(173 / 255.0f, 216 / 255.0f, 230 / 255.0f);
            this["LightBlue"] = new Color(173 / 255.0f, 216 / 255.0f, 230 / 255.0f);
            this["powder blue"] = new Color(176 / 255.0f, 224 / 255.0f, 230 / 255.0f);
            this["PowderBlue"] = new Color(176 / 255.0f, 224 / 255.0f, 230 / 255.0f);
            this["pale turquoise"] = new Color(175 / 255.0f, 238 / 255.0f, 238 / 255.0f);
            this["PaleTurquoise"] = new Color(175 / 255.0f, 238 / 255.0f, 238 / 255.0f);
            this["dark turquoise"] = new Color(0 / 255.0f, 206 / 255.0f, 209 / 255.0f);
            this["DarkTurquoise"] = new Color(0 / 255.0f, 206 / 255.0f, 209 / 255.0f);
            this["medium turquoise"] = new Color(72 / 255.0f, 209 / 255.0f, 204 / 255.0f);
            this["MediumTurquoise"] = new Color(72 / 255.0f, 209 / 255.0f, 204 / 255.0f);
            this["turquoise"] = new Color(64 / 255.0f, 224 / 255.0f, 208 / 255.0f);
            this["cyan"] = new Color(0 / 255.0f, 255 / 255.0f, 255 / 255.0f);
            this["light cyan"] = new Color(224 / 255.0f, 255 / 255.0f, 255 / 255.0f);
            this["LightCyan"] = new Color(224 / 255.0f, 255 / 255.0f, 255 / 255.0f);
            this["cadet blue"] = new Color(95 / 255.0f, 158 / 255.0f, 160 / 255.0f);
            this["CadetBlue"] = new Color(95 / 255.0f, 158 / 255.0f, 160 / 255.0f);
            this["medium aquamarine"] = new Color(102 / 255.0f, 205 / 255.0f, 170 / 255.0f);
            this["MediumAquamarine"] = new Color(102 / 255.0f, 205 / 255.0f, 170 / 255.0f);
            this["aquamarine"] = new Color(127 / 255.0f, 255 / 255.0f, 212 / 255.0f);
            this["dark green"] = new Color(0 / 255.0f, 100 / 255.0f, 0 / 255.0f);
            this["DarkGreen"] = new Color(0 / 255.0f, 100 / 255.0f, 0 / 255.0f);
            this["dark olive"] = new Color(85 / 255.0f, 107 / 255.0f, 47 / 255.0f);
            this["DarkOliveGreen"] = new Color(85 / 255.0f, 107 / 255.0f, 47 / 255.0f);
            this["dark sea"] = new Color(143 / 255.0f, 188 / 255.0f, 143 / 255.0f);
            this["DarkSeaGreen"] = new Color(143 / 255.0f, 188 / 255.0f, 143 / 255.0f);
            this["sea green"] = new Color(46 / 255.0f, 139 / 255.0f, 87 / 255.0f);
            this["SeaGreen"] = new Color(46 / 255.0f, 139 / 255.0f, 87 / 255.0f);
            this["medium sea"] = new Color(60 / 255.0f, 179 / 255.0f, 113 / 255.0f);
            this["MediumSeaGreen"] = new Color(60 / 255.0f, 179 / 255.0f, 113 / 255.0f);
            this["light sea"] = new Color(32 / 255.0f, 178 / 255.0f, 170 / 255.0f);
            this["LightSeaGreen"] = new Color(32 / 255.0f, 178 / 255.0f, 170 / 255.0f);
            this["pale green"] = new Color(152 / 255.0f, 251 / 255.0f, 152 / 255.0f);
            this["PaleGreen"] = new Color(152 / 255.0f, 251 / 255.0f, 152 / 255.0f);
            this["spring green"] = new Color(0 / 255.0f, 255 / 255.0f, 127 / 255.0f);
            this["SpringGreen"] = new Color(0 / 255.0f, 255 / 255.0f, 127 / 255.0f);
            this["lawn green"] = new Color(124 / 255.0f, 252 / 255.0f, 0 / 255.0f);
            this["LawnGreen"] = new Color(124 / 255.0f, 252 / 255.0f, 0 / 255.0f);
            this["green"] = new Color(0 / 255.0f, 255 / 255.0f, 0 / 255.0f);
            this["chartreuse"] = new Color(127 / 255.0f, 255 / 255.0f, 0 / 255.0f);
            this["medium spring"] = new Color(0 / 255.0f, 250 / 255.0f, 154 / 255.0f);
            this["MediumSpringGreen"] = new Color(0 / 255.0f, 250 / 255.0f, 154 / 255.0f);
            this["green yellow"] = new Color(173 / 255.0f, 255 / 255.0f, 47 / 255.0f);
            this["GreenYellow"] = new Color(173 / 255.0f, 255 / 255.0f, 47 / 255.0f);
            this["lime"] = new Color(0 / 255.0f, 255 / 255.0f, 0 / 255.0f);
            this["lime green"] = new Color(50 / 255.0f, 205 / 255.0f, 50 / 255.0f);
            this["LimeGreen"] = new Color(50 / 255.0f, 205 / 255.0f, 50 / 255.0f);
            this["yellow green"] = new Color(154 / 255.0f, 205 / 255.0f, 50 / 255.0f);
            this["YellowGreen"] = new Color(154 / 255.0f, 205 / 255.0f, 50 / 255.0f);
            this["forest green"] = new Color(34 / 255.0f, 139 / 255.0f, 34 / 255.0f);
            this["ForestGreen"] = new Color(34 / 255.0f, 139 / 255.0f, 34 / 255.0f);
            this["olive drab"] = new Color(107 / 255.0f, 142 / 255.0f, 35 / 255.0f);
            this["OliveDrab"] = new Color(107 / 255.0f, 142 / 255.0f, 35 / 255.0f);
            this["dark khaki"] = new Color(189 / 255.0f, 183 / 255.0f, 107 / 255.0f);
            this["DarkKhaki"] = new Color(189 / 255.0f, 183 / 255.0f, 107 / 255.0f);
            this["khaki"] = new Color(240 / 255.0f, 230 / 255.0f, 140 / 255.0f);
            this["pale goldenrod"] = new Color(238 / 255.0f, 232 / 255.0f, 170 / 255.0f);
            this["PaleGoldenrod"] = new Color(238 / 255.0f, 232 / 255.0f, 170 / 255.0f);
            this["light goldenrod"] = new Color(250 / 255.0f, 250 / 255.0f, 210 / 255.0f);
            this["LightGoldenrodYellow"] = new Color(250 / 255.0f, 250 / 255.0f, 210 / 255.0f);
            this["light yellow"] = new Color(255 / 255.0f, 255 / 255.0f, 224 / 255.0f);
            this["LightYellow"] = new Color(255 / 255.0f, 255 / 255.0f, 224 / 255.0f);
            this["yellow"] = new Color(255 / 255.0f, 255 / 255.0f, 0 / 255.0f);
            this["gold"] = new Color(255 / 255.0f, 215 / 255.0f, 0 / 255.0f);
            this["light goldenrod"] = new Color(238 / 255.0f, 221 / 255.0f, 130 / 255.0f);
            this["LightGoldenrod"] = new Color(238 / 255.0f, 221 / 255.0f, 130 / 255.0f);
            this["goldenrod"] = new Color(218 / 255.0f, 165 / 255.0f, 32 / 255.0f);
            this["dark goldenrod"] = new Color(184 / 255.0f, 134 / 255.0f, 11 / 255.0f);
            this["DarkGoldenrod"] = new Color(184 / 255.0f, 134 / 255.0f, 11 / 255.0f);
            this["rosy brown"] = new Color(188 / 255.0f, 143 / 255.0f, 143 / 255.0f);
            this["RosyBrown"] = new Color(188 / 255.0f, 143 / 255.0f, 143 / 255.0f);
            this["indian red"] = new Color(205 / 255.0f, 92 / 255.0f, 92 / 255.0f);
            this["IndianRed"] = new Color(205 / 255.0f, 92 / 255.0f, 92 / 255.0f);
            this["saddle brown"] = new Color(139 / 255.0f, 69 / 255.0f, 19 / 255.0f);
            this["SaddleBrown"] = new Color(139 / 255.0f, 69 / 255.0f, 19 / 255.0f);
            this["sienna"] = new Color(160 / 255.0f, 82 / 255.0f, 45 / 255.0f);
            this["peru"] = new Color(205 / 255.0f, 133 / 255.0f, 63 / 255.0f);
            this["burlywood"] = new Color(222 / 255.0f, 184 / 255.0f, 135 / 255.0f);
            this["beige"] = new Color(245 / 255.0f, 245 / 255.0f, 220 / 255.0f);
            this["wheat"] = new Color(245 / 255.0f, 222 / 255.0f, 179 / 255.0f);
            this["sandy brown"] = new Color(244 / 255.0f, 164 / 255.0f, 96 / 255.0f);
            this["SandyBrown"] = new Color(244 / 255.0f, 164 / 255.0f, 96 / 255.0f);
            this["tan"] = new Color(210 / 255.0f, 180 / 255.0f, 140 / 255.0f);
            this["chocolate"] = new Color(210 / 255.0f, 105 / 255.0f, 30 / 255.0f);
            this["firebrick"] = new Color(178 / 255.0f, 34 / 255.0f, 34 / 255.0f);
            this["brown"] = new Color(165 / 255.0f, 42 / 255.0f, 42 / 255.0f);
            this["dark salmon"] = new Color(233 / 255.0f, 150 / 255.0f, 122 / 255.0f);
            this["DarkSalmon"] = new Color(233 / 255.0f, 150 / 255.0f, 122 / 255.0f);
            this["salmon"] = new Color(250 / 255.0f, 128 / 255.0f, 114 / 255.0f);
            this["light salmon"] = new Color(255 / 255.0f, 160 / 255.0f, 122 / 255.0f);
            this["LightSalmon"] = new Color(255 / 255.0f, 160 / 255.0f, 122 / 255.0f);
            this["orange"] = new Color(255 / 255.0f, 165 / 255.0f, 0 / 255.0f);
            this["dark orange"] = new Color(255 / 255.0f, 140 / 255.0f, 0 / 255.0f);
            this["DarkOrange"] = new Color(255 / 255.0f, 140 / 255.0f, 0 / 255.0f);
            this["coral"] = new Color(255 / 255.0f, 127 / 255.0f, 80 / 255.0f);
            this["light coral"] = new Color(240 / 255.0f, 128 / 255.0f, 128 / 255.0f);
            this["LightCoral"] = new Color(240 / 255.0f, 128 / 255.0f, 128 / 255.0f);
            this["tomato"] = new Color(255 / 255.0f, 99 / 255.0f, 71 / 255.0f);
            this["orange red"] = new Color(255 / 255.0f, 69 / 255.0f, 0 / 255.0f);
            this["OrangeRed"] = new Color(255 / 255.0f, 69 / 255.0f, 0 / 255.0f);
            this["red"] = new Color(255 / 255.0f, 0 / 255.0f, 0 / 255.0f);
            this["hot pink"] = new Color(255 / 255.0f, 105 / 255.0f, 180 / 255.0f);
            this["HotPink"] = new Color(255 / 255.0f, 105 / 255.0f, 180 / 255.0f);
            this["deep pink"] = new Color(255 / 255.0f, 20 / 255.0f, 147 / 255.0f);
            this["DeepPink"] = new Color(255 / 255.0f, 20 / 255.0f, 147 / 255.0f);
            this["pink"] = new Color(255 / 255.0f, 192 / 255.0f, 203 / 255.0f);
            this["light pink"] = new Color(255 / 255.0f, 182 / 255.0f, 193 / 255.0f);
            this["LightPink"] = new Color(255 / 255.0f, 182 / 255.0f, 193 / 255.0f);
            this["pale violet"] = new Color(219 / 255.0f, 112 / 255.0f, 147 / 255.0f);
            this["PaleVioletRed"] = new Color(219 / 255.0f, 112 / 255.0f, 147 / 255.0f);
            this["maroon"] = new Color(176 / 255.0f, 48 / 255.0f, 96 / 255.0f);
            this["medium violet"] = new Color(199 / 255.0f, 21 / 255.0f, 133 / 255.0f);
            this["MediumVioletRed"] = new Color(199 / 255.0f, 21 / 255.0f, 133 / 255.0f);
            this["violet red"] = new Color(208 / 255.0f, 32 / 255.0f, 144 / 255.0f);
            this["VioletRed"] = new Color(208 / 255.0f, 32 / 255.0f, 144 / 255.0f);
            this["magenta"] = new Color(255 / 255.0f, 0 / 255.0f, 255 / 255.0f);
            this["violet"] = new Color(238 / 255.0f, 130 / 255.0f, 238 / 255.0f);
            this["plum"] = new Color(221 / 255.0f, 160 / 255.0f, 221 / 255.0f);
            this["orchid"] = new Color(218 / 255.0f, 112 / 255.0f, 214 / 255.0f);
            this["medium orchid"] = new Color(186 / 255.0f, 85 / 255.0f, 211 / 255.0f);
            this["MediumOrchid"] = new Color(186 / 255.0f, 85 / 255.0f, 211 / 255.0f);
            this["dark orchid"] = new Color(153 / 255.0f, 50 / 255.0f, 204 / 255.0f);
            this["DarkOrchid"] = new Color(153 / 255.0f, 50 / 255.0f, 204 / 255.0f);
            this["dark violet"] = new Color(148 / 255.0f, 0 / 255.0f, 211 / 255.0f);
            this["DarkViolet"] = new Color(148 / 255.0f, 0 / 255.0f, 211 / 255.0f);
            this["blue violet"] = new Color(138 / 255.0f, 43 / 255.0f, 226 / 255.0f);
            this["BlueViolet"] = new Color(138 / 255.0f, 43 / 255.0f, 226 / 255.0f);
            this["purple"] = new Color(160 / 255.0f, 32 / 255.0f, 240 / 255.0f);
            this["medium purple"] = new Color(147 / 255.0f, 112 / 255.0f, 219 / 255.0f);
            this["MediumPurple"] = new Color(147 / 255.0f, 112 / 255.0f, 219 / 255.0f);
            this["thistle"] = new Color(216 / 255.0f, 191 / 255.0f, 216 / 255.0f);
            this["snow1"] = new Color(255 / 255.0f, 250 / 255.0f, 250 / 255.0f);
            this["snow2"] = new Color(238 / 255.0f, 233 / 255.0f, 233 / 255.0f);
            this["snow3"] = new Color(205 / 255.0f, 201 / 255.0f, 201 / 255.0f);
            this["snow4"] = new Color(139 / 255.0f, 137 / 255.0f, 137 / 255.0f);
            this["seashell1"] = new Color(255 / 255.0f, 245 / 255.0f, 238 / 255.0f);
            this["seashell2"] = new Color(238 / 255.0f, 229 / 255.0f, 222 / 255.0f);
            this["seashell3"] = new Color(205 / 255.0f, 197 / 255.0f, 191 / 255.0f);
            this["seashell4"] = new Color(139 / 255.0f, 134 / 255.0f, 130 / 255.0f);
            this["AntiqueWhite1"] = new Color(255 / 255.0f, 239 / 255.0f, 219 / 255.0f);
            this["AntiqueWhite2"] = new Color(238 / 255.0f, 223 / 255.0f, 204 / 255.0f);
            this["AntiqueWhite3"] = new Color(205 / 255.0f, 192 / 255.0f, 176 / 255.0f);
            this["AntiqueWhite4"] = new Color(139 / 255.0f, 131 / 255.0f, 120 / 255.0f);
            this["bisque1"] = new Color(255 / 255.0f, 228 / 255.0f, 196 / 255.0f);
            this["bisque2"] = new Color(238 / 255.0f, 213 / 255.0f, 183 / 255.0f);
            this["bisque3"] = new Color(205 / 255.0f, 183 / 255.0f, 158 / 255.0f);
            this["bisque4"] = new Color(139 / 255.0f, 125 / 255.0f, 107 / 255.0f);
            this["PeachPuff1"] = new Color(255 / 255.0f, 218 / 255.0f, 185 / 255.0f);
            this["PeachPuff2"] = new Color(238 / 255.0f, 203 / 255.0f, 173 / 255.0f);
            this["PeachPuff3"] = new Color(205 / 255.0f, 175 / 255.0f, 149 / 255.0f);
            this["PeachPuff4"] = new Color(139 / 255.0f, 119 / 255.0f, 101 / 255.0f);
            this["NavajoWhite1"] = new Color(255 / 255.0f, 222 / 255.0f, 173 / 255.0f);
            this["NavajoWhite2"] = new Color(238 / 255.0f, 207 / 255.0f, 161 / 255.0f);
            this["NavajoWhite3"] = new Color(205 / 255.0f, 179 / 255.0f, 139 / 255.0f);
            this["NavajoWhite4"] = new Color(139 / 255.0f, 121 / 255.0f, 94 / 255.0f);
            this["LemonChiffon1"] = new Color(255 / 255.0f, 250 / 255.0f, 205 / 255.0f);
            this["LemonChiffon2"] = new Color(238 / 255.0f, 233 / 255.0f, 191 / 255.0f);
            this["LemonChiffon3"] = new Color(205 / 255.0f, 201 / 255.0f, 165 / 255.0f);
            this["LemonChiffon4"] = new Color(139 / 255.0f, 137 / 255.0f, 112 / 255.0f);
            this["cornsilk1"] = new Color(255 / 255.0f, 248 / 255.0f, 220 / 255.0f);
            this["cornsilk2"] = new Color(238 / 255.0f, 232 / 255.0f, 205 / 255.0f);
            this["cornsilk3"] = new Color(205 / 255.0f, 200 / 255.0f, 177 / 255.0f);
            this["cornsilk4"] = new Color(139 / 255.0f, 136 / 255.0f, 120 / 255.0f);
            this["ivory1"] = new Color(255 / 255.0f, 255 / 255.0f, 240 / 255.0f);
            this["ivory2"] = new Color(238 / 255.0f, 238 / 255.0f, 224 / 255.0f);
            this["ivory3"] = new Color(205 / 255.0f, 205 / 255.0f, 193 / 255.0f);
            this["ivory4"] = new Color(139 / 255.0f, 139 / 255.0f, 131 / 255.0f);
            this["honeydew1"] = new Color(240 / 255.0f, 255 / 255.0f, 240 / 255.0f);
            this["honeydew2"] = new Color(224 / 255.0f, 238 / 255.0f, 224 / 255.0f);
            this["honeydew3"] = new Color(193 / 255.0f, 205 / 255.0f, 193 / 255.0f);
            this["honeydew4"] = new Color(131 / 255.0f, 139 / 255.0f, 131 / 255.0f);
            this["LavenderBlush1"] = new Color(255 / 255.0f, 240 / 255.0f, 245 / 255.0f);
            this["LavenderBlush2"] = new Color(238 / 255.0f, 224 / 255.0f, 229 / 255.0f);
            this["LavenderBlush3"] = new Color(205 / 255.0f, 193 / 255.0f, 197 / 255.0f);
            this["LavenderBlush4"] = new Color(139 / 255.0f, 131 / 255.0f, 134 / 255.0f);
            this["MistyRose1"] = new Color(255 / 255.0f, 228 / 255.0f, 225 / 255.0f);
            this["MistyRose2"] = new Color(238 / 255.0f, 213 / 255.0f, 210 / 255.0f);
            this["MistyRose3"] = new Color(205 / 255.0f, 183 / 255.0f, 181 / 255.0f);
            this["MistyRose4"] = new Color(139 / 255.0f, 125 / 255.0f, 123 / 255.0f);
            this["azure1"] = new Color(240 / 255.0f, 255 / 255.0f, 255 / 255.0f);
            this["azure2"] = new Color(224 / 255.0f, 238 / 255.0f, 238 / 255.0f);
            this["azure3"] = new Color(193 / 255.0f, 205 / 255.0f, 205 / 255.0f);
            this["azure4"] = new Color(131 / 255.0f, 139 / 255.0f, 139 / 255.0f);
            this["SlateBlue1"] = new Color(131 / 255.0f, 111 / 255.0f, 255 / 255.0f);
            this["SlateBlue2"] = new Color(122 / 255.0f, 103 / 255.0f, 238 / 255.0f);
            this["SlateBlue3"] = new Color(105 / 255.0f, 89 / 255.0f, 205 / 255.0f);
            this["SlateBlue4"] = new Color(71 / 255.0f, 60 / 255.0f, 139 / 255.0f);
            this["RoyalBlue1"] = new Color(72 / 255.0f, 118 / 255.0f, 255 / 255.0f);
            this["RoyalBlue2"] = new Color(67 / 255.0f, 110 / 255.0f, 238 / 255.0f);
            this["RoyalBlue3"] = new Color(58 / 255.0f, 95 / 255.0f, 205 / 255.0f);
            this["RoyalBlue4"] = new Color(39 / 255.0f, 64 / 255.0f, 139 / 255.0f);
            this["blue1"] = new Color(0 / 255.0f, 0 / 255.0f, 255 / 255.0f);
            this["blue2"] = new Color(0 / 255.0f, 0 / 255.0f, 238 / 255.0f);
            this["blue3"] = new Color(0 / 255.0f, 0 / 255.0f, 205 / 255.0f);
            this["blue4"] = new Color(0 / 255.0f, 0 / 255.0f, 139 / 255.0f);
            this["DodgerBlue1"] = new Color(30 / 255.0f, 144 / 255.0f, 255 / 255.0f);
            this["DodgerBlue2"] = new Color(28 / 255.0f, 134 / 255.0f, 238 / 255.0f);
            this["DodgerBlue3"] = new Color(24 / 255.0f, 116 / 255.0f, 205 / 255.0f);
            this["DodgerBlue4"] = new Color(16 / 255.0f, 78 / 255.0f, 139 / 255.0f);
            this["SteelBlue1"] = new Color(99 / 255.0f, 184 / 255.0f, 255 / 255.0f);
            this["SteelBlue2"] = new Color(92 / 255.0f, 172 / 255.0f, 238 / 255.0f);
            this["SteelBlue3"] = new Color(79 / 255.0f, 148 / 255.0f, 205 / 255.0f);
            this["SteelBlue4"] = new Color(54 / 255.0f, 100 / 255.0f, 139 / 255.0f);
            this["DeepSkyBlue1"] = new Color(0 / 255.0f, 191 / 255.0f, 255 / 255.0f);
            this["DeepSkyBlue2"] = new Color(0 / 255.0f, 178 / 255.0f, 238 / 255.0f);
            this["DeepSkyBlue3"] = new Color(0 / 255.0f, 154 / 255.0f, 205 / 255.0f);
            this["DeepSkyBlue4"] = new Color(0 / 255.0f, 104 / 255.0f, 139 / 255.0f);
            this["SkyBlue1"] = new Color(135 / 255.0f, 206 / 255.0f, 255 / 255.0f);
            this["SkyBlue2"] = new Color(126 / 255.0f, 192 / 255.0f, 238 / 255.0f);
            this["SkyBlue3"] = new Color(108 / 255.0f, 166 / 255.0f, 205 / 255.0f);
            this["SkyBlue4"] = new Color(74 / 255.0f, 112 / 255.0f, 139 / 255.0f);
            this["LightSkyBlue1"] = new Color(176 / 255.0f, 226 / 255.0f, 255 / 255.0f);
            this["LightSkyBlue2"] = new Color(164 / 255.0f, 211 / 255.0f, 238 / 255.0f);
            this["LightSkyBlue3"] = new Color(141 / 255.0f, 182 / 255.0f, 205 / 255.0f);
            this["LightSkyBlue4"] = new Color(96 / 255.0f, 123 / 255.0f, 139 / 255.0f);
            this["SlateGray1"] = new Color(198 / 255.0f, 226 / 255.0f, 255 / 255.0f);
            this["SlateGray2"] = new Color(185 / 255.0f, 211 / 255.0f, 238 / 255.0f);
            this["SlateGray3"] = new Color(159 / 255.0f, 182 / 255.0f, 205 / 255.0f);
            this["SlateGray4"] = new Color(108 / 255.0f, 123 / 255.0f, 139 / 255.0f);
            this["LightSteelBlue1"] = new Color(202 / 255.0f, 225 / 255.0f, 255 / 255.0f);
            this["LightSteelBlue2"] = new Color(188 / 255.0f, 210 / 255.0f, 238 / 255.0f);
            this["LightSteelBlue3"] = new Color(162 / 255.0f, 181 / 255.0f, 205 / 255.0f);
            this["LightSteelBlue4"] = new Color(110 / 255.0f, 123 / 255.0f, 139 / 255.0f);
            this["LightBlue1"] = new Color(191 / 255.0f, 239 / 255.0f, 255 / 255.0f);
            this["LightBlue2"] = new Color(178 / 255.0f, 223 / 255.0f, 238 / 255.0f);
            this["LightBlue3"] = new Color(154 / 255.0f, 192 / 255.0f, 205 / 255.0f);
            this["LightBlue4"] = new Color(104 / 255.0f, 131 / 255.0f, 139 / 255.0f);
            this["LightCyan1"] = new Color(224 / 255.0f, 255 / 255.0f, 255 / 255.0f);
            this["LightCyan2"] = new Color(209 / 255.0f, 238 / 255.0f, 238 / 255.0f);
            this["LightCyan3"] = new Color(180 / 255.0f, 205 / 255.0f, 205 / 255.0f);
            this["LightCyan4"] = new Color(122 / 255.0f, 139 / 255.0f, 139 / 255.0f);
            this["PaleTurquoise1"] = new Color(187 / 255.0f, 255 / 255.0f, 255 / 255.0f);
            this["PaleTurquoise2"] = new Color(174 / 255.0f, 238 / 255.0f, 238 / 255.0f);
            this["PaleTurquoise3"] = new Color(150 / 255.0f, 205 / 255.0f, 205 / 255.0f);
            this["PaleTurquoise4"] = new Color(102 / 255.0f, 139 / 255.0f, 139 / 255.0f);
            this["CadetBlue1"] = new Color(152 / 255.0f, 245 / 255.0f, 255 / 255.0f);
            this["CadetBlue2"] = new Color(142 / 255.0f, 229 / 255.0f, 238 / 255.0f);
            this["CadetBlue3"] = new Color(122 / 255.0f, 197 / 255.0f, 205 / 255.0f);
            this["CadetBlue4"] = new Color(83 / 255.0f, 134 / 255.0f, 139 / 255.0f);
            this["turquoise1"] = new Color(0 / 255.0f, 245 / 255.0f, 255 / 255.0f);
            this["turquoise2"] = new Color(0 / 255.0f, 229 / 255.0f, 238 / 255.0f);
            this["turquoise3"] = new Color(0 / 255.0f, 197 / 255.0f, 205 / 255.0f);
            this["turquoise4"] = new Color(0 / 255.0f, 134 / 255.0f, 139 / 255.0f);
            this["cyan1"] = new Color(0 / 255.0f, 255 / 255.0f, 255 / 255.0f);
            this["cyan2"] = new Color(0 / 255.0f, 238 / 255.0f, 238 / 255.0f);
            this["cyan3"] = new Color(0 / 255.0f, 205 / 255.0f, 205 / 255.0f);
            this["cyan4"] = new Color(0 / 255.0f, 139 / 255.0f, 139 / 255.0f);
            this["DarkSlateGray1"] = new Color(151 / 255.0f, 255 / 255.0f, 255 / 255.0f);
            this["DarkSlateGray2"] = new Color(141 / 255.0f, 238 / 255.0f, 238 / 255.0f);
            this["DarkSlateGray3"] = new Color(121 / 255.0f, 205 / 255.0f, 205 / 255.0f);
            this["DarkSlateGray4"] = new Color(82 / 255.0f, 139 / 255.0f, 139 / 255.0f);
            this["aquamarine1"] = new Color(127 / 255.0f, 255 / 255.0f, 212 / 255.0f);
            this["aquamarine2"] = new Color(118 / 255.0f, 238 / 255.0f, 198 / 255.0f);
            this["aquamarine3"] = new Color(102 / 255.0f, 205 / 255.0f, 170 / 255.0f);
            this["aquamarine4"] = new Color(69 / 255.0f, 139 / 255.0f, 116 / 255.0f);
            this["DarkSeaGreen1"] = new Color(193 / 255.0f, 255 / 255.0f, 193 / 255.0f);
            this["DarkSeaGreen2"] = new Color(180 / 255.0f, 238 / 255.0f, 180 / 255.0f);
            this["DarkSeaGreen3"] = new Color(155 / 255.0f, 205 / 255.0f, 155 / 255.0f);
            this["DarkSeaGreen4"] = new Color(105 / 255.0f, 139 / 255.0f, 105 / 255.0f);
            this["SeaGreen1"] = new Color(84 / 255.0f, 255 / 255.0f, 159 / 255.0f);
            this["SeaGreen2"] = new Color(78 / 255.0f, 238 / 255.0f, 148 / 255.0f);
            this["SeaGreen3"] = new Color(67 / 255.0f, 205 / 255.0f, 128 / 255.0f);
            this["SeaGreen4"] = new Color(46 / 255.0f, 139 / 255.0f, 87 / 255.0f);
            this["PaleGreen1"] = new Color(154 / 255.0f, 255 / 255.0f, 154 / 255.0f);
            this["PaleGreen2"] = new Color(144 / 255.0f, 238 / 255.0f, 144 / 255.0f);
            this["PaleGreen3"] = new Color(124 / 255.0f, 205 / 255.0f, 124 / 255.0f);
            this["PaleGreen4"] = new Color(84 / 255.0f, 139 / 255.0f, 84 / 255.0f);
            this["SpringGreen1"] = new Color(0 / 255.0f, 255 / 255.0f, 127 / 255.0f);
            this["SpringGreen2"] = new Color(0 / 255.0f, 238 / 255.0f, 118 / 255.0f);
            this["SpringGreen3"] = new Color(0 / 255.0f, 205 / 255.0f, 102 / 255.0f);
            this["SpringGreen4"] = new Color(0 / 255.0f, 139 / 255.0f, 69 / 255.0f);
            this["green1"] = new Color(0 / 255.0f, 255 / 255.0f, 0 / 255.0f);
            this["green2"] = new Color(0 / 255.0f, 238 / 255.0f, 0 / 255.0f);
            this["green3"] = new Color(0 / 255.0f, 205 / 255.0f, 0 / 255.0f);
            this["green4"] = new Color(0 / 255.0f, 139 / 255.0f, 0 / 255.0f);
            this["chartreuse1"] = new Color(127 / 255.0f, 255 / 255.0f, 0 / 255.0f);
            this["chartreuse2"] = new Color(118 / 255.0f, 238 / 255.0f, 0 / 255.0f);
            this["chartreuse3"] = new Color(102 / 255.0f, 205 / 255.0f, 0 / 255.0f);
            this["chartreuse4"] = new Color(69 / 255.0f, 139 / 255.0f, 0 / 255.0f);
            this["OliveDrab1"] = new Color(192 / 255.0f, 255 / 255.0f, 62 / 255.0f);
            this["OliveDrab2"] = new Color(179 / 255.0f, 238 / 255.0f, 58 / 255.0f);
            this["OliveDrab3"] = new Color(154 / 255.0f, 205 / 255.0f, 50 / 255.0f);
            this["OliveDrab4"] = new Color(105 / 255.0f, 139 / 255.0f, 34 / 255.0f);
            this["DarkOliveGreen1"] = new Color(202 / 255.0f, 255 / 255.0f, 112 / 255.0f);
            this["DarkOliveGreen2"] = new Color(188 / 255.0f, 238 / 255.0f, 104 / 255.0f);
            this["DarkOliveGreen3"] = new Color(162 / 255.0f, 205 / 255.0f, 90 / 255.0f);
            this["DarkOliveGreen4"] = new Color(110 / 255.0f, 139 / 255.0f, 61 / 255.0f);
            this["khaki1"] = new Color(255 / 255.0f, 246 / 255.0f, 143 / 255.0f);
            this["khaki2"] = new Color(238 / 255.0f, 230 / 255.0f, 133 / 255.0f);
            this["khaki3"] = new Color(205 / 255.0f, 198 / 255.0f, 115 / 255.0f);
            this["khaki4"] = new Color(139 / 255.0f, 134 / 255.0f, 78 / 255.0f);
            this["LightGoldenrod1"] = new Color(255 / 255.0f, 236 / 255.0f, 139 / 255.0f);
            this["LightGoldenrod2"] = new Color(238 / 255.0f, 220 / 255.0f, 130 / 255.0f);
            this["LightGoldenrod3"] = new Color(205 / 255.0f, 190 / 255.0f, 112 / 255.0f);
            this["LightGoldenrod4"] = new Color(139 / 255.0f, 129 / 255.0f, 76 / 255.0f);
            this["LightYellow1"] = new Color(255 / 255.0f, 255 / 255.0f, 224 / 255.0f);
            this["LightYellow2"] = new Color(238 / 255.0f, 238 / 255.0f, 209 / 255.0f);
            this["LightYellow3"] = new Color(205 / 255.0f, 205 / 255.0f, 180 / 255.0f);
            this["LightYellow4"] = new Color(139 / 255.0f, 139 / 255.0f, 122 / 255.0f);
            this["yellow1"] = new Color(255 / 255.0f, 255 / 255.0f, 0 / 255.0f);
            this["yellow2"] = new Color(238 / 255.0f, 238 / 255.0f, 0 / 255.0f);
            this["yellow3"] = new Color(205 / 255.0f, 205 / 255.0f, 0 / 255.0f);
            this["yellow4"] = new Color(139 / 255.0f, 139 / 255.0f, 0 / 255.0f);
            this["gold1"] = new Color(255 / 255.0f, 215 / 255.0f, 0 / 255.0f);
            this["gold2"] = new Color(238 / 255.0f, 201 / 255.0f, 0 / 255.0f);
            this["gold3"] = new Color(205 / 255.0f, 173 / 255.0f, 0 / 255.0f);
            this["gold4"] = new Color(139 / 255.0f, 117 / 255.0f, 0 / 255.0f);
            this["goldenrod1"] = new Color(255 / 255.0f, 193 / 255.0f, 37 / 255.0f);
            this["goldenrod2"] = new Color(238 / 255.0f, 180 / 255.0f, 34 / 255.0f);
            this["goldenrod3"] = new Color(205 / 255.0f, 155 / 255.0f, 29 / 255.0f);
            this["goldenrod4"] = new Color(139 / 255.0f, 105 / 255.0f, 20 / 255.0f);
            this["DarkGoldenrod1"] = new Color(255 / 255.0f, 185 / 255.0f, 15 / 255.0f);
            this["DarkGoldenrod2"] = new Color(238 / 255.0f, 173 / 255.0f, 14 / 255.0f);
            this["DarkGoldenrod3"] = new Color(205 / 255.0f, 149 / 255.0f, 12 / 255.0f);
            this["DarkGoldenrod4"] = new Color(139 / 255.0f, 101 / 255.0f, 8 / 255.0f);
            this["RosyBrown1"] = new Color(255 / 255.0f, 193 / 255.0f, 193 / 255.0f);
            this["RosyBrown2"] = new Color(238 / 255.0f, 180 / 255.0f, 180 / 255.0f);
            this["RosyBrown3"] = new Color(205 / 255.0f, 155 / 255.0f, 155 / 255.0f);
            this["RosyBrown4"] = new Color(139 / 255.0f, 105 / 255.0f, 105 / 255.0f);
            this["IndianRed1"] = new Color(255 / 255.0f, 106 / 255.0f, 106 / 255.0f);
            this["IndianRed2"] = new Color(238 / 255.0f, 99 / 255.0f, 99 / 255.0f);
            this["IndianRed3"] = new Color(205 / 255.0f, 85 / 255.0f, 85 / 255.0f);
            this["IndianRed4"] = new Color(139 / 255.0f, 58 / 255.0f, 58 / 255.0f);
            this["sienna1"] = new Color(255 / 255.0f, 130 / 255.0f, 71 / 255.0f);
            this["sienna2"] = new Color(238 / 255.0f, 121 / 255.0f, 66 / 255.0f);
            this["sienna3"] = new Color(205 / 255.0f, 104 / 255.0f, 57 / 255.0f);
            this["sienna4"] = new Color(139 / 255.0f, 71 / 255.0f, 38 / 255.0f);
            this["burlywood1"] = new Color(255 / 255.0f, 211 / 255.0f, 155 / 255.0f);
            this["burlywood2"] = new Color(238 / 255.0f, 197 / 255.0f, 145 / 255.0f);
            this["burlywood3"] = new Color(205 / 255.0f, 170 / 255.0f, 125 / 255.0f);
            this["burlywood4"] = new Color(139 / 255.0f, 115 / 255.0f, 85 / 255.0f);
            this["wheat1"] = new Color(255 / 255.0f, 231 / 255.0f, 186 / 255.0f);
            this["wheat2"] = new Color(238 / 255.0f, 216 / 255.0f, 174 / 255.0f);
            this["wheat3"] = new Color(205 / 255.0f, 186 / 255.0f, 150 / 255.0f);
            this["wheat4"] = new Color(139 / 255.0f, 126 / 255.0f, 102 / 255.0f);
            this["tan1"] = new Color(255 / 255.0f, 165 / 255.0f, 79 / 255.0f);
            this["tan2"] = new Color(238 / 255.0f, 154 / 255.0f, 73 / 255.0f);
            this["tan3"] = new Color(205 / 255.0f, 133 / 255.0f, 63 / 255.0f);
            this["tan4"] = new Color(139 / 255.0f, 90 / 255.0f, 43 / 255.0f);
            this["chocolate1"] = new Color(255 / 255.0f, 127 / 255.0f, 36 / 255.0f);
            this["chocolate2"] = new Color(238 / 255.0f, 118 / 255.0f, 33 / 255.0f);
            this["chocolate3"] = new Color(205 / 255.0f, 102 / 255.0f, 29 / 255.0f);
            this["chocolate4"] = new Color(139 / 255.0f, 69 / 255.0f, 19 / 255.0f);
            this["firebrick1"] = new Color(255 / 255.0f, 48 / 255.0f, 48 / 255.0f);
            this["firebrick2"] = new Color(238 / 255.0f, 44 / 255.0f, 44 / 255.0f);
            this["firebrick3"] = new Color(205 / 255.0f, 38 / 255.0f, 38 / 255.0f);
            this["firebrick4"] = new Color(139 / 255.0f, 26 / 255.0f, 26 / 255.0f);
            this["brown1"] = new Color(255 / 255.0f, 64 / 255.0f, 64 / 255.0f);
            this["brown2"] = new Color(238 / 255.0f, 59 / 255.0f, 59 / 255.0f);
            this["brown3"] = new Color(205 / 255.0f, 51 / 255.0f, 51 / 255.0f);
            this["brown4"] = new Color(139 / 255.0f, 35 / 255.0f, 35 / 255.0f);
            this["salmon1"] = new Color(255 / 255.0f, 140 / 255.0f, 105 / 255.0f);
            this["salmon2"] = new Color(238 / 255.0f, 130 / 255.0f, 98 / 255.0f);
            this["salmon3"] = new Color(205 / 255.0f, 112 / 255.0f, 84 / 255.0f);
            this["salmon4"] = new Color(139 / 255.0f, 76 / 255.0f, 57 / 255.0f);
            this["LightSalmon1"] = new Color(255 / 255.0f, 160 / 255.0f, 122 / 255.0f);
            this["LightSalmon2"] = new Color(238 / 255.0f, 149 / 255.0f, 114 / 255.0f);
            this["LightSalmon3"] = new Color(205 / 255.0f, 129 / 255.0f, 98 / 255.0f);
            this["LightSalmon4"] = new Color(139 / 255.0f, 87 / 255.0f, 66 / 255.0f);
            this["orange1"] = new Color(255 / 255.0f, 165 / 255.0f, 0 / 255.0f);
            this["orange2"] = new Color(238 / 255.0f, 154 / 255.0f, 0 / 255.0f);
            this["orange3"] = new Color(205 / 255.0f, 133 / 255.0f, 0 / 255.0f);
            this["orange4"] = new Color(139 / 255.0f, 90 / 255.0f, 0 / 255.0f);
            this["DarkOrange1"] = new Color(255 / 255.0f, 127 / 255.0f, 0 / 255.0f);
            this["DarkOrange2"] = new Color(238 / 255.0f, 118 / 255.0f, 0 / 255.0f);
            this["DarkOrange3"] = new Color(205 / 255.0f, 102 / 255.0f, 0 / 255.0f);
            this["DarkOrange4"] = new Color(139 / 255.0f, 69 / 255.0f, 0 / 255.0f);
            this["coral1"] = new Color(255 / 255.0f, 114 / 255.0f, 86 / 255.0f);
            this["coral2"] = new Color(238 / 255.0f, 106 / 255.0f, 80 / 255.0f);
            this["coral3"] = new Color(205 / 255.0f, 91 / 255.0f, 69 / 255.0f);
            this["coral4"] = new Color(139 / 255.0f, 62 / 255.0f, 47 / 255.0f);
            this["tomato1"] = new Color(255 / 255.0f, 99 / 255.0f, 71 / 255.0f);
            this["tomato2"] = new Color(238 / 255.0f, 92 / 255.0f, 66 / 255.0f);
            this["tomato3"] = new Color(205 / 255.0f, 79 / 255.0f, 57 / 255.0f);
            this["tomato4"] = new Color(139 / 255.0f, 54 / 255.0f, 38 / 255.0f);
            this["OrangeRed1"] = new Color(255 / 255.0f, 69 / 255.0f, 0 / 255.0f);
            this["OrangeRed2"] = new Color(238 / 255.0f, 64 / 255.0f, 0 / 255.0f);
            this["OrangeRed3"] = new Color(205 / 255.0f, 55 / 255.0f, 0 / 255.0f);
            this["OrangeRed4"] = new Color(139 / 255.0f, 37 / 255.0f, 0 / 255.0f);
            this["red1"] = new Color(255 / 255.0f, 0 / 255.0f, 0 / 255.0f);
            this["red2"] = new Color(238 / 255.0f, 0 / 255.0f, 0 / 255.0f);
            this["red3"] = new Color(205 / 255.0f, 0 / 255.0f, 0 / 255.0f);
            this["red4"] = new Color(139 / 255.0f, 0 / 255.0f, 0 / 255.0f);
            this["DeepPink1"] = new Color(255 / 255.0f, 20 / 255.0f, 147 / 255.0f);
            this["DeepPink2"] = new Color(238 / 255.0f, 18 / 255.0f, 137 / 255.0f);
            this["DeepPink3"] = new Color(205 / 255.0f, 16 / 255.0f, 118 / 255.0f);
            this["DeepPink4"] = new Color(139 / 255.0f, 10 / 255.0f, 80 / 255.0f);
            this["HotPink1"] = new Color(255 / 255.0f, 110 / 255.0f, 180 / 255.0f);
            this["HotPink2"] = new Color(238 / 255.0f, 106 / 255.0f, 167 / 255.0f);
            this["HotPink3"] = new Color(205 / 255.0f, 96 / 255.0f, 144 / 255.0f);
            this["HotPink4"] = new Color(139 / 255.0f, 58 / 255.0f, 98 / 255.0f);
            this["pink1"] = new Color(255 / 255.0f, 181 / 255.0f, 197 / 255.0f);
            this["pink2"] = new Color(238 / 255.0f, 169 / 255.0f, 184 / 255.0f);
            this["pink3"] = new Color(205 / 255.0f, 145 / 255.0f, 158 / 255.0f);
            this["pink4"] = new Color(139 / 255.0f, 99 / 255.0f, 108 / 255.0f);
            this["LightPink1"] = new Color(255 / 255.0f, 174 / 255.0f, 185 / 255.0f);
            this["LightPink2"] = new Color(238 / 255.0f, 162 / 255.0f, 173 / 255.0f);
            this["LightPink3"] = new Color(205 / 255.0f, 140 / 255.0f, 149 / 255.0f);
            this["LightPink4"] = new Color(139 / 255.0f, 95 / 255.0f, 101 / 255.0f);
            this["PaleVioletRed1"] = new Color(255 / 255.0f, 130 / 255.0f, 171 / 255.0f);
            this["PaleVioletRed2"] = new Color(238 / 255.0f, 121 / 255.0f, 159 / 255.0f);
            this["PaleVioletRed3"] = new Color(205 / 255.0f, 104 / 255.0f, 137 / 255.0f);
            this["PaleVioletRed4"] = new Color(139 / 255.0f, 71 / 255.0f, 93 / 255.0f);
            this["maroon1"] = new Color(255 / 255.0f, 52 / 255.0f, 179 / 255.0f);
            this["maroon2"] = new Color(238 / 255.0f, 48 / 255.0f, 167 / 255.0f);
            this["maroon3"] = new Color(205 / 255.0f, 41 / 255.0f, 144 / 255.0f);
            this["maroon4"] = new Color(139 / 255.0f, 28 / 255.0f, 98 / 255.0f);
            this["VioletRed1"] = new Color(255 / 255.0f, 62 / 255.0f, 150 / 255.0f);
            this["VioletRed2"] = new Color(238 / 255.0f, 58 / 255.0f, 140 / 255.0f);
            this["VioletRed3"] = new Color(205 / 255.0f, 50 / 255.0f, 120 / 255.0f);
            this["VioletRed4"] = new Color(139 / 255.0f, 34 / 255.0f, 82 / 255.0f);
            this["magenta1"] = new Color(255 / 255.0f, 0 / 255.0f, 255 / 255.0f);
            this["magenta2"] = new Color(238 / 255.0f, 0 / 255.0f, 238 / 255.0f);
            this["magenta3"] = new Color(205 / 255.0f, 0 / 255.0f, 205 / 255.0f);
            this["magenta4"] = new Color(139 / 255.0f, 0 / 255.0f, 139 / 255.0f);
            this["orchid1"] = new Color(255 / 255.0f, 131 / 255.0f, 250 / 255.0f);
            this["orchid2"] = new Color(238 / 255.0f, 122 / 255.0f, 233 / 255.0f);
            this["orchid3"] = new Color(205 / 255.0f, 105 / 255.0f, 201 / 255.0f);
            this["orchid4"] = new Color(139 / 255.0f, 71 / 255.0f, 137 / 255.0f);
            this["plum1"] = new Color(255 / 255.0f, 187 / 255.0f, 255 / 255.0f);
            this["plum2"] = new Color(238 / 255.0f, 174 / 255.0f, 238 / 255.0f);
            this["plum3"] = new Color(205 / 255.0f, 150 / 255.0f, 205 / 255.0f);
            this["plum4"] = new Color(139 / 255.0f, 102 / 255.0f, 139 / 255.0f);
            this["MediumOrchid1"] = new Color(224 / 255.0f, 102 / 255.0f, 255 / 255.0f);
            this["MediumOrchid2"] = new Color(209 / 255.0f, 95 / 255.0f, 238 / 255.0f);
            this["MediumOrchid3"] = new Color(180 / 255.0f, 82 / 255.0f, 205 / 255.0f);
            this["MediumOrchid4"] = new Color(122 / 255.0f, 55 / 255.0f, 139 / 255.0f);
            this["DarkOrchid1"] = new Color(191 / 255.0f, 62 / 255.0f, 255 / 255.0f);
            this["DarkOrchid2"] = new Color(178 / 255.0f, 58 / 255.0f, 238 / 255.0f);
            this["DarkOrchid3"] = new Color(154 / 255.0f, 50 / 255.0f, 205 / 255.0f);
            this["DarkOrchid4"] = new Color(104 / 255.0f, 34 / 255.0f, 139 / 255.0f);
            this["purple1"] = new Color(155 / 255.0f, 48 / 255.0f, 255 / 255.0f);
            this["purple2"] = new Color(145 / 255.0f, 44 / 255.0f, 238 / 255.0f);
            this["purple3"] = new Color(125 / 255.0f, 38 / 255.0f, 205 / 255.0f);
            this["purple4"] = new Color(85 / 255.0f, 26 / 255.0f, 139 / 255.0f);
            this["MediumPurple1"] = new Color(171 / 255.0f, 130 / 255.0f, 255 / 255.0f);
            this["MediumPurple2"] = new Color(159 / 255.0f, 121 / 255.0f, 238 / 255.0f);
            this["MediumPurple3"] = new Color(137 / 255.0f, 104 / 255.0f, 205 / 255.0f);
            this["MediumPurple4"] = new Color(93 / 255.0f, 71 / 255.0f, 139 / 255.0f);
            this["thistle1"] = new Color(255 / 255.0f, 225 / 255.0f, 255 / 255.0f);
            this["thistle2"] = new Color(238 / 255.0f, 210 / 255.0f, 238 / 255.0f);
            this["thistle3"] = new Color(205 / 255.0f, 181 / 255.0f, 205 / 255.0f);
            this["thistle4"] = new Color(139 / 255.0f, 123 / 255.0f, 139 / 255.0f);
            this["gray0"] = new Color(0 / 255.0f, 0 / 255.0f, 0 / 255.0f);
            this["grey0"] = new Color(0 / 255.0f, 0 / 255.0f, 0 / 255.0f);
            this["gray1"] = new Color(3 / 255.0f, 3 / 255.0f, 3 / 255.0f);
            this["grey1"] = new Color(3 / 255.0f, 3 / 255.0f, 3 / 255.0f);
            this["gray2"] = new Color(5 / 255.0f, 5 / 255.0f, 5 / 255.0f);
            this["grey2"] = new Color(5 / 255.0f, 5 / 255.0f, 5 / 255.0f);
            this["gray3"] = new Color(8 / 255.0f, 8 / 255.0f, 8 / 255.0f);
            this["grey3"] = new Color(8 / 255.0f, 8 / 255.0f, 8 / 255.0f);
            this["gray4"] = new Color(10 / 255.0f, 10 / 255.0f, 10 / 255.0f);
            this["grey4"] = new Color(10 / 255.0f, 10 / 255.0f, 10 / 255.0f);
            this["gray5"] = new Color(13 / 255.0f, 13 / 255.0f, 13 / 255.0f);
            this["grey5"] = new Color(13 / 255.0f, 13 / 255.0f, 13 / 255.0f);
            this["gray6"] = new Color(15 / 255.0f, 15 / 255.0f, 15 / 255.0f);
            this["grey6"] = new Color(15 / 255.0f, 15 / 255.0f, 15 / 255.0f);
            this["gray7"] = new Color(18 / 255.0f, 18 / 255.0f, 18 / 255.0f);
            this["grey7"] = new Color(18 / 255.0f, 18 / 255.0f, 18 / 255.0f);
            this["gray8"] = new Color(20 / 255.0f, 20 / 255.0f, 20 / 255.0f);
            this["grey8"] = new Color(20 / 255.0f, 20 / 255.0f, 20 / 255.0f);
            this["gray9"] = new Color(23 / 255.0f, 23 / 255.0f, 23 / 255.0f);
            this["grey9"] = new Color(23 / 255.0f, 23 / 255.0f, 23 / 255.0f);
            this["gray10"] = new Color(26 / 255.0f, 26 / 255.0f, 26 / 255.0f);
            this["grey10"] = new Color(26 / 255.0f, 26 / 255.0f, 26 / 255.0f);
            this["gray11"] = new Color(28 / 255.0f, 28 / 255.0f, 28 / 255.0f);
            this["grey11"] = new Color(28 / 255.0f, 28 / 255.0f, 28 / 255.0f);
            this["gray12"] = new Color(31 / 255.0f, 31 / 255.0f, 31 / 255.0f);
            this["grey12"] = new Color(31 / 255.0f, 31 / 255.0f, 31 / 255.0f);
            this["gray13"] = new Color(33 / 255.0f, 33 / 255.0f, 33 / 255.0f);
            this["grey13"] = new Color(33 / 255.0f, 33 / 255.0f, 33 / 255.0f);
            this["gray14"] = new Color(36 / 255.0f, 36 / 255.0f, 36 / 255.0f);
            this["grey14"] = new Color(36 / 255.0f, 36 / 255.0f, 36 / 255.0f);
            this["gray15"] = new Color(38 / 255.0f, 38 / 255.0f, 38 / 255.0f);
            this["grey15"] = new Color(38 / 255.0f, 38 / 255.0f, 38 / 255.0f);
            this["gray16"] = new Color(41 / 255.0f, 41 / 255.0f, 41 / 255.0f);
            this["grey16"] = new Color(41 / 255.0f, 41 / 255.0f, 41 / 255.0f);
            this["gray17"] = new Color(43 / 255.0f, 43 / 255.0f, 43 / 255.0f);
            this["grey17"] = new Color(43 / 255.0f, 43 / 255.0f, 43 / 255.0f);
            this["gray18"] = new Color(46 / 255.0f, 46 / 255.0f, 46 / 255.0f);
            this["grey18"] = new Color(46 / 255.0f, 46 / 255.0f, 46 / 255.0f);
            this["gray19"] = new Color(48 / 255.0f, 48 / 255.0f, 48 / 255.0f);
            this["grey19"] = new Color(48 / 255.0f, 48 / 255.0f, 48 / 255.0f);
            this["gray20"] = new Color(51 / 255.0f, 51 / 255.0f, 51 / 255.0f);
            this["grey20"] = new Color(51 / 255.0f, 51 / 255.0f, 51 / 255.0f);
            this["gray21"] = new Color(54 / 255.0f, 54 / 255.0f, 54 / 255.0f);
            this["grey21"] = new Color(54 / 255.0f, 54 / 255.0f, 54 / 255.0f);
            this["gray22"] = new Color(56 / 255.0f, 56 / 255.0f, 56 / 255.0f);
            this["grey22"] = new Color(56 / 255.0f, 56 / 255.0f, 56 / 255.0f);
            this["gray23"] = new Color(59 / 255.0f, 59 / 255.0f, 59 / 255.0f);
            this["grey23"] = new Color(59 / 255.0f, 59 / 255.0f, 59 / 255.0f);
            this["gray24"] = new Color(61 / 255.0f, 61 / 255.0f, 61 / 255.0f);
            this["grey24"] = new Color(61 / 255.0f, 61 / 255.0f, 61 / 255.0f);
            this["gray25"] = new Color(64 / 255.0f, 64 / 255.0f, 64 / 255.0f);
            this["grey25"] = new Color(64 / 255.0f, 64 / 255.0f, 64 / 255.0f);
            this["gray26"] = new Color(66 / 255.0f, 66 / 255.0f, 66 / 255.0f);
            this["grey26"] = new Color(66 / 255.0f, 66 / 255.0f, 66 / 255.0f);
            this["gray27"] = new Color(69 / 255.0f, 69 / 255.0f, 69 / 255.0f);
            this["grey27"] = new Color(69 / 255.0f, 69 / 255.0f, 69 / 255.0f);
            this["gray28"] = new Color(71 / 255.0f, 71 / 255.0f, 71 / 255.0f);
            this["grey28"] = new Color(71 / 255.0f, 71 / 255.0f, 71 / 255.0f);
            this["gray29"] = new Color(74 / 255.0f, 74 / 255.0f, 74 / 255.0f);
            this["grey29"] = new Color(74 / 255.0f, 74 / 255.0f, 74 / 255.0f);
            this["gray30"] = new Color(77 / 255.0f, 77 / 255.0f, 77 / 255.0f);
            this["grey30"] = new Color(77 / 255.0f, 77 / 255.0f, 77 / 255.0f);
            this["gray31"] = new Color(79 / 255.0f, 79 / 255.0f, 79 / 255.0f);
            this["grey31"] = new Color(79 / 255.0f, 79 / 255.0f, 79 / 255.0f);
            this["gray32"] = new Color(82 / 255.0f, 82 / 255.0f, 82 / 255.0f);
            this["grey32"] = new Color(82 / 255.0f, 82 / 255.0f, 82 / 255.0f);
            this["gray33"] = new Color(84 / 255.0f, 84 / 255.0f, 84 / 255.0f);
            this["grey33"] = new Color(84 / 255.0f, 84 / 255.0f, 84 / 255.0f);
            this["gray34"] = new Color(87 / 255.0f, 87 / 255.0f, 87 / 255.0f);
            this["grey34"] = new Color(87 / 255.0f, 87 / 255.0f, 87 / 255.0f);
            this["gray35"] = new Color(89 / 255.0f, 89 / 255.0f, 89 / 255.0f);
            this["grey35"] = new Color(89 / 255.0f, 89 / 255.0f, 89 / 255.0f);
            this["gray36"] = new Color(92 / 255.0f, 92 / 255.0f, 92 / 255.0f);
            this["grey36"] = new Color(92 / 255.0f, 92 / 255.0f, 92 / 255.0f);
            this["gray37"] = new Color(94 / 255.0f, 94 / 255.0f, 94 / 255.0f);
            this["grey37"] = new Color(94 / 255.0f, 94 / 255.0f, 94 / 255.0f);
            this["gray38"] = new Color(97 / 255.0f, 97 / 255.0f, 97 / 255.0f);
            this["grey38"] = new Color(97 / 255.0f, 97 / 255.0f, 97 / 255.0f);
            this["gray39"] = new Color(99 / 255.0f, 99 / 255.0f, 99 / 255.0f);
            this["grey39"] = new Color(99 / 255.0f, 99 / 255.0f, 99 / 255.0f);
            this["gray40"] = new Color(102 / 255.0f, 102 / 255.0f, 102 / 255.0f);
            this["grey40"] = new Color(102 / 255.0f, 102 / 255.0f, 102 / 255.0f);
            this["gray41"] = new Color(105 / 255.0f, 105 / 255.0f, 105 / 255.0f);
            this["grey41"] = new Color(105 / 255.0f, 105 / 255.0f, 105 / 255.0f);
            this["gray42"] = new Color(107 / 255.0f, 107 / 255.0f, 107 / 255.0f);
            this["grey42"] = new Color(107 / 255.0f, 107 / 255.0f, 107 / 255.0f);
            this["gray43"] = new Color(110 / 255.0f, 110 / 255.0f, 110 / 255.0f);
            this["grey43"] = new Color(110 / 255.0f, 110 / 255.0f, 110 / 255.0f);
            this["gray44"] = new Color(112 / 255.0f, 112 / 255.0f, 112 / 255.0f);
            this["grey44"] = new Color(112 / 255.0f, 112 / 255.0f, 112 / 255.0f);
            this["gray45"] = new Color(115 / 255.0f, 115 / 255.0f, 115 / 255.0f);
            this["grey45"] = new Color(115 / 255.0f, 115 / 255.0f, 115 / 255.0f);
            this["gray46"] = new Color(117 / 255.0f, 117 / 255.0f, 117 / 255.0f);
            this["grey46"] = new Color(117 / 255.0f, 117 / 255.0f, 117 / 255.0f);
            this["gray47"] = new Color(120 / 255.0f, 120 / 255.0f, 120 / 255.0f);
            this["grey47"] = new Color(120 / 255.0f, 120 / 255.0f, 120 / 255.0f);
            this["gray48"] = new Color(122 / 255.0f, 122 / 255.0f, 122 / 255.0f);
            this["grey48"] = new Color(122 / 255.0f, 122 / 255.0f, 122 / 255.0f);
            this["gray49"] = new Color(125 / 255.0f, 125 / 255.0f, 125 / 255.0f);
            this["grey49"] = new Color(125 / 255.0f, 125 / 255.0f, 125 / 255.0f);
            this["gray50"] = new Color(127 / 255.0f, 127 / 255.0f, 127 / 255.0f);
            this["grey50"] = new Color(127 / 255.0f, 127 / 255.0f, 127 / 255.0f);
            this["gray51"] = new Color(130 / 255.0f, 130 / 255.0f, 130 / 255.0f);
            this["grey51"] = new Color(130 / 255.0f, 130 / 255.0f, 130 / 255.0f);
            this["gray52"] = new Color(133 / 255.0f, 133 / 255.0f, 133 / 255.0f);
            this["grey52"] = new Color(133 / 255.0f, 133 / 255.0f, 133 / 255.0f);
            this["gray53"] = new Color(135 / 255.0f, 135 / 255.0f, 135 / 255.0f);
            this["grey53"] = new Color(135 / 255.0f, 135 / 255.0f, 135 / 255.0f);
            this["gray54"] = new Color(138 / 255.0f, 138 / 255.0f, 138 / 255.0f);
            this["grey54"] = new Color(138 / 255.0f, 138 / 255.0f, 138 / 255.0f);
            this["gray55"] = new Color(140 / 255.0f, 140 / 255.0f, 140 / 255.0f);
            this["grey55"] = new Color(140 / 255.0f, 140 / 255.0f, 140 / 255.0f);
            this["gray56"] = new Color(143 / 255.0f, 143 / 255.0f, 143 / 255.0f);
            this["grey56"] = new Color(143 / 255.0f, 143 / 255.0f, 143 / 255.0f);
            this["gray57"] = new Color(145 / 255.0f, 145 / 255.0f, 145 / 255.0f);
            this["grey57"] = new Color(145 / 255.0f, 145 / 255.0f, 145 / 255.0f);
            this["gray58"] = new Color(148 / 255.0f, 148 / 255.0f, 148 / 255.0f);
            this["grey58"] = new Color(148 / 255.0f, 148 / 255.0f, 148 / 255.0f);
            this["gray59"] = new Color(150 / 255.0f, 150 / 255.0f, 150 / 255.0f);
            this["grey59"] = new Color(150 / 255.0f, 150 / 255.0f, 150 / 255.0f);
            this["gray60"] = new Color(153 / 255.0f, 153 / 255.0f, 153 / 255.0f);
            this["grey60"] = new Color(153 / 255.0f, 153 / 255.0f, 153 / 255.0f);
            this["gray61"] = new Color(156 / 255.0f, 156 / 255.0f, 156 / 255.0f);
            this["grey61"] = new Color(156 / 255.0f, 156 / 255.0f, 156 / 255.0f);
            this["gray62"] = new Color(158 / 255.0f, 158 / 255.0f, 158 / 255.0f);
            this["grey62"] = new Color(158 / 255.0f, 158 / 255.0f, 158 / 255.0f);
            this["gray63"] = new Color(161 / 255.0f, 161 / 255.0f, 161 / 255.0f);
            this["grey63"] = new Color(161 / 255.0f, 161 / 255.0f, 161 / 255.0f);
            this["gray64"] = new Color(163 / 255.0f, 163 / 255.0f, 163 / 255.0f);
            this["grey64"] = new Color(163 / 255.0f, 163 / 255.0f, 163 / 255.0f);
            this["gray65"] = new Color(166 / 255.0f, 166 / 255.0f, 166 / 255.0f);
            this["grey65"] = new Color(166 / 255.0f, 166 / 255.0f, 166 / 255.0f);
            this["gray66"] = new Color(168 / 255.0f, 168 / 255.0f, 168 / 255.0f);
            this["grey66"] = new Color(168 / 255.0f, 168 / 255.0f, 168 / 255.0f);
            this["gray67"] = new Color(171 / 255.0f, 171 / 255.0f, 171 / 255.0f);
            this["grey67"] = new Color(171 / 255.0f, 171 / 255.0f, 171 / 255.0f);
            this["gray68"] = new Color(173 / 255.0f, 173 / 255.0f, 173 / 255.0f);
            this["grey68"] = new Color(173 / 255.0f, 173 / 255.0f, 173 / 255.0f);
            this["gray69"] = new Color(176 / 255.0f, 176 / 255.0f, 176 / 255.0f);
            this["grey69"] = new Color(176 / 255.0f, 176 / 255.0f, 176 / 255.0f);
            this["gray70"] = new Color(179 / 255.0f, 179 / 255.0f, 179 / 255.0f);
            this["grey70"] = new Color(179 / 255.0f, 179 / 255.0f, 179 / 255.0f);
            this["gray71"] = new Color(181 / 255.0f, 181 / 255.0f, 181 / 255.0f);
            this["grey71"] = new Color(181 / 255.0f, 181 / 255.0f, 181 / 255.0f);
            this["gray72"] = new Color(184 / 255.0f, 184 / 255.0f, 184 / 255.0f);
            this["grey72"] = new Color(184 / 255.0f, 184 / 255.0f, 184 / 255.0f);
            this["gray73"] = new Color(186 / 255.0f, 186 / 255.0f, 186 / 255.0f);
            this["grey73"] = new Color(186 / 255.0f, 186 / 255.0f, 186 / 255.0f);
            this["gray74"] = new Color(189 / 255.0f, 189 / 255.0f, 189 / 255.0f);
            this["grey74"] = new Color(189 / 255.0f, 189 / 255.0f, 189 / 255.0f);
            this["gray75"] = new Color(191 / 255.0f, 191 / 255.0f, 191 / 255.0f);
            this["grey75"] = new Color(191 / 255.0f, 191 / 255.0f, 191 / 255.0f);
            this["gray76"] = new Color(194 / 255.0f, 194 / 255.0f, 194 / 255.0f);
            this["grey76"] = new Color(194 / 255.0f, 194 / 255.0f, 194 / 255.0f);
            this["gray77"] = new Color(196 / 255.0f, 196 / 255.0f, 196 / 255.0f);
            this["grey77"] = new Color(196 / 255.0f, 196 / 255.0f, 196 / 255.0f);
            this["gray78"] = new Color(199 / 255.0f, 199 / 255.0f, 199 / 255.0f);
            this["grey78"] = new Color(199 / 255.0f, 199 / 255.0f, 199 / 255.0f);
            this["gray79"] = new Color(201 / 255.0f, 201 / 255.0f, 201 / 255.0f);
            this["grey79"] = new Color(201 / 255.0f, 201 / 255.0f, 201 / 255.0f);
            this["gray80"] = new Color(204 / 255.0f, 204 / 255.0f, 204 / 255.0f);
            this["grey80"] = new Color(204 / 255.0f, 204 / 255.0f, 204 / 255.0f);
            this["gray81"] = new Color(207 / 255.0f, 207 / 255.0f, 207 / 255.0f);
            this["grey81"] = new Color(207 / 255.0f, 207 / 255.0f, 207 / 255.0f);
            this["gray82"] = new Color(209 / 255.0f, 209 / 255.0f, 209 / 255.0f);
            this["grey82"] = new Color(209 / 255.0f, 209 / 255.0f, 209 / 255.0f);
            this["gray83"] = new Color(212 / 255.0f, 212 / 255.0f, 212 / 255.0f);
            this["grey83"] = new Color(212 / 255.0f, 212 / 255.0f, 212 / 255.0f);
            this["gray84"] = new Color(214 / 255.0f, 214 / 255.0f, 214 / 255.0f);
            this["grey84"] = new Color(214 / 255.0f, 214 / 255.0f, 214 / 255.0f);
            this["gray85"] = new Color(217 / 255.0f, 217 / 255.0f, 217 / 255.0f);
            this["grey85"] = new Color(217 / 255.0f, 217 / 255.0f, 217 / 255.0f);
            this["gray86"] = new Color(219 / 255.0f, 219 / 255.0f, 219 / 255.0f);
            this["grey86"] = new Color(219 / 255.0f, 219 / 255.0f, 219 / 255.0f);
            this["gray87"] = new Color(222 / 255.0f, 222 / 255.0f, 222 / 255.0f);
            this["grey87"] = new Color(222 / 255.0f, 222 / 255.0f, 222 / 255.0f);
            this["gray88"] = new Color(224 / 255.0f, 224 / 255.0f, 224 / 255.0f);
            this["grey88"] = new Color(224 / 255.0f, 224 / 255.0f, 224 / 255.0f);
            this["gray89"] = new Color(227 / 255.0f, 227 / 255.0f, 227 / 255.0f);
            this["grey89"] = new Color(227 / 255.0f, 227 / 255.0f, 227 / 255.0f);
            this["gray90"] = new Color(229 / 255.0f, 229 / 255.0f, 229 / 255.0f);
            this["grey90"] = new Color(229 / 255.0f, 229 / 255.0f, 229 / 255.0f);
            this["gray91"] = new Color(232 / 255.0f, 232 / 255.0f, 232 / 255.0f);
            this["grey91"] = new Color(232 / 255.0f, 232 / 255.0f, 232 / 255.0f);
            this["gray92"] = new Color(235 / 255.0f, 235 / 255.0f, 235 / 255.0f);
            this["grey92"] = new Color(235 / 255.0f, 235 / 255.0f, 235 / 255.0f);
            this["gray93"] = new Color(237 / 255.0f, 237 / 255.0f, 237 / 255.0f);
            this["grey93"] = new Color(237 / 255.0f, 237 / 255.0f, 237 / 255.0f);
            this["gray94"] = new Color(240 / 255.0f, 240 / 255.0f, 240 / 255.0f);
            this["grey94"] = new Color(240 / 255.0f, 240 / 255.0f, 240 / 255.0f);
            this["gray95"] = new Color(242 / 255.0f, 242 / 255.0f, 242 / 255.0f);
            this["grey95"] = new Color(242 / 255.0f, 242 / 255.0f, 242 / 255.0f);
            this["gray96"] = new Color(245 / 255.0f, 245 / 255.0f, 245 / 255.0f);
            this["grey96"] = new Color(245 / 255.0f, 245 / 255.0f, 245 / 255.0f);
            this["gray97"] = new Color(247 / 255.0f, 247 / 255.0f, 247 / 255.0f);
            this["grey97"] = new Color(247 / 255.0f, 247 / 255.0f, 247 / 255.0f);
            this["gray98"] = new Color(250 / 255.0f, 250 / 255.0f, 250 / 255.0f);
            this["grey98"] = new Color(250 / 255.0f, 250 / 255.0f, 250 / 255.0f);
            this["gray99"] = new Color(252 / 255.0f, 252 / 255.0f, 252 / 255.0f);
            this["grey99"] = new Color(252 / 255.0f, 252 / 255.0f, 252 / 255.0f);
            this["gray100"] = new Color(255 / 255.0f, 255 / 255.0f, 255 / 255.0f);
            this["grey100"] = new Color(255 / 255.0f, 255 / 255.0f, 255 / 255.0f);
            this["dark grey"] = new Color(169 / 255.0f, 169 / 255.0f, 169 / 255.0f);
            this["DarkGrey"] = new Color(169 / 255.0f, 169 / 255.0f, 169 / 255.0f);
            this["dark gray"] = new Color(169 / 255.0f, 169 / 255.0f, 169 / 255.0f);
            this["DarkGray"] = new Color(169 / 255.0f, 169 / 255.0f, 169 / 255.0f);
            this["dark blue"] = new Color(0 / 255.0f, 0 / 255.0f, 139 / 255.0f);
            this["DarkBlue"] = new Color(0 / 255.0f, 0 / 255.0f, 139 / 255.0f);
            this["dark cyan"] = new Color(0 / 255.0f, 139 / 255.0f, 139 / 255.0f);
            this["DarkCyan"] = new Color(0 / 255.0f, 139 / 255.0f, 139 / 255.0f);
            this["dark magenta"] = new Color(139 / 255.0f, 0 / 255.0f, 139 / 255.0f);
            this["DarkMagenta"] = new Color(139 / 255.0f, 0 / 255.0f, 139 / 255.0f);
            this["dark red"] = new Color(139 / 255.0f, 0 / 255.0f, 0 / 255.0f);
            this["DarkRed"] = new Color(139 / 255.0f, 0 / 255.0f, 0 / 255.0f);
            this["light green"] = new Color(144 / 255.0f, 238 / 255.0f, 144 / 255.0f);
            this["LightGreen"] = new Color(144 / 255.0f, 238 / 255.0f, 144 / 255.0f);
        }
    } // The boring NamedWebColorDictionary class
} // namespace
