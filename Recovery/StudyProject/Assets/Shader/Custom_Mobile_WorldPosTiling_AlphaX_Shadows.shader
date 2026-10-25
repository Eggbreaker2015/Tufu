Shader "Custom/Mobile_WorldPosTiling_AlphaX_Shadows" {
Properties {
		_MainTex ("Main Texture", 2D) = "white" {}
		_MainTilingWorld ("Main World Tiling", Float) = 1
		_BlendWidthX ("Alpha Edge X", Range(0, 0.5)) = 0.1
	}
// Recovered from the APK GLES programs; generated HLSL, not original author source.
SubShader {
Tags { "Queue"="Geometry+1" "RenderType"="Opaque" }
Pass {
Name "FORWARD"
Tags { "LightMode"="ForwardBase" "Queue"="Geometry+1" "RenderType"="Opaque" "SHADOWSUPPORT"="true" }
Cull Off
ZWrite Off
ZTest LEqual
Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
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
float4 unity_4LightPosX0;
float4 unity_4LightPosY0;
float4 unity_4LightPosZ0;
float4 unity_4LightAtten0;
float4 unity_LightColor[8];
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 _WorldSpaceLightPos0;
float _BlendWidthX;
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
static float v_u_xlat21;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float3 v_u_xlat16_5;
static float4 v_u_xlat16_1;
static float3 v_u_xlat16_6;
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
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat21 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat21 = rsqrt(v_u_xlat21);
    float4 _144 = v_u_xlat1;
    float3 _146 = v_u_xlat21.xxx * _144.xyz;
    v_u_xlat1.x = _146.x;
    v_u_xlat1.y = _146.y;
    v_u_xlat1.z = _146.z;
    v_vs_TEXCOORD1 = v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = v_u_xlat0.xyz;
    v_u_xlat2 = (-v_u_xlat0.xxxx) + unity_4LightPosX0;
    v_u_xlat3 = (-v_u_xlat0.yyyy) + unity_4LightPosY0;
    v_u_xlat0 = (-v_u_xlat0.zzzz) + unity_4LightPosZ0;
    v_u_xlat4 = v_u_xlat1.yyyy * v_u_xlat3;
    v_u_xlat3 *= v_u_xlat3;
    v_u_xlat3 = (v_u_xlat2 * v_u_xlat2) + v_u_xlat3;
    v_u_xlat2 = (v_u_xlat2 * v_u_xlat1.xxxx) + v_u_xlat4;
    v_u_xlat2 = (v_u_xlat0 * v_u_xlat1.zzzz) + v_u_xlat2;
    v_u_xlat0 = (v_u_xlat0 * v_u_xlat0) + v_u_xlat3;
    v_u_xlat0 = max(v_u_xlat0, 9.9999999747524270787835121154785e-07f.xxxx);
    v_u_xlat3 = rsqrt(v_u_xlat0);
    v_u_xlat0 = (v_u_xlat0 * unity_4LightAtten0) + 1.0f.xxxx;
    v_u_xlat0 = 1.0f.xxxx / v_u_xlat0;
    v_u_xlat2 *= v_u_xlat3;
    v_u_xlat2 = max(v_u_xlat2, 0.0f.xxxx);
    v_u_xlat0 *= v_u_xlat2;
    float3 _243 = v_u_xlat0.yyy * unity_LightColor[1].xyz;
    v_u_xlat2.x = _243.x;
    v_u_xlat2.y = _243.y;
    v_u_xlat2.z = _243.z;
    float4 _256 = v_u_xlat2;
    float3 _258 = (unity_LightColor[0].xyz * v_u_xlat0.xxx) + _256.xyz;
    v_u_xlat2.x = _258.x;
    v_u_xlat2.y = _258.y;
    v_u_xlat2.z = _258.z;
    float4 _268 = v_u_xlat0;
    float3 _273 = (unity_LightColor[2].xyz * _268.zzz) + v_u_xlat2.xyz;
    v_u_xlat0.x = _273.x;
    v_u_xlat0.y = _273.y;
    v_u_xlat0.z = _273.z;
    float4 _283 = v_u_xlat0;
    float4 _286 = v_u_xlat0;
    float3 _288 = (unity_LightColor[3].xyz * _283.www) + _286.xyz;
    v_u_xlat0.x = _288.x;
    v_u_xlat0.y = _288.y;
    v_u_xlat0.z = _288.z;
    v_u_xlat16_5.x = v_u_xlat1.y * v_u_xlat1.y;
    v_u_xlat16_5.x = (v_u_xlat1.x * v_u_xlat1.x) + (-v_u_xlat16_5.x);
    v_u_xlat16_1 = v_u_xlat1.yzzx * v_u_xlat1.xyzz;
    v_u_xlat16_6.x = dot(unity_SHBr, v_u_xlat16_1);
    v_u_xlat16_6.y = dot(unity_SHBg, v_u_xlat16_1);
    v_u_xlat16_6.z = dot(unity_SHBb, v_u_xlat16_1);
    v_u_xlat16_5 = (unity_SHC.xyz * v_u_xlat16_5.xxx) + v_u_xlat16_6;
    v_vs_TEXCOORD3 = v_u_xlat0.xyz + v_u_xlat16_5;
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
static float3 f_vs_TEXCOORD3;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

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
static float4 f_u_xlat1;
static float f_u_xlat8;
static float3 f_u_xlat16_2;
static float4 f_u_xlat16_0;
static float3 f_u_xlat16_3;
static float f_u_xlat16_14;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
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
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _188 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _188.x;
    f_u_xlat0.y = _188.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_2 *= f_u_xlat16_0.xyz;
    f_u_xlat1.x = f_vs_TEXCOORD1.x;
    f_u_xlat1.y = f_vs_TEXCOORD1.y;
    f_u_xlat1.z = f_vs_TEXCOORD1.z;
    f_u_xlat1.w = 1.0f;
    f_u_xlat16_3.x = dot(unity_SHAr, f_u_xlat1);
    f_u_xlat16_3.y = dot(unity_SHAg, f_u_xlat1);
    f_u_xlat16_3.z = dot(unity_SHAb, f_u_xlat1);
    f_u_xlat16_3 += f_vs_TEXCOORD3;
    f_u_xlat16_3 = max(f_u_xlat16_3, 0.0f.xxx);
    f_u_xlat16_3 = f_u_xlat16_0.xyz * f_u_xlat16_3;
    f_u_xlat16_14 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_14 = max(f_u_xlat16_14, 0.0f);
    float3 _257 = (f_u_xlat16_2 * f_u_xlat16_14.xxx) + f_u_xlat16_3;
    f_SV_Target0.x = _257.x;
    f_SV_Target0.y = _257.y;
    f_SV_Target0.z = _257.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat8 = 1.0f / _BlendWidthX;
    float3 _282 = f_u_xlat0;
    float2 _284 = f_u_xlat8.xx * _282.xy;
    f_u_xlat0.x = _284.x;
    f_u_xlat0.y = _284.y;
    float3 _289 = f_u_xlat0;
    float2 _293 = clamp(_289.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _293.x;
    f_u_xlat0.y = _293.y;
    f_u_xlat8 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat4.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat8 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat8;
    f_u_xlat0.x = f_u_xlat4.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 _WorldSpaceLightPos0;
float _BlendWidthX;
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
static float v_u_xlat16_2;
static float4 v_u_xlat16_0;
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
    v_u_xlat16_2 = v_u_xlat0.y * v_u_xlat0.y;
    v_u_xlat16_2 = (v_u_xlat0.x * v_u_xlat0.x) + (-v_u_xlat16_2);
    v_u_xlat16_0 = v_u_xlat0.yzzx * v_u_xlat0.xyzz;
    v_u_xlat16_3.x = dot(unity_SHBr, v_u_xlat16_0);
    v_u_xlat16_3.y = dot(unity_SHBg, v_u_xlat16_0);
    v_u_xlat16_3.z = dot(unity_SHBb, v_u_xlat16_0);
    v_vs_TEXCOORD3 = (unity_SHC.xyz * v_u_xlat16_2.xxx) + v_u_xlat16_3;
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
static float3 f_vs_TEXCOORD3;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

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
static float4 f_u_xlat1;
static float f_u_xlat8;
static float3 f_u_xlat16_2;
static float4 f_u_xlat16_0;
static float3 f_u_xlat16_3;
static float f_u_xlat16_14;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
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
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _188 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _188.x;
    f_u_xlat0.y = _188.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_2 *= f_u_xlat16_0.xyz;
    f_u_xlat1.x = f_vs_TEXCOORD1.x;
    f_u_xlat1.y = f_vs_TEXCOORD1.y;
    f_u_xlat1.z = f_vs_TEXCOORD1.z;
    f_u_xlat1.w = 1.0f;
    f_u_xlat16_3.x = dot(unity_SHAr, f_u_xlat1);
    f_u_xlat16_3.y = dot(unity_SHAg, f_u_xlat1);
    f_u_xlat16_3.z = dot(unity_SHAb, f_u_xlat1);
    f_u_xlat16_3 += f_vs_TEXCOORD3;
    f_u_xlat16_3 = max(f_u_xlat16_3, 0.0f.xxx);
    f_u_xlat16_3 = f_u_xlat16_0.xyz * f_u_xlat16_3;
    f_u_xlat16_14 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_14 = max(f_u_xlat16_14, 0.0f);
    float3 _257 = (f_u_xlat16_2 * f_u_xlat16_14.xxx) + f_u_xlat16_3;
    f_SV_Target0.x = _257.x;
    f_SV_Target0.y = _257.y;
    f_SV_Target0.z = _257.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat8 = 1.0f / _BlendWidthX;
    float3 _282 = f_u_xlat0;
    float2 _284 = f_u_xlat8.xx * _282.xy;
    f_u_xlat0.x = _284.x;
    f_u_xlat0.y = _284.y;
    float3 _289 = f_u_xlat0;
    float2 _293 = clamp(_289.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _293.x;
    f_u_xlat0.y = _293.y;
    f_u_xlat8 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat4.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat8 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat8;
    f_u_xlat0.x = f_u_xlat4.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
#elif defined(DIRECTIONAL) && defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4 _MainTex_ST;
float4 unity_4LightPosX0;
float4 unity_4LightPosY0;
float4 unity_4LightPosZ0;
float4 unity_4LightAtten0;
float4 unity_LightColor[8];
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float _BlendWidthX;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _LightColor0;
float4 _WorldSpaceLightPos0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
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
static float v_u_xlat21;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float3 v_u_xlat16_5;
static float4 v_u_xlat16_2;
static float3 v_u_xlat16_6;
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
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat21 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat21 = rsqrt(v_u_xlat21);
    float4 _144 = v_u_xlat1;
    float3 _146 = v_u_xlat21.xxx * _144.xyz;
    v_u_xlat1.x = _146.x;
    v_u_xlat1.y = _146.y;
    v_u_xlat1.z = _146.z;
    v_vs_TEXCOORD1 = v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = v_u_xlat0.xyz;
    v_u_xlat2 = (-v_u_xlat0.xxxx) + unity_4LightPosX0;
    v_u_xlat3 = (-v_u_xlat0.yyyy) + unity_4LightPosY0;
    v_u_xlat0 = (-v_u_xlat0.zzzz) + unity_4LightPosZ0;
    v_u_xlat4 = v_u_xlat1.yyyy * v_u_xlat3;
    v_u_xlat3 *= v_u_xlat3;
    v_u_xlat3 = (v_u_xlat2 * v_u_xlat2) + v_u_xlat3;
    v_u_xlat2 = (v_u_xlat2 * v_u_xlat1.xxxx) + v_u_xlat4;
    v_u_xlat2 = (v_u_xlat0 * v_u_xlat1.zzzz) + v_u_xlat2;
    v_u_xlat0 = (v_u_xlat0 * v_u_xlat0) + v_u_xlat3;
    v_u_xlat0 = max(v_u_xlat0, 9.9999999747524270787835121154785e-07f.xxxx);
    v_u_xlat3 = rsqrt(v_u_xlat0);
    v_u_xlat0 = (v_u_xlat0 * unity_4LightAtten0) + 1.0f.xxxx;
    v_u_xlat0 = 1.0f.xxxx / v_u_xlat0;
    v_u_xlat2 *= v_u_xlat3;
    v_u_xlat2 = max(v_u_xlat2, 0.0f.xxxx);
    v_u_xlat0 *= v_u_xlat2;
    float3 _243 = v_u_xlat0.yyy * unity_LightColor[1].xyz;
    v_u_xlat2.x = _243.x;
    v_u_xlat2.y = _243.y;
    v_u_xlat2.z = _243.z;
    float4 _256 = v_u_xlat2;
    float3 _258 = (unity_LightColor[0].xyz * v_u_xlat0.xxx) + _256.xyz;
    v_u_xlat2.x = _258.x;
    v_u_xlat2.y = _258.y;
    v_u_xlat2.z = _258.z;
    float4 _268 = v_u_xlat0;
    float3 _273 = (unity_LightColor[2].xyz * _268.zzz) + v_u_xlat2.xyz;
    v_u_xlat0.x = _273.x;
    v_u_xlat0.y = _273.y;
    v_u_xlat0.z = _273.z;
    float4 _283 = v_u_xlat0;
    float4 _286 = v_u_xlat0;
    float3 _288 = (unity_LightColor[3].xyz * _283.www) + _286.xyz;
    v_u_xlat0.x = _288.x;
    v_u_xlat0.y = _288.y;
    v_u_xlat0.z = _288.z;
    v_u_xlat16_5.x = v_u_xlat1.y * v_u_xlat1.y;
    v_u_xlat16_5.x = (v_u_xlat1.x * v_u_xlat1.x) + (-v_u_xlat16_5.x);
    v_u_xlat16_2 = v_u_xlat1.yzzx * v_u_xlat1.xyzz;
    v_u_xlat16_6.x = dot(unity_SHBr, v_u_xlat16_2);
    v_u_xlat16_6.y = dot(unity_SHBg, v_u_xlat16_2);
    v_u_xlat16_6.z = dot(unity_SHBb, v_u_xlat16_2);
    v_u_xlat16_5 = (unity_SHC.xyz * v_u_xlat16_5.xxx) + v_u_xlat16_6;
    v_u_xlat1.w = 1.0f;
    v_u_xlat16_6.x = dot(unity_SHAr, v_u_xlat1);
    v_u_xlat16_6.y = dot(unity_SHAg, v_u_xlat1);
    v_u_xlat16_6.z = dot(unity_SHAb, v_u_xlat1);
    v_u_xlat16_5 += v_u_xlat16_6;
    v_u_xlat16_5 = max(v_u_xlat16_5, 0.0f.xxx);
    v_vs_TEXCOORD3 = v_u_xlat0.xyz + v_u_xlat16_5;
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
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float2 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD3;
static float3 f_vs_TEXCOORD1;

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

static float2 f_u_xlat0;
static float f_u_xlat8;
static float2 f_u_xlat4;
static float4 f_u_xlat16_1;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_3;
static float f_u_xlat16_14;

void f_frag_main()
{
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat8 = 1.0f / _BlendWidthX;
    f_u_xlat0 = f_u_xlat8.xx * f_u_xlat0;
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f.xx, 1.0f.xx);
    f_u_xlat8 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat4.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat8 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat8;
    f_u_xlat0.x = f_u_xlat4.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat4 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_u_xlat4);
    f_u_xlat0.x *= f_u_xlat16_1.w;
    f_SV_Target0.w = f_u_xlat0.x;
    f_u_xlat16_2 = f_u_xlat16_1.xyz * _LightColor0.xyz;
    f_u_xlat16_3 = f_u_xlat16_1.xyz * f_vs_TEXCOORD3;
    f_u_xlat16_14 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_14 = max(f_u_xlat16_14, 0.0f);
    float3 _145 = (f_u_xlat16_2 * f_u_xlat16_14.xxx) + f_u_xlat16_3;
    f_SV_Target0.x = _145.x;
    f_SV_Target0.y = _145.y;
    f_SV_Target0.z = _145.z;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
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
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _WorldSpaceLightPos0;
float _BlendWidthX;
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
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD1 = v_u_xlat6.xxx * v_u_xlat0.xyz;
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
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat3;
static float3 f_u_xlat1;
static float f_u_xlat6;
static float3 f_u_xlat16_2;
static float4 f_u_xlat16_0;
static float f_u_xlat16_11;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat3 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat3.x = dot(f_u_xlat3, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat3.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat3.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat3 = f_vs_TEXCOORD2.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat3;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat3;
    f_u_xlat3 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat3.xy, f_u_xlat3.z);
    f_u_xlat3.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat6 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat3.x = (f_u_xlat3.x * f_u_xlat6) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat3.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat3.x;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _186 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _186.x;
    f_u_xlat0.y = _186.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_2 *= f_u_xlat16_0.xyz;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _219 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _219.x;
    f_SV_Target0.y = _219.y;
    f_SV_Target0.z = _219.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat6 = 1.0f / _BlendWidthX;
    float3 _244 = f_u_xlat0;
    float2 _246 = f_u_xlat6.xx * _244.xy;
    f_u_xlat0.x = _246.x;
    f_u_xlat0.y = _246.y;
    float3 _251 = f_u_xlat0;
    float2 _255 = clamp(_251.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _255.x;
    f_u_xlat0.y = _255.y;
    f_u_xlat6 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat3.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat3.x *= f_u_xlat6;
    f_u_xlat6 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat6;
    f_u_xlat0.x = f_u_xlat3.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
float _BlendWidthX;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _LightColor0;
float4 _WorldSpaceLightPos0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
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
    v_u_xlat16_2 += v_u_xlat16_3;
    v_vs_TEXCOORD3 = max(v_u_xlat16_2, 0.0f.xxx);
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
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float2 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD3;
static float3 f_vs_TEXCOORD1;

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

static float2 f_u_xlat0;
static float f_u_xlat8;
static float2 f_u_xlat4;
static float4 f_u_xlat16_1;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_3;
static float f_u_xlat16_14;

void f_frag_main()
{
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat8 = 1.0f / _BlendWidthX;
    f_u_xlat0 = f_u_xlat8.xx * f_u_xlat0;
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f.xx, 1.0f.xx);
    f_u_xlat8 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat4.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat8 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat8;
    f_u_xlat0.x = f_u_xlat4.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat4 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_u_xlat4);
    f_u_xlat0.x *= f_u_xlat16_1.w;
    f_SV_Target0.w = f_u_xlat0.x;
    f_u_xlat16_2 = f_u_xlat16_1.xyz * _LightColor0.xyz;
    f_u_xlat16_3 = f_u_xlat16_1.xyz * f_vs_TEXCOORD3;
    f_u_xlat16_14 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_14 = max(f_u_xlat16_14, 0.0f);
    float3 _145 = (f_u_xlat16_2 * f_u_xlat16_14.xxx) + f_u_xlat16_3;
    f_SV_Target0.x = _145.x;
    f_SV_Target0.y = _145.y;
    f_SV_Target0.z = _145.z;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
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
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _WorldSpaceLightPos0;
float _BlendWidthX;
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
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD1 = v_u_xlat6.xxx * v_u_xlat0.xyz;
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
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat3;
static float3 f_u_xlat1;
static float f_u_xlat6;
static float3 f_u_xlat16_2;
static float4 f_u_xlat16_0;
static float f_u_xlat16_11;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat3 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat3.x = dot(f_u_xlat3, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat3.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat3.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat3 = f_vs_TEXCOORD2.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat3;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat3;
    f_u_xlat3 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat3.xy, f_u_xlat3.z);
    f_u_xlat3.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat6 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat3.x = (f_u_xlat3.x * f_u_xlat6) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat3.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat3.x;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _186 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _186.x;
    f_u_xlat0.y = _186.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_2 *= f_u_xlat16_0.xyz;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _219 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _219.x;
    f_SV_Target0.y = _219.y;
    f_SV_Target0.z = _219.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat6 = 1.0f / _BlendWidthX;
    float3 _244 = f_u_xlat0;
    float2 _246 = f_u_xlat6.xx * _244.xy;
    f_u_xlat0.x = _246.x;
    f_u_xlat0.y = _246.y;
    float3 _251 = f_u_xlat0;
    float2 _255 = clamp(_251.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _255.x;
    f_u_xlat0.y = _255.y;
    f_u_xlat6 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat3.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat3.x *= f_u_xlat6;
    f_u_xlat6 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat6;
    f_u_xlat0.x = f_u_xlat3.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
#elif defined(DIRECTIONAL) && !defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4 _MainTex_ST;
float _BlendWidthX;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _LightColor0;
float4 _WorldSpaceLightPos0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;

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
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float2 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD1;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat0;
static float f_u_xlat6;
static float2 f_u_xlat3;
static float4 f_u_xlat16_1;
static float3 f_u_xlat16_2;
static float f_u_xlat16_11;

void f_frag_main()
{
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat6 = 1.0f / _BlendWidthX;
    f_u_xlat0 = f_u_xlat6.xx * f_u_xlat0;
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f.xx, 1.0f.xx);
    f_u_xlat6 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat3.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat3.x *= f_u_xlat6;
    f_u_xlat6 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat6;
    f_u_xlat0.x = f_u_xlat3.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat3 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_u_xlat3);
    f_u_xlat0.x *= f_u_xlat16_1.w;
    f_u_xlat16_2 = f_u_xlat16_1.xyz * _LightColor0.xyz;
    f_SV_Target0.w = f_u_xlat0.x;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _137 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _137.x;
    f_SV_Target0.y = _137.y;
    f_SV_Target0.z = _137.z;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
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
float _BlendWidthX;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _LightColor0;
float4 _WorldSpaceLightPos0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;

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
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float2 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD1;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat0;
static float f_u_xlat6;
static float2 f_u_xlat3;
static float4 f_u_xlat16_1;
static float3 f_u_xlat16_2;
static float f_u_xlat16_11;

void f_frag_main()
{
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat6 = 1.0f / _BlendWidthX;
    f_u_xlat0 = f_u_xlat6.xx * f_u_xlat0;
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f.xx, 1.0f.xx);
    f_u_xlat6 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat3.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat3.x *= f_u_xlat6;
    f_u_xlat6 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat6;
    f_u_xlat0.x = f_u_xlat3.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat3 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_u_xlat3);
    f_u_xlat0.x *= f_u_xlat16_1.w;
    f_u_xlat16_2 = f_u_xlat16_1.xyz * _LightColor0.xyz;
    f_SV_Target0.w = f_u_xlat0.x;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _137 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _137.x;
    f_SV_Target0.y = _137.y;
    f_SV_Target0.z = _137.z;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
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
Tags { "LightMode"="ForwardAdd" "Queue"="Geometry+1" "RenderType"="Opaque" "SHADOWSUPPORT"="true" }
Cull Off
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
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _WorldSpaceLightPos0;
float _BlendWidthX;
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
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat10;
static float4 v_vs_TEXCOORD3;
static float4 v_vs_TEXCOORD4;

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
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_WorldToLight)[1];
    v_u_xlat1 = (transpose(unity_WorldToLight)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_WorldToLight)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_vs_TEXCOORD3 = (transpose(unity_WorldToLight)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
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
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat0;
static float3 f_u_xlat4;
static float4 f_u_xlat1;
static float3 f_u_xlat2;
static float f_u_xlat8;
static float3 f_u_xlat16_3;
static float f_u_xlat12;
static bool f_u_xlatb1;
static float f_u_xlat16_7;
static float4 f_u_xlat16_0;
static float f_u_xlat16_15;

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
    float3 _144 = f_u_xlat4 + _ShadowOffsets[0].xyz;
    f_u_xlat1.x = _144.x;
    f_u_xlat1.y = _144.y;
    f_u_xlat1.z = _144.z;
    float3 txVec0 = float3(f_u_xlat1.xy, f_u_xlat1.z);
    f_u_xlat1.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat2 = f_u_xlat4 + _ShadowOffsets[1].xyz;
    float3 txVec1 = float3(f_u_xlat2.xy, f_u_xlat2.z);
    f_u_xlat1.y = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec1.xy, txVec1.z);
    f_u_xlat2 = f_u_xlat4 + _ShadowOffsets[2].xyz;
    f_u_xlat4 += _ShadowOffsets[3].xyz;
    float3 txVec2 = float3(f_u_xlat4.xy, f_u_xlat4.z);
    f_u_xlat1.w = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec2.xy, txVec2.z);
    float3 txVec3 = float3(f_u_xlat2.xy, f_u_xlat2.z);
    f_u_xlat1.z = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec3.xy, txVec3.z);
    f_u_xlat4.x = dot(f_u_xlat1, 0.25f.xxxx);
    f_u_xlat8 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + _LightShadowData.x;
    f_u_xlat16_3.x = (-f_u_xlat4.x) + 1.0f;
    f_u_xlat16_3.x = (f_u_xlat0.x * f_u_xlat16_3.x) + f_u_xlat4.x;
    f_u_xlat0 = f_vs_TEXCOORD2.yyyy * transpose(unity_WorldToLight)[1];
    f_u_xlat0 = (transpose(unity_WorldToLight)[0] * f_vs_TEXCOORD2.xxxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2] * f_vs_TEXCOORD2.zzzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3];
    float2 _286 = f_u_xlat0.xy / f_u_xlat0.ww;
    f_u_xlat1.x = _286.x;
    f_u_xlat1.y = _286.y;
    float4 _291 = f_u_xlat1;
    float2 _295 = _291.xy + 0.5f.xx;
    f_u_xlat1.x = _295.x;
    f_u_xlat1.y = _295.y;
    f_u_xlat12 = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat1.xy).w;
    f_u_xlatb1 = 0.0f < f_u_xlat0.z;
    f_u_xlat0.x = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat0.x = _LightTextureB0.Sample(sampler_LightTextureB0, f_u_xlat0.xx).x;
    f_u_xlat16_7 = float(f_u_xlatb1);
    f_u_xlat16_7 = f_u_xlat12 * f_u_xlat16_7;
    f_u_xlat16_7 = f_u_xlat0.x * f_u_xlat16_7;
    f_u_xlat16_3.x *= f_u_xlat16_7;
    f_u_xlat16_3 = f_u_xlat16_3.xxx * _LightColor0.xyz;
    float2 _355 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _355.x;
    f_u_xlat0.y = _355.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_3 *= f_u_xlat16_0.xyz;
    float3 _375 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat0.x = _375.x;
    f_u_xlat0.y = _375.y;
    f_u_xlat0.z = _375.z;
    f_u_xlat1.x = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat1.x = rsqrt(f_u_xlat1.x);
    float4 _392 = f_u_xlat0;
    float3 _396 = _392.xyz * f_u_xlat1.xxx;
    f_u_xlat0.x = _396.x;
    f_u_xlat0.y = _396.y;
    f_u_xlat0.z = _396.z;
    f_u_xlat16_15 = dot(f_vs_TEXCOORD1, f_u_xlat0.xyz);
    f_u_xlat16_15 = max(f_u_xlat16_15, 0.0f);
    float3 _416 = f_u_xlat16_15.xxx * f_u_xlat16_3;
    f_SV_Target0.x = _416.x;
    f_SV_Target0.y = _416.y;
    f_SV_Target0.z = _416.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat8 = 1.0f / _BlendWidthX;
    float4 _441 = f_u_xlat0;
    float2 _443 = f_u_xlat8.xx * _441.xy;
    f_u_xlat0.x = _443.x;
    f_u_xlat0.y = _443.y;
    float4 _448 = f_u_xlat0;
    float2 _452 = clamp(_448.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _452.x;
    f_u_xlat0.y = _452.y;
    f_u_xlat8 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat4.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat8 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat8;
    f_u_xlat0.x = f_u_xlat4.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _WorldSpaceLightPos0;
float _BlendWidthX;
TextureCube<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;

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
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat10;
static float3 v_vs_TEXCOORD3;
static float4 v_vs_TEXCOORD4;

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
    float3 _157 = v_u_xlat0.yyy * transpose(unity_WorldToLight)[1].xyz;
    v_u_xlat1.x = _157.x;
    v_u_xlat1.y = _157.y;
    v_u_xlat1.z = _157.z;
    float4 _170 = v_u_xlat1;
    float3 _172 = (transpose(unity_WorldToLight)[0].xyz * v_u_xlat0.xxx) + _170.xyz;
    v_u_xlat1.x = _172.x;
    v_u_xlat1.y = _172.y;
    v_u_xlat1.z = _172.z;
    float4 _182 = v_u_xlat0;
    float3 _187 = (transpose(unity_WorldToLight)[2].xyz * _182.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _187.x;
    v_u_xlat0.y = _187.y;
    v_u_xlat0.z = _187.z;
    v_vs_TEXCOORD3 = (transpose(unity_WorldToLight)[3].xyz * v_u_xlat0.www) + v_u_xlat0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
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
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float3 f_u_xlat1;
static float3 f_u_xlat5;
static float4 f_u_xlat2;
static float f_u_xlat8;
static float3 f_u_xlat16_3;
static float4 f_u_xlat16_0;
static float f_u_xlat16_15;

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
    f_u_xlat4 = f_vs_TEXCOORD2 + (-_LightPositionRange.xyz);
    f_u_xlat1.x = max(abs(f_u_xlat4.y), abs(f_u_xlat4.x));
    f_u_xlat1.x = max(abs(f_u_xlat4.z), f_u_xlat1.x);
    f_u_xlat1.x += (-_LightProjectionParams.z);
    f_u_xlat1.x = max(f_u_xlat1.x, 9.9999997473787516355514526367188e-06f);
    f_u_xlat1.x *= _LightProjectionParams.w;
    f_u_xlat1.x = _LightProjectionParams.y / f_u_xlat1.x;
    f_u_xlat1.x += (-_LightProjectionParams.x);
    f_u_xlat5 = f_u_xlat4 + 0.0078125f.xxx;
    float4 txVec0 = float4(f_u_xlat5, f_u_xlat1.x);
    f_u_xlat2.x = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xyz, txVec0.w);
    f_u_xlat5 = f_u_xlat4 + float3(-0.0078125f, -0.0078125f, 0.0078125f);
    float4 txVec1 = float4(f_u_xlat5, f_u_xlat1.x);
    f_u_xlat2.y = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec1.xyz, txVec1.w);
    f_u_xlat5 = f_u_xlat4 + float3(-0.0078125f, 0.0078125f, -0.0078125f);
    f_u_xlat4 += float3(0.0078125f, -0.0078125f, -0.0078125f);
    float4 txVec2 = float4(f_u_xlat4, f_u_xlat1.x);
    f_u_xlat2.w = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec2.xyz, txVec2.w);
    float4 txVec3 = float4(f_u_xlat5, f_u_xlat1.x);
    f_u_xlat2.z = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec3.xyz, txVec3.w);
    f_u_xlat4.x = dot(f_u_xlat2, 0.25f.xxxx);
    f_u_xlat8 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + _LightShadowData.x;
    f_u_xlat16_3.x = (-f_u_xlat4.x) + 1.0f;
    f_u_xlat16_3.x = (f_u_xlat0.x * f_u_xlat16_3.x) + f_u_xlat4.x;
    f_u_xlat0 = f_vs_TEXCOORD2.yyy * transpose(unity_WorldToLight)[1].xyz;
    f_u_xlat0 = (transpose(unity_WorldToLight)[0].xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2].xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3].xyz;
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0.xx).x;
    f_u_xlat0.x = f_u_xlat16_3.x * f_u_xlat0.x;
    f_u_xlat16_3 = f_u_xlat0.xxx * _LightColor0.xyz;
    float2 _317 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _317.x;
    f_u_xlat0.y = _317.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_3 *= f_u_xlat16_0.xyz;
    f_u_xlat0 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat1.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat1.x = rsqrt(f_u_xlat1.x);
    f_u_xlat0 *= f_u_xlat1.xxx;
    f_u_xlat16_15 = dot(f_vs_TEXCOORD1, f_u_xlat0);
    f_u_xlat16_15 = max(f_u_xlat16_15, 0.0f);
    float3 _362 = f_u_xlat16_15.xxx * f_u_xlat16_3;
    f_SV_Target0.x = _362.x;
    f_SV_Target0.y = _362.y;
    f_SV_Target0.z = _362.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat8 = 1.0f / _BlendWidthX;
    float3 _387 = f_u_xlat0;
    float2 _389 = f_u_xlat8.xx * _387.xy;
    f_u_xlat0.x = _389.x;
    f_u_xlat0.y = _389.y;
    float3 _394 = f_u_xlat0;
    float2 _398 = clamp(_394.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _398.x;
    f_u_xlat0.y = _398.y;
    f_u_xlat8 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat4.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat8 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat8;
    f_u_xlat0.x = f_u_xlat4.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _WorldSpaceLightPos0;
float _BlendWidthX;
TextureCube<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;

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
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat10;
static float3 v_vs_TEXCOORD3;
static float4 v_vs_TEXCOORD4;

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
    float3 _157 = v_u_xlat0.yyy * transpose(unity_WorldToLight)[1].xyz;
    v_u_xlat1.x = _157.x;
    v_u_xlat1.y = _157.y;
    v_u_xlat1.z = _157.z;
    float4 _170 = v_u_xlat1;
    float3 _172 = (transpose(unity_WorldToLight)[0].xyz * v_u_xlat0.xxx) + _170.xyz;
    v_u_xlat1.x = _172.x;
    v_u_xlat1.y = _172.y;
    v_u_xlat1.z = _172.z;
    float4 _182 = v_u_xlat0;
    float3 _187 = (transpose(unity_WorldToLight)[2].xyz * _182.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _187.x;
    v_u_xlat0.y = _187.y;
    v_u_xlat0.z = _187.z;
    v_vs_TEXCOORD3 = (transpose(unity_WorldToLight)[3].xyz * v_u_xlat0.www) + v_u_xlat0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
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
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat4;
static float3 f_u_xlat1;
static float3 f_u_xlat5;
static float4 f_u_xlat2;
static float f_u_xlat8;
static float3 f_u_xlat16_3;
static float f_u_xlat12;
static float4 f_u_xlat16_0;
static float f_u_xlat16_15;

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
    f_u_xlat4 = f_vs_TEXCOORD2 + (-_LightPositionRange.xyz);
    f_u_xlat1.x = max(abs(f_u_xlat4.y), abs(f_u_xlat4.x));
    f_u_xlat1.x = max(abs(f_u_xlat4.z), f_u_xlat1.x);
    f_u_xlat1.x += (-_LightProjectionParams.z);
    f_u_xlat1.x = max(f_u_xlat1.x, 9.9999997473787516355514526367188e-06f);
    f_u_xlat1.x *= _LightProjectionParams.w;
    f_u_xlat1.x = _LightProjectionParams.y / f_u_xlat1.x;
    f_u_xlat1.x += (-_LightProjectionParams.x);
    f_u_xlat5 = f_u_xlat4 + 0.0078125f.xxx;
    float4 txVec0 = float4(f_u_xlat5, f_u_xlat1.x);
    f_u_xlat2.x = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xyz, txVec0.w);
    f_u_xlat5 = f_u_xlat4 + float3(-0.0078125f, -0.0078125f, 0.0078125f);
    float4 txVec1 = float4(f_u_xlat5, f_u_xlat1.x);
    f_u_xlat2.y = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec1.xyz, txVec1.w);
    f_u_xlat5 = f_u_xlat4 + float3(-0.0078125f, 0.0078125f, -0.0078125f);
    f_u_xlat4 += float3(0.0078125f, -0.0078125f, -0.0078125f);
    float4 txVec2 = float4(f_u_xlat4, f_u_xlat1.x);
    f_u_xlat2.w = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec2.xyz, txVec2.w);
    float4 txVec3 = float4(f_u_xlat5, f_u_xlat1.x);
    f_u_xlat2.z = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec3.xyz, txVec3.w);
    f_u_xlat4.x = dot(f_u_xlat2, 0.25f.xxxx);
    f_u_xlat8 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + _LightShadowData.x;
    f_u_xlat16_3.x = (-f_u_xlat4.x) + 1.0f;
    f_u_xlat16_3.x = (f_u_xlat0.x * f_u_xlat16_3.x) + f_u_xlat4.x;
    f_u_xlat0 = f_vs_TEXCOORD2.yyy * transpose(unity_WorldToLight)[1].xyz;
    f_u_xlat0 = (transpose(unity_WorldToLight)[0].xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2].xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3].xyz;
    f_u_xlat12 = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0).w;
    f_u_xlat4.x = _LightTextureB0.Sample(sampler_LightTextureB0, f_u_xlat12.xx).x;
    f_u_xlat0.x *= f_u_xlat4.x;
    f_u_xlat0.x = f_u_xlat16_3.x * f_u_xlat0.x;
    f_u_xlat16_3 = f_u_xlat0.xxx * _LightColor0.xyz;
    float2 _332 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _332.x;
    f_u_xlat0.y = _332.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_3 *= f_u_xlat16_0.xyz;
    f_u_xlat0 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat1.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat1.x = rsqrt(f_u_xlat1.x);
    f_u_xlat0 *= f_u_xlat1.xxx;
    f_u_xlat16_15 = dot(f_vs_TEXCOORD1, f_u_xlat0);
    f_u_xlat16_15 = max(f_u_xlat16_15, 0.0f);
    float3 _377 = f_u_xlat16_15.xxx * f_u_xlat16_3;
    f_SV_Target0.x = _377.x;
    f_SV_Target0.y = _377.y;
    f_SV_Target0.z = _377.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat8 = 1.0f / _BlendWidthX;
    float3 _402 = f_u_xlat0;
    float2 _404 = f_u_xlat8.xx * _402.xy;
    f_u_xlat0.x = _404.x;
    f_u_xlat0.y = _404.y;
    float3 _409 = f_u_xlat0;
    float2 _413 = clamp(_409.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _413.x;
    f_u_xlat0.y = _413.y;
    f_u_xlat8 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat4.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat4.x *= f_u_xlat8;
    f_u_xlat8 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat8;
    f_u_xlat0.x = f_u_xlat4.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _WorldSpaceLightPos0;
float _BlendWidthX;
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
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat10;
static float4 v_vs_TEXCOORD3;
static float4 v_vs_TEXCOORD4;

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
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_WorldToLight)[1];
    v_u_xlat1 = (transpose(unity_WorldToLight)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_WorldToLight)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_vs_TEXCOORD3 = (transpose(unity_WorldToLight)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
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
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat0;
static float3 f_u_xlat3;
static float4 f_u_xlat1;
static float f_u_xlat6;
static float3 f_u_xlat16_2;
static float f_u_xlat9;
static bool f_u_xlatb1;
static float f_u_xlat16_5;
static float4 f_u_xlat16_0;
static float f_u_xlat16_11;

void f_frag_main()
{
    float3 _19 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = _19.x;
    f_u_xlat0.y = _19.y;
    f_u_xlat0.z = _19.z;
    f_u_xlat0.x = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat3 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat3.x = dot(f_u_xlat3, f_u_xlat1.xyz);
    f_u_xlat0.x = (-f_u_xlat3.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat3.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat1 = f_vs_TEXCOORD2.yyyy * RecoveryColumn_unity_WorldToShadow(1);
    f_u_xlat1 = (RecoveryColumn_unity_WorldToShadow(0) * f_vs_TEXCOORD2.xxxx) + f_u_xlat1;
    f_u_xlat1 = (RecoveryColumn_unity_WorldToShadow(2) * f_vs_TEXCOORD2.zzzz) + f_u_xlat1;
    f_u_xlat1 += RecoveryColumn_unity_WorldToShadow(3);
    f_u_xlat3 = f_u_xlat1.xyz / f_u_xlat1.www;
    float3 txVec0 = float3(f_u_xlat3.xy, f_u_xlat3.z);
    f_u_xlat3.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat6 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat3.x = (f_u_xlat3.x * f_u_xlat6) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat3.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat3.x;
    f_u_xlat0 = f_vs_TEXCOORD2.yyyy * transpose(unity_WorldToLight)[1];
    f_u_xlat0 = (transpose(unity_WorldToLight)[0] * f_vs_TEXCOORD2.xxxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2] * f_vs_TEXCOORD2.zzzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3];
    float2 _214 = f_u_xlat0.xy / f_u_xlat0.ww;
    f_u_xlat1.x = _214.x;
    f_u_xlat1.y = _214.y;
    float4 _219 = f_u_xlat1;
    float2 _223 = _219.xy + 0.5f.xx;
    f_u_xlat1.x = _223.x;
    f_u_xlat1.y = _223.y;
    f_u_xlat9 = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat1.xy).w;
    f_u_xlatb1 = 0.0f < f_u_xlat0.z;
    f_u_xlat0.x = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat0.x = _LightTextureB0.Sample(sampler_LightTextureB0, f_u_xlat0.xx).x;
    f_u_xlat16_5 = float(f_u_xlatb1);
    f_u_xlat16_5 = f_u_xlat9 * f_u_xlat16_5;
    f_u_xlat16_5 = f_u_xlat0.x * f_u_xlat16_5;
    f_u_xlat16_2.x *= f_u_xlat16_5;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _283 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _283.x;
    f_u_xlat0.y = _283.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_2 *= f_u_xlat16_0.xyz;
    float3 _303 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat0.x = _303.x;
    f_u_xlat0.y = _303.y;
    f_u_xlat0.z = _303.z;
    f_u_xlat1.x = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat1.x = rsqrt(f_u_xlat1.x);
    float4 _320 = f_u_xlat0;
    float3 _324 = _320.xyz * f_u_xlat1.xxx;
    f_u_xlat0.x = _324.x;
    f_u_xlat0.y = _324.y;
    f_u_xlat0.z = _324.z;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD1, f_u_xlat0.xyz);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _344 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _344.x;
    f_SV_Target0.y = _344.y;
    f_SV_Target0.z = _344.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat6 = 1.0f / _BlendWidthX;
    float4 _369 = f_u_xlat0;
    float2 _371 = f_u_xlat6.xx * _369.xy;
    f_u_xlat0.x = _371.x;
    f_u_xlat0.y = _371.y;
    float4 _376 = f_u_xlat0;
    float2 _380 = clamp(_376.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _380.x;
    f_u_xlat0.y = _380.y;
    f_u_xlat6 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat3.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat3.x *= f_u_xlat6;
    f_u_xlat6 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat6;
    f_u_xlat0.x = f_u_xlat3.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
#elif defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && !defined(POINT) && !defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
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
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _WorldSpaceLightPos0;
float _BlendWidthX;
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
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat6;
static float4 v_vs_TEXCOORD4;

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
    v_vs_TEXCOORD4 = 0.0f.xxxx;
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
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat3;
static float3 f_u_xlat1;
static float f_u_xlat6;
static float3 f_u_xlat16_2;
static float4 f_u_xlat16_0;
static float f_u_xlat16_11;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat3 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat3.x = dot(f_u_xlat3, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat3.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat3.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat3 = f_vs_TEXCOORD2.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat3;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat3;
    f_u_xlat3 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat3.xy, f_u_xlat3.z);
    f_u_xlat3.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat6 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat3.x = (f_u_xlat3.x * f_u_xlat6) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat3.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat3.x;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _186 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _186.x;
    f_u_xlat0.y = _186.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_2 *= f_u_xlat16_0.xyz;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _219 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _219.x;
    f_SV_Target0.y = _219.y;
    f_SV_Target0.z = _219.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat6 = 1.0f / _BlendWidthX;
    float3 _244 = f_u_xlat0;
    float2 _246 = f_u_xlat6.xx * _244.xy;
    f_u_xlat0.x = _246.x;
    f_u_xlat0.y = _246.y;
    float3 _251 = f_u_xlat0;
    float2 _255 = clamp(_251.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _255.x;
    f_u_xlat0.y = _255.y;
    f_u_xlat6 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat3.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat3.x *= f_u_xlat6;
    f_u_xlat6 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat6;
    f_u_xlat0.x = f_u_xlat3.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
#elif !defined(DIRECTIONAL) && defined(DIRECTIONAL_COOKIE) && !defined(POINT) && !defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToLight;
float4x4 unity_WorldToObject;
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
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _WorldSpaceLightPos0;
float _BlendWidthX;
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
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float2 v_vs_TEXCOORD3;
static float v_u_xlat9;
static float4 v_vs_TEXCOORD4;

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
    v_vs_TEXCOORD3 = (transpose(unity_WorldToLight)[3].xy * v_u_xlat1.ww) + v_u_xlat0.xy;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    v_vs_TEXCOORD1 = v_u_xlat9.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
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
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat3;
static float3 f_u_xlat1;
static float f_u_xlat6;
static float3 f_u_xlat16_2;
static float4 f_u_xlat16_0;
static float f_u_xlat16_11;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat3 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat3.x = dot(f_u_xlat3, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat3.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat3.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat3 = f_vs_TEXCOORD2.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat3;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat3;
    f_u_xlat3 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat3.xy, f_u_xlat3.z);
    f_u_xlat3.x = hlslcc_zcmp_ShadowMapTexture.SampleCmpLevelZero(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xy, txVec0.z);
    f_u_xlat6 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat3.x = (f_u_xlat3.x * f_u_xlat6) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat3.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat3.x;
    float2 _181 = f_vs_TEXCOORD2.yy * transpose(unity_WorldToLight)[1].xy;
    f_u_xlat0.x = _181.x;
    f_u_xlat0.y = _181.y;
    float3 _192 = f_u_xlat0;
    float2 _194 = (transpose(unity_WorldToLight)[0].xy * f_vs_TEXCOORD2.xx) + _192.xy;
    f_u_xlat0.x = _194.x;
    f_u_xlat0.y = _194.y;
    float3 _205 = f_u_xlat0;
    float2 _207 = (transpose(unity_WorldToLight)[2].xy * f_vs_TEXCOORD2.zz) + _205.xy;
    f_u_xlat0.x = _207.x;
    f_u_xlat0.y = _207.y;
    float3 _212 = f_u_xlat0;
    float2 _217 = _212.xy + transpose(unity_WorldToLight)[3].xy;
    f_u_xlat0.x = _217.x;
    f_u_xlat0.y = _217.y;
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0.xy).w;
    f_u_xlat0.x = f_u_xlat16_2.x * f_u_xlat0.x;
    f_u_xlat16_2 = f_u_xlat0.xxx * _LightColor0.xyz;
    float2 _249 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _249.x;
    f_u_xlat0.y = _249.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_2 *= f_u_xlat16_0.xyz;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _279 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _279.x;
    f_SV_Target0.y = _279.y;
    f_SV_Target0.z = _279.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat6 = 1.0f / _BlendWidthX;
    float3 _304 = f_u_xlat0;
    float2 _306 = f_u_xlat6.xx * _304.xy;
    f_u_xlat0.x = _306.x;
    f_u_xlat0.y = _306.y;
    float3 _311 = f_u_xlat0;
    float2 _315 = clamp(_311.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _315.x;
    f_u_xlat0.y = _315.y;
    f_u_xlat6 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat3.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat3.x *= f_u_xlat6;
    f_u_xlat6 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat6;
    f_u_xlat0.x = f_u_xlat3.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
#elif !defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && defined(POINT) && !defined(POINT_COOKIE) && defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
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
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _WorldSpaceLightPos0;
float _BlendWidthX;
TextureCube<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;

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
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat10;
static float3 v_vs_TEXCOORD3;
static float4 v_vs_TEXCOORD4;

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
    float3 _157 = v_u_xlat0.yyy * transpose(unity_WorldToLight)[1].xyz;
    v_u_xlat1.x = _157.x;
    v_u_xlat1.y = _157.y;
    v_u_xlat1.z = _157.z;
    float4 _170 = v_u_xlat1;
    float3 _172 = (transpose(unity_WorldToLight)[0].xyz * v_u_xlat0.xxx) + _170.xyz;
    v_u_xlat1.x = _172.x;
    v_u_xlat1.y = _172.y;
    v_u_xlat1.z = _172.z;
    float4 _182 = v_u_xlat0;
    float3 _187 = (transpose(unity_WorldToLight)[2].xyz * _182.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _187.x;
    v_u_xlat0.y = _187.y;
    v_u_xlat0.z = _187.z;
    v_vs_TEXCOORD3 = (transpose(unity_WorldToLight)[3].xyz * v_u_xlat0.www) + v_u_xlat0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
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
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat3;
static float3 f_u_xlat1;
static float f_u_xlat6;
static float3 f_u_xlat16_2;
static float4 f_u_xlat16_0;
static float f_u_xlat16_11;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat3 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat3.x = dot(f_u_xlat3, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat3.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat3.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat3 = f_vs_TEXCOORD2 + (-_LightPositionRange.xyz);
    f_u_xlat1.x = max(abs(f_u_xlat3.y), abs(f_u_xlat3.x));
    f_u_xlat1.x = max(abs(f_u_xlat3.z), f_u_xlat1.x);
    f_u_xlat1.x += (-_LightProjectionParams.z);
    f_u_xlat1.x = max(f_u_xlat1.x, 9.9999997473787516355514526367188e-06f);
    f_u_xlat1.x *= _LightProjectionParams.w;
    f_u_xlat1.x = _LightProjectionParams.y / f_u_xlat1.x;
    f_u_xlat1.x += (-_LightProjectionParams.x);
    float4 txVec0 = float4(f_u_xlat3, f_u_xlat1.x);
    f_u_xlat3.x = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xyz, txVec0.w);
    f_u_xlat6 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat3.x = (f_u_xlat3.x * f_u_xlat6) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat3.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat3.x;
    f_u_xlat0 = f_vs_TEXCOORD2.yyy * transpose(unity_WorldToLight)[1].xyz;
    f_u_xlat0 = (transpose(unity_WorldToLight)[0].xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2].xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3].xyz;
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0.xx).x;
    f_u_xlat0.x = f_u_xlat16_2.x * f_u_xlat0.x;
    f_u_xlat16_2 = f_u_xlat0.xxx * _LightColor0.xyz;
    float2 _256 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _256.x;
    f_u_xlat0.y = _256.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_2 *= f_u_xlat16_0.xyz;
    f_u_xlat0 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat1.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat1.x = rsqrt(f_u_xlat1.x);
    f_u_xlat0 *= f_u_xlat1.xxx;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD1, f_u_xlat0);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _302 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _302.x;
    f_SV_Target0.y = _302.y;
    f_SV_Target0.z = _302.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat6 = 1.0f / _BlendWidthX;
    float3 _327 = f_u_xlat0;
    float2 _329 = f_u_xlat6.xx * _327.xy;
    f_u_xlat0.x = _329.x;
    f_u_xlat0.y = _329.y;
    float3 _334 = f_u_xlat0;
    float2 _338 = clamp(_334.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _338.x;
    f_u_xlat0.y = _338.y;
    f_u_xlat6 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat3.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat3.x *= f_u_xlat6;
    f_u_xlat6 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat6;
    f_u_xlat0.x = f_u_xlat3.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _WorldSpaceLightPos0;
float _BlendWidthX;
TextureCube<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;

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
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat10;
static float3 v_vs_TEXCOORD3;
static float4 v_vs_TEXCOORD4;

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
    float3 _157 = v_u_xlat0.yyy * transpose(unity_WorldToLight)[1].xyz;
    v_u_xlat1.x = _157.x;
    v_u_xlat1.y = _157.y;
    v_u_xlat1.z = _157.z;
    float4 _170 = v_u_xlat1;
    float3 _172 = (transpose(unity_WorldToLight)[0].xyz * v_u_xlat0.xxx) + _170.xyz;
    v_u_xlat1.x = _172.x;
    v_u_xlat1.y = _172.y;
    v_u_xlat1.z = _172.z;
    float4 _182 = v_u_xlat0;
    float3 _187 = (transpose(unity_WorldToLight)[2].xyz * _182.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _187.x;
    v_u_xlat0.y = _187.y;
    v_u_xlat0.z = _187.z;
    v_vs_TEXCOORD3 = (transpose(unity_WorldToLight)[3].xyz * v_u_xlat0.www) + v_u_xlat0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
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
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float3 f_u_xlat3;
static float3 f_u_xlat1;
static float f_u_xlat6;
static float3 f_u_xlat16_2;
static float f_u_xlat9;
static float4 f_u_xlat16_0;
static float f_u_xlat16_11;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat3 = (-f_vs_TEXCOORD2) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat3.x = dot(f_u_xlat3, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat3.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat3.x;
    f_u_xlat0.x = (f_u_xlat0.x * _LightShadowData.z) + _LightShadowData.w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat3 = f_vs_TEXCOORD2 + (-_LightPositionRange.xyz);
    f_u_xlat1.x = max(abs(f_u_xlat3.y), abs(f_u_xlat3.x));
    f_u_xlat1.x = max(abs(f_u_xlat3.z), f_u_xlat1.x);
    f_u_xlat1.x += (-_LightProjectionParams.z);
    f_u_xlat1.x = max(f_u_xlat1.x, 9.9999997473787516355514526367188e-06f);
    f_u_xlat1.x *= _LightProjectionParams.w;
    f_u_xlat1.x = _LightProjectionParams.y / f_u_xlat1.x;
    f_u_xlat1.x += (-_LightProjectionParams.x);
    float4 txVec0 = float4(f_u_xlat3, f_u_xlat1.x);
    f_u_xlat3.x = hlslcc_zcmp_ShadowMapTexture.SampleCmp(sampler_hlslcc_zcmp_ShadowMapTexture, txVec0.xyz, txVec0.w);
    f_u_xlat6 = (-_LightShadowData.x) + 1.0f;
    f_u_xlat3.x = (f_u_xlat3.x * f_u_xlat6) + _LightShadowData.x;
    f_u_xlat16_2.x = (-f_u_xlat3.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat3.x;
    f_u_xlat0 = f_vs_TEXCOORD2.yyy * transpose(unity_WorldToLight)[1].xyz;
    f_u_xlat0 = (transpose(unity_WorldToLight)[0].xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2].xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3].xyz;
    f_u_xlat9 = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0).w;
    f_u_xlat3.x = _LightTextureB0.Sample(sampler_LightTextureB0, f_u_xlat9.xx).x;
    f_u_xlat0.x *= f_u_xlat3.x;
    f_u_xlat0.x = f_u_xlat16_2.x * f_u_xlat0.x;
    f_u_xlat16_2 = f_u_xlat0.xxx * _LightColor0.xyz;
    float2 _271 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _271.x;
    f_u_xlat0.y = _271.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_2 *= f_u_xlat16_0.xyz;
    f_u_xlat0 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat1.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat1.x = rsqrt(f_u_xlat1.x);
    f_u_xlat0 *= f_u_xlat1.xxx;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD1, f_u_xlat0);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _317 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _317.x;
    f_SV_Target0.y = _317.y;
    f_SV_Target0.z = _317.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat6 = 1.0f / _BlendWidthX;
    float3 _342 = f_u_xlat0;
    float2 _344 = f_u_xlat6.xx * _342.xy;
    f_u_xlat0.x = _344.x;
    f_u_xlat0.y = _344.y;
    float3 _349 = f_u_xlat0;
    float2 _353 = clamp(_349.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _353.x;
    f_u_xlat0.y = _353.y;
    f_u_xlat6 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat3.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat3.x *= f_u_xlat6;
    f_u_xlat6 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat6;
    f_u_xlat0.x = f_u_xlat3.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
#elif !defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && defined(POINT) && !defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToLight;
float4 _MainTex_ST;
float4 _WorldSpaceLightPos0;
Texture2D<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _BlendWidthX;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;

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
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat10;
static float3 v_vs_TEXCOORD3;
static float4 v_vs_TEXCOORD4;

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
    float3 _157 = v_u_xlat0.yyy * transpose(unity_WorldToLight)[1].xyz;
    v_u_xlat1.x = _157.x;
    v_u_xlat1.y = _157.y;
    v_u_xlat1.z = _157.z;
    float4 _170 = v_u_xlat1;
    float3 _172 = (transpose(unity_WorldToLight)[0].xyz * v_u_xlat0.xxx) + _170.xyz;
    v_u_xlat1.x = _172.x;
    v_u_xlat1.y = _172.y;
    v_u_xlat1.z = _172.z;
    float4 _182 = v_u_xlat0;
    float3 _187 = (transpose(unity_WorldToLight)[2].xyz * _182.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _187.x;
    v_u_xlat0.y = _187.y;
    v_u_xlat0.z = _187.z;
    v_vs_TEXCOORD3 = (transpose(unity_WorldToLight)[3].xyz * v_u_xlat0.www) + v_u_xlat0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
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
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float f_u_xlat6;
static float f_u_xlat16_1;
static float3 f_u_xlat16_3;
static float4 f_u_xlat16_0;
static float f_u_xlat4;
static float f_u_xlat2;

void f_frag_main()
{
    f_u_xlat0 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat6 = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat6 = rsqrt(f_u_xlat6);
    f_u_xlat0 = f_u_xlat6.xxx * f_u_xlat0;
    f_u_xlat16_1 = dot(f_vs_TEXCOORD1, f_u_xlat0);
    f_u_xlat16_1 = max(f_u_xlat16_1, 0.0f);
    f_u_xlat0 = f_vs_TEXCOORD2.yyy * transpose(unity_WorldToLight)[1].xyz;
    f_u_xlat0 = (transpose(unity_WorldToLight)[0].xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2].xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3].xyz;
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0.xx).x;
    f_u_xlat16_3 = f_u_xlat0.xxx * _LightColor0.xyz;
    float2 _105 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _105.x;
    f_u_xlat0.y = _105.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_3 *= f_u_xlat16_0.xyz;
    float3 _127 = f_u_xlat16_1.xxx * f_u_xlat16_3;
    f_SV_Target0.x = _127.x;
    f_SV_Target0.y = _127.y;
    f_SV_Target0.z = _127.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat4 = 1.0f / _BlendWidthX;
    float3 _155 = f_u_xlat0;
    float2 _157 = f_u_xlat4.xx * _155.xy;
    f_u_xlat0.x = _157.x;
    f_u_xlat0.y = _157.y;
    float3 _162 = f_u_xlat0;
    float2 _166 = clamp(_162.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _166.x;
    f_u_xlat0.y = _166.y;
    f_u_xlat4 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat2 = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat2 *= f_u_xlat4;
    f_u_xlat4 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4;
    f_u_xlat0.x = f_u_xlat2 * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
#elif defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && !defined(POINT) && !defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4 _MainTex_ST;
float _BlendWidthX;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _LightColor0;
float4 _WorldSpaceLightPos0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;

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
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat6;
static float4 v_vs_TEXCOORD4;

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
    v_vs_TEXCOORD4 = 0.0f.xxxx;
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
    return stage_output;
}
static float2 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD1;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat0;
static float f_u_xlat6;
static float2 f_u_xlat3;
static float4 f_u_xlat16_1;
static float3 f_u_xlat16_2;
static float f_u_xlat16_11;

void f_frag_main()
{
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat6 = 1.0f / _BlendWidthX;
    f_u_xlat0 = f_u_xlat6.xx * f_u_xlat0;
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f.xx, 1.0f.xx);
    f_u_xlat6 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat3.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat3.x *= f_u_xlat6;
    f_u_xlat6 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat6;
    f_u_xlat0.x = f_u_xlat3.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat3 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_u_xlat3);
    f_u_xlat0.x *= f_u_xlat16_1.w;
    f_u_xlat16_2 = f_u_xlat16_1.xyz * _LightColor0.xyz;
    f_SV_Target0.w = f_u_xlat0.x;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _137 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _137.x;
    f_SV_Target0.y = _137.y;
    f_SV_Target0.z = _137.z;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
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
Texture2D<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
Texture2D<float4> _LightTextureB0;
SamplerState sampler_LightTextureB0;
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _WorldSpaceLightPos0;
float _BlendWidthX;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;

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
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat10;
static float4 v_vs_TEXCOORD3;
static float4 v_vs_TEXCOORD4;

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
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_WorldToLight)[1];
    v_u_xlat1 = (transpose(unity_WorldToLight)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_WorldToLight)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_vs_TEXCOORD3 = (transpose(unity_WorldToLight)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
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
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat0;
static float2 f_u_xlat1;
static float f_u_xlat9;
static bool f_u_xlatb1;
static float3 f_u_xlat16_2;
static float4 f_u_xlat16_0;
static float f_u_xlat16_11;
static float f_u_xlat6;
static float f_u_xlat3;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD2.yyyy * transpose(unity_WorldToLight)[1];
    f_u_xlat0 = (transpose(unity_WorldToLight)[0] * f_vs_TEXCOORD2.xxxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2] * f_vs_TEXCOORD2.zzzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3];
    f_u_xlat1 = f_u_xlat0.xy / f_u_xlat0.ww;
    f_u_xlat1 += 0.5f.xx;
    f_u_xlat9 = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat1).w;
    f_u_xlatb1 = 0.0f < f_u_xlat0.z;
    f_u_xlat0.x = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat0.x = _LightTextureB0.Sample(sampler_LightTextureB0, f_u_xlat0.xx).x;
    f_u_xlat16_2.x = float(f_u_xlatb1);
    f_u_xlat16_2.x = f_u_xlat9 * f_u_xlat16_2.x;
    f_u_xlat16_2.x = f_u_xlat0.x * f_u_xlat16_2.x;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _121 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _121.x;
    f_u_xlat0.y = _121.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_2 *= f_u_xlat16_0.xyz;
    float3 _142 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat0.x = _142.x;
    f_u_xlat0.y = _142.y;
    f_u_xlat0.z = _142.z;
    f_u_xlat1.x = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat1.x = rsqrt(f_u_xlat1.x);
    float4 _159 = f_u_xlat0;
    float3 _163 = _159.xyz * f_u_xlat1.xxx;
    f_u_xlat0.x = _163.x;
    f_u_xlat0.y = _163.y;
    f_u_xlat0.z = _163.z;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD1, f_u_xlat0.xyz);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _183 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _183.x;
    f_SV_Target0.y = _183.y;
    f_SV_Target0.z = _183.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat6 = 1.0f / _BlendWidthX;
    float4 _209 = f_u_xlat0;
    float2 _211 = f_u_xlat6.xx * _209.xy;
    f_u_xlat0.x = _211.x;
    f_u_xlat0.y = _211.y;
    float4 _216 = f_u_xlat0;
    float2 _220 = clamp(_216.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _220.x;
    f_u_xlat0.y = _220.y;
    f_u_xlat6 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat3 = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat3 *= f_u_xlat6;
    f_u_xlat6 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat6;
    f_u_xlat0.x = f_u_xlat3 * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
#elif !defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE) && !defined(POINT) && defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_WorldToLight;
float4 _MainTex_ST;
float4 _WorldSpaceLightPos0;
TextureCube<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
Texture2D<float4> _LightTextureB0;
SamplerState sampler_LightTextureB0;
float4 _LightColor0;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _BlendWidthX;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;

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
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat10;
static float3 v_vs_TEXCOORD3;
static float4 v_vs_TEXCOORD4;

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
    float3 _157 = v_u_xlat0.yyy * transpose(unity_WorldToLight)[1].xyz;
    v_u_xlat1.x = _157.x;
    v_u_xlat1.y = _157.y;
    v_u_xlat1.z = _157.z;
    float4 _170 = v_u_xlat1;
    float3 _172 = (transpose(unity_WorldToLight)[0].xyz * v_u_xlat0.xxx) + _170.xyz;
    v_u_xlat1.x = _172.x;
    v_u_xlat1.y = _172.y;
    v_u_xlat1.z = _172.z;
    float4 _182 = v_u_xlat0;
    float3 _187 = (transpose(unity_WorldToLight)[2].xyz * _182.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _187.x;
    v_u_xlat0.y = _187.y;
    v_u_xlat0.z = _187.z;
    v_vs_TEXCOORD3 = (transpose(unity_WorldToLight)[3].xyz * v_u_xlat0.www) + v_u_xlat0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
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
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float2 f_vs_TEXCOORD0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float f_u_xlat6;
static float f_u_xlat16_1;
static float f_u_xlat2;
static float3 f_u_xlat16_3;
static float4 f_u_xlat16_0;
static float f_u_xlat4;

void f_frag_main()
{
    f_u_xlat0 = (-f_vs_TEXCOORD2) + _WorldSpaceLightPos0.xyz;
    f_u_xlat6 = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat6 = rsqrt(f_u_xlat6);
    f_u_xlat0 = f_u_xlat6.xxx * f_u_xlat0;
    f_u_xlat16_1 = dot(f_vs_TEXCOORD1, f_u_xlat0);
    f_u_xlat16_1 = max(f_u_xlat16_1, 0.0f);
    f_u_xlat0 = f_vs_TEXCOORD2.yyy * transpose(unity_WorldToLight)[1].xyz;
    f_u_xlat0 = (transpose(unity_WorldToLight)[0].xyz * f_vs_TEXCOORD2.xxx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2].xyz * f_vs_TEXCOORD2.zzz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3].xyz;
    f_u_xlat6 = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0).w;
    f_u_xlat2 = _LightTextureB0.Sample(sampler_LightTextureB0, f_u_xlat6.xx).x;
    f_u_xlat0.x *= f_u_xlat2;
    f_u_xlat16_3 = f_u_xlat0.xxx * _LightColor0.xyz;
    float2 _119 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _119.x;
    f_u_xlat0.y = _119.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy);
    f_u_xlat16_3 *= f_u_xlat16_0.xyz;
    float3 _141 = f_u_xlat16_1.xxx * f_u_xlat16_3;
    f_SV_Target0.x = _141.x;
    f_SV_Target0.y = _141.y;
    f_SV_Target0.z = _141.z;
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat4 = 1.0f / _BlendWidthX;
    float3 _169 = f_u_xlat0;
    float2 _171 = f_u_xlat4.xx * _169.xy;
    f_u_xlat0.x = _171.x;
    f_u_xlat0.y = _171.y;
    float3 _176 = f_u_xlat0;
    float2 _180 = clamp(_176.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _180.x;
    f_u_xlat0.y = _180.y;
    f_u_xlat4 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat2 = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat2 *= f_u_xlat4;
    f_u_xlat4 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4;
    f_u_xlat0.x = f_u_xlat2 * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat0.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = f_u_xlat0.x;
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
#elif !defined(DIRECTIONAL) && defined(DIRECTIONAL_COOKIE) && !defined(POINT) && !defined(POINT_COOKIE) && !defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(SHADOWS_SCREEN) && !defined(SHADOWS_SOFT) && !defined(SPOT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToLight;
float4x4 unity_WorldToObject;
float4 _MainTex_ST;
float _BlendWidthX;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
Texture2D<float4> _LightTexture0;
SamplerState sampler_LightTexture0;
float4 _LightColor0;
float4 _WorldSpaceLightPos0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;

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
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float2 v_vs_TEXCOORD3;
static float v_u_xlat9;
static float4 v_vs_TEXCOORD4;

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
    v_vs_TEXCOORD3 = (transpose(unity_WorldToLight)[3].xy * v_u_xlat1.ww) + v_u_xlat0.xy;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    v_vs_TEXCOORD1 = v_u_xlat9.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
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
    return stage_output;
}
static float2 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD1;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat0;
static float f_u_xlat6;
static float2 f_u_xlat3;
static float4 f_u_xlat16_1;
static float3 f_u_xlat16_2;
static float f_u_xlat16_11;

