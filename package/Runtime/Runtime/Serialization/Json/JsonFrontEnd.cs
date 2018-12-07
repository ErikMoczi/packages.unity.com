

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Properties.Serialization;
using UnityEngine.Assertions;
using Unity.Tiny.Serialization.CommandStream;

namespace Unity.Tiny.Serialization.Json
{
    internal static class JsonFrontEnd
    {
        /// <summary>
        /// Reads the given json data in to a command stream
        /// </summary>
        /// <param name="path">Json file containing modules, types etc.</param>
        /// <param name="output">Command stream to write to</param>
        public static void Accept(string path, Stream output)
        {
            using (var input = new FileStream(path, FileMode.Open))
            {
                Accept(input, output);
            }
        }

        public static void Accept(Stream input, Stream output)
        {
            var jsonStreamReader = new JsonStreamReader(input);
            
            using (var binaryStream = new MemoryStream())
            using (var binaryWriter = new BinaryWriter(binaryStream))
            {
                var commandStreamWriter = new BinaryWriter(output);
                
                var c = jsonStreamReader.ReadChar(); // '['
                Assert.IsTrue(c == '[', $"Json.FrontEnd.Accept expected '[' but found '{StringUtility.Escape(c)}' as the first character of the stream. Line=[1]");

                var fileInfo = new JsonObjectFileInfo();
                
                // Read the next full json object from '{' to '}
                JsonObjectReader objectReader;
                while (jsonStreamReader.TryReadObject(out objectReader, ref fileInfo))
                {
                    objectReader.ReadBeginObject();

                    // Unpack the typeId
                    var property = objectReader.ReadPropertyNameSegment();
                    Assert.IsTrue(property.Array[property.Offset] == '$');

                    var typeId = (TinyTypeId) objectReader.ReadUInt16();

                    objectReader.Position = 0;

                    commandStreamWriter.Write(CommandType.GetCreateCommandType(typeId));
                    
                    // Translate the json object to binary
                    BinaryTranslator.TranslateObject(ref objectReader, binaryWriter);
                    
                    // Write the command payload as binary
                    commandStreamWriter.Write((uint) binaryStream.Position);
                    commandStreamWriter.Write(binaryStream.GetBuffer(), 0, (int) binaryStream.Position);
                    binaryStream.Position = 0;

                    c = jsonStreamReader.ReadChar(); // ',' or ']'
                    if (!(c == ',' || c == ']'))
                    {
                        throw new Exception($"Json.FrontEnd.Accept expected ',' or ']' but found '{StringUtility.Escape(c)}' as the next character in the stream. Line=[{objectReader.Line}]");
                    }
                }
            }
        }

        /// <summary>
        /// Translates the given json object to custom binary representation
        /// This class makes no attempt to `parse` the object it simply converts from JSON to a binary object notation
        /// </summary>
        internal static class BinaryTranslator
        {
            private static readonly Stack<long> s_PositionStack = new Stack<long>();
            
            public static void TranslateObject(ref JsonObjectReader reader, BinaryWriter writer)
            {
                writer.Write(BinaryToken.BeginObject);
                writer.Write((uint) 0);
                s_PositionStack.Push(writer.BaseStream.Position);
                
                reader.ReadBeginObject();
                while (reader.ReadInObject())
                {
                    var propertyName = reader.ReadPropertyName();
                    
                    writer.Write(BinaryToken.Property);
                    writer.Write(propertyName);

                    TranslateValue(ref reader, writer);
                }
                
                writer.Write(BinaryToken.EndObject);
                PrependSize(writer);
            }

            private static void TranslateValue(ref JsonObjectReader reader, BinaryWriter writer)
            {
                var token = reader.GetCurrentJsonToken();
                switch (token)
                {
                    case JsonObjectReader.JsonToken.BeginArray:
                    {
                        writer.Write(BinaryToken.BeginArray);
                        writer.Write((uint) 0);
                        s_PositionStack.Push(writer.BaseStream.Position);
                        reader.ReadBeginArray();
                        while (reader.ReadInArray())
                        {
                            TranslateValue(ref reader, writer);
                        }
                        writer.Write(BinaryToken.EndArray);
                        PrependSize(writer);
                    }
                    break;

                    case JsonObjectReader.JsonToken.BeginObject:
                    {
                        TranslateObject(ref reader, writer);
                    }
                    break;
                        
                    case JsonObjectReader.JsonToken.Number:
                    {
                        writer.Write(BinaryToken.Value);
                        writer.Write((byte) TypeCode.Double);
                        writer.Write(double.Parse(reader.ReadNumber(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture));
                    }
                    break;
                        
                    case JsonObjectReader.JsonToken.String:
                    {
                        writer.Write(BinaryToken.Value);
                        writer.Write((byte) TypeCode.String);
                        writer.Write(reader.ReadString());
                    }
                    break;

                    case JsonObjectReader.JsonToken.True:
                    case JsonObjectReader.JsonToken.False:
                    {
                        writer.Write(BinaryToken.Value);
                        writer.Write((byte) TypeCode.Boolean);
                        writer.Write(reader.ReadBoolean());
                    }
                    break;

                    case JsonObjectReader.JsonToken.Null:
                    case JsonObjectReader.JsonToken.None:
                    case JsonObjectReader.JsonToken.EndObject:
                    case JsonObjectReader.JsonToken.EndArray:
                    case JsonObjectReader.JsonToken.ValueSeparator:
                    case JsonObjectReader.JsonToken.NameSeparator:
                    {
                        reader.ReadUntilValueSeparator();
                    }
                    break;
                        
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            private static void PrependSize(BinaryWriter writer)
            {
                var start = s_PositionStack.Pop();
                var end = writer.BaseStream.Position;
                var size = end - start;
                
                writer.BaseStream.Position = start - sizeof(uint);
                writer.Write((uint) size);
                writer.BaseStream.Position = end;    
            }
        }
    }
}

