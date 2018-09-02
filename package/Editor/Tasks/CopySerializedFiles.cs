using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.WriteTypes;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.Tasks
{
    public struct CopySerializedFiles : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IBuildParameters), typeof(IBuildResults) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public ReturnCodes Run(IBuildContext context)
        {
            return Run(context.GetContextObject<IBuildParameters>(), context.GetContextObject<IBuildResults>());
        }

        public static ReturnCodes Run(IBuildParameters parameters, IBuildResults results)
        {
            foreach (var writeResult in results.WriteResults.Values)
            {
                foreach (var resourceFile in writeResult.resourceFiles)
                {
                    var filePath = string.Format("{0}/{1}", parameters.OutputFolder, resourceFile.fileAlias);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    File.Copy(resourceFile.fileName, filePath);
                }
            }

            return ReturnCodes.Success;
        }
    }
}
