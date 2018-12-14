


namespace Unity.Tiny
{
    internal static class EntityTemplates
    {
        public static readonly EntityTemplate Empty = new EntityTemplate();

        public static readonly EntityTemplate Transform = new EntityTemplate(
            CoreIds.Core2D.TransformNode,
            CoreIds.Core2D.TransformLocalPosition,
            CoreIds.Core2D.TransformLocalRotation,
            CoreIds.Core2D.TransformLocalScale);

        public static readonly EntityTemplate RectTransform = new EntityTemplate(
            CoreIds.Core2D.TransformNode,
            CoreIds.Core2D.TransformLocalPosition,
            CoreIds.Core2D.TransformLocalRotation,
            CoreIds.Core2D.TransformLocalScale,
            CoreIds.UILayout.RectTransform);

        public static readonly EntityTemplate Camera = new EntityTemplate(
            CoreIds.Core2D.TransformNode,
            CoreIds.Core2D.TransformLocalPosition,
            CoreIds.Core2D.TransformLocalRotation,
            CoreIds.Core2D.TransformLocalScale,
            CoreIds.Core2D.Camera2D);

        public static readonly EntityTemplate Sprite = new EntityTemplate(
            CoreIds.Core2D.TransformNode,
            CoreIds.Core2D.TransformLocalPosition,
            CoreIds.Core2D.TransformLocalRotation,
            CoreIds.Core2D.TransformLocalScale,
            CoreIds.Core2D.Sprite2DRenderer);

        public static readonly EntityTemplate AudioSource = new EntityTemplate(
            CoreIds.Audio.AudioSource);

        public static readonly EntityTemplate Canvas = new EntityTemplate(
            CoreIds.Core2D.TransformNode,
            CoreIds.Core2D.TransformLocalPosition,
            CoreIds.Core2D.TransformLocalRotation,
            CoreIds.Core2D.TransformLocalScale,
            CoreIds.UILayout.RectTransform,
            CoreIds.UILayout.UICanvas);

        public static readonly EntityTemplate Image = new EntityTemplate(
            CoreIds.Core2D.TransformNode,
            CoreIds.Core2D.TransformLocalPosition,
            CoreIds.Core2D.TransformLocalRotation,
            CoreIds.Core2D.TransformLocalScale,
            CoreIds.UILayout.RectTransform,
            CoreIds.Core2D.Sprite2DRenderer,
            CoreIds.Core2D.Sprite2DRendererOptions);
    }
}


