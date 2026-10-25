Shader "Cinematic/Gnome_Final_PERFECT_STABLE_v4" {
Properties {
		[Header(Base Settings)] _Color ("Gnome Tint", Vector) = (1,1,1,1)
		_MainTex ("Gnome Albedo (RGB)", 2D) = "white" {}
		[Header(Toon Lighting)] _HColor ("Highlight Color", Vector) = (1,1,1,1)
		_SColor ("Shadow Color", Vector) = (0.2,0.2,0.2,1)
		_RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.5
		_RampSmooth ("Ramp Smoothing", Range(0.01, 0.5)) = 0.1
		[Header(Stone Settings)] _StoneTex ("Stone Albedo", 2D) = "gray" {}
		[Header(Transition Logic)] _Progress ("Transition Progress", Range(0, 1)) = 0
		_Feather ("Transition Softness", Range(0.001, 0.5)) = 0.05
	}
// Recovered from the APK GLES programs; generated HLSL, not original author source.
SubShader {
Tags { "Queue"="Geometry" "RenderType"="Opaque" }
Pass {
Name "FORWARD"
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
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;
static float3 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
    float3 v_vs_TEXCOORD3 : TEXCOORD3;
    float3 v_vs_TEXCOORD4 : TEXCOORD4;
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
static float4 v_vs_TEXCOORD6;
static float4 v_vs_TEXCOORD7;

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
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat18 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat18 = rsqrt(v_u_xlat18);
    float4 _144 = v_u_xlat1;
    float3 _146 = v_u_xlat18.xxx * _144.xyz;
    v_u_xlat1.x = _146.x;
    v_u_xlat1.y = _146.y;
    v_u_xlat1.z = _146.z;
    v_vs_TEXCOORD1 = v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = v_u_xlat0.xyz;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
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
    float3 _315 = v_u_xlat0.yyy * unity_LightColor[1].xyz;
    v_u_xlat1.x = _315.x;
    v_u_xlat1.y = _315.y;
    v_u_xlat1.z = _315.z;
    float4 _328 = v_u_xlat1;
    float3 _330 = (unity_LightColor[0].xyz * v_u_xlat0.xxx) + _328.xyz;
    v_u_xlat1.x = _330.x;
    v_u_xlat1.y = _330.y;
    v_u_xlat1.z = _330.z;
    float4 _340 = v_u_xlat0;
    float3 _345 = (unity_LightColor[2].xyz * _340.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _345.x;
    v_u_xlat0.y = _345.y;
    v_u_xlat0.z = _345.z;
    float4 _355 = v_u_xlat0;
    float4 _358 = v_u_xlat0;
    float3 _360 = (unity_LightColor[3].xyz * _355.www) + _358.xyz;
    v_u_xlat0.x = _360.x;
    v_u_xlat0.y = _360.y;
    v_u_xlat0.z = _360.z;
    float4 _367 = v_u_xlat0;
    float3 _370 = _367.xyz + v_u_xlat16_3;
    v_u_xlat0.x = _370.x;
    v_u_xlat0.y = _370.y;
    v_u_xlat0.z = _370.z;
    v_vs_TEXCOORD4 = v_u_xlat0.xyz;
    v_vs_TEXCOORD6 = 0.0f.xxxx;
    v_vs_TEXCOORD7 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD4;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
    float3 f_vs_TEXCOORD3 : TEXCOORD3;
    float3 f_vs_TEXCOORD4 : TEXCOORD4;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float3 f_u_xlat1;
static float f_u_xlat8;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_6;
static float3 f_u_xlat16_4;
static float3 f_u_xlat10_1;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat4 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat4.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat4.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4 = f_vs_TEXCOORD2.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat4;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat4;
    f_u_xlat4 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat4.xy, f_u_xlat4.z);
    f_u_xlat4.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat8 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat4.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat4.x;
    f_u_xlat16_6.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_6.x = (f_u_xlat16_6.x * 0.5f) + 0.5f;
    f_u_xlat0.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat0.x) + f_u_xlat16_6.x;
    f_u_xlat8 = _RampSmooth + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_6 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_6 = (f_u_xlat0.xxx * f_u_xlat16_6) + _SColor.xyz;
    f_u_xlat0.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat4.x = f_u_xlat0.x * _Progress;
    f_u_xlat4.x = (_Feather * (-2.0f)) + f_u_xlat4.x;
    f_u_xlat0.x = (_Progress * f_u_xlat0.x) + (-f_u_xlat4.x);
    f_u_xlat4.x = (-f_u_xlat4.x) + f_vs_TEXCOORD3.x;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_1 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat4 = (f_u_xlat16_4 * _Color.xyz) + (-f_u_xlat10_1);
    f_u_xlat0 = (f_u_xlat0.xxx * f_u_xlat4) + f_u_xlat10_1;
    f_u_xlat16_3 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_6 *= f_u_xlat16_3;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * f_u_xlat16_6;
    float3 _378 = (f_u_xlat0 * f_vs_TEXCOORD4) + f_u_xlat16_2;
    f_SV_Target0.x = _378.x;
    f_SV_Target0.y = _378.y;
    f_SV_Target0.z = _378.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
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
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;
static float3 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
    float3 v_vs_TEXCOORD3 : TEXCOORD3;
    float3 v_vs_TEXCOORD4 : TEXCOORD4;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat12;
static float3 v_u_xlat16_2;
static float4 v_u_xlat16_1;
static float3 v_u_xlat16_3;
static float4 v_vs_TEXCOORD6;
static float4 v_vs_TEXCOORD7;

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
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat12 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat12 = rsqrt(v_u_xlat12);
    float4 _139 = v_u_xlat0;
    float3 _141 = v_u_xlat12.xxx * _139.xyz;
    v_u_xlat0.x = _141.x;
    v_u_xlat0.y = _141.y;
    v_u_xlat0.z = _141.z;
    v_vs_TEXCOORD1 = v_u_xlat0.xyz;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
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
    v_vs_TEXCOORD4 = v_u_xlat16_2 + v_u_xlat16_3;
    v_vs_TEXCOORD6 = 0.0f.xxxx;
    v_vs_TEXCOORD7 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD4;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
    float3 f_vs_TEXCOORD3 : TEXCOORD3;
    float3 f_vs_TEXCOORD4 : TEXCOORD4;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float3 f_u_xlat1;
static float f_u_xlat8;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_6;
static float3 f_u_xlat16_4;
static float3 f_u_xlat10_1;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat4 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat4.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat4.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4 = f_vs_TEXCOORD2.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat4;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat4;
    f_u_xlat4 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat4.xy, f_u_xlat4.z);
    f_u_xlat4.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat8 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat4.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat4.x;
    f_u_xlat16_6.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_6.x = (f_u_xlat16_6.x * 0.5f) + 0.5f;
    f_u_xlat0.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat0.x) + f_u_xlat16_6.x;
    f_u_xlat8 = _RampSmooth + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_6 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_6 = (f_u_xlat0.xxx * f_u_xlat16_6) + _SColor.xyz;
    f_u_xlat0.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat4.x = f_u_xlat0.x * _Progress;
    f_u_xlat4.x = (_Feather * (-2.0f)) + f_u_xlat4.x;
    f_u_xlat0.x = (_Progress * f_u_xlat0.x) + (-f_u_xlat4.x);
    f_u_xlat4.x = (-f_u_xlat4.x) + f_vs_TEXCOORD3.x;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_1 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat4 = (f_u_xlat16_4 * _Color.xyz) + (-f_u_xlat10_1);
    f_u_xlat0 = (f_u_xlat0.xxx * f_u_xlat4) + f_u_xlat10_1;
    f_u_xlat16_3 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_6 *= f_u_xlat16_3;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * f_u_xlat16_6;
    float3 _378 = (f_u_xlat0 * f_vs_TEXCOORD4) + f_u_xlat16_2;
    f_SV_Target0.x = _378.x;
    f_SV_Target0.y = _378.y;
    f_SV_Target0.z = _378.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
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
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;
static float3 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD3 : TEXCOORD2;
    float3 v_vs_TEXCOORD4 : TEXCOORD3;
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
static float4 v_vs_TEXCOORD6;
static float4 v_vs_TEXCOORD7;

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
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat18 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat18 = rsqrt(v_u_xlat18);
    float4 _144 = v_u_xlat1;
    float3 _146 = v_u_xlat18.xxx * _144.xyz;
    v_u_xlat1.x = _146.x;
    v_u_xlat1.y = _146.y;
    v_u_xlat1.z = _146.z;
    v_vs_TEXCOORD1 = v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = v_u_xlat0.xyz;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
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
    float3 _315 = v_u_xlat0.yyy * unity_LightColor[1].xyz;
    v_u_xlat1.x = _315.x;
    v_u_xlat1.y = _315.y;
    v_u_xlat1.z = _315.z;
    float4 _328 = v_u_xlat1;
    float3 _330 = (unity_LightColor[0].xyz * v_u_xlat0.xxx) + _328.xyz;
    v_u_xlat1.x = _330.x;
    v_u_xlat1.y = _330.y;
    v_u_xlat1.z = _330.z;
    float4 _340 = v_u_xlat0;
    float3 _345 = (unity_LightColor[2].xyz * _340.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _345.x;
    v_u_xlat0.y = _345.y;
    v_u_xlat0.z = _345.z;
    float4 _355 = v_u_xlat0;
    float4 _358 = v_u_xlat0;
    float3 _360 = (unity_LightColor[3].xyz * _355.www) + _358.xyz;
    v_u_xlat0.x = _360.x;
    v_u_xlat0.y = _360.y;
    v_u_xlat0.z = _360.z;
    float4 _367 = v_u_xlat0;
    float3 _370 = _367.xyz + v_u_xlat16_3;
    v_u_xlat0.x = _370.x;
    v_u_xlat0.y = _370.y;
    v_u_xlat0.z = _370.z;
    v_vs_TEXCOORD4 = v_u_xlat0.xyz;
    v_vs_TEXCOORD6 = 0.0f.xxxx;
    v_vs_TEXCOORD7 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD4;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD3 : TEXCOORD2;
    float3 f_vs_TEXCOORD4 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat16_0;
static float3 f_u_xlat1;
static float3 f_u_xlat5;
static float f_u_xlat9;
static float3 f_u_xlat16_5;
static float3 f_u_xlat10_2;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat16_0.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_0.x = (f_u_xlat16_0.x * 0.5f) + 0.5f;
    f_u_xlat1.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat5.x = f_u_xlat16_0.x + (-f_u_xlat1.x);
    f_u_xlat9 = _RampSmooth + _RampThreshold;
    f_u_xlat1.x = (-f_u_xlat1.x) + f_u_xlat9;
    f_u_xlat1.x = 1.0f / f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat1.x = clamp(f_u_xlat1.x, 0.0f, 1.0f);
    f_u_xlat5.x = (f_u_xlat1.x * (-2.0f)) + 3.0f;
    f_u_xlat1.x *= f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat16_0 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_0 = (f_u_xlat1.xxx * f_u_xlat16_0) + _SColor.xyz;
    f_u_xlat1.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat5.x = f_u_xlat1.x * _Progress;
    f_u_xlat5.x = (_Feather * (-2.0f)) + f_u_xlat5.x;
    f_u_xlat1.x = (_Progress * f_u_xlat1.x) + (-f_u_xlat5.x);
    f_u_xlat5.x = (-f_u_xlat5.x) + f_vs_TEXCOORD3.x;
    f_u_xlat1.x = 1.0f / f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat1.x = clamp(f_u_xlat1.x, 0.0f, 1.0f);
    f_u_xlat5.x = (f_u_xlat1.x * (-2.0f)) + 3.0f;
    f_u_xlat1.x *= f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat16_5 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_2 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat5 = (f_u_xlat16_5 * _Color.xyz) + (-f_u_xlat10_2);
    f_u_xlat1 = (f_u_xlat1.xxx * f_u_xlat5) + f_u_xlat10_2;
    f_u_xlat16_3 = f_u_xlat1 * _LightColor0.xyz;
    f_u_xlat16_0 *= f_u_xlat16_3;
    float3 _221 = (f_u_xlat1 * f_vs_TEXCOORD4) + f_u_xlat16_0;
    f_SV_Target0.x = _221.x;
    f_SV_Target0.y = _221.y;
    f_SV_Target0.z = _221.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
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
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;
static float3 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
    float3 v_vs_TEXCOORD3 : TEXCOORD3;
    float3 v_vs_TEXCOORD4 : TEXCOORD4;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat15;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_vs_TEXCOORD6;
static float4 v_vs_TEXCOORD7;

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
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat15 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat15 = rsqrt(v_u_xlat15);
    float4 _144 = v_u_xlat1;
    float3 _146 = v_u_xlat15.xxx * _144.xyz;
    v_u_xlat1.x = _146.x;
    v_u_xlat1.y = _146.y;
    v_u_xlat1.z = _146.z;
    v_vs_TEXCOORD1 = v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = v_u_xlat0.xyz;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
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
    float3 _247 = v_u_xlat0.yyy * unity_LightColor[1].xyz;
    v_u_xlat1.x = _247.x;
    v_u_xlat1.y = _247.y;
    v_u_xlat1.z = _247.z;
    float4 _260 = v_u_xlat1;
    float3 _262 = (unity_LightColor[0].xyz * v_u_xlat0.xxx) + _260.xyz;
    v_u_xlat1.x = _262.x;
    v_u_xlat1.y = _262.y;
    v_u_xlat1.z = _262.z;
    float4 _272 = v_u_xlat0;
    float3 _277 = (unity_LightColor[2].xyz * _272.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _277.x;
    v_u_xlat0.y = _277.y;
    v_u_xlat0.z = _277.z;
    float4 _287 = v_u_xlat0;
    float4 _290 = v_u_xlat0;
    float3 _292 = (unity_LightColor[3].xyz * _287.www) + _290.xyz;
    v_u_xlat0.x = _292.x;
    v_u_xlat0.y = _292.y;
    v_u_xlat0.z = _292.z;
    v_vs_TEXCOORD4 = v_u_xlat0.xyz;
    v_vs_TEXCOORD6 = 0.0f.xxxx;
    v_vs_TEXCOORD7 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD4;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
    float3 f_vs_TEXCOORD3 : TEXCOORD3;
    float3 f_vs_TEXCOORD4 : TEXCOORD4;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float3 f_u_xlat1;
static float f_u_xlat8;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_6;
static float3 f_u_xlat16_4;
static float3 f_u_xlat10_1;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat4 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat4.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat4.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4 = f_vs_TEXCOORD2.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat4;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat4;
    f_u_xlat4 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat4.xy, f_u_xlat4.z);
    f_u_xlat4.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat8 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat4.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat4.x;
    f_u_xlat16_6.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_6.x = (f_u_xlat16_6.x * 0.5f) + 0.5f;
    f_u_xlat0.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat0.x) + f_u_xlat16_6.x;
    f_u_xlat8 = _RampSmooth + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_6 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_6 = (f_u_xlat0.xxx * f_u_xlat16_6) + _SColor.xyz;
    f_u_xlat0.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat4.x = f_u_xlat0.x * _Progress;
    f_u_xlat4.x = (_Feather * (-2.0f)) + f_u_xlat4.x;
    f_u_xlat0.x = (_Progress * f_u_xlat0.x) + (-f_u_xlat4.x);
    f_u_xlat4.x = (-f_u_xlat4.x) + f_vs_TEXCOORD3.x;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_1 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat4 = (f_u_xlat16_4 * _Color.xyz) + (-f_u_xlat10_1);
    f_u_xlat0 = (f_u_xlat0.xxx * f_u_xlat4) + f_u_xlat10_1;
    f_u_xlat16_3 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_6 *= f_u_xlat16_3;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * f_u_xlat16_6;
    float3 _378 = (f_u_xlat0 * f_vs_TEXCOORD4) + f_u_xlat16_2;
    f_SV_Target0.x = _378.x;
    f_SV_Target0.y = _378.y;
    f_SV_Target0.z = _378.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
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
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;
static float3 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD3 : TEXCOORD2;
    float3 v_vs_TEXCOORD4 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float3 v_vs_TEXCOORD2;
