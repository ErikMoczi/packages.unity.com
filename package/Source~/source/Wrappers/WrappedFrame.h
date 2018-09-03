#pragma once

#include "arcore_c_api.h"
#include "WrappingBase.h"

class WrappedFrame : public WrappingBase<ArFrame>
{
public:
    void CreateDefault();

    bool DidDisplayGeometryChange() const;
};
