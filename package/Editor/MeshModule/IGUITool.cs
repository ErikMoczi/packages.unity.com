namespace UnityEditor.Experimental.U2D.Animation
{
    internal interface IGUITool
    {
        int controlID { get; }
        void OnGUI();
        void OnInspectorGUI();
    }
}
