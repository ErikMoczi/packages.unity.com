
using System;

namespace Unity.Tiny
{
    [Flags]
    internal enum ContextUsage
    {
        None         = 0,                      // Don't use, equivalent to deprecated
        Edit         = 1 << 0,                 // For user interaction through the editor
        ImportExport = 1 << 1,                 // Use when you only need to export
        Tests        = 1 << 2,                 // For testing purposes only
        LiveLink     = 1 << 3,                 // For live link specific context
        All = Edit|ImportExport|Tests|LiveLink // Use every time
    }
}
