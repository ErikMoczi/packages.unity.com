#include "MathConversion.h"

#include <cstring>

void MathConversion::ToUnity(UnityXRVector3& positionUnity, const mathfu::Vector<float, 3>& positionFu)
{
    positionUnity.x = positionFu.data_[0];
    positionUnity.y = positionFu.data_[1];
    positionUnity.z = positionFu.data_[2];
}

void MathConversion::ToUnity(UnityXRVector4& rotationUnity, const mathfu::Quaternion<float>& rotationFu)
{
    rotationUnity.x = rotationFu.vector().data_[0];
    rotationUnity.y = rotationFu.vector().data_[1];
    rotationUnity.z = rotationFu.vector().data_[2];
    rotationUnity.w = rotationFu.scalar();
}

void MathConversion::ToUnity(UnityXRMatrix4x4& matrixUnity, const mathfu::Matrix<float, 4, 4>& matrixFu)
{
    std::memcpy(&matrixUnity, &matrixFu, sizeof(UnityXRMatrix4x4));
}

void MathConversion::ToMathFu(mathfu::Vector<float, 3>& positionFu, const UnityXRVector3& positionUnity)
{
    positionFu.data_[0] = positionUnity.x;
    positionFu.data_[1] = positionUnity.y;
    positionFu.data_[2] = positionUnity.z;
}

void MathConversion::ToMathFu(mathfu::Quaternion<float>& rotationFu, const UnityXRVector4& rotationUnity)
{
    rotationFu.vector().data_[0] = rotationUnity.x;
    rotationFu.vector().data_[1] = rotationUnity.y;
    rotationFu.vector().data_[2] = rotationUnity.z;
    rotationFu.scalar() = rotationUnity.w;
}

void MathConversion::ToMathFu(mathfu::Matrix<float, 4, 4>& matrixFu, const UnityXRMatrix4x4& matrixUnity)
{
    std::memcpy(&matrixFu, &matrixUnity, sizeof(UnityXRMatrix4x4));
}

void MathConversion::ToUnity(UnityXRVector3& positionUnity, const float* rawPositionGoogle)
{
    positionUnity.x = rawPositionGoogle[ToIndex(eGooglePose::PositionX) - ToIndex(eGooglePose::PositionBegin)];
    positionUnity.y = rawPositionGoogle[ToIndex(eGooglePose::PositionY) - ToIndex(eGooglePose::PositionBegin)];
    positionUnity.z = -rawPositionGoogle[ToIndex(eGooglePose::PositionZ) - ToIndex(eGooglePose::PositionBegin)];
}

void MathConversion::ToUnity(UnityXRVector4& rotationUnity, const float* rawRotationGoogle)
{
    rotationUnity.x = -rawRotationGoogle[ToIndex(eGooglePose::RotationX) - ToIndex(eGooglePose::RotationBegin)];
    rotationUnity.y = -rawRotationGoogle[ToIndex(eGooglePose::RotationY) - ToIndex(eGooglePose::RotationBegin)];
    rotationUnity.z = rawRotationGoogle[ToIndex(eGooglePose::RotationZ) - ToIndex(eGooglePose::RotationBegin)];
    rotationUnity.w = rawRotationGoogle[ToIndex(eGooglePose::RotationW) - ToIndex(eGooglePose::RotationBegin)];
}

void MathConversion::ToUnity(UnityXRVector3& positionUnity, UnityXRVector4& rotationUnity, const float* rawPoseGoogle)
{
    ToUnity(positionUnity, rawPoseGoogle + ToIndex(eGooglePose::PositionBegin));
    ToUnity(rotationUnity, rawPoseGoogle + ToIndex(eGooglePose::RotationBegin));
}

void MathConversion::ToUnity(UnityXRPose& poseUnity, const float* rawPoseGoogle)
{
    ToUnity(poseUnity.position, poseUnity.rotation, rawPoseGoogle);
}

void MathConversion::ToUnity(UnityXRMatrix4x4& matrixUnity, const float* rawMatrixGoogle)
{
    std::memcpy(&matrixUnity, rawMatrixGoogle, sizeof(UnityXRMatrix4x4));
    float* matrixUnityAsFloatArray = reinterpret_cast<float*>(&matrixUnity);
    matrixUnityAsFloatArray[ 2] *= -1.0f;
    matrixUnityAsFloatArray[ 6] *= -1.0f;
    matrixUnityAsFloatArray[ 8] *= -1.0f;
    matrixUnityAsFloatArray[ 9] *= -1.0f;
    matrixUnityAsFloatArray[11] *= -1.0f;
    matrixUnityAsFloatArray[14] *= -1.0f;
}

