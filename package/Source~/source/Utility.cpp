#include "Utility.h"
#include "arpresto_api.h"

const ArFrame* GetArFrame()
{
    ArFrame* frame;
    ArPresto_getFrame(&frame);
    return frame;
}

ArFrame* GetArFrameMutable()
{
    ArFrame* frame;
    ArPresto_getFrame(&frame);
    return frame;
}

const ArSession* GetArSession()
{
    return GetArSessionMutable();
}

ArSession* GetArSessionMutable()
{
    ArSession* session;
    ArPresto_getSession(&session);
    return session;
}

UnityXRTrackingState ConvertGoogleTrackingStateToUnity(ArTrackingState arTrackingState)
{
    switch (arTrackingState)
    {
    case AR_TRACKING_STATE_TRACKING:
        return kUnityXRTrackingStateTracking;

    case AR_TRACKING_STATE_PAUSED:
        return kUnityXRTrackingStateUnavailable;

    case AR_TRACKING_STATE_STOPPED:
        return kUnityXRTrackingStateUnavailable;

    default:
        return kUnityXRTrackingStateUnknown;
    }
}

const char* PrintArLightEstimateState(ArLightEstimateState state)
{
    switch (state)
    {
    case AR_LIGHT_ESTIMATE_STATE_NOT_VALID:
        return "AR_LIGHT_ESTIMATE_STATE_NOT_VALID";

    case AR_LIGHT_ESTIMATE_STATE_VALID:
        return "AR_LIGHT_ESTIMATE_STATE_VALID";

    default:
        return "Unknown ArLightEstimateState";
    }
}

const char* PrintArLightEstimationMode(ArLightEstimationMode mode)
{
    switch (mode)
    {
    case AR_LIGHT_ESTIMATION_MODE_DISABLED:
        return "AR_LIGHT_ESTIMATION_MODE_DISABLED";

    case AR_LIGHT_ESTIMATION_MODE_AMBIENT_INTENSITY:
        return "AR_LIGHT_ESTIMATION_MODE_AMBIENT_INTENSITY";

    default:
        return "Unknown ArLightEstimationMode";
    }
}

const char* PrintArPlaneFindingMode(ArPlaneFindingMode mode)
{
    switch (mode)
    {
    case AR_PLANE_FINDING_MODE_DISABLED:
        return "AR_PLANE_FINDING_MODE_DISABLED";

    case AR_PLANE_FINDING_MODE_HORIZONTAL:
        return "AR_PLANE_FINDING_MODE_HORIZONTAL";

    default:
        return "Unknown ArPlaneFindingMode";
    }
}

const char* PrintArPlaneType(ArPlaneType type)
{
    switch (type)
    {
    case AR_PLANE_HORIZONTAL_UPWARD_FACING:
        return "AR_PLANE_HORIZONTAL_UPWARD_FACING";

    case AR_PLANE_HORIZONTAL_DOWNWARD_FACING:
        return "AR_PLANE_HORIZONTAL_DOWNWARD_FACING";

    default:
        return "Unknown ArPlaneType";
    }
}

const char* PrintArStatus(ArStatus status)
{
    switch (status)
    {
    case AR_SUCCESS:
        return "AR_SUCCESS";

    case AR_ERROR_INVALID_ARGUMENT:
        return "AR_ERROR_INVALID_ARGUMENT";

    case AR_ERROR_FATAL:
        return "AR_ERROR_FATAL";

    case AR_ERROR_SESSION_PAUSED:
        return "AR_ERROR_SESSION_PAUSED";

    case AR_ERROR_SESSION_NOT_PAUSED:
        return "AR_ERROR_SESSION_NOT_PAUSED";

    case AR_ERROR_NOT_TRACKING:
        return "AR_ERROR_NOT_TRACKING";

    case AR_ERROR_TEXTURE_NOT_SET:
        return "AR_ERROR_TEXTURE_NOT_SET";

    case AR_ERROR_MISSING_GL_CONTEXT:
        return "AR_ERROR_MISSING_GL_CONTEXT";

    case AR_ERROR_UNSUPPORTED_CONFIGURATION:
        return "AR_ERROR_UNSUPPORTED_CONFIGURATION";

    case AR_ERROR_CAMERA_PERMISSION_NOT_GRANTED:
        return "AR_ERROR_CAMERA_PERMISSION_NOT_GRANTED";

    case AR_ERROR_DEADLINE_EXCEEDED:
        return "AR_ERROR_DEADLINE_EXCEEDED";

    case AR_ERROR_RESOURCE_EXHAUSTED:
        return "AR_ERROR_RESOURCE_EXHAUSTED";

    case AR_ERROR_NOT_YET_AVAILABLE:
        return "AR_ERROR_NOT_YET_AVAILABLE";

    case AR_UNAVAILABLE_ARCORE_NOT_INSTALLED:
        return "AR_UNAVAILABLE_ARCORE_NOT_INSTALLED";

    case AR_UNAVAILABLE_DEVICE_NOT_COMPATIBLE:
        return "AR_UNAVAILABLE_DEVICE_NOT_COMPATIBLE";

    case AR_UNAVAILABLE_APK_TOO_OLD:
        return "AR_UNAVAILABLE_APK_TOO_OLD";

    default:
        return "Unknown ArStatus";
    }
}

const char* PrintArTrackableType(ArTrackableType type)
{
    switch (type)
    {
    case AR_TRACKABLE_BASE_TRACKABLE:
        return "AR_TRACKABLE_BASE_TRACKABLE";

    case AR_TRACKABLE_PLANE:
        return "AR_TRACKABLE_PLANE";

    case AR_TRACKABLE_POINT:
        return "AR_TRACKABLE_POINT";

    case AR_TRACKABLE_NOT_VALID:
        return "AR_TRACKABLE_NOT_VALID";

    default:
        return "Unknown ArTrackableType";
    }
}

const char* PrintArTrackingState(ArTrackingState state)
{
    switch (state)
    {
    case AR_TRACKING_STATE_TRACKING:
        return "AR_TRACKING_STATE_TRACKING";

    case AR_TRACKING_STATE_PAUSED:
        return "AR_TRACKING_STATE_PAUSED";

    case AR_TRACKING_STATE_STOPPED:
        return "AR_TRACKING_STATE_STOPPED";

    default:
        return "Unknown ArTrackingState";
    }
}

const char* PrintArUpdateMode(ArUpdateMode mode)
{
    switch (mode)
    {
    case AR_UPDATE_MODE_BLOCKING:
        return "AR_UPDATE_MODE_BLOCKING";

    case AR_UPDATE_MODE_LATEST_CAMERA_IMAGE:
        return "AR_UPDATE_MODE_LATEST_CAMERA_IMAGE";

    default:
        return "Unknown ArUpdateMode";
    }
}
