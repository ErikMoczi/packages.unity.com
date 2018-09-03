namespace UnityEditor.TestTools.TestRunner.GUI
{
    internal interface IFileOpenInfo
    {
        string FilePath { get; set; }
        int LineNumber { get; set; }
    }
}
