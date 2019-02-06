
using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
    [UsedImplicitly]
    internal class AnimationCurveConverter : IConverterTo<AnimationCurve>, IConverterFrom<AnimationCurve>
    {
        [TinyCachable, UsedImplicitly] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<AnimationCurve>(TypeRefs.TinyEditorExtensions.CurveEntity);

        private static class Names
        {
            public const string keys         = nameof(keys);
            public const string value        = nameof(value);
            public const string time         = nameof(time);
            public const string inTangent    = nameof(inTangent);
            public const string outTangent   = nameof(outTangent);
            public const string inWeight     = nameof(inWeight);
            public const string outWeight    = nameof(outWeight);
            public const string weightedMode = nameof(weightedMode);
            public const string preWrapMode  = nameof(preWrapMode);
            public const string postWrapMode = nameof(postWrapMode);
        }

        public AnimationCurve ConvertTo(object @object, IRegistry registry)
        {
            return ConvertTo(@object as TinyObject);
        }

        private AnimationCurve ConvertTo(TinyObject tinyCurve)
        {
            ValidateType(tinyCurve);
            var curve = new AnimationCurve();
            curve.preWrapMode = tinyCurve.GetProperty<WrapMode>(Names.preWrapMode);
            curve.postWrapMode = tinyCurve.GetProperty<WrapMode>(Names.postWrapMode);

            var keys = tinyCurve[Names.keys] as TinyList;
            foreach (TinyObject key in keys)
            {
                curve.AddKey(new Keyframe
                {
                    value = key.GetProperty<float>(Names.value),
                    time = key.GetProperty<float>(Names.time),
                    inTangent = key.GetProperty<float>(Names.inTangent),
                    inWeight = key.GetProperty<float>(Names.inWeight),
                    outTangent = key.GetProperty<float>(Names.outTangent),
                    outWeight = key.GetProperty<float>(Names.outWeight),
                    weightedMode = key.GetProperty<WeightedMode>(Names.weightedMode)
                });
            }
            return curve;
        }

        public TinyObject ConvertFrom(TinyObject tinyCurve, AnimationCurve curve)
        {
            ValidateType(tinyCurve);
            tinyCurve.AssignPropertyFrom(Names.preWrapMode, curve.preWrapMode);
            tinyCurve.AssignPropertyFrom(Names.postWrapMode, curve.postWrapMode);
            
            var keys = tinyCurve[Names.keys] as TinyList;
            keys.Clear();
            foreach (var key in curve.keys)
            {
                var tinyKey = new TinyObject(tinyCurve.Registry, TypeRefs.TinyEditorExtensions.KeyFrame);
                tinyKey.AssignPropertyFrom(Names.value, key.value);
                tinyKey.AssignPropertyFrom(Names.time, key.time);
                tinyKey.AssignPropertyFrom(Names.inTangent, key.inTangent);
                tinyKey.AssignPropertyFrom(Names.inWeight, key.inWeight);
                tinyKey.AssignPropertyFrom(Names.outTangent, key.outTangent);
                tinyKey.AssignPropertyFrom(Names.outWeight, key.outWeight);
                tinyKey.AssignPropertyFrom(Names.weightedMode, key.weightedMode);
                keys.Add(tinyKey);
            }
            return tinyCurve;
        }

        public object ConvertFrom(object @object, AnimationCurve value, IRegistry registry)
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

            if (!@object.Type.Equals(TypeRefs.TinyEditorExtensions.CurveEntity))
            {
                throw new InvalidOperationException($"Cannot convert value to or from {nameof(AnimationCurve)}");
            }
        }
    }
}
