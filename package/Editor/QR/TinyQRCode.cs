using System;
using System.Collections.Generic;
using System.IO;
using Unity.Properties;
using Unity.Properties.Serialization;
using UnityEngine;

namespace Unity.Tiny
{
    internal class QRCode
    {
        public static Texture2D Generate(Uri url)
        {
            if (url == null)
            {
                return null;
            }

            var tempFile = new FileInfo(Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".png"));
            var inputFile = new FileInfo(Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".png"));
            try
            {
                var obj = new ObjectContainer(new Dictionary<string, object>
                {
                    ["data"] = url.AbsoluteUri,
                    ["pixelsPerModule"] = 20,
                });

                File.WriteAllText(inputFile.FullName, JsonSerializer.Serialize(obj));

                if (TinyShell.RunTool("qrcode",
                    $"-i {inputFile.FullName.DoubleQuoted()}",
                    $"-o {tempFile.FullName.DoubleQuoted()}"))
                {
                    if (tempFile.Exists)
                    {
                        try
                        {
                            byte[] fileData = File.ReadAllBytes(tempFile.FullName);
                            Texture2D texture = new Texture2D(2, 2);
                            if (texture.LoadImage(fileData))
                            {
                                texture.filterMode = FilterMode.Point;
                                return texture;
                            }
                        }
                        finally
                        {
                            tempFile.Delete();
                        }
                    }
                }
            }
            finally
            {
                inputFile.Delete();
            }
            return null;
        }
    }
}
