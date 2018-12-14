
using Unity.Properties;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Unity.Tiny
{
    internal class IMGUIUnityTypesAdapter:
        IVisitValueAdapter<Texture2D>
        ,IVisitValueAdapter<Sprite>
        ,IVisitValueAdapter<Tile>
        ,IVisitValueAdapter<Tilemap>
        ,IVisitValueAdapter<AudioClip>
        ,IVisitValueAdapter<AnimationClip>
        ,IVisitValueAdapter<TMPro.TMP_FontAsset>
    {
        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<Texture2D> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<Texture2D> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<Sprite> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<Sprite> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<Tile> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<Tile> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<Tilemap> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<Tilemap> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<AudioClip> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<AudioClip> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);
        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<AnimationClip> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<AnimationClip> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomClassVisit<TContainer>(ref TContainer container, ref UIVisitContext<TMPro.TMP_FontAsset> context)
            where TContainer : class, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);

        public bool CustomStructVisit<TContainer>(ref TContainer container, ref UIVisitContext<TMPro.TMP_FontAsset> context)
            where TContainer : struct, IPropertyContainer
            => IMGUIVisitorHelper.AsLeafItem(ref container, ref context, IMGUIVisitorHelper.PropertyField);
    }
}
