#include "Utility.h"
#include "WrappedHitResult.h"
#include "WrappedHitResultList.h"

template<>
void WrappingBase<ArHitResultList>::CreateOrAcquireDefaultImpl()
{
    ArHitResultList_create(GetArSession(), &m_Ptr);
}

template<>
void WrappingBase<ArHitResultList>::ReleaseImpl()
{
    ArHitResultList_destroy(m_Ptr);
}

WrappedHitResultList::WrappedHitResultList()
    : WrappingBase<ArHitResultList>()
{
}

WrappedHitResultList::WrappedHitResultList(eWrappedConstruction)
    : WrappingBase<ArHitResultList>()
{
    CreateOrAcquireDefault();
}

void WrappedHitResultList::CreateDefault()
{
    CreateOrAcquireDefault();
}

void WrappedHitResultList::HitTest(float xPixel, float yPixel)
{
    ArFrame_hitTest(GetArSession(), GetArFrame(), xPixel, yPixel, m_Ptr);
}

int32_t WrappedHitResultList::Size() const
{
    int32_t ret = 0;
    ArHitResultList_getSize(GetArSession(), m_Ptr, &ret);
    return ret;
}

void WrappedHitResultList::GetHitResultAt(int32_t index, WrappedHitResult& hitResult)
{
    ArHitResultList_getItem(GetArSession(), m_Ptr, index, hitResult);
}
