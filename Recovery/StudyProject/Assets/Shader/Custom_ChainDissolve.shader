Shader "Custom/ChainDissolve" {
Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo", 2D) = "white" {}
		_HColor ("Highlight Color", Vector) = (1,1,1,1)
		_SColor ("Shadow Color", Vector) = (0.45,0.45,0.45,1)
		_RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.5
		_RampSmoothing ("Ramp Smoothing", Range(0.001, 1)) = 0.1
		[Header(Outline)] _OutlineColor ("Outline Color", Vector) = (0,0,0,1)
		_OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.006
		[Header(Dissolve)] _Reveal ("Reveal (0..1)", Range(0, 1)) = 1
		_RevealDirection ("Reveal Dir (1=start to end)", Range(0, 1)) = 1
		_EdgeProgress ("Edge Progress (0..1, 1=anim end)", Range(0, 1)) = 0
		_EdgeWidthMin ("Edge Width Min", Range(0, 0.5)) = 0
		_EdgeWidthMax ("Edge Width Max", Range(0.001, 0.5)) = 0.18
		_EdgeRise ("Edge Rise (fraction of progress to peak)", Range(0.001, 0.5)) = 0.06
		[HDR] _EdgeColor ("Edge Color", Vector) = (1.4,0.7,0.2,1)
		_EdgeIntensity ("Edge Intensity", Range(0, 8)) = 2
	}
