#include "MathConversion.h"

#include <cstring>

void MathConversion::ToUnity(UnityXRVector3& xrPosition, const mathfu::Vector<float, 3>& fuPosition)
{
    xrPosition.x = fuPosition.data_[0];
    xrPosition.y = fuPosition.data_[1];
    xrPosition.z = fuPosition.data_[2];
}

void MathConversion::ToUnity(UnityXRVector4& xrRotation, const mathfu::Quaternion<float>& fuRotation)
{
    xrRotation.x = fuRotation.vector().data_[0];
    xrRotation.y = fuRotation.vector().data_[1];
    xrRotation.z = fuRotation.vector().data_[2];
    xrRotation.w = fuRotation.scalar();
}

void MathConversion::ToUnity(UnityXRMatrix4x4& xrMatrix, const mathfu::Matrix<float, 4, 4>& fuMatrix)
{
    std::memcpy(&xrMatrix, &fuMatrix, sizeof(UnityXRMatrix4x4));
}

void MathConversion::ToMathFu(mathfu::Vector<float, 3>& fuPosition, const UnityXRVector3& xrPosition)
{
    fuPosition.data_[0] = xrPosition.x;
    fuPosition.data_[1] = xrPosition.y;
    fuPosition.data_[2] = xrPosition.z;
}

void MathConversion::ToMathFu(mathfu::Quaternion<float>& fuRotation, const UnityXRVector4& xrRotation)
{
    fuRotation.vector().data_[0] = xrRotation.x;
    fuRotation.vector().data_[1] = xrRotation.y;
    fuRotation.vector().data_[2] = xrRotation.z;
    fuRotation.scalar() = xrRotation.w;
}

void MathConversion::ToMathFu(mathfu::Matrix<float, 4, 4>& fuMatrix, const UnityXRMatrix4x4& xrMatrix)
{
    std::memcpy(&fuMatrix, &xrMatrix, sizeof(UnityXRMatrix4x4));
}

void MathConversion::ToUnity(UnityXRVector3& xrPosition, const float* googlePositionRaw)
{
    xrPosition.x = googlePositionRaw[ToIndex(eGooglePose::PositionX) - ToIndex(eGooglePose::PositionBegin)];
    xrPosition.y = googlePositionRaw[ToIndex(eGooglePose::PositionY) - ToIndex(eGooglePose::PositionBegin)];
    xrPosition.z = -googlePositionRaw[ToIndex(eGooglePose::PositionZ) - ToIndex(eGooglePose::PositionBegin)];
}

void MathConversion::ToUnity(UnityXRVector4& xrRotation, const float* googleRotationRaw)
{
    xrRotation.x = -googleRotationRaw[ToIndex(eGooglePose::RotationX) - ToIndex(eGooglePose::RotationBegin)];
    xrRotation.y = -googleRotationRaw[ToIndex(eGooglePose::RotationY) - ToIndex(eGooglePose::RotationBegin)];
    xrRotation.z = googleRotationRaw[ToIndex(eGooglePose::RotationZ) - ToIndex(eGooglePose::RotationBegin)];
    xrRotation.w = googleRotationRaw[ToIndex(eGooglePose::RotationW) - ToIndex(eGooglePose::RotationBegin)];
}

void MathConversion::ToUnity(UnityXRVector3& xrPosition, UnityXRVector4& xrRotation, const float* googlePoseRaw)
{
    ToUnity(xrPosition, googlePoseRaw + ToIndex(eGooglePose::PositionBegin));
    ToUnity(xrRotation, googlePoseRaw + ToIndex(eGooglePose::RotationBegin));
}

void MathConversion::ToUnity(UnityXRPose& xrPose, const float* googlePoseRaw)
{
    ToUnity(xrPose.position, xrPose.rotation, googlePoseRaw);
}

void MathConversion::ToUnity(UnityXRMatrix4x4& xrMatrix, const float* rawMatrixGoogle)
{
    std::memcpy(&xrMatrix, rawMatrixGoogle, sizeof(UnityXRMatrix4x4));
    float* xrMatrixAsFloatArray = reinterpret_cast<float*>(&xrMatrix);
    xrMatrixAsFloatArray[ 2] *= -1.0f;
    xrMatrixAsFloatArray[ 6] *= -1.0f;
    xrMatrixAsFloatArray[ 8] *= -1.0f;
    xrMatrixAsFloatArray[ 9] *= -1.0f;
    xrMatrixAsFloatArray[11] *= -1.0f;
    xrMatrixAsFloatArray[14] *= -1.0f;
}

