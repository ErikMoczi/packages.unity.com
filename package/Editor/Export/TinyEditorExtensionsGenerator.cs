using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Tiny.Runtime.UIControlsExtensions;
using Unity.Tiny.Runtime.UIControls;

namespace Unity.Tiny
{
    internal static class TinyEditorExtensionsGenerator
    {
        private enum ExportedCurveType
        {
            Step,
            Linear,
            Bezier
        }

        private static readonly Dictionary<TinyObject, string> s_Gradients = new Dictionary<TinyObject, string>();
        private static readonly Dictionary<TinyObject, string> s_Curves = new Dictionary<TinyObject, string>();
        private static readonly Dictionary<TinyObject, string> s_Curves3 = new Dictionary<TinyObject, string>();
        private static readonly Dictionary<TinyObject, string> s_Transitions = new Dictionary<TinyObject, string>();

        public static string GetExportedAssetName(TinyObject tiny)
        {
            if (tiny.Type.Equals(TypeRefs.TinyEditorExtensions.Curve3Entity))
            {
                return GetCurve3Name(tiny);
            }

            if (tiny.Type.Equals(TypeRefs.TinyEditorExtensions.GradientEntity))
            {
                return GetGradientName(tiny);
            }

            if (tiny.Type.Equals(TypeRefs.TinyEditorExtensions.CurveEntity))
            {
                return GetCurveName(tiny);
            }

            if (tiny.Type.Equals(TypeRefs.UIControlsExtensions.TransitionEntity))
            {
                return GetTransitionName(tiny);
            }

            return string.Empty;
        }

        public static string GetExportedFieldType(TinyField field, TinyModule module)
        {
            var typeRef = field.FieldType;
            if (typeRef.Equals(TypeRefs.TinyEditorExtensions.GradientEntity)
                || typeRef.Equals(TypeRefs.TinyEditorExtensions.CurveEntity)
                || typeRef.Equals(TypeRefs.TinyEditorExtensions.Curve3Entity)
                || typeRef.Equals(TypeRefs.UIControlsExtensions.TransitionEntity))
            {
                return "ut.Entity";
            }

            return string.Empty;
        }

        public static string GetFieldTypeToIDL(IRegistry registry, TinyField field)
        {
            var typeRef = field.FieldType;
            if (typeRef.Equals(TypeRefs.TinyEditorExtensions.GradientEntity)
                || typeRef.Equals(TypeRefs.TinyEditorExtensions.CurveEntity)
                || typeRef.Equals(TypeRefs.TinyEditorExtensions.Curve3Entity)
                || typeRef.Equals(TypeRefs.UIControlsExtensions.TransitionEntity))
            {
                return "Entity";
            }

            return string.Empty;
        }