static float v_u_xlat12;
static float3 v_u_xlat16_2;
static float4 v_u_xlat16_1;
static float3 v_u_xlat16_3;
static float4 v_vs_TEXCOORD6;
static float4 v_vs_TEXCOORD7;

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
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat12 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat12 = rsqrt(v_u_xlat12);
    float4 _139 = v_u_xlat0;
    float3 _141 = v_u_xlat12.xxx * _139.xyz;
    v_u_xlat0.x = _141.x;
    v_u_xlat0.y = _141.y;
    v_u_xlat0.z = _141.z;
    v_vs_TEXCOORD1 = v_u_xlat0.xyz;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
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
    v_vs_TEXCOORD4 = v_u_xlat16_2 + v_u_xlat16_3;
    v_vs_TEXCOORD6 = 0.0f.xxxx;
    v_vs_TEXCOORD7 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD4;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD3 : TEXCOORD2;
    float3 f_vs_TEXCOORD4 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat16_0;
static float3 f_u_xlat1;
static float3 f_u_xlat5;
static float f_u_xlat9;
static float3 f_u_xlat16_5;
static float3 f_u_xlat10_2;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat16_0.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_0.x = (f_u_xlat16_0.x * 0.5f) + 0.5f;
    f_u_xlat1.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat5.x = f_u_xlat16_0.x + (-f_u_xlat1.x);
    f_u_xlat9 = _RampSmooth + _RampThreshold;
    f_u_xlat1.x = (-f_u_xlat1.x) + f_u_xlat9;
    f_u_xlat1.x = 1.0f / f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat1.x = clamp(f_u_xlat1.x, 0.0f, 1.0f);
    f_u_xlat5.x = (f_u_xlat1.x * (-2.0f)) + 3.0f;
    f_u_xlat1.x *= f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat16_0 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_0 = (f_u_xlat1.xxx * f_u_xlat16_0) + _SColor.xyz;
    f_u_xlat1.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat5.x = f_u_xlat1.x * _Progress;
    f_u_xlat5.x = (_Feather * (-2.0f)) + f_u_xlat5.x;
    f_u_xlat1.x = (_Progress * f_u_xlat1.x) + (-f_u_xlat5.x);
    f_u_xlat5.x = (-f_u_xlat5.x) + f_vs_TEXCOORD3.x;
    f_u_xlat1.x = 1.0f / f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat1.x = clamp(f_u_xlat1.x, 0.0f, 1.0f);
    f_u_xlat5.x = (f_u_xlat1.x * (-2.0f)) + 3.0f;
    f_u_xlat1.x *= f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat16_5 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_2 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat5 = (f_u_xlat16_5 * _Color.xyz) + (-f_u_xlat10_2);
    f_u_xlat1 = (f_u_xlat1.xxx * f_u_xlat5) + f_u_xlat10_2;
    f_u_xlat16_3 = f_u_xlat1 * _LightColor0.xyz;
    f_u_xlat16_0 *= f_u_xlat16_3;
    float3 _221 = (f_u_xlat1 * f_vs_TEXCOORD4) + f_u_xlat16_0;
    f_SV_Target0.x = _221.x;
    f_SV_Target0.y = _221.y;
    f_SV_Target0.z = _221.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
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
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;
static float3 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
    float3 v_vs_TEXCOORD3 : TEXCOORD3;
    float3 v_vs_TEXCOORD4 : TEXCOORD4;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat6;
static float4 v_vs_TEXCOORD6;
static float4 v_vs_TEXCOORD7;

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
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD1 = v_u_xlat6.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxx;
    v_vs_TEXCOORD6 = 0.0f.xxxx;
    v_vs_TEXCOORD7 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD4;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
    float3 f_vs_TEXCOORD3 : TEXCOORD3;
    float3 f_vs_TEXCOORD4 : TEXCOORD4;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float3 f_u_xlat1;
static float f_u_xlat8;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_6;
static float3 f_u_xlat16_4;
static float3 f_u_xlat10_1;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat4 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat4.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat4.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4 = f_vs_TEXCOORD2.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat4;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat4;
    f_u_xlat4 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat4.xy, f_u_xlat4.z);
    f_u_xlat4.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat8 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat4.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat4.x;
    f_u_xlat16_6.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_6.x = (f_u_xlat16_6.x * 0.5f) + 0.5f;
    f_u_xlat0.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat0.x) + f_u_xlat16_6.x;
    f_u_xlat8 = _RampSmooth + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_6 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_6 = (f_u_xlat0.xxx * f_u_xlat16_6) + _SColor.xyz;
    f_u_xlat0.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat4.x = f_u_xlat0.x * _Progress;
    f_u_xlat4.x = (_Feather * (-2.0f)) + f_u_xlat4.x;
    f_u_xlat0.x = (_Progress * f_u_xlat0.x) + (-f_u_xlat4.x);
    f_u_xlat4.x = (-f_u_xlat4.x) + f_vs_TEXCOORD3.x;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_1 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat4 = (f_u_xlat16_4 * _Color.xyz) + (-f_u_xlat10_1);
    f_u_xlat0 = (f_u_xlat0.xxx * f_u_xlat4) + f_u_xlat10_1;
    f_u_xlat16_3 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_6 *= f_u_xlat16_3;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * f_u_xlat16_6;
    float3 _378 = (f_u_xlat0 * f_vs_TEXCOORD4) + f_u_xlat16_2;
    f_SV_Target0.x = _378.x;
    f_SV_Target0.y = _378.y;
    f_SV_Target0.z = _378.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
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
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;
static float3 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD3 : TEXCOORD2;
    float3 v_vs_TEXCOORD4 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat15;
