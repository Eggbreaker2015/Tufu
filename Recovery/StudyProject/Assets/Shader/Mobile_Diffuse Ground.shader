Shader "Mobile/Diffuse Ground" {
Properties {
		_Texture0 ("Texture 0", 2D) = "white" {}
		_MaskColor1 ("Mask Color 1", Vector) = (0.627451,0.8117648,0.3215686,1)
		_MaskColor2 ("Mask Color 2", Vector) = (0.5333334,0.6980392,0.2784314,1)
		_UVTile ("UV Tile", Float) = 3
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
#pragma multi_compile _ INSTANCING_ON
#pragma multi_compile _ LIGHTPROBE_SH
#pragma multi_compile _ SHADOWS_SCREEN
#pragma multi_compile _ VERTEXLIGHT_ON
#if defined(DIRECTIONAL) && defined(INSTANCING_ON) && defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
int unity_BaseInstanceID;
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
float4 _LightColor0;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 _WorldSpaceLightPos0;
#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
#else
sampler2D _ShadowMapTexture;
#endif
float4 _ScreenParams;
#include "RecoveryDirectionalShadows.cginc"

float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
struct v_unity_Builtins0Array_Type
{
    float4 hlslcc_mtx4x4unity_ObjectToWorldArray[4];
    float4 hlslcc_mtx4x4unity_WorldToObjectArray[4];
};

struct v_unity_Builtins2Array_Type
{
    float4 unity_SHArArray;
    float4 unity_SHAgArray;
    float4 unity_SHAbArray;
    float4 unity_SHBrArray;
    float4 unity_SHBgArray;
    float4 unity_SHBbArray;
    float4 unity_SHCArray;
};

cbuffer UnityInstancing_PerDraw0
{
    v_unity_Builtins0Array_Type _40_unity_Builtins0Array[2] : packoffset(c0);
};

cbuffer UnityInstancing_PerDraw2
{
    v_unity_Builtins2Array_Type _210_unity_Builtins2Array[2] : packoffset(c0);
};







static float4 v_gl_Position;
static int v_gl_InstanceID;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static uint v_vs_SV_InstanceID0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    uint v_gl_InstanceID : SV_InstanceID;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
    nointerpolation uint v_vs_SV_InstanceID0 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static int v_u_xlati0;
static int v_u_xlati6;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float4 v_u_xlat3;
static float3 v_u_xlat6;
static float3 v_u_xlat16_4;
static float4 v_u_xlat16_2;
static float3 v_u_xlat16_5;
static float4 v_u_xlat5;
static float4 v_u_xlat0;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlati0 = v_gl_InstanceID + unity_BaseInstanceID;
    v_u_xlati6 = v_u_xlati0 << 3;
    v_u_xlati0 *= 7;
    v_u_xlat1 = v_in_POSITION0.yyyy * _40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[1];
    v_u_xlat1 = (_40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (_40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + _40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3];
    float4 _85 = v_u_xlat1;
    float3 _87 = (_40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3].xyz * v_in_POSITION0.www) + _85.xyz;
    v_u_xlat1.x = _87.x;
    v_u_xlat1.y = _87.y;
    v_u_xlat1.z = _87.z;
    v_u_xlat3 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat3 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat3;
    v_u_xlat3 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat3;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat3;
    v_u_xlat2.x = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[0].xyz);
    v_u_xlat2.y = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[1].xyz);
    v_u_xlat2.z = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[2].xyz);
    v_u_xlat6.x = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat6.x = rsqrt(v_u_xlat6.x);
    v_u_xlat6 = v_u_xlat6.xxx * v_u_xlat2.xyz;
    v_vs_TEXCOORD0 = v_u_xlat6;
    v_vs_TEXCOORD1 = v_u_xlat1.xyz;
    v_u_xlat16_4.x = v_u_xlat6.y * v_u_xlat6.y;
    v_u_xlat16_4.x = (v_u_xlat6.x * v_u_xlat6.x) + (-v_u_xlat16_4.x);
    v_u_xlat16_2 = v_u_xlat6.yzzx * v_u_xlat6.xyzz;
    v_u_xlat16_5.x = dot(_210_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBrArray, v_u_xlat16_2);
    v_u_xlat16_5.y = dot(_210_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBgArray, v_u_xlat16_2);
    v_u_xlat16_5.z = dot(_210_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBbArray, v_u_xlat16_2);
    v_u_xlat16_4 = (_210_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHCArray.xyz * v_u_xlat16_4.xxx) + v_u_xlat16_5;
    v_u_xlat2 = (-v_u_xlat1.yyyy) + unity_4LightPosY0;
    v_u_xlat3 = v_u_xlat6.yyyy * v_u_xlat2;
    v_u_xlat2 *= v_u_xlat2;
    v_u_xlat5 = (-v_u_xlat1.xxxx) + unity_4LightPosX0;
    v_u_xlat1 = (-v_u_xlat1.zzzz) + unity_4LightPosZ0;
    v_u_xlat3 = (v_u_xlat5 * v_u_xlat6.xxxx) + v_u_xlat3;
    v_u_xlat0 = (v_u_xlat1 * v_u_xlat6.zzzz) + v_u_xlat3;
    v_u_xlat2 = (v_u_xlat5 * v_u_xlat5) + v_u_xlat2;
    v_u_xlat1 = (v_u_xlat1 * v_u_xlat1) + v_u_xlat2;
    v_u_xlat1 = max(v_u_xlat1, 9.9999999747524270787835121154785e-07f.xxxx);
    v_u_xlat2 = rsqrt(v_u_xlat1);
    v_u_xlat1 = (v_u_xlat1 * unity_4LightAtten0) + 1.0f.xxxx;
    v_u_xlat1 = 1.0f.xxxx / v_u_xlat1;
    v_u_xlat0 *= v_u_xlat2;
    v_u_xlat0 = max(v_u_xlat0, 0.0f.xxxx);
    v_u_xlat0 = v_u_xlat1 * v_u_xlat0;
    float3 _328 = v_u_xlat0.yyy * unity_LightColor[1].xyz;
    v_u_xlat1.x = _328.x;
    v_u_xlat1.y = _328.y;
    v_u_xlat1.z = _328.z;
    float4 _341 = v_u_xlat1;
    float3 _343 = (unity_LightColor[0].xyz * v_u_xlat0.xxx) + _341.xyz;
    v_u_xlat1.x = _343.x;
    v_u_xlat1.y = _343.y;
    v_u_xlat1.z = _343.z;
    float4 _353 = v_u_xlat0;
    float3 _358 = (unity_LightColor[2].xyz * _353.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _358.x;
    v_u_xlat0.y = _358.y;
    v_u_xlat0.z = _358.z;
    float4 _368 = v_u_xlat0;
    float4 _371 = v_u_xlat0;
    float3 _373 = (unity_LightColor[3].xyz * _368.www) + _371.xyz;
    v_u_xlat0.x = _373.x;
    v_u_xlat0.y = _373.y;
    v_u_xlat0.z = _373.z;
    v_vs_TEXCOORD2 = v_u_xlat16_4 + v_u_xlat0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
    v_vs_SV_InstanceID0 = uint(v_gl_InstanceID);
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_gl_InstanceID = int(stage_input.v_gl_InstanceID);
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_SV_InstanceID0 = v_vs_SV_InstanceID0;
    return stage_output;
}
struct f_unity_Builtins2Array_Type
{
    float4 unity_SHArArray;
    float4 unity_SHAgArray;
    float4 unity_SHAbArray;
    float4 unity_SHBrArray;
    float4 unity_SHBgArray;
    float4 unity_SHBbArray;
    float4 unity_SHCArray;
};

cbuffer UnityInstancing_PerDraw2
{
    f_unity_Builtins2Array_Type _191_unity_Builtins2Array[2] : packoffset(c0);
};
















static float3 f_vs_TEXCOORD1;
static uint f_vs_SV_InstanceID0;
static float3 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
    nointerpolation uint f_vs_SV_InstanceID0 : TEXCOORD3;
    float4 recoveryPixelPosition : SV_Position;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat0;
static float3 f_u_xlat4;
static float4 f_u_xlat1;
static float f_u_xlat8;
static int f_u_xlati8;
static float3 f_u_xlat16_2;
static float f_u_xlat16_0;
static float3 f_u_xlat16_3;
static float f_u_xlat16_14;

