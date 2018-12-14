

using System;
using System.IO;

namespace Unity.Tiny
{
    internal static class TinyCryptography
    {
        internal static string ComputeHash(string path)
        {
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return ComputeHash(stream);
            }
        }
        
        internal static string ComputeHash(Stream stream)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                return new Guid(hash).ToString("N");
            }
        }
    }
}

