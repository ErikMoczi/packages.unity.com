using System.IO;
using System.Linq;
using System;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;

namespace UnityEditor.PackageManager.ValidationSuite.UI
{
    internal class PackageValidationWindow
    {
        private static string ResultsDisplay = "";
        private static bool ShowResultsDlg = false;

        [MenuItem("internal:Project/Packages/Validate")]
        internal static void ShowPackageManagerWindow()
        {
            ResultsDisplay = "";
            ShowResultsDlg = false;

            EditorApplication.update += Update;

            // ***** Hack *****  Until we have a better way to test a particular package, find the first package.
            var packagePath = FindFirstPackagePath();

            if (string.IsNullOrEmpty(packagePath))
            {
                EditorUtility.DisplayDialog("Invalid Operation", "No packages found in project", "OK");
                return;
            }

            if (!Directory.Exists(ValidationSuite.resultsPath))
                Directory.CreateDirectory(ValidationSuite.resultsPath);
            
            try
            {
                var context = new VettingContext();
                context.Initialize(packagePath);

                // Clear output file content
                var path = Path.Combine(ValidationSuite.resultsPath, context.ProjectPackageInfo.name + ".txt");
                File.WriteAllText(path, string.Format("Validation Suite Results for package \"{0}\"\r\n\r\n", packagePath));

                var testSuite = new ValidationSuite(SingleTestCompletedDelegate, AllTestsCompletedDelegate, context, path);

                testSuite.RunAsync();
            }
            catch(Exception e)
            {
                EditorUtility.DisplayDialog("Validation Setup Error", e.Message, "Dismiss", null);
            }
        }

        private static void Update()
        {
            if (ShowResultsDlg)
            {
                ShowResultsDlg = false;
                EditorApplication.update -= Update;
                EditorUtility.DisplayDialog("Validation Results", ResultsDisplay, "Dismiss", null);
            }
        }

        // This is called after every tests completes
        private static void SingleTestCompletedDelegate(IValidationTestResult testResult)
        {
            var path = testResult.ValidationTest.Suite.resultOutputPath;
            File.AppendAllText(path, string.Format("\r\nTest: \"{0}\"\r\nResult: {1}\r\n", testResult.ValidationTest.TestName, testResult.TestState));
            ResultsDisplay += string.Format("{0} - {1}\n", testResult.TestState, testResult.ValidationTest.TestName);
            if (testResult.TestOutput.Any())
            {
                File.AppendAllText(path, string.Join("\r\n", testResult.TestOutput.ToArray()) + "\r\n");
            }
        }

        private static void AllTestsCompletedDelegate(ValidationSuite suite, TestState testRunState)
        {
            File.AppendAllText(suite.resultOutputPath, "\r\nAll Done!  Result = " + testRunState);
            ShowResultsDlg = true;
        }

        private static string FindFirstPackagePath()
        {
            // This is a temporary function to find the first package.json until we have a mechanism in place to select one.
            var fileOfInterest = "package.json";

            // TODO: change to look in package directory once that works again...
            var paths = Directory.GetFiles("UnityPackageManager", fileOfInterest, SearchOption.AllDirectories);
            return paths.Length > 0 ? paths[0].Substring(0, paths[0].Length - (fileOfInterest.Length + 1)) : string.Empty;
        }
    }
}
