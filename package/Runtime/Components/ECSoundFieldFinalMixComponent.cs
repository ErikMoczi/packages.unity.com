using System;
using Unity.Entities;

namespace Unity.Audio.Megacity
{
    [Serializable]
    struct ECSoundFieldFinalMix : IComponentData
    {
        public ECSoundPannerData data;
    }
}
