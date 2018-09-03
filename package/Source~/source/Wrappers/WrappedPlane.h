#include "arcore_c_api.h"
#include "WrappingBase.h"

#include <vector>

struct IUnityXRPlaneDataAllocator;

struct UnityXRPlane;
struct UnityXRVector3;

class WrappedPlane : public WrappingBase<ArPlane>
{
public:
    void ConvertToUnityXRPlane(UnityXRPlane& unityPlane, IUnityXRPlaneDataAllocator& allocator);
    bool AcquireSubsumedBy(WrappedPlane& subsumedBy);
};
