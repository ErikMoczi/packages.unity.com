using System;

namespace UnityEditor.PackageManager.ValidationSuite
{
    internal class TemplateCIUtils : CIUtils
    {
        public static void ConvertProjectToTemplate(string projectPath = null, string destinationPath = null, bool forceReplace = true)
        {
            var launcher = new NodeLauncher();
            launcher.Script = GetCIUtilsScript();
            launcher.Args = "template convert";
            if (!string.IsNullOrEmpty(projectPath))
                launcher.Args += " --project-path \"" + projectPath + "\"";
            if (!string.IsNullOrEmpty(destinationPath))
                launcher.Args += " --dest \"" + destinationPath + "\"";
            if (forceReplace)
                launcher.Args += " --force-replace";
            launcher.Launch();
        }

        public static bool ValidateTemplatePackage(string packagePath, ref string outputLog, ref string errorLog)
        {
            var launcher = new NodeLauncher();
            launcher.Script = GetCIUtilsScript();
            launcher.Args = "template validate";
            if (!string.IsNullOrEmpty(packagePath))
                launcher.Args += " --package-path \"" + packagePath + "\"";
            try
            {
                launcher.Launch();
                return true;
            }
            catch (Exception)
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

        public static string Pack(string path, string destinationPath)
        {
            return _Pack("template", path, destinationPath);
        }
    }
}
