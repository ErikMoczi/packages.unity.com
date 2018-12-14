using System.IO;
using UnityEditor.MemoryProfiler;

namespace Unity.MemoryProfiler.Editor.Legacy
{
    internal class LegacyReader
    {
        const string k_Memsnap = ".memsnap";
        const string k_Memsnap2 = ".memsnap2";
        const string k_Memsnap3 = ".memsnap3";

        public bool IsLegacyFileFormat(string path)
        {
            string extension = Path.GetExtension(path);
            switch (extension)
            {
                case k_Memsnap:
                case k_Memsnap2:
                case k_Memsnap3:
                    return true;
                default:
                    return false;
            }
        }

        public PackedMemorySnapshot ReadFromFile(string path)
        {
            PackedMemorySnapshot snapshot = null;
            string json = null;

            string extension = Path.GetExtension(path);
            switch (extension)
            {
                case k_Memsnap:
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    using (Stream stream = File.Open(path, FileMode.Open))
                    {
                        snapshot = binaryFormatter.Deserialize(stream) as PackedMemorySnapshot;
                    }
                    break;
                case k_Memsnap2:
                    json = File.ReadAllText(path);
                    snapshot = UnityEngine.JsonUtility.FromJson<PackedMemorySnapshot>(json);
                    break;
                case k_Memsnap3:
                    json = File.ReadAllText(path);
                    JsonNetConverter converter = new JsonNetConverter();
                    json = converter.Convert(json);
                    snapshot = UnityEngine.JsonUtility.FromJson<PackedMemorySnapshot>(json);
                    break;
                default:
                    throw new System.Exception("Not a supported file format, provided extension was: "+ extension);
            }

            return snapshot;
        }
    }
}