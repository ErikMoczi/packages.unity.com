using JetBrains.Annotations;
using System.IO;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal delegate void OnMetadataLoadedCallback(IScriptingManager manager);

    internal interface IScriptingManager : IContextManager
    {
        event OnMetadataLoadedCallback OnMetadataLoaded;

        ScriptMetadata Metadata { get; }

        bool CompileScripts(TinyBuildOptions buildOptions);
        bool Apply(ScriptMetadata metadata, TinyContext context, TinyModule mainModule);
        void Refresh();
    }

    [UsedImplicitly]
    [ContextManager(ContextUsage.LiveLink)]
    internal class NullScriptingManager : ContextManager, IScriptingManager
    {
#pragma warning disable CS0067 // The event is never used
        public event OnMetadataLoadedCallback OnMetadataLoaded;
#pragma warning restore CS0067

        public ScriptMetadata Metadata { get; private set; }

        public NullScriptingManager(TinyContext context) : base(context) { }
        public bool CompileScripts(TinyBuildOptions buildOptions) { return true; }
        public bool Apply(ScriptMetadata metadata, TinyContext context, TinyModule mainModule) { return true; }
        public void Refresh() { }
    }

    [UsedImplicitly]
    [ContextManager(~ContextUsage.LiveLink)]
    internal class TinyScriptingManager : ContextManager, IScriptingManager
    {
        public event OnMetadataLoadedCallback OnMetadataLoaded;
        
        public ScriptMetadata Metadata { get; private set; }
        
        public TinyScriptingManager(TinyContext context) : base(context)
        {
            Metadata = new ScriptMetadata();
        }

        public bool CompileScripts(TinyBuildOptions options)
        {
            TinyBuildUtilities.RegenerateTSDefinitionFiles(options);

            var tsconfig = TinyBuildUtilities.RegenerateTsConfig(options);
            var tsmeta = TinyScriptUtility.GetTypeScriptOutputMetaFile(options);
            var metadata = TinyBuildUtilities.CompileTypeScript(tsconfig, tsmeta);
            if (metadata == null)
            {
                return false;
            }

            var context = options.Context;
            var manager = context.GetManager<IScriptingManager>();
            var mainModule = options.Project.Module.Dereference(options.Registry);
            return manager.Apply(metadata, context, mainModule);
        }

        public bool Apply(ScriptMetadata metadata, TinyContext context, TinyModule mainModule)
        {
            // at this point, compilation succeeded, and we were able to read the extracted metadata
            var destination = Metadata = new ScriptMetadata();
            try
            {
                PropertyContainer.Transfer(metadata, destination);

                if (destination.Resolve(context, mainModule))
                {
                    return true;
                }

                foreach (var error in destination.ResolutionErrors)
                {
                    Debug.LogException(error);
                }
                return false;
            }
            finally
            {
                OnMetadataLoaded?.Invoke(this);
            }
        }

        public void Refresh()
        {
            Assert.IsNotNull(TinyEditorApplication.EditorContext);
            TinyBuildUtilities.CompileScripts();
        }
    }

    internal static class ContextScriptingExtensions
    {
        public static ScriptMetadata GetScriptMetadata(this TinyContext context)
        {
            return context.GetManager<IScriptingManager>().Metadata;
        }
    }
}
