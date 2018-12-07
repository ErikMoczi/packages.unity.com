using JetBrains.Annotations;

namespace Unity.Tiny
{
    [ComponentFamily(
         requiredGuids: new []
         {
             CoreGuids.Text.Text2DRenderer,
             CoreGuids.Text.Text2DStyle,
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
    
    [ComponentFamily(
         requiredGuids: new []
         {
             CoreGuids.Text.Text2DStyleNativeFont,
             CoreGuids.Text.NativeFont,
         }),
     UsedImplicitly]
    internal class NativeFontFamily : ComponentFamily
    {
        public override string Name => "Native Font";
        public NativeFontFamily(FamilyDefinition definition, TinyContext tinyContext)
            : base(definition, tinyContext)
        {
        }
    }
    
    [ComponentFamily(
         requiredGuids: new []
         {
             CoreGuids.Text.Text2DStyleBitmapFont,
         }),
     UsedImplicitly]
    internal class BitmapFontFamily : ComponentFamily
    {
        public override string Name => "Bitmap Font";
        public BitmapFontFamily(FamilyDefinition definition, TinyContext tinyContext)
            : base(definition, tinyContext)
        {
        }
    }
}