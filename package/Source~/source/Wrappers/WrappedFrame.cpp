#include "Utility.h"
#include "WrappedFrame.h"

template<>
void WrappingBase<ArFrame>::CreateOrAcquireDefaultImpl()
{
    ArFrame_create(GetArSession(), &m_Ptr);
}

template<>
void WrappingBase<ArFrame>::ReleaseImpl()
{
    ArFrame_destroy(m_Ptr);
}

void WrappedFrame::CreateDefault()
{
    CreateOrAcquireDefault();
}

bool WrappedFrame::DidDisplayGeometryChange() const
{
    int32_t didGeometryChange = 0;
    ArFrame_getDisplayGeometryChanged(GetArSession(), m_Ptr, &didGeometryChange);
    return didGeometryChange != 0;
}
