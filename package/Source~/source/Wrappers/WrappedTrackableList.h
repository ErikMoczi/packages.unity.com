#pragma once

#include "arcore_c_api.h"
#include "Utility.h"
#include "WrappingBase.h"

template<typename T_WrappedTrackable, ArTrackableType T_TrackableType>
class WrappedTrackableList : public WrappingBase<ArTrackableList>
{
public:
    WrappedTrackableList()
        : WrappingBase<ArTrackableList>()
    {}

    WrappedTrackableList(eWrappedConstruction)
        : WrappingBase<ArTrackableList>()
    {
        CreateOrAcquireDefault();
    }

    void CreateDefault()
    {
        CreateOrAcquireDefault();
    }

    int32_t Size() const
    {
        int32_t ret = 0;
        ArTrackableList_getSize(GetArSession(), m_Ptr, &ret);
        return ret;
    }

    bool TryAcquireAt(int32_t trackableIndex, T_WrappedTrackable& trackable) const
    {
        ArTrackable* getter = nullptr;
        ArTrackableList_acquireItem(GetArSession(), m_Ptr, trackableIndex, &getter);

        ArTrackableType getterType = EnumCast<ArTrackableType>(-1);
        ArTrackable_getType(GetArSession(), getter, &getterType);
        if (getterType != T_TrackableType)
        {
            ArTrackable_release(getter);
            return false;
        }

        trackable.AssumeOwnership(reinterpret_cast<typename T_WrappedTrackable::UnderlyingType*>(getter));
        return true;
    }

protected:
    void GetAllTrackables()
    {
        ArSession_getAllTrackables(GetArSession(), T_TrackableType, m_Ptr);
    }

    void GetUpdatedTrackables()
    {
        ArFrame_getUpdatedTrackables(GetArSession(), GetArFrame(), T_TrackableType, m_Ptr);
    }
};
