using System.IO;
using System.Linq;
using System;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;

namespace UnityEditor.PackageManager.ValidationSuite.UI
{
    internal class PackageValidationWindow
    {
        private static string resultsFilePath = "ValidationSuiteResults.txt";
        private static string ResultsDisplay = "";
        private static bool ShowResultsDlg = false;

        public static bool RunValidationTestsSuite()
        {
            ResultsDisplay = "";
            var packagePath = FindFirstPackagePath();

            if (string.IsNullOrEmpty(packagePath))
            {
                return false;
            }

            try
            {
                // Clear output file content
                File.WriteAllText(resultsFilePath, string.Format("Validation Suite Results for package \"{0}\"\r\n\r\n", packagePath));

                var package = new VettingContext();
                package.Initialize(packagePath);

                ValidationSuite testSuite = new ValidationSuite(SingleTestCompletedDelegate, AllTestsCompletedDelegate, package);

                testSuite.RunAsync();
            }
            catch(Exception e)
            {
                File.AppendAllText(resultsFilePath, string.Format("\r\nTest Setup Error: \"{0}\"\r\n", e.Message));
            }
            return true;
        }

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

            try
            {
                // Clear output file content
                File.WriteAllText(resultsFilePath, string.Format("Validation Suite Results for package \"{0}\"\r\n\r\n", packagePath));

                var package = new VettingContext();
                package.Initialize(packagePath);

                ValidationSuite testSuite = new ValidationSuite(SingleTestCompletedDelegate, AllTestsCompletedDelegate, package);

                testSuite.RunAsync();
            }
            catch(Exception e)
            {
                File.AppendAllText(resultsFilePath, string.Format("\r\nTest Setup Error: \"{0}\"\r\n", e.Message));
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
            File.AppendAllText(resultsFilePath, string.Format("\r\nTest: \"{0}\"\r\nResult: {1}\r\n", testResult.ValidationTest.TestName, testResult.TestState));
            ResultsDisplay += string.Format("{0} - {1}\n", testResult.TestState, testResult.ValidationTest.TestName);
            if (testResult.TestOutput.Any())
            {
                File.AppendAllText(resultsFilePath, string.Join("\r\n", testResult.TestOutput.ToArray()) + "\r\n");
            }
        }

        private static void AllTestsCompletedDelegate(TestState testRunState)
        {
            File.AppendAllText(resultsFilePath, "\r\nAll Done!  Result = " + testRunState);
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
