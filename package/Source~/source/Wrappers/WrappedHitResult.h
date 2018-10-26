#pragma once

#include "arcore_c_api.h"

struct UnityXRPose;

class WrappedHitResult
{
public:
    WrappedHitResult();
    WrappedHitResult(const ArHitResult* arHitResult);

    operator const ArHitResult*() const;
    const ArHitResult* Get() const;

    void GetPose(ArPose* arPose) const;
    void GetPose(UnityXRPose& xrPose) const;
    float GetDistance() const;    

protected:
    const ArHitResult* m_ArHitResult;
};

class WrappedHitResultMutable : public WrappedHitResult
{
public:
    WrappedHitResultMutable();
    WrappedHitResultMutable(ArHitResult* arHitResult);

    operator ArHitResult*();
    ArHitResult* Get();

    void GetFromList(const ArHitResultList* hitResultList, int32_t index);

protected:
    ArHitResult*& GetArHitResultMutable();
};

class WrappedHitResultRaii : public WrappedHitResultMutable
{
public:
    WrappedHitResultRaii();
    ~WrappedHitResultRaii();

private:
    WrappedHitResultRaii(const WrappedHitResultRaii&);
    WrappedHitResultRaii& operator=(const WrappedHitResultRaii&);
};
