using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEditor.Experimental.U2D.Animation
{
    public interface IWeightsGenerator
    {
        BoneWeight[] Calculate(Vector2[] vertices, Edge[] edges, Vector2[] controlPoints, Edge[] bones, int[] pins);
    }
}
