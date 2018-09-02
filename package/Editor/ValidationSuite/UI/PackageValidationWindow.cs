using System.IO;
using System.Linq;

namespace UnityEditor.PackageManager.ValidationSuite.UI
{
    internal class PackageValidationWindow
    {
        private static string resultsFilePath = "ValidationSuiteResults.txt";

        [MenuItem("internal:Project/Packages/Validate")]
        public static void ShowPackageManagerWindow()
        {
            // ***** Hack *****  Until we have a better way to test a particular package, find the first package.
            var path = FindFirstPackagePath();

            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Invalid Operation", "No packages found in project", "OK");
                return;
            }

            // Clear output file content
            File.WriteAllText(resultsFilePath, string.Format("Validation Suite Results for package \"{0}\"\r\n\r\n", path));

            // Check that we are using the right Unity version before we proceed.
            // Eventually, we could launch different functionality here based on version.
            string version = UnityEngine.Application.unityVersion;
            ValidationSuite testSuite = new ValidationSuite(SingleTestCompletedDelegate, AllTestsCompletednDelegate, path);

            testSuite.RunAsync();
        }

        // This is called after every tests completes
        private static void SingleTestCompletedDelegate(IValidationTestResult testResult)
        {
            File.AppendAllText(resultsFilePath, string.Format("\r\nTest: \"{0}\"\r\nResult: {1}\r\n", testResult.ValidationTest.TestName, testResult.TestState));
            if (testResult.TestOutput.Any())
            {
                File.AppendAllText(resultsFilePath, string.Join("\r\n", testResult.TestOutput.ToArray()) + "\r\n");
            }
        }

        private static void AllTestsCompletednDelegate(TestState testRunState)
        {
            File.AppendAllText(resultsFilePath, "\r\nAll Done!  Result = " + testRunState);
        }

        private static string FindFirstPackagePath()
        {
            // This is a temporary function to find the first package.json until we have a mechanism in place to select one.
            var fileOfInterest = "package.json";

            // TODO: change to look in package directory once that works again...
            var paths = Directory.GetFiles("Assets", fileOfInterest, SearchOption.AllDirectories);
            return paths.Length > 0 ? paths[0].Substring(0, paths[0].Length - (fileOfInterest.Length + 1)) : string.Empty;
        }
    }
}