// Recovered from the APK GLES programs; generated HLSL, not original author source.
SubShader {
Tags { "Queue"="Geometry" "RenderType"="Opaque" }
Pass {
Name "OUTLINE"
Tags { "LightMode"="Always" "Queue"="Geometry" "RenderType"="Opaque" }
Cull Front
ZWrite On
ZTest LEqual
Blend One Zero, One Zero
ColorMask RGBA
BlendOp Add, Add
Stencil {
Ref 0
ReadMask 255
WriteMask 255
Comp Always
Pass Keep
Fail Keep
ZFail Keep
}
HLSLPROGRAM
#pragma target 4.5
#pragma vertex RecoveryVert
#pragma fragment RecoveryFrag
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float _OutlineWidth;
float _RevealDirection;
float _EdgeRise;
float _EdgeProgress;
float _EdgeWidthMin;
float _EdgeWidthMax;
float _Reveal;
float4 _OutlineColor;
static float4 v_gl_Position;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;
static float2 v_in_TEXCOORD1;
static float v_vs_TEXCOORD0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float v_vs_TEXCOORD0 : TEXCOORD0;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat2;

void v_vert_main()
{
    float3 _23 = (v_in_NORMAL0 * _OutlineWidth.xxx) + v_in_POSITION0.xyz;
    v_u_xlat0.x = _23.x;
    v_u_xlat0.y = _23.y;
    v_u_xlat0.z = _23.z;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_u_xlat0 += transpose(unity_ObjectToWorld)[3];
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_u_xlat0.x = v_in_TEXCOORD1.x;
    v_u_xlat0.x = clamp(v_u_xlat0.x, 0.0f, 1.0f);
    v_u_xlat2 = (-v_u_xlat0.x) + 1.0f;
    v_u_xlat0.x = (-v_u_xlat2) + v_u_xlat0.x;
    v_vs_TEXCOORD0 = (_RevealDirection * v_u_xlat0.x) + v_u_xlat2;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    return stage_output;
}
static float f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float f_vs_TEXCOORD0 : TEXCOORD0;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float f_u_xlat0;
static float f_u_xlat1;
static float f_u_xlat2;
static bool f_u_xlatb0;

void f_frag_main()
{
    f_u_xlat0 = _EdgeRise + (-1.0f);
    f_u_xlat0 = 1.0f / f_u_xlat0;
    f_u_xlat1 = _EdgeProgress + (-1.0f);
    f_u_xlat0 *= f_u_xlat1;
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f, 1.0f);
    f_u_xlat1 = (f_u_xlat0 * (-2.0f)) + 3.0f;
    f_u_xlat0 *= f_u_xlat0;
    f_u_xlat0 *= f_u_xlat1;
    f_u_xlat1 = 1.0f / _EdgeRise;
    f_u_xlat1 *= _EdgeProgress;
    f_u_xlat1 = clamp(f_u_xlat1, 0.0f, 1.0f);
    f_u_xlat2 = (f_u_xlat1 * (-2.0f)) + 3.0f;
    f_u_xlat1 *= f_u_xlat1;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat0 *= f_u_xlat1;
    f_u_xlat1 = (-_EdgeWidthMin) + _EdgeWidthMax;
    f_u_xlat0 = (f_u_xlat0 * f_u_xlat1) + _EdgeWidthMin;
    f_u_xlat1 = (-f_vs_TEXCOORD0) + _Reveal;
    f_u_xlat0 = (f_u_xlat0 * 0.5f) + f_u_xlat1;
    f_u_xlatb0 = f_u_xlat0 < 0.0f;
    if (f_u_xlatb0)
    {
        discard;
    }
    f_SV_Target0 = _OutlineColor;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
ENDHLSL
}
Pass {
Name ""
Tags { "LightMode"="ForwardBase" "Queue"="Geometry" "RenderType"="Opaque" "SHADOWSUPPORT"="true" }
Cull Back
ZWrite On
ZTest LEqual
Blend One Zero, One Zero
ColorMask RGBA
BlendOp Add, Add
Stencil {
Ref 0
ReadMask 255
WriteMask 255
Comp Always
Pass Keep
Fail Keep
ZFail Keep
}
HLSLPROGRAM
#pragma target 4.5
#pragma vertex RecoveryVert
#pragma fragment RecoveryFrag
#define DIRECTIONAL 1
#pragma multi_compile _ LIGHTPROBE_SH
#pragma multi_compile _ SHADOWS_SCREEN
#pragma multi_compile _ VERTEXLIGHT_ON
#if defined(DIRECTIONAL) && defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToShadow[4];
float _RevealDirection;
float4 _MainTex_ST;
float _EdgeRise;
float _EdgeProgress;
float _EdgeWidthMin;
float _EdgeWidthMax;
float _Reveal;
float _EdgeIntensity;
float4 _WorldSpaceLightPos0;
float _RampSmoothing;
float _RampThreshold;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _LightShadowData;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 glstate_lightmodel_ambient;
float4 _LightColor0;
float4 _EdgeColor;
#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
#else
sampler2D _ShadowMapTexture;
#endif
float4 _ScreenParams;
#include "RecoveryDirectionalShadows.cginc"

float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_in_TEXCOORD1;
static float v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float2 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float v_vs_TEXCOORD2 : TEXCOORD1;
    float3 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD3 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat4;
static float v_u_xlat10;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_u_xlat1.x = v_in_TEXCOORD1.x;
    v_u_xlat1.x = clamp(v_u_xlat1.x, 0.0f, 1.0f);
    v_u_xlat4 = (-v_u_xlat1.x) + 1.0f;
    v_u_xlat1.x = (-v_u_xlat4) + v_u_xlat1.x;
    v_vs_TEXCOORD2 = (_RevealDirection * v_u_xlat1.x) + v_u_xlat4;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_vs_TEXCOORD1 = v_u_xlat10.xxx * v_u_xlat1.xyz;
    v_u_xlat1 = v_u_xlat0.yyyy * RecoveryColumn_unity_WorldToShadow(1);
    v_u_xlat1 = (RecoveryColumn_unity_WorldToShadow(0) * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (RecoveryColumn_unity_WorldToShadow(2) * v_u_xlat0.zzzz) + v_u_xlat1;
    v_vs_TEXCOORD3 = (RecoveryColumn_unity_WorldToShadow(3) * v_u_xlat0.wwww) + v_u_xlat1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float f_vs_TEXCOORD2 : TEXCOORD1;
    float3 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD3 : TEXCOORD3;
    float4 recoveryPixelPosition : SV_Position;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float f_u_xlat8;
static bool f_u_xlatb4;
static float3 f_u_xlat16_1;
static float f_u_xlat12;
static float3 f_u_xlat2;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0.x = _EdgeRise + (-1.0f);
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat4.x = _EdgeProgress + (-1.0f);
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = 1.0f / _EdgeRise;
    f_u_xlat4.x *= _EdgeProgress;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = (-_EdgeWidthMin) + _EdgeWidthMax;
    f_u_xlat0.x = (f_u_xlat0.x * f_u_xlat4.x) + _EdgeWidthMin;
    f_u_xlat4.x = (-f_vs_TEXCOORD2) + _Reveal;
    f_u_xlat8 = (f_u_xlat0.x * 0.5f) + f_u_xlat4.x;
    f_u_xlat0.x = max(f_u_xlat0.x, 9.9999997473787516355514526367188e-05f);
    f_u_xlat0.x = abs(f_u_xlat4.x) / f_u_xlat0.x;
    f_u_xlat0.x = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat0.x = max(f_u_xlat0.x, 0.0f);
    f_u_xlat0.x *= _EdgeIntensity;
    f_u_xlatb4 = f_u_xlat8 < 0.0f;
    if (f_u_xlatb4)
    {
        discard;
    }
    f_u_xlat4.x = dot(f_vs_TEXCOORD1, f_vs_TEXCOORD1);
    f_u_xlat4.x = rsqrt(f_u_xlat4.x);
    f_u_xlat4 = f_u_xlat4.xxx * f_vs_TEXCOORD1;
    f_u_xlat16_1.x = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_1.x = rsqrt(f_u_xlat16_1.x);
    f_u_xlat16_1 = f_u_xlat16_1.xxx * _WorldSpaceLightPos0.xyz;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat16_1);
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (-_RampSmoothing) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat8) + f_u_xlat4.x;
    f_u_xlat12 = _RampSmoothing + _RampThreshold;
    f_u_xlat8 = (-f_u_xlat8) + f_u_xlat12;
    f_u_xlat8 = 1.0f / f_u_xlat8;
    f_u_xlat4.x = f_u_xlat8 * f_u_xlat4.x;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    float3 txVec0 = float3(f_vs_TEXCOORD3.xy, f_vs_TEXCOORD3.z);
    f_u_xlat8 = RecoverySampleDirectionalShadow(txVec0);
    f_u_xlat12 = (-RecoveryDirectionalShadowData().x) + 1.0f;
    f_u_xlat8 = (f_u_xlat8 * f_u_xlat12) + RecoveryDirectionalShadowData().x;
    f_u_xlat4.x = f_u_xlat8 * f_u_xlat4.x;
    f_u_xlat2 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat4 = (f_u_xlat4.xxx * f_u_xlat2) + _SColor.xyz;
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat2 = f_u_xlat16_2 * _Color.xyz;
    f_u_xlat16_1 = f_u_xlat4 * f_u_xlat2;
    f_u_xlat16_3 = glstate_lightmodel_ambient.xyz + glstate_lightmodel_ambient.xyz;
    f_u_xlat16_3 = f_u_xlat2 * f_u_xlat16_3;
    f_u_xlat16_1 = (f_u_xlat16_1 * _LightColor0.xyz) + f_u_xlat16_3;
    f_u_xlat0 = (_EdgeColor.xyz * f_u_xlat0.xxx) + f_u_xlat16_1;
    f_SV_Target0.x = f_u_xlat0.x;
    f_SV_Target0.y = f_u_xlat0.y;
    f_SV_Target0.z = f_u_xlat0.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    RecoveryShadowScreenUV = stage_input.recoveryPixelPosition.xy / _ScreenParams.xy;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToShadow[4];
float _RevealDirection;
float4 _MainTex_ST;
float _EdgeRise;
float _EdgeProgress;
float _EdgeWidthMin;
float _EdgeWidthMax;
float _Reveal;
float _EdgeIntensity;
float4 _WorldSpaceLightPos0;
float _RampSmoothing;
float _RampThreshold;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _LightShadowData;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 glstate_lightmodel_ambient;
float4 _LightColor0;
float4 _EdgeColor;
#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
#else
sampler2D _ShadowMapTexture;
#endif
float4 _ScreenParams;
#include "RecoveryDirectionalShadows.cginc"

float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_in_TEXCOORD1;
static float v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float2 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float v_vs_TEXCOORD2 : TEXCOORD1;
    float3 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD3 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat4;
static float v_u_xlat10;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_u_xlat1.x = v_in_TEXCOORD1.x;
    v_u_xlat1.x = clamp(v_u_xlat1.x, 0.0f, 1.0f);
    v_u_xlat4 = (-v_u_xlat1.x) + 1.0f;
    v_u_xlat1.x = (-v_u_xlat4) + v_u_xlat1.x;
    v_vs_TEXCOORD2 = (_RevealDirection * v_u_xlat1.x) + v_u_xlat4;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_vs_TEXCOORD1 = v_u_xlat10.xxx * v_u_xlat1.xyz;
    v_u_xlat1 = v_u_xlat0.yyyy * RecoveryColumn_unity_WorldToShadow(1);
    v_u_xlat1 = (RecoveryColumn_unity_WorldToShadow(0) * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (RecoveryColumn_unity_WorldToShadow(2) * v_u_xlat0.zzzz) + v_u_xlat1;
    v_vs_TEXCOORD3 = (RecoveryColumn_unity_WorldToShadow(3) * v_u_xlat0.wwww) + v_u_xlat1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float f_vs_TEXCOORD2 : TEXCOORD1;
    float3 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD3 : TEXCOORD3;
    float4 recoveryPixelPosition : SV_Position;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float f_u_xlat8;
static bool f_u_xlatb4;
static float3 f_u_xlat16_1;
static float f_u_xlat12;
static float3 f_u_xlat2;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0.x = _EdgeRise + (-1.0f);
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat4.x = _EdgeProgress + (-1.0f);
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = 1.0f / _EdgeRise;
    f_u_xlat4.x *= _EdgeProgress;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = (-_EdgeWidthMin) + _EdgeWidthMax;
    f_u_xlat0.x = (f_u_xlat0.x * f_u_xlat4.x) + _EdgeWidthMin;
    f_u_xlat4.x = (-f_vs_TEXCOORD2) + _Reveal;
    f_u_xlat8 = (f_u_xlat0.x * 0.5f) + f_u_xlat4.x;
    f_u_xlat0.x = max(f_u_xlat0.x, 9.9999997473787516355514526367188e-05f);
    f_u_xlat0.x = abs(f_u_xlat4.x) / f_u_xlat0.x;
    f_u_xlat0.x = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat0.x = max(f_u_xlat0.x, 0.0f);
    f_u_xlat0.x *= _EdgeIntensity;
    f_u_xlatb4 = f_u_xlat8 < 0.0f;
    if (f_u_xlatb4)
    {
        discard;
    }
    f_u_xlat4.x = dot(f_vs_TEXCOORD1, f_vs_TEXCOORD1);
    f_u_xlat4.x = rsqrt(f_u_xlat4.x);
    f_u_xlat4 = f_u_xlat4.xxx * f_vs_TEXCOORD1;
    f_u_xlat16_1.x = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_1.x = rsqrt(f_u_xlat16_1.x);
    f_u_xlat16_1 = f_u_xlat16_1.xxx * _WorldSpaceLightPos0.xyz;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat16_1);
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (-_RampSmoothing) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat8) + f_u_xlat4.x;
    f_u_xlat12 = _RampSmoothing + _RampThreshold;
    f_u_xlat8 = (-f_u_xlat8) + f_u_xlat12;
    f_u_xlat8 = 1.0f / f_u_xlat8;
    f_u_xlat4.x = f_u_xlat8 * f_u_xlat4.x;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    float3 txVec0 = float3(f_vs_TEXCOORD3.xy, f_vs_TEXCOORD3.z);
    f_u_xlat8 = RecoverySampleDirectionalShadow(txVec0);
    f_u_xlat12 = (-RecoveryDirectionalShadowData().x) + 1.0f;
    f_u_xlat8 = (f_u_xlat8 * f_u_xlat12) + RecoveryDirectionalShadowData().x;
    f_u_xlat4.x = f_u_xlat8 * f_u_xlat4.x;
    f_u_xlat2 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat4 = (f_u_xlat4.xxx * f_u_xlat2) + _SColor.xyz;
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat2 = f_u_xlat16_2 * _Color.xyz;
    f_u_xlat16_1 = f_u_xlat4 * f_u_xlat2;
    f_u_xlat16_3 = glstate_lightmodel_ambient.xyz + glstate_lightmodel_ambient.xyz;
    f_u_xlat16_3 = f_u_xlat2 * f_u_xlat16_3;
    f_u_xlat16_1 = (f_u_xlat16_1 * _LightColor0.xyz) + f_u_xlat16_3;
    f_u_xlat0 = (_EdgeColor.xyz * f_u_xlat0.xxx) + f_u_xlat16_1;
    f_SV_Target0.x = f_u_xlat0.x;
    f_SV_Target0.y = f_u_xlat0.y;
    f_SV_Target0.z = f_u_xlat0.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    RecoveryShadowScreenUV = stage_input.recoveryPixelPosition.xy / _ScreenParams.xy;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float _RevealDirection;
