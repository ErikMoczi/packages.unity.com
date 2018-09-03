#pragma once

#include "DepthProvider.h"
#include "PlaneProvider.h"
#include "RaycastProvider.h"
#include "ReferencePointProvider.h"
#include "SessionProvider.h"

class LifecycleProviderEnvironment : public IXRLifecycleProvider
{
public:
    LifecycleProviderEnvironment();
    ~LifecycleProviderEnvironment();

    virtual XRErrorCode UNITY_INTERFACE_API Initialize(IXRInstance* xrInstance) override;
    virtual void UNITY_INTERFACE_API Shutdown(IXRInstance* xrInstance) override;

    virtual XRErrorCode UNITY_INTERFACE_API Start(IXRInstance* xrInstance) override;
    virtual void UNITY_INTERFACE_API Stop(IXRInstance* xrInstance) override;

    const SessionProvider& GetSessionProvider() const;
    SessionProvider& GetSessionProviderMutable();

private:
    void ShutdownImpl();

    DepthProvider m_DepthProvider;
    PlaneProvider m_PlaneProvider;
    RaycastProvider m_RaycastProvider;
    ReferencePointProvider m_ReferencePointProvider;
    SessionProvider m_SessionProvider;

    bool m_Initialized;
};
