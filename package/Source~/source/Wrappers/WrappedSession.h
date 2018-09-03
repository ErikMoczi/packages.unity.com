#pragma once
#include <jni.h>

#include "arcore_c_api.h"
#include "WrappingBase.h"

class WrappedSession : public WrappingBase<ArSession>
{
public:
    WrappedSession();
    ~WrappedSession();

    ArStatus TryCreate(JNIEnv* jniEnv, jobject applicationContext);

    bool TryConfigure(const ArConfig* config);
    bool UpdateAndOverwriteFrame();

    void SetCameraTextureName(uint32_t textureId);
    void SetDisplayGeometry(int rotation, int width, int height);

    bool ConnectOrResume();
    bool DisconnectOrPause();
    bool IsConnected() const;

    int64_t GetTimestamp() const;

private:
    bool m_IsConnected;
};

