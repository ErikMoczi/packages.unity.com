#include "WrappedSession.h"

const int kInvalidOrientation = -1;
static int ConvertUnityToGoogleOrientation(UnityXRScreenOrientation unityOrientation)
{
    switch (unityOrientation)
    {
    case kUnityXRScreenOrientationPortrait:
        return 0; // ROTATION_0

    case kUnityXRScreenOrientationPortraitUpsideDown:
        return 2; // ROTATION_180

    case kUnityXRScreenOrientationLandscapeLeft:
        return 1; // ROTATION_90

    case kUnityXRScreenOrientationLandscapeRight:
        return 3; // ROTATION_270

    default:
        return kInvalidOrientation;
    }
}

WrappedSession::WrappedSession()
    : m_ArSession(nullptr)
{
}

WrappedSession::WrappedSession(const ArSession* arSession)
    : m_ArSession(arSession)
{
}

WrappedSession::operator const ArSession*() const
{
    return m_ArSession;
}

const ArSession* WrappedSession::Get() const
{
    return m_ArSession;
}

WrappedSessionMutable::WrappedSessionMutable()
{
}

WrappedSessionMutable::WrappedSessionMutable(ArSession* arSession)
    : WrappedSession(arSession)
{
}

WrappedSessionMutable::operator ArSession*()
{
    return GetArSessionMutable();
}

ArSession* WrappedSessionMutable::Get()
{
    return GetArSessionMutable();
}

bool WrappedSessionMutable::TrySetDisplayGeometry(UnityXRScreenOrientation xrOrientation, float screenWidth, float screenHeight)
{
    int googleOrientation = ConvertUnityToGoogleOrientation(xrOrientation);
    if (kInvalidOrientation == googleOrientation)
        return false;

    ArSession_setDisplayGeometry(GetArSessionMutable(), googleOrientation, screenWidth, screenHeight);
    return true;
}

ArSession*& WrappedSessionMutable::GetArSessionMutable()
{
    return *const_cast<ArSession**>(&m_ArSession);
}
