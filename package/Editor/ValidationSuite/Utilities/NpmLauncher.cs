using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.WSA;
using Debug = UnityEngine.Debug;

namespace UnityEditor.PackageManager.ValidationSuite
{    
    /// <summary>
    /// Standardize npm launch in order to make sure registry is always specified and npm path is escaped
    /// </summary>
    internal class NpmLauncher
    {
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

        public const string ProductionRepositoryUrl = "https://packages.unity.com/";

        public string Registry { get; set; }
        public string Command { get; set; }
        public string CommandArgs { get; set; }
        public string LogLevel { get; set; }
        public int WaitTime { get; set; }
        
        public StringBuilder OutputLog = new StringBuilder();
        public StringBuilder ErrorLog = new StringBuilder();

        public string WorkingDirectory
        {
            set { Process.StartInfo.WorkingDirectory = value; }
        }

        public Process Process { get; protected set; }
        
        public NpmLauncher()
        {
            WaitTime = 1000 * 60 * 10;        // 10 Minutes
            
            Process = new Process();
            Process.StartInfo.FileName = GetNodePath();
            Process.StartInfo.Arguments = GetArguments();
            Process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process.StartInfo.UseShellExecute = false;
            Process.StartInfo.RedirectStandardOutput = true;
            Process.StartInfo.RedirectStandardError = true;
        }

        protected string GetArguments()
        {
            var args = "\"" + GetNpmFilePath() + "\" " + Command + " " + CommandArgs;
            if (!string.IsNullOrEmpty(Registry))
                args += " --registry \"" + Registry + "\"";
            if (!string.IsNullOrEmpty(LogLevel))
                args += " --loglevel=" + LogLevel;

            return args;
        }

        public void Launch()
        {
            if (string.IsNullOrEmpty(Command))
                throw new Exception("No command set for npm;");
            
            Process.StartInfo.Arguments = GetArguments();
            
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                Process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                        outputWaitHandle.Set();
                    else
                        OutputLog.AppendLine(e.Data);
                };
                Process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                        errorWaitHandle.Set();
                    else
                        ErrorLog.AppendLine(e.Data);
                };

                Process.Start();

                Process.BeginOutputReadLine();
                Process.BeginErrorReadLine();

                // Wait for maximum of 10 minutes
                if (Process.WaitForExit(WaitTime))
                {
                    if (Process.ExitCode != 0)
                    {
                        Debug.LogError("Failed to run npm");
                        throw new ApplicationException("Launching npm has failed with command: " + Command + "\nOutput: " + OutputLog + "\nError: " + ErrorLog);
                    }
                }
                else
                {
                    Process.Kill();
                    throw new TimeoutException("Launching npm has failed with timeout for command: " + Command + "\nOutput: " + OutputLog + "\nError: " + ErrorLog);
                }
            }
        }

        public void Install(string packageFileName)
        {
            Command = "install";
            CommandArgs = "\"" + packageFileName + "\"";
            Launch();            
        }

        public void Pack(string packageId)
        {
            Command = "pack";
            CommandArgs = packageId;
            Launch();                        
        }
    }
}