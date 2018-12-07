

using System;
using JetBrains.Annotations;
using Unity.Tiny.Runtime.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Tiny
{
    [WithComponent(
        CoreGuids.Text.Text2DRenderer,
        CoreGuids.Text.Text2DStyle,
        CoreGuids.Text.Text2DStyleNativeFont,
        CoreGuids.Text.NativeFont,
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
    internal class UITextRendererBindings : BindingProfile
    {
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<Text>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<Text>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            try
            {
                var text2DRenderer = entity.GetComponent<Runtime.Text.TinyText2DRenderer>();
                var text2DStyle = entity.GetComponent<Runtime.Text.TinyText2DStyle>();
                var text2DStyleNativeFont = entity.GetComponent<Runtime.Text.TinyText2DStyleNativeFont>();
                var nativeFont = entity.GetComponent<Runtime.Text.TinyNativeFont>();

                var text = GetComponent<Text>(entity);

                text.text = text2DRenderer.text;
                var bold = text2DStyleNativeFont.weight == 700;
                var italic = text2DStyleNativeFont.italic;

                if (bold && italic)
                {
                    text.fontStyle = FontStyle.BoldAndItalic;
                }
                else if (bold)
                {
                    text.fontStyle = FontStyle.Bold;
                }
                else if (italic)
                {
                    text.fontStyle = FontStyle.Italic;
                }
                else
                {
                    text.fontStyle = FontStyle.Normal;
                }

                text.lineSpacing = 1;
                text.supportRichText = false;
                text.alignment = TinyGUIUtility.GetTextAnchorFromPivot(text2DRenderer.pivot);

                text.color = text2DStyle.color;
                switch (nativeFont.name)
                {
                    case TinyFontName.SansSerif:
                        text.font = TinyFonts.GetSansSerifFont(bold, italic);
                        break;
                    case TinyFontName.Serif:
                        text.font = TinyFonts.GetSerifFont(bold, italic);
                        break;
                    case TinyFontName.Monospace:
                        text.font = TinyFonts.GetMonoSpaceFont(bold, italic);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var actualWorldSize = text2DStyle.size * nativeFont.worldUnitsToPt;
                text.fontSize = (int)(1.35f * actualWorldSize);

                var autoFit = entity.GetComponent<TinyText2DAutoFit>(); 
                if (autoFit.IsValid)
                {
                    text.resizeTextForBestFit = true;
                    text.resizeTextMinSize = (int) (1.35f * autoFit.minSize * nativeFont.worldUnitsToPt);
                    text.resizeTextMaxSize = (int) (1.35f * autoFit.maxSize * nativeFont.worldUnitsToPt);
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                }
                else
                {
                    text.resizeTextForBestFit = false;
                    text.verticalOverflow = VerticalWrapMode.Overflow;
                    text.horizontalOverflow = HorizontalWrapMode.Overflow;
                }
            }
            finally
            {
                UnityEditor.SceneView.RepaintAll();
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>(entity).root as RectTransform);
            }
        }
    }
}