void f_frag_main()
{
    float3 _19 = f_vs_TEXCOORD1 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = _19.x;
    f_u_xlat0.y = _19.y;
    f_u_xlat0.z = _19.z;
    f_u_xlat0.x = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat4 = (-f_vs_TEXCOORD1) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat1.xyz);
    f_u_xlat0.x = (-f_u_xlat4.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat4.x;
    f_u_xlat0.x = (f_u_xlat0.x * RecoveryDirectionalShadowData().z) + RecoveryDirectionalShadowData().w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4 = f_vs_TEXCOORD1.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD1.xxx) + f_u_xlat4;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD1.zzz) + f_u_xlat4;
    f_u_xlat4 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat4.xy, f_u_xlat4.z);
    f_u_xlat4.x = RecoverySampleDirectionalShadow(txVec0);
    f_u_xlat8 = (-RecoveryDirectionalShadowData().x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + RecoveryDirectionalShadowData().x;
    f_u_xlati8 = int(f_vs_SV_InstanceID0) + unity_BaseInstanceID;
    f_u_xlati8 *= 7;
    f_u_xlat16_2.x = (-f_u_xlat4.x) + _191_unity_Builtins2Array[f_u_xlati8 / 7].unity_SHCArray.w;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat4.x;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _220 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _220.x;
    f_u_xlat0.y = _220.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    float3 _242 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat1.x = _242.x;
    f_u_xlat1.y = _242.y;
    f_u_xlat1.z = _242.z;
    float3 _256 = (f_u_xlat16_0.xxx * f_u_xlat1.xyz) + _MaskColor1.xyz;
    f_u_xlat0.x = _256.x;
    f_u_xlat0.y = _256.y;
    f_u_xlat0.w = _256.z;
    f_u_xlat16_2 *= f_u_xlat0.xyw;
    f_u_xlat1.x = f_vs_TEXCOORD0.x;
    f_u_xlat1.y = f_vs_TEXCOORD0.y;
    f_u_xlat1.z = f_vs_TEXCOORD0.z;
    f_u_xlat1.w = 1.0f;
    f_u_xlat16_3.x = dot(_191_unity_Builtins2Array[f_u_xlati8 / 7].unity_SHArArray, f_u_xlat1);
    f_u_xlat16_3.y = dot(_191_unity_Builtins2Array[f_u_xlati8 / 7].unity_SHAgArray, f_u_xlat1);
    f_u_xlat16_3.z = dot(_191_unity_Builtins2Array[f_u_xlati8 / 7].unity_SHAbArray, f_u_xlat1);
    f_u_xlat16_3 += f_vs_TEXCOORD2;
    f_u_xlat16_3 = max(f_u_xlat16_3, 0.0f.xxx);
    f_u_xlat16_3 = f_u_xlat0.xyw * f_u_xlat16_3;
    f_u_xlat16_14 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_14 = max(f_u_xlat16_14, 0.0f);
    float3 _325 = (f_u_xlat16_2 * f_u_xlat16_14.xxx) + f_u_xlat16_3;
    f_SV_Target0.x = _325.x;
    f_SV_Target0.y = _325.y;
    f_SV_Target0.z = _325.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_SV_InstanceID0 = stage_input.f_vs_SV_InstanceID0;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    RecoveryShadowScreenUV = stage_input.recoveryPixelPosition.xy / _ScreenParams.xy;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(INSTANCING_ON) && defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
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
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 _WorldSpaceLightPos0;
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
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
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
static float4 v_vs_TEXCOORD4;
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
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat21 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat21 = rsqrt(v_u_xlat21);
    float4 _131 = v_u_xlat1;
    float3 _133 = v_u_xlat21.xxx * _131.xyz;
    v_u_xlat1.x = _133.x;
    v_u_xlat1.y = _133.y;
    v_u_xlat1.z = _133.z;
    v_vs_TEXCOORD0 = v_u_xlat1.xyz;
    v_vs_TEXCOORD1 = v_u_xlat0.xyz;
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
    float3 _230 = v_u_xlat0.yyy * unity_LightColor[1].xyz;
    v_u_xlat2.x = _230.x;
    v_u_xlat2.y = _230.y;
    v_u_xlat2.z = _230.z;
    float4 _243 = v_u_xlat2;
    float3 _245 = (unity_LightColor[0].xyz * v_u_xlat0.xxx) + _243.xyz;
    v_u_xlat2.x = _245.x;
    v_u_xlat2.y = _245.y;
    v_u_xlat2.z = _245.z;
    float4 _255 = v_u_xlat0;
    float3 _260 = (unity_LightColor[2].xyz * _255.zzz) + v_u_xlat2.xyz;
    v_u_xlat0.x = _260.x;
    v_u_xlat0.y = _260.y;
    v_u_xlat0.z = _260.z;
    float4 _270 = v_u_xlat0;
    float4 _273 = v_u_xlat0;
    float3 _275 = (unity_LightColor[3].xyz * _270.www) + _273.xyz;
    v_u_xlat0.x = _275.x;
    v_u_xlat0.y = _275.y;
    v_u_xlat0.z = _275.z;
    v_u_xlat16_5.x = v_u_xlat1.y * v_u_xlat1.y;
    v_u_xlat16_5.x = (v_u_xlat1.x * v_u_xlat1.x) + (-v_u_xlat16_5.x);
    v_u_xlat16_1 = v_u_xlat1.yzzx * v_u_xlat1.xyzz;
    v_u_xlat16_6.x = dot(unity_SHBr, v_u_xlat16_1);
    v_u_xlat16_6.y = dot(unity_SHBg, v_u_xlat16_1);
    v_u_xlat16_6.z = dot(unity_SHBb, v_u_xlat16_1);
    v_u_xlat16_5 = (unity_SHC.xyz * v_u_xlat16_5.xxx) + v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat0.xyz + v_u_xlat16_5;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
    float4 recoveryPixelPosition : SV_Position;
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
static float f_u_xlat16_0;
static float3 f_u_xlat16_3;
static float f_u_xlat16_14;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD1 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat4 = (-f_vs_TEXCOORD1) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat1.xyz);
    f_u_xlat0.x = (-f_u_xlat4.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat4.x;
    f_u_xlat0.x = (f_u_xlat0.x * RecoveryDirectionalShadowData().z) + RecoveryDirectionalShadowData().w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4 = f_vs_TEXCOORD1.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD1.xxx) + f_u_xlat4;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD1.zzz) + f_u_xlat4;
    f_u_xlat4 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat4.xy, f_u_xlat4.z);
    f_u_xlat4.x = RecoverySampleDirectionalShadow(txVec0);
    f_u_xlat8 = (-RecoveryDirectionalShadowData().x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + RecoveryDirectionalShadowData().x;
    f_u_xlat16_2.x = (-f_u_xlat4.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat4.x;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _188 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _188.x;
    f_u_xlat0.y = _188.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat4 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat0 = (f_u_xlat16_0.xxx * f_u_xlat4) + _MaskColor1.xyz;
    f_u_xlat16_2 *= f_u_xlat0;
    f_u_xlat1.x = f_vs_TEXCOORD0.x;
    f_u_xlat1.y = f_vs_TEXCOORD0.y;
    f_u_xlat1.z = f_vs_TEXCOORD0.z;
    f_u_xlat1.w = 1.0f;
    f_u_xlat16_3.x = dot(unity_SHAr, f_u_xlat1);
    f_u_xlat16_3.y = dot(unity_SHAg, f_u_xlat1);
    f_u_xlat16_3.z = dot(unity_SHAb, f_u_xlat1);
    f_u_xlat16_3 += f_vs_TEXCOORD2;
    f_u_xlat16_3 = max(f_u_xlat16_3, 0.0f.xxx);
    f_u_xlat16_3 = f_u_xlat0 * f_u_xlat16_3;
    f_u_xlat16_14 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_14 = max(f_u_xlat16_14, 0.0f);
    float3 _271 = (f_u_xlat16_2 * f_u_xlat16_14.xxx) + f_u_xlat16_3;
    f_SV_Target0.x = _271.x;
    f_SV_Target0.y = _271.y;
    f_SV_Target0.z = _271.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    RecoveryShadowScreenUV = stage_input.recoveryPixelPosition.xy / _ScreenParams.xy;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && defined(INSTANCING_ON) && defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
int unity_BaseInstanceID;
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _LightColor0;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 _WorldSpaceLightPos0;
#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
#else
sampler2D _ShadowMapTexture;
#endif
float4 _ScreenParams;
#include "RecoveryDirectionalShadows.cginc"

float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
struct v_unity_Builtins0Array_Type
{
    float4 hlslcc_mtx4x4unity_ObjectToWorldArray[4];
    float4 hlslcc_mtx4x4unity_WorldToObjectArray[4];
};

struct v_unity_Builtins2Array_Type
{
    float4 unity_SHArArray;
    float4 unity_SHAgArray;
    float4 unity_SHAbArray;
    float4 unity_SHBrArray;
    float4 unity_SHBgArray;
    float4 unity_SHBbArray;
    float4 unity_SHCArray;
};

cbuffer UnityInstancing_PerDraw0
{
    v_unity_Builtins0Array_Type _40_unity_Builtins0Array[2] : packoffset(c0);
};

cbuffer UnityInstancing_PerDraw2
{
    v_unity_Builtins2Array_Type _198_unity_Builtins2Array[2] : packoffset(c0);
};


static float4 v_gl_Position;
static int v_gl_InstanceID;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static float3 v_vs_TEXCOORD2;
static uint v_vs_SV_InstanceID0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    uint v_gl_InstanceID : SV_InstanceID;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
    nointerpolation uint v_vs_SV_InstanceID0 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static int v_u_xlati0;
static int v_u_xlati5;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float3 v_u_xlat5;
static float v_u_xlat16_3;
static float4 v_u_xlat16_1;
static float3 v_u_xlat16_4;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlati0 = v_gl_InstanceID + unity_BaseInstanceID;
    v_u_xlati5 = v_u_xlati0 << 3;
    v_u_xlati0 *= 7;
    v_u_xlat1 = v_in_POSITION0.yyyy * _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[1];
    v_u_xlat1 = (_40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (_40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3];
    v_vs_TEXCOORD1 = (_40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3].xyz * v_in_POSITION0.www) + v_u_xlat1.xyz;
    v_u_xlat1 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat1;
    v_u_xlat1.x = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[2].xyz);
    v_u_xlat5.x = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat5.x = rsqrt(v_u_xlat5.x);
    v_u_xlat5 = v_u_xlat5.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD0 = v_u_xlat5;
    v_u_xlat16_3 = v_u_xlat5.y * v_u_xlat5.y;
    v_u_xlat16_3 = (v_u_xlat5.x * v_u_xlat5.x) + (-v_u_xlat16_3);
    v_u_xlat16_1 = v_u_xlat5.yzzx * v_u_xlat5.xyzz;
    v_u_xlat16_4.x = dot(_198_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBrArray, v_u_xlat16_1);
    v_u_xlat16_4.y = dot(_198_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBgArray, v_u_xlat16_1);
    v_u_xlat16_4.z = dot(_198_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBbArray, v_u_xlat16_1);
    v_vs_TEXCOORD2 = (_198_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHCArray.xyz * v_u_xlat16_3.xxx) + v_u_xlat16_4;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
    v_vs_SV_InstanceID0 = uint(v_gl_InstanceID);
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_gl_InstanceID = int(stage_input.v_gl_InstanceID);
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_SV_InstanceID0 = v_vs_SV_InstanceID0;
    return stage_output;
}
struct f_unity_Builtins2Array_Type
{
    float4 unity_SHArArray;
    float4 unity_SHAgArray;
    float4 unity_SHAbArray;
    float4 unity_SHBrArray;
    float4 unity_SHBgArray;
    float4 unity_SHBbArray;
    float4 unity_SHCArray;
};

cbuffer UnityInstancing_PerDraw2
{
    f_unity_Builtins2Array_Type _191_unity_Builtins2Array[2] : packoffset(c0);
};
















static float3 f_vs_TEXCOORD1;
static uint f_vs_SV_InstanceID0;
static float3 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
    nointerpolation uint f_vs_SV_InstanceID0 : TEXCOORD3;
    float4 recoveryPixelPosition : SV_Position;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat0;
static float3 f_u_xlat4;
static float4 f_u_xlat1;
static float f_u_xlat8;
static int f_u_xlati8;
static float3 f_u_xlat16_2;
static float f_u_xlat16_0;
static float3 f_u_xlat16_3;
static float f_u_xlat16_14;

