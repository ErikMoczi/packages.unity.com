using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities
{
    public static class HashingMethods
    {
        static Hash128 ToHash128(byte[] hash)
        {
            var result = new Hash128(BitConverter.ToUInt32(hash, 0), BitConverter.ToUInt32(hash, 4),
                BitConverter.ToUInt32(hash, 8), BitConverter.ToUInt32(hash, 12));
            return result;
        }
        static GUID ToGUID(byte[] hash)
        {
            var resultStr = BitConverter.ToString(hash).Replace("-", "").ToLower();
            var result = new GUID(resultStr);
            return result;
        }

        public static byte[] CalculateStreamMD5(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

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
            if (objects == null)
                throw new ArgumentNullException("objects");

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

        public static GUID CalculateMD5Guid(params object[] objects)
        {
            byte[] hash = CalculateMD5(objects);
            return ToGUID(hash);
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