static float3 v_vs_TEXCOORD2;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_vs_TEXCOORD6;
static float4 v_vs_TEXCOORD7;

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
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat15 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat15 = rsqrt(v_u_xlat15);
    float4 _144 = v_u_xlat1;
    float3 _146 = v_u_xlat15.xxx * _144.xyz;
    v_u_xlat1.x = _146.x;
    v_u_xlat1.y = _146.y;
    v_u_xlat1.z = _146.z;
    v_vs_TEXCOORD1 = v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = v_u_xlat0.xyz;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
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
    float3 _248 = v_u_xlat0.yyy * unity_LightColor[1].xyz;
    v_u_xlat1.x = _248.x;
    v_u_xlat1.y = _248.y;
    v_u_xlat1.z = _248.z;
    float4 _261 = v_u_xlat1;
    float3 _263 = (unity_LightColor[0].xyz * v_u_xlat0.xxx) + _261.xyz;
    v_u_xlat1.x = _263.x;
    v_u_xlat1.y = _263.y;
    v_u_xlat1.z = _263.z;
    float4 _273 = v_u_xlat0;
    float3 _278 = (unity_LightColor[2].xyz * _273.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _278.x;
    v_u_xlat0.y = _278.y;
    v_u_xlat0.z = _278.z;
    float4 _288 = v_u_xlat0;
    float4 _291 = v_u_xlat0;
    float3 _293 = (unity_LightColor[3].xyz * _288.www) + _291.xyz;
    v_u_xlat0.x = _293.x;
    v_u_xlat0.y = _293.y;
    v_u_xlat0.z = _293.z;
    v_vs_TEXCOORD4 = v_u_xlat0.xyz;
    v_vs_TEXCOORD6 = 0.0f.xxxx;
    v_vs_TEXCOORD7 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD4;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD3 : TEXCOORD2;
    float3 f_vs_TEXCOORD4 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat16_0;
static float3 f_u_xlat1;
static float3 f_u_xlat5;
static float f_u_xlat9;
static float3 f_u_xlat16_5;
static float3 f_u_xlat10_2;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat16_0.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_0.x = (f_u_xlat16_0.x * 0.5f) + 0.5f;
    f_u_xlat1.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat5.x = f_u_xlat16_0.x + (-f_u_xlat1.x);
    f_u_xlat9 = _RampSmooth + _RampThreshold;
    f_u_xlat1.x = (-f_u_xlat1.x) + f_u_xlat9;
    f_u_xlat1.x = 1.0f / f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat1.x = clamp(f_u_xlat1.x, 0.0f, 1.0f);
    f_u_xlat5.x = (f_u_xlat1.x * (-2.0f)) + 3.0f;
    f_u_xlat1.x *= f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat16_0 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_0 = (f_u_xlat1.xxx * f_u_xlat16_0) + _SColor.xyz;
    f_u_xlat1.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat5.x = f_u_xlat1.x * _Progress;
    f_u_xlat5.x = (_Feather * (-2.0f)) + f_u_xlat5.x;
    f_u_xlat1.x = (_Progress * f_u_xlat1.x) + (-f_u_xlat5.x);
    f_u_xlat5.x = (-f_u_xlat5.x) + f_vs_TEXCOORD3.x;
    f_u_xlat1.x = 1.0f / f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat1.x = clamp(f_u_xlat1.x, 0.0f, 1.0f);
    f_u_xlat5.x = (f_u_xlat1.x * (-2.0f)) + 3.0f;
    f_u_xlat1.x *= f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat16_5 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_2 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat5 = (f_u_xlat16_5 * _Color.xyz) + (-f_u_xlat10_2);
    f_u_xlat1 = (f_u_xlat1.xxx * f_u_xlat5) + f_u_xlat10_2;
    f_u_xlat16_3 = f_u_xlat1 * _LightColor0.xyz;
    f_u_xlat16_0 *= f_u_xlat16_3;
    float3 _221 = (f_u_xlat1 * f_vs_TEXCOORD4) + f_u_xlat16_0;
    f_SV_Target0.x = _221.x;
    f_SV_Target0.y = _221.y;
    f_SV_Target0.z = _221.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
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
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;
static float3 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD3 : TEXCOORD2;
    float3 v_vs_TEXCOORD4 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float3 v_vs_TEXCOORD2;
static float v_u_xlat6;
static float4 v_vs_TEXCOORD6;
static float4 v_vs_TEXCOORD7;

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
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD1 = v_u_xlat6.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxx;
    v_vs_TEXCOORD6 = 0.0f.xxxx;
    v_vs_TEXCOORD7 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD4;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD3 : TEXCOORD2;
    float3 f_vs_TEXCOORD4 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat16_0;
static float3 f_u_xlat1;
static float3 f_u_xlat5;
static float f_u_xlat9;
static float3 f_u_xlat16_5;
static float3 f_u_xlat10_2;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat16_0.x = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_0.x = (f_u_xlat16_0.x * 0.5f) + 0.5f;
    f_u_xlat1.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat5.x = f_u_xlat16_0.x + (-f_u_xlat1.x);
    f_u_xlat9 = _RampSmooth + _RampThreshold;
    f_u_xlat1.x = (-f_u_xlat1.x) + f_u_xlat9;
    f_u_xlat1.x = 1.0f / f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat1.x = clamp(f_u_xlat1.x, 0.0f, 1.0f);
    f_u_xlat5.x = (f_u_xlat1.x * (-2.0f)) + 3.0f;
    f_u_xlat1.x *= f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat16_0 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_0 = (f_u_xlat1.xxx * f_u_xlat16_0) + _SColor.xyz;
    f_u_xlat1.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat5.x = f_u_xlat1.x * _Progress;
    f_u_xlat5.x = (_Feather * (-2.0f)) + f_u_xlat5.x;
    f_u_xlat1.x = (_Progress * f_u_xlat1.x) + (-f_u_xlat5.x);
    f_u_xlat5.x = (-f_u_xlat5.x) + f_vs_TEXCOORD3.x;
    f_u_xlat1.x = 1.0f / f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat1.x = clamp(f_u_xlat1.x, 0.0f, 1.0f);
    f_u_xlat5.x = (f_u_xlat1.x * (-2.0f)) + 3.0f;
    f_u_xlat1.x *= f_u_xlat1.x;
    f_u_xlat1.x *= f_u_xlat5.x;
    f_u_xlat16_5 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_2 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat5 = (f_u_xlat16_5 * _Color.xyz) + (-f_u_xlat10_2);
    f_u_xlat1 = (f_u_xlat1.xxx * f_u_xlat5) + f_u_xlat10_2;
    f_u_xlat16_3 = f_u_xlat1 * _LightColor0.xyz;
    f_u_xlat16_0 *= f_u_xlat16_3;
    float3 _221 = (f_u_xlat1 * f_vs_TEXCOORD4) + f_u_xlat16_0;
    f_SV_Target0.x = _221.x;
    f_SV_Target0.y = _221.y;
    f_SV_Target0.z = _221.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
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
Name "FORWARD"
Tags { "LightMode"="ForwardAdd" "Queue"="Geometry" "RenderType"="Opaque" "SHADOWSUPPORT"="true" }
Cull Back
ZWrite Off
ZTest LEqual
Blend One One, One One
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
#pragma multi_compile DIRECTIONAL DIRECTIONAL_COOKIE POINT POINT_COOKIE SPOT
#pragma multi_compile _ SHADOWS_CUBE SHADOWS_DEPTH SHADOWS_SCREEN
#pragma multi_compile _ SHADOWS_SOFT
#if !defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && !defined(POINT) && !defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && defined(SHADOWS_SOFT) && defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToLight;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
float4 _MainTex_ST;
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
float4 _ShadowOffsets[4];
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
Texture2D<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
Texture2D<float4> _LightTextureB0;
SamplerState sampler_LightTextureB0;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
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
static float v_u_xlat10;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_vs_TEXCOORD1 = v_u_xlat10.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_WorldToLight)[1];
    v_u_xlat1 = (transpose(unity_WorldToLight)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_WorldToLight)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_vs_TEXCOORD4 = (transpose(unity_WorldToLight)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
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
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

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

static float4 f_u_xlat0;
static float3 f_u_xlat5;
static float4 f_u_xlat1;
static float3 f_u_xlat2;
static float f_u_xlat10;
static float f_u_xlat16_3;
static float f_u_xlat15;
static bool f_u_xlatb1;
static float3 f_u_xlat16_8;
static float3 f_u_xlat16_5;
static float3 f_u_xlat10_1;
static float3 f_u_xlat16_4;

void f_frag_main()
{
    float3 _19 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = _19.x;
    f_u_xlat0.y = _19.y;
    f_u_xlat0.z = _19.z;
    f_u_xlat0.x = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat5 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat5.x = dot(f_u_xlat5, f_u_xlat1.xyz);
    f_u_xlat0.x = (-f_u_xlat5.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat5.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat1 = f_vs_TEXCOORD2.yyyy * RecoveryColumn_unity_WorldToShadow(1);
    f_u_xlat1 = (RecoveryColumn_unity_WorldToShadow(0) * f_vs_TEXCOORD2.xxxx) + f_u_xlat1;
    f_u_xlat1 = (RecoveryColumn_unity_WorldToShadow(2) * f_vs_TEXCOORD2.zzzz) + f_u_xlat1;
    f_u_xlat1 += RecoveryColumn_unity_WorldToShadow(3);
    f_u_xlat5 = f_u_xlat1.xyz / f_u_xlat1.www;
    float3 _144 = f_u_xlat5 + _ShadowOffsets[0].xyz;
    f_u_xlat1.x = _144.x;
    f_u_xlat1.y = _144.y;
    f_u_xlat1.z = _144.z;
    float3 txVec0 = float3(f_u_xlat1.xy, f_u_xlat1.z);
    f_u_xlat1.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat2 = f_u_xlat5 + _ShadowOffsets[1].xyz;
    float3 txVec1 = float3(f_u_xlat2.xy, f_u_xlat2.z);
    f_u_xlat1.y = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec1.xy, txVec1.z);
    f_u_xlat2 = f_u_xlat5 + _ShadowOffsets[2].xyz;
    f_u_xlat5 += _ShadowOffsets[3].xyz;
    float3 txVec2 = float3(f_u_xlat5.xy, f_u_xlat5.z);
    f_u_xlat1.w = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec2.xy, txVec2.z);
    float3 txVec3 = float3(f_u_xlat2.xy, f_u_xlat2.z);
    f_u_xlat1.z = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec3.xy, txVec3.z);
    f_u_xlat5.x = dot(f_u_xlat1, 0.25f.xxxx);
    f_u_xlat10 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat5.x = (f_u_xlat5.x * f_u_xlat10) + _LightShadowData.x;
    f_u_xlat16_3 = (-f_u_xlat5.x) + 1.0f;
    f_u_xlat16_3 = (f_u_xlat0.x * f_u_xlat16_3) + f_u_xlat5.x;
    f_u_xlat0 = f_vs_TEXCOORD2.yyyy * transpose(unity_WorldToLight)[1];
    f_u_xlat0 = (transpose(unity_WorldToLight)[0] * f_vs_TEXCOORD2.xxxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2] * f_vs_TEXCOORD2.zzzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3];
    float2 _283 = f_u_xlat0.xy / f_u_xlat0.ww;
    f_u_xlat1.x = _283.x;
    f_u_xlat1.y = _283.y;
    float4 _288 = f_u_xlat1;
    float2 _292 = _288.xy + 0.5f.xx;
    f_u_xlat1.x = _292.x;
    f_u_xlat1.y = _292.y;
    f_u_xlat15 = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat1.xy).w;
    f_u_xlatb1 = 0.0f < f_u_xlat0.z;
    f_u_xlat0.x = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat0.x = _LightTextureB0.Sample(sampler_LightTextureB0, f_u_xlat0.xx).x;
    f_u_xlat16_8.x = float(f_u_xlatb1);
    f_u_xlat16_8.x = f_u_xlat15 * f_u_xlat16_8.x;
    f_u_xlat16_8.x = f_u_xlat0.x * f_u_xlat16_8.x;
    f_u_xlat16_3 *= f_u_xlat16_8.x;
    float3 _350 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat0.x = _350.x;
    f_u_xlat0.y = _350.y;
    f_u_xlat0.z = _350.z;
    f_u_xlat15 = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat15 = rsqrt(f_u_xlat15);
    float4 _366 = f_u_xlat0;
    float3 _368 = f_u_xlat15.xxx * _366.xyz;
    f_u_xlat0.x = _368.x;
    f_u_xlat0.y = _368.y;
    f_u_xlat0.z = _368.z;
    f_u_xlat16_8.x = dot(f_vs_TEXCOORD1, f_u_xlat0.xyz);
    f_u_xlat16_8.x = (f_u_xlat16_8.x * 0.5f) + 0.5f;
    f_u_xlat0.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat5.x = (-f_u_xlat0.x) + f_u_xlat16_8.x;
    f_u_xlat10 = _RampSmooth + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat10;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat5.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat5.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat5.x;
    f_u_xlat16_8 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_8 = (f_u_xlat0.xxx * f_u_xlat16_8) + _SColor.xyz;
    f_u_xlat0.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat5.x = f_u_xlat0.x * _Progress;
    f_u_xlat5.x = (_Feather * (-2.0f)) + f_u_xlat5.x;
    f_u_xlat0.x = (_Progress * f_u_xlat0.x) + (-f_u_xlat5.x);
    f_u_xlat5.x = (-f_u_xlat5.x) + f_vs_TEXCOORD3.x;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat5.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat5.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat5.x;
    f_u_xlat16_5 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_1 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat5 = (f_u_xlat16_5 * _Color.xyz) + (-f_u_xlat10_1);
    float4 _546 = f_u_xlat0;
    float3 _551 = (_546.xxx * f_u_xlat5) + f_u_xlat10_1;
    f_u_xlat0.x = _551.x;
    f_u_xlat0.y = _551.y;
    f_u_xlat0.z = _551.z;
    f_u_xlat16_4 = f_u_xlat0.xyz * _LightColor0.xyz;
    f_u_xlat16_8 *= f_u_xlat16_4;
    float3 _573 = f_u_xlat16_3.xxx * f_u_xlat16_8;
    f_SV_Target0.x = _573.x;
    f_SV_Target0.y = _573.y;
    f_SV_Target0.z = _573.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && defined(POINT) && !defined(POINT_COOKIE) && defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToLight;
