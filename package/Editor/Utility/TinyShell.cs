

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    using Debug = UnityEngine.Debug;

    internal class ShellProcessOutput
    {
        public bool Succeeded { get; set; } = true;
        public string Command { get; set; }
        public string CommandOutput { get; set; }
        public string FullOutput { get; set; }
        public string ErrorOutput { get; set; }
        public int ExitCode { get; set; }
    }

    internal class ShellProcessArgs
    {
        public const int DefaultMaxIdleTimeInMilliseconds = 30000;
        
        public static readonly ShellProcessArgs Default = new ShellProcessArgs();

        public DirectoryInfo WorkingDirectory { get; set; }
        public IEnumerable<string> ExtraPaths { get; set; }
        public bool ThrowOnError { get; set; } = true;
        public int MaxIdleTimeInMilliseconds { get; set; } = DefaultMaxIdleTimeInMilliseconds;
    }

    internal static class EnumerableUtility
    {
        public static IEnumerable<T> AsEnumerable<T>(this T obj)
        {
            yield return obj;
        }
    }

    internal static class TinyShell
    {
        #region Constants
#if UNITY_EDITOR_WIN
        private const char s_PathSeparator = ';';
#else
        private const char s_PathSeparator = ':';
#endif
        #endregion

        public static string ToolsManagerNativeName()
        {
            var toolsManager = "TinyToolsManager";
#if UNITY_EDITOR_WIN
            return $"{toolsManager}-win";
#elif UNITY_EDITOR_OSX
            return $"{toolsManager}-macos";
#else
            throw new NotImplementedException();
#endif
        }

        public static bool RunTool(string name, params string[] args)
        {
            var dir = new DirectoryInfo(TinyRuntimeInstaller.GetToolDirectory("manager"));
            var shellArgs = new ShellProcessArgs() { WorkingDirectory = dir, ExtraPaths = dir.FullName.AsEnumerable() };
            var result = RunInShell($"{ToolsManagerNativeName()} {name} {string.Join(" ", args)}", shellArgs);
            return result.Succeeded;
        }

        public static Process RunToolNoWait(string name, string[] args, DataReceivedEventHandler outputReceived = null, DataReceivedEventHandler errorReceived = null)
        {
            var dir = new DirectoryInfo(TinyRuntimeInstaller.GetToolDirectory("manager"));
            var shellArgs = new ShellProcessArgs() { WorkingDirectory = dir, ExtraPaths = dir.FullName.AsEnumerable() };
            return RunNoWait(ToolsManagerNativeName(), $"{name} {string.Join(" ", args)}", shellArgs, outputReceived, errorReceived);
        }

        public static Process RunNoWait(string executable, string arguments, ShellProcessArgs processArgs, DataReceivedEventHandler outputReceived = null, DataReceivedEventHandler errorReceived = null)
        {
            try
            {
                var exeDir = Where(executable, processArgs.ExtraPaths);
                if (exeDir == null)
                {
                    if (processArgs.ThrowOnError)
                    {
                        throw new FileNotFoundException($"Could not find command '{executable}' in the given search locations.");
                    }
                    return null;
                }
                return StartProcess(Path.Combine(exeDir.FullName, executable), arguments, processArgs.WorkingDirectory, outputReceived, errorReceived);
            }
            catch (Exception e)
            {
                TinyEditorAnalytics.SendException("Shell.RunNoWait", e);
                throw;
            }
        }

        public static ShellProcessOutput RunInShell(string command, ShellProcessArgs args)
        {
            Assert.IsFalse(string.IsNullOrEmpty(command));
            Assert.IsNotNull(args);
            
            try
            {
                var extraPaths = args.ExtraPaths;
                var workingDirectory = args.WorkingDirectory;
                var throwOnError = args.ThrowOnError;

                var runOutput = new ShellProcessOutput();

                var hasErrors = false;
                var output = new StringBuilder();
                var logOutput = new StringBuilder();
                var errorOutput = new StringBuilder();
                
                // Setup shell command
                if (extraPaths != null)
                {
                    var sb = new StringBuilder(128);
                    foreach (var part in extraPaths)
                    {
                        if (string.IsNullOrEmpty(part))
                            continue;

                        if (sb.Length > 0)
                        {
                            sb.Append(s_PathSeparator);
                        }

#if UNITY_EDITOR_WIN
                        sb.Append(part.Trim('"'));
#else
                        sb.Append(part[0] == '"' ? part : part.DoubleQuoted());
#endif
                    }

#if UNITY_EDITOR_WIN
                    command = $"SET PATH={sb}{s_PathSeparator}%PATH%{Environment.NewLine}{command}";
#else
                    command = $"export PATH={sb}{s_PathSeparator}$PATH{Environment.NewLine}{command}";
#endif
                }
                
                LogProcessData($"TINY SHELL> {(workingDirectory?.FullName ?? new DirectoryInfo(".").FullName)}",
                    logOutput);
                LogProcessData(command, logOutput);

                // Setup temporary command file
                var tmpCommandFile = Path.GetTempPath() + Guid.NewGuid().ToString();
#if UNITY_EDITOR_WIN
                tmpCommandFile += ".bat";
#else
                tmpCommandFile += ".sh";
#endif
                File.WriteAllText(tmpCommandFile, command);

                // Prepare data received handlers
                DataReceivedEventHandler outputReceived = (sender, e) =>
                {
                    LogProcessData(e.Data, output);
                    logOutput.AppendLine(e.Data);
                };
                DataReceivedEventHandler errorReceived = (sender, e) =>
                {
                    LogProcessData(e.Data, output);
                    logOutput.AppendLine(e.Data);
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        errorOutput.AppendLine(e.Data);
                        hasErrors = true;
                    }
                };

                // Run command in shell and wait for exit
                try
                {
#if UNITY_EDITOR_WIN
                    using (var process = StartProcess("cmd.exe", $"/Q /C \"{tmpCommandFile}\"", workingDirectory, outputReceived, errorReceived))
#else
                    using (var process = StartProcess("bash", $"\"{tmpCommandFile}\"", workingDirectory, outputReceived,
                        errorReceived))
#endif
                    {
                        var exitCode = WaitForProcess(process, output, args.MaxIdleTimeInMilliseconds);
                        runOutput.ExitCode = exitCode;
                        runOutput.Command = command;
                        runOutput.CommandOutput = output.ToString();
                        runOutput.FullOutput = logOutput.ToString();
                        runOutput.ErrorOutput = errorOutput.ToString();
                        LogProcessData($"Process exited with code '{exitCode}'", logOutput);
                        hasErrors |= (exitCode != 0);
                    }
                }
                finally
                {
                    File.Delete(tmpCommandFile);
                }

                if (hasErrors && throwOnError)
                {
                    throw new Exception($"{TinyConstants.ApplicationName}: " + errorOutput.ToString());
                }

                runOutput.Succeeded = !hasErrors;

                return runOutput;
            }
            catch (Exception e)
            {
                TinyEditorAnalytics.SendException("Shell.RunInShell", e);
                throw;
            }
        }

        private static Process StartProcess(string command, string arguments, DirectoryInfo workingDirectory = null, DataReceivedEventHandler outputReceived = null, DataReceivedEventHandler errorReceived = null)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory?.FullName ?? new DirectoryInfo(".").FullName,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };

            if (outputReceived != null)
            {
                process.OutputDataReceived += outputReceived;
            }
            if (errorReceived != null)
            {
                process.ErrorDataReceived += errorReceived;
            }

            process.Start();

            if (outputReceived != null)
            {
                process.BeginOutputReadLine();
            }
            if (errorReceived != null)
            {
                process.BeginErrorReadLine();
            }
            return process;
        }

        private static void LogProcessData(string data, StringBuilder output)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            Console.WriteLine(data); // Editor.log
            output.AppendLine(data);
        }

        private static int WaitForProcess(Process process, StringBuilder output, int maxIdleTime)
        {
            for (;;)
            {
                var len = output.Length;

                if (process.WaitForExit(maxIdleTime))
                {
                    // WaitForExit with a timeout will not wait for async event handling operations to finish.
                    // To ensure that async event handling has been completed, call WaitForExit that takes no parameters.
                    // See remarks: https://msdn.microsoft.com/en-us/library/ty0d8k56(v=vs.110)
                    process.WaitForExit();

                    return process.ExitCode;
                }

                if (output.Length != len)
                {
                    continue;
                }

                // idle for too long with no output? -> kill
                // nb: testing the process threads WaitState doesn't work on OSX
                Debug.LogError("Idle process detected. See console for more details.");
                process.Kill();
                return -1;
            }
        }

        private static DirectoryInfo Where(string exec, IEnumerable<string> extraPaths)
        {
            if (extraPaths == null)
            {
                return null;
            }
            return (from path in extraPaths
                where Directory.Exists(path)
                let files = Directory.EnumerateFiles(path, $"{exec}*")
                from file in files
                where Path.GetFileNameWithoutExtension(file) == exec
                select new DirectoryInfo(path)).FirstOrDefault();
        }
    }
}

