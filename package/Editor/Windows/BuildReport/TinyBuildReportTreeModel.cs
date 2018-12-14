

using System.IO;

namespace Unity.Tiny
{
    /// <summary>
    /// Underlying data for the tree
    /// </summary>
    internal class TinyBuildReportTreeModel : TinyTreeModel
    {
        private static int m_IdCounter = 0;

        public int GetNewId
        {
            get { return m_IdCounter++; }
        }

        public TinyBuildReportTreeModel(IRegistry registry, TinyModule.Reference mainModule) : base(registry, mainModule)
        {
        }

        public TinyBuildReport GetBuildReport()
        {
            m_IdCounter = 0;

            var buildOptions = TinyBuildPipeline.WorkspaceBuildOptions;
            var buildDir = buildOptions.BuildFolder.FullName;
            var jsonFile = new FileInfo(Path.Combine(buildDir, "build-report.json"));
            if (jsonFile.Exists)
            {
                var json = File.ReadAllText(jsonFile.FullName);
                return TinyBuildReport.FromJson(json);
            }

            return null;
        }
    }
}