float4x4 unity_MatrixV;
float4 _MainTex_ST;
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
float4 _LightPositionRange;
float4 _LightProjectionParams;
TextureCube<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
Texture2D<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
TextureCube<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
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
static float v_u_xlat10;
static float3 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_vs_TEXCOORD1 = v_u_xlat10.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    float3 _161 = v_u_xlat0.yyy * transpose(unity_WorldToLight)[1].xyz;
    v_u_xlat1.x = _161.x;
    v_u_xlat1.y = _161.y;
    v_u_xlat1.z = _161.z;
    float4 _174 = v_u_xlat1;
    float3 _176 = (transpose(unity_WorldToLight)[0].xyz * v_u_xlat0.xxx) + _174.xyz;
    v_u_xlat1.x = _176.x;
    v_u_xlat1.y = _176.y;
    v_u_xlat1.z = _176.z;
    float4 _186 = v_u_xlat0;
    float3 _191 = (transpose(unity_WorldToLight)[2].xyz * _186.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _191.x;
    v_u_xlat0.y = _191.y;
    v_u_xlat0.z = _191.z;
    v_vs_TEXCOORD4 = (transpose(unity_WorldToLight)[3].xyz * v_u_xlat0.www) + v_u_xlat0.xyz;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
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
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

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
static float3 f_u_xlat6;
static float4 f_u_xlat2;
static float f_u_xlat10;
static float3 f_u_xlat16_3;
static float f_u_xlat15;
static float3 f_u_xlat16_1;
static float3 f_u_xlat10_2;
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
    f_u_xlat5 = f_vs_TEXCOORD2 + (-_LightPositionRange.xyz);
    f_u_xlat1.x = max(abs(f_u_xlat5.y), abs(f_u_xlat5.x));
    f_u_xlat1.x = max(abs(f_u_xlat5.z), f_u_xlat1.x);
    f_u_xlat1.x += (-_LightProjectionParams.z);
    f_u_xlat1.x = max(f_u_xlat1.x, 9.9999997473787516355514526367188e-06f);
    f_u_xlat1.x *= _LightProjectionParams.w;
    f_u_xlat1.x = _LightProjectionParams.y / f_u_xlat1.x;
    f_u_xlat1.x += (-_LightProjectionParams.x);
    f_u_xlat6 = f_u_xlat5 + 0.0078125f.xxx;
    float4 txVec0 = float4(f_u_xlat6, f_u_xlat1.x);
    f_u_xlat2.x = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xyz, txVec0.w);
    f_u_xlat6 = f_u_xlat5 + float3(-0.0078125f, -0.0078125f, 0.0078125f);
    float4 txVec1 = float4(f_u_xlat6, f_u_xlat1.x);
    f_u_xlat2.y = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec1.xyz, txVec1.w);
    f_u_xlat6 = f_u_xlat5 + float3(-0.0078125f, 0.0078125f, -0.0078125f);
    f_u_xlat5 += float3(0.0078125f, -0.0078125f, -0.0078125f);
    float4 txVec2 = float4(f_u_xlat5, f_u_xlat1.x);
    f_u_xlat2.w = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec2.xyz, txVec2.w);
    float4 txVec3 = float4(f_u_xlat6, f_u_xlat1.x);
    f_u_xlat2.z = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec3.xyz, txVec3.w);
    f_u_xlat5.x = dot(f_u_xlat2, 0.25f.xxxx);
    f_u_xlat10 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat5.x = (f_u_xlat5.x * f_u_xlat10) + _LightShadowData.x;
    f_u_xlat16_3.x = (-f_u_xlat5.x) + 1.0f;
    f_u_xlat16_3.x = (f_u_xlat0.x * f_u_xlat16_3.x) + f_u_xlat5.x;
    f_u_xlat0 = f_vs_TEXCOORD2.yyy * transpose(unity_WorldToLight)[1].xyz;
    f_u_xlat0 = (transpose(unity_WorldToLight)[0].xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2].xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3].xyz;
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0.xx).x;
    f_u_xlat0.x = f_u_xlat16_3.x * f_u_xlat0.x;
    f_u_xlat5 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat1.x = dot(f_u_xlat5, f_u_xlat5);
    f_u_xlat1.x = rsqrt(f_u_xlat1.x);
    f_u_xlat5 *= f_u_xlat1.xxx;
    f_u_xlat16_3.x = dot(f_vs_TEXCOORD1, f_u_xlat5);
    f_u_xlat16_3.x = (f_u_xlat16_3.x * 0.5f) + 0.5f;
    f_u_xlat5.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat10 = (-f_u_xlat5.x) + f_u_xlat16_3.x;
    f_u_xlat15 = _RampSmooth + _RampThreshold;
    f_u_xlat5.x = (-f_u_xlat5.x) + f_u_xlat15;
    f_u_xlat5.x = 1.0f / f_u_xlat5.x;
    f_u_xlat5.x *= f_u_xlat10;
    f_u_xlat5.x = clamp(f_u_xlat5.x, 0.0f, 1.0f);
    f_u_xlat10 = (f_u_xlat5.x * (-2.0f)) + 3.0f;
    f_u_xlat5.x *= f_u_xlat5.x;
    f_u_xlat5.x *= f_u_xlat10;
    f_u_xlat16_3 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_3 = (f_u_xlat5.xxx * f_u_xlat16_3) + _SColor.xyz;
    f_u_xlat5.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat10 = f_u_xlat5.x * _Progress;
    f_u_xlat10 = (_Feather * (-2.0f)) + f_u_xlat10;
    f_u_xlat5.x = (_Progress * f_u_xlat5.x) + (-f_u_xlat10);
    f_u_xlat10 = (-f_u_xlat10) + f_vs_TEXCOORD3.x;
    f_u_xlat5.x = 1.0f / f_u_xlat5.x;
    f_u_xlat5.x *= f_u_xlat10;
    f_u_xlat5.x = clamp(f_u_xlat5.x, 0.0f, 1.0f);
    f_u_xlat10 = (f_u_xlat5.x * (-2.0f)) + 3.0f;
    f_u_xlat5.x *= f_u_xlat5.x;
    f_u_xlat5.x *= f_u_xlat10;
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_2 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat1 = (f_u_xlat16_1 * _Color.xyz) + (-f_u_xlat10_2);
    f_u_xlat5 = (f_u_xlat5.xxx * f_u_xlat1) + f_u_xlat10_2;
    f_u_xlat16_4 = f_u_xlat5 * _LightColor0.xyz;
    f_u_xlat16_3 *= f_u_xlat16_4;
    float3 _503 = f_u_xlat0.xxx * f_u_xlat16_3;
    f_SV_Target0.x = _503.x;
    f_SV_Target0.y = _503.y;
    f_SV_Target0.z = _503.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && !defined(POINT) && defined(POINT_COOKIE) && defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToLight;
