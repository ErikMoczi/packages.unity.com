#pragma once

#include "arcore_c_api.h"
#include "Unity/UnityXRTrackable.h"

class WrappedTrackable
{
public:
    WrappedTrackable();
    WrappedTrackable(const ArTrackable* arTrackable);

    operator const ArTrackable*() const;
    const ArTrackable* Get() const;

    ArTrackableType GetType() const;
    UnityXRTrackingState GetTrackingState() const;

protected:
    const ArTrackable* m_ArTrackable;
};

class WrappedTrackableMutable : public WrappedTrackable
{
public:
    WrappedTrackableMutable();
    WrappedTrackableMutable(ArTrackable* arTrackable);

    operator ArTrackable*();
    ArTrackable* Get();

protected:
    ArTrackable*& GetArTrackableMutable();
};

class WrappedTrackableRaii : public WrappedTrackableMutable
{
public:
    WrappedTrackableRaii();
    ~WrappedTrackableRaii();

    void AcquireFromHitResult(const ArHitResult* arHitResult);
    void AcquireFromList(const ArTrackableList* arTrackableList, int32_t index);
    void Release();

private:
    WrappedTrackableRaii(const WrappedTrackableRaii&);
    WrappedTrackableRaii& operator=(const WrappedTrackableRaii&);
};