void MathConversion::ToUnityProjectionMatrix(UnityXRMatrix4x4& projectionMatrixUnity, const float* rawProjectionMatrixGoogle)
{
    std::memcpy(&projectionMatrixUnity, rawProjectionMatrixGoogle, sizeof(UnityXRMatrix4x4));
}

void MathConversion::ToGoogle(float* rawPositionGoogle, const UnityXRVector3& positionUnity)
{
    rawPositionGoogle[ToIndex(eGooglePose::PositionX) - ToIndex(eGooglePose::PositionBegin)] = positionUnity.x;
    rawPositionGoogle[ToIndex(eGooglePose::PositionY) - ToIndex(eGooglePose::PositionBegin)] = positionUnity.y;
    rawPositionGoogle[ToIndex(eGooglePose::PositionZ) - ToIndex(eGooglePose::PositionBegin)] = -positionUnity.z;
}

void MathConversion::ToGoogle(float* rawRotationGoogle, const UnityXRVector4& rotationUnity)
{
    rawRotationGoogle[ToIndex(eGooglePose::RotationX) - ToIndex(eGooglePose::RotationBegin)] = -rotationUnity.x;
    rawRotationGoogle[ToIndex(eGooglePose::RotationY) - ToIndex(eGooglePose::RotationBegin)] = -rotationUnity.y;
    rawRotationGoogle[ToIndex(eGooglePose::RotationZ) - ToIndex(eGooglePose::RotationBegin)] = rotationUnity.z;
    rawRotationGoogle[ToIndex(eGooglePose::RotationW) - ToIndex(eGooglePose::RotationBegin)] = rotationUnity.w;
}

void MathConversion::ToGoogle(float* rawPoseGoogle, const UnityXRVector3& positionUnity, const UnityXRVector4& rotationUnity)
{
    ToGoogle(rawPoseGoogle + ToIndex(eGooglePose::PositionBegin), positionUnity);
    ToGoogle(rawPoseGoogle + ToIndex(eGooglePose::RotationBegin), rotationUnity);
}

void MathConversion::ToGoogle(float* rawPoseGoogle, const UnityXRPose& poseUnity)
{
    ToGoogle(rawPoseGoogle, poseUnity.position, poseUnity.rotation);
}

void MathConversion::ToGoogle(float* rawMatrixGoogle, const UnityXRMatrix4x4& matrixUnity)
{
    std::memcpy(rawMatrixGoogle, &matrixUnity, sizeof(UnityXRMatrix4x4));
    for (int rowAndColumnIndex = 0; rowAndColumnIndex < 4;++rowAndColumnIndex)
    {
        rawMatrixGoogle[2 + 4 * rowAndColumnIndex] *= -1.0f;
        rawMatrixGoogle[8 + rowAndColumnIndex] *= -1.0f;
    }
}

void MathConversion::ToMathFu(mathfu::Vector<float, 3>& positionFu, const float* rawPositionGoogle)
{
    UnityXRVector3 positionUnity;
    ToUnity(positionUnity, rawPositionGoogle);
    ToMathFu(positionFu, positionUnity);
}

void MathConversion::ToMathFu(mathfu::Quaternion<float>& rotationFu, const float* rawRotationGoogle)
{
    UnityXRVector4 rotationUnity;
    ToUnity(rotationUnity, rawRotationGoogle);
    ToMathFu(rotationFu, rotationUnity);
}

void MathConversion::ToMathFu(mathfu::Matrix<float, 4, 4>& matrixFu, const float* rawMatrixGoogle)
{
    UnityXRMatrix4x4 matrixUnity;
    ToUnity(matrixUnity, rawMatrixGoogle);
    ToMathFu(matrixFu, matrixUnity);
}

void MathConversion::ToGoogle(float* rawPositionGoogle, const mathfu::Vector<float, 3>& positionFu)
{
    UnityXRVector3 positionUnity;
    ToUnity(positionUnity, positionFu);
    ToGoogle(rawPositionGoogle, positionUnity);
}

void MathConversion::ToGoogle(float* rawRotationGoogle, const mathfu::Quaternion<float>& rotationFu)
{
    UnityXRVector4 rotationUnity;
    ToUnity(rotationUnity, rotationFu);
    ToGoogle(rawRotationGoogle, rotationUnity);
}

void MathConversion::ToGoogle(float* rawMatrixGoogle, const mathfu::Matrix<float, 4, 4>& matrixFu)
{
    UnityXRMatrix4x4 matrixUnity;
    ToUnity(matrixUnity, matrixFu);
    ToGoogle(rawMatrixGoogle, matrixUnity);
}
