using System;
using System.Collections.Generic;
using UnityEditor.Build.Interfaces;
using UnityEditor.Build.Tasks;

namespace UnityEditor.Build
{
    public static class DefaultBuildTasks
    {
        public enum Presets
        {
            PlayerScriptsOnly,
            AssetBundleCompatible,
            AutopackReleaseContent,
            //AutopackFastDeployContent,
        }

        public static IList<IBuildTask> Create(Presets preset, bool compileScripts = true)
        {
            switch (preset)
            {
                case Presets.PlayerScriptsOnly:
                    return PlayerScriptsOnly();
                case Presets.AssetBundleCompatible:
                    return AssetBundleCompatible(compileScripts);
                case Presets.AutopackReleaseContent:
                    return AutopackReleaseContent(compileScripts);
                default:
                    throw new NotImplementedException(string.Format("Presets for '{0}' not yet implemented.", preset));
            }
        }

        static IList<IBuildTask> PlayerScriptsOnly()
        {
            var buildTasks = new List<IBuildTask>();

            // Setup
            buildTasks.Add(new ProjectInCleanState());
            buildTasks.Add(new SwitchToBuildPlatform());

            // Player Scripts
            buildTasks.Add(new BuildPlayerScripts());
            buildTasks.Add(new SetBundleSettingsTypeDB());
            buildTasks.Add(new PostScriptsCallback());

            // Dependency
            // - Empty

            // Packing
            // - Empty

            // Writing
            // - Empty

            return buildTasks;
        }

        static IList<IBuildTask> AutopackReleaseContent(bool compileScripts)
        {
            var buildTasks = new List<IBuildTask>();

            // Setup
            buildTasks.Add(new ProjectInCleanState());
            buildTasks.Add(new SwitchToBuildPlatform());
            buildTasks.Add(new RebuildAtlasCache());

            // Player Scripts
            if (compileScripts)
            {
                buildTasks.Add(new BuildPlayerScripts());
                buildTasks.Add(new SetBundleSettingsTypeDB());
                buildTasks.Add(new PostScriptsCallback());
            }

            // Dependency
            buildTasks.Add(new CalculateSceneDependencyData());
            buildTasks.Add(new CalculateAssetDependencyData());
            buildTasks.Add(new StripUnusedSpriteSources());
            buildTasks.Add(new PostDependencyCallback());

            // Packing
            buildTasks.Add(new GenerateReleaseAutoPacking());
            buildTasks.Add(new GenerateCommands());
            buildTasks.Add(new PostPackingCallback());

            // Writing
            buildTasks.Add(new WriteSerializedFiles());
            buildTasks.Add(new CopySerializedFiles());
            buildTasks.Add(new PostWritingCallback());

            return buildTasks;
        }

        static IList<IBuildTask> AssetBundleCompatible(bool compileScripts)
        {
            var buildTasks = new List<IBuildTask>();

            // Setup
            buildTasks.Add(new ProjectInCleanState());
            buildTasks.Add(new ValidateBundleAssignments());
            buildTasks.Add(new SwitchToBuildPlatform());
            buildTasks.Add(new RebuildAtlasCache());

            // Player Scripts
            if (compileScripts)
            {
                buildTasks.Add(new BuildPlayerScripts());
                buildTasks.Add(new SetBundleSettingsTypeDB());
                buildTasks.Add(new PostScriptsCallback());
            }

            // Dependency
            buildTasks.Add(new CalculateSceneDependencyData());
            buildTasks.Add(new CalculateAssetDependencyData());
            buildTasks.Add(new StripUnusedSpriteSources());
            buildTasks.Add(new PostDependencyCallback());

            // Packing
            buildTasks.Add(new GenerateBundlePacking());
            buildTasks.Add(new GenerateBundleCommands());
            buildTasks.Add(new GenerateBundleMaps());
            buildTasks.Add(new PostPackingCallback());

            // Writing
            buildTasks.Add(new WriteSerializedFiles());
            buildTasks.Add(new ArchiveAndCompressBundles());
            buildTasks.Add(new PostWritingCallback());

            // Generate manifest files
            // TODO: IMPL manifest generation

            return buildTasks;
        }
    }
}