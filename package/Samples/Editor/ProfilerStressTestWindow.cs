#if RM_SAMPLES

using EditorDiagnostics;
using UnityEngine;

/*
 * Event Viewer window stress test.  Events are pushed to this window from ResourceManager/Samples/ProfilerStressTest.cs.
 */
internal class ProfilerStressTestWindow : EventViewerWindow
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
        return graph == "EventStress";
    }
    protected override bool showEventPanel { get { return true; } }

    protected override void OnInitializeGraphView(EventGraphListView gv)
    {
        gv.DefineGraph("EventStress", 0,
            new GraphLayerBarChartMesh(0, "Stuff", "Misc Stuff", Color.green),
            new GraphLayerBarChartMesh(1, "Stuff", "Misc Stuff", Color.red * .8f),
            new GraphLayerBarChartMesh(2, "Stuff", "Misc Stuff", Color.blue * .5f)
            );
    }

}
#endif