void f_frag_main()
{
    float3 _19 = f_vs_TEXCOORD1 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = _19.x;
    f_u_xlat0.y = _19.y;
    f_u_xlat0.z = _19.z;
    f_u_xlat0.x = dot(f_u_xlat0.xyz, f_u_xlat0.xyz);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat4 = (-f_vs_TEXCOORD1) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat1.xyz);
    f_u_xlat0.x = (-f_u_xlat4.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat4.x;
    f_u_xlat0.x = (f_u_xlat0.x * RecoveryDirectionalShadowData().z) + RecoveryDirectionalShadowData().w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4 = f_vs_TEXCOORD1.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD1.xxx) + f_u_xlat4;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD1.zzz) + f_u_xlat4;
    f_u_xlat4 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat4.xy, f_u_xlat4.z);
    f_u_xlat4.x = RecoverySampleDirectionalShadow(txVec0);
    f_u_xlat8 = (-RecoveryDirectionalShadowData().x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + RecoveryDirectionalShadowData().x;
    f_u_xlati8 = int(f_vs_SV_InstanceID0) + unity_BaseInstanceID;
    f_u_xlati8 *= 7;
    f_u_xlat16_2.x = (-f_u_xlat4.x) + _191_unity_Builtins2Array[f_u_xlati8 / 7].unity_SHCArray.w;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat4.x;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _220 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _220.x;
    f_u_xlat0.y = _220.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    float3 _242 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat1.x = _242.x;
    f_u_xlat1.y = _242.y;
    f_u_xlat1.z = _242.z;
    float3 _256 = (f_u_xlat16_0.xxx * f_u_xlat1.xyz) + _MaskColor1.xyz;
    f_u_xlat0.x = _256.x;
    f_u_xlat0.y = _256.y;
    f_u_xlat0.w = _256.z;
    f_u_xlat16_2 *= f_u_xlat0.xyw;
    f_u_xlat1.x = f_vs_TEXCOORD0.x;
    f_u_xlat1.y = f_vs_TEXCOORD0.y;
    f_u_xlat1.z = f_vs_TEXCOORD0.z;
    f_u_xlat1.w = 1.0f;
    f_u_xlat16_3.x = dot(_191_unity_Builtins2Array[f_u_xlati8 / 7].unity_SHArArray, f_u_xlat1);
    f_u_xlat16_3.y = dot(_191_unity_Builtins2Array[f_u_xlati8 / 7].unity_SHAgArray, f_u_xlat1);
    f_u_xlat16_3.z = dot(_191_unity_Builtins2Array[f_u_xlati8 / 7].unity_SHAbArray, f_u_xlat1);
    f_u_xlat16_3 += f_vs_TEXCOORD2;
    f_u_xlat16_3 = max(f_u_xlat16_3, 0.0f.xxx);
    f_u_xlat16_3 = f_u_xlat0.xyw * f_u_xlat16_3;
    f_u_xlat16_14 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_14 = max(f_u_xlat16_14, 0.0f);
    float3 _325 = (f_u_xlat16_2 * f_u_xlat16_14.xxx) + f_u_xlat16_3;
    f_SV_Target0.x = _325.x;
    f_SV_Target0.y = _325.y;
    f_SV_Target0.z = _325.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_SV_InstanceID0 = stage_input.f_vs_SV_InstanceID0;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    RecoveryShadowScreenUV = stage_input.recoveryPixelPosition.xy / _ScreenParams.xy;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && defined(INSTANCING_ON) && defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_MatrixVP;
int unity_BaseInstanceID;
float4 unity_4LightPosY0;
float4 unity_4LightPosX0;
float4 unity_4LightPosZ0;
float4 unity_4LightAtten0;
float4 unity_LightColor[8];
float4 _LightColor0;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 _WorldSpaceLightPos0;
struct v_unity_Builtins0Array_Type
{
    float4 hlslcc_mtx4x4unity_ObjectToWorldArray[4];
    float4 hlslcc_mtx4x4unity_WorldToObjectArray[4];
};

struct v_unity_Builtins2Array_Type
{
    float4 unity_SHArArray;
    float4 unity_SHAgArray;
    float4 unity_SHAbArray;
    float4 unity_SHBrArray;
    float4 unity_SHBgArray;
    float4 unity_SHBbArray;
    float4 unity_SHCArray;
};

cbuffer UnityInstancing_PerDraw0
{
    v_unity_Builtins0Array_Type _40_unity_Builtins0Array[2] : packoffset(c0);
};

cbuffer UnityInstancing_PerDraw2
{
    v_unity_Builtins2Array_Type _210_unity_Builtins2Array[2] : packoffset(c0);
};







static float4 v_gl_Position;
static int v_gl_InstanceID;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;
static uint v_vs_SV_InstanceID0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    uint v_gl_InstanceID : SV_InstanceID;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
    nointerpolation uint v_vs_SV_InstanceID0 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static int v_u_xlati0;
static int v_u_xlati6;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float4 v_u_xlat3;
static float3 v_u_xlat6;
static float3 v_u_xlat16_4;
static float4 v_u_xlat16_2;
static float3 v_u_xlat16_5;
static float4 v_u_xlat5;
static float4 v_u_xlat0;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlati0 = v_gl_InstanceID + unity_BaseInstanceID;
    v_u_xlati6 = v_u_xlati0 << 3;
    v_u_xlati0 *= 7;
    v_u_xlat1 = v_in_POSITION0.yyyy * _40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[1];
    v_u_xlat1 = (_40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (_40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + _40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3];
    float4 _85 = v_u_xlat1;
    float3 _87 = (_40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3].xyz * v_in_POSITION0.www) + _85.xyz;
    v_u_xlat1.x = _87.x;
    v_u_xlat1.y = _87.y;
    v_u_xlat1.z = _87.z;
    v_u_xlat3 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat3 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat3;
    v_u_xlat3 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat3;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat3;
    v_u_xlat2.x = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[0].xyz);
    v_u_xlat2.y = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[1].xyz);
    v_u_xlat2.z = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati6 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[2].xyz);
    v_u_xlat6.x = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat6.x = rsqrt(v_u_xlat6.x);
    v_u_xlat6 = v_u_xlat6.xxx * v_u_xlat2.xyz;
    v_vs_TEXCOORD0 = v_u_xlat6;
    v_vs_TEXCOORD1 = v_u_xlat1.xyz;
    v_u_xlat16_4.x = v_u_xlat6.y * v_u_xlat6.y;
    v_u_xlat16_4.x = (v_u_xlat6.x * v_u_xlat6.x) + (-v_u_xlat16_4.x);
    v_u_xlat16_2 = v_u_xlat6.yzzx * v_u_xlat6.xyzz;
    v_u_xlat16_5.x = dot(_210_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBrArray, v_u_xlat16_2);
    v_u_xlat16_5.y = dot(_210_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBgArray, v_u_xlat16_2);
    v_u_xlat16_5.z = dot(_210_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBbArray, v_u_xlat16_2);
    v_u_xlat16_4 = (_210_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHCArray.xyz * v_u_xlat16_4.xxx) + v_u_xlat16_5;
    v_u_xlat2 = (-v_u_xlat1.yyyy) + unity_4LightPosY0;
    v_u_xlat3 = v_u_xlat6.yyyy * v_u_xlat2;
    v_u_xlat2 *= v_u_xlat2;
    v_u_xlat5 = (-v_u_xlat1.xxxx) + unity_4LightPosX0;
    v_u_xlat1 = (-v_u_xlat1.zzzz) + unity_4LightPosZ0;
    v_u_xlat3 = (v_u_xlat5 * v_u_xlat6.xxxx) + v_u_xlat3;
    v_u_xlat0 = (v_u_xlat1 * v_u_xlat6.zzzz) + v_u_xlat3;
    v_u_xlat2 = (v_u_xlat5 * v_u_xlat5) + v_u_xlat2;
    v_u_xlat1 = (v_u_xlat1 * v_u_xlat1) + v_u_xlat2;
    v_u_xlat1 = max(v_u_xlat1, 9.9999999747524270787835121154785e-07f.xxxx);
    v_u_xlat2 = rsqrt(v_u_xlat1);
    v_u_xlat1 = (v_u_xlat1 * unity_4LightAtten0) + 1.0f.xxxx;
    v_u_xlat1 = 1.0f.xxxx / v_u_xlat1;
    v_u_xlat0 *= v_u_xlat2;
    v_u_xlat0 = max(v_u_xlat0, 0.0f.xxxx);
    v_u_xlat0 = v_u_xlat1 * v_u_xlat0;
    float3 _328 = v_u_xlat0.yyy * unity_LightColor[1].xyz;
    v_u_xlat1.x = _328.x;
    v_u_xlat1.y = _328.y;
    v_u_xlat1.z = _328.z;
    float4 _341 = v_u_xlat1;
    float3 _343 = (unity_LightColor[0].xyz * v_u_xlat0.xxx) + _341.xyz;
    v_u_xlat1.x = _343.x;
    v_u_xlat1.y = _343.y;
    v_u_xlat1.z = _343.z;
    float4 _353 = v_u_xlat0;
    float3 _358 = (unity_LightColor[2].xyz * _353.zzz) + v_u_xlat1.xyz;
    v_u_xlat0.x = _358.x;
    v_u_xlat0.y = _358.y;
    v_u_xlat0.z = _358.z;
    float4 _368 = v_u_xlat0;
    float4 _371 = v_u_xlat0;
    float3 _373 = (unity_LightColor[3].xyz * _368.www) + _371.xyz;
    v_u_xlat0.x = _373.x;
    v_u_xlat0.y = _373.y;
    v_u_xlat0.z = _373.z;
    v_vs_TEXCOORD2 = v_u_xlat16_4 + v_u_xlat0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
    v_vs_SV_InstanceID0 = uint(v_gl_InstanceID);
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_gl_InstanceID = int(stage_input.v_gl_InstanceID);
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_SV_InstanceID0 = v_vs_SV_InstanceID0;
    return stage_output;
}
struct f_unity_Builtins2Array_Type
{
    float4 unity_SHArArray;
    float4 unity_SHAgArray;
    float4 unity_SHAbArray;
    float4 unity_SHBrArray;
    float4 unity_SHBgArray;
    float4 unity_SHBbArray;
    float4 unity_SHCArray;
};

cbuffer UnityInstancing_PerDraw2
{
    f_unity_Builtins2Array_Type _48_unity_Builtins2Array[2] : packoffset(c0);
};









static uint f_vs_SV_InstanceID0;
static float3 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
    nointerpolation uint f_vs_SV_InstanceID0 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static int f_u_xlati0;
static float4 f_u_xlat1;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_3;
static float3 f_u_xlat0;
static float f_u_xlat16_0;
static float3 f_u_xlat4;
static float f_u_xlat16_14;