void f_frag_main()
{
    f_u_xlat0.x = frac(f_vs_TEXCOORD0.x);
    f_u_xlat0.y = (-f_u_xlat0.x) + 1.0f;
    f_u_xlat6 = 1.0f / _BlendWidthX;
    f_u_xlat0 = f_u_xlat6.xx * f_u_xlat0;
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f.xx, 1.0f.xx);
    f_u_xlat6 = (f_u_xlat0.y * (-2.0f)) + 3.0f;
    f_u_xlat3.x = f_u_xlat0.y * f_u_xlat0.y;
    f_u_xlat3.x *= f_u_xlat6;
    f_u_xlat6 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat6;
    f_u_xlat0.x = f_u_xlat3.x * f_u_xlat0.x;
    f_u_xlat0.x = min(f_u_xlat0.x, 1.0f);
    f_u_xlat3 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_u_xlat3);
    f_u_xlat0.x *= f_u_xlat16_1.w;
    f_SV_Target0.w = f_u_xlat0.x;
    f_u_xlat0 = f_vs_TEXCOORD2.yy * transpose(unity_WorldToLight)[1].xy;
    f_u_xlat0 = (transpose(unity_WorldToLight)[0].xy * f_vs_TEXCOORD2.xx) + f_u_xlat0;
    f_u_xlat0 = (transpose(unity_WorldToLight)[2].xy * f_vs_TEXCOORD2.zz) + f_u_xlat0;
    f_u_xlat0 += transpose(unity_WorldToLight)[3].xy;
    f_u_xlat0.x = _LightTexture0.Sample(sampler_LightTexture0, f_u_xlat0).w;
    f_u_xlat16_2 = f_u_xlat0.xxx * _LightColor0.xyz;
    f_u_xlat16_2 = f_u_xlat16_1.xyz * f_u_xlat16_2;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD1, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _183 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _183.x;
    f_SV_Target0.y = _183.y;
    f_SV_Target0.z = _183.z;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
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
Name "DEFERRED"
Tags { "LightMode"="Deferred" "Queue"="Geometry+1" "RenderType"="Opaque" }
Cull Off
ZWrite Off
ZTest LEqual
Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
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
#pragma multi_compile _ LIGHTPROBE_SH
#pragma multi_compile _ UNITY_HDR_ON
#if defined(LIGHTPROBE_SH) && defined(UNITY_HDR_ON)
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
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD1 : TEXCOORD0;
    float3 v_vs_TEXCOORD2 : TEXCOORD1;
    float3 v_vs_TEXCOORD4 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float2 v_vs_TEXCOORD0;
