#pragma once

#include "arcore_c_api.h"
#include "mathfu/matrix.h"
#include "mathfu/quaternion.h"
#include "mathfu/vector.h"
#include "Unity/UnityXRTypes.h"
#include "Utility.h"

class MathConversion
{
public:
    // simple type conversion, no coordinate system conversion
    static void ToUnity(UnityXRVector3& positionUnity, const mathfu::Vector<float, 3>& positionFu);
    static void ToUnity(UnityXRVector4& rotationUnity, const mathfu::Quaternion<float>& rotationFu);
    static void ToUnity(UnityXRMatrix4x4& matrixUnity, const mathfu::Matrix<float, 4, 4>& matrixFu);

    // simple type conversion, no coordinate system conversion
    static void ToMathFu(mathfu::Vector<float, 3>& positionFu, const UnityXRVector3& positionUnity);
    static void ToMathFu(mathfu::Quaternion<float>& rotationFu, const UnityXRVector4& rotationUnity);
    static void ToMathFu(mathfu::Matrix<float, 4, 4>& matrixFu, const UnityXRMatrix4x4& matrixUnity);

    // converts between coordinate systems
    static void ToUnity(UnityXRVector3& positionUnity, const float* rawPositionGoogle);
    static void ToUnity(UnityXRVector4& rotationUnity, const float* rawRotationGoogle);
    static void ToUnity(UnityXRVector3& positionUnity, UnityXRVector4& rotationUnity, const float* rawPoseGoogle);
    static void ToUnity(UnityXRPose& poseUnity, const float* rawPoseGoogle);
    static void ToUnity(UnityXRMatrix4x4& matrixUnity, const float* rawMatrixGoogle);
    static void ToUnityProjectionMatrix(UnityXRMatrix4x4& projectionMatrixUnity, const float* rawProjectionMatrixGoogle);

    // converts between coordinate systems
    static void ToGoogle(float* rawPositionGoogle, const UnityXRVector3& positionUnity);
    static void ToGoogle(float* rawRotationGoogle, const UnityXRVector4& rotationUnity);
    static void ToGoogle(float* rawPoseGoogle, const UnityXRVector3& positionUnity, const UnityXRVector4& rotationUnity);
    static void ToGoogle(float* rawPoseGoogle, const UnityXRPose& poseUnity);
    static void ToGoogle(float* rawMatrixGoogle, const UnityXRMatrix4x4& matrixUnity);

    // shortcut methods using Unity types as an in-between
    static void ToMathFu(mathfu::Vector<float, 3>& positionFu, const float* rawPositionGoogle);
    static void ToMathFu(mathfu::Quaternion<float>& rotationFu, const float* rawRotationGoogle);
    static void ToMathFu(mathfu::Matrix<float, 4, 4>& matrixFu, const float* rawMatrixGoogle);

    // shortcut methods using Unity types as an in-between
    static void ToGoogle(float* rawPositionGoogle, const mathfu::Vector<float, 3>& positionFu);
    static void ToGoogle(float* rawRotationGoogle, const mathfu::Quaternion<float>& rotationFu);
    static void ToGoogle(float* rawMatrixGoogle, const mathfu::Matrix<float, 4, 4>& matrixFu);
};
