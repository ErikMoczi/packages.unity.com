using System;
using Unity.Entities;

namespace Unity.Audio.Megacity
{
    [Serializable]
    struct ECSoundFieldContributor : IComponentData
    {
        public ECSoundPannerData data;
    }
}
