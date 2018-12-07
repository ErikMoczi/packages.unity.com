﻿using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;

namespace Unity.Tiny
{
    internal abstract class BasicServer
    {
        protected BasicServer(string name, bool useIPC)
        {
            Name = name;
            UseIPC = useIPC;

            // Restore process if its already running
            try
            {
                ServerProcess = BackupPID > 0 ? Process.GetProcessById(BackupPID) : null;
            }
            catch (ArgumentException)
            {
                ServerProcess = null;
            }

            if (ServerProcess == null || ServerProcess.HasExited || ServerProcess.ProcessName != TinyShell.ToolsManagerNativeName())
            {
                BackupPID = 0;
                BackupPort = 0;
                BackupIPCPort = 0;
            }
            else
            {
                Port = BackupPort;
                IPCPort = BackupIPCPort;
                Listening = SetupIPC();
            }
        }

        protected enum ServerEvent { Connected, DataReceived, Disconnected, Broadcast, Reconnect };
        private Process ServerProcess { get; set; }
        protected string Name { get; set; }
        protected abstract string[] ShellArgs { get; }
        public bool Listening { get; private set; }
        public int Port { get; private set; }
        public abstract Uri URL { get; }

        protected bool UseIPC { get; private set; }
        protected IPCStream IPCStream { get; private set; }
        private int IPCPort { get; set; }

        private int BackupPID
        {
            get => EditorPrefs.GetInt($"Unity.Tiny.{Name}.PID", 0);
            set => EditorPrefs.SetInt($"Unity.Tiny.{Name}.PID", value);
        }

        private int BackupPort
        {
            get => EditorPrefs.GetInt($"Unity.Tiny.{Name}.Port", 0);
            set => EditorPrefs.SetInt($"Unity.Tiny.{Name}.Port", value);
        }

        private int BackupIPCPort
        {
            get => EditorPrefs.GetInt($"Unity.Tiny.{Name}.IPCPort", 0);
            set => EditorPrefs.SetInt($"Unity.Tiny.{Name}.IPCPort", value);
        }

        public static string LocalIP
        {
            get
            {
                string localIP;
                try
                {
                    // Connect a UDP socket and read its local endpoint. This is more accurate
                    // way when there are multi ip addresses available on local machine.
                    using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                    {
                        socket.Connect("8.8.8.8", 65530);
                        IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                        localIP = endPoint.Address.ToString();
                    }
                }
                catch (SocketException)
                {
                    // Network unreachable? Use loopback address
                    localIP = "127.0.0.1";
                }
                return localIP;
            }
        }

        private bool ParseServerListening(string line)
        {
            var regex = new Regex(@"\[Server\] listening on port (\d+)");
            var match = regex.Match(line);
            if (match.Success)
            {
                Port = int.Parse(match.Groups[1].Value);
                return true;
            }
            return false;
        }

        private bool ParseIPCListening(string line)
        {
            var regex = new Regex(@"\[Server\] ipc listening on port (\d+)");
            var match = regex.Match(line);
            if (match.Success)
            {
                IPCPort = int.Parse(match.Groups[1].Value);
                return true;
            }
            return false;
        }

        private bool SetupProcess()
        {
            // Setup data received handlers with a signal event
            string stdout = "", stderr = "";
            bool serverListening = false, ipcListening = false;
            ManualResetEvent isRunning = new ManualResetEvent(false);
            void outputReceived(object sender, DataReceivedEventArgs args)
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    if (!Listening)
                    {
                        stdout += args.Data;
                        if (!args.Data.EndsWith(Environment.NewLine))
                        {
                            stdout += Environment.NewLine;
                        }
                        ipcListening |= !UseIPC || ParseIPCListening(args.Data);
                        serverListening |= ParseServerListening(args.Data);
                        if (serverListening && ipcListening)
                        {
                            isRunning.Set();
                        }
                    }
                    //UnityEngine.Debug.Log($"{Name}: {args.Data}");
                }
            }
            void errorReceived(object sender, DataReceivedEventArgs args)
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    if (!Listening)
                    {
                        stderr += args.Data;
                        if (!args.Data.EndsWith(Environment.NewLine))
                        {
                            stderr += Environment.NewLine;
                        }
                    }
                    //UnityEngine.Debug.LogError($"{Name} error: {args.Data}");
                }
            }

            // Start server
            ServerProcess = TinyShell.RunToolNoWait(Name.ToLower(), ShellArgs, outputReceived, errorReceived);

            // Wait for server to start listening
            var running = ServerProcess != null && isRunning.WaitOne(10000);

            // Check if server process state is valid
            if (!running || ServerProcess.HasExited || !string.IsNullOrWhiteSpace(stderr))
            {
                var msg = $"Failed to start {Name}.";
                if (!string.IsNullOrEmpty(stderr))
                {
                    msg += $"\n{stderr}";
                }
                else if (!string.IsNullOrEmpty(stdout))
                {
                    msg += $"\n{stdout}";
                }
                UnityEngine.Debug.LogError(msg);
                Close();
                return false;
            }

            // Cancel data received handlers
            ServerProcess.CancelOutputRead();
            ServerProcess.CancelErrorRead();
            ServerProcess.OutputDataReceived -= outputReceived;
            ServerProcess.ErrorDataReceived -= errorReceived;
            return true;
        }

        private bool SetupIPC()
        {
            if (UseIPC)
            {
                IPCStream = new IPCStream();
                if (!IPCStream.Connect(IPCPort))
                {
                    UnityEngine.Debug.LogError("Failed to connect IPC stream.");
                    Close();
                    return false;
                }
                IPCStream.OnDataReceived += OnIPCDataReceived;
                IPCStream.OnClosed += OnIPCClosed;
                IPCStream.StartReadAsync();
            }
            return true;
        }

        public virtual bool Listen(int port)
        {
            if (Listening)
            {
                return true;
            }

            Port = port;
            if (!SetupProcess())
            {
                return false;
            }
            if (!SetupIPC())
            {
                return false;
            }

            BackupPID = ServerProcess?.Id ?? 0;
            BackupPort = Port;
            BackupIPCPort = IPCPort;
            Listening = true;
            return true;
        }

        public virtual void Close()
        {
            if (!Listening)
            {
                return;
            }

            Listening = false;
            BackupIPCPort = 0;
            BackupPort = 0;
            BackupPID = 0;

            if (IPCStream != null)
            {
                IPCStream.OnDataReceived -= OnIPCDataReceived;
                IPCStream.OnClosed -= OnIPCClosed;
                IPCStream.Close();
                IPCStream = null;
            }

            if (ServerProcess != null)
            {
                if (!ServerProcess.HasExited)
                {
                    ServerProcess.Kill();
                }
                ServerProcess.Dispose();
                ServerProcess = null;
            }
        }

        protected virtual void OnIPCDataReceived(object sender, byte[] data)
        {
            // implement in derived class
        }

        protected virtual void OnIPCClosed(object sender, EventArgs args)
        {
            // implement in derived class
        }
    }
}
