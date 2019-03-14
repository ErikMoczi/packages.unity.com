using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite
{
    internal class CIUtils
    {
        internal const string UpmCIUtilsId = "upm-ci-utils@0.8.7";

        internal static string GetCIUtilsScript()
        {
            var persistentDataPath = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "../../Unity"));
            var upmTemplateUtilsPath = Path.Combine(persistentDataPath, UpmCIUtilsId);
            var buildScript = Path.Combine(upmTemplateUtilsPath, "node_modules/upm-ci-utils/index.js");

            if (File.Exists(buildScript))
                return buildScript;

            if (!Directory.Exists(upmTemplateUtilsPath))
                Directory.CreateDirectory(upmTemplateUtilsPath);

            var launcher = new NodeLauncher();
            launcher.NpmLogLevel = "error";
            launcher.NpmRegistry = NodeLauncher.BintrayNpmRegistryUrl;
            launcher.WorkingDirectory = upmTemplateUtilsPath;
            launcher.NpmPrefix = ".";

            try
            {
                launcher.NpmInstall(UpmCIUtilsId);
            }
            catch (ApplicationException exception)
            {
                exception.Data["code"] = "installFailed";
                throw exception;
            }

            return File.Exists(buildScript) ? buildScript : string.Empty;
        }

        internal static List<string> _Pack(string command, string path, string destinationPath)
        {
            //Create a copy of the package on the temp folder so that it can be modified

            var launcher = new NodeLauncher();
            launcher.WorkingDirectory = path;
            launcher.Script = GetCIUtilsScript();
            launcher.Args = command + " pack --npm-path \"" + NodeLauncher.NpmScriptPath + "\"";
            launcher.Launch();

            var outputLines = launcher.OutputLog.ToString().Trim().Split(Environment.NewLine.ToCharArray());
            List<string> packagePaths = new List<string>();

            Regex packageNameRegex = new Regex(@"^com.*tgz$",
                RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

            foreach (var line in outputLines)
            {
                var match = packageNameRegex.Match(line);
                if (match.Success) {
                    //Copy the file to the destinationPath
                    string finalPackagePath = Path.Combine(destinationPath, line);

                    if (File.Exists(finalPackagePath))
                    {
                        File.Delete(finalPackagePath);
                    }

                    var packagePath = Path.Combine(path, "upm-ci~/packages", line);

                    Debug.LogFormat("Moving {0} to {1}", packagePath, finalPackagePath);
                    File.Move(packagePath, finalPackagePath);
                    packagePaths.Add(packagePath);
                }
            }

            //See if upm-ci~ exists and remove
            if (Directory.Exists(Path.Combine(path, "upm-ci~")))
            {
                Directory.Delete(Path.Combine(path, "upm-ci~"), true);
            }
            
            return packagePaths;
        }
    }
}
