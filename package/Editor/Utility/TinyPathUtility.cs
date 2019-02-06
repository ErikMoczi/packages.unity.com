
using System.IO;
using UnityEngine;

namespace Unity.Tiny
{
    internal class TinyPathUtility
    {
        public static string AssetsFullPath { get; } = ToFowardSlashes(Application.dataPath);
        public static string ProjectFullPath { get; } = ToFowardSlashes(new DirectoryInfo(AssetsFullPath).Parent.FullName);

        /// <summary>
        /// Takes a path string and ensures that '/' is used instead of '\'.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        public static string ToFowardSlashes(string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>
        /// Given a path under the project's folder, will return the relative path.
        /// This is useful when we need a path that starts with "Assets/" and we were given a full path.
        /// </summary>
        /// <param name="path">The path we want converted as a relative path.</param>
        /// <returns>The relative path (if computed) or the original path. </returns>
        public static string RelativeToProjectFolder(string path)
        {
            var relative = ToFowardSlashes(path);
            if (relative.StartsWith(ProjectFullPath))
            {
                if (relative.Length != ProjectFullPath.Length)
                {
                    return relative.Remove(0, ProjectFullPath.Length + 1);
                }

                return "./";
            }
            return path;
        }
    }
}
