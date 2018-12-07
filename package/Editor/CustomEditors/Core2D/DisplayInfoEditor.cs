using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.Core2D.DisplayInfo)]
    [UsedImplicitly]
    internal class DisplayInfoEditor : ComponentEditor
    {
        public DisplayInfoEditor(TinyContext context)
            : base(context) { }

        public override bool Visit(ref UIVisitContext<TinyObject> context)
        {
            // Until we move the settings. Display info is read-only
            var enabled = GUI.enabled;
            GUI.enabled = false;
            var result = base.Visit(ref context);
            GUI.enabled = enabled;
            return result;
        }

    }
}

