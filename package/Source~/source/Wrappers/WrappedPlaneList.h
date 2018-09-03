#pragma once

#include "WrappedPlane.h"
#include "WrappedTrackableList.h"

class WrappedPlaneList : public WrappedTrackableList<WrappedPlane, AR_TRACKABLE_PLANE>
{
public:
    WrappedPlaneList();
    WrappedPlaneList(eWrappedConstruction);

    void GetAllPlanes();
    void GetUpdatedPlanes();
};
