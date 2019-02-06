using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite
{
    internal class CIUtils
    {
        internal const string UpmCIUtilsId = "upm-ci-utils";

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

        internal static string _Pack(string command, string path, string destinationPath)
        {
            //Create a copy of the package on the temp folder so that it can be modified 

            var launcher = new NodeLauncher();
            launcher.WorkingDirectory = path;
            launcher.Script = GetCIUtilsScript();
            launcher.Args = command + " pack --artifacts-path . --npm-path \"" + NodeLauncher.NpmScriptPath + "\"";
            launcher.Launch();

            var packageName = launcher.OutputLog.ToString().Trim();

            //Copy the file to the destinationPath
            string finalPackagePath = Path.Combine(destinationPath, packageName);
            
            if (File.Exists(finalPackagePath))
                File.Delete(finalPackagePath);

            File.Move(Path.Combine(path, packageName), finalPackagePath);

            return packageName;
        }
    }
}
