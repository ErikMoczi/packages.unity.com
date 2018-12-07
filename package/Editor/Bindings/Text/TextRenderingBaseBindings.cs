using System;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using Unity.Tiny.Runtime.Text;
using UnityEngine.UI;

namespace Unity.Tiny
{
    internal abstract class TextRenderingBaseBindings<TText> : BindingProfile
        where TText : TMP_Text
    {
        protected virtual float SizeFactor => 1.0f;
        
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<MeshRenderer>(entity, renderer =>
            {
                renderer.sharedMaterial = new Material(Shader.Find("GUI/Text Shader"));
            });
            AddMissingComponent<TText>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<TText>(entity);
            RemoveComponent<MeshRenderer>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            var text2DRenderer = entity.GetComponent<TinyText2DRenderer>();
            var text2DStyle = entity.GetComponent<TinyText2DStyle>();

            var text = GetComponent<TText>(entity);
            text.text = text2DRenderer.text;
            text.fontStyle = FontStyles.Normal;
            text.lineSpacing = 1;
            text.richText = false;
            text.alignment = TinyGUIUtility.GetTextAlignmentFromPivot(text2DRenderer.pivot);
            text.color = text2DStyle.color;
            text.fontSize = text2DStyle.size * SizeFactor;
            text.isOrthographic = true;
            text.enableWordWrapping = false;
            
            Transfer(entity, text);
        }

        protected virtual void Transfer(TinyEntity entity, TText text)
        {
        }
    }

   
    internal class BitmapFontRenderingBaseBindings<TText> : TextRenderingBaseBindings<TText>
        where TText : TMP_Text
    {
        protected override void Transfer(TinyEntity entity, TText text)
        {
            var text2DStyleBitmapFont = entity.GetComponent<TinyText2DStyleBitmapFont>();
            text.font = text2DStyleBitmapFont.font;
        }
    }
    
    internal class NativeFontRenderingBaseBindings<TText> : TextRenderingBaseBindings<TText>
        where TText : TMP_Text
    {
        protected override float SizeFactor => 4.0f/3.0f;
        
        protected override void Transfer(TinyEntity entity, TText text)
        {
            var textMesh = GetComponent<TText>(entity);
            var text2DStyleNativeFont = entity.GetComponent<TinyText2DStyleNativeFont>();
            var nativeFont = entity.GetComponent<TinyNativeFont>();

            var bold = text2DStyleNativeFont.weight == 700;
            var italic = text2DStyleNativeFont.italic;

            if (bold && italic)
            {
                textMesh.fontStyle = FontStyles.Bold | FontStyles.Italic;
            }
            else if (bold)
            {
                textMesh.fontStyle = FontStyles.Bold;
            }
            else if (italic)
            {
                textMesh.fontStyle = FontStyles.Italic;
            }
            else
            {
                textMesh.fontStyle = FontStyles.Normal;
            }
            switch (nativeFont.name)
            {
                case TinyFontName.SansSerif:
                    textMesh.font = TinyFonts.GetSansSerifFont(bold, italic);
                    break;
                case TinyFontName.Serif:
                    textMesh.font = TinyFonts.GetSerifFont(bold, italic);
                    break;
                case TinyFontName.Monospace:
                    textMesh.font = TinyFonts.GetMonoSpaceFont(bold, italic);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [WithComponent(
        CoreGuids.Text.Text2DRenderer,
        CoreGuids.Text.Text2DStyle,
        CoreGuids.Text.Text2DStyleBitmapFont,
        CoreGuids.Core2D.TransformNode)]
    [WithoutComponent(
        CoreGuids.UILayout.RectTransform
    )]
    [BindingDependency(
        typeof(TransformBinding)
    )]
    [UsedImplicitly]
    internal sealed class BitmapTextBaseBindings : BitmapFontRenderingBaseBindings<TextMeshPro>
    {
        public override void Transfer(TinyEntity entity)
        {
            base.Transfer(entity);
            var rt = GetComponent<RectTransform>(entity);
            rt.sizeDelta = Vector2.zero;
        }
    }

    [WithComponent(
        CoreGuids.Text.Text2DRenderer,
        CoreGuids.Text.Text2DStyle,
        CoreGuids.Text.Text2DStyleBitmapFont,
        CoreGuids.UILayout.RectTransform,
        CoreGuids.Core2D.RectTransformFinalSize
    )]
    [OptionalComponent(
        CoreGuids.Text.Text2DAutoFit
    )]
    [BindingDependency(
        typeof(RectTransformBindings)
    )]
    [UsedImplicitly]
    internal sealed class UiBitmapTextBaseBindings : BitmapFontRenderingBaseBindings<TextMeshProUGUI>
    {
        protected override void Transfer(TinyEntity entity, TextMeshProUGUI text)
        {
            base.Transfer(entity, text);
            try
            {
                var autoFit = entity.GetComponent<TinyText2DAutoFit>();
                if (autoFit.IsValid)
                {
                    text.enableAutoSizing = true;
                    text.fontSizeMin = autoFit.minSize * SizeFactor;
                    text.fontSizeMax = autoFit.maxSize * SizeFactor;
                    text.enableWordWrapping = false;
                }
                else
                {
                    text.enableAutoSizing = false;
                    text.enableWordWrapping = false;
                }
            }
            finally
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>(entity).root as RectTransform);
            }
        }
    }
    
    [WithComponent(
        CoreGuids.Text.Text2DRenderer,
        CoreGuids.Text.Text2DStyle,
        CoreGuids.Text.Text2DStyleNativeFont,
        CoreGuids.Text.NativeFont,
        CoreGuids.Core2D.TransformNode)]
    [WithoutComponent(
        CoreGuids.UILayout.RectTransform
    )]
    [BindingDependency(
        typeof(TransformBinding)
    )]
    [UsedImplicitly]
    internal sealed class NativeTextBaseBindings : NativeFontRenderingBaseBindings<TextMeshPro>
    {
        public override void Transfer(TinyEntity entity)
        {
            base.Transfer(entity);
            var rt = GetComponent<RectTransform>(entity);
            rt.sizeDelta = Vector2.zero;
        }
    }

    [WithComponent(
        CoreGuids.Text.Text2DRenderer,
        CoreGuids.Text.Text2DStyle,
        CoreGuids.Text.Text2DStyleNativeFont,
        CoreGuids.Text.NativeFont,
        CoreGuids.UILayout.RectTransform,
        CoreGuids.Core2D.RectTransformFinalSize)]
    [OptionalComponent(
        CoreGuids.Text.Text2DAutoFit)]
    [BindingDependency(
        typeof(RectTransformBindings))]
    [UsedImplicitly]
    internal sealed class UiNativeTextBaseBindings : NativeFontRenderingBaseBindings<TextMeshProUGUI>
    {
        protected override void Transfer(TinyEntity entity, TextMeshProUGUI text)
        {
            base.Transfer(entity, text);
            try
            {
                var autoFit = entity.GetComponent<TinyText2DAutoFit>();
                if (autoFit.IsValid)
                {
                    text.enableAutoSizing = true;
                    text.fontSizeMin = autoFit.minSize * SizeFactor;
                    text.fontSizeMax = autoFit.maxSize * SizeFactor;
                    text.enableWordWrapping = false;
                }
                else
                {
                    text.enableAutoSizing = false;
                    text.enableWordWrapping = false;
                }
            }
            finally
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>(entity).root as RectTransform);
            }
        }
    }
}