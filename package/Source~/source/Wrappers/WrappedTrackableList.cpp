#include "Utility.h"
#include "WrappedTrackableList.h"

WrappedTrackableList::WrappedTrackableList()
    : m_ArTrackableList(nullptr)
{
}

WrappedTrackableList::WrappedTrackableList(const ArTrackableList* arTrackableList)
    : m_ArTrackableList(arTrackableList)
{
}

WrappedTrackableList::operator const ArTrackableList*() const
{
    return m_ArTrackableList;
}

const ArTrackableList* WrappedTrackableList::Get() const
{
    return m_ArTrackableList;
}

int32_t WrappedTrackableList::Size() const
{
    int32_t ret = 0;
    ArTrackableList_getSize(GetArSession(), m_ArTrackableList, &ret);
    return ret;
}

WrappedTrackableListMutable::WrappedTrackableListMutable()
{
}

WrappedTrackableListMutable::WrappedTrackableListMutable(ArTrackableList* arTrackableList)
    : WrappedTrackableList(arTrackableList)
{
}

WrappedTrackableListMutable::operator ArTrackableList*()
{
    return GetArTrackableListMutable();
}

ArTrackableList* WrappedTrackableListMutable::Get()
{
    return GetArTrackableListMutable();
}

void WrappedTrackableListMutable::PopulateList_All(ArTrackableType arTrackableType)
{
    ArSession_getAllTrackables(GetArSession(), arTrackableType, GetArTrackableListMutable());
}

void WrappedTrackableListMutable::PopulateList_UpdatedOnly(ArTrackableType arTrackableType)
{
    ArFrame_getUpdatedTrackables(GetArSession(), GetArFrame(), arTrackableType, GetArTrackableListMutable());
}

ArTrackableList*& WrappedTrackableListMutable::GetArTrackableListMutable()
{
    return *const_cast<ArTrackableList**>(&m_ArTrackableList);
}

WrappedTrackableListRaii::WrappedTrackableListRaii()
{
    ArTrackableList_create(GetArSession(), &GetArTrackableListMutable());
}

WrappedTrackableListRaii::~WrappedTrackableListRaii()
{
    if (m_ArTrackableList != nullptr)
        ArTrackableList_destroy(GetArTrackableListMutable());
}
