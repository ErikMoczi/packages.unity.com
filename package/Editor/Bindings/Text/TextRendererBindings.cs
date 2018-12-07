

using System;
using JetBrains.Annotations;
using Unity.Tiny.Runtime.Text;
using UnityEngine;

namespace Unity.Tiny
{
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
    internal class TextRendererBindings : BindingProfile
    {
        
        
        
        public override void LoadBindings(TinyEntity entity)
        {
            AddMissingComponent<MeshRenderer>(entity, renderer =>
            {
                renderer.sharedMaterial = new Material(Shader.Find("GUI/Text Shader"));
            });
            AddMissingComponent<TextMesh>(entity);
        }

        public override void UnloadBindings(TinyEntity entity)
        {
            RemoveComponent<TextMesh>(entity);
            RemoveComponent<MeshRenderer>(entity);
        }

        public override void Transfer(TinyEntity entity)
        {
            try
            {
                var text2DRenderer = entity.GetComponent<Runtime.Text.TinyText2DRenderer>();
                var text2DStyle = entity.GetComponent<Runtime.Text.TinyText2DStyle>();
                var text2DStyleNativeFont = entity.GetComponent<Runtime.Text.TinyText2DStyleNativeFont>();
                var nativeFont = entity.GetComponent<Runtime.Text.TinyNativeFont>();

                var textMesh = GetComponent<TextMesh>(entity);

                textMesh.text = text2DRenderer.text;

                var bold = text2DStyleNativeFont.weight == 700;
                var italic = text2DStyleNativeFont.italic;

                if (bold && italic)
                {
                    textMesh.fontStyle = FontStyle.BoldAndItalic;
                }
                else if (bold)
                {
                    textMesh.fontStyle = FontStyle.Bold;
                }
                else if (italic)
                {
                    textMesh.fontStyle = FontStyle.Italic;
                }
                else
                {
                    textMesh.fontStyle = FontStyle.Normal;
                }

                textMesh.lineSpacing = 1;
                textMesh.richText = false;
                textMesh.alignment = TextAlignment.Left;

                textMesh.anchor = TinyGUIUtility.GetTextAnchorFromPivot(text2DRenderer.pivot);

                textMesh.color = text2DStyle.color;
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

                var actualWorldSize = text2DStyle.size * nativeFont.worldUnitsToPt;
                textMesh.characterSize = actualWorldSize / textMesh.font.fontSize;
                textMesh.fontSize = (int)(textMesh.font.fontSize * 12.5f);

                var renderer = GetComponent<MeshRenderer>(entity);
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.Clear();
                block.SetTexture("_MainTex", textMesh.font.material.mainTexture);
                renderer.SetPropertyBlock(block);
            }
            finally
            {
                UnityEditor.SceneView.RepaintAll();
            }
        }
    }
}

