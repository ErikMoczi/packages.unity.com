using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEditor.PackageManager.ValidationSuite;
using Semver;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class ChangeLogValidation : BaseValidation
    {
        public ChangeLogValidation()
        {
            TestName = "ChangeLog Validation";
            TestDescription = "Validate Changelog contains entry for given package.";
            TestCategory = TestCategory.DataValidation;
            SupportedValidations = new[] { ValidationType.PackageManager };
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;

            // Check if the file exists first
            var changeLogPath = Path.Combine(Context.ProjectPackageInfo.path, Utilities.ChangeLogFilename);

            if(!System.IO.File.Exists(changeLogPath))
            {
                TestState = TestState.Failed;
                TestOutput.Add("Cannot find chanlog at: " + changeLogPath);
                return;
            }

            SemVersion packageJsonVersion;
            
            if (!SemVersion.TryParse(Context.ProjectPackageInfo.version, out packageJsonVersion))
            {
                TestState = TestState.Failed;
                TestOutput.Add(string.Format("Version format is not valid: {0} in: [{1}]", Context.ProjectPackageInfo.version, Context.ProjectPackageInfo.path));
                return;
            }
            // We must strip the -build<commit> off the prerelease
            var buildInfoIndex = packageJsonVersion.Prerelease.IndexOf("build");
            if (buildInfoIndex > 0)
            {
                var cleanPrerelease = packageJsonVersion.Prerelease.Substring(0, buildInfoIndex - 1);
                packageJsonVersion = packageJsonVersion.Change(null, null, null, cleanPrerelease, null);
            }
            else
            {
                packageJsonVersion = packageJsonVersion.Change(null, null, null, "", null);
            }

            // We are basically searching for a string ## [Version] - YYYY-MM-DD
            var changeLogLineRegex = @"## \[(?<version>(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-(0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(\.(0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*)?(\+[0-9a-zA-Z-]+(\.[0-9a-zA-Z-]+)*)?)\] - (?<date>\d{4}-\d{1,2}-\d{1,2})";
            
            var textChangeLog = File.ReadAllText(changeLogPath);

            MatchCollection matches = Regex.Matches(textChangeLog, changeLogLineRegex);
            if(matches.Count == 0)
            {
                TestState = TestState.Failed;
                TestOutput.Add(string.Format("Can't find any entries in changelog that fits `format: ## [x.y.z] - YYYY-MM-DD` in: [{0}]", changeLogPath));
                return;
            }

            int index = 1;
            Match found = null;
            foreach (Match match in matches)
            {
                SemVersion versionInChangelog;
                if (!SemVersion.TryParse(match.Groups["version"].ToString(), out versionInChangelog))
                {
                    TestState = TestState.Failed;
                    TestOutput.Add(string.Format("Version format {0} is not valid in: [{1}]", match.Groups["version"].ToString(), changeLogPath));
                    return;
                }
                
                if(versionInChangelog == packageJsonVersion)
                {
                    found = match;
                    DateTime date;
                    string[] dateFormats = { "yyyy-MM-dd", "yyyy-MM-d", "yyyy-M-dd", "yyyy-M-d" };
                    var dateToCheck = match.Groups["date"].ToString();
                    if(!DateTime.TryParseExact(dateToCheck, 
                                dateFormats, 
                                CultureInfo.InvariantCulture, 
                                DateTimeStyles.None, 
                                out date))
                    {
                        TestState = TestState.Failed;
                        TestOutput.Add(string.Format("Date {0} is not valid expecting format: YYYY-MM-DD in: [{1}]", dateToCheck, changeLogPath));
                    }
                    break;
                }
                index++;
            }

            if(found == null)
            {
                TestState = TestState.Failed;
                TestOutput.Add(string.Format("No changelog entry for version `{0}` (expected: `## [{0}] - YYYY-MM-DD`) found in: [{1}]", packageJsonVersion.ToString(), changeLogPath));
            }
            else if(found != null && index != 1)
            {
                TestState = TestState.Failed;
                TestOutput.Add(string.Format("Found changelog entry `{0}`, but it was not the first entry of the changelog (it was entry #{1}) in: [{2}]", found.ToString(), index, changeLogPath));
            }
        }
    }
}