float4 _MainTex_ST;
float _EdgeRise;
float _EdgeProgress;
float _EdgeWidthMin;
float _EdgeWidthMax;
float _Reveal;
float _EdgeIntensity;
float4 _WorldSpaceLightPos0;
float _RampSmoothing;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 glstate_lightmodel_ambient;
float4 _LightColor0;
float4 _EdgeColor;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_in_TEXCOORD1;
static float v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float2 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float v_vs_TEXCOORD2 : TEXCOORD1;
    float3 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat2;
static float v_u_xlat6;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat0 += transpose(unity_ObjectToWorld)[3];
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_u_xlat0.x = v_in_TEXCOORD1.x;
    v_u_xlat0.x = clamp(v_u_xlat0.x, 0.0f, 1.0f);
    v_u_xlat2 = (-v_u_xlat0.x) + 1.0f;
    v_u_xlat0.x = (-v_u_xlat2) + v_u_xlat0.x;
    v_vs_TEXCOORD2 = (_RevealDirection * v_u_xlat0.x) + v_u_xlat2;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD1 = v_u_xlat6.xxx * v_u_xlat0.xyz;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float f_vs_TEXCOORD2 : TEXCOORD1;
    float3 f_vs_TEXCOORD1 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float f_u_xlat8;
static bool f_u_xlatb4;
static float3 f_u_xlat16_1;
static float f_u_xlat12;
static float3 f_u_xlat2;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0.x = _EdgeRise + (-1.0f);
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat4.x = _EdgeProgress + (-1.0f);
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = 1.0f / _EdgeRise;
    f_u_xlat4.x *= _EdgeProgress;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = (-_EdgeWidthMin) + _EdgeWidthMax;
    f_u_xlat0.x = (f_u_xlat0.x * f_u_xlat4.x) + _EdgeWidthMin;
    f_u_xlat4.x = (-f_vs_TEXCOORD2) + _Reveal;
    f_u_xlat8 = (f_u_xlat0.x * 0.5f) + f_u_xlat4.x;
    f_u_xlat0.x = max(f_u_xlat0.x, 9.9999997473787516355514526367188e-05f);
    f_u_xlat0.x = abs(f_u_xlat4.x) / f_u_xlat0.x;
    f_u_xlat0.x = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat0.x = max(f_u_xlat0.x, 0.0f);
    f_u_xlat0.x *= _EdgeIntensity;
    f_u_xlatb4 = f_u_xlat8 < 0.0f;
    if (f_u_xlatb4)
    {
        discard;
    }
    f_u_xlat4.x = dot(f_vs_TEXCOORD1, f_vs_TEXCOORD1);
    f_u_xlat4.x = rsqrt(f_u_xlat4.x);
    f_u_xlat4 = f_u_xlat4.xxx * f_vs_TEXCOORD1;
    f_u_xlat16_1.x = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_1.x = rsqrt(f_u_xlat16_1.x);
    f_u_xlat16_1 = f_u_xlat16_1.xxx * _WorldSpaceLightPos0.xyz;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat16_1);
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (-_RampSmoothing) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat8) + f_u_xlat4.x;
    f_u_xlat12 = _RampSmoothing + _RampThreshold;
    f_u_xlat8 = (-f_u_xlat8) + f_u_xlat12;
    f_u_xlat8 = 1.0f / f_u_xlat8;
    f_u_xlat4.x = f_u_xlat8 * f_u_xlat4.x;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat2 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat4 = (f_u_xlat4.xxx * f_u_xlat2) + _SColor.xyz;
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat2 = f_u_xlat16_2 * _Color.xyz;
    f_u_xlat16_1 = f_u_xlat4 * f_u_xlat2;
    f_u_xlat16_3 = glstate_lightmodel_ambient.xyz + glstate_lightmodel_ambient.xyz;
    f_u_xlat16_3 = f_u_xlat2 * f_u_xlat16_3;
    f_u_xlat16_1 = (f_u_xlat16_1 * _LightColor0.xyz) + f_u_xlat16_3;
    f_u_xlat0 = (_EdgeColor.xyz * f_u_xlat0.xxx) + f_u_xlat16_1;
    f_SV_Target0.x = f_u_xlat0.x;
    f_SV_Target0.y = f_u_xlat0.y;
    f_SV_Target0.z = f_u_xlat0.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToShadow[4];
