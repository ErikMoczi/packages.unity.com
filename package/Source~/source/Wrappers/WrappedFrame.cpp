#include "Utility.h"
#include "WrappedFrame.h"

WrappedFrame::WrappedFrame()
    : m_ArFrame(nullptr)
{
}

WrappedFrame::WrappedFrame(const ArFrame* arFrame)
    : m_ArFrame(arFrame)
{
}

WrappedFrame::operator const ArFrame*() const
{
    return m_ArFrame;
}

const ArFrame* WrappedFrame::Get() const
{
    return m_ArFrame;
}

int64_t WrappedFrame::GetTimestamp() const
{
    int64_t ret = 0;
    ArFrame_getTimestamp(GetArSession(), m_ArFrame, &ret);
    return ret;
}

bool WrappedFrame::DidDisplayGeometryChange() const
{
    int32_t didGeometryChange = 0;
    ArFrame_getDisplayGeometryChanged(GetArSession(), m_ArFrame, &didGeometryChange);
    return didGeometryChange != 0;
}

void WrappedFrame::TransformDisplayUvCoords(int32_t numCoords, const float* uvsToTransform, float* results) const
{
    ArFrame_transformDisplayUvCoords(GetArSession(), m_ArFrame, numCoords, uvsToTransform, results);
}
