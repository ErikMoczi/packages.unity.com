using JetBrains.Annotations;

namespace Unity.Tiny
{
    [ComponentFamily(
         requiredGuids: new []
         {
             CoreGuids.Text.Text2DRenderer,
             CoreGuids.Text.Text2DStyle,
             CoreGuids.Text.Text2DStyleNativeFont,
             CoreGuids.Text.NativeFont,
             CoreGuids.Core2D.RectTransformFinalSize
         },
         optionalGuids: new[]
         {
             CoreGuids.Text.Text2DAutoFit
         }),
     ExtendedComponentFamily(typeof(TextFamily)),
     UsedImplicitly]
    internal class UITextFamily : ComponentFamily
    {
        public override string Name => "UI Text";
        public UITextFamily(FamilyDefinition definition, TinyContext tinyContext)
            : base(definition, tinyContext)
        {
        }
    }
}