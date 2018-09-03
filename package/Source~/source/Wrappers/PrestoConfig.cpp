#include "PrestoConfig.h"
#include "arpresto_api.h"

static ArPrestoConfig s_PrestoConfig = {};

static bool s_Dirty = true;

void SetLightEstimationMode(ArLightEstimationMode mode)
{
    if (s_PrestoConfig.light_estimation_mode == mode)
        return;

    s_PrestoConfig.light_estimation_mode = mode;
    s_Dirty = true;
}

void SetPlaneFindingMode(ArPlaneFindingMode mode)
{
    if (s_PrestoConfig.plane_finding_mode == mode)
        return;

    s_PrestoConfig.plane_finding_mode = mode;
    s_Dirty = true;
}

void SetUpdateMode(ArUpdateMode mode)
{
    if (s_PrestoConfig.update_mode == mode)
        return;

    s_PrestoConfig.update_mode = mode;
    s_Dirty = true;
}

void SetCloudAnchorMode(ArCloudAnchorMode mode)
{
    if (s_PrestoConfig.cloud_anchor_mode == mode)
        return;

    s_PrestoConfig.cloud_anchor_mode = mode;
    s_Dirty = true;
}

void UpdateConfigIfNeeded()
{
    if (!s_Dirty)
        return;

    ArPresto_setConfiguration(&s_PrestoConfig);
    s_Dirty = false;
}
