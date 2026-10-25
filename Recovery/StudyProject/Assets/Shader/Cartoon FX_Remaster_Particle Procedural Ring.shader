Shader "Cartoon FX/Remaster/Particle Procedural Ring" {
Properties {
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Blend Source", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Blend Destination", Float) = 10
		[Toggle(_CFXR_DISSOLVE)] _UseDissolve ("Enable Dissolve", Float) = 0
		[NoScaleOffset] _DissolveTex ("Dissolve Texture", 2D) = "gray" {}
		_DissolveSmooth ("Dissolve Smoothing", Range(0.0001, 0.5)) = 0.1
		[ToggleNoKeyword] _InvertDissolveTex ("Invert Dissolve Texture", Float) = 0
		_MainTex ("Texture", 2D) = "white" {}
		[Toggle] _SingleChannel ("Single Channel Texture", Float) = 0
		[Toggle(_CFXR_RADIAL_UV)] _UseRadialUV ("Enable Radial UVs", Float) = 0
		_RingTopOffset ("Ring Offset", Float) = 0.05
		[Toggle(_CFXR_WORLD_SPACE_RING)] _WorldSpaceRing ("World Space", Float) = 0
		_HdrMultiply ("HDR Multiplier", Float) = 2
		[Toggle(_FADING_ON)] _UseSP ("Soft Particles", Float) = 0
		_SoftParticlesFadeDistanceNear ("Near Fade", Float) = 0
		_SoftParticlesFadeDistanceFar ("Far Fade", Float) = 1
		[KeywordEnum(Off,On,CustomTexture)] _CFXR_DITHERED_SHADOWS ("Dithered Shadows", Float) = 0
		_ShadowStrength ("Shadows Strength Max", Range(0, 1)) = 1
		_DitherCustom ("Dithering 3D Texture", 3D) = "black" {}
	}
