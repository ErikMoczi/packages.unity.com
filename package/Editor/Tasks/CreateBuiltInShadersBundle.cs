using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Optional build task that extracts Unity's built in shaders and assigns them to the specified bundle 
    /// </summary>
    public class CreateBuiltInShadersBundle : IBuildTask
    {
        static readonly GUID k_BuiltInGuid = new GUID("0000000000000000f000000000000000");

        const int k_Version = 1;
        public int Version { get { return k_Version; } }

        static readonly Type[] k_RequiredTypes = { typeof(IDependencyData) };
        public Type[] RequiredContextTypes { get { return k_RequiredTypes; } }

        public string ShaderBundleName { get; set; }

        public CreateBuiltInShadersBundle(string bundleName)
        {
            ShaderBundleName = bundleName;
        }

        public ReturnCode Run(IBuildContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            IBundleExplictObjectLayout layout;
            if (!context.TryGetContextObject(out layout))
            {
                layout = new BundleExplictObjectLayout();
                context.SetContextObject(layout);
            }

            return Run(context.GetContextObject<IDependencyData>(), layout, ShaderBundleName);
        }

        static ReturnCode Run(IDependencyData dependencyData, IBundleExplictObjectLayout layout, string bundleName)
        {
            HashSet<ObjectIdentifier> buildInObjects = new HashSet<ObjectIdentifier>();
            foreach (var dependencyInfo in dependencyData.AssetInfo.Values)
                buildInObjects.UnionWith(dependencyInfo.referencedObjects.Where(x => x.guid == k_BuiltInGuid));

            foreach (var dependencyInfo in dependencyData.SceneInfo.Values)
                buildInObjects.UnionWith(dependencyInfo.referencedObjects.Where(x => x.guid == k_BuiltInGuid));

            ObjectIdentifier[] usedSet = buildInObjects.ToArray();
            Type[] usedTypes = ContentBuildInterface.GetTypeForObjects(usedSet);

            Type shader = typeof(Shader);
            for (var i = 0; i < usedTypes.Length; i++)
            {
                if (usedTypes[i] != shader)
                    continue;

                layout.ExplicitObjectLocation.Add(usedSet[i], bundleName);
            }

            return ReturnCode.Success;
        }
    }
}
