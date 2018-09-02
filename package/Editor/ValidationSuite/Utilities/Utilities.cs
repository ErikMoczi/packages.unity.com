using System.Diagnostics;
using System.IO;
using System;
using UnityEngine;
using UnityEditor.PackageManager.ValidationSuite.ValidationTests;

namespace UnityEditor.PackageManager.ValidationSuite
{   
    internal class Utilities
    {
        internal const string PackageJsonFilename = "package.json";
        internal const string ChangeLogFilename = "CHANGELOG.md";
        internal const string EditorAssemblyDefintionSuffix = ".Editor.asmdef";
        internal const string EditorTestsAssemblyDefintionSuffix = ".EditorTests.asmdef";
        internal const string RuntimeAssemblyDefintionSuffix = ".Runtime.asmdef";
        internal const string RuntimeTestsAssemblyDefintionSuffix = ".RuntimeTests.asmdef";

        static string GetNodePath()
        {
            var nodePath = Path.Combine(EditorApplication.applicationContentsPath, "Tools");
            nodePath = Path.Combine(nodePath, "nodejs");
            #if UNITY_EDITOR_OSX
            nodePath = Path.Combine(nodePath, "bin");
            nodePath = Path.Combine(nodePath, "node");
            #elif UNITY_EDITOR_WIN
            nodePath = Path.Combine(nodePath, "node.exe");
            #endif
            return nodePath;
        }

        static string GetNpmFilePath()
        {
            var npmFilePath = Path.Combine(EditorApplication.applicationContentsPath, "Tools");
            npmFilePath = Path.Combine(npmFilePath, "nodejs");
            #if UNITY_EDITOR_OSX            
            npmFilePath = Path.Combine(npmFilePath, "lib");
            #endif
            npmFilePath = Path.Combine(npmFilePath, "node_modules");
            npmFilePath = Path.Combine(npmFilePath, "npm");
            npmFilePath = Path.Combine(npmFilePath, "bin");
            npmFilePath = Path.Combine(npmFilePath, "npm-cli.js");
            return npmFilePath;
        }

        internal static T GetDataFromJson<T>(string jsonFile)
        {
            return JsonUtility.FromJson<T>(File.ReadAllText(jsonFile));
        }

        internal static string CreatePackage(string path, string workingDirectory)
        {
            //No Need to delete the file, npm pack always overwrite: https://docs.npmjs.com/cli/pack
            var args =  GetNpmFilePath() + " pack " + Path.Combine(Path.Combine(Application.dataPath, ".."), path);
            var packageName = "";
            
            Process process = new Process();

            process.StartInfo.FileName = GetNodePath();
            process.StartInfo.Arguments = args;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (sender, arg) => {if(!string.IsNullOrEmpty(arg.Data.Trim())) packageName = arg.Data;};
 
            process.Start();
            process.BeginOutputReadLine();
            
            //Wait for maximum of 10 minutes
            process.WaitForExit(1000*60*10);

            if(!process.HasExited)
            {
                process.Kill();
                throw new TimeoutException("Creating package failed.");
            }

            if(process.ExitCode != 0)
                throw new ApplicationException("Creating package failed.");

            return packageName;
        }

        internal static string ExtractPackage(string package, string workingDirectory, string packageName)
        {
            //verify if package exists
            if(!package.EndsWith(".tgz"))
                throw new ArgumentException("Package should be a .tgz file");

            var fullPackagePath = Path.Combine(workingDirectory, package);
            var modulePath = Path.Combine(workingDirectory, "node_modules");
            if (!System.IO.File.Exists(fullPackagePath))
                throw new FileNotFoundException(fullPackagePath + " was not found.");
            
            //Clean node_modules if it exists
            if(System.IO.File.Exists(modulePath))
            {
                try{
                    Directory.Delete(modulePath, true);
                } catch(Exception e) {
                    throw e;
                }
            }
                
            var args = GetNpmFilePath() + " install \"" + package + "\" --loglevel=error";
            var process = new Process();

            process.StartInfo.FileName = GetNodePath();
            process.StartInfo.Arguments = args;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            //Wait 10 minutes for pack to happen
            process.WaitForExit(1000*60*10);
            if(!process.HasExited)
            {
                process.Kill();
                throw new TimeoutException("Creating package failed...");
            }
                
            if(process.ExitCode != 0)
                throw new ApplicationException("Creating package failed.");

            var extractedPackagePath = Path.Combine(modulePath, packageName);
            if(!System.IO.Directory.Exists(extractedPackagePath))
                throw new DirectoryNotFoundException(extractedPackagePath + " was not found.");

            return extractedPackagePath;
        
        }
    }
}