static float v_u_xlat12;
static float4 v_vs_TEXCOORD3;
static float3 v_u_xlat16_2;
static float4 v_u_xlat16_1;
static float3 v_u_xlat16_3;

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
    v_vs_TEXCOORD3 = 0.0f.xxxx;
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
    v_u_xlat16_2 += v_u_xlat16_3;
    v_vs_TEXCOORD4 = max(v_u_xlat16_2, 0.0f.xxx);
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
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;
static float4 f_SV_Target3;
static float3 f_vs_TEXCOORD4;
static float4 f_SV_Target1;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target2;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD1 : TEXCOORD0;
    float3 f_vs_TEXCOORD2 : TEXCOORD1;
    float3 f_vs_TEXCOORD4 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
    float4 f_SV_Target1 : SV_Target1;
    float4 f_SV_Target2 : SV_Target2;
    float4 f_SV_Target3 : SV_Target3;
};

static float4 f_u_xlat0;
static float3 f_u_xlat16_0;

void f_frag_main()
{
    float2 _20 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _20.x;
    f_u_xlat0.y = _20.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy).xyz;
    f_SV_Target0.x = f_u_xlat16_0.x;
    f_SV_Target0.y = f_u_xlat16_0.y;
    f_SV_Target0.z = f_u_xlat16_0.z;
    float3 _55 = f_u_xlat16_0 * f_vs_TEXCOORD4;
    f_SV_Target3.x = _55.x;
    f_SV_Target3.y = _55.y;
    f_SV_Target3.z = _55.z;
    f_SV_Target0.w = 1.0f;
    f_SV_Target1 = 0.0f.xxxx;
    float3 _73 = (f_vs_TEXCOORD1 * 0.5f.xxx) + 0.5f.xxx;
    f_u_xlat0.x = _73.x;
    f_u_xlat0.y = _73.y;
    f_u_xlat0.z = _73.z;
    f_u_xlat0.w = 1.0f;
    f_SV_Target2 = f_u_xlat0;
    f_SV_Target3.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    stage_output.f_SV_Target3 = f_SV_Target3;
    stage_output.f_SV_Target1 = f_SV_Target1;
    stage_output.f_SV_Target2 = f_SV_Target2;
    return stage_output;
}
#elif defined(LIGHTPROBE_SH) && !defined(UNITY_HDR_ON)
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
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD1 : TEXCOORD0;
    float3 v_vs_TEXCOORD2 : TEXCOORD1;
    float3 v_vs_TEXCOORD4 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float2 v_vs_TEXCOORD0;
