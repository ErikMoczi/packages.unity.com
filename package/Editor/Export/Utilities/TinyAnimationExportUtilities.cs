using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Unity.Tiny.Runtime.Animation;

using UnityEngine;
using UnityEditor;

namespace Unity.Tiny
{
    internal class TinyAnimationExportUtilities
    {
        #region Members
        private static Dictionary<string, uint> s_IndexMap = new Dictionary<string, uint>() { { ".x", 0 }, { ".y", 1 }, { ".z", 2 }, { ".w", 3 },
                                                                                    { ".r", 0 }, { ".g", 1 }, { ".b", 2 }, { ".a", 3 } };
        #endregion

        #region Types
        //Identifier so the objectToEntityMap can pick curve entities
        private class AnimationCurveUnityObject : ScriptableObject
        {
        }

        private delegate void PopulateCurveDelegate(TinyEntity curveEntity, AnimationCurve[] unityCurves, TinyType.Reference interpolationType);
        #endregion

        #region PopulateAnimationClipEntity
        internal static void PopulateAnimationClipEntity(TinyEntityGroup entityGroup, Dictionary<UnityEngine.Object, TinyEntity> objectToEntityMap, TinyEntity clipEntity, AnimationClip clip)
        {
            //creating the clip component that will hold all the curves' data
            var animationClip = clipEntity.AddComponent(TypeRefs.Animation.AnimationClip);
            var editorCurves = AnimationUtility.GetCurveBindings(clip);
            AnimationCurve[] unityCurves;

            //Generic reusable parameters
            PopulateCurveDelegate populateCurveDelegate;
            TinyType.Reference interpolationType;
            TinyType.Reference descType;
            string genericDescName = "";
            string typedDescName = "";

            var index = 0;
            while(index < editorCurves.Length)
            {
                //creating the curve entity
                var curveEntity = clipEntity.Registry.CreateEntity(TinyId.New(), clipEntity.Name + $"_AnimationCurve{index}");

                entityGroup.AddEntityReference(curveEntity.Ref);
                objectToEntityMap.Add(ScriptableObject.CreateInstance<AnimationCurveUnityObject>(), curveEntity);

                //each clip has a list of typed descriptors, each of them having one generic descriptor, 
                //which holds data for once curve and target information
                var desc = new TinyObject(clipEntity.Registry, TypeRefs.Animation.AnimationPlayerDesc);

                //for the moment, propertyOffset and component ID are hardcoded, at runtime
                desc[nameof(TinyAnimationPlayerDesc.curve)] = curveEntity.Ref;

                //check tangent mode and value types to create the proper curves with proper constructors
                //ex : Linear mode and Vector2 values will generate LinearCurveVector2 curve entities and use 
                //     the TinyAnimationPlayerDescVector2 descriptor

                switch (GetTargetType(clip, editorCurves, index))
                {
                    case var type when type == typeof(Vector2):
                        descType = TypeRefs.Animation.AnimationPlayerDescVector2;
                        genericDescName = nameof(TinyAnimationPlayerDescVector2.desc);
                        typedDescName = nameof(TinyAnimationClip.animationPlayerDescVector2);
                        InitializeUnityCurves(clip, editorCurves, out unityCurves, 2, ref index);
                        populateCurveDelegate = PopulateVector2CurveEntity;
                        switch (AnimationUtility.GetKeyRightTangentMode(unityCurves[0], 0))
                        {
                            case AnimationUtility.TangentMode.Constant:
                                interpolationType = TypeRefs.Interpolation.StepCurveVector2;
                                break;
                            case AnimationUtility.TangentMode.Linear:
                                interpolationType = TypeRefs.Interpolation.LinearCurveVector2;
                                break;
                            default:
                                interpolationType = TypeRefs.Interpolation.BezierCurveVector2;
                                break;
                        }
                        break;
                    case var type when type == typeof(Vector3):
                        descType = TypeRefs.Animation.AnimationPlayerDescVector3;
                        genericDescName = nameof(TinyAnimationPlayerDescVector3.desc);
                        typedDescName = nameof(TinyAnimationClip.animationPlayerDescVector3);
                        InitializeUnityCurves(clip, editorCurves, out unityCurves, 3, ref index);
                        populateCurveDelegate = PopulateVector3CurveEntity;
                        switch (AnimationUtility.GetKeyRightTangentMode(unityCurves[0], 0))
                        {
                            case AnimationUtility.TangentMode.Constant:
                                interpolationType = TypeRefs.Interpolation.StepCurveVector3;
                                break;
                            case AnimationUtility.TangentMode.Linear:
                                interpolationType = TypeRefs.Interpolation.LinearCurveVector3;
                                break;
                            default:
                                interpolationType = TypeRefs.Interpolation.BezierCurveVector3;
                                break;
                        }
                        break;
                    case var type when type == typeof(Quaternion):
                        descType = TypeRefs.Animation.AnimationPlayerDescQuaternion;
                        genericDescName = nameof(TinyAnimationPlayerDescQuaternion.desc);
                        typedDescName = nameof(TinyAnimationClip.animationPlayerDescQuaternion);
                        InitializeUnityCurves(clip, editorCurves, out unityCurves, 4, ref index);
                        populateCurveDelegate = PopulateQuaternionCurveEntity;
                        switch (AnimationUtility.GetKeyRightTangentMode(unityCurves[0], 0))
                        {
                            case AnimationUtility.TangentMode.Constant:
                                interpolationType = TypeRefs.Interpolation.StepCurveQuaternion;
                                break;
                            case AnimationUtility.TangentMode.Linear:
                                interpolationType = TypeRefs.Interpolation.LinearCurveQuaternion;
                                break;
                            default:
                                interpolationType = TypeRefs.Interpolation.BezierCurveQuaternion;
                                break;
                        }
                        break;
                    case var type when type == typeof(Color):
                        descType = TypeRefs.Animation.AnimationPlayerDescColor;
                        genericDescName = nameof(TinyAnimationPlayerDescColor.desc);
                        typedDescName = nameof(TinyAnimationClip.animationPlayerDescQuaternion);
                        InitializeUnityCurves(clip, editorCurves, out unityCurves, 4, ref index);
                        populateCurveDelegate = PopulateColorCurveEntity;
                        switch (AnimationUtility.GetKeyRightTangentMode(unityCurves[0], 0))
                        {
                            case AnimationUtility.TangentMode.Constant:
                                interpolationType = TypeRefs.Interpolation.StepCurveColor;
                                break;
                            case AnimationUtility.TangentMode.Linear:
                                interpolationType = TypeRefs.Interpolation.LinearCurveColor;
                                break;
                            default:
                                interpolationType = TypeRefs.Interpolation.BezierCurveColor;
                                break;
                        }
                        break;
                    default:
                        descType = TypeRefs.Animation.AnimationPlayerDescFloat;
                        genericDescName = nameof(TinyAnimationPlayerDescFloat.desc);
                        typedDescName = nameof(TinyAnimationClip.animationPlayerDescFloat);
                        InitializeUnityCurves(clip, editorCurves, out unityCurves, 1, ref index);
                        populateCurveDelegate = PopulateFloatCurveEntity;
                        switch (AnimationUtility.GetKeyRightTangentMode(unityCurves[0], 0))
                        {
                            case AnimationUtility.TangentMode.Constant:
                                interpolationType = TypeRefs.Interpolation.StepCurveFloat;
                                break;
                            case AnimationUtility.TangentMode.Linear:
                                interpolationType = TypeRefs.Interpolation.LinearCurveFloat;
                                break;
                            default:
                                interpolationType = TypeRefs.Interpolation.BezierCurveFloat;
                                break;
                        }
                        break;
                }

                if (unityCurves.Length > 1)
                {
                    AddInbetweenKeys(unityCurves);
                }

                BakeResultsInTangents(unityCurves);

                populateCurveDelegate(curveEntity, unityCurves, interpolationType);
                AddDescriptor(animationClip, desc, descType, genericDescName, typedDescName);
            }
        }

