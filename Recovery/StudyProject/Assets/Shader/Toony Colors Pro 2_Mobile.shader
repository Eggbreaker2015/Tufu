Shader "Toony Colors Pro 2/Mobile" {
Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_HColor ("Highlight Color", Vector) = (0.785,0.785,0.785,1)
		_SColor ("Shadow Color", Vector) = (0.195,0.195,0.195,1)
		_MainTex ("Main Texture (RGB) Spec/MatCap Mask (A) ", 2D) = "white" {}
		[TCP2Gradient] _Ramp ("#RAMPT# Toon Ramp (RGB)", 2D) = "gray" {}
		_RampThreshold ("#RAMPF# Ramp Threshold", Range(0, 1)) = 0.5
		_RampSmooth ("#RAMPF# Ramp Smoothing", Range(0.01, 1)) = 0.1
		_BumpMap ("#NORM# Normal map (RGB)", 2D) = "bump" {}
	}
// Recovered from the APK GLES programs; generated HLSL, not original author source.
SubShader {
Tags { "RenderType"="Opaque" }
Pass {
Name "FORWARD"
Tags { "LightMode"="ForwardBase" "RenderType"="Opaque" "SHADOWSUPPORT"="true" }
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
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
float4 _MainTex_ST;
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 unity_4LightPosY0;
float4 unity_4LightPosX0;
float4 unity_4LightPosZ0;
float4 unity_4LightAtten0;
float4 unity_LightColor[8];
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 _LightColor0;
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_TEXCOORD0;
static float2 v_vs_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
    float3 v_vs_TEXCOORD3 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat18;
static float3 v_u_xlat16_3;
static float4 v_u_xlat16_2;
static float3 v_u_xlat16_4;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float4 v_vs_TEXCOORD5;
static float4 v_vs_TEXCOORD6;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    float4 _54 = v_u_xlat0;
    float3 _56 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + _54.xyz;
    v_u_xlat0.x = _56.x;
    v_u_xlat0.y = _56.y;
    v_u_xlat0.z = _56.z;
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    float2 _110 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = _110.x;
    v_u_xlat1.y = _110.y;
    v_vs_TEXCOORD0 = v_u_xlat1.xy;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat18 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat18 = rsqrt(v_u_xlat18);
    float4 _150 = v_u_xlat1;
    float3 _152 = v_u_xlat18.xxx * _150.xyz;
    v_u_xlat1.x = _152.x;
    v_u_xlat1.y = _152.y;
    v_u_xlat1.z = _152.z;
    v_vs_TEXCOORD1 = v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = v_u_xlat0.xyz;
    v_u_xlat16_3.x = v_u_xlat1.y * v_u_xlat1.y;
    v_u_xlat16_3.x = (v_u_xlat1.x * v_u_xlat1.x) + (-v_u_xlat16_3.x);
    v_u_xlat16_2 = v_u_xlat1.yzzx * v_u_xlat1.xyzz;
    v_u_xlat16_4.x = dot(unity_SHBr, v_u_xlat16_2);
    v_u_xlat16_4.y = dot(unity_SHBg, v_u_xlat16_2);
    v_u_xlat16_4.z = dot(unity_SHBb, v_u_xlat16_2);
    v_u_xlat16_3 = (unity_SHC.xyz * v_u_xlat16_3.xxx) + v_u_xlat16_4;
    v_u_xlat1.w = 1.0f;
    v_u_xlat16_4.x = dot(unity_SHAr, v_u_xlat1);
    v_u_xlat16_4.y = dot(unity_SHAg, v_u_xlat1);
    v_u_xlat16_4.z = dot(unity_SHAb, v_u_xlat1);
    v_u_xlat16_3 += v_u_xlat16_4;
    v_u_xlat2 = (-v_u_xlat0.yyyy) + unity_4LightPosY0;
    v_u_xlat4 = v_u_xlat1.yyyy * v_u_xlat2;
    v_u_xlat2 *= v_u_xlat2;
    v_u_xlat5 = (-v_u_xlat0.xxxx) + unity_4LightPosX0;
    v_u_xlat0 = (-v_u_xlat0.zzzz) + unity_4LightPosZ0;
    v_u_xlat4 = (v_u_xlat5 * v_u_xlat1.xxxx) + v_u_xlat4;
    v_u_xlat1 = (v_u_xlat0 * v_u_xlat1.zzzz) + v_u_xlat4;
    v_u_xlat2 = (v_u_xlat5 * v_u_xlat5) + v_u_xlat2;
    v_u_xlat0 = (v_u_xlat0 * v_u_xlat0) + v_u_xlat2;
    v_u_xlat0 = max(v_u_xlat0, 9.9999999747524270787835121154785e-07f.xxxx);
    v_u_xlat2 = rsqrt(v_u_xlat0);
    v_u_xlat0 = (v_u_xlat0 * unity_4LightAtten0) + 1.0f.xxxx;
    v_u_xlat0 = 1.0f.xxxx / v_u_xlat0;
    v_u_xlat1 *= v_u_xlat2;
    v_u_xlat1 = max(v_u_xlat1, 0.0f.xxxx);
    v_u_xlat0 *= v_u_xlat1;
    float3 _317 = v_u_xlat0.yyy * unity_LightColor[1].xyz;
    v_u_xlat1.x = _317.x;
    v_u_xlat1.y = _317.y;
    v_u_xlat1.z = _317.z;
    float4 _330 = v_u_xlat1;
    float3 _332 = (unity_LightColor[0].xyz * v_u_xlat0.xxx) + _330.xyz;
    v_u_xlat1.x = _332.x;
    v_u_xlat1.y = _332.y;
    v_u_xlat1.z = _332.z;
    float4 _342 = v_u_xlat0;
    float3 _347 = (unity_LightColor[2].xyz * _342.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _347.x;
    v_u_xlat0.y = _347.y;
    v_u_xlat0.z = _347.z;
    float4 _357 = v_u_xlat0;
    float4 _360 = v_u_xlat0;
    float3 _362 = (unity_LightColor[3].xyz * _357.www) + _360.xyz;
    v_u_xlat0.x = _362.x;
    v_u_xlat0.y = _362.y;
    v_u_xlat0.z = _362.z;
    float4 _369 = v_u_xlat0;
    float3 _372 = _369.xyz + v_u_xlat16_3;
    v_u_xlat0.x = _372.x;
    v_u_xlat0.y = _372.y;
    v_u_xlat0.z = _372.z;
    v_vs_TEXCOORD3 = v_u_xlat0.xyz;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
    v_vs_TEXCOORD6 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD3;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
    float3 f_vs_TEXCOORD3 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat5;
static float3 f_u_xlat1;
static float f_u_xlat10;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_7;
static float3 f_u_xlat16_3;
static float3 f_u_xlat16_0;
static float3 f_u_xlat16_4;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat5 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat5.x = dot(f_u_xlat5, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat5.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat5.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat5 = f_vs_TEXCOORD2.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat5 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat5;
    f_u_xlat5 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat5;
    f_u_xlat5 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat5.xy, f_u_xlat5.z);
    f_u_xlat5.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat10 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat5.x = (f_u_xlat5.x * f_u_xlat10) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat5.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat5.x;
    f_u_xlat16_7.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_7.x = (f_u_xlat16_7.x * 0.5f) + 0.5f;
    f_u_xlat16_7.x = max(f_u_xlat16_7.x, 0.0f);
    f_u_xlat0.x = ((-_RampSmooth) * 0.5f) + _RampThreshold;
    f_u_xlat5.x = (-f_u_xlat0.x) + f_u_xlat16_7.x;
    f_u_xlat10 = (_RampSmooth * 0.5f) + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat10;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat5.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat5.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat5.x;
    f_u_xlat16_2.x *= f_u_xlat0.x;
    f_u_xlat16_7 = (-_HColor.xyz) + _SColor.xyz;
    f_u_xlat16_7 = (_SColor.www * f_u_xlat16_7) + _HColor.xyz;
    f_u_xlat16_3 = (-f_u_xlat16_7) + _HColor.xyz;
    f_u_xlat16_2 = (f_u_xlat16_2.xxx * f_u_xlat16_3) + f_u_xlat16_7;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat16_3 = f_u_xlat16_0 * _Color.xyz;
    f_u_xlat16_4 = f_u_xlat16_3 * _LightColor0.xyz;
    f_u_xlat16_2 *= f_u_xlat16_4;
    float3 _316 = (f_u_xlat16_3 * f_vs_TEXCOORD3) + f_u_xlat16_2;
    f_SV_Target0.x = _316.x;
    f_SV_Target0.y = _316.y;
    f_SV_Target0.z = _316.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
float4 _MainTex_ST;
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 _LightColor0;
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float4 v_in_TEXCOORD0;
static float2 v_vs_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
    float3 v_vs_TEXCOORD3 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat12;
static float3 v_u_xlat16_2;
static float4 v_u_xlat16_1;
static float3 v_u_xlat16_3;
static float4 v_vs_TEXCOORD5;
static float4 v_vs_TEXCOORD6;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    float2 _101 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = _101.x;
    v_u_xlat0.y = _101.y;
    v_vs_TEXCOORD0 = v_u_xlat0.xy;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat12 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat12 = rsqrt(v_u_xlat12);
    float4 _145 = v_u_xlat0;
    float3 _147 = v_u_xlat12.xxx * _145.xyz;
    v_u_xlat0.x = _147.x;
    v_u_xlat0.y = _147.y;
    v_u_xlat0.z = _147.z;
    v_vs_TEXCOORD1 = v_u_xlat0.xyz;
    v_u_xlat16_2.x = v_u_xlat0.y * v_u_xlat0.y;
    v_u_xlat16_2.x = (v_u_xlat0.x * v_u_xlat0.x) + (-v_u_xlat16_2.x);
    v_u_xlat16_1 = v_u_xlat0.yzzx * v_u_xlat0.xyzz;
    v_u_xlat16_3.x = dot(unity_SHBr, v_u_xlat16_1);
    v_u_xlat16_3.y = dot(unity_SHBg, v_u_xlat16_1);
    v_u_xlat16_3.z = dot(unity_SHBb, v_u_xlat16_1);
    v_u_xlat16_2 = (unity_SHC.xyz * v_u_xlat16_2.xxx) + v_u_xlat16_3;
    v_u_xlat0.w = 1.0f;
    v_u_xlat16_3.x = dot(unity_SHAr, v_u_xlat0);
    v_u_xlat16_3.y = dot(unity_SHAg, v_u_xlat0);
    v_u_xlat16_3.z = dot(unity_SHAb, v_u_xlat0);
    v_vs_TEXCOORD3 = v_u_xlat16_2 + v_u_xlat16_3;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
    v_vs_TEXCOORD6 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
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
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD3;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
    float3 f_vs_TEXCOORD3 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat5;
static float3 f_u_xlat1;
static float f_u_xlat10;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_7;
static float3 f_u_xlat16_3;
static float3 f_u_xlat16_0;
static float3 f_u_xlat16_4;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat5 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat5.x = dot(f_u_xlat5, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat5.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat5.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat5 = f_vs_TEXCOORD2.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat5 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat5;
    f_u_xlat5 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat5;
    f_u_xlat5 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat5.xy, f_u_xlat5.z);
    f_u_xlat5.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat10 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat5.x = (f_u_xlat5.x * f_u_xlat10) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat5.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat5.x;
    f_u_xlat16_7.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_7.x = (f_u_xlat16_7.x * 0.5f) + 0.5f;
    f_u_xlat16_7.x = max(f_u_xlat16_7.x, 0.0f);
    f_u_xlat0.x = ((-_RampSmooth) * 0.5f) + _RampThreshold;
    f_u_xlat5.x = (-f_u_xlat0.x) + f_u_xlat16_7.x;
    f_u_xlat10 = (_RampSmooth * 0.5f) + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat10;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat5.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat5.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat5.x;
    f_u_xlat16_2.x *= f_u_xlat0.x;
    f_u_xlat16_7 = (-_HColor.xyz) + _SColor.xyz;
    f_u_xlat16_7 = (_SColor.www * f_u_xlat16_7) + _HColor.xyz;
    f_u_xlat16_3 = (-f_u_xlat16_7) + _HColor.xyz;
    f_u_xlat16_2 = (f_u_xlat16_2.xxx * f_u_xlat16_3) + f_u_xlat16_7;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat16_3 = f_u_xlat16_0 * _Color.xyz;
    f_u_xlat16_4 = f_u_xlat16_3 * _LightColor0.xyz;
    f_u_xlat16_2 *= f_u_xlat16_4;
    float3 _316 = (f_u_xlat16_3 * f_vs_TEXCOORD3) + f_u_xlat16_2;
    f_SV_Target0.x = _316.x;
    f_SV_Target0.y = _316.y;
    f_SV_Target0.z = _316.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4 _MainTex_ST;
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 unity_4LightPosY0;
float4 unity_4LightPosX0;
float4 unity_4LightPosZ0;
float4 unity_4LightAtten0;
float4 unity_LightColor[8];
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 _LightColor0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_TEXCOORD0;
static float2 v_vs_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD3 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat18;
static float3 v_vs_TEXCOORD2;
static float3 v_u_xlat16_3;
static float4 v_u_xlat16_2;
static float3 v_u_xlat16_4;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    float4 _54 = v_u_xlat0;
    float3 _56 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + _54.xyz;
    v_u_xlat0.x = _56.x;
    v_u_xlat0.y = _56.y;
    v_u_xlat0.z = _56.z;
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    float2 _110 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = _110.x;
    v_u_xlat1.y = _110.y;
    v_vs_TEXCOORD0 = v_u_xlat1.xy;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat18 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat18 = rsqrt(v_u_xlat18);
    float4 _150 = v_u_xlat1;
    float3 _152 = v_u_xlat18.xxx * _150.xyz;
    v_u_xlat1.x = _152.x;
    v_u_xlat1.y = _152.y;
    v_u_xlat1.z = _152.z;
    v_vs_TEXCOORD1 = v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = v_u_xlat0.xyz;
    v_u_xlat16_3.x = v_u_xlat1.y * v_u_xlat1.y;
    v_u_xlat16_3.x = (v_u_xlat1.x * v_u_xlat1.x) + (-v_u_xlat16_3.x);
    v_u_xlat16_2 = v_u_xlat1.yzzx * v_u_xlat1.xyzz;
    v_u_xlat16_4.x = dot(unity_SHBr, v_u_xlat16_2);
    v_u_xlat16_4.y = dot(unity_SHBg, v_u_xlat16_2);
    v_u_xlat16_4.z = dot(unity_SHBb, v_u_xlat16_2);
    v_u_xlat16_3 = (unity_SHC.xyz * v_u_xlat16_3.xxx) + v_u_xlat16_4;
    v_u_xlat1.w = 1.0f;
    v_u_xlat16_4.x = dot(unity_SHAr, v_u_xlat1);
    v_u_xlat16_4.y = dot(unity_SHAg, v_u_xlat1);
    v_u_xlat16_4.z = dot(unity_SHAb, v_u_xlat1);
    v_u_xlat16_3 += v_u_xlat16_4;
    v_u_xlat2 = (-v_u_xlat0.yyyy) + unity_4LightPosY0;
    v_u_xlat4 = v_u_xlat1.yyyy * v_u_xlat2;
    v_u_xlat2 *= v_u_xlat2;
    v_u_xlat5 = (-v_u_xlat0.xxxx) + unity_4LightPosX0;
    v_u_xlat0 = (-v_u_xlat0.zzzz) + unity_4LightPosZ0;
    v_u_xlat4 = (v_u_xlat5 * v_u_xlat1.xxxx) + v_u_xlat4;
    v_u_xlat1 = (v_u_xlat0 * v_u_xlat1.zzzz) + v_u_xlat4;
    v_u_xlat2 = (v_u_xlat5 * v_u_xlat5) + v_u_xlat2;
    v_u_xlat0 = (v_u_xlat0 * v_u_xlat0) + v_u_xlat2;
    v_u_xlat0 = max(v_u_xlat0, 9.9999999747524270787835121154785e-07f.xxxx);
    v_u_xlat2 = rsqrt(v_u_xlat0);
    v_u_xlat0 = (v_u_xlat0 * unity_4LightAtten0) + 1.0f.xxxx;
    v_u_xlat0 = 1.0f.xxxx / v_u_xlat0;
    v_u_xlat1 *= v_u_xlat2;
    v_u_xlat1 = max(v_u_xlat1, 0.0f.xxxx);
    v_u_xlat0 *= v_u_xlat1;
    float3 _317 = v_u_xlat0.yyy * unity_LightColor[1].xyz;
    v_u_xlat1.x = _317.x;
    v_u_xlat1.y = _317.y;
    v_u_xlat1.z = _317.z;
    float4 _330 = v_u_xlat1;
    float3 _332 = (unity_LightColor[0].xyz * v_u_xlat0.xxx) + _330.xyz;
    v_u_xlat1.x = _332.x;
    v_u_xlat1.y = _332.y;
    v_u_xlat1.z = _332.z;
    float4 _342 = v_u_xlat0;
    float3 _347 = (unity_LightColor[2].xyz * _342.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _347.x;
    v_u_xlat0.y = _347.y;
    v_u_xlat0.z = _347.z;
    float4 _357 = v_u_xlat0;
    float4 _360 = v_u_xlat0;
    float3 _362 = (unity_LightColor[3].xyz * _357.www) + _360.xyz;
    v_u_xlat0.x = _362.x;
    v_u_xlat0.y = _362.y;
    v_u_xlat0.z = _362.z;
    float4 _369 = v_u_xlat0;
    float3 _372 = _369.xyz + v_u_xlat16_3;
    v_u_xlat0.x = _372.x;
    v_u_xlat0.y = _372.y;
    v_u_xlat0.z = _372.z;
    v_vs_TEXCOORD3 = v_u_xlat0.xyz;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD3;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD3 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat16_0;
static float f_u_xlat1;
static float f_u_xlat5;
static float f_u_xlat9;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_1;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat16_0.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_0.x = (f_u_xlat16_0.x * 0.5f) + 0.5f;
    f_u_xlat16_0.x = max(f_u_xlat16_0.x, 0.0f);
    f_u_xlat1 = ((-_RampSmooth) * 0.5f) + _RampThreshold;
    f_u_xlat5 = f_u_xlat16_0.x + (-f_u_xlat1);
    f_u_xlat9 = (_RampSmooth * 0.5f) + _RampThreshold;
    f_u_xlat1 = (-f_u_xlat1) + f_u_xlat9;
    f_u_xlat1 = 1.0f / f_u_xlat1;
    f_u_xlat1 *= f_u_xlat5;
    f_u_xlat1 = clamp(f_u_xlat1, 0.0f, 1.0f);
    f_u_xlat5 = (f_u_xlat1 * (-2.0f)) + 3.0f;
    f_u_xlat1 *= f_u_xlat1;
    f_u_xlat1 *= f_u_xlat5;
    f_u_xlat16_0 = (-_HColor.xyz) + _SColor.xyz;
    f_u_xlat16_0 = (_SColor.www * f_u_xlat16_0) + _HColor.xyz;
    f_u_xlat16_2 = (-f_u_xlat16_0) + _HColor.xyz;
    f_u_xlat16_0 = (f_u_xlat1.xxx * f_u_xlat16_2) + f_u_xlat16_0;
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat16_2 = f_u_xlat16_1 * _Color.xyz;
    f_u_xlat16_3 = f_u_xlat16_2 * _LightColor0.xyz;
    f_u_xlat16_0 *= f_u_xlat16_3;
    float3 _137 = (f_u_xlat16_2 * f_vs_TEXCOORD3) + f_u_xlat16_0;
    f_SV_Target0.x = _137.x;
    f_SV_Target0.y = _137.y;
    f_SV_Target0.z = _137.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
float4 _MainTex_ST;
float4 unity_4LightPosY0;
float4 unity_4LightPosX0;
float4 unity_4LightPosZ0;
float4 unity_4LightAtten0;
float4 unity_LightColor[8];
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 _LightColor0;
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_TEXCOORD0;
static float2 v_vs_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
    float3 v_vs_TEXCOORD3 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat15;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_vs_TEXCOORD5;
static float4 v_vs_TEXCOORD6;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    float4 _54 = v_u_xlat0;
    float3 _56 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + _54.xyz;
    v_u_xlat0.x = _56.x;
    v_u_xlat0.y = _56.y;
    v_u_xlat0.z = _56.z;
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    float2 _110 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = _110.x;
    v_u_xlat1.y = _110.y;
    v_vs_TEXCOORD0 = v_u_xlat1.xy;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat15 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat15 = rsqrt(v_u_xlat15);
    float4 _150 = v_u_xlat1;
    float3 _152 = v_u_xlat15.xxx * _150.xyz;
    v_u_xlat1.x = _152.x;
    v_u_xlat1.y = _152.y;
    v_u_xlat1.z = _152.z;
    v_vs_TEXCOORD1 = v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = v_u_xlat0.xyz;
    v_u_xlat2 = (-v_u_xlat0.yyyy) + unity_4LightPosY0;
    v_u_xlat3 = v_u_xlat1.yyyy * v_u_xlat2;
    v_u_xlat2 *= v_u_xlat2;
    v_u_xlat4 = (-v_u_xlat0.xxxx) + unity_4LightPosX0;
    v_u_xlat0 = (-v_u_xlat0.zzzz) + unity_4LightPosZ0;
    v_u_xlat3 = (v_u_xlat4 * v_u_xlat1.xxxx) + v_u_xlat3;
    v_u_xlat1 = (v_u_xlat0 * v_u_xlat1.zzzz) + v_u_xlat3;
    v_u_xlat2 = (v_u_xlat4 * v_u_xlat4) + v_u_xlat2;
    v_u_xlat0 = (v_u_xlat0 * v_u_xlat0) + v_u_xlat2;
    v_u_xlat0 = max(v_u_xlat0, 9.9999999747524270787835121154785e-07f.xxxx);
    v_u_xlat2 = rsqrt(v_u_xlat0);
    v_u_xlat0 = (v_u_xlat0 * unity_4LightAtten0) + 1.0f.xxxx;
    v_u_xlat0 = 1.0f.xxxx / v_u_xlat0;
    v_u_xlat1 *= v_u_xlat2;
    v_u_xlat1 = max(v_u_xlat1, 0.0f.xxxx);
    v_u_xlat0 *= v_u_xlat1;
    float3 _249 = v_u_xlat0.yyy * unity_LightColor[1].xyz;
    v_u_xlat1.x = _249.x;
    v_u_xlat1.y = _249.y;
    v_u_xlat1.z = _249.z;
    float4 _262 = v_u_xlat1;
    float3 _264 = (unity_LightColor[0].xyz * v_u_xlat0.xxx) + _262.xyz;
    v_u_xlat1.x = _264.x;
    v_u_xlat1.y = _264.y;
    v_u_xlat1.z = _264.z;
    float4 _274 = v_u_xlat0;
    float3 _279 = (unity_LightColor[2].xyz * _274.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _279.x;
    v_u_xlat0.y = _279.y;
    v_u_xlat0.z = _279.z;
    float4 _289 = v_u_xlat0;
    float4 _292 = v_u_xlat0;
    float3 _294 = (unity_LightColor[3].xyz * _289.www) + _292.xyz;
    v_u_xlat0.x = _294.x;
    v_u_xlat0.y = _294.y;
    v_u_xlat0.z = _294.z;
    v_vs_TEXCOORD3 = v_u_xlat0.xyz;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
    v_vs_TEXCOORD6 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD3;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
    float3 f_vs_TEXCOORD3 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat5;
static float3 f_u_xlat1;
static float f_u_xlat10;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_7;
static float3 f_u_xlat16_3;
static float3 f_u_xlat16_0;
static float3 f_u_xlat16_4;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat5 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat5.x = dot(f_u_xlat5, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat5.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat5.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat5 = f_vs_TEXCOORD2.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat5 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat5;
    f_u_xlat5 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat5;
    f_u_xlat5 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat5.xy, f_u_xlat5.z);
    f_u_xlat5.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat10 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat5.x = (f_u_xlat5.x * f_u_xlat10) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat5.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat5.x;
    f_u_xlat16_7.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_7.x = (f_u_xlat16_7.x * 0.5f) + 0.5f;
    f_u_xlat16_7.x = max(f_u_xlat16_7.x, 0.0f);
    f_u_xlat0.x = ((-_RampSmooth) * 0.5f) + _RampThreshold;
    f_u_xlat5.x = (-f_u_xlat0.x) + f_u_xlat16_7.x;
    f_u_xlat10 = (_RampSmooth * 0.5f) + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat10;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat5.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat5.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat5.x;
    f_u_xlat16_2.x *= f_u_xlat0.x;
    f_u_xlat16_7 = (-_HColor.xyz) + _SColor.xyz;
    f_u_xlat16_7 = (_SColor.www * f_u_xlat16_7) + _HColor.xyz;
    f_u_xlat16_3 = (-f_u_xlat16_7) + _HColor.xyz;
    f_u_xlat16_2 = (f_u_xlat16_2.xxx * f_u_xlat16_3) + f_u_xlat16_7;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat16_3 = f_u_xlat16_0 * _Color.xyz;
    f_u_xlat16_4 = f_u_xlat16_3 * _LightColor0.xyz;
    f_u_xlat16_2 *= f_u_xlat16_4;
    float3 _316 = (f_u_xlat16_3 * f_vs_TEXCOORD3) + f_u_xlat16_2;
    f_SV_Target0.x = _316.x;
    f_SV_Target0.y = _316.y;
    f_SV_Target0.z = _316.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4 _MainTex_ST;
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 _LightColor0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_TEXCOORD0;
static float2 v_vs_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD3 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float3 v_vs_TEXCOORD2;
static float v_u_xlat12;
static float3 v_u_xlat16_2;
static float4 v_u_xlat16_1;
static float3 v_u_xlat16_3;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    float2 _101 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = _101.x;
    v_u_xlat0.y = _101.y;
    v_vs_TEXCOORD0 = v_u_xlat0.xy;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat12 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat12 = rsqrt(v_u_xlat12);
    float4 _145 = v_u_xlat0;
    float3 _147 = v_u_xlat12.xxx * _145.xyz;
    v_u_xlat0.x = _147.x;
    v_u_xlat0.y = _147.y;
    v_u_xlat0.z = _147.z;
    v_vs_TEXCOORD1 = v_u_xlat0.xyz;
    v_u_xlat16_2.x = v_u_xlat0.y * v_u_xlat0.y;
    v_u_xlat16_2.x = (v_u_xlat0.x * v_u_xlat0.x) + (-v_u_xlat16_2.x);
    v_u_xlat16_1 = v_u_xlat0.yzzx * v_u_xlat0.xyzz;
    v_u_xlat16_3.x = dot(unity_SHBr, v_u_xlat16_1);
    v_u_xlat16_3.y = dot(unity_SHBg, v_u_xlat16_1);
    v_u_xlat16_3.z = dot(unity_SHBb, v_u_xlat16_1);
    v_u_xlat16_2 = (unity_SHC.xyz * v_u_xlat16_2.xxx) + v_u_xlat16_3;
    v_u_xlat0.w = 1.0f;
    v_u_xlat16_3.x = dot(unity_SHAr, v_u_xlat0);
    v_u_xlat16_3.y = dot(unity_SHAg, v_u_xlat0);
    v_u_xlat16_3.z = dot(unity_SHAb, v_u_xlat0);
    v_vs_TEXCOORD3 = v_u_xlat16_2 + v_u_xlat16_3;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD3;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD3 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat16_0;
static float f_u_xlat1;
static float f_u_xlat5;
static float f_u_xlat9;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_1;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat16_0.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_0.x = (f_u_xlat16_0.x * 0.5f) + 0.5f;
    f_u_xlat16_0.x = max(f_u_xlat16_0.x, 0.0f);
    f_u_xlat1 = ((-_RampSmooth) * 0.5f) + _RampThreshold;
    f_u_xlat5 = f_u_xlat16_0.x + (-f_u_xlat1);
    f_u_xlat9 = (_RampSmooth * 0.5f) + _RampThreshold;
    f_u_xlat1 = (-f_u_xlat1) + f_u_xlat9;
    f_u_xlat1 = 1.0f / f_u_xlat1;
    f_u_xlat1 *= f_u_xlat5;
    f_u_xlat1 = clamp(f_u_xlat1, 0.0f, 1.0f);
    f_u_xlat5 = (f_u_xlat1 * (-2.0f)) + 3.0f;
    f_u_xlat1 *= f_u_xlat1;
    f_u_xlat1 *= f_u_xlat5;
    f_u_xlat16_0 = (-_HColor.xyz) + _SColor.xyz;
    f_u_xlat16_0 = (_SColor.www * f_u_xlat16_0) + _HColor.xyz;
    f_u_xlat16_2 = (-f_u_xlat16_0) + _HColor.xyz;
    f_u_xlat16_0 = (f_u_xlat1.xxx * f_u_xlat16_2) + f_u_xlat16_0;
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat16_2 = f_u_xlat16_1 * _Color.xyz;
    f_u_xlat16_3 = f_u_xlat16_2 * _LightColor0.xyz;
    f_u_xlat16_0 *= f_u_xlat16_3;
    float3 _137 = (f_u_xlat16_2 * f_vs_TEXCOORD3) + f_u_xlat16_0;
    f_SV_Target0.x = _137.x;
    f_SV_Target0.y = _137.y;
    f_SV_Target0.z = _137.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
float4 _MainTex_ST;
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 _LightColor0;
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float4 v_in_TEXCOORD0;
static float2 v_vs_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
    float3 v_vs_TEXCOORD3 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat6;
static float4 v_vs_TEXCOORD5;
static float4 v_vs_TEXCOORD6;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    float2 _101 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = _101.x;
    v_u_xlat0.y = _101.y;
    v_vs_TEXCOORD0 = v_u_xlat0.xy;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD1 = v_u_xlat6.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD3 = 0.0f.xxx;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
    v_vs_TEXCOORD6 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
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
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD3;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
    float3 f_vs_TEXCOORD3 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat5;
static float3 f_u_xlat1;
static float f_u_xlat10;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_7;
static float3 f_u_xlat16_3;
static float3 f_u_xlat16_0;
static float3 f_u_xlat16_4;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat5 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat5.x = dot(f_u_xlat5, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat5.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat5.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat5 = f_vs_TEXCOORD2.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat5 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat5;
    f_u_xlat5 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat5;
    f_u_xlat5 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat5.xy, f_u_xlat5.z);
    f_u_xlat5.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat10 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat5.x = (f_u_xlat5.x * f_u_xlat10) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat5.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat5.x;
    f_u_xlat16_7.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_7.x = (f_u_xlat16_7.x * 0.5f) + 0.5f;
    f_u_xlat16_7.x = max(f_u_xlat16_7.x, 0.0f);
    f_u_xlat0.x = ((-_RampSmooth) * 0.5f) + _RampThreshold;
    f_u_xlat5.x = (-f_u_xlat0.x) + f_u_xlat16_7.x;
    f_u_xlat10 = (_RampSmooth * 0.5f) + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat10;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat5.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat5.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat5.x;
    f_u_xlat16_2.x *= f_u_xlat0.x;
    f_u_xlat16_7 = (-_HColor.xyz) + _SColor.xyz;
    f_u_xlat16_7 = (_SColor.www * f_u_xlat16_7) + _HColor.xyz;
    f_u_xlat16_3 = (-f_u_xlat16_7) + _HColor.xyz;
    f_u_xlat16_2 = (f_u_xlat16_2.xxx * f_u_xlat16_3) + f_u_xlat16_7;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat16_3 = f_u_xlat16_0 * _Color.xyz;
    f_u_xlat16_4 = f_u_xlat16_3 * _LightColor0.xyz;
    f_u_xlat16_2 *= f_u_xlat16_4;
    float3 _316 = (f_u_xlat16_3 * f_vs_TEXCOORD3) + f_u_xlat16_2;
    f_SV_Target0.x = _316.x;
    f_SV_Target0.y = _316.y;
    f_SV_Target0.z = _316.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4 _MainTex_ST;
float4 unity_4LightPosY0;
float4 unity_4LightPosX0;
float4 unity_4LightPosZ0;
float4 unity_4LightAtten0;
float4 unity_LightColor[8];
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 _LightColor0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_TEXCOORD0;
static float2 v_vs_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD3 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat15;
static float3 v_vs_TEXCOORD2;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    float4 _54 = v_u_xlat0;
    float3 _56 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + _54.xyz;
    v_u_xlat0.x = _56.x;
    v_u_xlat0.y = _56.y;
    v_u_xlat0.z = _56.z;
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    float2 _110 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = _110.x;
    v_u_xlat1.y = _110.y;
    v_vs_TEXCOORD0 = v_u_xlat1.xy;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat15 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat15 = rsqrt(v_u_xlat15);
    float4 _150 = v_u_xlat1;
    float3 _152 = v_u_xlat15.xxx * _150.xyz;
    v_u_xlat1.x = _152.x;
    v_u_xlat1.y = _152.y;
    v_u_xlat1.z = _152.z;
    v_vs_TEXCOORD1 = v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = v_u_xlat0.xyz;
    v_u_xlat2 = (-v_u_xlat0.yyyy) + unity_4LightPosY0;
    v_u_xlat3 = v_u_xlat1.yyyy * v_u_xlat2;
    v_u_xlat2 *= v_u_xlat2;
    v_u_xlat4 = (-v_u_xlat0.xxxx) + unity_4LightPosX0;
    v_u_xlat0 = (-v_u_xlat0.zzzz) + unity_4LightPosZ0;
    v_u_xlat3 = (v_u_xlat4 * v_u_xlat1.xxxx) + v_u_xlat3;
    v_u_xlat1 = (v_u_xlat0 * v_u_xlat1.zzzz) + v_u_xlat3;
    v_u_xlat2 = (v_u_xlat4 * v_u_xlat4) + v_u_xlat2;
    v_u_xlat0 = (v_u_xlat0 * v_u_xlat0) + v_u_xlat2;
    v_u_xlat0 = max(v_u_xlat0, 9.9999999747524270787835121154785e-07f.xxxx);
    v_u_xlat2 = rsqrt(v_u_xlat0);
    v_u_xlat0 = (v_u_xlat0 * unity_4LightAtten0) + 1.0f.xxxx;
    v_u_xlat0 = 1.0f.xxxx / v_u_xlat0;
    v_u_xlat1 *= v_u_xlat2;
    v_u_xlat1 = max(v_u_xlat1, 0.0f.xxxx);
    v_u_xlat0 *= v_u_xlat1;
    float3 _250 = v_u_xlat0.yyy * unity_LightColor[1].xyz;
    v_u_xlat1.x = _250.x;
    v_u_xlat1.y = _250.y;
    v_u_xlat1.z = _250.z;
    float4 _263 = v_u_xlat1;
    float3 _265 = (unity_LightColor[0].xyz * v_u_xlat0.xxx) + _263.xyz;
    v_u_xlat1.x = _265.x;
    v_u_xlat1.y = _265.y;
    v_u_xlat1.z = _265.z;
    float4 _275 = v_u_xlat0;
    float3 _280 = (unity_LightColor[2].xyz * _275.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _280.x;
    v_u_xlat0.y = _280.y;
    v_u_xlat0.z = _280.z;
    float4 _290 = v_u_xlat0;
    float4 _293 = v_u_xlat0;
    float3 _295 = (unity_LightColor[3].xyz * _290.www) + _293.xyz;
    v_u_xlat0.x = _295.x;
    v_u_xlat0.y = _295.y;
    v_u_xlat0.z = _295.z;
    v_vs_TEXCOORD3 = v_u_xlat0.xyz;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD3;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD3 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat16_0;
static float f_u_xlat1;
static float f_u_xlat5;
static float f_u_xlat9;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_1;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat16_0.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_0.x = (f_u_xlat16_0.x * 0.5f) + 0.5f;
    f_u_xlat16_0.x = max(f_u_xlat16_0.x, 0.0f);
    f_u_xlat1 = ((-_RampSmooth) * 0.5f) + _RampThreshold;
    f_u_xlat5 = f_u_xlat16_0.x + (-f_u_xlat1);
    f_u_xlat9 = (_RampSmooth * 0.5f) + _RampThreshold;
    f_u_xlat1 = (-f_u_xlat1) + f_u_xlat9;
    f_u_xlat1 = 1.0f / f_u_xlat1;
    f_u_xlat1 *= f_u_xlat5;
    f_u_xlat1 = clamp(f_u_xlat1, 0.0f, 1.0f);
    f_u_xlat5 = (f_u_xlat1 * (-2.0f)) + 3.0f;
    f_u_xlat1 *= f_u_xlat1;
    f_u_xlat1 *= f_u_xlat5;
    f_u_xlat16_0 = (-_HColor.xyz) + _SColor.xyz;
    f_u_xlat16_0 = (_SColor.www * f_u_xlat16_0) + _HColor.xyz;
    f_u_xlat16_2 = (-f_u_xlat16_0) + _HColor.xyz;
    f_u_xlat16_0 = (f_u_xlat1.xxx * f_u_xlat16_2) + f_u_xlat16_0;
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat16_2 = f_u_xlat16_1 * _Color.xyz;
    f_u_xlat16_3 = f_u_xlat16_2 * _LightColor0.xyz;
    f_u_xlat16_0 *= f_u_xlat16_3;
    float3 _137 = (f_u_xlat16_2 * f_vs_TEXCOORD3) + f_u_xlat16_0;
    f_SV_Target0.x = _137.x;
    f_SV_Target0.y = _137.y;
    f_SV_Target0.z = _137.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4 _MainTex_ST;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _Color;
float4 _LightColor0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_TEXCOORD0;
static float2 v_vs_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD3 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float3 v_vs_TEXCOORD2;
static float v_u_xlat6;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    float2 _101 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = _101.x;
    v_u_xlat0.y = _101.y;
    v_vs_TEXCOORD0 = v_u_xlat0.xy;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD1 = v_u_xlat6.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD3 = 0.0f.xxx;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD3;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD3 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat16_0;
static float f_u_xlat1;
static float f_u_xlat5;
static float f_u_xlat9;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_1;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat16_0.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_0.x = (f_u_xlat16_0.x * 0.5f) + 0.5f;
    f_u_xlat16_0.x = max(f_u_xlat16_0.x, 0.0f);
    f_u_xlat1 = ((-_RampSmooth) * 0.5f) + _RampThreshold;
    f_u_xlat5 = f_u_xlat16_0.x + (-f_u_xlat1);
    f_u_xlat9 = (_RampSmooth * 0.5f) + _RampThreshold;
    f_u_xlat1 = (-f_u_xlat1) + f_u_xlat9;
    f_u_xlat1 = 1.0f / f_u_xlat1;
    f_u_xlat1 *= f_u_xlat5;
    f_u_xlat1 = clamp(f_u_xlat1, 0.0f, 1.0f);
    f_u_xlat5 = (f_u_xlat1 * (-2.0f)) + 3.0f;
    f_u_xlat1 *= f_u_xlat1;
    f_u_xlat1 *= f_u_xlat5;
    f_u_xlat16_0 = (-_HColor.xyz) + _SColor.xyz;
    f_u_xlat16_0 = (_SColor.www * f_u_xlat16_0) + _HColor.xyz;
    f_u_xlat16_2 = (-f_u_xlat16_0) + _HColor.xyz;
    f_u_xlat16_0 = (f_u_xlat1.xxx * f_u_xlat16_2) + f_u_xlat16_0;
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat16_2 = f_u_xlat16_1 * _Color.xyz;
    f_u_xlat16_3 = f_u_xlat16_2 * _LightColor0.xyz;
    f_u_xlat16_0 *= f_u_xlat16_3;
    float3 _137 = (f_u_xlat16_2 * f_vs_TEXCOORD3) + f_u_xlat16_0;
    f_SV_Target0.x = _137.x;
    f_SV_Target0.y = _137.y;
    f_SV_Target0.z = _137.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
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
