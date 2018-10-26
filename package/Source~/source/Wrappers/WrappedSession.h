#pragma once

#include "arcore_c_api.h"
#include "Unity/IUnityXRCamera.h"

class WrappedSession
{
public:
    WrappedSession();
    WrappedSession(const ArSession* arSession);

    operator const ArSession*() const;
    const ArSession* Get() const;

protected:
    const ArSession* m_ArSession;
};

class WrappedSessionMutable : public WrappedSession
{
public:
    WrappedSessionMutable();
    WrappedSessionMutable(ArSession* arSession);

    operator ArSession*();
    ArSession* Get();

    bool TrySetDisplayGeometry(UnityXRScreenOrientation xrOrientation, float screenWidth, float screenHeight);

protected:
    ArSession*& GetArSessionMutable();
};
