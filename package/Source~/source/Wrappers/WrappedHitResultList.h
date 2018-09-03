#pragma once

#include "arcore_c_api.h"
#include "WrappingBase.h"

class WrappedHitResult;

class WrappedHitResultList : public WrappingBase<ArHitResultList>
{
public:
    WrappedHitResultList();
    WrappedHitResultList(eWrappedConstruction);

    void CreateDefault();
    void HitTest(float xPixel, float yPixel);

    int32_t Size() const;
    void GetHitResultAt(int32_t index, WrappedHitResult& hitResult);
};
