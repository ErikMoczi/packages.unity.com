using System;

namespace UnityEditor.AI.Planner.Visualizer
{
    interface IPlanVisualizer
    {
        int MaxDepth { get; set; }
        int MaxChildrenNodes { get; set; }
        IVisualizerNode RootNodeOverride { get; set; }

        void Refresh();
    }
}
