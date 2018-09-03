#include "arcore_c_api.h"
#include "WrappingBase.h"

class WrappedLightEstimate : public WrappingBase<ArLightEstimate>
{
public:
    WrappedLightEstimate();
    WrappedLightEstimate(eWrappedConstruction);

    void CreateDefault();
    void GetFromFrame();

    ArLightEstimateState GetState() const;
    float GetPixelIntensity() const;
};
