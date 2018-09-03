namespace UnityEditor.TestTools.TestRunner.GUI
{
    internal class FileOpenInfo : IFileOpenInfo
    {
        public string FilePath { get; set; }
        public int LineNumber { get; set; }

        public FileOpenInfo()
        {
            LineNumber = 1;
            FilePath = string.Empty;
        }
    }
}
