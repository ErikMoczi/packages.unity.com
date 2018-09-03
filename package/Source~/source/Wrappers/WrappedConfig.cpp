#include "Utility.h"
#include "WrappedConfig.h"

template<>
void WrappingBase<ArConfig>::CreateOrAcquireDefaultImpl()
{
    ArConfig_create(GetArSession(), &m_Ptr);
}

template<>
void WrappingBase<ArConfig>::ReleaseImpl()
{
    ArConfig_destroy(m_Ptr);
}

void WrappedConfig::CreateDefault()
{
    CreateOrAcquireDefault();
}

ArLightEstimationMode WrappedConfig::GetLightEstimationMode() const
{
    ArLightEstimationMode mode = AR_LIGHT_ESTIMATION_MODE_AMBIENT_INTENSITY;
    ArConfig_getLightEstimationMode(GetArSession(), m_Ptr, &mode);
    return mode;
}

ArPlaneFindingMode WrappedConfig::GetPlaneFindingMode() const
{
    ArPlaneFindingMode mode = AR_PLANE_FINDING_MODE_HORIZONTAL;
    ArConfig_getPlaneFindingMode(GetArSession(), m_Ptr, &mode);
    return mode;
}

ArUpdateMode WrappedConfig::GetUpdateMode() const
{
    ArUpdateMode mode = AR_UPDATE_MODE_BLOCKING;
    ArConfig_getUpdateMode(GetArSession(), m_Ptr, &mode);
    return mode;
}

bool WrappedConfig::TrySetLightEstimationMode(ArLightEstimationMode mode)
{
    ArLightEstimationMode currentMode = GetLightEstimationMode();
    if (currentMode == mode)
        return true;

    ArConfig_setLightEstimationMode(GetArSession(), m_Ptr, mode);
    if (!IsSupported())
    {
        ArConfig_setLightEstimationMode(GetArSession(), m_Ptr, currentMode);
        return false;
    }

    return true;
}

bool WrappedConfig::TrySetPlaneFindingMode(ArPlaneFindingMode mode)
{
    ArPlaneFindingMode currentMode = GetPlaneFindingMode();
    if (currentMode == mode)
        return true;

    ArConfig_setPlaneFindingMode(GetArSession(), m_Ptr, mode);
    if (!IsSupported())
    {
        ArConfig_setPlaneFindingMode(GetArSession(), m_Ptr, currentMode);
        return false;
    }

    return true;
}

bool WrappedConfig::TrySetUpdateMode(ArUpdateMode mode)
{
    ArUpdateMode currentMode = GetUpdateMode();
    if (currentMode == mode)
        return true;

    ArConfig_setUpdateMode(GetArSession(), m_Ptr, mode);
    if (!IsSupported())
    {
        ArConfig_setUpdateMode(GetArSession(), m_Ptr, currentMode);
        return false;
    }

    return true;
}

bool WrappedConfig::IsSupported() const
{
    return ARSTATUS_SUCCEEDED(ArSession_checkSupported(GetArSession(), m_Ptr));
}
