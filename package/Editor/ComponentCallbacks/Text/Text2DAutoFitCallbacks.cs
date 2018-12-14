using JetBrains.Annotations;

using Unity.Tiny.Runtime.Text;

namespace Unity.Tiny
{
    [TinyComponentCallback(
        CoreGuids.Text.Text2DAutoFit)]
    [UsedImplicitly]
    internal class Text2DAutoFitCallbacks : ComponentCallback
    {
        protected override void OnAddComponent(TinyEntity entity, TinyObject component)
        {
            var autoFit = entity.GetComponent<TinyText2DAutoFit>();
            var text2DStyle = entity.GetComponent<TinyText2DStyle>();
            if (text2DStyle.IsValid)
            {
                autoFit.minSize = text2DStyle.size;
                autoFit.maxSize = text2DStyle.size;
            }
            else
            {
                autoFit.minSize = 2;
                autoFit.maxSize = 10;
            }
        }
    }
}