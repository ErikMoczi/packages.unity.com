using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.PackageManager.ValidationSuite
{
    internal class ProjectTemplateUtils
    {
        internal const string UpmTemplateUtilsId = "upm-template-utils@0.2.0";

        internal static string GetProjectTemplateUtilsScript()
        {
            var persistentDataPath = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "../../Unity"));
            var upmTemplateUtilsPath = Path.Combine(persistentDataPath, UpmTemplateUtilsId);
            var buildScript = Path.Combine(upmTemplateUtilsPath, "node_modules/upm-template-utils/index.js");

            if (File.Exists(buildScript))
                return buildScript;

            if (!Directory.Exists(upmTemplateUtilsPath))
                Directory.CreateDirectory(upmTemplateUtilsPath);

            var launcher = new NodeLauncher();
            launcher.NpmLogLevel = "error";
            launcher.NpmRegistry = NodeLauncher.StagingRepositoryUrl;
            launcher.WorkingDirectory = upmTemplateUtilsPath;

            try
            {
                launcher.NpmInstall(UpmTemplateUtilsId);
            }
            catch (ApplicationException exception)
            {
                exception.Data["code"] = "installFailed";
                throw exception;
            }

            return File.Exists(buildScript) ? buildScript : string.Empty;
        }

        internal static void ConvertProjectToTemplate(string projectPath = null, string destinationPath = null, bool forceReplace = true)
        {
            var launcher = new NodeLauncher();
            launcher.Script = GetProjectTemplateUtilsScript();
            launcher.Args = "template convert";
            if (!string.IsNullOrEmpty(projectPath))
                launcher.Args += " --project-path \"" + projectPath + "\"";
            if (!string.IsNullOrEmpty(destinationPath))
                launcher.Args += " --dest \"" + destinationPath + "\"";
            if (forceReplace)
                launcher.Args += " --force-replace";
            launcher.Launch();
        }

        internal static bool ValidateTemplatePackage(string packagePath, ref string outputLog, ref string errorLog)
        {
            var launcher = new NodeLauncher();
            launcher.Script = GetProjectTemplateUtilsScript();
            launcher.Args = "template validate";
            if (!string.IsNullOrEmpty(packagePath))
                launcher.Args += " --package-path \"" + packagePath + "\"";
            try
            {
                launcher.Launch();
                return true;
            }
            catch(Exception)
            {
                if (launcher.Process.HasExited && launcher.Process.ExitCode != 0)
                {
                    outputLog = launcher.OutputLog.ToString();
                    errorLog = launcher.ErrorLog.ToString();
                }
                else
                {
                    throw;
                }
            }
            return false;
        }
    }
}