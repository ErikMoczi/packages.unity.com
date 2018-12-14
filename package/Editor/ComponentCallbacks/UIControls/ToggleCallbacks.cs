using JetBrains.Annotations;

namespace Unity.Tiny
{
    [TinyComponentCallback(
        CoreGuids.UIControls.Toggle)]
    [UsedImplicitly]
    internal class ToggleCallbacks : ComponentCallback
    {
        protected override void OnAddComponent(TinyEntity entity, TinyObject component)
        {
            component.AssignIfDifferent("sprite2DRenderer", entity.Ref);
        }
    }
}