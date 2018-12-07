using JetBrains.Annotations;

namespace Unity.Tiny
{
    [ComponentFamily(
         requiredGuids: new []
         {
             CoreGuids.Text.Text2DRenderer,
             CoreGuids.Text.Text2DStyle,
             CoreGuids.Text.Text2DStyleNativeFont,
             CoreGuids.Text.NativeFont
         }),
     UsedImplicitly]
    internal class TextFamily : ComponentFamily
    {
        public override string Name => "Text";
        public TextFamily(FamilyDefinition definition, TinyContext tinyContext)
            : base(definition, tinyContext)
        {
        }
    }
}