        #endregion

        #region PopulateCurveEntity
        private static void PopulateFloatCurveEntity(TinyEntity curveEntity, AnimationCurve[] unityCurves, TinyType.Reference interpolationType)
        {
            var curveObject = curveEntity.AddComponent(interpolationType);

            for (var i = 0; i < unityCurves[0].keys.Length; ++i)
            {
                ((TinyList)curveObject["times"]).Add(unityCurves[0].keys[i].time);

                float value, inValue, outValue;
                GetValues(unityCurves[0], i, out value, out inValue, out outValue);

                ((TinyList)curveObject["values"]).Add(value);

                if (curveObject.HasProperty("inValues")) //if it has inValues, it should have outValues
                {
                    ((TinyList)curveObject["inValues"]).Add(inValue);
                    ((TinyList)curveObject["outValues"]).Add(outValue);
                }
            }
        }

        private static void PopulateVector2CurveEntity(TinyEntity curveEntity, AnimationCurve[] unityCurves, TinyType.Reference interpolationType)
        {
            var curveObject = curveEntity.AddComponent(interpolationType);
            var converter = new Vector2Converter();

            for (var i = 0; i < unityCurves[0].keys.Length; ++i)
            {
                Vector2 value, inValue, outValue;
                GetValues(unityCurves, i, out value, out inValue, out outValue);
                AssignKey(curveObject, TypeRefs.Math.Vector2, unityCurves[0].keys[i].time, value, inValue, outValue, converter);
            }
        }

