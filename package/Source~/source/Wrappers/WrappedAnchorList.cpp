#include "Utility.h"
#include "WrappedAnchor.h"
#include "WrappedAnchorList.h"

WrappedAnchorList::WrappedAnchorList()
    : m_ArAnchorList(nullptr)
{
}

WrappedAnchorList::WrappedAnchorList(const ArAnchorList* arAnchorList)
    : m_ArAnchorList(arAnchorList)
{
}

WrappedAnchorList::operator const ArAnchorList*()
{
    return m_ArAnchorList;
}

const ArAnchorList* WrappedAnchorList::Get() const
{
    return m_ArAnchorList;
}

int32_t WrappedAnchorList::Size() const
{
    int32_t ret = 0;
    ArAnchorList_getSize(GetArSession(), m_ArAnchorList, &ret);
    return ret;
}

WrappedAnchorListMutable::WrappedAnchorListMutable()
{
}

WrappedAnchorListMutable::WrappedAnchorListMutable(ArAnchorList* arAnchorList)
    : WrappedAnchorList(arAnchorList)
{
}

WrappedAnchorListMutable::operator ArAnchorList*()
{
    return GetArAnchorListMutable();
}

ArAnchorList* WrappedAnchorListMutable::Get()
{
    return GetArAnchorListMutable();
}

void WrappedAnchorListMutable::PopulateList()
{
    ArSession_getAllAnchors(GetArSession(), GetArAnchorListMutable());
}

ArAnchorList*& WrappedAnchorListMutable::GetArAnchorListMutable()
{
    return *const_cast<ArAnchorList**>(&m_ArAnchorList);
}

WrappedAnchorListRaii::WrappedAnchorListRaii()
{
    ArAnchorList_create(GetArSession(), &GetArAnchorListMutable());
}

WrappedAnchorListRaii::~WrappedAnchorListRaii()
{
    if (m_ArAnchorList != nullptr)
        ArAnchorList_destroy(GetArAnchorListMutable());
}
