using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class DiffEvaluation : BaseValidation
    {
        public DiffEvaluation()
        {
            TestName = "Package Diff Evaluation";
            TestDescription = "Produces a report of what's been changed in this version of the package.";
            TestCategory = TestCategory.DataValidation;
            SupportedValidations = new[] { ValidationType.AssetStore, ValidationType.LocalDevelopment, ValidationType.Publishing };
        }

        internal class PackageCompareData
        {
            public List<string> Added { get; set; }
            public List<string> Removed { get; set; }
            public List<string> Modified { get; set; }
            public StringBuilder TreeOutput { get; set; }

            internal PackageCompareData()
            {
                Added = new List<string>();
                Removed = new List<string>();
                Modified = new List<string>();

                TreeOutput = new StringBuilder();
            }
        }

        protected override void Run()
        {
            // no previous package was found.
            if (Context.PreviousPackageInfo == null)
            {
                TestOutput.Add("No previous package version. Skipping diff evaluation.");
                TestState = TestState.NotRun;
                return;
            }

            // Flag certain file types are requiring special attention.
            // Asmdef - can cause breaks on client's updates to packages.
            // package.json - Will change infomation in UI
            //      - Diff actual file, report what changed...
            // Meta files - if all meta files have changed, that's a red flag
            // if there are no common files, all files have changed,
            GenerateReport(ValidationSuiteReport.resultsPath, Context.PublishPackageInfo, Context.PreviousPackageInfo);

            TestState = TestState.Succeeded;
        }

        public void GenerateReport(string outputPath, VettingContext.ManifestData package1, VettingContext.ManifestData package2)
        {
            // no previous package was found.
            if (Context.PreviousPackageInfo == null)
            {
                TestState = TestState.NotRun;
                return;
            }

            var compareData = new PackageCompareData();

            compareData.TreeOutput.AppendLine("<" + package1.name + ">");
            Compare(compareData, package1.path, package2.path, 1);

            string fileName = Path.Combine(outputPath, package1.name + "@" + package1.version) + ".delta";
            StringBuilder Outout = new StringBuilder();
            Outout.AppendLine("Package Update Delta Evaluation");
            Outout.AppendLine("-------------------------------");
            Outout.AppendLine("");
            Outout.AppendLine("Package Name: " + package1.name);
            Outout.AppendLine("Package Version: " + package1.version);
            Outout.AppendLine("Compared to Version: " + package2.version);
            Outout.AppendLine("");
            if (compareData.Added.Any())
            {
                Outout.AppendLine("New in package:");
                foreach (var addedFile in compareData.Added)
                {
                    Outout.AppendLine("    " + addedFile.Substring(package2.path.Length));
                }

                Outout.AppendLine("");
            }

            if (compareData.Removed.Any())
            {
                Outout.AppendLine("Removed from package:");
                foreach (var removedFile in compareData.Removed)
                {
                    Outout.AppendLine("    " + removedFile.Substring(package1.path.Length));
                }

                Outout.AppendLine("");
            }

            if (compareData.Modified.Any())
            {
                Outout.AppendLine("Modified:");
                foreach (var modifiedFile in compareData.Modified)
                {
                    Outout.AppendLine("    " + modifiedFile.Substring(package1.path.Length));
                }

                Outout.AppendLine("");
            }

            Outout.AppendLine("");
            Outout.AppendLine("Package Tree");
            Outout.AppendLine("------------");
            Outout.AppendLine("");
            Outout.Append(compareData.TreeOutput);

            File.WriteAllText(fileName, Outout.ToString());
        }

        private void Compare(PackageCompareData compareData, string path1, string path2, int depth = 0)
        {
            var AddedTag = "  ++ADDED++";
            var RemovedTag = "  --REMOVED--";
            var ModifiedTag = "  (MODIFIED)";
            var linePrefix = string.Empty;
            for (int i = 0; i < (depth * 4); i++)
                linePrefix += " ";

            // Take a snapshot of the file system.  
            List<String> files1 = string.IsNullOrEmpty(path1) ? new List<string>() : Directory.GetFiles(path1).Select(d => d.Substring(path1.Length + 1).ToLower()).ToList();
            List<String> files2 = string.IsNullOrEmpty(path2) ? new List<string>() : Directory.GetFiles(path2).Select(d => d.Substring(path2.Length + 1).ToLower()).ToList();

            foreach (var file in files1)
            {
                if (files2.Contains(file))
                {
                    var file1 = new FileInfo(Path.Combine(path1, file));
                    var file2 = new FileInfo(Path.Combine(path2, file));
                    if (file1.Length == file2.Length)
                    {
                        compareData.TreeOutput.AppendLine(linePrefix + file);
                    }
                    else
                    {
                        compareData.TreeOutput.AppendLine(linePrefix + file + ModifiedTag);
                        compareData.Modified.Add(Path.Combine(path1, file));
                    }
                }
                else
                {
                    compareData.TreeOutput.AppendLine(linePrefix + file + RemovedTag);
                    compareData.Removed.Add(Path.Combine(path1, file));
                }
            }

            foreach (var file in files2)
            {
                if (!files1.Contains(file))
                {
                    compareData.TreeOutput.AppendLine(linePrefix + file + AddedTag);
                    compareData.Added.Add(Path.Combine(path2, file));
                }
            }

            // Start by comparing directories
            List<String> dirs1 = string.IsNullOrEmpty(path1) ? new List<string>() : Directory.GetDirectories(path1).Select(d => d.Substring(path1.Length + 1).ToLower()).ToList();
            List<String> dirs2 = string.IsNullOrEmpty(path2) ? new List<string>() : Directory.GetDirectories(path2).Select(d => d.Substring(path2.Length + 1).ToLower()).ToList();
            depth++;

            foreach (var directory in dirs1)
            {
                if (dirs2.Contains(directory))
                {
                    compareData.TreeOutput.AppendLine(linePrefix + "<" + directory + ">");
                    Compare(compareData, Path.Combine(path1, directory), Path.Combine(path2, directory), depth);
                }
                else
                {
                    compareData.TreeOutput.AppendLine(linePrefix + "<" + directory + ">" + RemovedTag);
                    Compare(compareData, Path.Combine(path1, directory), null, depth);
                }
            }

            foreach (var directory in dirs2)
            {
                if (!dirs2.Contains(directory))
                {
                    compareData.TreeOutput.AppendLine(linePrefix + "<" + directory + ">" + AddedTag);
                    Compare(compareData, null, Path.Combine(path2, directory), depth);
                }
            }
        }
    }
}