void f_frag_main()
{
    f_u_xlati0 = int(f_vs_SV_InstanceID0) + unity_BaseInstanceID;
    f_u_xlati0 *= 7;
    f_u_xlat1.x = f_vs_TEXCOORD0.x;
    f_u_xlat1.y = f_vs_TEXCOORD0.y;
    f_u_xlat1.z = f_vs_TEXCOORD0.z;
    f_u_xlat1.w = 1.0f;
    f_u_xlat16_2.x = dot(_48_unity_Builtins2Array[f_u_xlati0 / 7].unity_SHArArray, f_u_xlat1);
    f_u_xlat16_2.y = dot(_48_unity_Builtins2Array[f_u_xlati0 / 7].unity_SHAgArray, f_u_xlat1);
    f_u_xlat16_2.z = dot(_48_unity_Builtins2Array[f_u_xlati0 / 7].unity_SHAbArray, f_u_xlat1);
    f_u_xlat16_3 = _LightColor0.xyz * _48_unity_Builtins2Array[f_u_xlati0 / 7].unity_SHCArray.www;
    f_u_xlat16_2 += f_vs_TEXCOORD2;
    f_u_xlat16_2 = max(f_u_xlat16_2, 0.0f.xxx);
    float2 _103 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _103.x;
    f_u_xlat0.y = _103.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat4 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat0 = (f_u_xlat16_0.xxx * f_u_xlat4) + _MaskColor1.xyz;
    f_u_xlat16_2 *= f_u_xlat0;
    f_u_xlat16_3 *= f_u_xlat0;
    f_u_xlat16_14 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_14 = max(f_u_xlat16_14, 0.0f);
    float3 _155 = (f_u_xlat16_3 * f_u_xlat16_14.xxx) + f_u_xlat16_2;
    f_SV_Target0.x = _155.x;
    f_SV_Target0.y = _155.y;
    f_SV_Target0.z = _155.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_SV_InstanceID0 = stage_input.f_vs_SV_InstanceID0;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && defined(INSTANCING_ON) && !defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
int unity_BaseInstanceID;
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _LightColor0;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 _WorldSpaceLightPos0;
#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
#else
sampler2D _ShadowMapTexture;
#endif
float4 _ScreenParams;
#include "RecoveryDirectionalShadows.cginc"

float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
struct v_unity_Builtins0Array_Type
{
    float4 hlslcc_mtx4x4unity_ObjectToWorldArray[4];
    float4 hlslcc_mtx4x4unity_WorldToObjectArray[4];
};

cbuffer UnityInstancing_PerDraw0
{
    v_unity_Builtins0Array_Type _36_unity_Builtins0Array[2] : packoffset(c0);
};


static float4 v_gl_Position;
static int v_gl_InstanceID;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static uint v_vs_SV_InstanceID0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    uint v_gl_InstanceID : SV_InstanceID;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    nointerpolation uint v_vs_SV_InstanceID0 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static int v_u_xlati0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat0;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlati0 = v_gl_InstanceID + unity_BaseInstanceID;
    v_u_xlati0 = v_u_xlati0 << 3;
    v_u_xlat1 = v_in_POSITION0.yyyy * _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[1];
    v_u_xlat1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3];
    v_vs_TEXCOORD1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3].xyz * v_in_POSITION0.www) + v_u_xlat1.xyz;
    v_u_xlat1 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat1;
    v_u_xlat1.x = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[2].xyz);
    v_u_xlat0 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat0 = rsqrt(v_u_xlat0);
    v_vs_TEXCOORD0 = v_u_xlat0.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
    v_vs_SV_InstanceID0 = uint(v_gl_InstanceID);
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_gl_InstanceID = int(stage_input.v_gl_InstanceID);
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_SV_InstanceID0 = v_vs_SV_InstanceID0;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float4 recoveryPixelPosition : SV_Position;
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
static float f_u_xlat16_0;
static float f_u_xlat16_11;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD1 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat3 = (-f_vs_TEXCOORD1) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat3.x = dot(f_u_xlat3, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat3.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat3.x;
    f_u_xlat0.x = (f_u_xlat0.x * RecoveryDirectionalShadowData().z) + RecoveryDirectionalShadowData().w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat3 = f_vs_TEXCOORD1.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD1.xxx) + f_u_xlat3;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD1.zzz) + f_u_xlat3;
    f_u_xlat3 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat3.xy, f_u_xlat3.z);
    f_u_xlat3.x = RecoverySampleDirectionalShadow(txVec0);
    f_u_xlat6 = (-RecoveryDirectionalShadowData().x) + 1.0f;
    f_u_xlat3.x = (f_u_xlat3.x * f_u_xlat6) + RecoveryDirectionalShadowData().x;
    f_u_xlat16_2.x = (-f_u_xlat3.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat3.x;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _186 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _186.x;
    f_u_xlat0.y = _186.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat3 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat0 = (f_u_xlat16_0.xxx * f_u_xlat3) + _MaskColor1.xyz;
    f_u_xlat16_2 *= f_u_xlat0;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _233 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _233.x;
    f_SV_Target0.y = _233.y;
    f_SV_Target0.z = _233.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    RecoveryShadowScreenUV = stage_input.recoveryPixelPosition.xy / _ScreenParams.xy;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(INSTANCING_ON) && defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
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
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 _WorldSpaceLightPos0;
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
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static float3 v_vs_TEXCOORD2;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat12;
static float v_u_xlat16_2;
static float4 v_u_xlat16_0;
static float3 v_u_xlat16_3;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_vs_TEXCOORD1 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat12 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat12 = rsqrt(v_u_xlat12);
    float4 _126 = v_u_xlat0;
    float3 _128 = v_u_xlat12.xxx * _126.xyz;
    v_u_xlat0.x = _128.x;
    v_u_xlat0.y = _128.y;
    v_u_xlat0.z = _128.z;
    v_vs_TEXCOORD0 = v_u_xlat0.xyz;
    v_u_xlat16_2 = v_u_xlat0.y * v_u_xlat0.y;
    v_u_xlat16_2 = (v_u_xlat0.x * v_u_xlat0.x) + (-v_u_xlat16_2);
    v_u_xlat16_0 = v_u_xlat0.yzzx * v_u_xlat0.xyzz;
    v_u_xlat16_3.x = dot(unity_SHBr, v_u_xlat16_0);
    v_u_xlat16_3.y = dot(unity_SHBg, v_u_xlat16_0);
    v_u_xlat16_3.z = dot(unity_SHBb, v_u_xlat16_0);
    v_vs_TEXCOORD2 = (unity_SHC.xyz * v_u_xlat16_2.xxx) + v_u_xlat16_3;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
    float4 recoveryPixelPosition : SV_Position;
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
static float f_u_xlat16_0;
static float3 f_u_xlat16_3;
static float f_u_xlat16_14;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD1 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat4 = (-f_vs_TEXCOORD1) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat4.x = dot(f_u_xlat4, f_u_xlat1.xyz);
    f_u_xlat0.x = (-f_u_xlat4.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat4.x;
    f_u_xlat0.x = (f_u_xlat0.x * RecoveryDirectionalShadowData().z) + RecoveryDirectionalShadowData().w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4 = f_vs_TEXCOORD1.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD1.xxx) + f_u_xlat4;
    f_u_xlat4 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD1.zzz) + f_u_xlat4;
    f_u_xlat4 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat4.xy, f_u_xlat4.z);
    f_u_xlat4.x = RecoverySampleDirectionalShadow(txVec0);
    f_u_xlat8 = (-RecoveryDirectionalShadowData().x) + 1.0f;
    f_u_xlat4.x = (f_u_xlat4.x * f_u_xlat8) + RecoveryDirectionalShadowData().x;
    f_u_xlat16_2.x = (-f_u_xlat4.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat4.x;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _188 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _188.x;
    f_u_xlat0.y = _188.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat4 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat0 = (f_u_xlat16_0.xxx * f_u_xlat4) + _MaskColor1.xyz;
    f_u_xlat16_2 *= f_u_xlat0;
    f_u_xlat1.x = f_vs_TEXCOORD0.x;
    f_u_xlat1.y = f_vs_TEXCOORD0.y;
    f_u_xlat1.z = f_vs_TEXCOORD0.z;
    f_u_xlat1.w = 1.0f;
    f_u_xlat16_3.x = dot(unity_SHAr, f_u_xlat1);
    f_u_xlat16_3.y = dot(unity_SHAg, f_u_xlat1);
    f_u_xlat16_3.z = dot(unity_SHAb, f_u_xlat1);
    f_u_xlat16_3 += f_vs_TEXCOORD2;
    f_u_xlat16_3 = max(f_u_xlat16_3, 0.0f.xxx);
    f_u_xlat16_3 = f_u_xlat0 * f_u_xlat16_3;
    f_u_xlat16_14 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_14 = max(f_u_xlat16_14, 0.0f);
    float3 _271 = (f_u_xlat16_2 * f_u_xlat16_14.xxx) + f_u_xlat16_3;
    f_SV_Target0.x = _271.x;
    f_SV_Target0.y = _271.y;
    f_SV_Target0.z = _271.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    RecoveryShadowScreenUV = stage_input.recoveryPixelPosition.xy / _ScreenParams.xy;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(INSTANCING_ON) && defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
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
float4 _WorldSpaceLightPos0;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 _LightColor0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static float3 v_vs_TEXCOORD1;
static float3 v_vs_TEXCOORD2;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
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
static float4 v_vs_TEXCOORD4;

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
    v_u_xlat1.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat21 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat21 = rsqrt(v_u_xlat21);
    float4 _131 = v_u_xlat1;
    float3 _133 = v_u_xlat21.xxx * _131.xyz;
    v_u_xlat1.x = _133.x;
    v_u_xlat1.y = _133.y;
    v_u_xlat1.z = _133.z;
    v_vs_TEXCOORD0 = v_u_xlat1.xyz;
    v_vs_TEXCOORD1 = v_u_xlat0.xyz;
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
    float3 _230 = v_u_xlat0.yyy * unity_LightColor[1].xyz;
    v_u_xlat2.x = _230.x;
    v_u_xlat2.y = _230.y;
    v_u_xlat2.z = _230.z;
    float4 _243 = v_u_xlat2;
    float3 _245 = (unity_LightColor[0].xyz * v_u_xlat0.xxx) + _243.xyz;
    v_u_xlat2.x = _245.x;
    v_u_xlat2.y = _245.y;
    v_u_xlat2.z = _245.z;
    float4 _255 = v_u_xlat0;
    float3 _260 = (unity_LightColor[2].xyz * _255.zzz) + v_u_xlat2.xyz;
    v_u_xlat0.x = _260.x;
    v_u_xlat0.y = _260.y;
    v_u_xlat0.z = _260.z;
    float4 _270 = v_u_xlat0;
    float4 _273 = v_u_xlat0;
    float3 _275 = (unity_LightColor[3].xyz * _270.www) + _273.xyz;
    v_u_xlat0.x = _275.x;
    v_u_xlat0.y = _275.y;
    v_u_xlat0.z = _275.z;
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
    v_vs_TEXCOORD2 = v_u_xlat0.xyz + v_u_xlat16_5;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float3 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float f_u_xlat16_0;
static float3 f_u_xlat1;
static float f_u_xlat16_1;
static float3 f_u_xlat4;
static float3 f_u_xlat16_3;
static float3 f_u_xlat16_2;

void f_frag_main()
{
    f_u_xlat16_0 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_0 = max(f_u_xlat16_0, 0.0f);
    float2 _32 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat1.x = _32.x;
    f_u_xlat1.y = _32.y;
    f_u_xlat16_1 = _Texture0.Sample(sampler_Texture0, f_u_xlat1.xy).x;
    f_u_xlat4 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat1 = (f_u_xlat16_1.xxx * f_u_xlat4) + _MaskColor1.xyz;
    f_u_xlat16_3 = f_u_xlat1 * _LightColor0.xyz;
    f_u_xlat16_2 = f_u_xlat1 * f_vs_TEXCOORD2;
    float3 _84 = (f_u_xlat16_3 * f_u_xlat16_0.xxx) + f_u_xlat16_2;
    f_SV_Target0.x = _84.x;
    f_SV_Target0.y = _84.y;
    f_SV_Target0.z = _84.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(INSTANCING_ON) && !defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _LightColor0;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 _WorldSpaceLightPos0;
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
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat6;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_vs_TEXCOORD1 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD0 = v_u_xlat6.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float4 recoveryPixelPosition : SV_Position;
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
static float f_u_xlat16_0;
static float f_u_xlat16_11;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD1 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat3 = (-f_vs_TEXCOORD1) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat3.x = dot(f_u_xlat3, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat3.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat3.x;
    f_u_xlat0.x = (f_u_xlat0.x * RecoveryDirectionalShadowData().z) + RecoveryDirectionalShadowData().w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat3 = f_vs_TEXCOORD1.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD1.xxx) + f_u_xlat3;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD1.zzz) + f_u_xlat3;
    f_u_xlat3 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat3.xy, f_u_xlat3.z);
    f_u_xlat3.x = RecoverySampleDirectionalShadow(txVec0);
    f_u_xlat6 = (-RecoveryDirectionalShadowData().x) + 1.0f;
    f_u_xlat3.x = (f_u_xlat3.x * f_u_xlat6) + RecoveryDirectionalShadowData().x;
    f_u_xlat16_2.x = (-f_u_xlat3.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat3.x;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _186 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _186.x;
    f_u_xlat0.y = _186.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat3 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat0 = (f_u_xlat16_0.xxx * f_u_xlat3) + _MaskColor1.xyz;
    f_u_xlat16_2 *= f_u_xlat0;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _233 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _233.x;
    f_SV_Target0.y = _233.y;
    f_SV_Target0.z = _233.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    RecoveryShadowScreenUV = stage_input.recoveryPixelPosition.xy / _ScreenParams.xy;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && defined(INSTANCING_ON) && defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_MatrixVP;
int unity_BaseInstanceID;
float4 _LightColor0;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 _WorldSpaceLightPos0;
struct v_unity_Builtins0Array_Type
{
    float4 hlslcc_mtx4x4unity_ObjectToWorldArray[4];
    float4 hlslcc_mtx4x4unity_WorldToObjectArray[4];
};

struct v_unity_Builtins2Array_Type
{
    float4 unity_SHArArray;
    float4 unity_SHAgArray;
    float4 unity_SHAbArray;
    float4 unity_SHBrArray;
    float4 unity_SHBgArray;
    float4 unity_SHBbArray;
    float4 unity_SHCArray;
};

cbuffer UnityInstancing_PerDraw0
{
    v_unity_Builtins0Array_Type _40_unity_Builtins0Array[2] : packoffset(c0);
};

cbuffer UnityInstancing_PerDraw2
{
    v_unity_Builtins2Array_Type _198_unity_Builtins2Array[2] : packoffset(c0);
};


static float4 v_gl_Position;
static int v_gl_InstanceID;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static float3 v_vs_TEXCOORD2;
static uint v_vs_SV_InstanceID0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    uint v_gl_InstanceID : SV_InstanceID;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
    nointerpolation uint v_vs_SV_InstanceID0 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static int v_u_xlati0;
static int v_u_xlati5;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float3 v_u_xlat5;
static float v_u_xlat16_3;
static float4 v_u_xlat16_1;
static float3 v_u_xlat16_4;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlati0 = v_gl_InstanceID + unity_BaseInstanceID;
    v_u_xlati5 = v_u_xlati0 << 3;
    v_u_xlati0 *= 7;
    v_u_xlat1 = v_in_POSITION0.yyyy * _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[1];
    v_u_xlat1 = (_40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (_40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3];
    v_vs_TEXCOORD1 = (_40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3].xyz * v_in_POSITION0.www) + v_u_xlat1.xyz;
    v_u_xlat1 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat1;
    v_u_xlat1.x = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[2].xyz);
    v_u_xlat5.x = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat5.x = rsqrt(v_u_xlat5.x);
    v_u_xlat5 = v_u_xlat5.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD0 = v_u_xlat5;
    v_u_xlat16_3 = v_u_xlat5.y * v_u_xlat5.y;
    v_u_xlat16_3 = (v_u_xlat5.x * v_u_xlat5.x) + (-v_u_xlat16_3);
    v_u_xlat16_1 = v_u_xlat5.yzzx * v_u_xlat5.xyzz;
    v_u_xlat16_4.x = dot(_198_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBrArray, v_u_xlat16_1);
    v_u_xlat16_4.y = dot(_198_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBgArray, v_u_xlat16_1);
    v_u_xlat16_4.z = dot(_198_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBbArray, v_u_xlat16_1);
    v_vs_TEXCOORD2 = (_198_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHCArray.xyz * v_u_xlat16_3.xxx) + v_u_xlat16_4;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
    v_vs_SV_InstanceID0 = uint(v_gl_InstanceID);
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_gl_InstanceID = int(stage_input.v_gl_InstanceID);
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_SV_InstanceID0 = v_vs_SV_InstanceID0;
    return stage_output;
}
struct f_unity_Builtins2Array_Type
{
    float4 unity_SHArArray;
    float4 unity_SHAgArray;
    float4 unity_SHAbArray;
    float4 unity_SHBrArray;
    float4 unity_SHBgArray;
    float4 unity_SHBbArray;
    float4 unity_SHCArray;
};

cbuffer UnityInstancing_PerDraw2
{
    f_unity_Builtins2Array_Type _48_unity_Builtins2Array[2] : packoffset(c0);
};









static uint f_vs_SV_InstanceID0;
static float3 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD2;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
    nointerpolation uint f_vs_SV_InstanceID0 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static int f_u_xlati0;
static float4 f_u_xlat1;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_3;
static float3 f_u_xlat0;
static float f_u_xlat16_0;
static float3 f_u_xlat4;
static float f_u_xlat16_14;

