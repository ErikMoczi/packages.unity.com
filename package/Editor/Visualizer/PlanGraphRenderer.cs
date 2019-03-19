using System;
using GraphVisualizer;
using UnityEditor.AI.Planner.Visualizer;

class PlanGraphRenderer : DefaultGraphRenderer
{
    public PlanGraphRenderer(Action<PlanGraphRenderer, IVisualizerNode> nodeCallback)
    {
        nodeClicked += node => nodeCallback(this, node as IVisualizerNode);
    }
}