float4x4 unity_MatrixV;
float4 _MainTex_ST;
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
float4 _LightPositionRange;
float4 _LightProjectionParams;
TextureCube<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
TextureCube<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
Texture2D<float4> _LightTextureB0;
SamplerState sampler_LightTextureB0;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
TextureCube<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
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
static float v_u_xlat10;
static float3 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_vs_TEXCOORD1 = v_u_xlat10.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    float3 _161 = v_u_xlat0.yyy * transpose(unity_WorldToLight)[1].xyz;
    v_u_xlat1.x = _161.x;
    v_u_xlat1.y = _161.y;
    v_u_xlat1.z = _161.z;
    float4 _174 = v_u_xlat1;
    float3 _176 = (transpose(unity_WorldToLight)[0].xyz * v_u_xlat0.xxx) + _174.xyz;
    v_u_xlat1.x = _176.x;
    v_u_xlat1.y = _176.y;
    v_u_xlat1.z = _176.z;
    float4 _186 = v_u_xlat0;
    float3 _191 = (transpose(unity_WorldToLight)[2].xyz * _186.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _191.x;
    v_u_xlat0.y = _191.y;
    v_u_xlat0.z = _191.z;
    v_vs_TEXCOORD4 = (transpose(unity_WorldToLight)[3].xyz * v_u_xlat0.www) + v_u_xlat0.xyz;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
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
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

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
static float3 f_u_xlat6;
static float4 f_u_xlat2;
static float f_u_xlat10;
static float3 f_u_xlat16_3;
static float f_u_xlat15;
static float3 f_u_xlat16_1;
static float3 f_u_xlat10_2;
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
    f_u_xlat5 = f_vs_TEXCOORD2 + (-_LightPositionRange.xyz);
    f_u_xlat1.x = max(abs(f_u_xlat5.y), abs(f_u_xlat5.x));
    f_u_xlat1.x = max(abs(f_u_xlat5.z), f_u_xlat1.x);
    f_u_xlat1.x += (-_LightProjectionParams.z);
    f_u_xlat1.x = max(f_u_xlat1.x, 9.9999997473787516355514526367188e-06f);
    f_u_xlat1.x *= _LightProjectionParams.w;
    f_u_xlat1.x = _LightProjectionParams.y / f_u_xlat1.x;
    f_u_xlat1.x += (-_LightProjectionParams.x);
    f_u_xlat6 = f_u_xlat5 + 0.0078125f.xxx;
    float4 txVec0 = float4(f_u_xlat6, f_u_xlat1.x);
    f_u_xlat2.x = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xyz, txVec0.w);
    f_u_xlat6 = f_u_xlat5 + float3(-0.0078125f, -0.0078125f, 0.0078125f);
    float4 txVec1 = float4(f_u_xlat6, f_u_xlat1.x);
    f_u_xlat2.y = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec1.xyz, txVec1.w);
    f_u_xlat6 = f_u_xlat5 + float3(-0.0078125f, 0.0078125f, -0.0078125f);
    f_u_xlat5 += float3(0.0078125f, -0.0078125f, -0.0078125f);
    float4 txVec2 = float4(f_u_xlat5, f_u_xlat1.x);
    f_u_xlat2.w = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec2.xyz, txVec2.w);
    float4 txVec3 = float4(f_u_xlat6, f_u_xlat1.x);
    f_u_xlat2.z = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec3.xyz, txVec3.w);
    f_u_xlat5.x = dot(f_u_xlat2, 0.25f.xxxx);
    f_u_xlat10 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat5.x = (f_u_xlat5.x * f_u_xlat10) + _LightShadowData.x;
    f_u_xlat16_3.x = (-f_u_xlat5.x) + 1.0f;
    f_u_xlat16_3.x = (f_u_xlat0.x * f_u_xlat16_3.x) + f_u_xlat5.x;
    f_u_xlat0 = f_vs_TEXCOORD2.yyy * transpose(unity_WorldToLight)[1].xyz;
    f_u_xlat0 = (transpose(unity_WorldToLight)[0].xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2].xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3].xyz;
    f_u_xlat15 = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0).w;
    f_u_xlat5.x = _LightTextureB0.Sample(sampler_LightTextureB0, f_u_xlat15.xx).x;
    f_u_xlat0.x *= f_u_xlat5.x;
    f_u_xlat0.x = f_u_xlat16_3.x * f_u_xlat0.x;
    f_u_xlat5 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat1.x = dot(f_u_xlat5, f_u_xlat5);
    f_u_xlat1.x = rsqrt(f_u_xlat1.x);
    f_u_xlat5 *= f_u_xlat1.xxx;
    f_u_xlat16_3.x = dot(f_vs_TEXCOORD1, f_u_xlat5);
    f_u_xlat16_3.x = (f_u_xlat16_3.x * 0.5f) + 0.5f;
    f_u_xlat5.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat10 = (-f_u_xlat5.x) + f_u_xlat16_3.x;
    f_u_xlat15 = _RampSmooth + _RampThreshold;
    f_u_xlat5.x = (-f_u_xlat5.x) + f_u_xlat15;
    f_u_xlat5.x = 1.0f / f_u_xlat5.x;
    f_u_xlat5.x *= f_u_xlat10;
    f_u_xlat5.x = clamp(f_u_xlat5.x, 0.0f, 1.0f);
    f_u_xlat10 = (f_u_xlat5.x * (-2.0f)) + 3.0f;
    f_u_xlat5.x *= f_u_xlat5.x;
    f_u_xlat5.x *= f_u_xlat10;
    f_u_xlat16_3 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_3 = (f_u_xlat5.xxx * f_u_xlat16_3) + _SColor.xyz;
    f_u_xlat5.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat10 = f_u_xlat5.x * _Progress;
    f_u_xlat10 = (_Feather * (-2.0f)) + f_u_xlat10;
    f_u_xlat5.x = (_Progress * f_u_xlat5.x) + (-f_u_xlat10);
    f_u_xlat10 = (-f_u_xlat10) + f_vs_TEXCOORD3.x;
    f_u_xlat5.x = 1.0f / f_u_xlat5.x;
    f_u_xlat5.x *= f_u_xlat10;
    f_u_xlat5.x = clamp(f_u_xlat5.x, 0.0f, 1.0f);
    f_u_xlat10 = (f_u_xlat5.x * (-2.0f)) + 3.0f;
    f_u_xlat5.x *= f_u_xlat5.x;
    f_u_xlat5.x *= f_u_xlat10;
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_2 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat1 = (f_u_xlat16_1 * _Color.xyz) + (-f_u_xlat10_2);
    f_u_xlat5 = (f_u_xlat5.xxx * f_u_xlat1) + f_u_xlat10_2;
    f_u_xlat16_4 = f_u_xlat5 * _LightColor0.xyz;
    f_u_xlat16_3 *= f_u_xlat16_4;
    float3 _517 = f_u_xlat0.xxx * f_u_xlat16_3;
    f_SV_Target0.x = _517.x;
    f_SV_Target0.y = _517.y;
    f_SV_Target0.z = _517.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && !defined(POINT) && !defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToLight;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
float4 _MainTex_ST;
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
Texture2D<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
Texture2D<float4> _LightTextureB0;
SamplerState sampler_LightTextureB0;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
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
static float v_u_xlat10;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_vs_TEXCOORD1 = v_u_xlat10.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_WorldToLight)[1];
    v_u_xlat1 = (transpose(unity_WorldToLight)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_WorldToLight)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_vs_TEXCOORD4 = (transpose(unity_WorldToLight)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
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
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

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

static float4 f_u_xlat0;
static float3 f_u_xlat4;
static float4 f_u_xlat1;
static float f_u_xlat8;
static float f_u_xlat16_2;
static float f_u_xlat12;
static bool f_u_xlatb1;
static float3 f_u_xlat16_6;
static float3 f_u_xlat16_4;
static float3 f_u_xlat10_1;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    float3 _19 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = _19.x;
    f_u_xlat0.y = _19.y;
    f_u_xlat0.z = _19.z;
    f_u_xlat0.x = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat4 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat1.xyz);
    f_u_xlat0.x = (-f_u_xlat4.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat4.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat1 = f_vs_TEXCOORD2.yyyy * RecoveryColumn_unity_WorldToShadow(1);
    f_u_xlat1 = (RecoveryColumn_unity_WorldToShadow(0) * f_vs_TEXCOORD2.xxxx) + f_u_xlat1;
    f_u_xlat1 = (RecoveryColumn_unity_WorldToShadow(2) * f_vs_TEXCOORD2.zzzz) + f_u_xlat1;
    f_u_xlat1 += RecoveryColumn_unity_WorldToShadow(3);
    f_u_xlat4 = f_u_xlat1.xyz / f_u_xlat1.www;
    float3 txVec0 = float3(f_u_xlat4.xy, f_u_xlat4.z);
    f_u_xlat4.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat8 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + _LightShadowData.x;
    f_u_xlat16_2 = (-f_u_xlat4.x) + 1.0f;
    f_u_xlat16_2 = (f_u_xlat0.x * f_u_xlat16_2) + f_u_xlat4.x;
    f_u_xlat0 = f_vs_TEXCOORD2.yyyy * transpose(unity_WorldToLight)[1];
    f_u_xlat0 = (transpose(unity_WorldToLight)[0] * f_vs_TEXCOORD2.xxxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2] * f_vs_TEXCOORD2.zzzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3];
    float2 _211 = f_u_xlat0.xy / f_u_xlat0.ww;
    f_u_xlat1.x = _211.x;
    f_u_xlat1.y = _211.y;
    float4 _216 = f_u_xlat1;
    float2 _220 = _216.xy + 0.5f.xx;
    f_u_xlat1.x = _220.x;
    f_u_xlat1.y = _220.y;
    f_u_xlat12 = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat1.xy).w;
    f_u_xlatb1 = 0.0f < f_u_xlat0.z;
    f_u_xlat0.x = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat0.x = _LightTextureB0.Sample(sampler_LightTextureB0, f_u_xlat0.xx).x;
    f_u_xlat16_6.x = float(f_u_xlatb1);
    f_u_xlat16_6.x = f_u_xlat12 * f_u_xlat16_6.x;
    f_u_xlat16_6.x = f_u_xlat0.x * f_u_xlat16_6.x;
    f_u_xlat16_2 *= f_u_xlat16_6.x;
    float3 _278 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat0.x = _278.x;
    f_u_xlat0.y = _278.y;
    f_u_xlat0.z = _278.z;
    f_u_xlat12 = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat12 = rsqrt(f_u_xlat12);
    float4 _294 = f_u_xlat0;
    float3 _296 = f_u_xlat12.xxx * _294.xyz;
    f_u_xlat0.x = _296.x;
    f_u_xlat0.y = _296.y;
    f_u_xlat0.z = _296.z;
    f_u_xlat16_6.x = dot(f_vs_TEXCOORD1, f_u_xlat0.xyz);
    f_u_xlat16_6.x = (f_u_xlat16_6.x * 0.5f) + 0.5f;
    f_u_xlat0.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat0.x) + f_u_xlat16_6.x;
    f_u_xlat8 = _RampSmooth + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_6 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_6 = (f_u_xlat0.xxx * f_u_xlat16_6) + _SColor.xyz;
    f_u_xlat0.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat4.x = f_u_xlat0.x * _Progress;
    f_u_xlat4.x = (_Feather * (-2.0f)) + f_u_xlat4.x;
    f_u_xlat0.x = (_Progress * f_u_xlat0.x) + (-f_u_xlat4.x);
    f_u_xlat4.x = (-f_u_xlat4.x) + f_vs_TEXCOORD3.x;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_1 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat4 = (f_u_xlat16_4 * _Color.xyz) + (-f_u_xlat10_1);
    float4 _474 = f_u_xlat0;
    float3 _479 = (_474.xxx * f_u_xlat4) + f_u_xlat10_1;
    f_u_xlat0.x = _479.x;
    f_u_xlat0.y = _479.y;
    f_u_xlat0.z = _479.z;
    f_u_xlat16_3 = f_u_xlat0.xyz * _LightColor0.xyz;
    f_u_xlat16_6 *= f_u_xlat16_3;
    float3 _501 = f_u_xlat16_2.xxx * f_u_xlat16_6;
    f_SV_Target0.x = _501.x;
    f_SV_Target0.y = _501.y;
    f_SV_Target0.z = _501.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && !defined(POINT) && !defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