void f_frag_main()
{
    f_u_xlati0 = int(f_vs_SV_InstanceID0) + unity_BaseInstanceID;
    f_u_xlati0 *= 7;
    f_u_xlat1.x = f_vs_TEXCOORD0.x;
    f_u_xlat1.y = f_vs_TEXCOORD0.y;
    f_u_xlat1.z = f_vs_TEXCOORD0.z;
    f_u_xlat1.w = 1.0f;
    f_u_xlat16_2.x = dot(_48_unity_Builtins2Array[f_u_xlati0 / 7].unity_SHArArray, f_u_xlat1);
    f_u_xlat16_2.y = dot(_48_unity_Builtins2Array[f_u_xlati0 / 7].unity_SHAgArray, f_u_xlat1);
    f_u_xlat16_2.z = dot(_48_unity_Builtins2Array[f_u_xlati0 / 7].unity_SHAbArray, f_u_xlat1);
    f_u_xlat16_3 = _LightColor0.xyz * _48_unity_Builtins2Array[f_u_xlati0 / 7].unity_SHCArray.www;
    f_u_xlat16_2 += f_vs_TEXCOORD2;
    f_u_xlat16_2 = max(f_u_xlat16_2, 0.0f.xxx);
    float2 _103 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _103.x;
    f_u_xlat0.y = _103.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat4 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat0 = (f_u_xlat16_0.xxx * f_u_xlat4) + _MaskColor1.xyz;
    f_u_xlat16_2 *= f_u_xlat0;
    f_u_xlat16_3 *= f_u_xlat0;
    f_u_xlat16_14 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_14 = max(f_u_xlat16_14, 0.0f);
    float3 _155 = (f_u_xlat16_3 * f_u_xlat16_14.xxx) + f_u_xlat16_2;
    f_SV_Target0.x = _155.x;
    f_SV_Target0.y = _155.y;
    f_SV_Target0.z = _155.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_SV_InstanceID0 = stage_input.f_vs_SV_InstanceID0;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && defined(INSTANCING_ON) && !defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
int unity_BaseInstanceID;
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _LightColor0;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 _WorldSpaceLightPos0;
#if defined(UNITY_NO_SCREENSPACE_SHADOWS)
Texture2D<float4> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;
#else
sampler2D _ShadowMapTexture;
#endif
float4 _ScreenParams;
#include "RecoveryDirectionalShadows.cginc"

float4 RecoveryColumn_unity_WorldToShadow(int index) { return transpose(unity_WorldToShadow[index / 4])[index % 4]; }
struct v_unity_Builtins0Array_Type
{
    float4 hlslcc_mtx4x4unity_ObjectToWorldArray[4];
    float4 hlslcc_mtx4x4unity_WorldToObjectArray[4];
};

cbuffer UnityInstancing_PerDraw0
{
    v_unity_Builtins0Array_Type _36_unity_Builtins0Array[2] : packoffset(c0);
};


static float4 v_gl_Position;
static int v_gl_InstanceID;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static uint v_vs_SV_InstanceID0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    uint v_gl_InstanceID : SV_InstanceID;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    nointerpolation uint v_vs_SV_InstanceID0 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static int v_u_xlati0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat0;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlati0 = v_gl_InstanceID + unity_BaseInstanceID;
    v_u_xlati0 = v_u_xlati0 << 3;
    v_u_xlat1 = v_in_POSITION0.yyyy * _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[1];
    v_u_xlat1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3];
    v_vs_TEXCOORD1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3].xyz * v_in_POSITION0.www) + v_u_xlat1.xyz;
    v_u_xlat1 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat1;
    v_u_xlat1.x = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[2].xyz);
    v_u_xlat0 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat0 = rsqrt(v_u_xlat0);
    v_vs_TEXCOORD0 = v_u_xlat0.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
    v_vs_SV_InstanceID0 = uint(v_gl_InstanceID);
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_gl_InstanceID = int(stage_input.v_gl_InstanceID);
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_SV_InstanceID0 = v_vs_SV_InstanceID0;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float4 recoveryPixelPosition : SV_Position;
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
static float f_u_xlat16_0;
static float f_u_xlat16_11;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD1 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat3 = (-f_vs_TEXCOORD1) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat3.x = dot(f_u_xlat3, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat3.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat3.x;
    f_u_xlat0.x = (f_u_xlat0.x * RecoveryDirectionalShadowData().z) + RecoveryDirectionalShadowData().w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat3 = f_vs_TEXCOORD1.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD1.xxx) + f_u_xlat3;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD1.zzz) + f_u_xlat3;
    f_u_xlat3 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat3.xy, f_u_xlat3.z);
    f_u_xlat3.x = RecoverySampleDirectionalShadow(txVec0);
    f_u_xlat6 = (-RecoveryDirectionalShadowData().x) + 1.0f;
    f_u_xlat3.x = (f_u_xlat3.x * f_u_xlat6) + RecoveryDirectionalShadowData().x;
    f_u_xlat16_2.x = (-f_u_xlat3.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat3.x;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _186 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _186.x;
    f_u_xlat0.y = _186.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat3 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat0 = (f_u_xlat16_0.xxx * f_u_xlat3) + _MaskColor1.xyz;
    f_u_xlat16_2 *= f_u_xlat0;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _233 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _233.x;
    f_SV_Target0.y = _233.y;
    f_SV_Target0.z = _233.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    RecoveryShadowScreenUV = stage_input.recoveryPixelPosition.xy / _ScreenParams.xy;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && defined(INSTANCING_ON) && !defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_MatrixVP;
int unity_BaseInstanceID;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 _LightColor0;
float4 _WorldSpaceLightPos0;
struct v_unity_Builtins0Array_Type
{
    float4 hlslcc_mtx4x4unity_ObjectToWorldArray[4];
    float4 hlslcc_mtx4x4unity_WorldToObjectArray[4];
};

cbuffer UnityInstancing_PerDraw0
{
    v_unity_Builtins0Array_Type _36_unity_Builtins0Array[2] : packoffset(c0);
};


static float4 v_gl_Position;
static int v_gl_InstanceID;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static uint v_vs_SV_InstanceID0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    uint v_gl_InstanceID : SV_InstanceID;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    nointerpolation uint v_vs_SV_InstanceID0 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static int v_u_xlati0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat0;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlati0 = v_gl_InstanceID + unity_BaseInstanceID;
    v_u_xlati0 = v_u_xlati0 << 3;
    v_u_xlat1 = v_in_POSITION0.yyyy * _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[1];
    v_u_xlat1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3];
    v_vs_TEXCOORD1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3].xyz * v_in_POSITION0.www) + v_u_xlat1.xyz;
    v_u_xlat1 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat1;
    v_u_xlat1.x = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[2].xyz);
    v_u_xlat0 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat0 = rsqrt(v_u_xlat0);
    v_vs_TEXCOORD0 = v_u_xlat0.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
    v_vs_SV_InstanceID0 = uint(v_gl_InstanceID);
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_gl_InstanceID = int(stage_input.v_gl_InstanceID);
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_SV_InstanceID0 = v_vs_SV_InstanceID0;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float f_u_xlat16_0;
static float3 f_u_xlat2;
static float3 f_u_xlat16_1;
static float f_u_xlat16_7;

void f_frag_main()
{
    float2 _19 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _19.x;
    f_u_xlat0.y = _19.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat2 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat0 = (f_u_xlat16_0.xxx * f_u_xlat2) + _MaskColor1.xyz;
    f_u_xlat16_1 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_7 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_7 = max(f_u_xlat16_7, 0.0f);
    float3 _77 = f_u_xlat16_7.xxx * f_u_xlat16_1;
    f_SV_Target0.x = _77.x;
    f_SV_Target0.y = _77.y;
    f_SV_Target0.z = _77.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(INSTANCING_ON) && defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 _WorldSpaceLightPos0;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 _LightColor0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static float3 v_vs_TEXCOORD2;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD2 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat12;
static float3 v_u_xlat16_2;
static float4 v_u_xlat16_1;
static float3 v_u_xlat16_3;
static float4 v_vs_TEXCOORD4;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_vs_TEXCOORD1 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat12 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat12 = rsqrt(v_u_xlat12);
    float4 _126 = v_u_xlat0;
    float3 _128 = v_u_xlat12.xxx * _126.xyz;
    v_u_xlat0.x = _128.x;
    v_u_xlat0.y = _128.y;
    v_u_xlat0.z = _128.z;
    v_vs_TEXCOORD0 = v_u_xlat0.xyz;
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
    v_vs_TEXCOORD2 = max(v_u_xlat16_2, 0.0f.xxx);
    v_vs_TEXCOORD4 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float3 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float f_u_xlat16_0;
static float3 f_u_xlat1;
static float f_u_xlat16_1;
static float3 f_u_xlat4;
static float3 f_u_xlat16_3;
static float3 f_u_xlat16_2;

void f_frag_main()
{
    f_u_xlat16_0 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_0 = max(f_u_xlat16_0, 0.0f);
    float2 _32 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat1.x = _32.x;
    f_u_xlat1.y = _32.y;
    f_u_xlat16_1 = _Texture0.Sample(sampler_Texture0, f_u_xlat1.xy).x;
    f_u_xlat4 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat1 = (f_u_xlat16_1.xxx * f_u_xlat4) + _MaskColor1.xyz;
    f_u_xlat16_3 = f_u_xlat1 * _LightColor0.xyz;
    f_u_xlat16_2 = f_u_xlat1 * f_vs_TEXCOORD2;
    float3 _84 = (f_u_xlat16_3 * f_u_xlat16_0.xxx) + f_u_xlat16_2;
    f_SV_Target0.x = _84.x;
    f_SV_Target0.y = _84.y;
    f_SV_Target0.z = _84.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(INSTANCING_ON) && !defined(LIGHTPROBE_SH) && defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4x4 unity_MatrixV;
float4x4 unity_WorldToShadow[4];
float4 unity_ShadowFadeCenterAndType;
float3 _WorldSpaceCameraPos;
float4 _LightShadowData;
Texture2D<float4> hlslcc_zcmp_ShadowMapTexture;
SamplerComparisonState sampler_hlslcc_zcmp_ShadowMapTexture;
float4 _LightColor0;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 _WorldSpaceLightPos0;
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
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat6;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_vs_TEXCOORD1 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD0 = v_u_xlat6.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float4 recoveryPixelPosition : SV_Position;
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
static float f_u_xlat16_0;
static float f_u_xlat16_11;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD1 + (-unity_ShadowFadeCenterAndType.xyz);
    f_u_xlat0.x = dot(f_u_xlat0, f_u_xlat0);
    f_u_xlat0.x = sqrt(f_u_xlat0.x);
    f_u_xlat3 = (-f_vs_TEXCOORD1) + _WorldSpaceCameraPos;
    f_u_xlat1.x = transpose(unity_MatrixV)[0].z;
    f_u_xlat1.y = transpose(unity_MatrixV)[1].z;
    f_u_xlat1.z = transpose(unity_MatrixV)[2].z;
    f_u_xlat3.x = dot(f_u_xlat3, f_u_xlat1);
    f_u_xlat0.x = (-f_u_xlat3.x) + f_u_xlat0.x;
    f_u_xlat0.x = (unity_ShadowFadeCenterAndType.w * f_u_xlat0.x) + f_u_xlat3.x;
    f_u_xlat0.x = (f_u_xlat0.x * RecoveryDirectionalShadowData().z) + RecoveryDirectionalShadowData().w;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat3 = f_vs_TEXCOORD1.yyy * RecoveryColumn_unity_WorldToShadow(1).xyz;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(0).xyz * f_vs_TEXCOORD1.xxx) + f_u_xlat3;
    f_u_xlat3 = (RecoveryColumn_unity_WorldToShadow(2).xyz * f_vs_TEXCOORD1.zzz) + f_u_xlat3;
    f_u_xlat3 += RecoveryColumn_unity_WorldToShadow(3).xyz;
    float3 txVec0 = float3(f_u_xlat3.xy, f_u_xlat3.z);
    f_u_xlat3.x = RecoverySampleDirectionalShadow(txVec0);
    f_u_xlat6 = (-RecoveryDirectionalShadowData().x) + 1.0f;
    f_u_xlat3.x = (f_u_xlat3.x * f_u_xlat6) + RecoveryDirectionalShadowData().x;
    f_u_xlat16_2.x = (-f_u_xlat3.x) + 1.0f;
    f_u_xlat16_2.x = (f_u_xlat0.x * f_u_xlat16_2.x) + f_u_xlat3.x;
    f_u_xlat16_2 = f_u_xlat16_2.xxx * _LightColor0.xyz;
    float2 _186 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _186.x;
    f_u_xlat0.y = _186.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat3 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat0 = (f_u_xlat16_0.xxx * f_u_xlat3) + _MaskColor1.xyz;
    f_u_xlat16_2 *= f_u_xlat0;
    f_u_xlat16_11 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_11 = max(f_u_xlat16_11, 0.0f);
    float3 _233 = f_u_xlat16_11.xxx * f_u_xlat16_2;
    f_SV_Target0.x = _233.x;
    f_SV_Target0.y = _233.y;
    f_SV_Target0.z = _233.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    RecoveryShadowScreenUV = stage_input.recoveryPixelPosition.xy / _ScreenParams.xy;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(INSTANCING_ON) && !defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 _LightColor0;
float4 _WorldSpaceLightPos0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
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
    v_vs_TEXCOORD1 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD0 = v_u_xlat6.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float f_u_xlat16_0;
static float3 f_u_xlat2;
static float3 f_u_xlat16_1;
static float f_u_xlat16_7;

void f_frag_main()
{
    float2 _19 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _19.x;
    f_u_xlat0.y = _19.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat2 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat0 = (f_u_xlat16_0.xxx * f_u_xlat2) + _MaskColor1.xyz;
    f_u_xlat16_1 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_7 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_7 = max(f_u_xlat16_7, 0.0f);
    float3 _77 = f_u_xlat16_7.xxx * f_u_xlat16_1;
    f_SV_Target0.x = _77.x;
    f_SV_Target0.y = _77.y;
    f_SV_Target0.z = _77.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && defined(INSTANCING_ON) && !defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_MatrixVP;
int unity_BaseInstanceID;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 _LightColor0;
float4 _WorldSpaceLightPos0;
struct v_unity_Builtins0Array_Type
{
    float4 hlslcc_mtx4x4unity_ObjectToWorldArray[4];
    float4 hlslcc_mtx4x4unity_WorldToObjectArray[4];
};

cbuffer UnityInstancing_PerDraw0
{
    v_unity_Builtins0Array_Type _36_unity_Builtins0Array[2] : packoffset(c0);
};


static float4 v_gl_Position;
static int v_gl_InstanceID;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static uint v_vs_SV_InstanceID0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    uint v_gl_InstanceID : SV_InstanceID;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    nointerpolation uint v_vs_SV_InstanceID0 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static int v_u_xlati0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat0;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_TEXCOORD5;

void v_vert_main()
{
    v_u_xlati0 = v_gl_InstanceID + unity_BaseInstanceID;
    v_u_xlati0 = v_u_xlati0 << 3;
    v_u_xlat1 = v_in_POSITION0.yyyy * _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[1];
    v_u_xlat1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3];
    v_vs_TEXCOORD1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3].xyz * v_in_POSITION0.www) + v_u_xlat1.xyz;
    v_u_xlat1 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat1;
    v_u_xlat1.x = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[2].xyz);
    v_u_xlat0 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat0 = rsqrt(v_u_xlat0);
    v_vs_TEXCOORD0 = v_u_xlat0.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
    v_vs_TEXCOORD5 = 0.0f.xxxx;
    v_vs_SV_InstanceID0 = uint(v_gl_InstanceID);
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_gl_InstanceID = int(stage_input.v_gl_InstanceID);
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_SV_InstanceID0 = v_vs_SV_InstanceID0;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float f_u_xlat16_0;
static float3 f_u_xlat2;
static float3 f_u_xlat16_1;
static float f_u_xlat16_7;

