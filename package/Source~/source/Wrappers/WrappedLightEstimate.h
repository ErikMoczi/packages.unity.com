#pragma once

#include "arcore_c_api.h"

class WrappedLightEstimate
{
public:
    WrappedLightEstimate();
    WrappedLightEstimate(const ArLightEstimate* arLightEstimate);

    operator const ArLightEstimate*() const;
    const ArLightEstimate* Get() const;

    void GetColorCorrection(float* colorCorrection) const;
    ArLightEstimateState GetState() const;
    float GetPixelIntensity() const;    

protected:
    const ArLightEstimate* m_ArLightEstimate;
};

class WrappedLightEstimateMutable : public WrappedLightEstimate
{
public:
    WrappedLightEstimateMutable();
    WrappedLightEstimateMutable(ArLightEstimate* arLightEstimate);

    operator ArLightEstimate*();
    ArLightEstimate* Get();

    void GetFromFrame();

protected:
    ArLightEstimate*& GetArLightEstimateMutable();
};

class WrappedLightEstimateRaii : public WrappedLightEstimateMutable
{
public:
    WrappedLightEstimateRaii();
    ~WrappedLightEstimateRaii();

private:
    WrappedLightEstimateRaii(const WrappedLightEstimateRaii&);
    WrappedLightEstimateRaii& operator=(const WrappedLightEstimateRaii&);
};
