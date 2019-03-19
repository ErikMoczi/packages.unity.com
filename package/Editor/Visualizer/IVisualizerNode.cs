using System;
using GraphVisualizer;
using Unity.AI.Planner;
using UnityEngine;

namespace UnityEditor.AI.Planner.Visualizer
{
    interface IVisualizerNode
    {
        bool ExpansionNode { get; }
        string Label { get; }

        // From Node
        Node parent { get; }
        object content { get; }
    }
}
