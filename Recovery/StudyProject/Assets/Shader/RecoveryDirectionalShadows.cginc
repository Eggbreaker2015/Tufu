// The APK's GLES programs sample a raw directional depth map. Desktop Built-in
// Forward rendering binds an already filtered screen-space mask under the same
// _ShadowMapTexture name. Its strength and distance fade are already included.
#ifndef TUFU_RECOVERY_DIRECTIONAL_SHADOWS_INCLUDED
#define TUFU_RECOVERY_DIRECTIONAL_SHADOWS_INCLUDED

// Set by each recovered fragment wrapper from SV_Position / _ScreenParams.xy.
static float2 RecoveryShadowScreenUV;

float4 RecoveryDirectionalShadowData()
{
#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
    return _LightShadowData;
#else
    return 0.0;
#endif
}

float RecoverySampleDirectionalShadow(float3 shadowPosition)
{
#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
    return hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(
        sampler_hlslcc_zcmp_ShadowMapTexture, shadowPosition.xy, shadowPosition.z);
#else
    return tex2D(_ShadowMapTexture, RecoveryShadowScreenUV).r;
#endif
}
#endif