float4 _MainTex_ST;
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
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
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD1 = v_u_xlat6.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

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
static float3 f_u_xlat4;
static float3 f_u_xlat16_4;
static float3 f_u_xlat10_1;
static float3 f_u_xlat16_2;
static float f_u_xlat16_14;
static float f_u_xlat8;
static float3 f_u_xlat16_3;
static float3 f_u_xlat1;

void f_frag_main()
{
    f_u_xlat0.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat4.x = f_u_xlat0.x * _Progress;
    f_u_xlat4.x = (_Feather * (-2.0f)) + f_u_xlat4.x;
    f_u_xlat0.x = (_Progress * f_u_xlat0.x) + (-f_u_xlat4.x);
    f_u_xlat4.x = (-f_u_xlat4.x) + f_vs_TEXCOORD3.x;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_1 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat4 = (f_u_xlat16_4 * _Color.xyz) + (-f_u_xlat10_1);
    f_u_xlat0 = (f_u_xlat0.xxx * f_u_xlat4) + f_u_xlat10_1;
    f_u_xlat16_2 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_14 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_14 = (f_u_xlat16_14 * 0.5f) + 0.5f;
    f_u_xlat0.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat0.x) + f_u_xlat16_14;
    f_u_xlat8 = _RampSmooth + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_3 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_3 = (f_u_xlat0.xxx * f_u_xlat16_3) + _SColor.xyz;
    f_u_xlat16_2 *= f_u_xlat16_3;
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat4 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat4.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat4.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4 = f_vs_TEXCOORD2.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat4;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat4;
    f_u_xlat4 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat4.xy, f_u_xlat4.z);
    f_u_xlat4.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat8 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + _LightShadowData.x;
    f_u_xlat16_14 = (-f_u_xlat4.x) + 1.0f;
    f_u_xlat16_14 = (f_u_xlat0.x * f_u_xlat16_14) + f_u_xlat4.x;
    float3 _365 = f_u_xlat16_14.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _365.x;
    f_SV_Target0.y = _365.y;
    f_SV_Target0.z = _365.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(DIRECTIONAL) && defined(DIRECTIONAL_COOKIE) && !defined(POINT) && !defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToLight;
float4x4 unity_WorldToObject;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
float4 _MainTex_ST;
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
Texture2D<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
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
static float2 v_vs_TEXCOORD4;
static float v_u_xlat9;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    float2 _106 = v_u_xlat1.yy * transpose(unity_WorldToLight)[1].xy;
    v_u_xlat0.x = _106.x;
    v_u_xlat0.y = _106.y;
    float4 _120 = v_u_xlat0;
    float2 _122 = (transpose(unity_WorldToLight)[0].xy * v_u_xlat1.xx) + _120.xy;
    v_u_xlat0.x = _122.x;
    v_u_xlat0.y = _122.y;
    float4 _133 = v_u_xlat0;
    float2 _135 = (transpose(unity_WorldToLight)[2].xy * v_u_xlat1.zz) + _133.xy;
    v_u_xlat0.x = _135.x;
    v_u_xlat0.y = _135.y;
    v_vs_TEXCOORD4 = (transpose(unity_WorldToLight)[3].xy * v_u_xlat1.ww) + v_u_xlat0.xy;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    v_vs_TEXCOORD1 = v_u_xlat9.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

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
static float3 f_u_xlat4;
static float3 f_u_xlat16_4;
static float3 f_u_xlat10_1;
static float3 f_u_xlat16_2;
static float f_u_xlat16_14;
static float f_u_xlat8;
static float3 f_u_xlat16_3;
static float3 f_u_xlat1;

void f_frag_main()
{
    f_u_xlat0.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat4.x = f_u_xlat0.x * _Progress;
    f_u_xlat4.x = (_Feather * (-2.0f)) + f_u_xlat4.x;
    f_u_xlat0.x = (_Progress * f_u_xlat0.x) + (-f_u_xlat4.x);
    f_u_xlat4.x = (-f_u_xlat4.x) + f_vs_TEXCOORD3.x;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_1 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat4 = (f_u_xlat16_4 * _Color.xyz) + (-f_u_xlat10_1);
    f_u_xlat0 = (f_u_xlat0.xxx * f_u_xlat4) + f_u_xlat10_1;
    f_u_xlat16_2 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_14 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_14 = (f_u_xlat16_14 * 0.5f) + 0.5f;
    f_u_xlat0.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat0.x) + f_u_xlat16_14;
    f_u_xlat8 = _RampSmooth + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_3 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_3 = (f_u_xlat0.xxx * f_u_xlat16_3) + _SColor.xyz;
    f_u_xlat16_2 *= f_u_xlat16_3;
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat4 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat4.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat4.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4 = f_vs_TEXCOORD2.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat4;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat4;
    f_u_xlat4 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat4.xy, f_u_xlat4.z);
    f_u_xlat4.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat8 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + _LightShadowData.x;
    f_u_xlat16_14 = (-f_u_xlat4.x) + 1.0f;
    f_u_xlat16_14 = (f_u_xlat0.x * f_u_xlat16_14) + f_u_xlat4.x;
    float2 _366 = f_vs_TEXCOORD2.yy * transpose(unity_WorldToLight)[1].xy;
    f_u_xlat0.x = _366.x;
    f_u_xlat0.y = _366.y;
    float3 _377 = f_u_xlat0;
    float2 _379 = (transpose(unity_WorldToLight)[0].xy * f_vs_TEXCOORD2.xx) + _377.xy;
    f_u_xlat0.x = _379.x;
    f_u_xlat0.y = _379.y;
    float3 _390 = f_u_xlat0;
    float2 _392 = (transpose(unity_WorldToLight)[2].xy * f_vs_TEXCOORD2.zz) + _390.xy;
    f_u_xlat0.x = _392.x;
    f_u_xlat0.y = _392.y;
    float3 _397 = f_u_xlat0;
    float2 _402 = _397.xy + transpose(unity_WorldToLight)[3].xy;
    f_u_xlat0.x = _402.x;
    f_u_xlat0.y = _402.y;
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0.xy).w;
    f_u_xlat0.x = f_u_xlat16_14 * f_u_xlat0.x;
    float3 _424 = f_u_xlat0.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _424.x;
    f_SV_Target0.y = _424.y;
    f_SV_Target0.z = _424.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && defined(POINT) && !defined(POINT_COOKIE) && defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToLight;
float4x4 unity_MatrixV;
float4 _MainTex_ST;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
float4 _LightPositionRange;
float4 _LightProjectionParams;
TextureCube<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
Texture2D<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
TextureCube<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
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
static float v_u_xlat10;
static float3 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_vs_TEXCOORD1 = v_u_xlat10.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    float3 _161 = v_u_xlat0.yyy * transpose(unity_WorldToLight)[1].xyz;
    v_u_xlat1.x = _161.x;
    v_u_xlat1.y = _161.y;
    v_u_xlat1.z = _161.z;
    float4 _174 = v_u_xlat1;
    float3 _176 = (transpose(unity_WorldToLight)[0].xyz * v_u_xlat0.xxx) + _174.xyz;
    v_u_xlat1.x = _176.x;
    v_u_xlat1.y = _176.y;
    v_u_xlat1.z = _176.z;
    float4 _186 = v_u_xlat0;
    float3 _191 = (transpose(unity_WorldToLight)[2].xyz * _186.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _191.x;
    v_u_xlat0.y = _191.y;
    v_u_xlat0.z = _191.z;
    v_vs_TEXCOORD4 = (transpose(unity_WorldToLight)[3].xyz * v_u_xlat0.www) + v_u_xlat0.xyz;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
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
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

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
static float f_u_xlat12;
static float3 f_u_xlat16_1;
static float3 f_u_xlat4;
static float f_u_xlat8;
static float3 f_u_xlat16_4;
static float3 f_u_xlat10_2;
static float3 f_u_xlat16_3;
static float3 f_u_xlat2;
static float f_u_xlat16_13;

void f_frag_main()
{
    f_u_xlat0 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat12 = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat12 = rsqrt(f_u_xlat12);
    f_u_xlat0 = f_u_xlat12.xxx * f_u_xlat0;
    f_u_xlat16_1.x = dot(f_vs_TEXCOORD1, f_u_xlat0);
    f_u_xlat16_1.x = (f_u_xlat16_1.x * 0.5f) + 0.5f;
    f_u_xlat0.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat0.x) + f_u_xlat16_1.x;
    f_u_xlat8 = _RampSmooth + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_1 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_1 = (f_u_xlat0.xxx * f_u_xlat16_1) + _SColor.xyz;
    f_u_xlat0.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat4.x = f_u_xlat0.x * _Progress;
    f_u_xlat4.x = (_Feather * (-2.0f)) + f_u_xlat4.x;
    f_u_xlat0.x = (_Progress * f_u_xlat0.x) + (-f_u_xlat4.x);
    f_u_xlat4.x = (-f_u_xlat4.x) + f_vs_TEXCOORD3.x;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_2 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat4 = (f_u_xlat16_4 * _Color.xyz) + (-f_u_xlat10_2);
    f_u_xlat0 = (f_u_xlat0.xxx * f_u_xlat4) + f_u_xlat10_2;
    f_u_xlat16_3 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_1 *= f_u_xlat16_3;
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat4 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat2.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat2.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat2.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat2);
    f_u_xlat0.x = (-f_u_xlat4.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat4.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4 = f_vs_TEXCOORD2 + (-_LightPositionRange.xyz);
    f_u_xlat2.x = max(abs(f_u_xlat4.y), abs(f_u_xlat4.x));
    f_u_xlat2.x = max(abs(f_u_xlat4.z), f_u_xlat2.x);
    f_u_xlat2.x += (-_LightProjectionParams.z);
    f_u_xlat2.x = max(f_u_xlat2.x, 9.9999997473787516355514526367188e-06f);
    f_u_xlat2.x *= _LightProjectionParams.w;
    f_u_xlat2.x = _LightProjectionParams.y / f_u_xlat2.x;
    f_u_xlat2.x += (-_LightProjectionParams.x);
    float4 txVec0 = float4(f_u_xlat4, f_u_xlat2.x);
    f_u_xlat4.x = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xyz, txVec0.w);
    f_u_xlat8 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + _LightShadowData.x;
    f_u_xlat16_13 = (-f_u_xlat4.x) + 1.0f;
    f_u_xlat16_13 = (f_u_xlat0.x * f_u_xlat16_13) + f_u_xlat4.x;
    f_u_xlat0 = f_vs_TEXCOORD2.yyy * transpose(unity_WorldToLight)[1].xyz;
    f_u_xlat0 = (transpose(unity_WorldToLight)[0].xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2].xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3].xyz;
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0.xx).x;
    f_u_xlat0.x = f_u_xlat16_13 * f_u_xlat0.x;
    float3 _449 = f_u_xlat0.xxx * f_u_xlat16_1;
    f_SV_Target0.x = _449.x;
    f_SV_Target0.y = _449.y;
    f_SV_Target0.z = _449.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && !defined(POINT) && defined(POINT_COOKIE) && defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToLight;