float _RevealDirection;
float4 _MainTex_ST;
float _EdgeRise;
float _EdgeProgress;
float _EdgeWidthMin;
float _EdgeWidthMax;
float _Reveal;
float _EdgeIntensity;
float4 _WorldSpaceLightPos0;
float _RampSmoothing;
float _RampThreshold;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _LightShadowData;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 glstate_lightmodel_ambient;
float4 _LightColor0;
float4 _EdgeColor;
#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
#else
sampler2D _ShadowMapTexture;
#endif
float4 _ScreenParams;
#include "RecoveryDirectionalShadows.cginc"

float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_in_TEXCOORD1;
static float v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float2 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float v_vs_TEXCOORD2 : TEXCOORD1;
    float3 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD3 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat4;
static float v_u_xlat10;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_u_xlat1.x = v_in_TEXCOORD1.x;
    v_u_xlat1.x = clamp(v_u_xlat1.x, 0.0f, 1.0f);
    v_u_xlat4 = (-v_u_xlat1.x) + 1.0f;
    v_u_xlat1.x = (-v_u_xlat4) + v_u_xlat1.x;
    v_vs_TEXCOORD2 = (_RevealDirection * v_u_xlat1.x) + v_u_xlat4;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_vs_TEXCOORD1 = v_u_xlat10.xxx * v_u_xlat1.xyz;
    v_u_xlat1 = v_u_xlat0.yyyy * RecoveryColumn_unity_WorldToShadow(1);
    v_u_xlat1 = (RecoveryColumn_unity_WorldToShadow(0) * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (RecoveryColumn_unity_WorldToShadow(2) * v_u_xlat0.zzzz) + v_u_xlat1;
    v_vs_TEXCOORD3 = (RecoveryColumn_unity_WorldToShadow(3) * v_u_xlat0.wwww) + v_u_xlat1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float f_vs_TEXCOORD2 : TEXCOORD1;
    float3 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD3 : TEXCOORD3;
    float4 recoveryPixelPosition : SV_Position;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float f_u_xlat8;
static bool f_u_xlatb4;
static float3 f_u_xlat16_1;
static float f_u_xlat12;
static float3 f_u_xlat2;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0.x = _EdgeRise + (-1.0f);
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat4.x = _EdgeProgress + (-1.0f);
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = 1.0f / _EdgeRise;
    f_u_xlat4.x *= _EdgeProgress;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = (-_EdgeWidthMin) + _EdgeWidthMax;
    f_u_xlat0.x = (f_u_xlat0.x * f_u_xlat4.x) + _EdgeWidthMin;
    f_u_xlat4.x = (-f_vs_TEXCOORD2) + _Reveal;
    f_u_xlat8 = (f_u_xlat0.x * 0.5f) + f_u_xlat4.x;
    f_u_xlat0.x = max(f_u_xlat0.x, 9.9999997473787516355514526367188e-05f);
    f_u_xlat0.x = abs(f_u_xlat4.x) / f_u_xlat0.x;
    f_u_xlat0.x = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat0.x = max(f_u_xlat0.x, 0.0f);
    f_u_xlat0.x *= _EdgeIntensity;
    f_u_xlatb4 = f_u_xlat8 < 0.0f;
    if (f_u_xlatb4)
    {
        discard;
    }
    f_u_xlat4.x = dot(f_vs_TEXCOORD1, f_vs_TEXCOORD1);
    f_u_xlat4.x = rsqrt(f_u_xlat4.x);
    f_u_xlat4 = f_u_xlat4.xxx * f_vs_TEXCOORD1;
    f_u_xlat16_1.x = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_1.x = rsqrt(f_u_xlat16_1.x);
    f_u_xlat16_1 = f_u_xlat16_1.xxx * _WorldSpaceLightPos0.xyz;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat16_1);
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (-_RampSmoothing) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat8) + f_u_xlat4.x;
    f_u_xlat12 = _RampSmoothing + _RampThreshold;
    f_u_xlat8 = (-f_u_xlat8) + f_u_xlat12;
    f_u_xlat8 = 1.0f / f_u_xlat8;
    f_u_xlat4.x = f_u_xlat8 * f_u_xlat4.x;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    float3 txVec0 = float3(f_vs_TEXCOORD3.xy, f_vs_TEXCOORD3.z);
    f_u_xlat8 = RecoverySampleDirectionalShadow(txVec0);
    f_u_xlat12 = (-RecoveryDirectionalShadowData().x) + 1.0f;
    f_u_xlat8 = (f_u_xlat8 * f_u_xlat12) + RecoveryDirectionalShadowData().x;
    f_u_xlat4.x = f_u_xlat8 * f_u_xlat4.x;
    f_u_xlat2 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat4 = (f_u_xlat4.xxx * f_u_xlat2) + _SColor.xyz;
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat2 = f_u_xlat16_2 * _Color.xyz;
    f_u_xlat16_1 = f_u_xlat4 * f_u_xlat2;
    f_u_xlat16_3 = glstate_lightmodel_ambient.xyz + glstate_lightmodel_ambient.xyz;
    f_u_xlat16_3 = f_u_xlat2 * f_u_xlat16_3;
    f_u_xlat16_1 = (f_u_xlat16_1 * _LightColor0.xyz) + f_u_xlat16_3;
    f_u_xlat0 = (_EdgeColor.xyz * f_u_xlat0.xxx) + f_u_xlat16_1;
    f_SV_Target0.x = f_u_xlat0.x;
    f_SV_Target0.y = f_u_xlat0.y;
    f_SV_Target0.z = f_u_xlat0.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    RecoveryShadowScreenUV = stage_input.recoveryPixelPosition.xy / _ScreenParams.xy;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float _RevealDirection;
float4 _MainTex_ST;
float _EdgeRise;
float _EdgeProgress;
float _EdgeWidthMin;
float _EdgeWidthMax;
float _Reveal;
float _EdgeIntensity;
float4 _WorldSpaceLightPos0;
float _RampSmoothing;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 glstate_lightmodel_ambient;
float4 _LightColor0;
float4 _EdgeColor;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_in_TEXCOORD1;
static float v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float2 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float v_vs_TEXCOORD2 : TEXCOORD1;
    float3 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat2;
