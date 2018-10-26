#include "Utility.h"
#include "WrappedLightEstimate.h"

WrappedLightEstimate::WrappedLightEstimate()
    : m_ArLightEstimate(nullptr)
{
}

WrappedLightEstimate::WrappedLightEstimate(const ArLightEstimate* arLightEstimate)
    : m_ArLightEstimate(arLightEstimate)
{
}

WrappedLightEstimate::operator const ArLightEstimate*() const
{
    return m_ArLightEstimate;
}

const ArLightEstimate* WrappedLightEstimate::Get() const
{
    return m_ArLightEstimate;
}

void WrappedLightEstimate::GetColorCorrection(float* colorCorrection) const
{
    ArLightEstimate_getColorCorrection(GetArSession(), m_ArLightEstimate, colorCorrection);
}    

ArLightEstimateState WrappedLightEstimate::GetState() const
{
    ArLightEstimateState ret = EnumCast<ArLightEstimateState>(-1);
    ArLightEstimate_getState(GetArSession(), m_ArLightEstimate, &ret);
    return ret;
}

float WrappedLightEstimate::GetPixelIntensity() const
{
    float ret = -1.0f;
    ArLightEstimate_getPixelIntensity(GetArSession(), m_ArLightEstimate, &ret);
    return ret;
}

WrappedLightEstimateMutable::WrappedLightEstimateMutable()
{
}

WrappedLightEstimateMutable::WrappedLightEstimateMutable(ArLightEstimate* arLightEstimate)
    : WrappedLightEstimate(arLightEstimate)
{
}

WrappedLightEstimateMutable::operator ArLightEstimate*()
{
    return GetArLightEstimateMutable();
}

ArLightEstimate* WrappedLightEstimateMutable::Get()
{
    return GetArLightEstimateMutable();
}

void WrappedLightEstimateMutable::GetFromFrame()
{
    ArFrame_getLightEstimate(GetArSession(), GetArFrame(), GetArLightEstimateMutable());
}

ArLightEstimate*& WrappedLightEstimateMutable::GetArLightEstimateMutable()
{
    return *const_cast<ArLightEstimate**>(&m_ArLightEstimate);
}

WrappedLightEstimateRaii::WrappedLightEstimateRaii()
{
    ArLightEstimate_create(GetArSession(), &GetArLightEstimateMutable());
}

WrappedLightEstimateRaii::~WrappedLightEstimateRaii()
{
    if (m_ArLightEstimate != nullptr)
        ArLightEstimate_destroy(GetArLightEstimateMutable());
}