float4x4 unity_MatrixV;
float4 _MainTex_ST;
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
float4 _LightPositionRange;
float4 _LightProjectionParams;
TextureCube<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
TextureCube<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
Texture2D<float4> _LightTextureB0;
SamplerState sampler_LightTextureB0;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
TextureCube<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
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
static float v_u_xlat10;
static float3 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_vs_TEXCOORD1 = v_u_xlat10.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    float3 _161 = v_u_xlat0.yyy * transpose(unity_WorldToLight)[1].xyz;
    v_u_xlat1.x = _161.x;
    v_u_xlat1.y = _161.y;
    v_u_xlat1.z = _161.z;
    float4 _174 = v_u_xlat1;
    float3 _176 = (transpose(unity_WorldToLight)[0].xyz * v_u_xlat0.xxx) + _174.xyz;
    v_u_xlat1.x = _176.x;
    v_u_xlat1.y = _176.y;
    v_u_xlat1.z = _176.z;
    float4 _186 = v_u_xlat0;
    float3 _191 = (transpose(unity_WorldToLight)[2].xyz * _186.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _191.x;
    v_u_xlat0.y = _191.y;
    v_u_xlat0.z = _191.z;
    v_vs_TEXCOORD4 = (transpose(unity_WorldToLight)[3].xyz * v_u_xlat0.www) + v_u_xlat0.xyz;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
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
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

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
static float f_u_xlat15;
static float3 f_u_xlat16_1;
static float3 f_u_xlat10_3;
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
    f_u_xlat5 = f_vs_TEXCOORD2 + (-_LightPositionRange.xyz);
    f_u_xlat1.x = max(abs(f_u_xlat5.y), abs(f_u_xlat5.x));
    f_u_xlat1.x = max(abs(f_u_xlat5.z), f_u_xlat1.x);
    f_u_xlat1.x += (-_LightProjectionParams.z);
    f_u_xlat1.x = max(f_u_xlat1.x, 9.9999997473787516355514526367188e-06f);
    f_u_xlat1.x *= _LightProjectionParams.w;
    f_u_xlat1.x = _LightProjectionParams.y / f_u_xlat1.x;
    f_u_xlat1.x += (-_LightProjectionParams.x);
    float4 txVec0 = float4(f_u_xlat5, f_u_xlat1.x);
    f_u_xlat5.x = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xyz, txVec0.w);
    f_u_xlat10 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat5.x = (f_u_xlat5.x * f_u_xlat10) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat5.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat5.x;
    f_u_xlat0 = f_vs_TEXCOORD2.yyy * transpose(unity_WorldToLight)[1].xyz;
    f_u_xlat0 = (transpose(unity_WorldToLight)[0].xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2].xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3].xyz;
    f_u_xlat15 = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0).w;
    f_u_xlat5.x = _LightTextureB0.Sample(sampler_LightTextureB0, f_u_xlat15.xx).x;
    f_u_xlat0.x *= f_u_xlat5.x;
    f_u_xlat0.x = f_u_xlat16_2.x * f_u_xlat0.x;
    f_u_xlat5 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat1.x = dot(f_u_xlat5, f_u_xlat5);
    f_u_xlat1.x = rsqrt(f_u_xlat1.x);
    f_u_xlat5 *= f_u_xlat1.xxx;
    f_u_xlat16_2.x = dot(f_vs_TEXCOORD1, f_u_xlat5);
    f_u_xlat16_2.x = (f_u_xlat16_2.x * 0.5f) + 0.5f;
    f_u_xlat5.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat10 = (-f_u_xlat5.x) + f_u_xlat16_2.x;
    f_u_xlat15 = _RampSmooth + _RampThreshold;
    f_u_xlat5.x = (-f_u_xlat5.x) + f_u_xlat15;
    f_u_xlat5.x = 1.0f / f_u_xlat5.x;
    f_u_xlat5.x *= f_u_xlat10;
    f_u_xlat5.x = clamp(f_u_xlat5.x, 0.0f, 1.0f);
    f_u_xlat10 = (f_u_xlat5.x * (-2.0f)) + 3.0f;
    f_u_xlat5.x *= f_u_xlat5.x;
    f_u_xlat5.x *= f_u_xlat10;
    f_u_xlat16_2 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_2 = (f_u_xlat5.xxx * f_u_xlat16_2) + _SColor.xyz;
    f_u_xlat5.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat10 = f_u_xlat5.x * _Progress;
    f_u_xlat10 = (_Feather * (-2.0f)) + f_u_xlat10;
    f_u_xlat5.x = (_Progress * f_u_xlat5.x) + (-f_u_xlat10);
    f_u_xlat10 = (-f_u_xlat10) + f_vs_TEXCOORD3.x;
    f_u_xlat5.x = 1.0f / f_u_xlat5.x;
    f_u_xlat5.x *= f_u_xlat10;
    f_u_xlat5.x = clamp(f_u_xlat5.x, 0.0f, 1.0f);
    f_u_xlat10 = (f_u_xlat5.x * (-2.0f)) + 3.0f;
    f_u_xlat5.x *= f_u_xlat5.x;
    f_u_xlat5.x *= f_u_xlat10;
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_3 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat1 = (f_u_xlat16_1 * _Color.xyz) + (-f_u_xlat10_3);
    f_u_xlat5 = (f_u_xlat5.xxx * f_u_xlat1) + f_u_xlat10_3;
    f_u_xlat16_4 = f_u_xlat5 * _LightColor0.xyz;
    f_u_xlat16_2 *= f_u_xlat16_4;
    float3 _456 = f_u_xlat0.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _456.x;
    f_SV_Target0.y = _456.y;
    f_SV_Target0.z = _456.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && defined(POINT) && !defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToLight;
float4 _MainTex_ST;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
Texture2D<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
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
static float v_u_xlat10;
static float3 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_vs_TEXCOORD1 = v_u_xlat10.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    float3 _161 = v_u_xlat0.yyy * transpose(unity_WorldToLight)[1].xyz;
    v_u_xlat1.x = _161.x;
    v_u_xlat1.y = _161.y;
    v_u_xlat1.z = _161.z;
    float4 _174 = v_u_xlat1;
    float3 _176 = (transpose(unity_WorldToLight)[0].xyz * v_u_xlat0.xxx) + _174.xyz;
    v_u_xlat1.x = _176.x;
    v_u_xlat1.y = _176.y;
    v_u_xlat1.z = _176.z;
    float4 _186 = v_u_xlat0;
    float3 _191 = (transpose(unity_WorldToLight)[2].xyz * _186.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _191.x;
    v_u_xlat0.y = _191.y;
    v_u_xlat0.z = _191.z;
    v_vs_TEXCOORD4 = (transpose(unity_WorldToLight)[3].xyz * v_u_xlat0.www) + v_u_xlat0.xyz;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
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
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

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
static float f_u_xlat12;
static float3 f_u_xlat16_1;
static float3 f_u_xlat4;
static float f_u_xlat8;
static float3 f_u_xlat16_4;
static float3 f_u_xlat10_2;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat12 = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat12 = rsqrt(f_u_xlat12);
    f_u_xlat0 = f_u_xlat12.xxx * f_u_xlat0;
    f_u_xlat16_1.x = dot(f_vs_TEXCOORD1, f_u_xlat0);
    f_u_xlat16_1.x = (f_u_xlat16_1.x * 0.5f) + 0.5f;
    f_u_xlat0.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat0.x) + f_u_xlat16_1.x;
    f_u_xlat8 = _RampSmooth + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_1 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_1 = (f_u_xlat0.xxx * f_u_xlat16_1) + _SColor.xyz;
    f_u_xlat0.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat4.x = f_u_xlat0.x * _Progress;
    f_u_xlat4.x = (_Feather * (-2.0f)) + f_u_xlat4.x;
    f_u_xlat0.x = (_Progress * f_u_xlat0.x) + (-f_u_xlat4.x);
    f_u_xlat4.x = (-f_u_xlat4.x) + f_vs_TEXCOORD3.x;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_2 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat4 = (f_u_xlat16_4 * _Color.xyz) + (-f_u_xlat10_2);
    f_u_xlat0 = (f_u_xlat0.xxx * f_u_xlat4) + f_u_xlat10_2;
    f_u_xlat16_3 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_1 *= f_u_xlat16_3;
    f_u_xlat0 = f_vs_TEXCOORD2.yyy * transpose(unity_WorldToLight)[1].xyz;
    f_u_xlat0 = (transpose(unity_WorldToLight)[0].xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2].xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3].xyz;
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0.xx).x;
    float3 _281 = f_u_xlat0.xxx * f_u_xlat16_1;
    f_SV_Target0.x = _281.x;
    f_SV_Target0.y = _281.y;
    f_SV_Target0.z = _281.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && !defined(POINT) && !defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4 _MainTex_ST;
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
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
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD1 = v_u_xlat6.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;

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

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float3 f_u_xlat16_4;
static float3 f_u_xlat10_1;
static float3 f_u_xlat16_2;
static float f_u_xlat16_14;
static float f_u_xlat8;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat4.x = f_u_xlat0.x * _Progress;
    f_u_xlat4.x = (_Feather * (-2.0f)) + f_u_xlat4.x;
    f_u_xlat0.x = (_Progress * f_u_xlat0.x) + (-f_u_xlat4.x);
    f_u_xlat4.x = (-f_u_xlat4.x) + f_vs_TEXCOORD3.x;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_1 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat4 = (f_u_xlat16_4 * _Color.xyz) + (-f_u_xlat10_1);
    f_u_xlat0 = (f_u_xlat0.xxx * f_u_xlat4) + f_u_xlat10_1;
    f_u_xlat16_2 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_14 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_14 = (f_u_xlat16_14 * 0.5f) + 0.5f;
    f_u_xlat0.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat0.x) + f_u_xlat16_14;
    f_u_xlat8 = _RampSmooth + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_3 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_3 = (f_u_xlat0.xxx * f_u_xlat16_3) + _SColor.xyz;
    float3 _212 = f_u_xlat16_2 * f_u_xlat16_3;
    f_SV_Target0.x = _212.x;
    f_SV_Target0.y = _212.y;
    f_SV_Target0.z = _212.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && !defined(POINT) && !defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToLight;
