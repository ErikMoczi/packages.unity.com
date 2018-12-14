using System;
using System.Collections.Generic;
using System.IO;
using Unity.Tiny.Serialization.Json;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal abstract class TinyScriptedImporter<T> : ScriptedImporter
        where T : TinyScriptableObject
    {
        protected T CreateAsset(AssetImportContext ctx) 
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.hideFlags |= HideFlags.NotEditable;

            using (var stream = new FileStream(ctx.assetPath, FileMode.Open, FileAccess.Read))
            {
                asset.Objects = GetRegistryObjectIds(stream);
                stream.Position = 0;
                asset.Hash = TinyCryptography.ComputeHash(stream);
            }
            
            var icon = GetThumbnailForAsset(ctx, asset);
            asset.Icon = icon;

            ctx.AddObjectToAsset("asset", asset, icon);
            ctx.SetMainObject(asset);
            
            return asset;
        }

        protected virtual Texture2D GetThumbnailForAsset(AssetImportContext ctx, T asset)
        {
            return null;
        }
        
        /// <summary>
        /// Reads a file and extracts all registry object ids
        /// 
        /// @TODO Can we find an elegeant and generic solution for this problem?
        ///       We are trying to efficiently extract the `Id` property from all top-level objects
        /// </summary>
        /// <returns></returns>
        private static string[] GetRegistryObjectIds(Stream stream)
        {
            var jsonStreamReader = new JsonStreamReader(stream);
            
            jsonStreamReader.SkipWhiteSpace();
            var c = jsonStreamReader.ReadChar(); // '['
            
            Assert.IsTrue(c == '[', $"UTScriptableObject.GetRegistryObjectIds expected '[' but found '{StringUtility.Escape(c)}' as the first character of the stream. Line=[1]");
            
            var fileInfo = new JsonObjectFileInfo();

            var result = new List<string>();
            
            JsonObjectReader objectReader;
            while (jsonStreamReader.TryReadObject(out objectReader, ref fileInfo))
            {
                var id = ReadObjectId(ref objectReader);

                if (string.IsNullOrEmpty(id))
                {
                    throw new Exception($"UTScriptableObject.GetRegistryObjectIds top level object is missing the `Id` property. Line=[{objectReader.Line}]");
                }
                
                result.Add(id);
                
                jsonStreamReader.SkipWhiteSpace();
                c = jsonStreamReader.ReadChar(); // ',' or ']'
                if (!(c == ',' || c == ']'))
                {
                    throw new Exception($"UTScriptableObject.GetRegistryObjectIds expected ',' or ']' but found '{StringUtility.Escape(c)}' as the next character in the stream. Line=[{objectReader.Line}]");
                }
            }

            return result.ToArray();
        }
        
        private static string ReadObjectId(ref JsonObjectReader reader, int depth = 0)
        {
            var id = string.Empty;
            
            reader.ReadBeginObject();
            while (reader.ReadInObject())
            {
                var segment = reader.ReadPropertyNameSegment();

                if (depth == 0 && SegmentEquals(segment, TinyRegistryObjectBase.IdProperty.Name))
                {
                    id = reader.ReadString();
                }
                else
                {
                    ReadPropertyValue(ref reader, depth);
                }
            }

            return id;
        }

        private static bool SegmentEquals(ArraySegment<char> segment, string str)
        {
            if (segment.Count != str.Length)
            {
                return false;
            }

            for (var i = 0; i < segment.Count; i++)
            {
                if (segment.Array[segment.Offset + i] != str[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void ReadPropertyValue(ref JsonObjectReader reader, int depth)
        {
            var token = reader.GetCurrentJsonToken();
            switch (token)
            {
                case JsonObjectReader.JsonToken.BeginArray:
                {
                    reader.ReadBeginArray();
                    while (reader.ReadInArray())
                    {
                        ReadPropertyValue(ref reader, depth);
                    }
                }
                    break;

                case JsonObjectReader.JsonToken.BeginObject:
                {
                    ReadObjectId(ref reader, depth + 1);
                }
                    break;
                    
                case JsonObjectReader.JsonToken.Number:
                    reader.ReadNumberSegment();
                    break;
                case JsonObjectReader.JsonToken.String:
                    reader.ReadStringSegment();
                    break;
                case JsonObjectReader.JsonToken.True:
                case JsonObjectReader.JsonToken.False:
                    reader.ReadBoolean();
                    break;
                case JsonObjectReader.JsonToken.Null:
                case JsonObjectReader.JsonToken.None:
                case JsonObjectReader.JsonToken.EndObject:
                case JsonObjectReader.JsonToken.EndArray:
                case JsonObjectReader.JsonToken.ValueSeparator:
                case JsonObjectReader.JsonToken.NameSeparator:
                {
                    reader.ReadValueSeparator();
                }
                    break;
                    
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

