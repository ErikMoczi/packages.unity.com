#pragma once

#include "arcore_c_api.h"

class WrappedFrame
{
public:
    WrappedFrame();
    WrappedFrame(const ArFrame* arFrame);

    operator const ArFrame*() const;
    const ArFrame* Get() const;

    int64_t GetTimestamp() const;
    bool DidDisplayGeometryChange() const;
    void TransformDisplayUvCoords(int32_t numCoords, const float* uvsToTransform, float* results) const;

protected:
    const ArFrame* m_ArFrame;
};
