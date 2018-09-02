using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using UnityEngine;

namespace UnityEditor.Build.Utilities
{
    public static class HashingMethods
    {
        static Hash128 ToHash128(byte[] hash)
        {
            return new Hash128(BitConverter.ToUInt32(hash, 0), BitConverter.ToUInt32(hash, 4),
                BitConverter.ToUInt32(hash, 8), BitConverter.ToUInt32(hash, 12));
        }

        public static byte[] CalculateStreamMD5(Stream stream)
        {
            byte[] hash;
            stream.Position = 0;
            using (var md5 = MD5.Create())
                hash = md5.ComputeHash(stream);
            return hash;
        }

        public static Hash128 CalculateStreamMD5Hash(Stream stream)
        {
            byte[] hash = CalculateStreamMD5(stream);
            return ToHash128(hash);
        }

        public static byte[] CalculateMD5(object obj)
        {
            byte[] hash;
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, obj);
                hash = CalculateStreamMD5(stream);
            }
            return hash;
        }

        public static Hash128 CalculateMD5Hash(object obj)
        {

            byte[] hash = CalculateMD5(obj);
            return ToHash128(hash);
        }

        public static byte[] CalculateMD5(params object[] objects)
        {
            byte[] hash;
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                foreach (var obj in objects)
                {
                    if (obj != null)
                        formatter.Serialize(stream, obj);
                }
                hash = CalculateStreamMD5(stream);
            }
            return hash;
        }

        public static Hash128 CalculateMD5Hash(params object[] objects)
        {
            byte[] hash = CalculateMD5(objects);
            return ToHash128(hash);
        }

        public static byte[] CalculateFileMD5(string filePath)
        {
            byte[] hash;
            using (var stream = new FileStream(filePath, FileMode.Open))
                hash = CalculateStreamMD5(stream);
            return hash;
        }

        public static Hash128 CalculateFileMD5Hash(string filePath)
        {
            byte[] hash = CalculateFileMD5(filePath);
            return ToHash128(hash);
        }
    }
}
