

namespace Unity.Tiny
{
    [System.Flags]
    internal enum TinyExportFlags
    {
        /// <summary>
        /// No special case
        /// </summary>
        None = 0,

        /// <summary>
        /// (no-op)
        /// This object is already included by the runtime
        /// During export is will be ignored
        /// </summary>
        RuntimeIncluded = 1 << 0,
        
        /// <summary>
        /// This object has been reflected from scripts.
        /// It should be considered read-only, but requires bindings.
        /// </summary>
        DefinedInScript = 1 << 1,
        
        /// <summary>
        /// This object is editor-only. It is used to convert editor-data
        /// into data that can be consumed by the runtime.
        /// It will not be exported as-is and will not have bindings in TypeScript.
        /// </summary>
        EditorExtension = 1 << 2,

        /// <summary>
        /// This object is only for development.
        /// It will not be exported in Release configuration.
        /// </summary>
        Development = 1 << 4
    }
}

