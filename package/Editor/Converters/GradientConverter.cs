using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [UsedImplicitly]
    internal class GradientConverter : IConverterTo<Gradient>, IConverterFrom<Gradient>
    {
        [TinyCachable, UsedImplicitly] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<Gradient>(TypeRefs.TinyEditorExtensions.GradientEntity);

        private static class Names
        {
            public const string mode      = nameof(mode);
            public const string alphaKeys = nameof(alphaKeys);
            public const string alpha     = nameof(alpha);
            public const string time      = nameof(time);
            public const string colorKeys = nameof(colorKeys);
            public const string color     = nameof(color);
        }
        
        public Gradient ConvertTo(object @object, IRegistry registry)
        {
            return ConvertTo(@object as TinyObject);
        }

        private Gradient ConvertTo(TinyObject tinyGradient)
        {
            ValidateType(tinyGradient);
            var gradient = new Gradient();
            gradient.mode = tinyGradient.GetProperty<GradientMode>(Names.mode);

            var alphaKeys = tinyGradient[Names.alphaKeys] as TinyList;
            var alphas = new List<GradientAlphaKey>(alphaKeys.Count);
            foreach (TinyObject key in alphaKeys)
            {
                alphas.Add(new GradientAlphaKey(
                    key.GetProperty<float>(Names.alpha),
                    key.GetProperty<float>(Names.time))
                );
            }
            
            var colorKeys = tinyGradient[Names.colorKeys] as TinyList;
            var colors = new List<GradientColorKey>(colorKeys.Count);
            foreach (TinyObject key in colorKeys)
            {
                colors.Add(new GradientColorKey(
                    key.GetProperty<Color>(Names.color),
                    key.GetProperty<float>(Names.time))
                );
            }

            gradient.alphaKeys = alphas.ToArray();
            gradient.colorKeys = colors.ToArray();
            return gradient;
        }

        public TinyObject ConvertFrom(TinyObject tiny, Gradient gradient)
        {
            ValidateType(tiny);
            var registry = tiny.Registry;
            
            tiny.AssignPropertyFrom(Names.mode, gradient.mode);

            var alphaKeys = tiny[Names.alphaKeys] as TinyList;
            alphaKeys.Clear();
            foreach (var key in gradient.alphaKeys)
            {
                alphaKeys.Add(new TinyObject(registry, TypeRefs.TinyEditorExtensions.GradientAlphaKey)
                {
                    [Names.alpha] = key.alpha,
                    [Names.time] = key.time
                });
            }
            
            var colorKeys = tiny[Names.colorKeys] as TinyList;
            colorKeys.Clear();
            foreach (var key in gradient.colorKeys)
            {
                colorKeys.Add(new TinyObject(registry, TypeRefs.TinyEditorExtensions.GradientColorKey)
                {
                    [Names.color] = new TinyObject(registry, TypeRefs.Core2D.Color).AssignFrom(key.color),
                    [Names.time]  = key.time
                });
            }
            return tiny;
        }
        public object ConvertFrom(object @object, Gradient value, IRegistry registry)
        {
            return ConvertFrom(@object as TinyObject, value);
        }

        private void ValidateType(TinyObject @object)
        {
            if (null == @object)
            {
                throw new ArgumentNullException(nameof(@object));
            }

            if (null == @object.Registry)
            {
                throw new ArgumentNullException(nameof(@object.Registry));
            }

            if (!@object.Type.Equals(TypeRefs.TinyEditorExtensions.GradientEntity))
            {
                throw new InvalidOperationException($"Cannot convert value to or from {nameof(Gradient)}");
            }
        }

    }
}

