#include "Utility.h"
#include "WrappedHitResult.h"
#include "WrappedHitResultList.h"

WrappedHitResultList::WrappedHitResultList()
    : m_ArHitResultList(nullptr)
{
}

WrappedHitResultList::WrappedHitResultList(const ArHitResultList* arHitResultList)
    : m_ArHitResultList(arHitResultList)
{
}

WrappedHitResultList::operator const ArHitResultList*() const
{
    return m_ArHitResultList;
}

const ArHitResultList* WrappedHitResultList::Get() const
{
    return m_ArHitResultList;
}

int32_t WrappedHitResultList::Size() const
{
    int32_t ret = 0;
    ArHitResultList_getSize(GetArSession(), m_ArHitResultList, &ret);
    return ret;
}

WrappedHitResultListMutable::WrappedHitResultListMutable()
{
}

WrappedHitResultListMutable::WrappedHitResultListMutable(ArHitResultList* arHitResultList)
    : WrappedHitResultList(arHitResultList)
{
}

WrappedHitResultListMutable::operator ArHitResultList*()
{
    return GetArHitResultListMutable();
}

ArHitResultList* WrappedHitResultListMutable::Get()
{
    return GetArHitResultListMutable();
}

void WrappedHitResultListMutable::HitTest(float xPixel, float yPixel)
{
    ArFrame_hitTest(GetArSession(), GetArFrame(), xPixel, yPixel, GetArHitResultListMutable());
}

ArHitResultList*& WrappedHitResultListMutable::GetArHitResultListMutable()
{
    return *const_cast<ArHitResultList**>(&m_ArHitResultList);
}

WrappedHitResultListRaii::WrappedHitResultListRaii()
{
    ArHitResultList_create(GetArSession(), &GetArHitResultListMutable());
}

WrappedHitResultListRaii::~WrappedHitResultListRaii()
{
    if (m_ArHitResultList != nullptr)
        ArHitResultList_destroy(GetArHitResultListMutable());
}
