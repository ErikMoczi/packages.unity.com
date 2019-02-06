

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Properties;
using UnityEngine;

namespace Unity.Tiny
{
    internal class PlayableAdHTML5Builder : ITinyBuilder
    {
        public ITinyBuildStep[] GetBuildSteps()
        {
            return new ITinyBuildStep[] 
            {
                new VerifyPlayableAdModuleStep(),
                new InsertOverlayStep()
            }.Union(new TinyHTML5Builder().GetBuildSteps()).ToArray();
        }

        private class VerifyPlayableAdModuleStep : ITinyBuildStep
        {
            public string Name => "Verify Playable Ad Module";

            public bool Enabled(TinyBuildContext context) => true;

            public bool Run(TinyBuildContext context)
            {
                if (!TinyHTML5Builder.UsesAdSupport(context.Options.Project))
                {
                    throw new NotSupportedException("Enable UTiny.PlayableAd module in order to build for this build configuration.");
                }

                return true;
            }
        }

        private class InsertOverlayStep : ITinyBuildStep
        {
            public string Name => "Insert Overlay Step";

            public bool Enabled(TinyBuildContext context) => true;

            public bool Run(TinyBuildContext context)
            {
                var includePlayableAdBootstrap = context.Options.Configuration != TinyBuildConfiguration.Release;

                if (includePlayableAdBootstrap)
                {
                    Action<HTTPServer> handler = null;

                    handler = (httpServerInstance) => {
                        UpdateBootstrap(context.Options.GetBuildFile("index.html"));
                        httpServerInstance.OnBeforeReloadOrOpen -= handler;
                    };

                    HTTPServer.Instance.OnBeforeReloadOrOpen += handler;
                }

                return true;
            }

            private static void UpdateBootstrap(FileInfo fileName)
            {
                var ipAddress = HTTPServer.Instance.URL;
                var qrLink = ipAddress.AbsoluteUri + "index.html";
                var qrCode = QRCode.Generate(new UriBuilder(qrLink).Uri);

                byte[] data = qrCode.EncodeToPNG();

                var playableAdWrapper = new DirectoryInfo(TinyRuntimeInstaller.GetToolDirectory("playableadwrapper"));

                var bootstrapPath = Path.Combine(playableAdWrapper.FullName, "index.html");
                var indexHtmlPath = fileName.FullName;

                var bootstrap = File.ReadAllText(bootstrapPath);
                var indexHtml = File.ReadAllText(indexHtmlPath);

                bootstrap = bootstrap.Replace("{bootstrap_url}", qrLink);
                bootstrap = bootstrap.Replace("{bootstrap_qrcode_src}", "data:image/png;base64, " + Convert.ToBase64String(data));

                File.WriteAllText(indexHtmlPath, indexHtml.Replace("<div id=\"unity_ads_bootstrap_container\" />", bootstrap));
            }
        }
    }
}
