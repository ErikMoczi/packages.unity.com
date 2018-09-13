using System;
using System.Collections;
using System.IO;

using UnityEngine;
using UnityEngine.Networking;

namespace UnityEngine.XR.Management.Sample
{
	public class SampleUtilities
	{

        public static string GetSerializationFilename(string fileName, string outputPath)
        {
            string filename = Path.Combine(outputPath, fileName);
            filename += SampleConstants.kSerialisedSettingsExt;
            return fileName;
        }

        public static void WriteSettings<T>(T settings, string filename)
        {
            if (File.Exists(filename))
                File.Delete(filename);

            // NOTE: We just use JSON here for expediency. You can use whatever serialization method you want.
            string json = JsonUtility.ToJson(settings, true);

            using (FileStream fs = File.OpenWrite(filename))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(json);
                }
            }
        }

        public static IEnumerator ReadSettings(string filename, System.Action<SampleSettings> callback)
        {
            SampleSettings settings = null;
            #if UNITY_EDITOR
                UnityEditor.EditorBuildSettings.TryGetConfigObject(SampleConstants.kSettingsKey, out settings);
            #else
                string json = "";

                if (filename.Contains("://"))
                {
                    UnityWebRequest www = UnityWebRequest.Get(filename);
                    yield return  www.SendWebRequest();
                    json = www.downloadHandler.text;
                }
                else
                {
                    if (File.Exists(filename))
                    {
                        json = System.IO.File.ReadAllText(filename);
                    }
                }

                if (!String.IsNullOrEmpty(json))
                {
                    settings = new SampleSettings();
                    JsonUtility.FromJsonOverwrite(json, settings);
                }
            #endif
            callback(settings);
            yield return null;
        }
	}
}
