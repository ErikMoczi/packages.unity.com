using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.Tiny
{
    internal class HTTPServer : BasicServer
    {
        public static HTTPServer Instance { get; private set; }
        protected override string[] ShellArgs
        {
            get
            {
                var projectDir = Path.GetFullPath(".");
                var unityVersion = InternalEditorUtility.GetUnityVersion();
                var profilerVersion = 0x20181101;
                return new string[]
                {
                    $"-p {Port}",
                    $"-w {Process.GetCurrentProcess().Id}",
                    $"-c {ContentDir.DoubleQuoted()}",
                    $"-t {projectDir.DoubleQuoted()}",
                    $"-r {profilerVersion}"
                };
            }
        }
        public override Uri URL => new UriBuilder("http", LocalIP, Port).Uri;

        public Uri LocalURL => Listening ? new UriBuilder("http", "localhost", Port).Uri : new Uri(Path.Combine(ContentDir, "index.html"));
        private string ContentDir { get; set; }

        [TinyInitializeOnLoad(200)]
        private static void Initialize()
        {
            Instance = new HTTPServer();
            TinyEditorApplication.OnCloseProject += (project, context) =>
            {
                Instance.Close();
            };
        }

        private HTTPServer() : base("HTTPServer", useIPC: false)
        {
        }

        private void Host(string contentDir, int port)
        {
            // Close previous httpserver
            Close();

            // Start new httpserver
            ContentDir = contentDir;
            if (base.Listen(port))
            {
                UnityEngine.Debug.Log($"{TinyConstants.ApplicationName} project content hosted at {URL.AbsoluteUri}");
            }
        }

        public void ReloadOrOpen(string contentDir, int port)
        {
            if (port == 0 || string.IsNullOrEmpty(contentDir) || !Directory.Exists(contentDir))
            {
                return;
            }

            using (var progress = new TinyEditorUtility.ProgressBarScope())
            {
                progress.Update($"{TinyConstants.ApplicationName} Preview", "Starting local HTTP server...");

                // Get hosted URL from content directory
                Host(contentDir, port);

                // Reload or open content URL
                if (WebSocketServer.Instance.HasClients)
                {
                    WebSocketServer.Instance.SendReload();
                }
                else
                {
                    Application.OpenURL(LocalURL.AbsoluteUri);
                }
            }
        }
    }
}