static float v_u_xlat6;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat0 += transpose(unity_ObjectToWorld)[3];
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_u_xlat0.x = v_in_TEXCOORD1.x;
    v_u_xlat0.x = clamp(v_u_xlat0.x, 0.0f, 1.0f);
    v_u_xlat2 = (-v_u_xlat0.x) + 1.0f;
    v_u_xlat0.x = (-v_u_xlat2) + v_u_xlat0.x;
    v_vs_TEXCOORD2 = (_RevealDirection * v_u_xlat0.x) + v_u_xlat2;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD1 = v_u_xlat6.xxx * v_u_xlat0.xyz;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float f_vs_TEXCOORD2 : TEXCOORD1;
    float3 f_vs_TEXCOORD1 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float f_u_xlat8;
static bool f_u_xlatb4;
static float3 f_u_xlat16_1;
static float f_u_xlat12;
static float3 f_u_xlat2;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0.x = _EdgeRise + (-1.0f);
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat4.x = _EdgeProgress + (-1.0f);
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = 1.0f / _EdgeRise;
    f_u_xlat4.x *= _EdgeProgress;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = (-_EdgeWidthMin) + _EdgeWidthMax;
    f_u_xlat0.x = (f_u_xlat0.x * f_u_xlat4.x) + _EdgeWidthMin;
    f_u_xlat4.x = (-f_vs_TEXCOORD2) + _Reveal;
    f_u_xlat8 = (f_u_xlat0.x * 0.5f) + f_u_xlat4.x;
    f_u_xlat0.x = max(f_u_xlat0.x, 9.9999997473787516355514526367188e-05f);
    f_u_xlat0.x = abs(f_u_xlat4.x) / f_u_xlat0.x;
    f_u_xlat0.x = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat0.x = max(f_u_xlat0.x, 0.0f);
    f_u_xlat0.x *= _EdgeIntensity;
    f_u_xlatb4 = f_u_xlat8 < 0.0f;
    if (f_u_xlatb4)
    {
        discard;
    }
    f_u_xlat4.x = dot(f_vs_TEXCOORD1, f_vs_TEXCOORD1);
    f_u_xlat4.x = rsqrt(f_u_xlat4.x);
    f_u_xlat4 = f_u_xlat4.xxx * f_vs_TEXCOORD1;
    f_u_xlat16_1.x = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_1.x = rsqrt(f_u_xlat16_1.x);
    f_u_xlat16_1 = f_u_xlat16_1.xxx * _WorldSpaceLightPos0.xyz;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat16_1);
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (-_RampSmoothing) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat8) + f_u_xlat4.x;
    f_u_xlat12 = _RampSmoothing + _RampThreshold;
    f_u_xlat8 = (-f_u_xlat8) + f_u_xlat12;
    f_u_xlat8 = 1.0f / f_u_xlat8;
    f_u_xlat4.x = f_u_xlat8 * f_u_xlat4.x;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat2 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat4 = (f_u_xlat4.xxx * f_u_xlat2) + _SColor.xyz;
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat2 = f_u_xlat16_2 * _Color.xyz;
    f_u_xlat16_1 = f_u_xlat4 * f_u_xlat2;
    f_u_xlat16_3 = glstate_lightmodel_ambient.xyz + glstate_lightmodel_ambient.xyz;
    f_u_xlat16_3 = f_u_xlat2 * f_u_xlat16_3;
    f_u_xlat16_1 = (f_u_xlat16_1 * _LightColor0.xyz) + f_u_xlat16_3;
    f_u_xlat0 = (_EdgeColor.xyz * f_u_xlat0.xxx) + f_u_xlat16_1;
    f_SV_Target0.x = f_u_xlat0.x;
    f_SV_Target0.y = f_u_xlat0.y;
    f_SV_Target0.z = f_u_xlat0.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToShadow[4];
float _RevealDirection;
float4 _MainTex_ST;
float _EdgeRise;
float _EdgeProgress;
float _EdgeWidthMin;
float _EdgeWidthMax;
float _Reveal;
float _EdgeIntensity;
float4 _WorldSpaceLightPos0;
float _RampSmoothing;
float _RampThreshold;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _LightShadowData;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 glstate_lightmodel_ambient;
float4 _LightColor0;
float4 _EdgeColor;
#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
#else
sampler2D _ShadowMapTexture;
#endif
float4 _ScreenParams;
#include "RecoveryDirectionalShadows.cginc"

float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_in_TEXCOORD1;
static float v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float2 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float v_vs_TEXCOORD2 : TEXCOORD1;
    float3 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD3 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat4;
