using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace Unity.Tiny
{
    internal class TinyGameView : TinyEditorWindowOverride<EditorWindow>
    {
        private static readonly List<TinyGameView> s_ActiveWindows = new List<TinyGameView>();

        private Uri QRCodeURL { get; set; }
        private Texture2D QRCodeTexture { get; set; }
        private int MinMaxBtnSize => 32;
        private bool Minimized { get; set; }

        public override void OnEnable()
        {
            base.OnEnable();
            s_ActiveWindows.Add(this);

            var imgui = new IMGUIContainer(OnGUI);
            Root.Add(imgui);
            imgui.StretchToParentSize();

            WebSocketServer.Instance.OnClientConnected += Refresh;
            WebSocketServer.Instance.OnClientDisconnected += Refresh;
        }

        public override void OnDisable()
        {
            WebSocketServer.Instance.OnClientDisconnected -= Refresh;
            WebSocketServer.Instance.OnClientConnected -= Refresh;

            base.OnDisable();
            s_ActiveWindows.Remove(this);
        }

        private static void Refresh(WebSocketServer.Client client)
        {
            Bridge.GameView.RepaintAll();
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.update += () =>
            {
                foreach (var window in s_ActiveWindows)
                {
                    window.Root.visible = HTTPServer.Instance != null && HTTPServer.Instance.Listening;
                }
            };
        }

        private void OnGUI()
        {
            var httpServer = HTTPServer.Instance;
            if (httpServer == null || !httpServer.Listening)
            {
                return;
            }

            // Update QR Code
            if (httpServer.URL != QRCodeURL || QRCodeTexture == null)
            {
                QRCodeURL = httpServer.URL;
                QRCodeTexture = QRCode.Generate(httpServer.URL);
            }

            var contentRect = new Rect(Root.contentRect.x, Root.contentRect.y + 14, Root.contentRect.width, Root.contentRect.height);
            var minMaxBtnRect = new Rect(0, contentRect.height - MinMaxBtnSize, MinMaxBtnSize, MinMaxBtnSize);
            if (Minimized)
            {
                // Draw maximize button
                GUILayout.BeginArea(minMaxBtnRect);
                if (GUILayout.Button(new GUIContent(QRCodeTexture, "Maximize connection information."), GUILayout.Width(MinMaxBtnSize), GUILayout.Height(MinMaxBtnSize)))
                {
                    Minimized = !Minimized;
                }
                GUILayout.EndArea();
            }
            else
            {
                // Draw background
                EditorGUI.DrawRect(contentRect, new Color(0, 0, 0, 0.6f));

                // Draw minimize button
                GUILayout.BeginArea(minMaxBtnRect);
                if (GUILayout.Button(new GUIContent(QRCodeTexture, "Minimize connection information."), GUILayout.Width(MinMaxBtnSize), GUILayout.Height(MinMaxBtnSize)))
                {
                    Minimized = !Minimized;
                }
                GUILayout.EndArea();

                // Draw GUI elements
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.FlexibleSpace();

                    // Draw build time stamp
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(new GUIContent($"Serving build made on {httpServer.BuildTimeStamp} at:"), EditorStyles.whiteLabel);
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.Space(15);

                    // Draw IP address label
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(new GUIContent(httpServer.URL.AbsoluteUri, "Open a new web browser preview."), EditorStyles.whiteLabel))
                        {
                            Application.OpenURL(httpServer.LocalURL.AbsoluteUri);
                        }
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.Space(15);

                    // Draw QR code texture
                    using (new GUILayout.HorizontalScope())
                    {
                        var size = Math.Min(contentRect.width, contentRect.height) / 3;
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(new GUIContent(QRCodeTexture, "Open a new web browser preview."), EditorStyles.whiteLabel, GUILayout.Width(size), GUILayout.Height(size)))
                        {
                            Application.OpenURL(httpServer.LocalURL.AbsoluteUri);
                        }
                        GUILayout.FlexibleSpace();
                    }

                    // Draw clients dropdown
                    var wsServer = WebSocketServer.Instance;
                    if (wsServer != null)
                    {
                        GUILayout.Space(15);

                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("Active Client:", EditorStyles.whiteLabel, GUILayout.ExpandWidth(false));

                            var size = contentRect.width / 3;
                            var index = wsServer.ActiveClientIndex;
                            if (index >= 0)
                            {
                                var clients = wsServer.Clients.Select(c => c.Label).ToArray();
                                wsServer.ActiveClientIndex = EditorGUILayout.Popup(index, clients, GUILayout.Width(size));
                            }
                            else
                            {
                                EditorGUILayout.Popup(0, new string[] { "None" }, GUILayout.Width(size));
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }

                    GUILayout.Space(15);

                    // Draw message about read only state
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(new GUIContent("Changes in the editor while in play mode will not be reflected in running clients."), EditorStyles.whiteLabel);
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }
    }
}