static float v_u_xlat12;
static float4 v_vs_TEXCOORD3;
static float3 v_u_xlat16_2;
static float4 v_u_xlat16_1;
static float3 v_u_xlat16_3;

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
    v_vs_TEXCOORD3 = 0.0f.xxxx;
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
    v_u_xlat16_2 += v_u_xlat16_3;
    v_vs_TEXCOORD4 = max(v_u_xlat16_2, 0.0f.xxx);
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
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD4;
static float4 f_SV_Target3;
static float4 f_SV_Target1;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target2;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD1 : TEXCOORD0;
    float3 f_vs_TEXCOORD2 : TEXCOORD1;
    float3 f_vs_TEXCOORD4 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
    float4 f_SV_Target1 : SV_Target1;
    float4 f_SV_Target2 : SV_Target2;
    float4 f_SV_Target3 : SV_Target3;
};

static float4 f_u_xlat0;
static float3 f_u_xlat16_0;
static float3 f_u_xlat16_1;

void f_frag_main()
{
    float2 _20 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _20.x;
    f_u_xlat0.y = _20.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy).xyz;
    f_SV_Target0.x = f_u_xlat16_0.x;
    f_SV_Target0.y = f_u_xlat16_0.y;
    f_SV_Target0.z = f_u_xlat16_0.z;
    f_u_xlat16_1 = f_u_xlat16_0 * f_vs_TEXCOORD4;
    float3 _59 = exp2(-f_u_xlat16_1);
    f_SV_Target3.x = _59.x;
    f_SV_Target3.y = _59.y;
    f_SV_Target3.z = _59.z;
    f_SV_Target0.w = 1.0f;
    f_SV_Target1 = 0.0f.xxxx;
    float3 _77 = (f_vs_TEXCOORD1 * 0.5f.xxx) + 0.5f.xxx;
    f_u_xlat0.x = _77.x;
    f_u_xlat0.y = _77.y;
    f_u_xlat0.z = _77.z;
    f_u_xlat0.w = 1.0f;
    f_SV_Target2 = f_u_xlat0;
    f_SV_Target3.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    stage_output.f_SV_Target3 = f_SV_Target3;
    stage_output.f_SV_Target1 = f_SV_Target1;
    stage_output.f_SV_Target2 = f_SV_Target2;
    return stage_output;
}
#elif !defined(LIGHTPROBE_SH) && defined(UNITY_HDR_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4 _MainTex_ST;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD1 : TEXCOORD0;
    float3 v_vs_TEXCOORD2 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float2 v_vs_TEXCOORD0;