        private static void PopulateVector3CurveEntity(TinyEntity curveEntity, AnimationCurve[] unityCurves, TinyType.Reference interpolationType)
        {
            var curveObject = curveEntity.AddComponent(interpolationType);
            var converter = new Vector3Converter();

            for (var i = 0; i < unityCurves[0].keys.Length; ++i)
            {
                Vector3 value, inValue, outValue;
                GetValues(unityCurves, i, out value, out inValue, out outValue);
                AssignKey(curveObject, TypeRefs.Math.Vector3, unityCurves[0].keys[i].time, value, inValue, outValue, converter);
            }
        }

        private static void PopulateQuaternionCurveEntity(TinyEntity curveEntity, AnimationCurve[] unityCurves, TinyType.Reference interpolationType)
        {
            var curveObject = curveEntity.AddComponent(interpolationType);
            var converter = new QuaternionConverter();

            for (var i = 0; i < unityCurves[0].keys.Length; ++i)
            {
                Quaternion value, inValue, outValue;
                GetValues(unityCurves, i, out value, out inValue, out outValue);
                AssignKey(curveObject, TypeRefs.Math.Quaternion, unityCurves[0].keys[i].time, value, inValue, outValue, converter);
            }
        }

        private static void PopulateColorCurveEntity(TinyEntity curveEntity, AnimationCurve[] unityCurves, TinyType.Reference interpolationType)
        {
            var curveObject = curveEntity.AddComponent(interpolationType);
            var converter = new ColorConverter();

            for (var i = 0; i < unityCurves[0].keys.Length; ++i)
            {
                Color value, inValue, outValue;
                GetValues(unityCurves, i, out value, out inValue, out outValue);
                AssignKey(curveObject, TypeRefs.Core2D.Color, unityCurves[0].keys[i].time, value, inValue, outValue, converter);
            }
        }

