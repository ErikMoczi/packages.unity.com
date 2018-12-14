using JetBrains.Annotations;

namespace Unity.Tiny
{
    [TinyComponentCallback(
        CoreGuids.UIControls.Button)]
    [UsedImplicitly]
    internal class ButtonCallbacks : ComponentCallback
    {
        protected override void OnAddComponent(TinyEntity entity, TinyObject component)
        {
            component.AssignIfDifferent("sprite2DRenderer", entity.Ref);
        }
    }
}