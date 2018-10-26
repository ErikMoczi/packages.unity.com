#pragma once

#include "arcore_c_api.h"

class WrappedAnchorList
{
public:
    WrappedAnchorList();
    WrappedAnchorList(const ArAnchorList* arAnchorList);

    operator const ArAnchorList*();
    const ArAnchorList* Get() const;

    int32_t Size() const;

protected:
    const ArAnchorList* m_ArAnchorList;
};

class WrappedAnchorListMutable : public WrappedAnchorList
{
public:
    WrappedAnchorListMutable();
    WrappedAnchorListMutable(ArAnchorList* arAnchorList);

    operator ArAnchorList*();
    ArAnchorList* Get();

    void PopulateList();

protected:
    ArAnchorList*& GetArAnchorListMutable();
};

class WrappedAnchorListRaii : public WrappedAnchorListMutable
{
public:
    WrappedAnchorListRaii();
    ~WrappedAnchorListRaii();

private:
    WrappedAnchorListRaii(const WrappedAnchorListRaii& original);
    WrappedAnchorListRaii& operator=(const WrappedAnchorListRaii& copyFrom);
};
