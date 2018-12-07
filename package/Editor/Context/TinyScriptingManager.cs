
using System;
using System.Linq;
using JetBrains.Annotations;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    [UsedImplicitly]
    [ContextManager(ContextUsage.All)]
    internal class TinyScriptingManager : ContextManager
    {
        public delegate void OnMetadataLoadedCallback(TinyScriptingManager manager);

        public event OnMetadataLoadedCallback OnMetadataLoaded;
        
        public ScriptMetadata Metadata { get; private set; }
        
        public TinyScriptingManager(TinyContext context) : base(context)
        {
            Metadata = new ScriptMetadata();
        }

        public void Refresh()
        {
            Assert.IsNotNull(TinyEditorApplication.EditorContext);
            TinyBuildUtilities.CompileScripts(TinyBuildPipeline.WorkspaceBuildOptions);
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
    }

    internal static class ContextScriptingExtensions
    {
        public static ScriptMetadata GetScriptMetadata(this TinyContext context)
        {
            return context.GetManager<TinyScriptingManager>().Metadata;
        }
    }
}