        public static void Generate(IRegistry registry, TinyProject project, TinyEntityGroup entityGroup)
        {
            var module = project.Module.Dereference(project.Registry);

            // Enumerate implicit assets references
            foreach (var entity in module.EnumerateDependencies().Entities())
            {
                foreach (var component in entity.Components)
                {
                    var properties = component.EnumerateProperties().ToList();

                    foreach (var gradientProp in properties.SelectMany(prop => GradientFilter(prop.Value)))
                    {
                        var hash = Guid.NewGuid();
                        var exportedName = $"assets/gradients/{hash:N}";

                        var gradientEntity = entityGroup.CreateEntity(exportedName);
                        var gradient = gradientProp.As<Gradient>();
                        var typeRef = gradient.mode == GradientMode.Blend
                            ? TypeRefs.Interpolation.LinearCurveColor
                            : TypeRefs.Interpolation.StepCurveColor;

                        var curve = gradientEntity.AddComponent(typeRef);
                        PopulateCurve(gradient, curve);
                        s_Gradients[gradientProp] = exportedName;
                    }

                    HashSet<TinyObject> visitedCurves = new HashSet<TinyObject>();
                    foreach (var curve3Prop in properties.SelectMany(prop => Curve3Filter(prop.Value)))
                    {
                        foreach (var subCurve in curve3Prop.EnumerateProperties()
                            .SelectMany(prop => CurveFilter(prop.Value)))
                        {
                            visitedCurves.Add(subCurve);
                        }

                        var hash = System.Guid.NewGuid();
                        var exportedName = $"assets/curves/{hash:N}";
                        var curveEntity = entityGroup.CreateEntity(exportedName);

                        var curveX = curve3Prop.GetProperty<AnimationCurve>("x");
                        var curveXType = GetCurveType(curveX);
                        var curveY = curve3Prop.GetProperty<AnimationCurve>("y");
                        var curveYType = GetCurveType(curveY);
                        var curveZ = curve3Prop.GetProperty<AnimationCurve>("z");
                        var curveZType = GetCurveType(curveZ);

                        var type = curveXType;
                        if (type != curveYType || type != curveZType)
                        {
                            type = ExportedCurveType.Bezier;
                        }
                        
                        NormalizeKeys(curveX, curveY, curveZ);

                        TinyObject curve;
                        switch (type)
                        {
                            case ExportedCurveType.Step:
                                curve = curveEntity.AddComponent(TypeRefs.Interpolation.StepCurveVector3);
                                PopulateStepCurve3(curveX, curveY, curveZ, curve);
                                break;
                            case ExportedCurveType.Linear:
                                curve = curveEntity.AddComponent(TypeRefs.Interpolation.LinearCurveVector3);
                                PopulateLinearCurve3(curveX, curveY, curveZ, curve);
                                break;
                            case ExportedCurveType.Bezier:
                                curve = curveEntity.AddComponent(TypeRefs.Interpolation.BezierCurveVector3);
                                PopulateBezierCurve3(curveX, curveY, curveZ, curve);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        s_Curves3[curve3Prop] = exportedName;
                    }

                    foreach (var curveProp in properties.SelectMany(prop => CurveFilter(prop.Value)))
                    {
                        if (visitedCurves.Contains(curveProp))
                        {
                            return;
                        }

                        var hash = Guid.NewGuid();
                        var exportedName = $"assets/curves/{hash:N}";
                        var curveEntity = entityGroup.CreateEntity(exportedName);
                        var animationCurve = curveProp.As<AnimationCurve>();

                        TinyObject curve;
                        switch (GetCurveType(animationCurve))
                        {
                            case ExportedCurveType.Step:
                                curve = curveEntity.AddComponent(TypeRefs.Interpolation.StepCurveFloat);
                                PopulateStepCurve(animationCurve, curve);
                                break;
                            case ExportedCurveType.Linear:
                                curve = curveEntity.AddComponent(TypeRefs.Interpolation.LinearCurveFloat);
                                PopulateLinearCurve(animationCurve, curve);
                                break;
                            case ExportedCurveType.Bezier:
                                curve = curveEntity.AddComponent(TypeRefs.Interpolation.BezierCurveFloat);
                                PopulateBezierCurve(animationCurve, curve);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        s_Curves[curveProp] = exportedName;
                    }

                    foreach (var transitionProp in properties.SelectMany(prop => TransitionFilter(prop.Value)))
                    {
                        var hash = Guid.NewGuid();
                        var exportedName = $"assets/transitions/{hash:N}";
                        var transitionEntity = entityGroup.CreateEntity(exportedName);
                        PopulateTransition(new TinyTransitionEntity(transitionProp), transitionEntity);
                        s_Transitions[transitionProp] = exportedName;
                    }
                }
            }
        }

        private static IEnumerable<TinyObject> GradientFilter(object @object)
        {
            if (@object is TinyObject tiny && tiny.Type.Equals(TypeRefs.TinyEditorExtensions.GradientEntity))
            {
                yield return tiny;
            }
        }

        private static IEnumerable<TinyObject> Curve3Filter(object @object)
        {
            if (@object is TinyObject tiny && tiny.Type.Equals(TypeRefs.TinyEditorExtensions.Curve3Entity))
            {
                yield return tiny;
            }
        }

        private static IEnumerable<TinyObject> CurveFilter(object @object)
        {
            if (@object is TinyObject tiny && tiny.Type.Equals(TypeRefs.TinyEditorExtensions.CurveEntity))
            {
                yield return tiny;
            }
        }
        
        private static IEnumerable<TinyObject> TransitionFilter(object @object)
        {
            if (@object is TinyObject tiny && tiny.Type.Equals(TypeRefs.UIControlsExtensions.TransitionEntity))
            {
                yield return tiny;
            }
        }

        private static void PopulateCurve(Gradient gradient, TinyObject export)
        {
            var currentColor = Color.white;
            var currentOffset = float.MaxValue;

            var registry = export.Registry;
            var times = export["times"] as TinyList;
            var values = export["values"] as TinyList;

            for (int i = 0, j = 0; i < gradient.alphaKeys.Length || j < gradient.colorKeys.Length;)
            {
                var alphaOffset = float.MaxValue;
                var colorOffset = float.MaxValue;

                if (i < gradient.alphaKeys.Length)
                {
                    alphaOffset = gradient.alphaKeys[i].time;
                }

                if (j < gradient.colorKeys.Length)
                {
                    colorOffset = gradient.colorKeys[j].time;
                }

                if (alphaOffset == colorOffset)
                {
                    currentColor = gradient.colorKeys[j].color;
                    currentColor.a = gradient.alphaKeys[i].alpha;
                    currentOffset = colorOffset;
                    ++i;
                    ++j;
                }
                else if (alphaOffset < colorOffset)
                {
                    currentColor.a = gradient.alphaKeys[i].alpha;
                    currentOffset = alphaOffset;
                    ++i;
                }
                else // if (alpha > color)
                {
                    var alpha = currentColor.a;
                    currentColor = gradient.colorKeys[j].color;
                    currentColor.a = alpha;
                    currentOffset = colorOffset;
                    ++j;
                }

                times.Add(currentOffset);
                values.Add(new TinyObject(registry, TypeRefs.Core2D.Color).AssignFrom(currentColor));
            }
        }

        private static ExportedCurveType GetCurveType(AnimationCurve curve)
        {
            var constant = true;
            var linear = true;
            for (int i = 0; i < curve.keys.Length; ++i)
            {
                var key = curve.keys[i];
                constant &= float.IsInfinity(key.inTangent) && float.IsInfinity(key.outTangent);
                linear &= key.inWeight == 0 && key.outWeight == 0;
            }

            if (constant)
            {
                return ExportedCurveType.Step;
            }

            if (linear)
            {
                return ExportedCurveType.Linear;
            }

            return ExportedCurveType.Bezier;
        }

        private static void PopulateStepCurve(AnimationCurve curve, TinyObject export)
        {
            var times = export["times"] as TinyList;
            var values = export["values"] as TinyList;
            foreach (var key in curve.keys)
            {
                times.Add(key.time);
                values.Add(key.value);
            }
        }

        private static void PopulateStepCurve3(
            AnimationCurve curveX,
            AnimationCurve curveY,
            AnimationCurve curveZ,
            TinyObject export)
        {
            var times = export["times"] as TinyList;
            var values = export["values"] as TinyList;

            var keyX = curveX.keys.OrderBy(k => k.time).ToList();
            var keyY = curveY.keys.OrderBy(k => k.time).ToList();
            var keyZ = curveZ.keys.OrderBy(k => k.time).ToList();
            
            for (var i = 0; i < curveX.length; ++i)
            {
                times.Add(keyX[i].time);
                values.Add(new TinyObject(export.Registry, TypeRefs.Math.Vector3)
                {
                    ["x"] = keyX[i].value,
                    ["y"] = keyY[i].value,
                    ["z"] = keyZ[i].value
                });
            }
        }
        
        private static void PopulateLinearCurve(AnimationCurve curve, TinyObject export)
        {
            var times = export["times"] as TinyList;
            var values = export["values"] as TinyList;
            foreach (var key in curve.keys)
            {
                times.Add(key.time);
                values.Add(key.value);
            }
        }
        
        private static void PopulateLinearCurve3(
            AnimationCurve curveX,
            AnimationCurve curveY,
            AnimationCurve curveZ,
            TinyObject export)
        {
            var times = export["times"] as TinyList;
            var values = export["values"] as TinyList;

            var keyX = curveX.keys.OrderBy(k => k.time).ToList();
            var keyY = curveY.keys.OrderBy(k => k.time).ToList();
            var keyZ = curveZ.keys.OrderBy(k => k.time).ToList();
            
            for (var i = 0; i < curveX.length; ++i)
            {
                times.Add(keyX[i].time);
                values.Add(new TinyObject(export.Registry, TypeRefs.Math.Vector3)
                {
                    ["x"] = keyX[i].value,
                    ["y"] = keyY[i].value,
                    ["z"] = keyZ[i].value
                });
            }
        }

        private static void PopulateBezierCurve(AnimationCurve curve, TinyObject export)
        {
            var times = export["times"] as TinyList;
            var values = export["values"] as TinyList;
            var outValues = export["outValues"] as TinyList;
            var inValues = export["inValues"] as TinyList;

            var keys = curve.keys;
            for (var i = 0; i < keys.Length; i++)
            {
                var current = keys[i];
                times.Add(current.time);
                values.Add(current.value);
                if (i == 0)
                {
                    inValues.Add(current.inTangent);
                }

                if (i == keys.Length - 1)
                {
                    outValues.Add(current.outTangent);
                    continue;
                }

                var next = keys[i + 1];
                var start = current.value;
                var end = next.value;
                var d = (next.time - current.time) / 3.0f;
                var p0 = end - d * next.inTangent *
                         (current.weightedMode.HasFlag(WeightedMode.In) ? current.inWeight : 1.0f);
                var p1 = start + d * current.outTangent *
                         (current.weightedMode.HasFlag(WeightedMode.In) ? current.outWeight : 1.0f);
                inValues.Add(p0);
                outValues.Add(p1);
            }
        }
        
        private static void PopulateBezierCurve3(
            AnimationCurve curveX,
            AnimationCurve curveY,
            AnimationCurve curveZ,
            TinyObject export)
        {
            var times = export["times"] as TinyList;
            var values = export["values"] as TinyList;
            var outValues = export["outValues"] as TinyList;
            var inValues = export["inValues"] as TinyList;
            
            var keyX = curveX.keys.OrderBy(k => k.time).ToList();
            var keyY = curveY.keys.OrderBy(k => k.time).ToList();
            var keyZ = curveZ.keys.OrderBy(k => k.time).ToList();

            for (var i = 0; i < curveX.length; i++)
            {
                var kX = keyX[i];
                var kY = keyY[i];
                var kZ = keyZ[i];
                times.Add(kX.time);
                values.Add(new TinyObject(export.Registry, TypeRefs.Math.Vector3)
                {
                    ["x"] = keyX[i].value,
                    ["y"] = keyY[i].value,
                    ["z"] = keyZ[i].value
                });
                
                if (i == 0)
                {
                    inValues.Add(new TinyObject(export.Registry, TypeRefs.Math.Vector3)
                    {
                        ["x"] = kX.inTangent,
                        ["y"] = kY.inTangent,
                        ["z"] = kZ.inTangent
                    });
                }

                if (i == curveX.length - 1)
                {
                    outValues.Add(new TinyObject(export.Registry, TypeRefs.Math.Vector3)
                    {
                        ["x"] = kX.outTangent,
                        ["y"] = kY.outTangent,
                        ["z"] = kZ.outTangent
                    });
                    continue;
                }

                var inOutX = Get(kX, keyX[i + 1]);
                var inOutY = Get(kY, keyY[i + 1]);
                var inOutZ = Get(kZ, keyZ[i + 1]);
                
                inValues.Add(new TinyObject(export.Registry, TypeRefs.Math.Vector3)
                {
                    ["x"] = inOutX.x,
                    ["y"] = inOutY.x,
                    ["z"] = inOutZ.x
                });
                    
                outValues.Add(new TinyObject(export.Registry, TypeRefs.Math.Vector3)
                {
                    ["x"] = inOutX.y,
                    ["y"] = inOutY.y,
                    ["z"] = inOutZ.y
                });
            }
        }

        private static Vector2 Get(Keyframe current, Keyframe next)
        {
            var start = current.value;
            var end = next.value;
            var d = (next.time - current.time) / 3.0f;
            var p0 = end - d * next.inTangent *
                     (current.weightedMode.HasFlag(WeightedMode.In) ? current.inWeight : 1.0f);
            var p1 = start + d * current.outTangent *
                     (current.weightedMode.HasFlag(WeightedMode.In) ? current.outWeight : 1.0f);
            return new Vector2(p0, p1);
        }

        private static string GetGradientName(TinyObject gradient)
        {
            return s_Gradients.TryGetValue(gradient, out var name) ? name : string.Empty;
        }

        private static string GetCurveName(TinyObject curve)
        {
            return s_Curves.TryGetValue(curve, out var name) ? name : string.Empty;
        }

        private static string GetCurve3Name(TinyObject curve)
        {
            return s_Curves3.TryGetValue(curve, out var name) ? name : string.Empty;
        }
        
        private static string GetTransitionName(TinyObject curve)
        {
            return s_Transitions.TryGetValue(curve, out var name) ? name : string.Empty;
        }

        private static void NormalizeKeys(params AnimationCurve[] curves)
        {
            var time = new HashSet<float>();
            foreach (var curve in curves)
            {
                foreach (var key in curve.keys)
                {
                    time.Add(key.time);
                }
            }

            var allTimes = time.ToList();
            allTimes.Sort();

            foreach (var curve in curves)
            {
                foreach (var t in allTimes)
                {
                    curve.AddKey(t, curve.Evaluate(t));
                }
            }
        }

        private static void PopulateTransition(TinyTransitionEntity transition, TinyEntity export)
        {
            switch (transition.type)
            {
                case TinyTransitionType.ColorTint:
                    var tint = export.AddComponent<TinyColorTintTransition>();
                    tint.normal = transition.colorTint.normal;
                    tint.hover = transition.colorTint.hover;
                    tint.pressed = transition.colorTint.pressed;
                    tint.disabled = transition.colorTint.disabled;
                    break;
                case TinyTransitionType.Sprite:
                    var sprite = export.AddComponent<TinySpriteTransition>();
                    sprite.normal = transition.spriteSwap.normal;
                    sprite.hover = transition.spriteSwap.hover;
                    sprite.pressed = transition.spriteSwap.pressed;
                    sprite.disabled = transition.spriteSwap.disabled;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }            
        }
    }
}