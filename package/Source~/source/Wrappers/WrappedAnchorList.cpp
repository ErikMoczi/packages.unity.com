#include "Utility.h"
#include "WrappedAnchor.h"
#include "WrappedAnchorList.h"

template<>
void WrappingBase<ArAnchorList>::CreateOrAcquireDefaultImpl()
{
    ArAnchorList_create(GetArSession(), &m_Ptr);
}

template<>
void WrappingBase<ArAnchorList>::ReleaseImpl()
{
    ArAnchorList_destroy(m_Ptr);
}

WrappedAnchorList::WrappedAnchorList()
    : WrappingBase<ArAnchorList>()
{
}

WrappedAnchorList::WrappedAnchorList(eWrappedConstruction)
    : WrappingBase<ArAnchorList>()
{
    CreateOrAcquireDefault();
}

void WrappedAnchorList::CreateDefault()
{
    CreateOrAcquireDefault();
}

void WrappedAnchorList::GetAllAnchors()
{
    ArSession_getAllAnchors(GetArSession(), m_Ptr);
}

void WrappedAnchorList::AcquireAt(int32_t index, WrappedAnchor& anchor) const
{
    ArAnchorList_acquireItem(GetArSession(), m_Ptr, index, &anchor);
    anchor.InitRefCount();
}

int32_t WrappedAnchorList::Size() const
{
    int32_t ret = 0;
    ArAnchorList_getSize(GetArSession(), m_Ptr, &ret);
    return ret;
}
