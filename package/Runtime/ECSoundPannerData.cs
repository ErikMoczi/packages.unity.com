using System;
using Unity.Mathematics;

namespace Unity.Audio.Megacity
{
    [Serializable]
    struct ECSoundPannerData
    {
        public float directL;
        public float directR;
        public float directL_LPF;
        public float directR_LPF;

        public void Add(ECSoundPannerData v)
        {
            directL += v.directL;
            directR += v.directR;
            directL_LPF += v.directL_LPF;
            directR_LPF += v.directR_LPF;
        }

        public void CalculatePanParams(
            float distanceFromCenter,
            float4 localPos,
            float3 sourcePosition,
            float3 coneDirection,
            ref ECSoundEmitterDefinition emitter,
            float4 listenerPosition,
            out float frontPan,
            out float leftPan,
            out float rightPan)
        {
            // Calculate distance from center point and between min and max radii
            var minDist = math.min(emitter.minDist, emitter.maxDist);
            var maxDist = math.max(emitter.minDist, emitter.maxDist);
            var distanceBetweenMinAndMax = math.max(0.0f, distanceFromCenter - emitter.minDist);

            // Logarithmic and linear attenuation blend
            var deltaDist = math.max(0.1f, maxDist - minDist);
            var logAttenuation = maxDist / (maxDist + minDist * distanceBetweenMinAndMax * distanceBetweenMinAndMax);
            var linearAttenuation = math.clamp(math.pow(10.0f, -3.0f * distanceBetweenMinAndMax / deltaDist), 0.0f, 1.0f);
            var attenuation = emitter.volume * (logAttenuation + (linearAttenuation - logAttenuation) * emitter.falloffMode);

            // Calculate listener position relative to cone orientation to see if it falls inside or outside cone transition range.
            var deviationFromConeDirection = math.dot(math.normalize(sourcePosition - listenerPosition.xyz), coneDirection);
            float doubleLocalAngle = math.acos(deviationFromConeDirection) * (360.0f / (float)math.PI);
            float coneFalloff = math.clamp((emitter.coneAngle - doubleLocalAngle) / math.max(0.001f, emitter.coneTransition), 0.0f, 1.0f);

            attenuation *= coneFalloff;

            // Left/right panning
            var leftDir = 0.5f * localPos.z - localPos.x;
            var rightDir = 0.5f * localPos.z + localPos.x;
            leftPan = attenuation * (0.65f + 0.45f * math.clamp(leftDir * math.abs(leftDir) / (distanceFromCenter + 0.001f), -1.0f, 1.0f));
            rightPan = attenuation * (0.65f + 0.45f * math.clamp(rightDir * math.abs(rightDir) / (distanceFromCenter + 0.001f), -1.0f, 1.0f));

            // Front/back panning (influence of lowpass filter)
            frontPan = 0.65f + 0.45f * math.clamp(localPos.z * math.abs(localPos.z) / (distanceFromCenter + 0.001f), -1.0f, 1.0f);
            frontPan *= coneFalloff;
        }

        public void CalculatePanningAdditive(
            float3 sourcePosition,
            float3 coneDirection,
            ref ECSoundEmitterDefinition emitter,
            ref float4x4 listenerWorldToLocal,
            float4 listenerPosition,
            float distanceFromCenter)
        {
            var localPos = math.mul(listenerWorldToLocal, new float4(sourcePosition, 1.0f));
            float frontPan, leftPan, rightPan;
            CalculatePanParams(distanceFromCenter, localPos, sourcePosition, coneDirection, ref emitter, listenerPosition, out frontPan, out leftPan, out rightPan);
            float backPan = 1.0f - frontPan;
            float dryPan = 1.0f;
            directL += leftPan * frontPan * dryPan;
            directR += rightPan * frontPan * dryPan;
            directL_LPF += leftPan * backPan * dryPan;
            directR_LPF += rightPan * backPan * dryPan;
        }

        public void CalculatePanningDirect(
            float3 sourcePosition,
            float3 coneDirection,
            ref ECSoundEmitterDefinition emitter,
            ref float4x4 listenerWorldToLocal,
            float4 listenerPosition,
            float distanceFromCenter)
        {
            var localPos = math.mul(listenerWorldToLocal, new float4(sourcePosition, 1.0f));
            float frontPan, leftPan, rightPan;
            CalculatePanParams(distanceFromCenter, localPos, sourcePosition, coneDirection, ref emitter, listenerPosition, out frontPan, out leftPan, out rightPan);
            float backPan = 1.0f - frontPan;
            float dryPan = 1.0f;
            directL = leftPan * frontPan * dryPan;
            directR = rightPan * frontPan * dryPan;
            directL_LPF = leftPan * backPan * dryPan;
            directR_LPF = rightPan * backPan * dryPan;
        }
    }
}
