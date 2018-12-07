using System;
using System.Diagnostics;
using System.IO;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.Tiny
{
    internal class HTTPServer : BasicServer
    {
        public static HTTPServer Instance { get; private set; }
        private string ContentDir { get; set; }
        public string ContentURL { get; private set; }

        protected override string[] ShellArgs
        {
            get
            {
                var projectDir = Path.GetFullPath(".");
                var unityVersion = InternalEditorUtility.GetUnityVersion();
                var profilerVersion = unityVersion.Major > 2018 || (unityVersion.Major == 2018 && unityVersion.Minor > 2) ?
                    0x20180306 : unityVersion.Major == 2018 && unityVersion.Minor == 2 ? 0x20180123 : 0x20170327;
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

        [TinyInitializeOnLoad]
        private static void Initialize()
        {
            Instance = new HTTPServer();
            TinyEditorApplication.OnCloseProject += (project, context) => { Instance.Close(); };
        }

        private HTTPServer() : base("httpserver", useIPC: false)
        {
        }

        private string Host(string contentDir, int port)
        {
            Close();

            ContentDir = contentDir;
            if (!base.Listen(port))
            {
                // Could not start HTTP server, use file URL
                return new Uri(Path.Combine(contentDir, "index.html")).AbsoluteUri;
            }

            UnityEngine.Debug.Log($"{TinyConstants.ApplicationName} project content hosted at {IPAddress}");
            return new UriBuilder("http", "localhost", Port).Uri.AbsoluteUri;
        }

        public void ReloadOrOpen(DirectoryInfo contentDir, int port)
        {
            if (contentDir != null)
            {
                // Get content URL from content directory
                ContentURL = Host(contentDir.FullName, port);

                // Reload or open content URL
                if (WebSocketServer.Instance.HasClients)
                {
                    WebSocketServer.Instance.SendReload();
                }
                else
                {
                    Application.OpenURL(ContentURL);
                }
            }
        }
    }
}
