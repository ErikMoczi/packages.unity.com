using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Unity.VectorGraphics.Editor;

public class SpriteDataTests
{
    [Test]
    public void EncodeOutlines_SplitsVectorsAndLengths()
    {
        var input = new List<Vector2[]> {
            new Vector2[] { new Vector2(0,0) },
            new Vector2[] { new Vector2(1,1), new Vector2(2,2) },
            new Vector2[] { new Vector2(3,3) }
        };
        var vectors = new List<Vector2>();
        var lengths = new List<int>();

        SVGPhysicsOutlineDataProvider.EncodeOutlines(input, ref vectors, ref lengths);
    
        Assert.AreEqual(4, vectors.Count);
        Assert.AreEqual(3, lengths.Count);

        Assert.AreEqual(new Vector2(0, 0), vectors[0]);
        Assert.AreEqual(new Vector2(1, 1), vectors[1]);
        Assert.AreEqual(new Vector2(2, 2), vectors[2]);
        Assert.AreEqual(new Vector2(3, 3), vectors[3]);

        Assert.AreEqual(1, lengths[0]);
        Assert.AreEqual(2, lengths[1]);
        Assert.AreEqual(1, lengths[2]);

        var outlines = SVGPhysicsOutlineDataProvider.DecodeOutlines(vectors, lengths);

        Assert.AreEqual(input, outlines);
    }
}