void f_frag_main()
{
    float2 _19 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _19.x;
    f_u_xlat0.y = _19.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat2 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat0 = (f_u_xlat16_0.xxx * f_u_xlat2) + _MaskColor1.xyz;
    f_u_xlat16_1 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_7 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_7 = max(f_u_xlat16_7, 0.0f);
    float3 _77 = f_u_xlat16_7.xxx * f_u_xlat16_1;
    f_SV_Target0.x = _77.x;
    f_SV_Target0.y = _77.y;
    f_SV_Target0.z = _77.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(DIRECTIONAL) && !defined(INSTANCING_ON) && !defined(LIGHTPROBE_SH) && !defined(SHADOWS_SCREEN) && !defined(VERTEXLIGHT_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
float4 _LightColor0;
float4 _WorldSpaceLightPos0;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
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
    v_vs_TEXCOORD1 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD0 = v_u_xlat6.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD4 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float3 f_u_xlat0;
static float f_u_xlat16_0;
static float3 f_u_xlat2;
static float3 f_u_xlat16_1;
static float f_u_xlat16_7;

void f_frag_main()
{
    float2 _19 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _19.x;
    f_u_xlat0.y = _19.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat2 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat0 = (f_u_xlat16_0.xxx * f_u_xlat2) + _MaskColor1.xyz;
    f_u_xlat16_1 = f_u_xlat0 * _LightColor0.xyz;
    f_u_xlat16_7 = dot(f_vs_TEXCOORD0, _WorldSpaceLightPos0.xyz);
    f_u_xlat16_7 = max(f_u_xlat16_7, 0.0f);
    float3 _77 = f_u_xlat16_7.xxx * f_u_xlat16_1;
    f_SV_Target0.x = _77.x;
    f_SV_Target0.y = _77.y;
    f_SV_Target0.z = _77.z;
    f_SV_Target0.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
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
Name "DEFERRED"
Tags { "LightMode"="Deferred" "RenderType"="Opaque" }
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
#pragma multi_compile _ INSTANCING_ON
#pragma multi_compile _ LIGHTPROBE_SH
#pragma multi_compile _ UNITY_HDR_ON
#if defined(INSTANCING_ON) && defined(LIGHTPROBE_SH) && defined(UNITY_HDR_ON)
float4x4 unity_MatrixVP;
int unity_BaseInstanceID;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
struct v_unity_Builtins0Array_Type
{
    float4 hlslcc_mtx4x4unity_ObjectToWorldArray[4];
    float4 hlslcc_mtx4x4unity_WorldToObjectArray[4];
};

struct v_unity_Builtins2Array_Type
{
    float4 unity_SHArArray;
    float4 unity_SHAgArray;
    float4 unity_SHAbArray;
    float4 unity_SHBrArray;
    float4 unity_SHBgArray;
    float4 unity_SHBbArray;
    float4 unity_SHCArray;
};

cbuffer UnityInstancing_PerDraw0
{
    v_unity_Builtins0Array_Type _40_unity_Builtins0Array[2] : packoffset(c0);
};

cbuffer UnityInstancing_PerDraw2
{
    v_unity_Builtins2Array_Type _201_unity_Builtins2Array[2] : packoffset(c0);
};


static float4 v_gl_Position;
static int v_gl_InstanceID;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static float3 v_vs_TEXCOORD3;
static uint v_vs_SV_InstanceID0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    uint v_gl_InstanceID : SV_InstanceID;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD3 : TEXCOORD2;
    nointerpolation uint v_vs_SV_InstanceID0 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static int v_u_xlati0;
static int v_u_xlati5;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float3 v_u_xlat5;
static float4 v_vs_TEXCOORD2;
static float v_u_xlat16_3;
static float4 v_u_xlat16_1;
static float3 v_u_xlat16_4;

void v_vert_main()
{
    v_u_xlati0 = v_gl_InstanceID + unity_BaseInstanceID;
    v_u_xlati5 = v_u_xlati0 << 3;
    v_u_xlati0 *= 7;
    v_u_xlat1 = v_in_POSITION0.yyyy * _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[1];
    v_u_xlat1 = (_40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (_40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3];
    v_vs_TEXCOORD1 = (_40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3].xyz * v_in_POSITION0.www) + v_u_xlat1.xyz;
    v_u_xlat1 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat1;
    v_u_xlat1.x = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[2].xyz);
    v_u_xlat5.x = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat5.x = rsqrt(v_u_xlat5.x);
    v_u_xlat5 = v_u_xlat5.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD0 = v_u_xlat5;
    v_vs_TEXCOORD2 = 0.0f.xxxx;
    v_u_xlat16_3 = v_u_xlat5.y * v_u_xlat5.y;
    v_u_xlat16_3 = (v_u_xlat5.x * v_u_xlat5.x) + (-v_u_xlat16_3);
    v_u_xlat16_1 = v_u_xlat5.yzzx * v_u_xlat5.xyzz;
    v_u_xlat16_4.x = dot(_201_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBrArray, v_u_xlat16_1);
    v_u_xlat16_4.y = dot(_201_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBgArray, v_u_xlat16_1);
    v_u_xlat16_4.z = dot(_201_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBbArray, v_u_xlat16_1);
    v_vs_TEXCOORD3 = (_201_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHCArray.xyz * v_u_xlat16_3.xxx) + v_u_xlat16_4;
    v_vs_SV_InstanceID0 = uint(v_gl_InstanceID);
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_gl_InstanceID = int(stage_input.v_gl_InstanceID);
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_SV_InstanceID0 = v_vs_SV_InstanceID0;
    return stage_output;
}
struct f_unity_Builtins2Array_Type
{
    float4 unity_SHArArray;
    float4 unity_SHAgArray;
    float4 unity_SHAbArray;
    float4 unity_SHBrArray;
    float4 unity_SHBgArray;
    float4 unity_SHBbArray;
    float4 unity_SHCArray;
};

cbuffer UnityInstancing_PerDraw2
{
    f_unity_Builtins2Array_Type _117_unity_Builtins2Array[2] : packoffset(c0);
};







static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float4 f_SV_Target1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target2;
static uint f_vs_SV_InstanceID0;
static float3 f_vs_TEXCOORD3;
static float4 f_SV_Target3;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD3 : TEXCOORD2;
    nointerpolation uint f_vs_SV_InstanceID0 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
    float4 f_SV_Target1 : SV_Target1;
    float4 f_SV_Target2 : SV_Target2;
    float4 f_SV_Target3 : SV_Target3;
};

static float3 f_u_xlat0;
static float f_u_xlat16_0;
static float3 f_u_xlat3;
static float4 f_u_xlat1;
static int f_u_xlati9;
static float3 f_u_xlat16_2;

void f_frag_main()
{
    float2 _19 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _19.x;
    f_u_xlat0.y = _19.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat3 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat0 = (f_u_xlat16_0.xxx * f_u_xlat3) + _MaskColor1.xyz;
    f_SV_Target0.x = f_u_xlat0.x;
    f_SV_Target0.y = f_u_xlat0.y;
    f_SV_Target0.z = f_u_xlat0.z;
    f_SV_Target0.w = 1.0f;
    f_SV_Target1 = 0.0f.xxxx;
    float3 _80 = (f_vs_TEXCOORD0 * 0.5f.xxx) + 0.5f.xxx;
    f_u_xlat1.x = _80.x;
    f_u_xlat1.y = _80.y;
    f_u_xlat1.z = _80.z;
    f_u_xlat1.w = 1.0f;
    f_SV_Target2 = f_u_xlat1;
    f_u_xlati9 = int(f_vs_SV_InstanceID0) + unity_BaseInstanceID;
    f_u_xlati9 *= 7;
    f_u_xlat1.x = f_vs_TEXCOORD0.x;
    f_u_xlat1.y = f_vs_TEXCOORD0.y;
    f_u_xlat1.z = f_vs_TEXCOORD0.z;
    f_u_xlat1.w = 1.0f;
    f_u_xlat16_2.x = dot(_117_unity_Builtins2Array[f_u_xlati9 / 7].unity_SHArArray, f_u_xlat1);
    f_u_xlat16_2.y = dot(_117_unity_Builtins2Array[f_u_xlati9 / 7].unity_SHAgArray, f_u_xlat1);
    f_u_xlat16_2.z = dot(_117_unity_Builtins2Array[f_u_xlati9 / 7].unity_SHAbArray, f_u_xlat1);
    f_u_xlat16_2 += f_vs_TEXCOORD3;
    f_u_xlat16_2 = max(f_u_xlat16_2, 0.0f.xxx);
    float3 _153 = f_u_xlat0 * f_u_xlat16_2;
    f_SV_Target3.x = _153.x;
    f_SV_Target3.y = _153.y;
    f_SV_Target3.z = _153.z;
    f_SV_Target3.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_SV_InstanceID0 = stage_input.f_vs_SV_InstanceID0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    stage_output.f_SV_Target1 = f_SV_Target1;
    stage_output.f_SV_Target2 = f_SV_Target2;
    stage_output.f_SV_Target3 = f_SV_Target3;
    return stage_output;
}
#elif !defined(INSTANCING_ON) && defined(LIGHTPROBE_SH) && defined(UNITY_HDR_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static float3 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD3 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat12;
static float4 v_vs_TEXCOORD2;
static float3 v_u_xlat16_2;
static float4 v_u_xlat16_1;
static float3 v_u_xlat16_3;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_vs_TEXCOORD1 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat12 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat12 = rsqrt(v_u_xlat12);
    float4 _126 = v_u_xlat0;
    float3 _128 = v_u_xlat12.xxx * _126.xyz;
    v_u_xlat0.x = _128.x;
    v_u_xlat0.y = _128.y;
    v_u_xlat0.z = _128.z;
    v_vs_TEXCOORD0 = v_u_xlat0.xyz;
    v_vs_TEXCOORD2 = 0.0f.xxxx;
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
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float4 f_SV_Target3;
static float3 f_vs_TEXCOORD3;
static float4 f_SV_Target1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target2;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD3 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
    float4 f_SV_Target1 : SV_Target1;
    float4 f_SV_Target2 : SV_Target2;
    float4 f_SV_Target3 : SV_Target3;
};

