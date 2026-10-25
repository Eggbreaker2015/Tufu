Shader "Custom/DarkFadePortal" {
Properties {
		_Color ("Tint", Vector) = (0,0,0,1)
		_FadeStart ("Fade Start", Float) = 0
		_FadeEnd ("Fade End", Float) = 2
		_Softness ("Softness", Float) = 0.5
		_NoiseTex ("Noise", 2D) = "white" {}
		_NoiseStrength ("Noise Strength", Float) = 0.2
		_NoiseScale ("Noise Scale", Float) = 2
	}
// Recovered from the APK GLES programs; generated HLSL, not original author source.
SubShader {
Tags { "Queue"="Transparent" "RenderType"="Transparent" }
Pass {
Name ""
Tags { "Queue"="Transparent" "RenderType"="Transparent" }
Cull Back
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
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float _NoiseScale;
Texture2D<float4> _NoiseTex;
SamplerState sampler_NoiseTex;
float _NoiseStrength;
float _FadeStart;
float _FadeEnd;
float _Softness;
float4 _Color;
static float4 v_gl_Position;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float2 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float2 v_vs_TEXCOORD0 : TEXCOORD0;
    float3 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;

void v_vert_main()
{
    v_vs_TEXCOORD0 = v_in_TEXCOORD0;
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat1 = v_u_xlat0 + transpose(unity_ObjectToWorld)[3];
    v_vs_TEXCOORD1 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + v_u_xlat0.xyz;
    v_u_xlat0 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat0 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat0;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat0;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float2 f_vs_TEXCOORD0;
static float3 f_vs_TEXCOORD1;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float2 f_vs_TEXCOORD0 : TEXCOORD0;
    float3 f_vs_TEXCOORD1 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat0;
static float f_u_xlat16_0;
static float f_u_xlat1;
static float f_u_xlat2;

void f_frag_main()
{
    float2 _18 = f_vs_TEXCOORD0 * _NoiseScale.xx;
    f_u_xlat0.x = _18.x;
    f_u_xlat0.y = _18.y;
    f_u_xlat16_0 = _NoiseTex.Sample(sampler_NoiseTex, f_u_xlat0.xy).x;
    f_u_xlat0.x = f_u_xlat16_0 + (-0.5f);
    f_u_xlat0.x *= _NoiseStrength;
    f_u_xlat1 = (-_FadeStart) + _FadeEnd;
    f_u_xlat1 = 1.0f / f_u_xlat1;
    f_u_xlat2 = f_vs_TEXCOORD1.z + (-_FadeStart);
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat1 = clamp(f_u_xlat1, 0.0f, 1.0f);
    f_u_xlat2 = (f_u_xlat1 * (-2.0f)) + 3.0f;
    f_u_xlat1 *= f_u_xlat1;
    f_u_xlat0.x = (f_u_xlat2 * f_u_xlat1) + f_u_xlat0.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat1 = 1.0f / _Softness;
    f_u_xlat0.x = f_u_xlat1 * f_u_xlat0.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat1 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat1;
    f_u_xlat0.w = f_u_xlat0.x * _Color.w;
    f_u_xlat0.x = _Color.xyz.x;
    f_u_xlat0.y = _Color.xyz.y;
    f_u_xlat0.z = _Color.xyz.z;
    f_SV_Target0 = f_u_xlat0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
ENDHLSL
}
}
}