// Recovered from the APK GLES programs; generated HLSL, not original author source.
SubShader {
Tags { "IgnoreProjector"="true" "Queue"="Transparent" "RenderType"="Transparent" }
Pass {
Name "BASE"
Tags { "IgnoreProjector"="true" "LightMode"="ForwardBase" "Queue"="Transparent" "RenderType"="Transparent" }
Cull Off
ZWrite Off
ZTest LEqual
Blend [_SrcBlend] [_DstBlend], [_SrcBlend] [_DstBlend]
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
#pragma multi_compile _ SOFTPARTICLES_ON
#pragma shader_feature_local _ _CFXR_DISSOLVE
#pragma shader_feature_local _ _CFXR_WORLD_SPACE_RING
#if defined(SOFTPARTICLES_ON) && defined(_CFXR_DISSOLVE) && defined(_CFXR_WORLD_SPACE_RING)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float _RingTopOffset;
float4 _MainTex_ST;
float _InvertDissolveTex;
Texture2D<float4> _DissolveTex;
SamplerState sampler_DissolveTex;
float _DissolveSmooth;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float4 v_in_TEXCOORD2;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;
static float4 v_vs_TEXCOORD5;
static float4 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float4 v_in_TEXCOORD2 : TEXCOORD2;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_vs_TEXCOORD2 : TEXCOORD2;
    float4 v_vs_TEXCOORD5 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float3 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float4 v_vs_TEXCOORD0;

void v_vert_main()
{
    v_u_xlat0 = v_in_TEXCOORD0.yyy * v_in_NORMAL0;
    v_u_xlat9 = min(v_in_TEXCOORD2.x, v_in_TEXCOORD2.w);
    v_u_xlat0 = ((-v_u_xlat0) * v_u_xlat9.xxx) + v_in_POSITION0.xyz;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_u_xlat1 += transpose(unity_ObjectToWorld)[3];
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0;
    v_u_xlat0.x = v_u_xlat9 / v_in_TEXCOORD2.w;
    v_u_xlat0.x = (-v_u_xlat0.x) + 1.0f;
    v_vs_TEXCOORD1.x = v_u_xlat0.x * v_u_xlat0.x;
    v_vs_TEXCOORD1.z = v_in_TEXCOORD2.y / v_in_TEXCOORD2.w;
    v_vs_TEXCOORD1.y = (-_RingTopOffset) + 1.0f;
    v_vs_TEXCOORD1.w = v_in_TEXCOORD2.z;
    float2 _154 = v_in_TEXCOORD0.yy * v_in_TEXCOORD0.zw;
    v_u_xlat0.x = _154.x;
    v_u_xlat0.y = _154.y;
    float3 _161 = v_u_xlat0;
    float2 _163 = v_u_xlat9.xx * _161.xy;
    v_u_xlat0.x = _163.x;
    v_u_xlat0.y = _163.y;
    float3 _168 = v_u_xlat0;
    float2 _172 = _168.xy / v_in_TEXCOORD2.ww;
    v_u_xlat0.x = _172.x;
    v_u_xlat0.y = _172.y;
    float2 _183 = (-v_u_xlat0.xy) + v_in_TEXCOORD0.zw;
    v_vs_TEXCOORD2.z = _183.x;
    v_vs_TEXCOORD2.w = _183.y;
    v_u_xlat0.x = v_in_TEXCOORD2.z;
    v_u_xlat0.y = 0.0f;
    float3 _193 = v_u_xlat0;
    float2 _197 = _193.xy + v_in_TEXCOORD0.xy;
    v_u_xlat0.x = _197.x;
    v_u_xlat0.y = _197.y;
    float3 _202 = v_u_xlat0;
    float2 _210 = (_202.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = _210.x;
    v_u_xlat0.y = _210.y;
    float2 _219 = (-v_u_xlat0.xy) + 1.0f.xx;
    v_vs_TEXCOORD2.x = _219.x;
    v_vs_TEXCOORD2.y = _219.y;
    v_vs_TEXCOORD5 = v_in_TEXCOORD1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_TEXCOORD2 = stage_input.v_in_TEXCOORD2;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD5 = v_vs_TEXCOORD5;
    return stage_output;
}
static float4 f_vs_TEXCOORD2;
static float4 f_vs_TEXCOORD5;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD1 : TEXCOORD1;
    float4 f_vs_TEXCOORD2 : TEXCOORD2;
    float4 f_vs_TEXCOORD5 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static bool f_u_xlatb0;
static float f_u_xlat16_4;
static float3 f_u_xlat16_1;
static float f_u_xlat16_5;
static float f_u_xlat16_9;
static float4 f_u_xlat0;
static float f_u_xlat4;
static float f_u_xlat8;
static float4 f_u_xlat16_2;
static bool f_u_xlatb4;
static float4 f_u_xlat16_3;
static float4 f_u_xlat16_0;
static bool f_u_xlatb2;

void f_frag_main()
{
    f_u_xlatb0 = 0.0f >= _InvertDissolveTex;
    f_u_xlat16_4 = _DissolveTex.Sample(sampler_DissolveTex, f_vs_TEXCOORD2.xy).x;
    f_u_xlat16_1.x = (-f_u_xlat16_4) + 1.0f;
    float _42;
    if (f_u_xlatb0)
    {
        _42 = f_u_xlat16_1.x;
    }
    else
    {
        _42 = f_u_xlat16_4;
    }
    f_u_xlat16_1.x = _42;
    f_u_xlat16_5 = f_u_xlat16_1.x + _DissolveSmooth;
    f_u_xlat16_1.x += (-_DissolveSmooth);
    f_u_xlat16_5 = (-f_u_xlat16_1.x) + f_u_xlat16_5;
    f_u_xlat16_5 = 1.0f / f_u_xlat16_5;
    f_u_xlat16_9 = (_DissolveSmooth * 2.0f) + 1.0f;
    f_u_xlat16_9 = (f_vs_TEXCOORD5.x * f_u_xlat16_9) + (-_DissolveSmooth);
    f_u_xlat16_1.x = (-f_u_xlat16_1.x) + f_u_xlat16_9;
    f_u_xlat16_1.x = f_u_xlat16_5 * f_u_xlat16_1.x;
    f_u_xlat16_1.x = clamp(f_u_xlat16_1.x, 0.0f, 1.0f);
    f_u_xlat16_5 = (f_u_xlat16_1.x * (-2.0f)) + 3.0f;
    f_u_xlat16_1.x *= f_u_xlat16_1.x;
    f_u_xlat16_1.x *= f_u_xlat16_5;
    f_u_xlat0.x = (-f_vs_TEXCOORD1.z) + f_vs_TEXCOORD1.y;
    f_u_xlat4 = (-f_u_xlat0.x) + f_vs_TEXCOORD1.y;
    f_u_xlat4 = 1.0f / f_u_xlat4;
    f_u_xlat8 = dot(f_vs_TEXCOORD2.zw, f_vs_TEXCOORD2.zw);
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat8 += (-f_vs_TEXCOORD1.x);
    f_u_xlat0.x = f_u_xlat4 * f_u_xlat0.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4;
    f_u_xlat4 = 1.0f / f_vs_TEXCOORD1.z;
    f_u_xlat4 *= f_u_xlat8;
    f_u_xlat4 = clamp(f_u_xlat4, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4 * (-2.0f)) + 3.0f;
    f_u_xlat4 *= f_u_xlat4;
    f_u_xlat0.x = (f_u_xlat8 * f_u_xlat4) + (-f_u_xlat0.x);
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD2.xy);
    f_u_xlatb4 = 0.0f < _SingleChannel;
    float3 _217;
    if (f_u_xlatb4)
    {
        _217 = 1.0f.xxx;
    }
    else
    {
        _217 = f_u_xlat16_2.xyz;
    }
    f_u_xlat16_3.x = _217.x;
    f_u_xlat16_3.y = _217.y;
    f_u_xlat16_3.z = _217.z;
    float _232;
    if (f_u_xlatb4)
    {
        _232 = f_u_xlat16_2.x;
    }
    else
    {
        _232 = f_u_xlat16_2.w;
    }
    f_u_xlat16_3.w = _232;
    f_u_xlat0 = f_u_xlat0.xxxx * f_u_xlat16_3;
    f_u_xlat16_0 = f_u_xlat0 * f_vs_COLOR0;
    f_u_xlat16_1.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_1 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlatb2 = 0.0f < _HdrMultiply;
    float3 _275;
    if (f_u_xlatb2)
    {
        _275 = f_u_xlat16_1;
    }
    else
    {
        _275 = f_u_xlat16_0.xyz;
    }
    f_SV_Target0.x = _275.x;
    f_SV_Target0.y = _275.y;
    f_SV_Target0.z = _275.z;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD5 = stage_input.f_vs_TEXCOORD5;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SOFTPARTICLES_ON) && defined(_CFXR_DISSOLVE) && defined(_CFXR_WORLD_SPACE_RING)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float _RingTopOffset;
float4 _MainTex_ST;
float _InvertDissolveTex;
Texture2D<float4> _DissolveTex;
SamplerState sampler_DissolveTex;
float _DissolveSmooth;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float4 v_in_TEXCOORD2;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;
static float4 v_vs_TEXCOORD5;
static float4 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float4 v_in_TEXCOORD2 : TEXCOORD2;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_vs_TEXCOORD2 : TEXCOORD2;
    float4 v_vs_TEXCOORD5 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float3 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float4 v_vs_TEXCOORD0;

void v_vert_main()
{
    v_u_xlat0 = v_in_TEXCOORD0.yyy * v_in_NORMAL0;
    v_u_xlat9 = min(v_in_TEXCOORD2.x, v_in_TEXCOORD2.w);
    v_u_xlat0 = ((-v_u_xlat0) * v_u_xlat9.xxx) + v_in_POSITION0.xyz;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_u_xlat1 += transpose(unity_ObjectToWorld)[3];
    v_u_xlat2 = v_u_xlat1.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat2 = (transpose(unity_MatrixVP)[0] * v_u_xlat1.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(unity_MatrixVP)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(unity_MatrixVP)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0;
    v_u_xlat0.x = v_u_xlat9 / v_in_TEXCOORD2.w;
    v_u_xlat0.x = (-v_u_xlat0.x) + 1.0f;
    v_vs_TEXCOORD1.x = v_u_xlat0.x * v_u_xlat0.x;
    v_vs_TEXCOORD1.z = v_in_TEXCOORD2.y / v_in_TEXCOORD2.w;
    v_vs_TEXCOORD1.y = (-_RingTopOffset) + 1.0f;
    v_vs_TEXCOORD1.w = v_in_TEXCOORD2.z;
    float2 _154 = v_in_TEXCOORD0.yy * v_in_TEXCOORD0.zw;
    v_u_xlat0.x = _154.x;
    v_u_xlat0.y = _154.y;
    float3 _161 = v_u_xlat0;
    float2 _163 = v_u_xlat9.xx * _161.xy;
    v_u_xlat0.x = _163.x;
    v_u_xlat0.y = _163.y;
    float3 _168 = v_u_xlat0;
    float2 _172 = _168.xy / v_in_TEXCOORD2.ww;
    v_u_xlat0.x = _172.x;
    v_u_xlat0.y = _172.y;
    float2 _183 = (-v_u_xlat0.xy) + v_in_TEXCOORD0.zw;
    v_vs_TEXCOORD2.z = _183.x;
    v_vs_TEXCOORD2.w = _183.y;
    v_u_xlat0.x = v_in_TEXCOORD2.z;
    v_u_xlat0.y = 0.0f;
    float3 _193 = v_u_xlat0;
    float2 _197 = _193.xy + v_in_TEXCOORD0.xy;
    v_u_xlat0.x = _197.x;
    v_u_xlat0.y = _197.y;
    float3 _202 = v_u_xlat0;
    float2 _210 = (_202.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = _210.x;
    v_u_xlat0.y = _210.y;
    float2 _219 = (-v_u_xlat0.xy) + 1.0f.xx;
    v_vs_TEXCOORD2.x = _219.x;
    v_vs_TEXCOORD2.y = _219.y;
    v_vs_TEXCOORD5 = v_in_TEXCOORD1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_TEXCOORD2 = stage_input.v_in_TEXCOORD2;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD5 = v_vs_TEXCOORD5;
    return stage_output;
}
static float4 f_vs_TEXCOORD2;
static float4 f_vs_TEXCOORD5;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD1 : TEXCOORD1;
    float4 f_vs_TEXCOORD2 : TEXCOORD2;
    float4 f_vs_TEXCOORD5 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static bool f_u_xlatb0;
static float f_u_xlat16_4;
static float3 f_u_xlat16_1;
static float f_u_xlat16_5;
static float f_u_xlat16_9;
static float4 f_u_xlat0;
static float f_u_xlat4;
static float f_u_xlat8;
static float4 f_u_xlat16_2;
static bool f_u_xlatb4;
static float4 f_u_xlat16_3;
static float4 f_u_xlat16_0;
static bool f_u_xlatb2;

void f_frag_main()
{
    f_u_xlatb0 = 0.0f >= _InvertDissolveTex;
    f_u_xlat16_4 = _DissolveTex.Sample(sampler_DissolveTex, f_vs_TEXCOORD2.xy).x;
    f_u_xlat16_1.x = (-f_u_xlat16_4) + 1.0f;
    float _42;
    if (f_u_xlatb0)
    {
        _42 = f_u_xlat16_1.x;
    }
    else
    {
        _42 = f_u_xlat16_4;
    }
    f_u_xlat16_1.x = _42;
    f_u_xlat16_5 = f_u_xlat16_1.x + _DissolveSmooth;
    f_u_xlat16_1.x += (-_DissolveSmooth);
    f_u_xlat16_5 = (-f_u_xlat16_1.x) + f_u_xlat16_5;
    f_u_xlat16_5 = 1.0f / f_u_xlat16_5;
    f_u_xlat16_9 = (_DissolveSmooth * 2.0f) + 1.0f;
    f_u_xlat16_9 = (f_vs_TEXCOORD5.x * f_u_xlat16_9) + (-_DissolveSmooth);
    f_u_xlat16_1.x = (-f_u_xlat16_1.x) + f_u_xlat16_9;
    f_u_xlat16_1.x = f_u_xlat16_5 * f_u_xlat16_1.x;
    f_u_xlat16_1.x = clamp(f_u_xlat16_1.x, 0.0f, 1.0f);
    f_u_xlat16_5 = (f_u_xlat16_1.x * (-2.0f)) + 3.0f;
    f_u_xlat16_1.x *= f_u_xlat16_1.x;
    f_u_xlat16_1.x *= f_u_xlat16_5;
    f_u_xlat0.x = (-f_vs_TEXCOORD1.z) + f_vs_TEXCOORD1.y;
    f_u_xlat4 = (-f_u_xlat0.x) + f_vs_TEXCOORD1.y;
    f_u_xlat4 = 1.0f / f_u_xlat4;
    f_u_xlat8 = dot(f_vs_TEXCOORD2.zw, f_vs_TEXCOORD2.zw);
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat8;
    f_u_xlat8 += (-f_vs_TEXCOORD1.x);
    f_u_xlat0.x = f_u_xlat4 * f_u_xlat0.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat4 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat4;
    f_u_xlat4 = 1.0f / f_vs_TEXCOORD1.z;
    f_u_xlat4 *= f_u_xlat8;
    f_u_xlat4 = clamp(f_u_xlat4, 0.0f, 1.0f);
    f_u_xlat8 = (f_u_xlat4 * (-2.0f)) + 3.0f;
    f_u_xlat4 *= f_u_xlat4;
    f_u_xlat0.x = (f_u_xlat8 * f_u_xlat4) + (-f_u_xlat0.x);
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD2.xy);
    f_u_xlatb4 = 0.0f < _SingleChannel;
    float3 _217;
    if (f_u_xlatb4)
    {
        _217 = 1.0f.xxx;
    }
    else
    {
        _217 = f_u_xlat16_2.xyz;
    }
    f_u_xlat16_3.x = _217.x;
    f_u_xlat16_3.y = _217.y;
    f_u_xlat16_3.z = _217.z;
    float _232;
    if (f_u_xlatb4)
    {
        _232 = f_u_xlat16_2.x;
    }
    else
    {
        _232 = f_u_xlat16_2.w;
    }
    f_u_xlat16_3.w = _232;
    f_u_xlat0 = f_u_xlat0.xxxx * f_u_xlat16_3;
    f_u_xlat16_0 = f_u_xlat0 * f_vs_COLOR0;
    f_u_xlat16_1.x *= f_u_xlat16_0.w;
    f_SV_Target0.w = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_1 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlatb2 = 0.0f < _HdrMultiply;
    float3 _275;
    if (f_u_xlatb2)
    {
        _275 = f_u_xlat16_1;
    }
    else
    {
        _275 = f_u_xlat16_0.xyz;
    }
    f_SV_Target0.x = _275.x;
    f_SV_Target0.y = _275.y;
    f_SV_Target0.z = _275.z;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD5 = stage_input.f_vs_TEXCOORD5;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(SOFTPARTICLES_ON) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_WORLD_SPACE_RING)
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;
float _RingTopOffset;
float4 _MainTex_ST;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float4 v_in_TEXCOORD2;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD2 : TEXCOORD2;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_vs_TEXCOORD2 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float2 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat6;
static float4 v_u_xlat2;
static float4 v_vs_TEXCOORD0;

void v_vert_main()
{
    v_u_xlat0 = v_in_TEXCOORD0.yy * (-v_in_TEXCOORD0.zw);
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_MatrixV)[1];
    v_u_xlat1 = (transpose(unity_MatrixV)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixV)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixV)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    v_u_xlat6 = min(v_in_TEXCOORD2.x, v_in_TEXCOORD2.w);
    v_u_xlat0 = (v_u_xlat0 * v_u_xlat6.xx) + v_u_xlat1.xy;
    v_u_xlat2 = v_u_xlat0.yyyy * transpose(glstate_matrix_projection)[1];
    v_u_xlat2 = (transpose(glstate_matrix_projection)[0] * v_u_xlat0.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(glstate_matrix_projection)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(glstate_matrix_projection)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0;
    v_u_xlat0.x = v_u_xlat6 / v_in_TEXCOORD2.w;
    v_u_xlat0.x = (-v_u_xlat0.x) + 1.0f;
    v_vs_TEXCOORD1.x = v_u_xlat0.x * v_u_xlat0.x;
    v_vs_TEXCOORD1.z = v_in_TEXCOORD2.y / v_in_TEXCOORD2.w;
    v_vs_TEXCOORD1.y = (-_RingTopOffset) + 1.0f;
    v_vs_TEXCOORD1.w = v_in_TEXCOORD2.z;
    v_u_xlat0 = v_in_TEXCOORD0.yy * v_in_TEXCOORD0.zw;
    v_u_xlat0 = v_u_xlat6.xx * v_u_xlat0;
    v_u_xlat0 /= v_in_TEXCOORD2.ww;
    float2 _169 = (-v_u_xlat0) + v_in_TEXCOORD0.zw;
    v_vs_TEXCOORD2.z = _169.x;
    v_vs_TEXCOORD2.w = _169.y;
    v_u_xlat0.x = v_in_TEXCOORD2.z;
    v_u_xlat0.y = 0.0f;
    v_u_xlat0 += v_in_TEXCOORD0.xy;
    v_u_xlat0 = (v_u_xlat0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    float2 _194 = (-v_u_xlat0) + 1.0f.xx;
    v_vs_TEXCOORD2.x = _194.x;
    v_vs_TEXCOORD2.y = _194.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD2 = stage_input.v_in_TEXCOORD2;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float4 f_vs_TEXCOORD1;
static float4 f_vs_TEXCOORD2;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD1 : TEXCOORD1;
    float4 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat0;
static float f_u_xlat3;
static float f_u_xlat6;
static float4 f_u_xlat16_1;
static bool f_u_xlatb3;
static float4 f_u_xlat16_2;
static float4 f_u_xlat16_0;
static bool f_u_xlatb1;

void f_frag_main()
{
    f_u_xlat0.x = (-f_vs_TEXCOORD1.z) + f_vs_TEXCOORD1.y;
    f_u_xlat3 = (-f_u_xlat0.x) + f_vs_TEXCOORD1.y;
    f_u_xlat3 = 1.0f / f_u_xlat3;
    f_u_xlat6 = dot(f_vs_TEXCOORD2.zw, f_vs_TEXCOORD2.zw);
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat6;
    f_u_xlat6 += (-f_vs_TEXCOORD1.x);
    f_u_xlat0.x = f_u_xlat3 * f_u_xlat0.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat3 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat3;
    f_u_xlat3 = 1.0f / f_vs_TEXCOORD1.z;
    f_u_xlat3 *= f_u_xlat6;
    f_u_xlat3 = clamp(f_u_xlat3, 0.0f, 1.0f);
    f_u_xlat6 = (f_u_xlat3 * (-2.0f)) + 3.0f;
    f_u_xlat3 *= f_u_xlat3;
    f_u_xlat0.x = (f_u_xlat6 * f_u_xlat3) + (-f_u_xlat0.x);
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD2.xy);
    f_u_xlatb3 = 0.0f < _SingleChannel;
    float3 _127;
    if (f_u_xlatb3)
    {
        _127 = 1.0f.xxx;
    }
    else
    {
        _127 = f_u_xlat16_1.xyz;
    }
    f_u_xlat16_2.x = _127.x;
    f_u_xlat16_2.y = _127.y;
    f_u_xlat16_2.z = _127.z;
    float _143;
    if (f_u_xlatb3)
    {
        _143 = f_u_xlat16_1.x;
    }
    else
    {
        _143 = f_u_xlat16_1.w;
    }
    f_u_xlat16_2.w = _143;
    f_u_xlat0 = f_u_xlat0.xxxx * f_u_xlat16_2;
    f_u_xlat16_0 = f_u_xlat0 * f_vs_COLOR0;
    float3 _168 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlat16_2.x = _168.x;
    f_u_xlat16_2.y = _168.y;
    f_u_xlat16_2.z = _168.z;
    f_u_xlatb1 = 0.0f < _HdrMultiply;
    float3 _181;
    if (f_u_xlatb1)
    {
        _181 = f_u_xlat16_2.xyz;
    }
    else
    {
        _181 = f_u_xlat16_0.xyz;
    }
    f_SV_Target0.x = _181.x;
    f_SV_Target0.y = _181.y;
    f_SV_Target0.z = _181.z;
    f_SV_Target0.w = min(f_u_xlat16_0.w, 1.0f);
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SOFTPARTICLES_ON) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_WORLD_SPACE_RING)
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;
float _RingTopOffset;
float4 _MainTex_ST;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float4 v_in_TEXCOORD2;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD2 : TEXCOORD2;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_vs_TEXCOORD2 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float2 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat6;
static float4 v_u_xlat2;
static float4 v_vs_TEXCOORD0;

void v_vert_main()
{
    v_u_xlat0 = v_in_TEXCOORD0.yy * (-v_in_TEXCOORD0.zw);
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_MatrixV)[1];
    v_u_xlat1 = (transpose(unity_MatrixV)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixV)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixV)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    v_u_xlat6 = min(v_in_TEXCOORD2.x, v_in_TEXCOORD2.w);
    v_u_xlat0 = (v_u_xlat0 * v_u_xlat6.xx) + v_u_xlat1.xy;
    v_u_xlat2 = v_u_xlat0.yyyy * transpose(glstate_matrix_projection)[1];
    v_u_xlat2 = (transpose(glstate_matrix_projection)[0] * v_u_xlat0.xxxx) + v_u_xlat2;
    v_u_xlat2 = (transpose(glstate_matrix_projection)[2] * v_u_xlat1.zzzz) + v_u_xlat2;
    v_gl_Position = (transpose(glstate_matrix_projection)[3] * v_u_xlat1.wwww) + v_u_xlat2;
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0;
    v_u_xlat0.x = v_u_xlat6 / v_in_TEXCOORD2.w;
    v_u_xlat0.x = (-v_u_xlat0.x) + 1.0f;
    v_vs_TEXCOORD1.x = v_u_xlat0.x * v_u_xlat0.x;
    v_vs_TEXCOORD1.z = v_in_TEXCOORD2.y / v_in_TEXCOORD2.w;
    v_vs_TEXCOORD1.y = (-_RingTopOffset) + 1.0f;
    v_vs_TEXCOORD1.w = v_in_TEXCOORD2.z;
    v_u_xlat0 = v_in_TEXCOORD0.yy * v_in_TEXCOORD0.zw;
    v_u_xlat0 = v_u_xlat6.xx * v_u_xlat0;
    v_u_xlat0 /= v_in_TEXCOORD2.ww;
    float2 _169 = (-v_u_xlat0) + v_in_TEXCOORD0.zw;
    v_vs_TEXCOORD2.z = _169.x;
    v_vs_TEXCOORD2.w = _169.y;
    v_u_xlat0.x = v_in_TEXCOORD2.z;
    v_u_xlat0.y = 0.0f;
    v_u_xlat0 += v_in_TEXCOORD0.xy;
    v_u_xlat0 = (v_u_xlat0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    float2 _194 = (-v_u_xlat0) + 1.0f.xx;
    v_vs_TEXCOORD2.x = _194.x;
    v_vs_TEXCOORD2.y = _194.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_TEXCOORD2 = stage_input.v_in_TEXCOORD2;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float4 f_vs_TEXCOORD1;
static float4 f_vs_TEXCOORD2;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD1 : TEXCOORD1;
    float4 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat0;
static float f_u_xlat3;
static float f_u_xlat6;
static float4 f_u_xlat16_1;
static bool f_u_xlatb3;
static float4 f_u_xlat16_2;
static float4 f_u_xlat16_0;
static bool f_u_xlatb1;

void f_frag_main()
{
    f_u_xlat0.x = (-f_vs_TEXCOORD1.z) + f_vs_TEXCOORD1.y;
    f_u_xlat3 = (-f_u_xlat0.x) + f_vs_TEXCOORD1.y;
    f_u_xlat3 = 1.0f / f_u_xlat3;
    f_u_xlat6 = dot(f_vs_TEXCOORD2.zw, f_vs_TEXCOORD2.zw);
    f_u_xlat0.x = (-f_u_xlat0.x) + f_u_xlat6;
    f_u_xlat6 += (-f_vs_TEXCOORD1.x);
    f_u_xlat0.x = f_u_xlat3 * f_u_xlat0.x;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat3 = (f_u_xlat0.x * (-2.0f)) + 3.0f;
    f_u_xlat0.x *= f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat3;
    f_u_xlat3 = 1.0f / f_vs_TEXCOORD1.z;
    f_u_xlat3 *= f_u_xlat6;
    f_u_xlat3 = clamp(f_u_xlat3, 0.0f, 1.0f);
    f_u_xlat6 = (f_u_xlat3 * (-2.0f)) + 3.0f;
    f_u_xlat3 *= f_u_xlat3;
    f_u_xlat0.x = (f_u_xlat6 * f_u_xlat3) + (-f_u_xlat0.x);
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD2.xy);
    f_u_xlatb3 = 0.0f < _SingleChannel;
    float3 _127;
    if (f_u_xlatb3)
    {
        _127 = 1.0f.xxx;
    }
    else
    {
        _127 = f_u_xlat16_1.xyz;
    }
    f_u_xlat16_2.x = _127.x;
    f_u_xlat16_2.y = _127.y;
    f_u_xlat16_2.z = _127.z;
    float _143;
    if (f_u_xlatb3)
    {
        _143 = f_u_xlat16_1.x;
    }
    else
    {
        _143 = f_u_xlat16_1.w;
    }
    f_u_xlat16_2.w = _143;
    f_u_xlat0 = f_u_xlat0.xxxx * f_u_xlat16_2;
    f_u_xlat16_0 = f_u_xlat0 * f_vs_COLOR0;
    float3 _168 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlat16_2.x = _168.x;
    f_u_xlat16_2.y = _168.y;
    f_u_xlat16_2.z = _168.z;
    f_u_xlatb1 = 0.0f < _HdrMultiply;
    float3 _181;
    if (f_u_xlatb1)
    {
        _181 = f_u_xlat16_2.xyz;
    }
    else
    {
        _181 = f_u_xlat16_0.xyz;
    }
    f_SV_Target0.x = _181.x;
    f_SV_Target0.y = _181.y;
    f_SV_Target0.z = _181.z;
    f_SV_Target0.w = min(f_u_xlat16_0.w, 1.0f);
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
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
Name "ShadowCaster"
Tags { "IgnoreProjector"="true" "LightMode"="ShadowCaster" "Queue"="Transparent" "RenderType"="Transparent" "SHADOWSUPPORT"="true" }
Cull Off
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
#pragma shader_feature_local _ _CFXR_DISSOLVE
#pragma shader_feature_local _ _CFXR_WORLD_SPACE_RING
#if defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && defined(_CFXR_DISSOLVE) && defined(_CFXR_WORLD_SPACE_RING)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4x4 unity_MatrixVP;
float _RingTopOffset;
float4 _MainTex_ST;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _InvertDissolveTex;
Texture2D<float4> _DissolveTex;
SamplerState sampler_DissolveTex;
float _DissolveSmooth;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD2;
static float4 v_in_TEXCOORD2;
static float4 v_vs_TEXCOORD3;
static float4 v_vs_TEXCOORD4;
static float4 v_in_TEXCOORD1;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float4 v_in_TEXCOORD2 : TEXCOORD2;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD2 : TEXCOORD1;
    float4 v_vs_TEXCOORD3 : TEXCOORD2;
    float4 v_vs_TEXCOORD4 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_vs_TEXCOORD1;
static float4 v_u_xlat0;
static float3 v_u_xlat3;
static float4 v_u_xlat1;
static float v_u_xlat10;
static float3 v_u_xlat2;
static bool v_u_xlatb10;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0;
    v_vs_TEXCOORD2.z = v_in_TEXCOORD2.y / v_in_TEXCOORD2.w;
    v_vs_TEXCOORD2.y = (-_RingTopOffset) + 1.0f;
    v_vs_TEXCOORD2.w = v_in_TEXCOORD2.z;
    v_u_xlat0.x = min(v_in_TEXCOORD2.x, v_in_TEXCOORD2.w);
    v_u_xlat3.x = v_u_xlat0.x / v_in_TEXCOORD2.w;
    v_u_xlat3.x = (-v_u_xlat3.x) + 1.0f;
    v_vs_TEXCOORD2.x = v_u_xlat3.x * v_u_xlat3.x;
    v_u_xlat1.x = v_in_TEXCOORD2.z;
    v_u_xlat1.y = 0.0f;
    float2 _81 = v_u_xlat1.xy + v_in_TEXCOORD0.xy;
    v_u_xlat3.x = _81.x;
    v_u_xlat3.y = _81.y;
    float3 _86 = v_u_xlat3;
    float2 _95 = (_86.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat3.x = _95.x;
    v_u_xlat3.y = _95.y;
    float2 _105 = (-v_u_xlat3.xy) + 1.0f.xx;
    v_vs_TEXCOORD3.x = _105.x;
    v_vs_TEXCOORD3.y = _105.y;
    float2 _114 = v_in_TEXCOORD0.yy * v_in_TEXCOORD0.zw;
    v_u_xlat3.x = _114.x;
    v_u_xlat3.y = _114.y;
    float3 _121 = v_u_xlat3;
    float2 _123 = v_u_xlat0.xx * _121.xy;
    v_u_xlat3.x = _123.x;
    v_u_xlat3.y = _123.y;
    float3 _128 = v_u_xlat3;
    float2 _132 = _128.xy / v_in_TEXCOORD2.ww;
    v_u_xlat3.x = _132.x;
    v_u_xlat3.y = _132.y;
    float2 _142 = (-v_u_xlat3.xy) + v_in_TEXCOORD0.zw;
    v_vs_TEXCOORD3.z = _142.x;
    v_vs_TEXCOORD3.w = _142.y;
    v_vs_TEXCOORD4 = v_in_TEXCOORD1;
    v_u_xlat3 = v_in_TEXCOORD0.yyy * v_in_NORMAL0;
    float4 _158 = v_u_xlat0;
    float3 _164 = ((-v_u_xlat3) * _158.xxx) + v_in_POSITION0.xyz;
    v_u_xlat0.x = _164.x;
    v_u_xlat0.y = _164.y;
    v_u_xlat0.z = _164.z;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    float3 _215 = ((-v_u_xlat0.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat1.x = _215.x;
    v_u_xlat1.y = _215.y;
    v_u_xlat1.z = _215.z;
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    float4 _232 = v_u_xlat1;
    float3 _234 = v_u_xlat10.xxx * _232.xyz;
    v_u_xlat1.x = _234.x;
    v_u_xlat1.y = _234.y;
    v_u_xlat1.z = _234.z;
    v_u_xlat2.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat2.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat2.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat2, v_u_xlat2);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_u_xlat2 = v_u_xlat10.xxx * v_u_xlat2;
    v_u_xlat1.x = dot(v_u_xlat2, v_u_xlat1.xyz);
    v_u_xlat1.x = ((-v_u_xlat1.x) * v_u_xlat1.x) + 1.0f;
    v_u_xlat1.x = sqrt(v_u_xlat1.x);
    v_u_xlat1.x *= unity_LightShadowBias.z;
    float4 _296 = v_u_xlat1;
    float3 _301 = ((-v_u_xlat2) * _296.xxx) + v_u_xlat0.xyz;
    v_u_xlat1.x = _301.x;
    v_u_xlat1.y = _301.y;
    v_u_xlat1.z = _301.z;
    v_u_xlatb10 = unity_LightShadowBias.z != 0.0f;
    float3 _316;
    if (v_u_xlatb10)
    {
        _316 = v_u_xlat1.xyz;
    }
    else
    {
        _316 = v_u_xlat0.xyz;
    }
    v_u_xlat0.x = _316.x;
    v_u_xlat0.y = _316.y;
    v_u_xlat0.z = _316.z;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_u_xlat0 = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_u_xlat1.x = max(-v_u_xlat0.w, v_u_xlat0.z);
    v_u_xlat1.x = (-v_u_xlat0.z) + v_u_xlat1.x;
    v_gl_Position.z = (unity_LightShadowBias.y * v_u_xlat1.x) + v_u_xlat0.z;
    v_gl_Position.x = v_u_xlat0.xyw.x;
    v_gl_Position.y = v_u_xlat0.xyw.y;
    v_gl_Position.w = v_u_xlat0.xyw.z;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD2 = stage_input.v_in_TEXCOORD2;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float4 f_vs_TEXCOORD2;
static float4 f_vs_TEXCOORD3;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD4;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD2 : TEXCOORD1;
    float4 f_vs_TEXCOORD3 : TEXCOORD2;
    float4 f_vs_TEXCOORD4 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float f_u_xlat0;
static float f_u_xlat2;
static float f_u_xlat4;
static float2 f_u_xlat16_2;
static bool f_u_xlatb6;
static float f_u_xlat16_1;
static bool f_u_xlatb0;
static float f_u_xlat16_3;
static float f_u_xlat16_5;
static float f_u_xlat16_7;

void f_frag_main()
{
    f_u_xlat0 = (-f_vs_TEXCOORD2.z) + f_vs_TEXCOORD2.y;
    f_u_xlat2 = (-f_u_xlat0) + f_vs_TEXCOORD2.y;
    f_u_xlat2 = 1.0f / f_u_xlat2;
    f_u_xlat4 = dot(f_vs_TEXCOORD3.zw, f_vs_TEXCOORD3.zw);
    f_u_xlat0 = (-f_u_xlat0) + f_u_xlat4;
    f_u_xlat4 += (-f_vs_TEXCOORD2.x);
    f_u_xlat0 = f_u_xlat2 * f_u_xlat0;
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f, 1.0f);
    f_u_xlat2 = (f_u_xlat0 * (-2.0f)) + 3.0f;
    f_u_xlat0 *= f_u_xlat0;
    f_u_xlat0 *= f_u_xlat2;
    f_u_xlat2 = 1.0f / f_vs_TEXCOORD2.z;
    f_u_xlat2 *= f_u_xlat4;
    f_u_xlat2 = clamp(f_u_xlat2, 0.0f, 1.0f);
    f_u_xlat4 = (f_u_xlat2 * (-2.0f)) + 3.0f;
    f_u_xlat2 *= f_u_xlat2;
    f_u_xlat0 = (f_u_xlat4 * f_u_xlat2) + (-f_u_xlat0);
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f, 1.0f);
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD3.xy).xw;
    f_u_xlatb6 = 0.0f < _SingleChannel;
    float _109;
    if (f_u_xlatb6)
    {
        _109 = f_u_xlat16_2.x;
    }
    else
    {
        _109 = f_u_xlat16_2.y;
    }
    f_u_xlat16_1 = _109;
    f_u_xlat0 *= f_u_xlat16_1;
    f_u_xlat16_1 = f_u_xlat0 * f_vs_COLOR0.w;
    f_u_xlatb0 = 0.0f >= _InvertDissolveTex;
    f_u_xlat16_2.x = _DissolveTex.Sample(sampler_DissolveTex, f_vs_TEXCOORD3.xy).x;
    f_u_xlat16_3 = (-f_u_xlat16_2.x) + 1.0f;
    float _144;
    if (f_u_xlatb0)
    {
        _144 = f_u_xlat16_3;
    }
    else
    {
        _144 = f_u_xlat16_2.x;
    }
    f_u_xlat16_3 = _144;
    f_u_xlat16_5 = f_u_xlat16_3 + _DissolveSmooth;
    f_u_xlat16_3 += (-_DissolveSmooth);
    f_u_xlat16_5 = (-f_u_xlat16_3) + f_u_xlat16_5;
    f_u_xlat16_5 = 1.0f / f_u_xlat16_5;
    f_u_xlat16_7 = (_DissolveSmooth * 2.0f) + 1.0f;
    f_u_xlat16_7 = (f_vs_TEXCOORD4.x * f_u_xlat16_7) + (-_DissolveSmooth);
    f_u_xlat16_3 = (-f_u_xlat16_3) + f_u_xlat16_7;
    f_u_xlat16_3 = f_u_xlat16_5 * f_u_xlat16_3;
    f_u_xlat16_3 = clamp(f_u_xlat16_3, 0.0f, 1.0f);
    f_u_xlat16_5 = (f_u_xlat16_3 * (-2.0f)) + 3.0f;
    f_u_xlat16_3 *= f_u_xlat16_3;
    f_u_xlat16_3 *= f_u_xlat16_5;
    f_u_xlat16_1 = f_u_xlat16_3 * f_u_xlat16_1;
    f_u_xlat16_1 = min(f_u_xlat16_1, 1.0f);
    f_u_xlat16_1 += (-0.00999999977648258209228515625f);
    f_u_xlatb0 = f_u_xlat16_1 < 0.0f;
    if (f_u_xlatb0)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SHADOWS_CUBE) && defined(SHADOWS_DEPTH) && defined(_CFXR_DISSOLVE) && defined(_CFXR_WORLD_SPACE_RING)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4x4 unity_MatrixVP;
float _RingTopOffset;
float4 _MainTex_ST;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _InvertDissolveTex;
Texture2D<float4> _DissolveTex;
SamplerState sampler_DissolveTex;
float _DissolveSmooth;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD2;
static float4 v_in_TEXCOORD2;
static float4 v_vs_TEXCOORD3;
static float4 v_vs_TEXCOORD4;
static float4 v_in_TEXCOORD1;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float4 v_in_TEXCOORD2 : TEXCOORD2;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD2 : TEXCOORD1;
    float4 v_vs_TEXCOORD3 : TEXCOORD2;
    float4 v_vs_TEXCOORD4 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_vs_TEXCOORD1;
static float4 v_u_xlat0;
static float3 v_u_xlat3;
static float4 v_u_xlat1;
static float v_u_xlat10;
static float3 v_u_xlat2;
static bool v_u_xlatb10;
static float v_u_xlat6;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0;
    v_vs_TEXCOORD2.z = v_in_TEXCOORD2.y / v_in_TEXCOORD2.w;
    v_vs_TEXCOORD2.y = (-_RingTopOffset) + 1.0f;
    v_vs_TEXCOORD2.w = v_in_TEXCOORD2.z;
    v_u_xlat0.x = min(v_in_TEXCOORD2.x, v_in_TEXCOORD2.w);
    v_u_xlat3.x = v_u_xlat0.x / v_in_TEXCOORD2.w;
    v_u_xlat3.x = (-v_u_xlat3.x) + 1.0f;
    v_vs_TEXCOORD2.x = v_u_xlat3.x * v_u_xlat3.x;
    v_u_xlat1.x = v_in_TEXCOORD2.z;
    v_u_xlat1.y = 0.0f;
    float2 _81 = v_u_xlat1.xy + v_in_TEXCOORD0.xy;
    v_u_xlat3.x = _81.x;
    v_u_xlat3.y = _81.y;
    float3 _86 = v_u_xlat3;
    float2 _95 = (_86.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat3.x = _95.x;
    v_u_xlat3.y = _95.y;
    float2 _105 = (-v_u_xlat3.xy) + 1.0f.xx;
    v_vs_TEXCOORD3.x = _105.x;
    v_vs_TEXCOORD3.y = _105.y;
    float2 _114 = v_in_TEXCOORD0.yy * v_in_TEXCOORD0.zw;
    v_u_xlat3.x = _114.x;
    v_u_xlat3.y = _114.y;
    float3 _121 = v_u_xlat3;
    float2 _123 = v_u_xlat0.xx * _121.xy;
    v_u_xlat3.x = _123.x;
    v_u_xlat3.y = _123.y;
    float3 _128 = v_u_xlat3;
    float2 _132 = _128.xy / v_in_TEXCOORD2.ww;
    v_u_xlat3.x = _132.x;
    v_u_xlat3.y = _132.y;
    float2 _142 = (-v_u_xlat3.xy) + v_in_TEXCOORD0.zw;
    v_vs_TEXCOORD3.z = _142.x;
    v_vs_TEXCOORD3.w = _142.y;
    v_vs_TEXCOORD4 = v_in_TEXCOORD1;
    v_u_xlat3 = v_in_TEXCOORD0.yyy * v_in_NORMAL0;
    float4 _158 = v_u_xlat0;
    float3 _164 = ((-v_u_xlat3) * _158.xxx) + v_in_POSITION0.xyz;
    v_u_xlat0.x = _164.x;
    v_u_xlat0.y = _164.y;
    v_u_xlat0.z = _164.z;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat0;
    float3 _215 = ((-v_u_xlat0.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat1.x = _215.x;
    v_u_xlat1.y = _215.y;
    v_u_xlat1.z = _215.z;
    v_u_xlat10 = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    float4 _232 = v_u_xlat1;
    float3 _234 = v_u_xlat10.xxx * _232.xyz;
    v_u_xlat1.x = _234.x;
    v_u_xlat1.y = _234.y;
    v_u_xlat1.z = _234.z;
    v_u_xlat2.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat2.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat2.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat10 = dot(v_u_xlat2, v_u_xlat2);
    v_u_xlat10 = rsqrt(v_u_xlat10);
    v_u_xlat2 = v_u_xlat10.xxx * v_u_xlat2;
    v_u_xlat1.x = dot(v_u_xlat2, v_u_xlat1.xyz);
    v_u_xlat1.x = ((-v_u_xlat1.x) * v_u_xlat1.x) + 1.0f;
    v_u_xlat1.x = sqrt(v_u_xlat1.x);
    v_u_xlat1.x *= unity_LightShadowBias.z;
    float4 _296 = v_u_xlat1;
    float3 _301 = ((-v_u_xlat2) * _296.xxx) + v_u_xlat0.xyz;
    v_u_xlat1.x = _301.x;
    v_u_xlat1.y = _301.y;
    v_u_xlat1.z = _301.z;
    v_u_xlatb10 = unity_LightShadowBias.z != 0.0f;
    float3 _316;
    if (v_u_xlatb10)
    {
        _316 = v_u_xlat1.xyz;
    }
    else
    {
        _316 = v_u_xlat0.xyz;
    }
    v_u_xlat0.x = _316.x;
    v_u_xlat0.y = _316.y;
    v_u_xlat0.z = _316.z;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_u_xlat0 = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_u_xlat1.x = unity_LightShadowBias.x / v_u_xlat0.w;
    v_u_xlat1.x = clamp(v_u_xlat1.x, 0.0f, 1.0f);
    v_u_xlat6 = v_u_xlat0.z + v_u_xlat1.x;
    v_u_xlat1.x = max(-v_u_xlat0.w, v_u_xlat6);
    v_gl_Position.x = v_u_xlat0.xyw.x;
    v_gl_Position.y = v_u_xlat0.xyw.y;
    v_gl_Position.w = v_u_xlat0.xyw.z;
    v_u_xlat0.x = (-v_u_xlat6) + v_u_xlat1.x;
    v_gl_Position.z = (unity_LightShadowBias.y * v_u_xlat0.x) + v_u_xlat6;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD2 = stage_input.v_in_TEXCOORD2;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float4 f_vs_TEXCOORD2;
static float4 f_vs_TEXCOORD3;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD4;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD2 : TEXCOORD1;
    float4 f_vs_TEXCOORD3 : TEXCOORD2;
    float4 f_vs_TEXCOORD4 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float f_u_xlat0;
static float f_u_xlat2;
static float f_u_xlat4;
static float2 f_u_xlat16_2;
static bool f_u_xlatb6;
static float f_u_xlat16_1;
static bool f_u_xlatb0;
static float f_u_xlat16_3;
static float f_u_xlat16_5;
static float f_u_xlat16_7;

void f_frag_main()
{
    f_u_xlat0 = (-f_vs_TEXCOORD2.z) + f_vs_TEXCOORD2.y;
    f_u_xlat2 = (-f_u_xlat0) + f_vs_TEXCOORD2.y;
    f_u_xlat2 = 1.0f / f_u_xlat2;
    f_u_xlat4 = dot(f_vs_TEXCOORD3.zw, f_vs_TEXCOORD3.zw);
    f_u_xlat0 = (-f_u_xlat0) + f_u_xlat4;
    f_u_xlat4 += (-f_vs_TEXCOORD2.x);
    f_u_xlat0 = f_u_xlat2 * f_u_xlat0;
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f, 1.0f);
    f_u_xlat2 = (f_u_xlat0 * (-2.0f)) + 3.0f;
    f_u_xlat0 *= f_u_xlat0;
    f_u_xlat0 *= f_u_xlat2;
    f_u_xlat2 = 1.0f / f_vs_TEXCOORD2.z;
    f_u_xlat2 *= f_u_xlat4;
    f_u_xlat2 = clamp(f_u_xlat2, 0.0f, 1.0f);
    f_u_xlat4 = (f_u_xlat2 * (-2.0f)) + 3.0f;
    f_u_xlat2 *= f_u_xlat2;
    f_u_xlat0 = (f_u_xlat4 * f_u_xlat2) + (-f_u_xlat0);
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f, 1.0f);
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD3.xy).xw;
    f_u_xlatb6 = 0.0f < _SingleChannel;
    float _109;
    if (f_u_xlatb6)
    {
        _109 = f_u_xlat16_2.x;
    }
    else
    {
        _109 = f_u_xlat16_2.y;
    }
    f_u_xlat16_1 = _109;
    f_u_xlat0 *= f_u_xlat16_1;
    f_u_xlat16_1 = f_u_xlat0 * f_vs_COLOR0.w;
    f_u_xlatb0 = 0.0f >= _InvertDissolveTex;
    f_u_xlat16_2.x = _DissolveTex.Sample(sampler_DissolveTex, f_vs_TEXCOORD3.xy).x;
    f_u_xlat16_3 = (-f_u_xlat16_2.x) + 1.0f;
    float _144;
    if (f_u_xlatb0)
    {
        _144 = f_u_xlat16_3;
    }
    else
    {
        _144 = f_u_xlat16_2.x;
    }
    f_u_xlat16_3 = _144;
    f_u_xlat16_5 = f_u_xlat16_3 + _DissolveSmooth;
    f_u_xlat16_3 += (-_DissolveSmooth);
    f_u_xlat16_5 = (-f_u_xlat16_3) + f_u_xlat16_5;
    f_u_xlat16_5 = 1.0f / f_u_xlat16_5;
    f_u_xlat16_7 = (_DissolveSmooth * 2.0f) + 1.0f;
    f_u_xlat16_7 = (f_vs_TEXCOORD4.x * f_u_xlat16_7) + (-_DissolveSmooth);
    f_u_xlat16_3 = (-f_u_xlat16_3) + f_u_xlat16_7;
    f_u_xlat16_3 = f_u_xlat16_5 * f_u_xlat16_3;
    f_u_xlat16_3 = clamp(f_u_xlat16_3, 0.0f, 1.0f);
    f_u_xlat16_5 = (f_u_xlat16_3 * (-2.0f)) + 3.0f;
    f_u_xlat16_3 *= f_u_xlat16_3;
    f_u_xlat16_3 *= f_u_xlat16_5;
    f_u_xlat16_1 = f_u_xlat16_3 * f_u_xlat16_1;
    f_u_xlat16_1 = min(f_u_xlat16_1, 1.0f);
    f_u_xlat16_1 += (-0.00999999977648258209228515625f);
    f_u_xlatb0 = f_u_xlat16_1 < 0.0f;
    if (f_u_xlatb0)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SHADOWS_CUBE) && defined(SHADOWS_DEPTH) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_WORLD_SPACE_RING)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float _RingTopOffset;