float4 _MainTex_ST;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
Texture2D<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
Texture2D<float4> _LightTextureB0;
SamplerState sampler_LightTextureB0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
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
static float v_u_xlat10;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_vs_TEXCOORD1 = v_u_xlat10.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_WorldToLight)[1];
    v_u_xlat1 = (transpose(unity_WorldToLight)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_WorldToLight)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_vs_TEXCOORD4 = (transpose(unity_WorldToLight)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
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
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

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

static float4 f_u_xlat0;
static float f_u_xlat12;
static float3 f_u_xlat16_1;
static float3 f_u_xlat4;
static float f_u_xlat8;
static float3 f_u_xlat16_4;
static float3 f_u_xlat10_2;
static float3 f_u_xlat16_3;
static float2 f_u_xlat2;
static bool f_u_xlatb2;
static float f_u_xlat16_13;

void f_frag_main()
{
    float3 _19 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat0.x = _19.x;
    f_u_xlat0.y = _19.y;
    f_u_xlat0.z = _19.z;
    f_u_xlat12 = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat12 = rsqrt(f_u_xlat12);
    float4 _41 = f_u_xlat0;
    float3 _43 = f_u_xlat12.xxx * _41.xyz;
    f_u_xlat0.x = _43.x;
    f_u_xlat0.y = _43.y;
    f_u_xlat0.z = _43.z;
    f_u_xlat16_1.x = dot(f_vs_TEXCOORD1, f_u_xlat0.xyz);
    f_u_xlat16_1.x = (f_u_xlat16_1.x * 0.5f) + 0.5f;
    f_u_xlat0.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat0.x) + f_u_xlat16_1.x;
    f_u_xlat8 = _RampSmooth + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_1 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_1 = (f_u_xlat0.xxx * f_u_xlat16_1) + _SColor.xyz;
    f_u_xlat0.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat4.x = f_u_xlat0.x * _Progress;
    f_u_xlat4.x = (_Feather * (-2.0f)) + f_u_xlat4.x;
    f_u_xlat0.x = (_Progress * f_u_xlat0.x) + (-f_u_xlat4.x);
    f_u_xlat4.x = (-f_u_xlat4.x) + f_vs_TEXCOORD3.x;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_2 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat4 = (f_u_xlat16_4 * _Color.xyz) + (-f_u_xlat10_2);
    float4 _233 = f_u_xlat0;
    float3 _238 = (_233.xxx * f_u_xlat4) + f_u_xlat10_2;
    f_u_xlat0.x = _238.x;
    f_u_xlat0.y = _238.y;
    f_u_xlat0.z = _238.z;
    f_u_xlat16_3 = f_u_xlat0.xyz * _LightColor0.xyz;
    f_u_xlat16_1 *= f_u_xlat16_3;
    f_u_xlat0 = f_vs_TEXCOORD2.yyyy * transpose(unity_WorldToLight)[1];
    f_u_xlat0 = (transpose(unity_WorldToLight)[0] * f_vs_TEXCOORD2.xxxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2] * f_vs_TEXCOORD2.zzzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3];
    f_u_xlat2 = f_u_xlat0.xy / f_u_xlat0.ww;
    f_u_xlat2 += 0.5f.xx;
    f_u_xlat12 = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat2).w;
    f_u_xlatb2 = 0.0f < f_u_xlat0.z;
    f_u_xlat0.x = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat0.x = _LightTextureB0.Sample(sampler_LightTextureB0, f_u_xlat0.xx).x;
    f_u_xlat16_13 = float(f_u_xlatb2);
    f_u_xlat16_13 = f_u_xlat12 * f_u_xlat16_13;
    f_u_xlat16_13 = f_u_xlat0.x * f_u_xlat16_13;
    float3 _337 = f_u_xlat16_13.xxx * f_u_xlat16_1;
    f_SV_Target0.x = _337.x;
    f_SV_Target0.y = _337.y;
    f_SV_Target0.z = _337.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && !defined(POINT) && defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToLight;
float4 _MainTex_ST;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
TextureCube<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
Texture2D<float4> _LightTextureB0;
SamplerState sampler_LightTextureB0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
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
static float v_u_xlat10;
static float3 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_vs_TEXCOORD1 = v_u_xlat10.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    float3 _161 = v_u_xlat0.yyy * transpose(unity_WorldToLight)[1].xyz;
    v_u_xlat1.x = _161.x;
    v_u_xlat1.y = _161.y;
    v_u_xlat1.z = _161.z;
    float4 _174 = v_u_xlat1;
    float3 _176 = (transpose(unity_WorldToLight)[0].xyz * v_u_xlat0.xxx) + _174.xyz;
    v_u_xlat1.x = _176.x;
    v_u_xlat1.y = _176.y;
    v_u_xlat1.z = _176.z;
    float4 _186 = v_u_xlat0;
    float3 _191 = (transpose(unity_WorldToLight)[2].xyz * _186.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _191.x;
    v_u_xlat0.y = _191.y;
    v_u_xlat0.z = _191.z;
    v_vs_TEXCOORD4 = (transpose(unity_WorldToLight)[3].xyz * v_u_xlat0.www) + v_u_xlat0.xyz;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
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
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

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
static float f_u_xlat12;
static float3 f_u_xlat16_1;
static float3 f_u_xlat4;
static float f_u_xlat8;
static float3 f_u_xlat16_4;
static float3 f_u_xlat10_2;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat12 = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat12 = rsqrt(f_u_xlat12);
    f_u_xlat0 = f_u_xlat12.xxx * f_u_xlat0;
    f_u_xlat16_1.x = dot(f_vs_TEXCOORD1, f_u_xlat0);
    f_u_xlat16_1.x = (f_u_xlat16_1.x * 0.5f) + 0.5f;
    f_u_xlat0.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat0.x) + f_u_xlat16_1.x;
    f_u_xlat8 = _RampSmooth + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_1 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_1 = (f_u_xlat0.xxx * f_u_xlat16_1) + _SColor.xyz;
    f_u_xlat0.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat4.x = f_u_xlat0.x * _Progress;
    f_u_xlat4.x = (_Feather * (-2.0f)) + f_u_xlat4.x;
    f_u_xlat0.x = (_Progress * f_u_xlat0.x) + (-f_u_xlat4.x);
    f_u_xlat4.x = (-f_u_xlat4.x) + f_vs_TEXCOORD3.x;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_2 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat4 = (f_u_xlat16_4 * _Color.xyz) + (-f_u_xlat10_2);
    f_u_xlat0 = (f_u_xlat0.xxx * f_u_xlat4) + f_u_xlat10_2;
    f_u_xlat16_3 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_1 *= f_u_xlat16_3;
    f_u_xlat0 = f_vs_TEXCOORD2.yyy * transpose(unity_WorldToLight)[1].xyz;
    f_u_xlat0 = (transpose(unity_WorldToLight)[0].xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2].xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3].xyz;
    f_u_xlat12 = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0).w;
    f_u_xlat4.x = _LightTextureB0.Sample(sampler_LightTextureB0, f_u_xlat12.xx).x;
    f_u_xlat0.x *= f_u_xlat4.x;
    float3 _296 = f_u_xlat0.xxx * f_u_xlat16_1;
    f_SV_Target0.x = _296.x;
    f_SV_Target0.y = _296.y;
    f_SV_Target0.z = _296.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(DIRECTIONAL) && defined(DIRECTIONAL_COOKIE) && !defined(POINT) && !defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToLight;
float4x4 unity_WorldToObject;
float4 _MainTex_ST;
float _Feather;
float _Progress;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _StoneTex;
SamplerState sampler_StoneTex;
float4 _Color;
float4 _LightColor0;
float4 _WorldSpaceLightPos0;
float _RampSmooth;
float _RampThreshold;
float4 _HColor;
float4 _SColor;
Texture2D<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD3;
static float4 v_in_COLOR0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_COLOR0 : COLOR0;
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
static float2 v_vs_TEXCOORD4;
static float v_u_xlat9;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    float2 _106 = v_u_xlat1.yy * transpose(unity_WorldToLight)[1].xy;
    v_u_xlat0.x = _106.x;
    v_u_xlat0.y = _106.y;
    float4 _120 = v_u_xlat0;
    float2 _122 = (transpose(unity_WorldToLight)[0].xy * v_u_xlat1.xx) + _120.xy;
    v_u_xlat0.x = _122.x;
    v_u_xlat0.y = _122.y;
    float4 _133 = v_u_xlat0;
    float2 _135 = (transpose(unity_WorldToLight)[2].xy * v_u_xlat1.zz) + _133.xy;
    v_u_xlat0.x = _135.x;
    v_u_xlat0.y = _135.y;
    v_vs_TEXCOORD4 = (transpose(unity_WorldToLight)[3].xy * v_u_xlat1.ww) + v_u_xlat0.xy;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    v_vs_TEXCOORD1 = v_u_xlat9.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD3 = v_in_COLOR0.xyz;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float3 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

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
static float3 f_u_xlat4;
static float3 f_u_xlat16_4;
static float3 f_u_xlat10_1;
static float3 f_u_xlat16_2;
static float f_u_xlat16_14;
static float f_u_xlat8;
static float3 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat0.x = (_Feather * 2.0f) + 1.0f;
    f_u_xlat4.x = f_u_xlat0.x * _Progress;
    f_u_xlat4.x = (_Feather * (-2.0f)) + f_u_xlat4.x;
    f_u_xlat0.x = (_Progress * f_u_xlat0.x) + (-f_u_xlat4.x);
    f_u_xlat4.x = (-f_u_xlat4.x) + f_vs_TEXCOORD3.x;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat10_1 = _StoneTex.Sample(sampler_StoneTex, f_vs_TEXCOORD0).xyz;
    f_u_xlat4 = (f_u_xlat16_4 * _Color.xyz) + (-f_u_xlat10_1);
    f_u_xlat0 = (f_u_xlat0.xxx * f_u_xlat4) + f_u_xlat10_1;
    f_u_xlat16_2 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_14 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_14 = (f_u_xlat16_14 * 0.5f) + 0.5f;
    f_u_xlat0.x = (-_RampSmooth) + _RampThreshold;
    f_u_xlat4.x = (-f_u_xlat0.x) + f_u_xlat16_14;
    f_u_xlat8 = _RampSmooth + _RampThreshold;
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat0.x = 1.0f / f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4.x = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat16_3 = _HColor.xyz + (-_SColor.xyz);
    f_u_xlat16_3 = (f_u_xlat0.xxx * f_u_xlat16_3) + _SColor.xyz;
    f_u_xlat16_2 *= f_u_xlat16_3;
    float2 _223 = f_vs_TEXCOORD2.yy * transpose(unity_WorldToLight)[1].xy;
    f_u_xlat0.x = _223.x;
    f_u_xlat0.y = _223.y;
    float3 _236 = f_u_xlat0;
    float2 _238 = (transpose(unity_WorldToLight)[0].xy * f_vs_TEXCOORD2.xx) + _236.xy;
    f_u_xlat0.x = _238.x;
    f_u_xlat0.y = _238.y;
    float3 _250 = f_u_xlat0;
    float2 _252 = (transpose(unity_WorldToLight)[2].xy * f_vs_TEXCOORD2.zz) + _250.xy;
    f_u_xlat0.x = _252.x;
    f_u_xlat0.y = _252.y;
    float3 _257 = f_u_xlat0;
    float2 _263 = _257.xy + transpose(unity_WorldToLight)[3].xy;
    f_u_xlat0.x = _263.x;
    f_u_xlat0.y = _263.y;
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0.xy).w;
    float3 _281 = f_u_xlat0.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _281.x;
    f_SV_Target0.y = _281.y;
    f_SV_Target0.z = _281.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
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
