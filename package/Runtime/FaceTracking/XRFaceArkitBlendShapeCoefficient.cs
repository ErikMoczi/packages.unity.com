using System;
using System.Runtime.InteropServices;

namespace UnityEngine.XR.ARKit
{
    /// <summary>
    /// Enum values that represent face action units that affect the expression on the face
    /// </summary>
    public enum XRArkitBlendShapeLocation
    {
        BrowDownLeft        ,
        BrowDownRight       ,
        BrowInnerUp         ,
        BrowOuterUpLeft     ,
        BrowOuterUpRight    ,
        CheekPuff           ,
        CheekSquintLeft     ,
        CheekSquintRight    ,
        EyeBlinkLeft        ,
        EyeBlinkRight       ,
        EyeLookDownLeft     ,
        EyeLookDownRight    ,
        EyeLookInLeft       ,
        EyeLookInRight      ,
        EyeLookOutLeft      ,
        EyeLookOutRight     ,
        EyeLookUpLeft       ,
        EyeLookUpRight      ,
        EyeSquintLeft       ,
        EyeSquintRight      ,
        EyeWideLeft         ,
        EyeWideRight        ,
        JawForward          ,
        JawLeft             ,
        JawOpen             ,
        JawRight            ,
        MouthClose          ,
        MouthDimpleLeft     ,
        MouthDimpleRight    ,
        MouthFrownLeft      ,
        MouthFrownRight     ,
        MouthFunnel         ,
        MouthLeft           ,
        MouthLowerDownLeft  ,
        MouthLowerDownRight ,
        MouthPressLeft      ,
        MouthPressRight     ,
        MouthPucker         ,
        MouthRight          ,
        MouthRollLower      ,
        MouthRollUpper      ,
        MouthShrugLower     ,
        MouthShrugUpper     ,
        MouthSmileLeft      ,
        MouthSmileRight     ,
        MouthStretchLeft    ,
        MouthStretchRight   ,
        MouthUpperUpLeft    ,
        MouthUpperUpRight   ,
        NoseSneerLeft       ,
        NoseSneerRight      ,
        TongueOut
    }

    /// <summary>
    /// An entry that specifies how much of a specific <see cref="XRArkitBlendShapeLocation"/> is present in the current expression on the face.
    /// </summary>
    /// <remarks>
    /// You get a list of these for every expression a face makes.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct XRFaceArkitBlendShapeCoefficient : IEquatable<XRFaceArkitBlendShapeCoefficient>
    {
        // Fields to marshall/serialize from native code
        XRArkitBlendShapeLocation m_ArkitBlendShapeLocation;
        float m_Coefficient;

        /// <summary>
        /// The specific <see cref="XRArkitBlendShapeLocation"/> being examined.
        /// </summary>
        public XRArkitBlendShapeLocation arkitBlendShapeLocation
        {
            get { return m_ArkitBlendShapeLocation; }
        }

        /// <summary>
        /// A value from 0.0 to 1.0 that specifies how active the associated <see cref="XRArkitBlendShapeLocation"/> is in this expression.
        /// </summary>
        public float coefficient
        {
            get { return m_Coefficient; }
        }

        public bool Equals(XRFaceArkitBlendShapeCoefficient other)
        {
            return arkitBlendShapeLocation == other.arkitBlendShapeLocation && coefficient.Equals(other.coefficient);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is XRFaceArkitBlendShapeCoefficient && Equals((XRFaceArkitBlendShapeCoefficient)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)arkitBlendShapeLocation * 486187739) + coefficient.GetHashCode();
            }
        }

        public static bool operator==(XRFaceArkitBlendShapeCoefficient left, XRFaceArkitBlendShapeCoefficient right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(XRFaceArkitBlendShapeCoefficient left, XRFaceArkitBlendShapeCoefficient right)
        {
            return !left.Equals(right);
        }
    }
}
