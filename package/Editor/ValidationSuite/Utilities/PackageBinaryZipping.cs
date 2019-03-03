#if UNITY_2018_1_OR_NEWER

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite
{
    public static class PackageBinaryZipping
    {
        /// <summary>
        /// Creates a zip file containing the .dlls built from .asmdefs in the given package.
        /// </summary>
        /// <param name="packageRootPath">The path to the root of the package</param>
        /// <param name="packageName">The name of the package</param>
        /// <param name="packageVersion">The version of the package</param>
        /// <param name="zipDirectory">The directory where the zip file will be placed</param>
        /// <param name="zipFilePath">Upon returning, this will contain the path to the generated zip file</param>
        /// <returns>True if the zip succeeds. False if the 7z fails to create the zip file.</returns>


        public static bool TryZipPackageBinaries(string packageRootPath, string packageName, string packageVersion, string zipDirectory, out string zipFilePath)
        {
            if (!Directory.Exists(packageRootPath))
            {
                throw new ArgumentException("Could not find package " + packageRootPath);
            }

            zipFilePath = Path.Combine(zipDirectory, PackageDataZipFilename(packageName, packageVersion));
            var assemblies = Utilities.AssembliesForPackage(packageRootPath);

            if (!assemblies.Any())
                return Zip(null, new string[0], zipFilePath);

            var assembliesPath = Path.GetDirectoryName(assemblies.First().outputPath);
            var badAssembly = assemblies.FirstOrDefault(a => Path.GetDirectoryName(a.outputPath) != assembliesPath);
            if (badAssembly != null)
                throw new ArgumentException(badAssembly.outputPath + " is in an unexpected directory and cannot be zipped.");

            return Zip(assembliesPath, assemblies.Select(a => Path.GetFileName(a.outputPath)).ToArray(), zipFilePath);
        }

        private static bool Zip(string directoryPath, string[] filenames, string zipFilePath)
        {
            var zipper = Get7zPath();

            zipFilePath = Path.GetFullPath(zipFilePath);
            File.Delete(zipFilePath);
            string inputArguments = String.Format("a -tzip -mx3 \"{0}\" {1}", zipFilePath, String.Join(" ", filenames.Select(f => "\"" + f + "\"").ToArray()));
            if (filenames.Length == 0)
                inputArguments += "-x!*";

            var processStartInfo = new ProcessStartInfo(zipper, inputArguments);
            if (directoryPath != null)
                processStartInfo.WorkingDirectory = directoryPath;

            var process = Process.Start(processStartInfo);
            process.WaitForExit();
            return process.ExitCode == 0;
        }

        private static string Get7zPath()
        {
#if (UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)
            string execFilename = "7za";
#else
            string execFilename = "7z.exe";
#endif
            string zipper = EditorApplication.applicationContentsPath + "/Tools/" + execFilename;
            if (!File.Exists(zipper))
                throw new FileNotFoundException("Could not find " + zipper);
            return zipper;
        }

        internal static bool Unzip(string zipFilePath, string destPath)
        {
            string zipper = Get7zPath();
            string inputArguments = string.Format("x -o\"{0}\" \"{1}\"", destPath, zipFilePath);
            var process = Process.Start(zipper, inputArguments);
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new IOException("Failed to unzip " + zipFilePath);

            return true;
        }

        internal static string PackageDataZipFilename(string packageName, string packageVersion)
        {
            return string.Format("{0}@{1}.zip", packageName, packageVersion);
        }
    }
}

#endif