static float v_u_xlat6;
static float4 v_vs_TEXCOORD3;

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
    v_vs_TEXCOORD3 = 0.0f.xxxx;
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
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;
static float4 f_SV_Target1;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target2;
static float4 f_SV_Target3;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD1 : TEXCOORD0;
    float3 f_vs_TEXCOORD2 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
    float4 f_SV_Target1 : SV_Target1;
    float4 f_SV_Target2 : SV_Target2;
    float4 f_SV_Target3 : SV_Target3;
};

static float4 f_u_xlat0;
static float3 f_u_xlat16_0;

void f_frag_main()
{
    float2 _20 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _20.x;
    f_u_xlat0.y = _20.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy).xyz;
    f_SV_Target0.x = f_u_xlat16_0.x;
    f_SV_Target0.y = f_u_xlat16_0.y;
    f_SV_Target0.z = f_u_xlat16_0.z;
    f_SV_Target0.w = 1.0f;
    f_SV_Target1 = 0.0f.xxxx;
    float3 _62 = (f_vs_TEXCOORD1 * 0.5f.xxx) + 0.5f.xxx;
    f_u_xlat0.x = _62.x;
    f_u_xlat0.y = _62.y;
    f_u_xlat0.z = _62.z;
    f_u_xlat0.w = 1.0f;
    f_SV_Target2 = f_u_xlat0;
    f_SV_Target3 = float4(0.0f, 0.0f, 0.0f, 1.0f);
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    stage_output.f_SV_Target1 = f_SV_Target1;
    stage_output.f_SV_Target2 = f_SV_Target2;
    stage_output.f_SV_Target3 = f_SV_Target3;
    return stage_output;
}
#elif !defined(LIGHTPROBE_SH) && !defined(UNITY_HDR_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4 _MainTex_ST;
float _MainTilingWorld;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD2;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD1 : TEXCOORD0;
    float3 v_vs_TEXCOORD2 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float2 v_vs_TEXCOORD0;
