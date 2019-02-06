

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
            var artifactFile = buildOptions.GetArtifactFile("build-report.json");
            if (artifactFile.Exists)
            {
                var json = File.ReadAllText(artifactFile.FullName);
                return TinyBuildReport.FromJson(json);
            }

            return null;
        }
    }
}