        private static void AssignKey<T>(TinyObject curveObject, TinyType.Reference typeRef, float time, T value, T inValue, T outValue, IConverterFrom<T> converter)
        {
            TinyObject tinyValue, tinyInValue, tinyOutValue;
            tinyValue = new TinyObject(curveObject.Registry, typeRef);

            tinyValue = converter.ConvertFrom(tinyValue, value);

            ((TinyList)curveObject["times"]).Add(time);
            ((TinyList)curveObject["values"]).Add(tinyValue);

            if (curveObject.HasProperty("inValues")) //if it has inValues, it should have outValues
            {
                tinyInValue = new TinyObject(curveObject.Registry, typeRef);
                tinyInValue = converter.ConvertFrom(tinyInValue, inValue);

                tinyOutValue = new TinyObject(curveObject.Registry, typeRef);
                tinyOutValue = converter.ConvertFrom(tinyOutValue, outValue);

                ((TinyList)curveObject["inValues"]).Add(tinyInValue);
                ((TinyList)curveObject["outValues"]).Add(tinyOutValue);
            }
        }
        #endregion

        #region GetValues
        private static void GetValues(AnimationCurve unityCurve, int i, out float value, out float inValue, out float outValue)
        {
            var key = unityCurve.keys[i];

            value = key.value;
            inValue = key.inTangent;
            outValue = key.outTangent;
        }

        private static void GetValues(AnimationCurve[] unityCurves, int i, out Vector2 value, out Vector2 inValue, out Vector2 outValue)
        {
            value = new Vector2();
            inValue = new Vector2();
            outValue = new Vector2();

            GetValues(unityCurves[0], i, out value.x, out inValue.x, out outValue.x);
            GetValues(unityCurves[1], i, out value.y, out inValue.y, out outValue.y);
        }

        private static void GetValues(AnimationCurve[] unityCurves, int i, out Vector3 value, out Vector3 inValue, out Vector3 outValue)
        {
            value = new Vector3();
            inValue = new Vector3();
            outValue = new Vector3();

            GetValues(unityCurves[0], i, out value.x, out inValue.x, out outValue.x);
            GetValues(unityCurves[1], i, out value.y, out inValue.y, out outValue.y);
            GetValues(unityCurves[2], i, out value.z, out inValue.z, out outValue.z);
        }

        private static void GetValues(AnimationCurve[] unityCurves, int i, out Quaternion value, out Quaternion inValue, out Quaternion outValue)
        {
            value = new Quaternion();
            inValue = new Quaternion();
            outValue = new Quaternion();

            GetValues(unityCurves[0], i, out value.x, out inValue.x, out outValue.x);
            GetValues(unityCurves[1], i, out value.y, out inValue.y, out outValue.y);
            GetValues(unityCurves[2], i, out value.z, out inValue.z, out outValue.z);
            GetValues(unityCurves[3], i, out value.w, out inValue.w, out outValue.w);
        }

        private static void GetValues(AnimationCurve[] unityCurves, int i, out Color value, out Color inValue, out Color outValue)
        {
            value = new Color();
            inValue = new Color();
            outValue = new Color();

            GetValues(unityCurves[0], i, out value.r, out inValue.r, out outValue.r);
            GetValues(unityCurves[1], i, out value.g, out inValue.g, out outValue.g);
            GetValues(unityCurves[2], i, out value.b, out inValue.b, out outValue.b);
            GetValues(unityCurves[3], i, out value.a, out inValue.a, out outValue.a);
        }
        #endregion

        #region Utilities
        private static Type GetTargetType(AnimationClip clip, EditorCurveBinding[] editorCurves, int index)
        {
            var length = 0;
            var propertyNameExtension = "";

            while (true)
            {
                ++length;
                propertyNameExtension = Path.GetExtension(editorCurves[index++].propertyName).ToLowerInvariant();

                if (index >= editorCurves.Length ||
                    propertyNameExtension == "" ||
                    false == s_IndexMap.ContainsKey(propertyNameExtension) ||
                    false == IsContinuousCurves(clip, editorCurves[index - 1], editorCurves[index]))
                {
                    break;
                }
            }

            switch (length)
            {
                case 2:
                    return typeof(Vector2);
                case 3:
                    return typeof(Vector3);
                case 4:
                    if (propertyNameExtension == ".a")
                        return typeof(Color);
                    return typeof(Quaternion);
                default:
                    return typeof(float);
            }
        }