float4 _MainTex_ST;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_in_TEXCOORD0;
static float4 v_in_TEXCOORD2;
static float4 v_vs_TEXCOORD2;
static float4 v_vs_TEXCOORD3;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD2 : TEXCOORD2;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD2 : TEXCOORD1;
    float4 v_vs_TEXCOORD3 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_vs_TEXCOORD1;
static float4 v_u_xlat0;
static float2 v_u_xlat3;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;
static float v_u_xlat6;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0;
    v_u_xlat0.x = min(v_in_TEXCOORD2.x, v_in_TEXCOORD2.w);
    v_u_xlat3.x = v_u_xlat0.x / v_in_TEXCOORD2.w;
    v_u_xlat3.x = (-v_u_xlat3.x) + 1.0f;
    v_vs_TEXCOORD2.x = v_u_xlat3.x * v_u_xlat3.x;
    v_vs_TEXCOORD2.z = v_in_TEXCOORD2.y / v_in_TEXCOORD2.w;
    v_vs_TEXCOORD2.y = (-_RingTopOffset) + 1.0f;
    v_vs_TEXCOORD2.w = v_in_TEXCOORD2.z;
    v_u_xlat3 = v_in_TEXCOORD0.yy * v_in_TEXCOORD0.zw;
    float4 _75 = v_u_xlat0;
    float2 _78 = _75.xx * v_u_xlat3;
    v_u_xlat0.x = _78.x;
    v_u_xlat0.y = _78.y;
    float4 _83 = v_u_xlat0;
    float2 _87 = _83.xy / v_in_TEXCOORD2.ww;
    v_u_xlat0.x = _87.x;
    v_u_xlat0.y = _87.y;
    float2 _98 = (-v_u_xlat0.xy) + v_in_TEXCOORD0.zw;
    v_vs_TEXCOORD3.z = _98.x;
    v_vs_TEXCOORD3.w = _98.y;
    v_u_xlat0.x = v_in_TEXCOORD2.z;
    v_u_xlat0.y = 0.0f;
    float4 _108 = v_u_xlat0;
    float2 _112 = _108.xy + v_in_TEXCOORD0.xy;
    v_u_xlat0.x = _112.x;
    v_u_xlat0.y = _112.y;
    float4 _117 = v_u_xlat0;
    float2 _126 = (_117.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = _126.x;
    v_u_xlat0.y = _126.y;
    float2 _135 = (-v_u_xlat0.xy) + 1.0f.xx;
    v_vs_TEXCOORD3.x = _135.x;
    v_vs_TEXCOORD3.y = _135.y;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _179 = v_u_xlat0;
    float3 _181 = v_u_xlat9.xxx * _179.xyz;
    v_u_xlat0.x = _181.x;
    v_u_xlat0.y = _181.y;
    v_u_xlat0.z = _181.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _228 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _228.x;
    v_u_xlat2.y = _228.y;
    v_u_xlat2.z = _228.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _244 = v_u_xlat2;
    float3 _246 = v_u_xlat9.xxx * _244.xyz;
    v_u_xlat2.x = _246.x;
    v_u_xlat2.y = _246.y;
    v_u_xlat2.z = _246.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _270 = v_u_xlat0;
    float3 _278 = ((-_270.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _278.x;
    v_u_xlat0.y = _278.y;
    v_u_xlat0.z = _278.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _293;
    if (v_u_xlatb9)
    {
        _293 = v_u_xlat0.xyz;
    }
    else
    {
        _293 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _293.x;
    v_u_xlat0.y = _293.y;
    v_u_xlat0.z = _293.z;
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
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD2 = stage_input.v_in_TEXCOORD2;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float4 f_vs_TEXCOORD2;
static float4 f_vs_TEXCOORD3;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD2 : TEXCOORD1;
    float4 f_vs_TEXCOORD3 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float f_u_xlat0;
static float f_u_xlat2;
static float f_u_xlat4;
static float2 f_u_xlat16_2;
static bool f_u_xlatb6;
static float f_u_xlat16_1;
static bool f_u_xlatb0;

void f_frag_main()
{
    f_u_xlat0 = (-f_vs_TEXCOORD2.z) + f_vs_TEXCOORD2.y;
    f_u_xlat2 = (-f_u_xlat0) + f_vs_TEXCOORD2.y;
    f_u_xlat2 = 1.0f / f_u_xlat2;
    f_u_xlat4 = dot(f_vs_TEXCOORD3.zw, f_vs_TEXCOORD3.zw);
    f_u_xlat0 = (-f_u_xlat0) + f_u_xlat4;
    f_u_xlat4 += (-f_vs_TEXCOORD2.x);
    f_u_xlat0 = f_u_xlat2 * f_u_xlat0;
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f, 1.0f);
    f_u_xlat2 = (f_u_xlat0 * (-2.0f)) + 3.0f;
    f_u_xlat0 *= f_u_xlat0;
    f_u_xlat0 *= f_u_xlat2;
    f_u_xlat2 = 1.0f / f_vs_TEXCOORD2.z;
    f_u_xlat2 *= f_u_xlat4;
    f_u_xlat2 = clamp(f_u_xlat2, 0.0f, 1.0f);
    f_u_xlat4 = (f_u_xlat2 * (-2.0f)) + 3.0f;
    f_u_xlat2 *= f_u_xlat2;
    f_u_xlat0 = (f_u_xlat4 * f_u_xlat2) + (-f_u_xlat0);
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f, 1.0f);
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD3.xy).xw;
    f_u_xlatb6 = 0.0f < _SingleChannel;
    float _109;
    if (f_u_xlatb6)
    {
        _109 = f_u_xlat16_2.x;
    }
    else
    {
        _109 = f_u_xlat16_2.y;
    }
    f_u_xlat16_1 = _109;
    f_u_xlat0 *= f_u_xlat16_1;
    f_u_xlat16_1 = f_u_xlat0 * f_vs_COLOR0.w;
    f_u_xlat16_1 = min(f_u_xlat16_1, 1.0f);
    f_u_xlat16_1 += (-0.00999999977648258209228515625f);
    f_u_xlatb0 = f_u_xlat16_1 < 0.0f;
    if (f_u_xlatb0)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_WORLD_SPACE_RING)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float _RingTopOffset;
float4 _MainTex_ST;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_in_TEXCOORD0;
static float4 v_in_TEXCOORD2;
static float4 v_vs_TEXCOORD2;
static float4 v_vs_TEXCOORD3;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD2 : TEXCOORD2;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD2 : TEXCOORD1;
    float4 v_vs_TEXCOORD3 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_vs_TEXCOORD1;
static float4 v_u_xlat0;
static float2 v_u_xlat3;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0;
    v_u_xlat0.x = min(v_in_TEXCOORD2.x, v_in_TEXCOORD2.w);
    v_u_xlat3.x = v_u_xlat0.x / v_in_TEXCOORD2.w;
    v_u_xlat3.x = (-v_u_xlat3.x) + 1.0f;
    v_vs_TEXCOORD2.x = v_u_xlat3.x * v_u_xlat3.x;
    v_vs_TEXCOORD2.z = v_in_TEXCOORD2.y / v_in_TEXCOORD2.w;
    v_vs_TEXCOORD2.y = (-_RingTopOffset) + 1.0f;
    v_vs_TEXCOORD2.w = v_in_TEXCOORD2.z;
    v_u_xlat3 = v_in_TEXCOORD0.yy * v_in_TEXCOORD0.zw;
    float4 _75 = v_u_xlat0;
    float2 _78 = _75.xx * v_u_xlat3;
    v_u_xlat0.x = _78.x;
    v_u_xlat0.y = _78.y;
    float4 _83 = v_u_xlat0;
    float2 _87 = _83.xy / v_in_TEXCOORD2.ww;
    v_u_xlat0.x = _87.x;
    v_u_xlat0.y = _87.y;
    float2 _98 = (-v_u_xlat0.xy) + v_in_TEXCOORD0.zw;
    v_vs_TEXCOORD3.z = _98.x;
    v_vs_TEXCOORD3.w = _98.y;
    v_u_xlat0.x = v_in_TEXCOORD2.z;
    v_u_xlat0.y = 0.0f;
    float4 _108 = v_u_xlat0;
    float2 _112 = _108.xy + v_in_TEXCOORD0.xy;
    v_u_xlat0.x = _112.x;
    v_u_xlat0.y = _112.y;
    float4 _117 = v_u_xlat0;
    float2 _126 = (_117.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_u_xlat0.x = _126.x;
    v_u_xlat0.y = _126.y;
    float2 _135 = (-v_u_xlat0.xy) + 1.0f.xx;
    v_vs_TEXCOORD3.x = _135.x;
    v_vs_TEXCOORD3.y = _135.y;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _179 = v_u_xlat0;
    float3 _181 = v_u_xlat9.xxx * _179.xyz;
    v_u_xlat0.x = _181.x;
    v_u_xlat0.y = _181.y;
    v_u_xlat0.z = _181.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _228 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _228.x;
    v_u_xlat2.y = _228.y;
    v_u_xlat2.z = _228.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _244 = v_u_xlat2;
    float3 _246 = v_u_xlat9.xxx * _244.xyz;
    v_u_xlat2.x = _246.x;
    v_u_xlat2.y = _246.y;
    v_u_xlat2.z = _246.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _270 = v_u_xlat0;
    float3 _278 = ((-_270.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _278.x;
    v_u_xlat0.y = _278.y;
    v_u_xlat0.z = _278.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _293;
    if (v_u_xlatb9)
    {
        _293 = v_u_xlat0.xyz;
    }
    else
    {
        _293 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _293.x;
    v_u_xlat0.y = _293.y;
    v_u_xlat0.z = _293.z;
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
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD2 = stage_input.v_in_TEXCOORD2;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float4 f_vs_TEXCOORD2;
static float4 f_vs_TEXCOORD3;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD2 : TEXCOORD1;
    float4 f_vs_TEXCOORD3 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float f_u_xlat0;
static float f_u_xlat2;
static float f_u_xlat4;
static float2 f_u_xlat16_2;
static bool f_u_xlatb6;
static float f_u_xlat16_1;
static bool f_u_xlatb0;

void f_frag_main()
{
    f_u_xlat0 = (-f_vs_TEXCOORD2.z) + f_vs_TEXCOORD2.y;
    f_u_xlat2 = (-f_u_xlat0) + f_vs_TEXCOORD2.y;
    f_u_xlat2 = 1.0f / f_u_xlat2;
    f_u_xlat4 = dot(f_vs_TEXCOORD3.zw, f_vs_TEXCOORD3.zw);
    f_u_xlat0 = (-f_u_xlat0) + f_u_xlat4;
    f_u_xlat4 += (-f_vs_TEXCOORD2.x);
    f_u_xlat0 = f_u_xlat2 * f_u_xlat0;
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f, 1.0f);
    f_u_xlat2 = (f_u_xlat0 * (-2.0f)) + 3.0f;
    f_u_xlat0 *= f_u_xlat0;
    f_u_xlat0 *= f_u_xlat2;
    f_u_xlat2 = 1.0f / f_vs_TEXCOORD2.z;
    f_u_xlat2 *= f_u_xlat4;
    f_u_xlat2 = clamp(f_u_xlat2, 0.0f, 1.0f);
    f_u_xlat4 = (f_u_xlat2 * (-2.0f)) + 3.0f;
    f_u_xlat2 *= f_u_xlat2;
    f_u_xlat0 = (f_u_xlat4 * f_u_xlat2) + (-f_u_xlat0);
    f_u_xlat0 = clamp(f_u_xlat0, 0.0f, 1.0f);
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD3.xy).xw;
    f_u_xlatb6 = 0.0f < _SingleChannel;
    float _109;
    if (f_u_xlatb6)
    {
        _109 = f_u_xlat16_2.x;
    }
    else
    {
        _109 = f_u_xlat16_2.y;
    }
    f_u_xlat16_1 = _109;
    f_u_xlat0 *= f_u_xlat16_1;
    f_u_xlat16_1 = f_u_xlat0 * f_vs_COLOR0.w;
    f_u_xlat16_1 = min(f_u_xlat16_1, 1.0f);
    f_u_xlat16_1 += (-0.00999999977648258209228515625f);
    f_u_xlatb0 = f_u_xlat16_1 < 0.0f;
    if (f_u_xlatb0)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
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
