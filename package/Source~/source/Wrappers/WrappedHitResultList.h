#pragma once

#include "arcore_c_api.h"

class WrappedHitResultList
{
public:
    WrappedHitResultList();
    WrappedHitResultList(const ArHitResultList* arHitResultList);

    operator const ArHitResultList*() const;
    const ArHitResultList* Get() const;

    int32_t Size() const;

protected:
    const ArHitResultList* m_ArHitResultList;
};

class WrappedHitResultListMutable : public WrappedHitResultList
{
public:
    WrappedHitResultListMutable();
    WrappedHitResultListMutable(ArHitResultList* arHitResultList);

    operator ArHitResultList*();
    ArHitResultList* Get();

    void HitTest(float xPixel, float yPixel);

protected:
    ArHitResultList*& GetArHitResultListMutable();
};

class WrappedHitResultListRaii : public WrappedHitResultListMutable
{
public:
    WrappedHitResultListRaii();
    ~WrappedHitResultListRaii();

private:
    WrappedHitResultListRaii(const WrappedHitResultListRaii&);
    WrappedHitResultListRaii& operator=(const WrappedHitResultListRaii&);
};
