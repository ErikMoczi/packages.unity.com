#include "Utility.h"
#include "WrappedLightEstimate.h"

template<>
void WrappingBase<ArLightEstimate>::CreateOrAcquireDefaultImpl()
{
    ArLightEstimate_create(GetArSession(), &m_Ptr);
}

template<>
void WrappingBase<ArLightEstimate>::ReleaseImpl()
{
    ArLightEstimate_destroy(m_Ptr);
}

WrappedLightEstimate::WrappedLightEstimate()
    : WrappingBase<ArLightEstimate>()
{
}

WrappedLightEstimate::WrappedLightEstimate(eWrappedConstruction)
    : WrappingBase<ArLightEstimate>()
{
    CreateOrAcquireDefault();
}

void WrappedLightEstimate::CreateDefault()
{
    CreateOrAcquireDefault();
}

void WrappedLightEstimate::GetFromFrame()
{
    ArFrame_getLightEstimate(GetArSession(), GetArFrame(), m_Ptr);
}

ArLightEstimateState WrappedLightEstimate::GetState() const
{
    ArLightEstimateState ret = EnumCast<ArLightEstimateState>(-1);
    ArLightEstimate_getState(GetArSession(), m_Ptr, &ret);
    return ret;
}

float WrappedLightEstimate::GetPixelIntensity() const
{
    float ret = -1.0f;
    ArLightEstimate_getPixelIntensity(GetArSession(), m_Ptr, &ret);
    return ret;
}