static float4 f_u_xlat0;
static float f_u_xlat16_0;
static float3 f_u_xlat1;

void f_frag_main()
{
    float2 _20 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _20.x;
    f_u_xlat0.y = _20.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat1 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    float3 _56 = (f_u_xlat16_0.xxx * f_u_xlat1) + _MaskColor1.xyz;
    f_u_xlat0.x = _56.x;
    f_u_xlat0.y = _56.y;
    f_u_xlat0.z = _56.z;
    f_SV_Target0.x = f_u_xlat0.xyz.x;
    f_SV_Target0.y = f_u_xlat0.xyz.y;
    f_SV_Target0.z = f_u_xlat0.xyz.z;
    float3 _80 = f_u_xlat0.xyz * f_vs_TEXCOORD3;
    f_SV_Target3.x = _80.x;
    f_SV_Target3.y = _80.y;
    f_SV_Target3.z = _80.z;
    f_SV_Target0.w = 1.0f;
    f_SV_Target1 = 0.0f.xxxx;
    float3 _98 = (f_vs_TEXCOORD0 * 0.5f.xxx) + 0.5f.xxx;
    f_u_xlat0.x = _98.x;
    f_u_xlat0.y = _98.y;
    f_u_xlat0.z = _98.z;
    f_u_xlat0.w = 1.0f;
    f_SV_Target2 = f_u_xlat0;
    f_SV_Target3.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    stage_output.f_SV_Target3 = f_SV_Target3;
    stage_output.f_SV_Target1 = f_SV_Target1;
    stage_output.f_SV_Target2 = f_SV_Target2;
    return stage_output;
}
#elif defined(INSTANCING_ON) && defined(LIGHTPROBE_SH) && !defined(UNITY_HDR_ON)
float4x4 unity_MatrixVP;
int unity_BaseInstanceID;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
struct v_unity_Builtins0Array_Type
{
    float4 hlslcc_mtx4x4unity_ObjectToWorldArray[4];
    float4 hlslcc_mtx4x4unity_WorldToObjectArray[4];
};

struct v_unity_Builtins2Array_Type
{
    float4 unity_SHArArray;
    float4 unity_SHAgArray;
    float4 unity_SHAbArray;
    float4 unity_SHBrArray;
    float4 unity_SHBgArray;
    float4 unity_SHBbArray;
    float4 unity_SHCArray;
};

cbuffer UnityInstancing_PerDraw0
{
    v_unity_Builtins0Array_Type _40_unity_Builtins0Array[2] : packoffset(c0);
};

cbuffer UnityInstancing_PerDraw2
{
    v_unity_Builtins2Array_Type _201_unity_Builtins2Array[2] : packoffset(c0);
};


static float4 v_gl_Position;
static int v_gl_InstanceID;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static float3 v_vs_TEXCOORD3;
static uint v_vs_SV_InstanceID0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    uint v_gl_InstanceID : SV_InstanceID;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD3 : TEXCOORD2;
    nointerpolation uint v_vs_SV_InstanceID0 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static int v_u_xlati0;
static int v_u_xlati5;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float3 v_u_xlat5;
static float4 v_vs_TEXCOORD2;
static float v_u_xlat16_3;
static float4 v_u_xlat16_1;
static float3 v_u_xlat16_4;

void v_vert_main()
{
    v_u_xlati0 = v_gl_InstanceID + unity_BaseInstanceID;
    v_u_xlati5 = v_u_xlati0 << 3;
    v_u_xlati0 *= 7;
    v_u_xlat1 = v_in_POSITION0.yyyy * _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[1];
    v_u_xlat1 = (_40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (_40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3];
    v_vs_TEXCOORD1 = (_40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3].xyz * v_in_POSITION0.www) + v_u_xlat1.xyz;
    v_u_xlat1 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat1;
    v_u_xlat1.x = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, _40_unity_Builtins0Array[v_u_xlati5 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[2].xyz);
    v_u_xlat5.x = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat5.x = rsqrt(v_u_xlat5.x);
    v_u_xlat5 = v_u_xlat5.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD0 = v_u_xlat5;
    v_vs_TEXCOORD2 = 0.0f.xxxx;
    v_u_xlat16_3 = v_u_xlat5.y * v_u_xlat5.y;
    v_u_xlat16_3 = (v_u_xlat5.x * v_u_xlat5.x) + (-v_u_xlat16_3);
    v_u_xlat16_1 = v_u_xlat5.yzzx * v_u_xlat5.xyzz;
    v_u_xlat16_4.x = dot(_201_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBrArray, v_u_xlat16_1);
    v_u_xlat16_4.y = dot(_201_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBgArray, v_u_xlat16_1);
    v_u_xlat16_4.z = dot(_201_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHBbArray, v_u_xlat16_1);
    v_vs_TEXCOORD3 = (_201_unity_Builtins2Array[v_u_xlati0 / 7].unity_SHCArray.xyz * v_u_xlat16_3.xxx) + v_u_xlat16_4;
    v_vs_SV_InstanceID0 = uint(v_gl_InstanceID);
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_gl_InstanceID = int(stage_input.v_gl_InstanceID);
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_SV_InstanceID0 = v_vs_SV_InstanceID0;
    return stage_output;
}
struct f_unity_Builtins2Array_Type
{
    float4 unity_SHArArray;
    float4 unity_SHAgArray;
    float4 unity_SHAbArray;
    float4 unity_SHBrArray;
    float4 unity_SHBgArray;
    float4 unity_SHBbArray;
    float4 unity_SHCArray;
};

cbuffer UnityInstancing_PerDraw2
{
    f_unity_Builtins2Array_Type _117_unity_Builtins2Array[2] : packoffset(c0);
};







static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float4 f_SV_Target1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target2;
static uint f_vs_SV_InstanceID0;
static float3 f_vs_TEXCOORD3;
static float4 f_SV_Target3;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD3 : TEXCOORD2;
    nointerpolation uint f_vs_SV_InstanceID0 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
    float4 f_SV_Target1 : SV_Target1;
    float4 f_SV_Target2 : SV_Target2;
    float4 f_SV_Target3 : SV_Target3;
};

static float3 f_u_xlat0;
static float f_u_xlat16_0;
static float3 f_u_xlat3;
static float4 f_u_xlat1;
static int f_u_xlati9;
static float3 f_u_xlat16_2;

void f_frag_main()
{
    float2 _19 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _19.x;
    f_u_xlat0.y = _19.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat3 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    f_u_xlat0 = (f_u_xlat16_0.xxx * f_u_xlat3) + _MaskColor1.xyz;
    f_SV_Target0.x = f_u_xlat0.x;
    f_SV_Target0.y = f_u_xlat0.y;
    f_SV_Target0.z = f_u_xlat0.z;
    f_SV_Target0.w = 1.0f;
    f_SV_Target1 = 0.0f.xxxx;
    float3 _80 = (f_vs_TEXCOORD0 * 0.5f.xxx) + 0.5f.xxx;
    f_u_xlat1.x = _80.x;
    f_u_xlat1.y = _80.y;
    f_u_xlat1.z = _80.z;
    f_u_xlat1.w = 1.0f;
    f_SV_Target2 = f_u_xlat1;
    f_u_xlati9 = int(f_vs_SV_InstanceID0) + unity_BaseInstanceID;
    f_u_xlati9 *= 7;
    f_u_xlat1.x = f_vs_TEXCOORD0.x;
    f_u_xlat1.y = f_vs_TEXCOORD0.y;
    f_u_xlat1.z = f_vs_TEXCOORD0.z;
    f_u_xlat1.w = 1.0f;
    f_u_xlat16_2.x = dot(_117_unity_Builtins2Array[f_u_xlati9 / 7].unity_SHArArray, f_u_xlat1);
    f_u_xlat16_2.y = dot(_117_unity_Builtins2Array[f_u_xlati9 / 7].unity_SHAgArray, f_u_xlat1);
    f_u_xlat16_2.z = dot(_117_unity_Builtins2Array[f_u_xlati9 / 7].unity_SHAbArray, f_u_xlat1);
    f_u_xlat16_2 += f_vs_TEXCOORD3;
    f_u_xlat16_2 = max(f_u_xlat16_2, 0.0f.xxx);
    f_u_xlat16_2 = f_u_xlat0 * f_u_xlat16_2;
    float3 _156 = exp2(-f_u_xlat16_2);
    f_SV_Target3.x = _156.x;
    f_SV_Target3.y = _156.y;
    f_SV_Target3.z = _156.z;
    f_SV_Target3.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_SV_InstanceID0 = stage_input.f_vs_SV_InstanceID0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    stage_output.f_SV_Target1 = f_SV_Target1;
    stage_output.f_SV_Target2 = f_SV_Target2;
    stage_output.f_SV_Target3 = f_SV_Target3;
    return stage_output;
}
#elif defined(INSTANCING_ON) && !defined(LIGHTPROBE_SH) && defined(UNITY_HDR_ON)
float4x4 unity_MatrixVP;
int unity_BaseInstanceID;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
struct v_unity_Builtins0Array_Type
{
    float4 hlslcc_mtx4x4unity_ObjectToWorldArray[4];
    float4 hlslcc_mtx4x4unity_WorldToObjectArray[4];
};

cbuffer UnityInstancing_PerDraw0
{
    v_unity_Builtins0Array_Type _36_unity_Builtins0Array[2] : packoffset(c0);
};


static float4 v_gl_Position;
static int v_gl_InstanceID;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static uint v_vs_SV_InstanceID0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    uint v_gl_InstanceID : SV_InstanceID;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    nointerpolation uint v_vs_SV_InstanceID0 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static int v_u_xlati0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat0;
static float4 v_vs_TEXCOORD2;

void v_vert_main()
{
    v_u_xlati0 = v_gl_InstanceID + unity_BaseInstanceID;
    v_u_xlati0 = v_u_xlati0 << 3;
    v_u_xlat1 = v_in_POSITION0.yyyy * _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[1];
    v_u_xlat1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3];
    v_vs_TEXCOORD1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3].xyz * v_in_POSITION0.www) + v_u_xlat1.xyz;
    v_u_xlat1 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat1;
    v_u_xlat1.x = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[2].xyz);
    v_u_xlat0 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat0 = rsqrt(v_u_xlat0);
    v_vs_TEXCOORD0 = v_u_xlat0.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = 0.0f.xxxx;
    v_vs_SV_InstanceID0 = uint(v_gl_InstanceID);
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_gl_InstanceID = int(stage_input.v_gl_InstanceID);
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_SV_InstanceID0 = v_vs_SV_InstanceID0;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float4 f_SV_Target1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target2;
static float4 f_SV_Target3;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
    float4 f_SV_Target1 : SV_Target1;
    float4 f_SV_Target2 : SV_Target2;
    float4 f_SV_Target3 : SV_Target3;
};

static float4 f_u_xlat0;
static float f_u_xlat16_0;
static float3 f_u_xlat1;

