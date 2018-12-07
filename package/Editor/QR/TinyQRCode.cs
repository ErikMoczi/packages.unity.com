using System;
using System.Collections.Generic;
using System.IO;
using Unity.Properties;
using Unity.Properties.Serialization;
using UnityEngine;

namespace Unity.Tiny
{
    internal class TinyQRCode
    {
        private string m_Data;

        public TinyQRCode(string data)
        {
            m_Data = data;
        }

        public Texture2D GetGraphic(int pixelsPerModule)
        {
            var tempFile = new FileInfo(Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".png"));
            var inputFile = new FileInfo(Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".png"));

            try
            {
                var obj = new ObjectContainer(new Dictionary<string, object>{
                    ["data"] = m_Data,
                    ["pixelsPerModule"] = pixelsPerModule,
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