        private static bool IsContinuousCurves(AnimationClip clip, EditorCurveBinding current, EditorCurveBinding next)
        {
            //must be of same type
            if(current.type != next.type)
            {
                return false;
            }

            //must have the same interpolation type
            if(AnimationUtility.GetKeyRightTangentMode(AnimationUtility.GetEditorCurve(clip, current), 0) != 
                AnimationUtility.GetKeyRightTangentMode(AnimationUtility.GetEditorCurve(clip, next), 0))
            {
                return false;
            }

            //must target the same property
            if(Path.GetFileNameWithoutExtension(current.propertyName) != Path.GetFileNameWithoutExtension(next.propertyName))
            {
                return false;
            }

            //must be continuous on the same property
            if(s_IndexMap[Path.GetExtension(current.propertyName).ToLowerInvariant()] + 1 != s_IndexMap[Path.GetExtension(next.propertyName).ToLowerInvariant()])
            {
                return false;
            }

            return true;
        }

        private static void InitializeUnityCurves(AnimationClip clip, EditorCurveBinding[] editorCurves, out AnimationCurve[] unityCurves, uint size, ref int index)
        {
            unityCurves = new AnimationCurve[size];

            for (var i = 0; i < size; ++i)
            {
                unityCurves[i] = AnimationUtility.GetEditorCurve(clip, editorCurves[index++]);
            }
        }

        private static void AddInbetweenKeys(AnimationCurve[] unityCurves)
        {
            var timeList = new List<float>();
            foreach (var unityCurve in unityCurves)
            {
                timeList.AddRange(unityCurve.keys.Where(k => !timeList.Contains(k.time)).Select(k => k.time));
            }

            foreach (var unityCurve in unityCurves)
            {
                //for each time that's not already on the curve, add a key at that time
                foreach (var time in timeList.Except(unityCurve.keys.Select(k => k.time)))
                {
                    //this will compute correct tangents
                    TinyEditorBridge.AddInbetweenKeyToCurve(unityCurve, time);
                }
            }
        }
        private static void BakeResultsInTangents(AnimationCurve[] unityCurves)
        {
            //this bakes in the tangent the delta time factor
            //todo : have a more precise bezier match between Tiny and Unity
            var keyCount = unityCurves[0].keys.Length;
            if (keyCount > 1)
            {
                foreach (var unityCurve in unityCurves)
                {
                    var keys = new Keyframe[keyCount];

                    for (var keyIndex = 0; keyIndex < keyCount; ++keyIndex)
                    {
                        var currentKey = unityCurve.keys[keyIndex];

                        if (keyIndex > 0)
                        {
                            var previousKey = keys[keyIndex - 1];
                            currentKey.inTangent *= (currentKey.time - previousKey.time) * currentKey.inWeight;
                            currentKey.inTangent = currentKey.value - currentKey.inTangent;

                            //approximation of Unity's behaviour
                            var oldOutTangent = previousKey.outTangent;
                            previousKey.outTangent = Mathf.Lerp(currentKey.inTangent, previousKey.outTangent, previousKey.outWeight);
                            currentKey.inTangent = Mathf.Lerp(oldOutTangent, currentKey.inTangent, currentKey.inWeight);
                        }

                        if (keyIndex < keyCount - 1)
                        {
                            var nextKey = unityCurve.keys[keyIndex + 1];
                            currentKey.outTangent *= (nextKey.time - currentKey.time) * currentKey.outWeight;
                            currentKey.outTangent = currentKey.value + currentKey.outTangent;
                        }

                        keys[keyIndex] = currentKey;
                    }

                    unityCurve.keys = keys;
                }
            }
        }

        private static void AddDescriptor(TinyObject animationClip, TinyObject genericDesc, TinyType.Reference descType, string genericDescName, string typedDescName)
        {
            var typedDesc = new TinyObject(animationClip.Registry, descType);
            ((TinyObject)typedDesc[genericDescName]).CopyFrom(genericDesc);
            ((TinyList)animationClip[typedDescName]).Add(typedDesc);
        }
        #endregion
    }
}