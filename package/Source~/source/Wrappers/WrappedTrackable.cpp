#include "Utility.h"
#include "WrappedTrackable.h"

template<>
void WrappingBase<ArTrackable>::ReleaseImpl()
{
    ArTrackable_release(m_Ptr);
}

ArTrackableType WrappedTrackable::GetType() const
{
    ArTrackableType ret = EnumCast<ArTrackableType>(-1);
    ArTrackable_getType(GetArSession(), m_Ptr, &ret);
    return ret;
}