void MathConversion::ToUnityProjectionMatrix(UnityXRMatrix4x4& xrProjectionMatrix, const float* googleProjectionMatrixRaw)
{
    std::memcpy(&xrProjectionMatrix, googleProjectionMatrixRaw, sizeof(UnityXRMatrix4x4));
}

void MathConversion::ToGoogle(float* googlePositionRaw, const UnityXRVector3& xrPosition)
{
    googlePositionRaw[ToIndex(eGooglePose::PositionX) - ToIndex(eGooglePose::PositionBegin)] = xrPosition.x;
    googlePositionRaw[ToIndex(eGooglePose::PositionY) - ToIndex(eGooglePose::PositionBegin)] = xrPosition.y;
    googlePositionRaw[ToIndex(eGooglePose::PositionZ) - ToIndex(eGooglePose::PositionBegin)] = -xrPosition.z;
}

void MathConversion::ToGoogle(float* googleRotationRaw, const UnityXRVector4& xrRotation)
{
    googleRotationRaw[ToIndex(eGooglePose::RotationX) - ToIndex(eGooglePose::RotationBegin)] = -xrRotation.x;
    googleRotationRaw[ToIndex(eGooglePose::RotationY) - ToIndex(eGooglePose::RotationBegin)] = -xrRotation.y;
    googleRotationRaw[ToIndex(eGooglePose::RotationZ) - ToIndex(eGooglePose::RotationBegin)] = xrRotation.z;
    googleRotationRaw[ToIndex(eGooglePose::RotationW) - ToIndex(eGooglePose::RotationBegin)] = xrRotation.w;
}

void MathConversion::ToGoogle(float* googlePoseRaw, const UnityXRVector3& xrPosition, const UnityXRVector4& xrRotation)
{
    ToGoogle(googlePoseRaw + ToIndex(eGooglePose::PositionBegin), xrPosition);
    ToGoogle(googlePoseRaw + ToIndex(eGooglePose::RotationBegin), xrRotation);
}

void MathConversion::ToGoogle(float* googlePoseRaw, const UnityXRPose& xrPose)
{
    ToGoogle(googlePoseRaw, xrPose.position, xrPose.rotation);
}

void MathConversion::ToGoogle(float* googleMatrixRaw, const UnityXRMatrix4x4& xrMatrix)
{
    std::memcpy(googleMatrixRaw, &xrMatrix, sizeof(UnityXRMatrix4x4));
    for (int rowAndColumnIndex = 0; rowAndColumnIndex < 4;++rowAndColumnIndex)
    {
        googleMatrixRaw[2 + 4 * rowAndColumnIndex] *= -1.0f;
        googleMatrixRaw[8 + rowAndColumnIndex] *= -1.0f;
    }
}

void MathConversion::ToMathFu(mathfu::Vector<float, 3>& fuPosition, const float* googlePositionRaw)
{
    UnityXRVector3 xrPosition;
    ToUnity(xrPosition, googlePositionRaw);
    ToMathFu(fuPosition, xrPosition);
}

void MathConversion::ToMathFu(mathfu::Quaternion<float>& fuRotation, const float* googleRotationRaw)
{
    UnityXRVector4 xrRotation;
    ToUnity(xrRotation, googleRotationRaw);
    ToMathFu(fuRotation, xrRotation);
}

void MathConversion::ToMathFu(mathfu::Matrix<float, 4, 4>& fuMatrix, const float* googleMatrixRaw)
{
    UnityXRMatrix4x4 xrMatrix;
    ToUnity(xrMatrix, googleMatrixRaw);
    ToMathFu(fuMatrix, xrMatrix);
}

void MathConversion::ToGoogle(float* googlePositionRaw, const mathfu::Vector<float, 3>& fuPosition)
{
    UnityXRVector3 xrPosition;
    ToUnity(xrPosition, fuPosition);
    ToGoogle(googlePositionRaw, xrPosition);
}

void MathConversion::ToGoogle(float* googleRotationRaw, const mathfu::Quaternion<float>& fuRotation)
{
    UnityXRVector4 xrRotation;
    ToUnity(xrRotation, fuRotation);
    ToGoogle(googleRotationRaw, xrRotation);
}

void MathConversion::ToGoogle(float* googleMatrixRaw, const mathfu::Matrix<float, 4, 4>& fuMatrix)
{
    UnityXRMatrix4x4 xrMatrix;
    ToUnity(xrMatrix, fuMatrix);
    ToGoogle(googleMatrixRaw, xrMatrix);
}
