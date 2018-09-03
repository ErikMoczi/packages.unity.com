#include "Utility.h"
#include "WrappedTrackableList.h"

template<>
void WrappingBase<ArTrackableList>::ReleaseImpl()
{
    ArTrackableList_destroy(m_Ptr);
}

template<>
void WrappingBase<ArTrackableList>::CreateOrAcquireDefaultImpl()
{
    ArTrackableList_create(GetArSession(), &m_Ptr);
}
