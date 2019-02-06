

using JetBrains.Annotations;

namespace Unity.Tiny
{
    [UsedImplicitly]
    internal class EntityViewInvertedBindings : InvertedBindingsBase<TinyEntityView>
    {
        #region InvertedBindingsBase<TinyEntityView>
        public override void Create(TinyEntityView view, TinyEntityView @from)
        {
            // Nothing to do..
        }

        public override TinyType.Reference GetMainTinyType()
        {
            return TinyType.Reference.None;
        }
        #endregion
    }
}