static float v_u_xlat10;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_u_xlat1.x = v_in_TEXCOORD1.x;
    v_u_xlat1.x = clamp(v_u_xlat1.x, 0.0f, 1.0f);
    v_u_xlat4 = (-v_u_xlat1.x) + 1.0f;
    v_u_xlat1.x = (-v_u_xlat4) + v_u_xlat1.x;
    v_vs_TEXCOORD2 = (_RevealDirection * v_u_xlat1.x) + v_u_xlat4;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_vs_TEXCOORD1 = v_u_xlat10.xxx * v_u_xlat1.xyz;
    v_u_xlat1 = v_u_xlat0.yyyy * RecoveryColumn_unity_WorldToShadow(1);
    v_u_xlat1 = (RecoveryColumn_unity_WorldToShadow(0) * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (RecoveryColumn_unity_WorldToShadow(2) * v_u_xlat0.zzzz) + v_u_xlat1;
    v_vs_TEXCOORD3 = (RecoveryColumn_unity_WorldToShadow(3) * v_u_xlat0.wwww) + v_u_xlat1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float f_vs_TEXCOORD2 : TEXCOORD1;
    float3 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD3 : TEXCOORD3;
    float4 recoveryPixelPosition : SV_Position;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float f_u_xlat8;
static bool f_u_xlatb4;
static float3 f_u_xlat16_1;
static float f_u_xlat12;
static float3 f_u_xlat2;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0.x = _EdgeRise + (-1.0f);
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat4.x = _EdgeProgress + (-1.0f);
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = 1.0f / _EdgeRise;
    f_u_xlat4.x *= _EdgeProgress;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = (-_EdgeWidthMin) + _EdgeWidthMax;
    f_u_xlat0.x = (f_u_xlat0.x * f_u_xlat4.x) + _EdgeWidthMin;
    f_u_xlat4.x = (-f_vs_TEXCOORD2) + _Reveal;
    f_u_xlat8 = (f_u_xlat0.x * 0.5f) + f_u_xlat4.x;
    f_u_xlat0.x = max(f_u_xlat0.x, 9.9999997473787516355514526367188e-05f);
    f_u_xlat0.x = abs(f_u_xlat4.x) / f_u_xlat0.x;
    f_u_xlat0.x = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat0.x = max(f_u_xlat0.x, 0.0f);
    f_u_xlat0.x *= _EdgeIntensity;
    f_u_xlatb4 = f_u_xlat8 < 0.0f;
    if (f_u_xlatb4)
    {
        discard;
    }
    f_u_xlat4.x = dot(f_vs_TEXCOORD1, f_vs_TEXCOORD1);
    f_u_xlat4.x = rsqrt(f_u_xlat4.x);
    f_u_xlat4 = f_u_xlat4.xxx * f_vs_TEXCOORD1;
    f_u_xlat16_1.x = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_1.x = rsqrt(f_u_xlat16_1.x);
    f_u_xlat16_1 = f_u_xlat16_1.xxx * _WorldSpaceLightPos0.xyz;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat16_1);
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (-_RampSmoothing) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat8) + f_u_xlat4.x;
    f_u_xlat12 = _RampSmoothing + _RampThreshold;
    f_u_xlat8 = (-f_u_xlat8) + f_u_xlat12;
    f_u_xlat8 = 1.0f / f_u_xlat8;
    f_u_xlat4.x = f_u_xlat8 * f_u_xlat4.x;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    float3 txVec0 = float3(f_vs_TEXCOORD3.xy, f_vs_TEXCOORD3.z);
    f_u_xlat8 = RecoverySampleDirectionalShadow(txVec0);
    f_u_xlat12 = (-RecoveryDirectionalShadowData().x) + 1.0f;
    f_u_xlat8 = (f_u_xlat8 * f_u_xlat12) + RecoveryDirectionalShadowData().x;
    f_u_xlat4.x = f_u_xlat8 * f_u_xlat4.x;
    f_u_xlat2 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat4 = (f_u_xlat4.xxx * f_u_xlat2) + _SColor.xyz;
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat2 = f_u_xlat16_2 * _Color.xyz;
    f_u_xlat16_1 = f_u_xlat4 * f_u_xlat2;
    f_u_xlat16_3 = glstate_lightmodel_ambient.xyz + glstate_lightmodel_ambient.xyz;
    f_u_xlat16_3 = f_u_xlat2 * f_u_xlat16_3;
    f_u_xlat16_1 = (f_u_xlat16_1 * _LightColor0.xyz) + f_u_xlat16_3;
    f_u_xlat0 = (_EdgeColor.xyz * f_u_xlat0.xxx) + f_u_xlat16_1;
    f_SV_Target0.x = f_u_xlat0.x;
    f_SV_Target0.y = f_u_xlat0.y;
    f_SV_Target0.z = f_u_xlat0.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    RecoveryShadowScreenUV = stage_input.recoveryPixelPosition.xy / _ScreenParams.xy;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float _RevealDirection;
float4 _MainTex_ST;
float _EdgeRise;
float _EdgeProgress;
float _EdgeWidthMin;
float _EdgeWidthMax;
float _Reveal;
float _EdgeIntensity;
float4 _WorldSpaceLightPos0;
float _RampSmoothing;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 glstate_lightmodel_ambient;
float4 _LightColor0;
float4 _EdgeColor;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_in_TEXCOORD1;
static float v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float2 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float v_vs_TEXCOORD2 : TEXCOORD1;
    float3 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat2;
static float v_u_xlat6;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat0 += transpose(unity_ObjectToWorld)[3];
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_u_xlat0.x = v_in_TEXCOORD1.x;
    v_u_xlat0.x = clamp(v_u_xlat0.x, 0.0f, 1.0f);
    v_u_xlat2 = (-v_u_xlat0.x) + 1.0f;
    v_u_xlat0.x = (-v_u_xlat2) + v_u_xlat0.x;
    v_vs_TEXCOORD2 = (_RevealDirection * v_u_xlat0.x) + v_u_xlat2;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD1 = v_u_xlat6.xxx * v_u_xlat0.xyz;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float f_vs_TEXCOORD2 : TEXCOORD1;
    float3 f_vs_TEXCOORD1 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float f_u_xlat8;
static bool f_u_xlatb4;
static float3 f_u_xlat16_1;
static float f_u_xlat12;
static float3 f_u_xlat2;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0.x = _EdgeRise + (-1.0f);
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat4.x = _EdgeProgress + (-1.0f);
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = 1.0f / _EdgeRise;
    f_u_xlat4.x *= _EdgeProgress;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = (-_EdgeWidthMin) + _EdgeWidthMax;
    f_u_xlat0.x = (f_u_xlat0.x * f_u_xlat4.x) + _EdgeWidthMin;
    f_u_xlat4.x = (-f_vs_TEXCOORD2) + _Reveal;
    f_u_xlat8 = (f_u_xlat0.x * 0.5f) + f_u_xlat4.x;
    f_u_xlat0.x = max(f_u_xlat0.x, 9.9999997473787516355514526367188e-05f);
    f_u_xlat0.x = abs(f_u_xlat4.x) / f_u_xlat0.x;
    f_u_xlat0.x = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat0.x = max(f_u_xlat0.x, 0.0f);
    f_u_xlat0.x *= _EdgeIntensity;
    f_u_xlatb4 = f_u_xlat8 < 0.0f;
    if (f_u_xlatb4)
    {
        discard;
    }
    f_u_xlat4.x = dot(f_vs_TEXCOORD1, f_vs_TEXCOORD1);
    f_u_xlat4.x = rsqrt(f_u_xlat4.x);
    f_u_xlat4 = f_u_xlat4.xxx * f_vs_TEXCOORD1;
    f_u_xlat16_1.x = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_1.x = rsqrt(f_u_xlat16_1.x);
    f_u_xlat16_1 = f_u_xlat16_1.xxx * _WorldSpaceLightPos0.xyz;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat16_1);
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (-_RampSmoothing) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat8) + f_u_xlat4.x;
    f_u_xlat12 = _RampSmoothing + _RampThreshold;
    f_u_xlat8 = (-f_u_xlat8) + f_u_xlat12;
    f_u_xlat8 = 1.0f / f_u_xlat8;
    f_u_xlat4.x = f_u_xlat8 * f_u_xlat4.x;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat2 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat4 = (f_u_xlat4.xxx * f_u_xlat2) + _SColor.xyz;
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat2 = f_u_xlat16_2 * _Color.xyz;
    f_u_xlat16_1 = f_u_xlat4 * f_u_xlat2;
    f_u_xlat16_3 = glstate_lightmodel_ambient.xyz + glstate_lightmodel_ambient.xyz;
    f_u_xlat16_3 = f_u_xlat2 * f_u_xlat16_3;
    f_u_xlat16_1 = (f_u_xlat16_1 * _LightColor0.xyz) + f_u_xlat16_3;
    f_u_xlat0 = (_EdgeColor.xyz * f_u_xlat0.xxx) + f_u_xlat16_1;
    f_SV_Target0.x = f_u_xlat0.x;
    f_SV_Target0.y = f_u_xlat0.y;
    f_SV_Target0.z = f_u_xlat0.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float _RevealDirection;
