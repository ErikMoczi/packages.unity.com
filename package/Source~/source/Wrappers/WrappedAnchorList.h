#pragma once

#include "arcore_c_api.h"
#include "WrappingBase.h"

class WrappedAnchor;

class WrappedAnchorList : public WrappingBase<ArAnchorList>
{
public:
    WrappedAnchorList();
    WrappedAnchorList(eWrappedConstruction);

    void CreateDefault();

    void GetAllAnchors();
    void AcquireAt(int32_t index, WrappedAnchor& anchor) const;
    int32_t Size() const;
};
