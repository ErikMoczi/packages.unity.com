#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Shadows.hlsl"

Texture2D _CameraDepthTexture;
float3 _LightDirection;

float3 GetCurrentViewPosition()
{
    return UNITY_MATRIX_I_V._14_24_34;
}

float4 VFXTransformPositionWorldToClip(float3 posWS)
{
    return TransformWorldToHClip(posWS);
}

float4 VFXTransformPositionObjectToClip(float3 posOS)
{
    float3 posWS = TransformObjectToWorld(posOS);
    return VFXTransformPositionWorldToClip(posWS);
}

float3 VFXTransformPositionWorldToView(float3 posWS)
{
    return TransformWorldToView(posWS);
}

float4x4 VFXGetObjectToWorldMatrix()
{
    return GetObjectToWorldMatrix();
}

float4x4 VFXGetWorldToObjectMatrix()
{
    return GetWorldToObjectMatrix();
}

float3x3 VFXGetWorldToViewRotMatrix()
{
    return (float3x3)GetWorldToViewMatrix();
}

float3 VFXGetViewWorldPosition()
{
    return GetCurrentViewPosition();
}

float4x4 VFXGetViewToWorldMatrix()
{
    return UNITY_MATRIX_I_V;
}

float VFXSampleDepth(float4 posSS)
{
    return LOAD_TEXTURE2D(_CameraDepthTexture, posSS.xy).r;
}

float VFXLinearEyeDepth(float depth)
{
    return LinearEyeDepth(depth, _ZBufferParams);
}

void VFXApplyShadowBias(inout float4 posCS, inout float3 posWS, float3 normalWS)
{
    posWS = ApplyShadowBias(posWS, normalWS, _LightDirection);
    posCS = VFXTransformPositionWorldToClip(posWS);
}

void VFXApplyShadowBias(inout float4 posCS, inout float3 posWS)
{
    posWS = ApplyShadowBias(posWS, _LightDirection, _LightDirection);
    posCS = VFXTransformPositionWorldToClip(posWS);
}

float4 VFXApplyFog(float4 color,float4 posCS,float3 posWS)
{
   float4 fog = (float4)0;
   fog.rgb = unity_FogColor.rgb;
   fog.a = ComputeFogFactor(posCS.z * posCS.w); //TODO Move this to vertex stage to fit with LWRP result

#if VFX_BLENDMODE_ALPHA || IS_OPAQUE_PARTICLE
   color.rgb = lerp(fog.rgb, color.rgb, fog.a);
#elif VFX_BLENDMODE_ADD
   color.rgb *= fog.a;
#elif VFX_BLENDMODE_PREMULTIPLY
   color.rgb = lerp(fog.rgb * color.a, color.rgb, fog.a);
#endif
   return color;
}

float4 VFXApplyPreExposure(float4 color)
{
    return color;
}
