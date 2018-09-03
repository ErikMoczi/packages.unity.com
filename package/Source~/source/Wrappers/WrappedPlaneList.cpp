#include "WrappedPlaneList.h"

WrappedPlaneList::WrappedPlaneList()
    : WrappedTrackableList()
{
}

WrappedPlaneList::WrappedPlaneList(eWrappedConstruction e)
    : WrappedTrackableList(e)
{
}

void WrappedPlaneList::GetAllPlanes()
{
    GetAllTrackables();
}

void WrappedPlaneList::GetUpdatedPlanes()
{
    GetUpdatedTrackables();
}
