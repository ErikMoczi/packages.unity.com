

using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny.Test
{
    [TestFixture]
    internal class SamplesTest
    {
        private const string SamplesFolder = "Assets/TinySamples";

        [Test]
        public void LoadSamples()
        {
            foreach (var path in AssetDatabase.FindAssets($"t:{typeof(UTProject)}", new[] {SamplesFolder})
                .Select(AssetDatabase.GUIDToAssetPath))
            {
                var context = new TinyContext(ContextUsage.ImportExport);
                var project = Persistence.LoadProject(path, context.Registry);
                Assert.IsNotNull(project);
                context = null;
            }
        }
    }
    
}

