#if RM_SAMPLES

using UnityEngine;
using EditorDiagnostics;

internal class ProfilerStressTest : MonoBehaviour
{
    internal static string graphName = "EventStress";
    //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void init()
    {
        new GameObject("StressTestObject", typeof(ProfilerStressTest));
    }

    int[] lastValue = new int[] {0, 0, 0 };
    private void Update()
    {
        int count = Random.Range(10, 100);
        for (int i = 0; i < count; i++)
        {
            int stream = Random.Range(0, 3);
            int val = lastValue[stream] + Random.Range(-3, 3);
            if (val < 0)
                val = 0;
            EventCollector.PostEvent(new DiagnosticEvent(graphName, "", Random.Range(0, 250).ToString(), stream, Time.frameCount, val, null));
            lastValue[stream] = val;
        }
    }
}
#endif