float4 _MainTex_ST;
float _EdgeRise;
float _EdgeProgress;
float _EdgeWidthMin;
float _EdgeWidthMax;
float _Reveal;
float _EdgeIntensity;
float4 _WorldSpaceLightPos0;
float _RampSmoothing;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 glstate_lightmodel_ambient;
float4 _LightColor0;
float4 _EdgeColor;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_in_TEXCOORD1;
static float v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float2 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float v_vs_TEXCOORD2 : TEXCOORD1;
    float3 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat2;
static float v_u_xlat6;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat0 += transpose(unity_ObjectToWorld)[3];
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_u_xlat0.x = v_in_TEXCOORD1.x;
    v_u_xlat0.x = clamp(v_u_xlat0.x, 0.0f, 1.0f);
    v_u_xlat2 = (-v_u_xlat0.x) + 1.0f;
    v_u_xlat0.x = (-v_u_xlat2) + v_u_xlat0.x;
    v_vs_TEXCOORD2 = (_RevealDirection * v_u_xlat0.x) + v_u_xlat2;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD1 = v_u_xlat6.xxx * v_u_xlat0.xyz;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float f_vs_TEXCOORD2 : TEXCOORD1;
    float3 f_vs_TEXCOORD1 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float f_u_xlat8;
static bool f_u_xlatb4;
static float3 f_u_xlat16_1;
static float f_u_xlat12;
static float3 f_u_xlat2;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0.x = _EdgeRise + (-1.0f);
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat4.x = _EdgeProgress + (-1.0f);
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = 1.0f / _EdgeRise;
    f_u_xlat4.x *= _EdgeProgress;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat4.x = (-_EdgeWidthMin) + _EdgeWidthMax;
    f_u_xlat0.x = (f_u_xlat0.x * f_u_xlat4.x) + _EdgeWidthMin;
    f_u_xlat4.x = (-f_vs_TEXCOORD2) + _Reveal;
    f_u_xlat8 = (f_u_xlat0.x * 0.5f) + f_u_xlat4.x;
    f_u_xlat0.x = max(f_u_xlat0.x, 9.9999997473787516355514526367188e-05f);
    f_u_xlat0.x = abs(f_u_xlat4.x) / f_u_xlat0.x;
    f_u_xlat0.x = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat0.x = max(f_u_xlat0.x, 0.0f);
    f_u_xlat0.x *= _EdgeIntensity;
    f_u_xlatb4 = f_u_xlat8 < 0.0f;
    if (f_u_xlatb4)
    {
        discard;
    }
    f_u_xlat4.x = dot(f_vs_TEXCOORD1, f_vs_TEXCOORD1);
    f_u_xlat4.x = rsqrt(f_u_xlat4.x);
    f_u_xlat4 = f_u_xlat4.xxx * f_vs_TEXCOORD1;
    f_u_xlat16_1.x = dot(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_1.x = rsqrt(f_u_xlat16_1.x);
    f_u_xlat16_1 = f_u_xlat16_1.xxx * _WorldSpaceLightPos0.xyz;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat16_1);
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (-_RampSmoothing) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat8) + f_u_xlat4.x;
    f_u_xlat12 = _RampSmoothing + _RampThreshold;
    f_u_xlat8 = (-f_u_xlat8) + f_u_xlat12;
    f_u_xlat8 = 1.0f / f_u_xlat8;
    f_u_xlat4.x = f_u_xlat8 * f_u_xlat4.x;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4.x * (-2.0f)) + 3.0f;
    f_u_xlat4.x *= f_u_xlat4.x;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat2 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat4 = (f_u_xlat4.xxx * f_u_xlat2) + _SColor.xyz;
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat2 = f_u_xlat16_2 * _Color.xyz;
    f_u_xlat16_1 = f_u_xlat4 * f_u_xlat2;
    f_u_xlat16_3 = glstate_lightmodel_ambient.xyz + glstate_lightmodel_ambient.xyz;
    f_u_xlat16_3 = f_u_xlat2 * f_u_xlat16_3;
    f_u_xlat16_1 = (f_u_xlat16_1 * _LightColor0.xyz) + f_u_xlat16_3;
    f_u_xlat0 = (_EdgeColor.xyz * f_u_xlat0.xxx) + f_u_xlat16_1;
    f_SV_Target0.x = f_u_xlat0.x;
    f_SV_Target0.y = f_u_xlat0.y;
    f_SV_Target0.z = f_u_xlat0.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#else