void f_frag_main()
{
    float2 _20 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _20.x;
    f_u_xlat0.y = _20.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat1 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    float3 _56 = (f_u_xlat16_0.xxx * f_u_xlat1) + _MaskColor1.xyz;
    f_u_xlat0.x = _56.x;
    f_u_xlat0.y = _56.y;
    f_u_xlat0.z = _56.z;
    f_SV_Target0.x = f_u_xlat0.xyz.x;
    f_SV_Target0.y = f_u_xlat0.xyz.y;
    f_SV_Target0.z = f_u_xlat0.xyz.z;
    f_SV_Target0.w = 1.0f;
    f_SV_Target1 = 0.0f.xxxx;
    float3 _86 = (f_vs_TEXCOORD0 * 0.5f.xxx) + 0.5f.xxx;
    f_u_xlat0.x = _86.x;
    f_u_xlat0.y = _86.y;
    f_u_xlat0.z = _86.z;
    f_u_xlat0.w = 1.0f;
    f_SV_Target2 = f_u_xlat0;
    f_SV_Target3 = float4(0.0f, 0.0f, 0.0f, 1.0f);
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    stage_output.f_SV_Target1 = f_SV_Target1;
    stage_output.f_SV_Target2 = f_SV_Target2;
    stage_output.f_SV_Target3 = f_SV_Target3;
    return stage_output;
}
#elif !defined(INSTANCING_ON) && defined(LIGHTPROBE_SH) && !defined(UNITY_HDR_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static float3 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float3 v_vs_TEXCOORD3 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat12;
static float4 v_vs_TEXCOORD2;
static float3 v_u_xlat16_2;
static float4 v_u_xlat16_1;
static float3 v_u_xlat16_3;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_vs_TEXCOORD1 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat12 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat12 = rsqrt(v_u_xlat12);
    float4 _126 = v_u_xlat0;
    float3 _128 = v_u_xlat12.xxx * _126.xyz;
    v_u_xlat0.x = _128.x;
    v_u_xlat0.y = _128.y;
    v_u_xlat0.z = _128.z;
    v_vs_TEXCOORD0 = v_u_xlat0.xyz;
    v_vs_TEXCOORD2 = 0.0f.xxxx;
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
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float3 f_vs_TEXCOORD3;
static float4 f_SV_Target3;
static float4 f_SV_Target1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target2;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
    float3 f_vs_TEXCOORD3 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
    float4 f_SV_Target1 : SV_Target1;
    float4 f_SV_Target2 : SV_Target2;
    float4 f_SV_Target3 : SV_Target3;
};

static float4 f_u_xlat0;
static float f_u_xlat16_0;
static float3 f_u_xlat2;
static float3 f_u_xlat16_1;

void f_frag_main()
{
    float2 _20 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _20.x;
    f_u_xlat0.y = _20.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat2 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    float3 _56 = (f_u_xlat16_0.xxx * f_u_xlat2) + _MaskColor1.xyz;
    f_u_xlat0.x = _56.x;
    f_u_xlat0.y = _56.y;
    f_u_xlat0.z = _56.z;
    f_SV_Target0.x = f_u_xlat0.xyz.x;
    f_SV_Target0.y = f_u_xlat0.xyz.y;
    f_SV_Target0.z = f_u_xlat0.xyz.z;
    f_u_xlat16_1 = f_u_xlat0.xyz * f_vs_TEXCOORD3;
    float3 _84 = exp2(-f_u_xlat16_1);
    f_SV_Target3.x = _84.x;
    f_SV_Target3.y = _84.y;
    f_SV_Target3.z = _84.z;
    f_SV_Target0.w = 1.0f;
    f_SV_Target1 = 0.0f.xxxx;
    float3 _102 = (f_vs_TEXCOORD0 * 0.5f.xxx) + 0.5f.xxx;
    f_u_xlat0.x = _102.x;
    f_u_xlat0.y = _102.y;
    f_u_xlat0.z = _102.z;
    f_u_xlat0.w = 1.0f;
    f_SV_Target2 = f_u_xlat0;
    f_SV_Target3.w = 1.0f;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    stage_output.f_SV_Target3 = f_SV_Target3;
    stage_output.f_SV_Target1 = f_SV_Target1;
    stage_output.f_SV_Target2 = f_SV_Target2;
    return stage_output;
}
#elif !defined(INSTANCING_ON) && !defined(LIGHTPROBE_SH) && defined(UNITY_HDR_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat6;
static float4 v_vs_TEXCOORD2;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_vs_TEXCOORD1 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD0 = v_u_xlat6.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD2 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float4 f_SV_Target1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target2;
static float4 f_SV_Target3;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
    float4 f_SV_Target1 : SV_Target1;
    float4 f_SV_Target2 : SV_Target2;
    float4 f_SV_Target3 : SV_Target3;
};

static float4 f_u_xlat0;
static float f_u_xlat16_0;
static float3 f_u_xlat1;

void f_frag_main()
{
    float2 _20 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _20.x;
    f_u_xlat0.y = _20.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat1 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    float3 _56 = (f_u_xlat16_0.xxx * f_u_xlat1) + _MaskColor1.xyz;
    f_u_xlat0.x = _56.x;
    f_u_xlat0.y = _56.y;
    f_u_xlat0.z = _56.z;
    f_SV_Target0.x = f_u_xlat0.xyz.x;
    f_SV_Target0.y = f_u_xlat0.xyz.y;
    f_SV_Target0.z = f_u_xlat0.xyz.z;
    f_SV_Target0.w = 1.0f;
    f_SV_Target1 = 0.0f.xxxx;
    float3 _86 = (f_vs_TEXCOORD0 * 0.5f.xxx) + 0.5f.xxx;
    f_u_xlat0.x = _86.x;
    f_u_xlat0.y = _86.y;
    f_u_xlat0.z = _86.z;
    f_u_xlat0.w = 1.0f;
    f_SV_Target2 = f_u_xlat0;
    f_SV_Target3 = float4(0.0f, 0.0f, 0.0f, 1.0f);
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    stage_output.f_SV_Target1 = f_SV_Target1;
    stage_output.f_SV_Target2 = f_SV_Target2;
    stage_output.f_SV_Target3 = f_SV_Target3;
    return stage_output;
}
#elif defined(INSTANCING_ON) && !defined(LIGHTPROBE_SH) && !defined(UNITY_HDR_ON)
float4x4 unity_MatrixVP;
int unity_BaseInstanceID;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
struct v_unity_Builtins0Array_Type
{
    float4 hlslcc_mtx4x4unity_ObjectToWorldArray[4];
    float4 hlslcc_mtx4x4unity_WorldToObjectArray[4];
};

cbuffer UnityInstancing_PerDraw0
{
    v_unity_Builtins0Array_Type _36_unity_Builtins0Array[2] : packoffset(c0);
};


static float4 v_gl_Position;
static int v_gl_InstanceID;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;
static uint v_vs_SV_InstanceID0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    uint v_gl_InstanceID : SV_InstanceID;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    nointerpolation uint v_vs_SV_InstanceID0 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static int v_u_xlati0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float v_u_xlat0;
static float4 v_vs_TEXCOORD2;

void v_vert_main()
{
    v_u_xlati0 = v_gl_InstanceID + unity_BaseInstanceID;
    v_u_xlati0 = v_u_xlati0 << 3;
    v_u_xlat1 = v_in_POSITION0.yyyy * _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[1];
    v_u_xlat1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3];
    v_vs_TEXCOORD1 = (_36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_ObjectToWorldArray[3].xyz * v_in_POSITION0.www) + v_u_xlat1.xyz;
    v_u_xlat1 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat1;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat1;
    v_u_xlat1.x = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[0].xyz);
    v_u_xlat1.y = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[1].xyz);
    v_u_xlat1.z = dot(v_in_NORMAL0, _36_unity_Builtins0Array[v_u_xlati0 / 8].hlslcc_mtx4x4unity_WorldToObjectArray[2].xyz);
    v_u_xlat0 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat0 = rsqrt(v_u_xlat0);
    v_vs_TEXCOORD0 = v_u_xlat0.xxx * v_u_xlat1.xyz;
    v_vs_TEXCOORD2 = 0.0f.xxxx;
    v_vs_SV_InstanceID0 = uint(v_gl_InstanceID);
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_gl_InstanceID = int(stage_input.v_gl_InstanceID);
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_SV_InstanceID0 = v_vs_SV_InstanceID0;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float4 f_SV_Target1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target2;
static float4 f_SV_Target3;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
    float4 f_SV_Target1 : SV_Target1;
    float4 f_SV_Target2 : SV_Target2;
    float4 f_SV_Target3 : SV_Target3;
};

static float4 f_u_xlat0;
static float f_u_xlat16_0;
static float3 f_u_xlat1;

void f_frag_main()
{
    float2 _20 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _20.x;
    f_u_xlat0.y = _20.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat1 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    float3 _56 = (f_u_xlat16_0.xxx * f_u_xlat1) + _MaskColor1.xyz;
    f_u_xlat0.x = _56.x;
    f_u_xlat0.y = _56.y;
    f_u_xlat0.z = _56.z;
    f_SV_Target0.x = f_u_xlat0.xyz.x;
    f_SV_Target0.y = f_u_xlat0.xyz.y;
    f_SV_Target0.z = f_u_xlat0.xyz.z;
    f_SV_Target0.w = 1.0f;
    f_SV_Target1 = 0.0f.xxxx;
    float3 _86 = (f_vs_TEXCOORD0 * 0.5f.xxx) + 0.5f.xxx;
    f_u_xlat0.x = _86.x;
    f_u_xlat0.y = _86.y;
    f_u_xlat0.z = _86.z;
    f_u_xlat0.w = 1.0f;
    f_SV_Target2 = f_u_xlat0;
    f_SV_Target3 = 1.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    stage_output.f_SV_Target1 = f_SV_Target1;
    stage_output.f_SV_Target2 = f_SV_Target2;
    stage_output.f_SV_Target3 = f_SV_Target3;
    return stage_output;
}
#elif !defined(INSTANCING_ON) && !defined(LIGHTPROBE_SH) && !defined(UNITY_HDR_ON)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_WorldToObject;
float _UVTile;
Texture2D<float4> _Texture0;
SamplerState sampler_Texture0;
float4 _MaskColor1;
float4 _MaskColor2;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;
static float3 v_in_NORMAL0;
static float3 v_vs_TEXCOORD0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float3 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat6;
static float4 v_vs_TEXCOORD2;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_vs_TEXCOORD1 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat6 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat6 = rsqrt(v_u_xlat6);
    v_vs_TEXCOORD0 = v_u_xlat6.xxx * v_u_xlat0.xyz;
    v_vs_TEXCOORD2 = 0.0f.xxxx;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    return stage_output;
}
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;
static float4 f_SV_Target1;
static float3 f_vs_TEXCOORD0;
static float4 f_SV_Target2;
static float4 f_SV_Target3;

struct f_SPIRV_Cross_Input
{
    float3 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
    float4 f_SV_Target1 : SV_Target1;
    float4 f_SV_Target2 : SV_Target2;
    float4 f_SV_Target3 : SV_Target3;
};

static float4 f_u_xlat0;
static float f_u_xlat16_0;
static float3 f_u_xlat1;

void f_frag_main()
{
    float2 _20 = f_vs_TEXCOORD1.xz * _UVTile.xx;
    f_u_xlat0.x = _20.x;
    f_u_xlat0.y = _20.y;
    f_u_xlat16_0 = _Texture0.Sample(sampler_Texture0, f_u_xlat0.xy).x;
    f_u_xlat1 = (-_MaskColor1.xyz) + _MaskColor2.xyz;
    float3 _56 = (f_u_xlat16_0.xxx * f_u_xlat1) + _MaskColor1.xyz;
    f_u_xlat0.x = _56.x;
    f_u_xlat0.y = _56.y;
    f_u_xlat0.z = _56.z;
    f_SV_Target0.x = f_u_xlat0.xyz.x;
    f_SV_Target0.y = f_u_xlat0.xyz.y;
    f_SV_Target0.z = f_u_xlat0.xyz.z;
    f_SV_Target0.w = 1.0f;
    f_SV_Target1 = 0.0f.xxxx;
    float3 _86 = (f_vs_TEXCOORD0 * 0.5f.xxx) + 0.5f.xxx;
    f_u_xlat0.x = _86.x;
    f_u_xlat0.y = _86.y;
    f_u_xlat0.z = _86.z;
    f_u_xlat0.w = 1.0f;
    f_SV_Target2 = f_u_xlat0;
    f_SV_Target3 = 1.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
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
// Supply opaque ground to camera depth and light shadow maps.
UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
}
}
