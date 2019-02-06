using UnityEngine;

namespace Unity.Tiny
{
    [TinyCustomEditor(CoreGuids.PlayableAd.PlayableAdInfo)]
    internal class PlayableAdInfoEditor : ComponentEditor
    {
        public PlayableAdInfoEditor(TinyContext context)
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
