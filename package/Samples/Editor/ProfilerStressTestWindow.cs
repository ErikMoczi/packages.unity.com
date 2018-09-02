using EditorDiagnostics;
using UnityEditor;
using UnityEngine;

public class ProfilerStressTestWindow : EventViewerWindow
{
    //[MenuItem("Window/Stress Profiler", priority = 2053)]
    static void ShowWindow()
    {
        var window = GetWindow<ProfilerStressTestWindow>();
        window.titleContent = new GUIContent("Event Stress Test", "Event Stress");
        window.Show();
    }
    
    protected override bool OnCanHandleEvent(string graph)
    {
        return graph == ProfilerStressTest.graphName;
    }
    protected override bool showEventPanel { get { return true; } }

    protected override void OnInitializeGraphView(EventGraphListView gv)
    {
        gv.DefineGraph(ProfilerStressTest.graphName, 0,
            new GraphLayerBarChartMesh(0, "Stuff", "Misc Stuff", Color.green),
            new GraphLayerBarChartMesh(1, "Stuff", "Misc Stuff", Color.red * .8f),
            new GraphLayerBarChartMesh(2, "Stuff", "Misc Stuff", Color.blue * .5f)
            );
    }

}