#error This keyword combination was not present in the shipped APK shader.
#endif
ENDHLSL
}
Pass {
Name ""
Tags { "LightMode"="ShadowCaster" "Queue"="Geometry" "RenderType"="Opaque" "SHADOWSUPPORT"="true" }
Cull Back
ZWrite On
ZTest LEqual
Blend One Zero, One Zero
ColorMask RGBA
BlendOp Add, Add
Stencil {
Ref 0
ReadMask 255
WriteMask 255
Comp Always
Pass Keep
Fail Keep
ZFail Keep
}
HLSLPROGRAM
#pragma target 4.5
#pragma vertex RecoveryVert
#pragma fragment RecoveryFrag
#pragma multi_compile SHADOWS_CUBE SHADOWS_DEPTH
#if !defined(SHADOWS_CUBE) && defined(SHADOWS_DEPTH)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
float _RevealDirection;
float _EdgeRise;
float _EdgeProgress;
float _EdgeWidthMin;
float _EdgeWidthMax;
float _Reveal;
static float4 v_gl_Position;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;
static float2 v_in_TEXCOORD1;
static float v_vs_TEXCOORD2;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float v_vs_TEXCOORD2 : TEXCOORD0;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;
static float v_u_xlat6;
static float v_u_xlat3;

void v_vert_main()
{
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _55 = v_u_xlat0;
    float3 _57 = v_u_xlat9.xxx * _55.xyz;
    v_u_xlat0.x = _57.x;
    v_u_xlat0.y = _57.y;
    v_u_xlat0.z = _57.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _105 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _105.x;
    v_u_xlat2.y = _105.y;
    v_u_xlat2.z = _105.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _121 = v_u_xlat2;
    float3 _123 = v_u_xlat9.xxx * _121.xyz;
    v_u_xlat2.x = _123.x;
    v_u_xlat2.y = _123.y;
    v_u_xlat2.z = _123.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _149 = v_u_xlat0;
    float3 _157 = ((-_149.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _157.x;
    v_u_xlat0.y = _157.y;
    v_u_xlat0.z = _157.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _173;
    if (v_u_xlatb9)
    {
        _173 = v_u_xlat0.xyz;
    }
    else
    {
        _173 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _173.x;
    v_u_xlat0.y = _173.y;
    v_u_xlat0.z = _173.z;
    v_u_xlat2 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat2;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat2;
    v_u_xlat0 = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    v_u_xlat1.x = unity_LightShadowBias.x / v_u_xlat0.w;
    v_u_xlat1.x = clamp(v_u_xlat1.x, 0.0f, 1.0f);
    v_u_xlat6 = v_u_xlat0.z + v_u_xlat1.x;
    v_u_xlat1.x = max(-v_u_xlat0.w, v_u_xlat6);
    v_gl_Position.x = v_u_xlat0.xyw.x;
    v_gl_Position.y = v_u_xlat0.xyw.y;
    v_gl_Position.w = v_u_xlat0.xyw.z;
    v_u_xlat0.x = (-v_u_xlat6) + v_u_xlat1.x;
    v_gl_Position.z = (unity_LightShadowBias.y * v_u_xlat0.x) + v_u_xlat6;
    v_u_xlat0.x = v_in_TEXCOORD1.x;
    v_u_xlat0.x = clamp(v_u_xlat0.x, 0.0f, 1.0f);
    v_u_xlat3 = (-v_u_xlat0.x) + 1.0f;
    v_u_xlat0.x = (-v_u_xlat3) + v_u_xlat0.x;
    v_vs_TEXCOORD2 = (_RevealDirection * v_u_xlat0.x) + v_u_xlat3;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float f_vs_TEXCOORD2 : TEXCOORD0;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float f_u_xlat0;
static float f_u_xlat1;
static float f_u_xlat2;
static bool f_u_xlatb0;

void f_frag_main()
{
    f_u_xlat0 = _EdgeRise + (-1.0f);
    f_u_xlat0 = 1.0f / f_u_xlat0;
    f_u_xlat1 = _EdgeProgress + (-1.0f);
    f_u_xlat0 *= f_u_xlat1;
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f, 1.0f);
    f_u_xlat1 = (f_u_xlat0 * (-2.0f)) + 3.0f;
    f_u_xlat0 *= f_u_xlat0;
    f_u_xlat0 *= f_u_xlat1;
    f_u_xlat1 = 1.0f / _EdgeRise;
    f_u_xlat1 *= _EdgeProgress;
    f_u_xlat1 = clamp(f_u_xlat1, 0.0f, 1.0f);
    f_u_xlat2 = (f_u_xlat1 * (-2.0f)) + 3.0f;
    f_u_xlat1 *= f_u_xlat1;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat0 *= f_u_xlat1;
    f_u_xlat1 = (-_EdgeWidthMin) + _EdgeWidthMax;
    f_u_xlat0 = (f_u_xlat0 * f_u_xlat1) + _EdgeWidthMin;
    f_u_xlat1 = (-f_vs_TEXCOORD2) + _Reveal;
    f_u_xlat0 = (f_u_xlat0 * 0.5f) + f_u_xlat1;
    f_u_xlatb0 = f_u_xlat0 < 0.0f;
    if (f_u_xlatb0)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
float _RevealDirection;
float _EdgeRise;
float _EdgeProgress;
float _EdgeWidthMin;
float _EdgeWidthMax;
float _Reveal;
static float4 v_gl_Position;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;
static float2 v_in_TEXCOORD1;
static float v_vs_TEXCOORD2;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float v_vs_TEXCOORD2 : TEXCOORD0;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;
static float v_u_xlat3;

void v_vert_main()
{
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _55 = v_u_xlat0;
    float3 _57 = v_u_xlat9.xxx * _55.xyz;
    v_u_xlat0.x = _57.x;
    v_u_xlat0.y = _57.y;
    v_u_xlat0.z = _57.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _105 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _105.x;
    v_u_xlat2.y = _105.y;
    v_u_xlat2.z = _105.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _121 = v_u_xlat2;
    float3 _123 = v_u_xlat9.xxx * _121.xyz;
    v_u_xlat2.x = _123.x;
    v_u_xlat2.y = _123.y;
    v_u_xlat2.z = _123.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _149 = v_u_xlat0;
    float3 _157 = ((-_149.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _157.x;
    v_u_xlat0.y = _157.y;
    v_u_xlat0.z = _157.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _173;
    if (v_u_xlatb9)
    {
        _173 = v_u_xlat0.xyz;
    }
    else
    {
        _173 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _173.x;
    v_u_xlat0.y = _173.y;
    v_u_xlat0.z = _173.z;
    v_u_xlat2 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat2;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat2;
    v_u_xlat0 = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    v_u_xlat1.x = max(-v_u_xlat0.w, v_u_xlat0.z);
    v_u_xlat1.x = (-v_u_xlat0.z) + v_u_xlat1.x;
    v_gl_Position.z = (unity_LightShadowBias.y * v_u_xlat1.x) + v_u_xlat0.z;
    v_gl_Position.x = v_u_xlat0.xyw.x;
    v_gl_Position.y = v_u_xlat0.xyw.y;
    v_gl_Position.w = v_u_xlat0.xyw.z;
    v_u_xlat0.x = v_in_TEXCOORD1.x;
    v_u_xlat0.x = clamp(v_u_xlat0.x, 0.0f, 1.0f);
    v_u_xlat3 = (-v_u_xlat0.x) + 1.0f;
    v_u_xlat0.x = (-v_u_xlat3) + v_u_xlat0.x;
    v_vs_TEXCOORD2 = (_RevealDirection * v_u_xlat0.x) + v_u_xlat3;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float f_vs_TEXCOORD2 : TEXCOORD0;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float f_u_xlat0;
static float f_u_xlat1;
static float f_u_xlat2;
static bool f_u_xlatb0;

void f_frag_main()
{
    f_u_xlat0 = _EdgeRise + (-1.0f);
    f_u_xlat0 = 1.0f / f_u_xlat0;
    f_u_xlat1 = _EdgeProgress + (-1.0f);
    f_u_xlat0 *= f_u_xlat1;
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f, 1.0f);
    f_u_xlat1 = (f_u_xlat0 * (-2.0f)) + 3.0f;
    f_u_xlat0 *= f_u_xlat0;
    f_u_xlat0 *= f_u_xlat1;
    f_u_xlat1 = 1.0f / _EdgeRise;
    f_u_xlat1 *= _EdgeProgress;
    f_u_xlat1 = clamp(f_u_xlat1, 0.0f, 1.0f);
    f_u_xlat2 = (f_u_xlat1 * (-2.0f)) + 3.0f;
    f_u_xlat1 *= f_u_xlat1;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat0 *= f_u_xlat1;
    f_u_xlat1 = (-_EdgeWidthMin) + _EdgeWidthMax;
    f_u_xlat0 = (f_u_xlat0 * f_u_xlat1) + _EdgeWidthMin;
    f_u_xlat1 = (-f_vs_TEXCOORD2) + _Reveal;
    f_u_xlat0 = (f_u_xlat0 * 0.5f) + f_u_xlat1;
    f_u_xlatb0 = f_u_xlat0 < 0.0f;
    if (f_u_xlatb0)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#else
#error This keyword combination was not present in the shipped APK shader.
#endif
ENDHLSL
}
}
}
