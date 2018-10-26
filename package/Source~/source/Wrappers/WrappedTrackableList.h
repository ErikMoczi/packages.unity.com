#pragma once

#include "arcore_c_api.h"

class WrappedTrackableList
{
public:
    WrappedTrackableList();
    WrappedTrackableList(const ArTrackableList* arTrackableList);

    operator const ArTrackableList*() const;
    const ArTrackableList* Get() const;

    int32_t Size() const;

protected:
    const ArTrackableList* m_ArTrackableList;
};

class WrappedTrackableListMutable : public WrappedTrackableList
{
public:
    WrappedTrackableListMutable();
    WrappedTrackableListMutable(ArTrackableList* arTrackableList);

    operator ArTrackableList*();
    ArTrackableList* Get();

    void PopulateList_All(ArTrackableType arTrackableType);
    void PopulateList_UpdatedOnly(ArTrackableType arTrackableType);    

protected:
    ArTrackableList*& GetArTrackableListMutable();
};

class WrappedTrackableListRaii : public WrappedTrackableListMutable
{
public:
    WrappedTrackableListRaii();
    ~WrappedTrackableListRaii();

private:
    WrappedTrackableListRaii(const WrappedTrackableListRaii&);
    WrappedTrackableListRaii& operator=(const WrappedTrackableListRaii&);
};
