using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;

public class SVGParserTests
{
    [Test]
    public void ImportSVG_CreatesScene()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <rect width=""100"" height=""20"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        Assert.IsNotNull(sceneInfo.scene);
        Assert.IsNotNull(sceneInfo.scene.root);
    }

    [Test]
    public void ImportSVG_SupportsRects()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <rect x=""5"" y=""10"" width=""100"" height=""20"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        var rect = sceneInfo.scene.root.children[0].drawables[0] as Rectangle;
        Assert.IsNotNull(rect);
        Assert.AreEqual(new Vector2(5.0f, 10.0f), rect.position);
        Assert.AreEqual(new Vector2(100.0f, 20.0f), rect.size);
        Assert.AreEqual(Vector2.zero, rect.radiusTL);
        Assert.AreEqual(Vector2.zero, rect.radiusTR);
        Assert.AreEqual(Vector2.zero, rect.radiusBL);
        Assert.AreEqual(Vector2.zero, rect.radiusBR);
    }

    [Test]
    public void ImportSVG_SupportsCircles()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <circle cx=""50"" cy=""60"" r=""50"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));

        // Circles/ellipses are stored as a rectangle with rounded corners
        var rect = sceneInfo.scene.root.children[0].drawables[0] as Rectangle;
        Assert.IsNotNull(rect);
        Assert.AreEqual(new Vector2(0.0f, 10.0f), rect.position);
        Assert.AreEqual(new Vector2(100.0f, 100.0f), rect.size);
        Assert.AreEqual(new Vector2(50.0f, 50.0f), rect.radiusTL);
        Assert.AreEqual(new Vector2(50.0f, 50.0f), rect.radiusTR);
        Assert.AreEqual(new Vector2(50.0f, 50.0f), rect.radiusBL);
        Assert.AreEqual(new Vector2(50.0f, 50.0f), rect.radiusBR);
    }

    [Test]
    public void ImportSVG_SupportsEllipses()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <ellipse cx=""50"" cy=""60"" rx=""50"" ry=""60"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));

        // Circles/ellipses are stored as a rectangle with rounded corners
        var rect = sceneInfo.scene.root.children[0].drawables[0] as Rectangle;
        Assert.IsNotNull(rect);
        Assert.AreEqual(new Vector2(0.0f, 0.0f), rect.position);
        Assert.AreEqual(new Vector2(100.0f, 120.0f), rect.size);
        Assert.AreEqual(new Vector2(50.0f, 60.0f), rect.radiusTL);
        Assert.AreEqual(new Vector2(50.0f, 60.0f), rect.radiusTR);
        Assert.AreEqual(new Vector2(50.0f, 60.0f), rect.radiusBL);
        Assert.AreEqual(new Vector2(50.0f, 60.0f), rect.radiusBR);
    }

    [Test]
    public void ImportSVG_SupportsPaths()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <path d=""M10,10L100,100"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));

        var shape = sceneInfo.scene.root.children[0].drawables[0] as Shape;
        Assert.IsNotNull(shape);
        Assert.AreEqual(1, shape.contours.Length);
        
        var segs = shape.contours[0].segments;
        Assert.AreEqual(2, segs.Length);
        Assert.AreEqual(new Vector2(10, 10), segs[0].p0);
        Assert.AreEqual(new Vector2(100, 100), segs[1].p0);
    }

    [Test]
    public void ImportSVG_SupportsLines()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <line x1=""10"" y1=""10"" x2=""100"" y2=""100"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));

        var path = sceneInfo.scene.root.children[0].drawables[0] as Unity.VectorGraphics.Path;
        Assert.IsNotNull(path);

        var segs = path.contour.segments;
        Assert.AreEqual(2, segs.Length);
        Assert.AreEqual(new Vector2(10, 10), segs[0].p0);
        Assert.AreEqual(new Vector2(100, 100), segs[1].p0);
    }

    [Test]
    public void ImportSVG_SupportsPolylines()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <polyline points=""10,10,100,100"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));

        var shape = sceneInfo.scene.root.children[0].drawables[0] as Shape;
        Assert.IsNotNull(shape);
        Assert.AreEqual(1, shape.contours.Length);

        var segs = shape.contours[0].segments;
        Assert.AreEqual(2, segs.Length);
        Assert.AreEqual(new Vector2(10, 10), segs[0].p0);
        Assert.AreEqual(new Vector2(100, 100), segs[1].p0);
    }

    [Test]
    public void ImportSVG_SupportsPolygons()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <polygon points=""10,10,100,100"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));

        var shape = sceneInfo.scene.root.children[0].drawables[0] as Shape;
        Assert.IsNotNull(shape);
        Assert.AreEqual(1, shape.contours.Length);

        var segs = shape.contours[0].segments;
        Assert.AreEqual(2, segs.Length);
        Assert.AreEqual(new Vector2(10, 10), segs[0].p0);
        Assert.AreEqual(new Vector2(100, 100), segs[1].p0);
    }

    [Test]
    public void ImportSVG_SupportsSolidFills()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <rect x=""5"" y=""10"" width=""100"" height=""20"" fill=""red"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        var rect = sceneInfo.scene.root.children[0].drawables[0] as Rectangle;
        var fill = rect.fill as SolidFill;
        Assert.AreEqual(Color.red, fill.color);
    }

    [Test]
    public void ImportSVG_SupportsLinearGradientFills()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <defs>
                    <linearGradient id=""grad"">
                        <stop offset=""0%"" stop-color=""blue"" />
                        <stop offset=""100%"" stop-color=""red"" />
                    </linearGradient>
                </defs>
                <rect x=""5"" y=""10"" width=""100"" height=""20"" fill=""url(#grad)"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        var rect = sceneInfo.scene.root.children[0].drawables[0] as Rectangle;
        var fill = rect.fill as GradientFill;
        Assert.AreEqual(GradientFillType.Linear, fill.type);
        Assert.AreEqual(2, fill.stops.Length);
        Assert.AreEqual(0.0f, fill.stops[0].stopPercentage);
        Assert.AreEqual(Color.blue, fill.stops[0].color);
        Assert.AreEqual(1.0f, fill.stops[1].stopPercentage);
        Assert.AreEqual(Color.red, fill.stops[1].color);
    }

    [Test]
    public void ImportSVG_SupportsRadialGradientFills()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <defs>
                    <radialGradient id=""grad"">
                        <stop offset=""0%"" stop-color=""blue"" />
                        <stop offset=""100%"" stop-color=""red"" />
                    </radialGradient>
                </defs>
                <rect x=""5"" y=""10"" width=""100"" height=""20"" fill=""url(#grad)"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        var rect = sceneInfo.scene.root.children[0].drawables[0] as Rectangle;
        var fill = rect.fill as GradientFill;
        Assert.AreEqual(GradientFillType.Radial, fill.type);
        Assert.AreEqual(2, fill.stops.Length);
        Assert.AreEqual(0.0f, fill.stops[0].stopPercentage);
        Assert.AreEqual(Color.blue, fill.stops[0].color);
        Assert.AreEqual(1.0f, fill.stops[1].stopPercentage);
        Assert.AreEqual(Color.red, fill.stops[1].color);
    }

    [Test]
    public void ImportSVG_SupportsFillOpacities()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <rect x=""5"" y=""10"" width=""100"" height=""20"" fill=""red"" fill-opacity=""0.5"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        var rect = sceneInfo.scene.root.children[0].drawables[0] as Rectangle;
        var fill = rect.fill as SolidFill;
        Assert.AreEqual(0.5f, fill.color.a);
    }

    [Test]
    public void ImportSVG_SupportsGroups()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <g>
                    <rect x=""5"" y=""10"" width=""100"" height=""20"" />
                    <rect x=""5"" y=""50"" width=""100"" height=""20"" />
                </g>
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        Assert.AreEqual(1, sceneInfo.scene.root.children.Count);

        var group = sceneInfo.scene.root.children[0];
        Assert.AreEqual(2, group.children.Count);
    }
}
