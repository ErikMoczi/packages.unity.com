#pragma once

#include <android/log.h>
#include "arcore_c_api.h"
#include "Unity/UnityXRTrackable.h"
#include "Unity/UnityXRTypes.h"
#include <cstring>
#include <stdio.h>

#define EnumCast static_cast

class WrappedCamera;
const WrappedCamera& GetWrappedCamera();
WrappedCamera& GetWrappedCameraMutable();

ArFrame* GetArFrame();
ArSession* GetArSession();

class SessionProvider;
const SessionProvider& GetSessionProvider();
SessionProvider& GetSessionProviderMutable();

uint32_t GetCameraTextureName();

float GetScreenWidth();
float GetScreenHeight();

// From the docs: "The order of the values is: qx, qy, qz, qw, tx, ty, tz."
enum class eGooglePose
{
    RotationX,
    RotationY,
    RotationZ,
    RotationW,
    PositionX,
    PositionY,
    PositionZ,
    Count,

    RotationBegin = RotationX,
    RotationEnd = RotationW + 1,

    PositionBegin = PositionX,
    PositionEnd = PositionZ + 1,
};

#define kGooglePoseArraySize EnumCast<size_t>(eGooglePose::Count)

inline size_t ToIndex(eGooglePose key)
{
    return EnumCast<size_t>(key);
}

inline bool operator<(const UnityXRTrackableId& lhs, const UnityXRTrackableId& rhs)
{
    if (lhs.idPart[0] == rhs.idPart[0])
        return lhs.idPart[1] < rhs.idPart[1];
    return lhs.idPart[0] < rhs.idPart[0];
}

#define ARSTATUS_SUCCEEDED(ars) (ars >= AR_SUCCESS)
#define ARSTATUS_FAILED(ars) (ars < AR_SUCCESS)

#define kLogTag "Unity-ARCore"

#define DEBUG_LOG_FATAL(...) \
    __android_log_print(ANDROID_LOG_FATAL, kLogTag, __VA_ARGS__)

#define DEBUG_LOG_ERROR(...) \
    __android_log_print(ANDROID_LOG_ERROR, kLogTag, __VA_ARGS__)

#define DEBUG_LOG_WARNING(...) \
    __android_log_print(ANDROID_LOG_WARN, kLogTag, __VA_ARGS__)

#define DEBUG_LOG_VERBOSE(...) \
    __android_log_print(ANDROID_LOG_VERBOSE, kLogTag, __VA_ARGS__)

#define kIdPartIndexPointerInscription 0
#define kIdPartIndexOkayForMidUpdateScribble 1

template<typename T>
void ConvertToTrackableId(UnityXRTrackableId& outId, T* ptr)
{
    outId.idPart[kIdPartIndexPointerInscription] = reinterpret_cast<uintptr_t>(ptr);
    outId.idPart[kIdPartIndexOkayForMidUpdateScribble] = 0x600613A12A17C812;
}

template<typename T>
T* ConvertTrackableIdToPtr(const UnityXRTrackableId& id)
{
    return reinterpret_cast<T*>(id.idPart[kIdPartIndexPointerInscription]);
}

UnityXRTrackingState ConvertGoogleTrackingStateToUnity(ArTrackingState arTrackingState);

const char* PrintArLightEstimateState(ArLightEstimateState state);
const char* PrintArLightEstimationMode(ArLightEstimationMode mode);
const char* PrintArPlaneFindingMode(ArPlaneFindingMode mode);
const char* PrintArPlaneType(ArPlaneType type);
const char* PrintArStatus(ArStatus status);
const char* PrintArTrackableType(ArTrackableType type);
const char* PrintArTrackingState(ArTrackingState state);
const char* PrintArUpdateMode(ArUpdateMode mode);