static float v_u_xlat6;
static float4 v_vs_TEXCOORD3;

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
    v_vs_TEXCOORD3 = 0.0f.xxxx;
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
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;
static float4 f_SV_Target1;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target2;
static float4 f_SV_Target3;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD1 : TEXCOORD0;
    float3 f_vs_TEXCOORD2 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
    float4 f_SV_Target1 : SV_Target1;
    float4 f_SV_Target2 : SV_Target2;
    float4 f_SV_Target3 : SV_Target3;
};

static float4 f_u_xlat0;
static float3 f_u_xlat16_0;

void f_frag_main()
{
    float2 _20 = f_vs_TEXCOORD2.xz * _MainTilingWorld.xx;
    f_u_xlat0.x = _20.x;
    f_u_xlat0.y = _20.y;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_u_xlat0.xy).xyz;
    f_SV_Target0.x = f_u_xlat16_0.x;
    f_SV_Target0.y = f_u_xlat16_0.y;
    f_SV_Target0.z = f_u_xlat16_0.z;
    f_SV_Target0.w = 1.0f;
    f_SV_Target1 = 0.0f.xxxx;
    float3 _62 = (f_vs_TEXCOORD1 * 0.5f.xxx) + 0.5f.xxx;
    f_u_xlat0.x = _62.x;
    f_u_xlat0.y = _62.y;
    f_u_xlat0.z = _62.z;
    f_u_xlat0.w = 1.0f;
    f_SV_Target2 = f_u_xlat0;
    f_SV_Target3 = 1.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    stage_output.f_SV_Target1 = f_SV_Target1;
    stage_output.f_SV_Target2 = f_SV_Target2;
    stage_output.f_SV_Target3 = f_SV_Target3;
    return stage_output;
}
#else
#error This keyword combination was not present in the shipped APK shader.
#endif
ENDHLSL
}
Pass {
Name "ShadowCaster"
Tags { "LightMode"="ShadowCaster" "Queue"="Geometry+1" "RenderType"="Opaque" "SHADOWSUPPORT"="true" }
Cull Off
ZWrite On
ZTest LEqual
Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
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
float4 _MainTex_ST;
static float4 v_gl_Position;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;
static float4 v_in_TEXCOORD0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;
static float v_u_xlat6;
static float2 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;

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
    v_vs_TEXCOORD1 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    float3 _282 = v_in_POSITION0.yyy * transpose(unity_ObjectToWorld)[1].xyz;
    v_u_xlat0.x = _282.x;
    v_u_xlat0.y = _282.y;
    v_u_xlat0.z = _282.z;
    float4 _295 = v_u_xlat0;
    float3 _297 = (transpose(unity_ObjectToWorld)[0].xyz * v_in_POSITION0.xxx) + _295.xyz;
    v_u_xlat0.x = _297.x;
    v_u_xlat0.y = _297.y;
    v_u_xlat0.z = _297.z;
    float4 _310 = v_u_xlat0;
    float3 _312 = (transpose(unity_ObjectToWorld)[2].xyz * v_in_POSITION0.zzz) + _310.xyz;
    v_u_xlat0.x = _312.x;
    v_u_xlat0.y = _312.y;
    v_u_xlat0.z = _312.z;
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    return stage_output;
}
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

void f_frag_main()
{
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag()
{
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
float4 _MainTex_ST;
static float4 v_gl_Position;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;
static float4 v_in_TEXCOORD0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;
static float2 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;

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
    v_vs_TEXCOORD1 = (v_in_TEXCOORD0.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    float3 _269 = v_in_POSITION0.yyy * transpose(unity_ObjectToWorld)[1].xyz;
    v_u_xlat0.x = _269.x;
    v_u_xlat0.y = _269.y;
    v_u_xlat0.z = _269.z;
    float4 _282 = v_u_xlat0;
    float3 _284 = (transpose(unity_ObjectToWorld)[0].xyz * v_in_POSITION0.xxx) + _282.xyz;
    v_u_xlat0.x = _284.x;
    v_u_xlat0.y = _284.y;
    v_u_xlat0.z = _284.z;
    float4 _297 = v_u_xlat0;
    float3 _299 = (transpose(unity_ObjectToWorld)[2].xyz * v_in_POSITION0.zzz) + _297.xyz;
    v_u_xlat0.x = _299.x;
    v_u_xlat0.y = _299.y;
    v_u_xlat0.z = _299.z;
    v_vs_TEXCOORD2 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    return stage_output;
}
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

void f_frag_main()
{
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag()
{
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
