Shader "Cartoon FX/Remaster/Particle Ubershader" {
Properties {
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Blend Source", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Blend Destination", Float) = 10
		[KeywordEnumNoPrefix(Alpha Blending, _ALPHABLEND_ON, Alpha Blending Premultiplied, _ALPHAPREMULTIPLY_ON, Multiplicative, _ALPHAMODULATE_ON, Additive, _CFXR_ADDITIVE)] _BlendingType ("Blending Type", Float) = 0
		[ToggleNoKeyword] _ZWrite ("Depth Write", Float) = 0
		[Toggle(_ALPHATEST_ON)] _UseAlphaClip ("Alpha Clipping (Cutout)", Float) = 0
		_Cutoff ("Cutoff Threshold", Range(0.001, 1)) = 0.1
		[Toggle(_FADING_ON)] _UseSP ("Soft Particles", Float) = 0
		_SoftParticlesFadeDistanceNear ("Near Fade", Float) = 0
		_SoftParticlesFadeDistanceFar ("Far Fade", Float) = 1
		[Toggle(_CFXR_EDGE_FADING)] _UseEF ("Edge Fade", Float) = 0
		_EdgeFadePow ("Edge Fade Power", Float) = 1
		[Toggle(_CFXR_DISSOLVE)] _UseDissolve ("Enable Dissolve", Float) = 0
		_DissolveTex ("Dissolve Texture", 2D) = "gray" {}
		_DissolveSmooth ("Dissolve Smoothing", Range(0.0001, 0.5)) = 0.1
		[ToggleNoKeyword] _InvertDissolveTex ("Invert Dissolve Texture", Float) = 0
		[ToggleNoKeyword] _DoubleDissolve ("Double Dissolve", Float) = 0
		[Toggle] _UseDissolveOffsetUV ("Dissolve offset along X", Float) = 0
		_DissolveScroll ("UV Scrolling", Vector) = (0,0,0,0)
		[Toggle(_CFXR_UV_DISTORTION)] _UseUVDistortion ("Enable UV Distortion", Float) = 0
		[NoScaleOffset] _DistortTex ("Distortion Texture", 2D) = "gray" {}
		_DistortScrolling ("Scroll (XY) Tile (ZW)", Vector) = (0,0,1,1)
		[Toggle] _UseUV2Distortion ("Use UV2", Float) = 0
		_Distort ("Distortion Strength", Range(0, 2)) = 0.1
		[ToggleNoKeyword] _FadeAlongU ("Fade along Y", Float) = 0
		[Toggle] _UVDistortionAdd ("Add to base UV", Float) = 0
		[NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
		[Toggle] _SingleChannel ("Single Channel Texture", Float) = 0
		[KeywordEnum(Off,1x,2x)] _CFXR_OVERLAYTEX ("Enable Overlay Texture", Float) = 0
		[Enum(RGBA,0,RGB,1,A,2)] _CFXR_OVERLAYBLEND ("Overlay Blend Channels", Float) = 0
		[NoScaleOffset] _OverlayTex ("Overlay Texture", 2D) = "white" {}
		_OverlayTex_Scroll ("Overlay Scrolling / Scale", Vector) = (0.1,0.1,1,1)
		[Toggle(_FLIPBOOK_BLENDING)] _UseFB ("Flipbook Blending", Float) = 0
		[Toggle(_CFXR_SECONDCOLOR_LERP)] _UseSecondColor ("Secondary Vertex Color (TEXCOORD2)", Float) = 0
		[NoScaleOffset] _SecondColorTex ("Second Color Map", 2D) = "black" {}
		_SecondColorSmooth ("Second Color Smoothing", Range(0.0001, 0.5)) = 0.2
		[Toggle(_CFXR_FONT_COLORS)] _UseFontColor ("Use Font Colors", Float) = 0
		_HdrMultiply ("HDR Multiplier", Float) = 1
		[KeywordEnumNoPrefix(Off, _, Direct, _CFXR_LIGHTING_DIRECT, Indirect, _CFXR_LIGHTING_INDIRECT, Both, _CFXR_LIGHTING_ALL)] _UseLighting ("Mode", Float) = 0
		_DirectLightingRamp ("Direct Lighting Ramp", Range(0, 1)) = 1
		[Toggle(_NORMALMAP)] _UseNormalMap ("Enable Normal Map", Float) = 0
		[NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal Scale", Range(-1, 1)) = 1
		[Toggle(_EMISSION)] _UseEmission ("Enable Emission (TEXCOORD2)", Float) = 0
		[Toggle(_CFXR_LIGHTING_WPOS_OFFSET)] _UseLightingWorldPosOffset ("Enable World Pos. Offset", Float) = 0
		_LightingWorldPosStrength ("Offset Strength", Range(0, 1)) = 0.2
		[Toggle(_CFXR_LIGHTING_BACK)] _UseBackLighting ("Enable Backlighting", Float) = 0
		_DirLightScreenAtten ("Dir. Light Screen Attenuation", Range(0, 5)) = 1
		_BacklightTransmittance ("Backlight Transmittance", Range(0, 2)) = 1
		_IndirectLightingMix ("Indirect Lighting Mix", Range(0, 1)) = 0.5
		_ShadowColor ("Shadow Color", Vector) = (0,0,0,1)
		[KeywordEnum(Off,On,CustomTexture)] _CFXR_DITHERED_SHADOWS ("Dithered Shadows", Float) = 0
		_ShadowStrength ("Shadows Strength Max", Range(0, 1)) = 1
		_DitherCustom ("Dithering 3D Texture", 3D) = "black" {}
	}
// Recovered from the APK GLES programs; generated HLSL, not original author source.
SubShader {
Tags { "IgnoreProjector"="true" "PreviewType"="Plane" "Queue"="Transparent" "RenderType"="Transparent" }
Pass {
Name "BASE"
Tags { "IgnoreProjector"="true" "LightMode"="ForwardBase" "PreviewType"="Plane" "Queue"="Transparent" "RenderType"="Transparent" }
Cull Off
ZWrite [_ZWrite]
ZTest LEqual
Blend [_SrcBlend] [_DstBlend], One One
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
#pragma shader_feature_local _ _ALPHABLEND_ON
#pragma shader_feature_local _ _CFXR_ADDITIVE
#pragma shader_feature_local _ _CFXR_DISSOLVE
#pragma shader_feature_local _ _CFXR_OVERLAYTEX_1X
#pragma shader_feature_local _ _CFXR_UV_DISTORTION
#pragma shader_feature_local _ _FADING_ON
#pragma shader_feature_local _ _FLIPBOOK_BLENDING
#if defined(SOFTPARTICLES_ON) && !defined(_ALPHABLEND_ON) && defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && defined(_CFXR_OVERLAYTEX_1X) && defined(_CFXR_UV_DISTORTION) && defined(_FADING_ON) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4 _ProjectionParams;
float4 _Time;
float4 _OverlayTex_Scroll;
Texture2D<float4> _OverlayTex;
SamplerState sampler_OverlayTex;
float4 _DistortScrolling;
float _UseUV2Distortion;
float _UVDistortionAdd;
float _SingleChannel;
Texture2D<float4> _DistortTex;
SamplerState sampler_DistortTex;
float _Distort;
float _FadeAlongU;
float _HdrMultiply;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _CFXR_OVERLAYBLEND;
Texture2D<float4> _CameraDepthTexture;
SamplerState sampler_CameraDepthTexture;
float4 _ZBufferParams;
float4 unity_OrthoParams;
float _SoftParticlesFadeDistanceNear;
float _SoftParticlesFadeDistanceFar;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD1;
static float4 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD3 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat2;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat0 += transpose(unity_ObjectToWorld)[3];
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_gl_Position = v_u_xlat1;
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD1 = v_in_TEXCOORD1;
    v_u_xlat2 = v_u_xlat0.y * transpose(unity_MatrixV)[1].z;
    v_u_xlat0.x = (transpose(unity_MatrixV)[0].z * v_u_xlat0.x) + v_u_xlat2;
    v_u_xlat0.x = (transpose(unity_MatrixV)[2].z * v_u_xlat0.z) + v_u_xlat0.x;
    v_u_xlat0.x = (transpose(unity_MatrixV)[3].z * v_u_xlat0.w) + v_u_xlat0.x;
    v_vs_TEXCOORD3.z = -v_u_xlat0.x;
    v_u_xlat0.x = v_u_xlat1.y * _ProjectionParams.x;
    v_u_xlat0.w = v_u_xlat0.x * 0.5f;
    float2 _151 = v_u_xlat1.xw * 0.5f.xx;
    v_u_xlat0.x = _151.x;
    v_u_xlat0.z = _151.y;
    v_vs_TEXCOORD3.w = v_u_xlat1.w;
    float2 _163 = v_u_xlat0.zz + v_u_xlat0.xw;
    v_vs_TEXCOORD3.x = _163.x;
    v_vs_TEXCOORD3.y = _163.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;
static float4 f_vs_TEXCOORD3;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD3 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat0;
static float2 f_u_xlat10;
static float4 f_u_xlat16_0;
static float2 f_u_xlat5;
static bool3 f_u_xlatb1;
static float4 f_u_xlat1;
static float2 f_u_xlat16_5;
static float f_u_xlat15;
static bool3 f_u_xlatb2;
static float4 f_u_xlat16_3;
static float4 f_u_xlat16_4;
static float4 f_u_xlat16_1;
static float2 f_u_xlat2;
static float f_u_xlat7;
static bool f_u_xlatb12;

void f_frag_main()
{
    f_u_xlat0 = _Time.yy * _OverlayTex_Scroll.xy;
    f_u_xlat0 = frac(f_u_xlat0);
    f_u_xlat10 = f_vs_TEXCOORD0.zw + f_vs_TEXCOORD0.xy;
    f_u_xlat0 = (f_u_xlat10 * _OverlayTex_Scroll.zz) + f_u_xlat0;
    f_u_xlat16_0.x = _OverlayTex.Sample(sampler_OverlayTex, f_u_xlat0).x;
    f_u_xlat5 = _Time.yy * _DistortScrolling.xy;
    f_u_xlat5 = frac(f_u_xlat5);
    float4 _72 = float4(_UseUV2Distortion, _UVDistortionAdd, _SingleChannel, _UseUV2Distortion);
    f_u_xlatb1 = bool4(0.0f.xxxx.x < _72.x, 0.0f.xxxx.y < _72.y, 0.0f.xxxx.z < _72.z, 0.0f.xxxx.w < _72.w).xyz;
    float2 _81;
    if (f_u_xlatb1.x)
    {
        _81 = f_vs_TEXCOORD1.xy;
    }
    else
    {
        _81 = f_vs_TEXCOORD0.xy;
    }
    f_u_xlat1.x = _81.x;
    f_u_xlat1.w = _81.y;
    float4 _96 = f_u_xlat1;
    float2 _103 = (_96.xw * _DistortScrolling.zw) + f_vs_TEXCOORD0.zw;
    f_u_xlat1.x = _103.x;
    f_u_xlat1.w = _103.y;
    f_u_xlat5 += f_u_xlat1.xw;
    f_u_xlat16_5 = _DistortTex.Sample(sampler_DistortTex, f_u_xlat5).xy;
    float2 _124 = (f_u_xlat16_5 * 2.0f.xx) + (-1.0f).xx;
    f_u_xlat1.x = _124.x;
    f_u_xlat1.w = _124.y;
    f_u_xlat5 = f_u_xlat16_5 + (-f_vs_TEXCOORD0.xy);
    f_u_xlat5 = (_Distort.xx * f_u_xlat5) + f_vs_TEXCOORD0.xy;
    float4 _142 = f_u_xlat1;
    float2 _149 = (_142.xw * _Distort.xx) + f_vs_TEXCOORD0.xy;
    f_u_xlat1.x = _149.x;
    f_u_xlat1.w = _149.y;
    float2 _157;
    if (f_u_xlatb1.y)
    {
        _157 = f_u_xlat1.xw;
    }
    else
    {
        _157 = f_u_xlat5;
    }
    f_u_xlat5 = _157;
    float2 _169 = f_u_xlat5 + (-f_vs_TEXCOORD0.xy);
    f_u_xlat1.x = _169.x;
    f_u_xlat1.y = _169.y;
    f_u_xlat15 = f_vs_TEXCOORD0.y * 0.5f;
    float4 _182 = f_u_xlat1;
    float2 _187 = (f_u_xlat15.xx * _182.xy) + f_vs_TEXCOORD0.xy;
    f_u_xlat1.x = _187.x;
    f_u_xlat1.y = _187.y;
    float4 _198 = float4(_FadeAlongU, _HdrMultiply, _FadeAlongU, _FadeAlongU);
    bool2 _201 = bool4(0.0f.xxxx.x < _198.x, 0.0f.xxxx.y < _198.y, 0.0f.xxxx.z < _198.z, 0.0f.xxxx.w < _198.w).xy;
    f_u_xlatb2.x = _201.x;
    f_u_xlatb2.y = _201.y;
    float2 _208;
    if (f_u_xlatb2.x)
    {
        _208 = f_u_xlat1.xy;
    }
    else
    {
        _208 = f_u_xlat5;
    }
    f_u_xlat5 = _208;
    f_u_xlat16_3 = _MainTex.Sample(sampler_MainTex, f_u_xlat5);
    float3 _227;
    if (f_u_xlatb1.z)
    {
        _227 = 1.0f.xxx;
    }
    else
    {
        _227 = f_u_xlat16_3.xyz;
    }
    f_u_xlat16_4.x = _227.x;
    f_u_xlat16_4.y = _227.y;
    f_u_xlat16_4.z = _227.z;
    float _245;
    if (f_u_xlatb1.z)
    {
        _245 = f_u_xlat16_3.x;
    }
    else
    {
        _245 = f_u_xlat16_3.w;
    }
    f_u_xlat16_4.w = _245;
    f_u_xlat16_0 = f_u_xlat16_0.xxxx * f_u_xlat16_4;
    f_u_xlat16_1.x = f_u_xlat16_4.xyz.x;
    f_u_xlat16_1.y = f_u_xlat16_4.xyz.y;
    f_u_xlat16_1.z = f_u_xlat16_4.xyz.z;
    f_u_xlat16_4.x = f_u_xlat16_0.xyz.x;
    f_u_xlat16_4.y = f_u_xlat16_0.xyz.y;
    f_u_xlat16_4.z = f_u_xlat16_0.xyz.z;
    float4 _278 = _CFXR_OVERLAYBLEND.xxxx;
    bool2 _281 = bool4(_278.x == float4(2.0f, 0.0f, 1.0f, 0.0f).x, _278.y == float4(2.0f, 0.0f, 1.0f, 0.0f).y, _278.z == float4(2.0f, 0.0f, 1.0f, 0.0f).z, _278.w == float4(2.0f, 0.0f, 1.0f, 0.0f).w).xz;
    f_u_xlatb2.x = _281.x;
    f_u_xlatb2.z = _281.y;
    bool4 _290 = f_u_xlatb2.z.xxxx;
    f_u_xlat16_3 = float4(_290.x ? f_u_xlat16_4.x : f_u_xlat16_0.x, _290.y ? f_u_xlat16_4.y : f_u_xlat16_0.y, _290.z ? f_u_xlat16_4.z : f_u_xlat16_0.z, _290.w ? f_u_xlat16_4.w : f_u_xlat16_0.w);
    f_u_xlat16_1.w = f_u_xlat16_0.w;
    bool4 _299 = f_u_xlatb2.x.xxxx;
    f_u_xlat16_0 = float4(_299.x ? f_u_xlat16_1.x : f_u_xlat16_3.x, _299.y ? f_u_xlat16_1.y : f_u_xlat16_3.y, _299.z ? f_u_xlat16_1.z : f_u_xlat16_3.z, _299.w ? f_u_xlat16_1.w : f_u_xlat16_3.w);
    f_u_xlat16_0 *= f_vs_COLOR0;
    float3 _309 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlat16_4.x = _309.x;
    f_u_xlat16_4.y = _309.y;
    f_u_xlat16_4.z = _309.z;
    float3 _320;
    if (f_u_xlatb2.y)
    {
        _320 = f_u_xlat16_4.xyz;
    }
    else
    {
        _320 = f_u_xlat16_0.xyz;
    }
    f_SV_Target0.x = _320.x;
    f_SV_Target0.y = _320.y;
    f_SV_Target0.z = _320.z;
    f_u_xlat2 = f_vs_TEXCOORD3.xy / f_vs_TEXCOORD3.ww;
    f_u_xlat2.x = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, f_u_xlat2).x;
    f_u_xlat7 = (_ZBufferParams.z * f_u_xlat2.x) + _ZBufferParams.w;
    f_u_xlat2.x = (f_u_xlat2.x * _ProjectionParams.z) + _ProjectionParams.y;
    f_u_xlat7 = 1.0f / f_u_xlat7;
    f_u_xlatb12 = unity_OrthoParams.w == 1.0f;
    float _377;
    if (f_u_xlatb12)
    {
        _377 = f_u_xlat2.x;
    }
    else
    {
        _377 = f_u_xlat7;
    }
    f_u_xlat2.x = _377;
    f_u_xlat2.x += (-_SoftParticlesFadeDistanceNear);
    f_u_xlat2.x += (-f_vs_TEXCOORD3.z);
    f_u_xlat2.x *= _SoftParticlesFadeDistanceFar;
    f_u_xlat2.x = clamp(f_u_xlat2.x, 0.0f, 1.0f);
    f_u_xlat2.x = f_u_xlat16_0.w * f_u_xlat2.x;
    f_SV_Target0.w = min(f_u_xlat2.x, 1.0f);
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SOFTPARTICLES_ON) && !defined(_ALPHABLEND_ON) && defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && defined(_CFXR_OVERLAYTEX_1X) && defined(_CFXR_UV_DISTORTION) && defined(_FADING_ON) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _Time;
float4 _OverlayTex_Scroll;
Texture2D<float4> _OverlayTex;
SamplerState sampler_OverlayTex;
float4 _DistortScrolling;
float _UseUV2Distortion;
float _UVDistortionAdd;
float _SingleChannel;
Texture2D<float4> _DistortTex;
SamplerState sampler_DistortTex;
float _Distort;
float _FadeAlongU;
float _HdrMultiply;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _CFXR_OVERLAYBLEND;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;

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
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD1 = v_in_TEXCOORD1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat0;
static float2 f_u_xlat10;
static float4 f_u_xlat16_0;
static float2 f_u_xlat5;
static bool3 f_u_xlatb1;
static float4 f_u_xlat1;
static float2 f_u_xlat16_5;
static float f_u_xlat15;
static bool3 f_u_xlatb2;
static float4 f_u_xlat16_3;
static float4 f_u_xlat16_4;
static float4 f_u_xlat16_1;

void f_frag_main()
{
    f_u_xlat0 = _Time.yy * _OverlayTex_Scroll.xy;
    f_u_xlat0 = frac(f_u_xlat0);
    f_u_xlat10 = f_vs_TEXCOORD0.zw + f_vs_TEXCOORD0.xy;
    f_u_xlat0 = (f_u_xlat10 * _OverlayTex_Scroll.zz) + f_u_xlat0;
    f_u_xlat16_0.x = _OverlayTex.Sample(sampler_OverlayTex, f_u_xlat0).x;
    f_u_xlat5 = _Time.yy * _DistortScrolling.xy;
    f_u_xlat5 = frac(f_u_xlat5);
    float4 _72 = float4(_UseUV2Distortion, _UVDistortionAdd, _SingleChannel, _UseUV2Distortion);
    f_u_xlatb1 = bool4(0.0f.xxxx.x < _72.x, 0.0f.xxxx.y < _72.y, 0.0f.xxxx.z < _72.z, 0.0f.xxxx.w < _72.w).xyz;
    float2 _81;
    if (f_u_xlatb1.x)
    {
        _81 = f_vs_TEXCOORD1.xy;
    }
    else
    {
        _81 = f_vs_TEXCOORD0.xy;
    }
    f_u_xlat1.x = _81.x;
    f_u_xlat1.w = _81.y;
    float4 _96 = f_u_xlat1;
    float2 _103 = (_96.xw * _DistortScrolling.zw) + f_vs_TEXCOORD0.zw;
    f_u_xlat1.x = _103.x;
    f_u_xlat1.w = _103.y;
    f_u_xlat5 += f_u_xlat1.xw;
    f_u_xlat16_5 = _DistortTex.Sample(sampler_DistortTex, f_u_xlat5).xy;
    float2 _124 = (f_u_xlat16_5 * 2.0f.xx) + (-1.0f).xx;
    f_u_xlat1.x = _124.x;
    f_u_xlat1.w = _124.y;
    f_u_xlat5 = f_u_xlat16_5 + (-f_vs_TEXCOORD0.xy);
    f_u_xlat5 = (_Distort.xx * f_u_xlat5) + f_vs_TEXCOORD0.xy;
    float4 _142 = f_u_xlat1;
    float2 _149 = (_142.xw * _Distort.xx) + f_vs_TEXCOORD0.xy;
    f_u_xlat1.x = _149.x;
    f_u_xlat1.w = _149.y;
    float2 _157;
    if (f_u_xlatb1.y)
    {
        _157 = f_u_xlat1.xw;
    }
    else
    {
        _157 = f_u_xlat5;
    }
    f_u_xlat5 = _157;
    float2 _169 = f_u_xlat5 + (-f_vs_TEXCOORD0.xy);
    f_u_xlat1.x = _169.x;
    f_u_xlat1.y = _169.y;
    f_u_xlat15 = f_vs_TEXCOORD0.y * 0.5f;
    float4 _182 = f_u_xlat1;
    float2 _187 = (f_u_xlat15.xx * _182.xy) + f_vs_TEXCOORD0.xy;
    f_u_xlat1.x = _187.x;
    f_u_xlat1.y = _187.y;
    float4 _198 = float4(_FadeAlongU, _HdrMultiply, _FadeAlongU, _FadeAlongU);
    bool2 _201 = bool4(0.0f.xxxx.x < _198.x, 0.0f.xxxx.y < _198.y, 0.0f.xxxx.z < _198.z, 0.0f.xxxx.w < _198.w).xy;
    f_u_xlatb2.x = _201.x;
    f_u_xlatb2.y = _201.y;
    float2 _208;
    if (f_u_xlatb2.x)
    {
        _208 = f_u_xlat1.xy;
    }
    else
    {
        _208 = f_u_xlat5;
    }
    f_u_xlat5 = _208;
    f_u_xlat16_3 = _MainTex.Sample(sampler_MainTex, f_u_xlat5);
    float3 _227;
    if (f_u_xlatb1.z)
    {
        _227 = 1.0f.xxx;
    }
    else
    {
        _227 = f_u_xlat16_3.xyz;
    }
    f_u_xlat16_4.x = _227.x;
    f_u_xlat16_4.y = _227.y;
    f_u_xlat16_4.z = _227.z;
    float _245;
    if (f_u_xlatb1.z)
    {
        _245 = f_u_xlat16_3.x;
    }
    else
    {
        _245 = f_u_xlat16_3.w;
    }
    f_u_xlat16_4.w = _245;
    f_u_xlat16_0 = f_u_xlat16_0.xxxx * f_u_xlat16_4;
    f_u_xlat16_1.x = f_u_xlat16_4.xyz.x;
    f_u_xlat16_1.y = f_u_xlat16_4.xyz.y;
    f_u_xlat16_1.z = f_u_xlat16_4.xyz.z;
    f_u_xlat16_4.x = f_u_xlat16_0.xyz.x;
    f_u_xlat16_4.y = f_u_xlat16_0.xyz.y;
    f_u_xlat16_4.z = f_u_xlat16_0.xyz.z;
    float4 _278 = _CFXR_OVERLAYBLEND.xxxx;
    bool2 _281 = bool4(_278.x == float4(2.0f, 0.0f, 1.0f, 0.0f).x, _278.y == float4(2.0f, 0.0f, 1.0f, 0.0f).y, _278.z == float4(2.0f, 0.0f, 1.0f, 0.0f).z, _278.w == float4(2.0f, 0.0f, 1.0f, 0.0f).w).xz;
    f_u_xlatb2.x = _281.x;
    f_u_xlatb2.z = _281.y;
    bool4 _290 = f_u_xlatb2.z.xxxx;
    f_u_xlat16_3 = float4(_290.x ? f_u_xlat16_4.x : f_u_xlat16_0.x, _290.y ? f_u_xlat16_4.y : f_u_xlat16_0.y, _290.z ? f_u_xlat16_4.z : f_u_xlat16_0.z, _290.w ? f_u_xlat16_4.w : f_u_xlat16_0.w);
    f_u_xlat16_1.w = f_u_xlat16_0.w;
    bool4 _299 = f_u_xlatb2.x.xxxx;
    f_u_xlat16_0 = float4(_299.x ? f_u_xlat16_1.x : f_u_xlat16_3.x, _299.y ? f_u_xlat16_1.y : f_u_xlat16_3.y, _299.z ? f_u_xlat16_1.z : f_u_xlat16_3.z, _299.w ? f_u_xlat16_1.w : f_u_xlat16_3.w);
    f_u_xlat16_0 *= f_vs_COLOR0;
    float3 _309 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlat16_4.x = _309.x;
    f_u_xlat16_4.y = _309.y;
    f_u_xlat16_4.z = _309.z;
    float3 _320;
    if (f_u_xlatb2.y)
    {
        _320 = f_u_xlat16_4.xyz;
    }
    else
    {
        _320 = f_u_xlat16_0.xyz;
    }
    f_SV_Target0.x = _320.x;
    f_SV_Target0.y = _320.y;
    f_SV_Target0.z = _320.z;
    f_SV_Target0.w = min(f_u_xlat16_0.w, 1.0f);
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(SOFTPARTICLES_ON) && defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && defined(_CFXR_DISSOLVE) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && defined(_FADING_ON) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4 _ProjectionParams;
float4 _Time;
float2 _DissolveScroll;
float4 _DissolveTex_ST;
float _SingleChannel;
float _UseDissolveOffsetUV;
Texture2D<float4> _DissolveTex;
SamplerState sampler_DissolveTex;
float _InvertDissolveTex;
float _DissolveSmooth;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _DoubleDissolve;
Texture2D<float4> _CameraDepthTexture;
SamplerState sampler_CameraDepthTexture;
float4 _ZBufferParams;
float4 unity_OrthoParams;
float _SoftParticlesFadeDistanceNear;
float _SoftParticlesFadeDistanceFar;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD1;
static float4 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD3 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat2;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat0 += transpose(unity_ObjectToWorld)[3];
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_gl_Position = v_u_xlat1;
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD1 = v_in_TEXCOORD1;
    v_u_xlat2 = v_u_xlat0.y * transpose(unity_MatrixV)[1].z;
    v_u_xlat0.x = (transpose(unity_MatrixV)[0].z * v_u_xlat0.x) + v_u_xlat2;
    v_u_xlat0.x = (transpose(unity_MatrixV)[2].z * v_u_xlat0.z) + v_u_xlat0.x;
    v_u_xlat0.x = (transpose(unity_MatrixV)[3].z * v_u_xlat0.w) + v_u_xlat0.x;
    v_vs_TEXCOORD3.z = -v_u_xlat0.x;
    v_u_xlat0.x = v_u_xlat1.y * _ProjectionParams.x;
    v_u_xlat0.w = v_u_xlat0.x * 0.5f;
    float2 _151 = v_u_xlat1.xw * 0.5f.xx;
    v_u_xlat0.x = _151.x;
    v_u_xlat0.z = _151.y;
    v_vs_TEXCOORD3.w = v_u_xlat1.w;
    float2 _163 = v_u_xlat0.zz + v_u_xlat0.xw;
    v_vs_TEXCOORD3.x = _163.x;
    v_vs_TEXCOORD3.y = _163.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD3;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD3 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat0;
static float2 f_u_xlat10;
static bool2 f_u_xlatb10;
static float4 f_u_xlat16_0;
static float3 f_u_xlat16_1;
static float f_u_xlat5;
static float f_u_xlat16_6;
static bool f_u_xlatb0;
static float f_u_xlat16_11;
static float f_u_xlat16_16;
static float4 f_u_xlat16_2;
static float4 f_u_xlat16_3;
static bool f_u_xlatb4;
static float2 f_u_xlat4;
static float f_u_xlat9;
static bool f_u_xlatb14;

void f_frag_main()
{
    f_u_xlat0 = _Time.yy * float2(_DissolveScroll.x, _DissolveScroll.y);
    f_u_xlat0 = frac(f_u_xlat0);
    f_u_xlat10 = (f_vs_TEXCOORD0.xy * _DissolveTex_ST.xy) + _DissolveTex_ST.zw;
    f_u_xlat0 += f_u_xlat10;
    float4 _56 = float4(_SingleChannel, _UseDissolveOffsetUV, _SingleChannel, _UseDissolveOffsetUV);
    f_u_xlatb10 = bool4(0.0f.xxxx.x < _56.x, 0.0f.xxxx.y < _56.y, 0.0f.xxxx.z < _56.z, 0.0f.xxxx.w < _56.w).xy;
    float2 _64;
    if (f_u_xlatb10.y)
    {
        _64 = f_u_xlat0;
    }
    else
    {
        _64 = f_vs_TEXCOORD0.xy;
    }
    f_u_xlat0 = _64;
    f_u_xlat16_0.x = _DissolveTex.Sample(sampler_DissolveTex, f_u_xlat0).x;
    f_u_xlat16_1.x = (f_u_xlat16_0.x * 2.0f) + (-1.0f);
    f_u_xlat5 = (f_u_xlat16_1.x * f_vs_TEXCOORD1.z) + f_vs_TEXCOORD0.x;
    float _109;
    if (f_u_xlatb10.y)
    {
        _109 = f_u_xlat5;
    }
    else
    {
        _109 = f_u_xlat16_0.x;
    }
    f_u_xlat16_1.x = _109;
    f_u_xlat16_6 = (-f_u_xlat16_1.x) + 1.0f;
    f_u_xlatb0 = 0.0f >= _InvertDissolveTex;
    float _129;
    if (f_u_xlatb0)
    {
        _129 = f_u_xlat16_6;
    }
    else
    {
        _129 = f_u_xlat16_1.x;
    }
    f_u_xlat16_1.x = _129;
    f_u_xlat16_6 = f_u_xlat16_1.x + _DissolveSmooth;
    f_u_xlat16_1.x += (-_DissolveSmooth);
    f_u_xlat16_6 = (-f_u_xlat16_1.x) + f_u_xlat16_6;
    f_u_xlat16_6 = 1.0f / f_u_xlat16_6;
    f_u_xlat16_11 = (_DissolveSmooth * 2.0f) + 1.0f;
    f_u_xlat16_11 = (f_vs_TEXCOORD1.x * f_u_xlat16_11) + (-_DissolveSmooth);
    f_u_xlat16_16 = (-f_u_xlat16_1.x) + f_u_xlat16_11;
    f_u_xlat16_16 = f_u_xlat16_6 * f_u_xlat16_16;
    f_u_xlat16_16 = clamp(f_u_xlat16_16, 0.0f, 1.0f);
    f_u_xlat16_2.x = (f_u_xlat16_16 * (-2.0f)) + 3.0f;
    f_u_xlat16_16 *= f_u_xlat16_16;
    f_u_xlat16_16 *= f_u_xlat16_2.x;
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy);
    float3 _201;
    if (f_u_xlatb10.x)
    {
        _201 = 1.0f.xxx;
    }
    else
    {
        _201 = f_u_xlat16_2.xyz;
    }
    f_u_xlat16_3.x = _201.x;
    f_u_xlat16_3.y = _201.y;
    f_u_xlat16_3.z = _201.z;
    float _217;
    if (f_u_xlatb10.x)
    {
        _217 = f_u_xlat16_2.x;
    }
    else
    {
        _217 = f_u_xlat16_2.w;
    }
    f_u_xlat16_3.w = _217;
    f_u_xlat16_0 = f_u_xlat16_3 * f_vs_COLOR0;
    f_u_xlat16_16 *= f_u_xlat16_0.w;
    f_u_xlatb4 = 0.0f < _DoubleDissolve;
    float _241;
    if (f_u_xlatb4)
    {
        _241 = f_vs_TEXCOORD1.y;
    }
    else
    {
        _241 = 0.0f;
    }
    f_u_xlat16_3.x = _241;
    f_u_xlat16_11 += (-f_u_xlat16_3.x);
    f_u_xlatb4 = 0.0f < f_u_xlat16_3.x;
    f_u_xlat16_1.x = (-f_u_xlat16_1.x) + f_u_xlat16_11;
    f_u_xlat16_1.x = f_u_xlat16_6 * f_u_xlat16_1.x;
    f_u_xlat16_1.x = clamp(f_u_xlat16_1.x, 0.0f, 1.0f);
    f_u_xlat16_6 = (f_u_xlat16_1.x * (-2.0f)) + 3.0f;
    f_u_xlat16_1.x *= f_u_xlat16_1.x;
    f_u_xlat16_1.x = ((-f_u_xlat16_6) * f_u_xlat16_1.x) + f_u_xlat16_16;
    f_u_xlat16_1.x = clamp(f_u_xlat16_1.x, 0.0f, 1.0f);
    float _295;
    if (f_u_xlatb4)
    {
        _295 = f_u_xlat16_1.x;
    }
    else
    {
        _295 = f_u_xlat16_16;
    }
    f_u_xlat16_1.x = _295;
    f_u_xlat4 = f_vs_TEXCOORD3.xy / f_vs_TEXCOORD3.ww;
    f_u_xlat4.x = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, f_u_xlat4).x;
    f_u_xlat9 = (_ZBufferParams.z * f_u_xlat4.x) + _ZBufferParams.w;
    f_u_xlat4.x = (f_u_xlat4.x * _ProjectionParams.z) + _ProjectionParams.y;
    f_u_xlat9 = 1.0f / f_u_xlat9;
    f_u_xlatb14 = unity_OrthoParams.w == 1.0f;
    float _345;
    if (f_u_xlatb14)
    {
        _345 = f_u_xlat4.x;
    }
    else
    {
        _345 = f_u_xlat9;
    }
    f_u_xlat4.x = _345;
    f_u_xlat4.x += (-_SoftParticlesFadeDistanceNear);
    f_u_xlat4.x += (-f_vs_TEXCOORD3.z);
    f_u_xlat4.x *= _SoftParticlesFadeDistanceFar;
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat4.x = f_u_xlat16_1.x * f_u_xlat4.x;
    f_SV_Target0.w = min(f_u_xlat4.x, 1.0f);
    f_u_xlat16_1 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlatb4 = 0.0f < _HdrMultiply;
    float3 _400;
    if (f_u_xlatb4)
    {
        _400 = f_u_xlat16_1;
    }
    else
    {
        _400 = f_u_xlat16_0.xyz;
    }
    f_SV_Target0.x = _400.x;
    f_SV_Target0.y = _400.y;
    f_SV_Target0.z = _400.z;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(SOFTPARTICLES_ON) && defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && defined(_FADING_ON) && defined(_FLIPBOOK_BLENDING)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4 _ProjectionParams;
Texture2D<float4> _CameraDepthTexture;
SamplerState sampler_CameraDepthTexture;
float4 _ZBufferParams;
float4 unity_OrthoParams;
float _SoftParticlesFadeDistanceNear;
float _SoftParticlesFadeDistanceFar;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD1;
static float4 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD3 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float v_u_xlat2;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat0 += transpose(unity_ObjectToWorld)[3];
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_gl_Position = v_u_xlat1;
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD1 = v_in_TEXCOORD1;
    v_u_xlat2 = v_u_xlat0.y * transpose(unity_MatrixV)[1].z;
    v_u_xlat0.x = (transpose(unity_MatrixV)[0].z * v_u_xlat0.x) + v_u_xlat2;
    v_u_xlat0.x = (transpose(unity_MatrixV)[2].z * v_u_xlat0.z) + v_u_xlat0.x;
    v_u_xlat0.x = (transpose(unity_MatrixV)[3].z * v_u_xlat0.w) + v_u_xlat0.x;
    v_vs_TEXCOORD3.z = -v_u_xlat0.x;
    v_u_xlat0.x = v_u_xlat1.y * _ProjectionParams.x;
    v_u_xlat0.w = v_u_xlat0.x * 0.5f;
    float2 _151 = v_u_xlat1.xw * 0.5f.xx;
    v_u_xlat0.x = _151.x;
    v_u_xlat0.z = _151.y;
    v_vs_TEXCOORD3.w = v_u_xlat1.w;
    float2 _163 = v_u_xlat0.zz + v_u_xlat0.xw;
    v_vs_TEXCOORD3.x = _163.x;
    v_vs_TEXCOORD3.y = _163.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float4 f_vs_TEXCOORD3;
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD3 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat0;
static float f_u_xlat5;
static bool f_u_xlatb10;
static float4 f_u_xlat16_1;
static bool f_u_xlatb5;
static float3 f_u_xlat16_2;
static float4 f_u_xlat16_3;
static float4 f_u_xlat16_4;
static float4 f_u_xlat2;
static float4 f_u_xlat1;
static bool f_u_xlatb0;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD3.xy / f_vs_TEXCOORD3.ww;
    f_u_xlat0.x = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, f_u_xlat0).x;
    f_u_xlat5 = (_ZBufferParams.z * f_u_xlat0.x) + _ZBufferParams.w;
    f_u_xlat0.x = (f_u_xlat0.x * _ProjectionParams.z) + _ProjectionParams.y;
    f_u_xlat5 = 1.0f / f_u_xlat5;
    f_u_xlatb10 = unity_OrthoParams.w == 1.0f;
    float _67;
    if (f_u_xlatb10)
    {
        _67 = f_u_xlat0.x;
    }
    else
    {
        _67 = f_u_xlat5;
    }
    f_u_xlat0.x = _67;
    f_u_xlat0.x += (-_SoftParticlesFadeDistanceNear);
    f_u_xlat0.x += (-f_vs_TEXCOORD3.z);
    f_u_xlat0.x *= _SoftParticlesFadeDistanceFar;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.zw);
    f_u_xlatb5 = 0.0f < _SingleChannel;
    float3 _119;
    if (f_u_xlatb5)
    {
        _119 = f_u_xlat16_1.xxx;
    }
    else
    {
        _119 = f_u_xlat16_1.yzw;
    }
    f_u_xlat16_2 = _119;
    f_u_xlat16_3 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy);
    float _135;
    if (f_u_xlatb5)
    {
        _135 = f_u_xlat16_3.x;
    }
    else
    {
        _135 = f_u_xlat16_3.w;
    }
    f_u_xlat16_4.w = _135;
    float3 _146;
    if (f_u_xlatb5)
    {
        _146 = 1.0f.xxx;
    }
    else
    {
        _146 = f_u_xlat16_3.xyz;
    }
    f_u_xlat16_4.x = _146.x;
    f_u_xlat16_4.y = _146.y;
    f_u_xlat16_4.z = _146.z;
    float3 _165 = f_u_xlat16_2 + (-f_u_xlat16_4.yzw);
    f_u_xlat2.y = _165.x;
    f_u_xlat2.z = _165.y;
    f_u_xlat2.w = _165.z;
    f_u_xlat2.x = f_u_xlat16_1.x + (-f_u_xlat16_4.x);
    f_u_xlat1 = (f_vs_TEXCOORD1.yyyy * f_u_xlat2) + f_u_xlat16_4;
    f_u_xlat16_1 = f_u_xlat1 * f_vs_COLOR0;
    f_u_xlat0.x *= f_u_xlat16_1.w;
    f_SV_Target0.w = min(f_u_xlat0.x, 1.0f);
    float3 _209 = f_u_xlat16_1.xyz * _HdrMultiply.xxx;
    f_u_xlat16_4.x = _209.x;
    f_u_xlat16_4.y = _209.y;
    f_u_xlat16_4.z = _209.z;
    f_u_xlatb0 = 0.0f < _HdrMultiply;
    float3 _220;
    if (f_u_xlatb0)
    {
        _220 = f_u_xlat16_4.xyz;
    }
    else
    {
        _220 = f_u_xlat16_1.xyz;
    }
    f_SV_Target0.x = _220.x;
    f_SV_Target0.y = _220.y;
    f_SV_Target0.z = _220.z;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SOFTPARTICLES_ON) && defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && defined(_CFXR_DISSOLVE) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && defined(_FADING_ON) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _Time;
float2 _DissolveScroll;
float4 _DissolveTex_ST;
float _SingleChannel;
float _UseDissolveOffsetUV;
Texture2D<float4> _DissolveTex;
SamplerState sampler_DissolveTex;
float _InvertDissolveTex;
float _DissolveSmooth;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _DoubleDissolve;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;

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
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD1 = v_in_TEXCOORD1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat0;
static float2 f_u_xlat10;
static bool2 f_u_xlatb10;
static float4 f_u_xlat16_0;
static float3 f_u_xlat16_1;
static float f_u_xlat5;
static float f_u_xlat16_6;
static bool f_u_xlatb0;
static float f_u_xlat16_11;
static float f_u_xlat16_16;
static float4 f_u_xlat16_2;
static float4 f_u_xlat16_3;
static bool f_u_xlatb4;

void f_frag_main()
{
    f_u_xlat0 = _Time.yy * float2(_DissolveScroll.x, _DissolveScroll.y);
    f_u_xlat0 = frac(f_u_xlat0);
    f_u_xlat10 = (f_vs_TEXCOORD0.xy * _DissolveTex_ST.xy) + _DissolveTex_ST.zw;
    f_u_xlat0 += f_u_xlat10;
    float4 _56 = float4(_SingleChannel, _UseDissolveOffsetUV, _SingleChannel, _UseDissolveOffsetUV);
    f_u_xlatb10 = bool4(0.0f.xxxx.x < _56.x, 0.0f.xxxx.y < _56.y, 0.0f.xxxx.z < _56.z, 0.0f.xxxx.w < _56.w).xy;
    float2 _64;
    if (f_u_xlatb10.y)
    {
        _64 = f_u_xlat0;
    }
    else
    {
        _64 = f_vs_TEXCOORD0.xy;
    }
    f_u_xlat0 = _64;
    f_u_xlat16_0.x = _DissolveTex.Sample(sampler_DissolveTex, f_u_xlat0).x;
    f_u_xlat16_1.x = (f_u_xlat16_0.x * 2.0f) + (-1.0f);
    f_u_xlat5 = (f_u_xlat16_1.x * f_vs_TEXCOORD1.z) + f_vs_TEXCOORD0.x;
    float _109;
    if (f_u_xlatb10.y)
    {
        _109 = f_u_xlat5;
    }
    else
    {
        _109 = f_u_xlat16_0.x;
    }
    f_u_xlat16_1.x = _109;
    f_u_xlat16_6 = (-f_u_xlat16_1.x) + 1.0f;
    f_u_xlatb0 = 0.0f >= _InvertDissolveTex;
    float _129;
    if (f_u_xlatb0)
    {
        _129 = f_u_xlat16_6;
    }
    else
    {
        _129 = f_u_xlat16_1.x;
    }
    f_u_xlat16_1.x = _129;
    f_u_xlat16_6 = f_u_xlat16_1.x + _DissolveSmooth;
    f_u_xlat16_1.x += (-_DissolveSmooth);
    f_u_xlat16_6 = (-f_u_xlat16_1.x) + f_u_xlat16_6;
    f_u_xlat16_6 = 1.0f / f_u_xlat16_6;
    f_u_xlat16_11 = (_DissolveSmooth * 2.0f) + 1.0f;
    f_u_xlat16_11 = (f_vs_TEXCOORD1.x * f_u_xlat16_11) + (-_DissolveSmooth);
    f_u_xlat16_16 = (-f_u_xlat16_1.x) + f_u_xlat16_11;
    f_u_xlat16_16 = f_u_xlat16_6 * f_u_xlat16_16;
    f_u_xlat16_16 = clamp(f_u_xlat16_16, 0.0f, 1.0f);
    f_u_xlat16_2.x = (f_u_xlat16_16 * (-2.0f)) + 3.0f;
    f_u_xlat16_16 *= f_u_xlat16_16;
    f_u_xlat16_16 *= f_u_xlat16_2.x;
    f_u_xlat16_2 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy);
    float3 _201;
    if (f_u_xlatb10.x)
    {
        _201 = 1.0f.xxx;
    }
    else
    {
        _201 = f_u_xlat16_2.xyz;
    }
    f_u_xlat16_3.x = _201.x;
    f_u_xlat16_3.y = _201.y;
    f_u_xlat16_3.z = _201.z;
    float _217;
    if (f_u_xlatb10.x)
    {
        _217 = f_u_xlat16_2.x;
    }
    else
    {
        _217 = f_u_xlat16_2.w;
    }
    f_u_xlat16_3.w = _217;
    f_u_xlat16_0 = f_u_xlat16_3 * f_vs_COLOR0;
    f_u_xlat16_16 *= f_u_xlat16_0.w;
    f_u_xlatb4 = 0.0f < _DoubleDissolve;
    float _241;
    if (f_u_xlatb4)
    {
        _241 = f_vs_TEXCOORD1.y;
    }
    else
    {
        _241 = 0.0f;
    }
    f_u_xlat16_3.x = _241;
    f_u_xlat16_11 += (-f_u_xlat16_3.x);
    f_u_xlatb4 = 0.0f < f_u_xlat16_3.x;
    f_u_xlat16_1.x = (-f_u_xlat16_1.x) + f_u_xlat16_11;
    f_u_xlat16_1.x = f_u_xlat16_6 * f_u_xlat16_1.x;
    f_u_xlat16_1.x = clamp(f_u_xlat16_1.x, 0.0f, 1.0f);
    f_u_xlat16_6 = (f_u_xlat16_1.x * (-2.0f)) + 3.0f;
    f_u_xlat16_1.x *= f_u_xlat16_1.x;
    f_u_xlat16_1.x = ((-f_u_xlat16_6) * f_u_xlat16_1.x) + f_u_xlat16_16;
    f_u_xlat16_1.x = clamp(f_u_xlat16_1.x, 0.0f, 1.0f);
    float _295;
    if (f_u_xlatb4)
    {
        _295 = f_u_xlat16_1.x;
    }
    else
    {
        _295 = f_u_xlat16_16;
    }
    f_u_xlat16_1.x = _295;
    f_SV_Target0.w = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_1 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlatb4 = 0.0f < _HdrMultiply;
    float3 _320;
    if (f_u_xlatb4)
    {
        _320 = f_u_xlat16_1;
    }
    else
    {
        _320 = f_u_xlat16_0.xyz;
    }
    f_SV_Target0.x = _320.x;
    f_SV_Target0.y = _320.y;
    f_SV_Target0.z = _320.z;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SOFTPARTICLES_ON) && defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && defined(_FADING_ON) && defined(_FLIPBOOK_BLENDING)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;

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
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD1 = v_in_TEXCOORD1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static bool f_u_xlatb1;
static float3 f_u_xlat16_2;
static float4 f_u_xlat16_3;
static float4 f_u_xlat16_4;
static float4 f_u_xlat1;
static float4 f_u_xlat0;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.zw);
    f_u_xlatb1 = 0.0f < _SingleChannel;
    float3 _34;
    if (f_u_xlatb1)
    {
        _34 = f_u_xlat16_0.xxx;
    }
    else
    {
        _34 = f_u_xlat16_0.yzw;
    }
    f_u_xlat16_2 = _34;
    f_u_xlat16_3 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy);
    float _51;
    if (f_u_xlatb1)
    {
        _51 = f_u_xlat16_3.x;
    }
    else
    {
        _51 = f_u_xlat16_3.w;
    }
    f_u_xlat16_4.w = _51;
    float3 _66;
    if (f_u_xlatb1)
    {
        _66 = 1.0f.xxx;
    }
    else
    {
        _66 = f_u_xlat16_3.xyz;
    }
    f_u_xlat16_4.x = _66.x;
    f_u_xlat16_4.y = _66.y;
    f_u_xlat16_4.z = _66.z;
    float3 _88 = f_u_xlat16_2 + (-f_u_xlat16_4.yzw);
    f_u_xlat1.y = _88.x;
    f_u_xlat1.z = _88.y;
    f_u_xlat1.w = _88.z;
    f_u_xlat1.x = f_u_xlat16_0.x + (-f_u_xlat16_4.x);
    f_u_xlat0 = (f_vs_TEXCOORD1.yyyy * f_u_xlat1) + f_u_xlat16_4;
    f_u_xlat16_0 = f_u_xlat0 * f_vs_COLOR0;
    f_u_xlat16_2 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlatb1 = 0.0f < _HdrMultiply;
    float3 _125;
    if (f_u_xlatb1)
    {
        _125 = f_u_xlat16_2;
    }
    else
    {
        _125 = f_u_xlat16_0.xyz;
    }
    f_SV_Target0.x = _125.x;
    f_SV_Target0.y = _125.y;
    f_SV_Target0.z = _125.z;
    f_SV_Target0.w = min(f_u_xlat16_0.w, 1.0f);
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(SOFTPARTICLES_ON) && defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && defined(_FADING_ON) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4 _ProjectionParams;
Texture2D<float4> _CameraDepthTexture;
SamplerState sampler_CameraDepthTexture;
float4 _ZBufferParams;
float4 unity_OrthoParams;
float _SoftParticlesFadeDistanceNear;
float _SoftParticlesFadeDistanceFar;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float4 v_in_TEXCOORD1;
static float4 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD3 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_vs_TEXCOORD1;
static float v_u_xlat2;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat0 += transpose(unity_ObjectToWorld)[3];
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_gl_Position = v_u_xlat1;
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD1 = v_in_TEXCOORD1;
    v_u_xlat2 = v_u_xlat0.y * transpose(unity_MatrixV)[1].z;
    v_u_xlat0.x = (transpose(unity_MatrixV)[0].z * v_u_xlat0.x) + v_u_xlat2;
    v_u_xlat0.x = (transpose(unity_MatrixV)[2].z * v_u_xlat0.z) + v_u_xlat0.x;
    v_u_xlat0.x = (transpose(unity_MatrixV)[3].z * v_u_xlat0.w) + v_u_xlat0.x;
    v_vs_TEXCOORD3.z = -v_u_xlat0.x;
    v_u_xlat0.x = v_u_xlat1.y * _ProjectionParams.x;
    v_u_xlat0.w = v_u_xlat0.x * 0.5f;
    float2 _151 = v_u_xlat1.xw * 0.5f.xx;
    v_u_xlat0.x = _151.x;
    v_u_xlat0.z = _151.y;
    v_vs_TEXCOORD3.w = v_u_xlat1.w;
    float2 _163 = v_u_xlat0.zz + v_u_xlat0.xw;
    v_vs_TEXCOORD3.x = _163.x;
    v_vs_TEXCOORD3.y = _163.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float4 f_vs_TEXCOORD3;
static float4 f_vs_TEXCOORD0;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD3 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat0;
static float f_u_xlat3;
static bool f_u_xlatb6;
static float4 f_u_xlat16_1;
static bool f_u_xlatb3;
static float4 f_u_xlat16_2;
static bool f_u_xlatb0;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD3.xy / f_vs_TEXCOORD3.ww;
    f_u_xlat0.x = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, f_u_xlat0).x;
    f_u_xlat3 = (_ZBufferParams.z * f_u_xlat0.x) + _ZBufferParams.w;
    f_u_xlat0.x = (f_u_xlat0.x * _ProjectionParams.z) + _ProjectionParams.y;
    f_u_xlat3 = 1.0f / f_u_xlat3;
    f_u_xlatb6 = unity_OrthoParams.w == 1.0f;
    float _67;
    if (f_u_xlatb6)
    {
        _67 = f_u_xlat0.x;
    }
    else
    {
        _67 = f_u_xlat3;
    }
    f_u_xlat0.x = _67;
    f_u_xlat0.x += (-_SoftParticlesFadeDistanceNear);
    f_u_xlat0.x += (-f_vs_TEXCOORD3.z);
    f_u_xlat0.x *= _SoftParticlesFadeDistanceFar;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy);
    f_u_xlatb3 = 0.0f < _SingleChannel;
    float3 _118;
    if (f_u_xlatb3)
    {
        _118 = 1.0f.xxx;
    }
    else
    {
        _118 = f_u_xlat16_1.xyz;
    }
    f_u_xlat16_2.x = _118.x;
    f_u_xlat16_2.y = _118.y;
    f_u_xlat16_2.z = _118.z;
    float _133;
    if (f_u_xlatb3)
    {
        _133 = f_u_xlat16_1.x;
    }
    else
    {
        _133 = f_u_xlat16_1.w;
    }
    f_u_xlat16_2.w = _133;
    f_u_xlat16_1 = f_u_xlat16_2 * f_vs_COLOR0;
    f_u_xlat0.x *= f_u_xlat16_1.w;
    f_SV_Target0.w = min(f_u_xlat0.x, 1.0f);
    float3 _165 = f_u_xlat16_1.xyz * _HdrMultiply.xxx;
    f_u_xlat16_2.x = _165.x;
    f_u_xlat16_2.y = _165.y;
    f_u_xlat16_2.z = _165.z;
    f_u_xlatb0 = 0.0f < _HdrMultiply;
    float3 _176;
    if (f_u_xlatb0)
    {
        _176 = f_u_xlat16_2.xyz;
    }
    else
    {
        _176 = f_u_xlat16_1.xyz;
    }
    f_SV_Target0.x = _176.x;
    f_SV_Target0.y = _176.y;
    f_SV_Target0.z = _176.z;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(SOFTPARTICLES_ON) && !defined(_ALPHABLEND_ON) && defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && defined(_FADING_ON) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4 _ProjectionParams;
Texture2D<float4> _CameraDepthTexture;
SamplerState sampler_CameraDepthTexture;
float4 _ZBufferParams;
float4 unity_OrthoParams;
float _SoftParticlesFadeDistanceNear;
float _SoftParticlesFadeDistanceFar;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float4 v_in_TEXCOORD1;
static float4 v_vs_TEXCOORD3;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD3 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_vs_TEXCOORD1;
static float v_u_xlat2;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat0 += transpose(unity_ObjectToWorld)[3];
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_gl_Position = v_u_xlat1;
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD1 = v_in_TEXCOORD1;
    v_u_xlat2 = v_u_xlat0.y * transpose(unity_MatrixV)[1].z;
    v_u_xlat0.x = (transpose(unity_MatrixV)[0].z * v_u_xlat0.x) + v_u_xlat2;
    v_u_xlat0.x = (transpose(unity_MatrixV)[2].z * v_u_xlat0.z) + v_u_xlat0.x;
    v_u_xlat0.x = (transpose(unity_MatrixV)[3].z * v_u_xlat0.w) + v_u_xlat0.x;
    v_vs_TEXCOORD3.z = -v_u_xlat0.x;
    v_u_xlat0.x = v_u_xlat1.y * _ProjectionParams.x;
    v_u_xlat0.w = v_u_xlat0.x * 0.5f;
    float2 _151 = v_u_xlat1.xw * 0.5f.xx;
    v_u_xlat0.x = _151.x;
    v_u_xlat0.z = _151.y;
    v_vs_TEXCOORD3.w = v_u_xlat1.w;
    float2 _163 = v_u_xlat0.zz + v_u_xlat0.xw;
    v_vs_TEXCOORD3.x = _163.x;
    v_vs_TEXCOORD3.y = _163.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    return stage_output;
}
static float4 f_vs_TEXCOORD3;
static float4 f_vs_TEXCOORD0;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD3 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat0;
static float f_u_xlat3;
static bool f_u_xlatb6;
static float4 f_u_xlat16_1;
static bool f_u_xlatb3;
static float4 f_u_xlat16_2;
static bool f_u_xlatb0;

void f_frag_main()
{
    f_u_xlat0 = f_vs_TEXCOORD3.xy / f_vs_TEXCOORD3.ww;
    f_u_xlat0.x = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, f_u_xlat0).x;
    f_u_xlat3 = (_ZBufferParams.z * f_u_xlat0.x) + _ZBufferParams.w;
    f_u_xlat0.x = (f_u_xlat0.x * _ProjectionParams.z) + _ProjectionParams.y;
    f_u_xlat3 = 1.0f / f_u_xlat3;
    f_u_xlatb6 = unity_OrthoParams.w == 1.0f;
    float _67;
    if (f_u_xlatb6)
    {
        _67 = f_u_xlat0.x;
    }
    else
    {
        _67 = f_u_xlat3;
    }
    f_u_xlat0.x = _67;
    f_u_xlat0.x += (-_SoftParticlesFadeDistanceNear);
    f_u_xlat0.x += (-f_vs_TEXCOORD3.z);
    f_u_xlat0.x *= _SoftParticlesFadeDistanceFar;
    f_u_xlat0.x = clamp(f_u_xlat0.x, 0.0f, 1.0f);
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy);
    f_u_xlatb3 = 0.0f < _SingleChannel;
    float3 _118;
    if (f_u_xlatb3)
    {
        _118 = 1.0f.xxx;
    }
    else
    {
        _118 = f_u_xlat16_1.xyz;
    }
    f_u_xlat16_2.x = _118.x;
    f_u_xlat16_2.y = _118.y;
    f_u_xlat16_2.z = _118.z;
    float _133;
    if (f_u_xlatb3)
    {
        _133 = f_u_xlat16_1.x;
    }
    else
    {
        _133 = f_u_xlat16_1.w;
    }
    f_u_xlat16_2.w = _133;
    f_u_xlat16_1 = f_u_xlat16_2 * f_vs_COLOR0;
    f_u_xlat0.x *= f_u_xlat16_1.w;
    f_SV_Target0.w = min(f_u_xlat0.x, 1.0f);
    float3 _165 = f_u_xlat16_1.xyz * _HdrMultiply.xxx;
    f_u_xlat16_2.x = _165.x;
    f_u_xlat16_2.y = _165.y;
    f_u_xlat16_2.z = _165.z;
    f_u_xlatb0 = 0.0f < _HdrMultiply;
    float3 _176;
    if (f_u_xlatb0)
    {
        _176 = f_u_xlat16_2.xyz;
    }
    else
    {
        _176 = f_u_xlat16_1.xyz;
    }
    f_SV_Target0.x = _176.x;
    f_SV_Target0.y = _176.y;
    f_SV_Target0.z = _176.z;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SOFTPARTICLES_ON) && defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && defined(_FADING_ON) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float4 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_vs_TEXCOORD1;

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
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD1 = v_in_TEXCOORD1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static bool f_u_xlatb1;
static float4 f_u_xlat16_2;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy);
    f_u_xlatb1 = 0.0f < _SingleChannel;
    float3 _33;
    if (f_u_xlatb1)
    {
        _33 = 1.0f.xxx;
    }
    else
    {
        _33 = f_u_xlat16_0.xyz;
    }
    f_u_xlat16_2.x = _33.x;
    f_u_xlat16_2.y = _33.y;
    f_u_xlat16_2.z = _33.z;
    float _55;
    if (f_u_xlatb1)
    {
        _55 = f_u_xlat16_0.x;
    }
    else
    {
        _55 = f_u_xlat16_0.w;
    }
    f_u_xlat16_2.w = _55;
    f_u_xlat16_0 = f_u_xlat16_2 * f_vs_COLOR0;
    float3 _75 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlat16_2.x = _75.x;
    f_u_xlat16_2.y = _75.y;
    f_u_xlat16_2.z = _75.z;
    f_u_xlatb1 = 0.0f < _HdrMultiply;
    float3 _87;
    if (f_u_xlatb1)
    {
        _87 = f_u_xlat16_2.xyz;
    }
    else
    {
        _87 = f_u_xlat16_0.xyz;
    }
    f_SV_Target0.x = _87.x;
    f_SV_Target0.y = _87.y;
    f_SV_Target0.z = _87.z;
    f_SV_Target0.w = min(f_u_xlat16_0.w, 1.0f);
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SOFTPARTICLES_ON) && !defined(_ALPHABLEND_ON) && defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && defined(_FADING_ON) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float4 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_vs_TEXCOORD1;

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
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD1 = v_in_TEXCOORD1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static bool f_u_xlatb1;
static float4 f_u_xlat16_2;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy);
    f_u_xlatb1 = 0.0f < _SingleChannel;
    float3 _33;
    if (f_u_xlatb1)
    {
        _33 = 1.0f.xxx;
    }
    else
    {
        _33 = f_u_xlat16_0.xyz;
    }
    f_u_xlat16_2.x = _33.x;
    f_u_xlat16_2.y = _33.y;
    f_u_xlat16_2.z = _33.z;
    float _55;
    if (f_u_xlatb1)
    {
        _55 = f_u_xlat16_0.x;
    }
    else
    {
        _55 = f_u_xlat16_0.w;
    }
    f_u_xlat16_2.w = _55;
    f_u_xlat16_0 = f_u_xlat16_2 * f_vs_COLOR0;
    float3 _75 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlat16_2.x = _75.x;
    f_u_xlat16_2.y = _75.y;
    f_u_xlat16_2.z = _75.z;
    f_u_xlatb1 = 0.0f < _HdrMultiply;
    float3 _87;
    if (f_u_xlatb1)
    {
        _87 = f_u_xlat16_2.xyz;
    }
    else
    {
        _87 = f_u_xlat16_0.xyz;
    }
    f_SV_Target0.x = _87.x;
    f_SV_Target0.y = _87.y;
    f_SV_Target0.z = _87.z;
    f_SV_Target0.w = min(f_u_xlat16_0.w, 1.0f);
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(SOFTPARTICLES_ON) && !defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && !defined(_FADING_ON) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float4 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_vs_TEXCOORD1;

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
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD1 = v_in_TEXCOORD1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static bool f_u_xlatb1;
static float4 f_u_xlat16_2;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy);
    f_u_xlatb1 = 0.0f < _SingleChannel;
    float3 _33;
    if (f_u_xlatb1)
    {
        _33 = 1.0f.xxx;
    }
    else
    {
        _33 = f_u_xlat16_0.xyz;
    }
    f_u_xlat16_2.x = _33.x;
    f_u_xlat16_2.y = _33.y;
    f_u_xlat16_2.z = _33.z;
    float _55;
    if (f_u_xlatb1)
    {
        _55 = f_u_xlat16_0.x;
    }
    else
    {
        _55 = f_u_xlat16_0.w;
    }
    f_u_xlat16_2.w = _55;
    f_u_xlat16_0 = f_u_xlat16_2 * f_vs_COLOR0;
    float3 _75 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlat16_2.x = _75.x;
    f_u_xlat16_2.y = _75.y;
    f_u_xlat16_2.z = _75.z;
    f_u_xlatb1 = 0.0f < _HdrMultiply;
    float3 _87;
    if (f_u_xlatb1)
    {
        _87 = f_u_xlat16_2.xyz;
    }
    else
    {
        _87 = f_u_xlat16_0.xyz;
    }
    f_SV_Target0.x = _87.x;
    f_SV_Target0.y = _87.y;
    f_SV_Target0.z = _87.z;
    f_SV_Target0.w = min(f_u_xlat16_0.w, 1.0f);
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SOFTPARTICLES_ON) && !defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && !defined(_FADING_ON) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float4 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_vs_TEXCOORD1;

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
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD1 = v_in_TEXCOORD1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static bool f_u_xlatb1;
static float4 f_u_xlat16_2;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy);
    f_u_xlatb1 = 0.0f < _SingleChannel;
    float3 _33;
    if (f_u_xlatb1)
    {
        _33 = 1.0f.xxx;
    }
    else
    {
        _33 = f_u_xlat16_0.xyz;
    }
    f_u_xlat16_2.x = _33.x;
    f_u_xlat16_2.y = _33.y;
    f_u_xlat16_2.z = _33.z;
    float _55;
    if (f_u_xlatb1)
    {
        _55 = f_u_xlat16_0.x;
    }
    else
    {
        _55 = f_u_xlat16_0.w;
    }
    f_u_xlat16_2.w = _55;
    f_u_xlat16_0 = f_u_xlat16_2 * f_vs_COLOR0;
    float3 _75 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlat16_2.x = _75.x;
    f_u_xlat16_2.y = _75.y;
    f_u_xlat16_2.z = _75.z;
    f_u_xlatb1 = 0.0f < _HdrMultiply;
    float3 _87;
    if (f_u_xlatb1)
    {
        _87 = f_u_xlat16_2.xyz;
    }
    else
    {
        _87 = f_u_xlat16_0.xyz;
    }
    f_SV_Target0.x = _87.x;
    f_SV_Target0.y = _87.y;
    f_SV_Target0.z = _87.z;
    f_SV_Target0.w = min(f_u_xlat16_0.w, 1.0f);
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
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
Tags { "IgnoreProjector"="true" "LightMode"="ShadowCaster" "PreviewType"="Plane" "Queue"="Transparent" "RenderType"="Transparent" "SHADOWSUPPORT"="true" }
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
#pragma shader_feature_local _ _ALPHABLEND_ON
#pragma shader_feature_local _ _CFXR_ADDITIVE
#pragma shader_feature_local _ _CFXR_DISSOLVE
#pragma shader_feature_local _ _CFXR_DITHERED_SHADOWS_ON
#pragma shader_feature_local _ _CFXR_OVERLAYTEX_1X
#pragma shader_feature_local _ _CFXR_UV_DISTORTION
#pragma shader_feature_local _ _FLIPBOOK_BLENDING
#if defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(_ALPHABLEND_ON) && defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_DITHERED_SHADOWS_ON) && defined(_CFXR_OVERLAYTEX_1X) && defined(_CFXR_UV_DISTORTION) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
float4 _Time;
float4 _OverlayTex_Scroll;
Texture2D<float4> _OverlayTex;
SamplerState sampler_OverlayTex;
float4 _DistortScrolling;
float _UseUV2Distortion;
float _UVDistortionAdd;
float _SingleChannel;
Texture2D<float4> _DistortTex;
SamplerState sampler_DistortTex;
float _Distort;
float _FadeAlongU;
float _HdrMultiply;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _CFXR_OVERLAYBLEND;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD2;
static float4 v_in_TEXCOORD1;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_vs_TEXCOORD2 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD2 = v_in_TEXCOORD1;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _67 = v_u_xlat0;
    float3 _69 = v_u_xlat9.xxx * _67.xyz;
    v_u_xlat0.x = _69.x;
    v_u_xlat0.y = _69.y;
    v_u_xlat0.z = _69.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _116 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _116.x;
    v_u_xlat2.y = _116.y;
    v_u_xlat2.z = _116.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _132 = v_u_xlat2;
    float3 _134 = v_u_xlat9.xxx * _132.xyz;
    v_u_xlat2.x = _134.x;
    v_u_xlat2.y = _134.y;
    v_u_xlat2.z = _134.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _160 = v_u_xlat0;
    float3 _168 = ((-_160.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _168.x;
    v_u_xlat0.y = _168.y;
    v_u_xlat0.z = _168.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _184;
    if (v_u_xlatb9)
    {
        _184 = v_u_xlat0.xyz;
    }
    else
    {
        _184 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _184.x;
    v_u_xlat0.y = _184.y;
    v_u_xlat0.z = _184.z;
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
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
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

static float2 f_u_xlat0;
static float2 f_u_xlat10;
static float4 f_u_xlat16_0;
static float2 f_u_xlat5;
static bool3 f_u_xlatb1;
static float4 f_u_xlat1;
static float2 f_u_xlat16_5;
static float f_u_xlat15;
static bool3 f_u_xlatb2;
static float4 f_u_xlat16_3;
static float4 f_u_xlat16_4;
static float4 f_u_xlat16_1;
static float f_u_xlat16_19;
static float f_u_xlat16_9;

void f_frag_main()
{
    f_u_xlat0 = _Time.yy * _OverlayTex_Scroll.xy;
    f_u_xlat0 = frac(f_u_xlat0);
    f_u_xlat10 = f_vs_TEXCOORD1.zw + f_vs_TEXCOORD1.xy;
    f_u_xlat0 = (f_u_xlat10 * _OverlayTex_Scroll.zz) + f_u_xlat0;
    f_u_xlat16_0.x = _OverlayTex.Sample(sampler_OverlayTex, f_u_xlat0).x;
    f_u_xlat5 = _Time.yy * _DistortScrolling.xy;
    f_u_xlat5 = frac(f_u_xlat5);
    float4 _72 = float4(_UseUV2Distortion, _UVDistortionAdd, _SingleChannel, _UseUV2Distortion);
    f_u_xlatb1 = bool4(0.0f.xxxx.x < _72.x, 0.0f.xxxx.y < _72.y, 0.0f.xxxx.z < _72.z, 0.0f.xxxx.w < _72.w).xyz;
    float2 _81;
    if (f_u_xlatb1.x)
    {
        _81 = f_vs_TEXCOORD2.xy;
    }
    else
    {
        _81 = f_vs_TEXCOORD1.xy;
    }
    f_u_xlat1.x = _81.x;
    f_u_xlat1.w = _81.y;
    float4 _96 = f_u_xlat1;
    float2 _103 = (_96.xw * _DistortScrolling.zw) + f_vs_TEXCOORD1.zw;
    f_u_xlat1.x = _103.x;
    f_u_xlat1.w = _103.y;
    f_u_xlat5 += f_u_xlat1.xw;
    f_u_xlat16_5 = _DistortTex.Sample(sampler_DistortTex, f_u_xlat5).xy;
    float2 _124 = (f_u_xlat16_5 * 2.0f.xx) + (-1.0f).xx;
    f_u_xlat1.x = _124.x;
    f_u_xlat1.w = _124.y;
    f_u_xlat5 = f_u_xlat16_5 + (-f_vs_TEXCOORD1.xy);
    f_u_xlat5 = (_Distort.xx * f_u_xlat5) + f_vs_TEXCOORD1.xy;
    float4 _142 = f_u_xlat1;
    float2 _149 = (_142.xw * _Distort.xx) + f_vs_TEXCOORD1.xy;
    f_u_xlat1.x = _149.x;
    f_u_xlat1.w = _149.y;
    float2 _157;
    if (f_u_xlatb1.y)
    {
        _157 = f_u_xlat1.xw;
    }
    else
    {
        _157 = f_u_xlat5;
    }
    f_u_xlat5 = _157;
    float2 _169 = f_u_xlat5 + (-f_vs_TEXCOORD1.xy);
    f_u_xlat1.x = _169.x;
    f_u_xlat1.y = _169.y;
    f_u_xlat15 = f_vs_TEXCOORD1.y * 0.5f;
    float4 _182 = f_u_xlat1;
    float2 _187 = (f_u_xlat15.xx * _182.xy) + f_vs_TEXCOORD1.xy;
    f_u_xlat1.x = _187.x;
    f_u_xlat1.y = _187.y;
    float4 _198 = float4(_FadeAlongU, _HdrMultiply, _FadeAlongU, _FadeAlongU);
    bool2 _201 = bool4(0.0f.xxxx.x < _198.x, 0.0f.xxxx.y < _198.y, 0.0f.xxxx.z < _198.z, 0.0f.xxxx.w < _198.w).xy;
    f_u_xlatb2.x = _201.x;
    f_u_xlatb2.y = _201.y;
    float2 _208;
    if (f_u_xlatb2.x)
    {
        _208 = f_u_xlat1.xy;
    }
    else
    {
        _208 = f_u_xlat5;
    }
    f_u_xlat5 = _208;
    f_u_xlat16_3 = _MainTex.Sample(sampler_MainTex, f_u_xlat5);
    float3 _227;
    if (f_u_xlatb1.z)
    {
        _227 = 1.0f.xxx;
    }
    else
    {
        _227 = f_u_xlat16_3.xyz;
    }
    f_u_xlat16_4.x = _227.x;
    f_u_xlat16_4.y = _227.y;
    f_u_xlat16_4.z = _227.z;
    float _245;
    if (f_u_xlatb1.z)
    {
        _245 = f_u_xlat16_3.x;
    }
    else
    {
        _245 = f_u_xlat16_3.w;
    }
    f_u_xlat16_4.w = _245;
    f_u_xlat16_0 = f_u_xlat16_0.xxxx * f_u_xlat16_4;
    f_u_xlat16_1.x = f_u_xlat16_4.xyz.x;
    f_u_xlat16_1.y = f_u_xlat16_4.xyz.y;
    f_u_xlat16_1.z = f_u_xlat16_4.xyz.z;
    f_u_xlat16_4.x = f_u_xlat16_0.xyz.x;
    f_u_xlat16_4.y = f_u_xlat16_0.xyz.y;
    f_u_xlat16_4.z = f_u_xlat16_0.xyz.z;
    float4 _278 = _CFXR_OVERLAYBLEND.xxxx;
    bool2 _281 = bool4(_278.x == float4(2.0f, 0.0f, 1.0f, 0.0f).x, _278.y == float4(2.0f, 0.0f, 1.0f, 0.0f).y, _278.z == float4(2.0f, 0.0f, 1.0f, 0.0f).z, _278.w == float4(2.0f, 0.0f, 1.0f, 0.0f).w).xz;
    f_u_xlatb2.x = _281.x;
    f_u_xlatb2.z = _281.y;
    bool4 _290 = f_u_xlatb2.z.xxxx;
    f_u_xlat16_3 = float4(_290.x ? f_u_xlat16_4.x : f_u_xlat16_0.x, _290.y ? f_u_xlat16_4.y : f_u_xlat16_0.y, _290.z ? f_u_xlat16_4.z : f_u_xlat16_0.z, _290.w ? f_u_xlat16_4.w : f_u_xlat16_0.w);
    f_u_xlat16_1.w = f_u_xlat16_0.w;
    bool4 _299 = f_u_xlatb2.x.xxxx;
    f_u_xlat16_0 = float4(_299.x ? f_u_xlat16_1.x : f_u_xlat16_3.x, _299.y ? f_u_xlat16_1.y : f_u_xlat16_3.y, _299.z ? f_u_xlat16_1.z : f_u_xlat16_3.z, _299.w ? f_u_xlat16_1.w : f_u_xlat16_3.w);
    f_u_xlat16_0 *= f_vs_COLOR0;
    float3 _309 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlat16_4.x = _309.x;
    f_u_xlat16_4.y = _309.y;
    f_u_xlat16_4.z = _309.z;
    float3 _318;
    if (f_u_xlatb2.y)
    {
        _318 = f_u_xlat16_4.xyz;
    }
    else
    {
        _318 = f_u_xlat16_0.xyz;
    }
    f_u_xlat16_4.x = _318.x;
    f_u_xlat16_4.y = _318.y;
    f_u_xlat16_4.z = _318.z;
    f_u_xlat16_19 = min(f_u_xlat16_0.w, 1.0f);
    f_u_xlat16_9 = max(f_u_xlat16_4.z, f_u_xlat16_4.y);
    f_u_xlat16_4.x = max(f_u_xlat16_9, f_u_xlat16_4.x);
    f_u_xlat16_4.x = (f_u_xlat16_4.x * f_u_xlat16_19) + (-0.00999999977648258209228515625f);
    f_u_xlatb2.x = f_u_xlat16_4.x < 0.0f;
    if (f_u_xlatb2.x)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
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
#elif defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && defined(_CFXR_DISSOLVE) && defined(_CFXR_DITHERED_SHADOWS_ON) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
float4 _Time;
float2 _DissolveScroll;
float4 _DissolveTex_ST;
float _SingleChannel;
float _UseDissolveOffsetUV;
Texture2D<float4> _DissolveTex;
SamplerState sampler_DissolveTex;
float _InvertDissolveTex;
float _DissolveSmooth;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _DoubleDissolve;
float _ShadowStrength;
Texture3D<float4> _DitherMaskLOD;
SamplerState sampler_DitherMaskLOD;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD2;
static float4 v_in_TEXCOORD1;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_vs_TEXCOORD2 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD2 = v_in_TEXCOORD1;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _67 = v_u_xlat0;
    float3 _69 = v_u_xlat9.xxx * _67.xyz;
    v_u_xlat0.x = _69.x;
    v_u_xlat0.y = _69.y;
    v_u_xlat0.z = _69.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _116 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _116.x;
    v_u_xlat2.y = _116.y;
    v_u_xlat2.z = _116.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _132 = v_u_xlat2;
    float3 _134 = v_u_xlat9.xxx * _132.xyz;
    v_u_xlat2.x = _134.x;
    v_u_xlat2.y = _134.y;
    v_u_xlat2.z = _134.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _160 = v_u_xlat0;
    float3 _168 = ((-_160.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _168.x;
    v_u_xlat0.y = _168.y;
    v_u_xlat0.z = _168.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _184;
    if (v_u_xlatb9)
    {
        _184 = v_u_xlat0.xyz;
    }
    else
    {
        _184 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _184.x;
    v_u_xlat0.y = _184.y;
    v_u_xlat0.z = _184.z;
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
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float4 f_gl_FragCoord;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_TEXCOORD2;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD1 : TEXCOORD1;
    float4 f_vs_TEXCOORD2 : TEXCOORD2;
    float4 f_gl_FragCoord : SV_Position;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat0;
static float2 f_u_xlat6;
static bool2 f_u_xlatb6;
static float2 f_u_xlat16_0;
static float f_u_xlat16_1;
static float f_u_xlat3;
static float f_u_xlat16_4;
static bool f_u_xlatb0;
static float f_u_xlat16_7;
static float f_u_xlat16_10;
static float f_u_xlat16_2;
static float3 f_u_xlat1;

void f_frag_main()
{
    float4 hlslcc_FragCoord = float4(f_gl_FragCoord.xyz, 1.0f / f_gl_FragCoord.w);
    f_u_xlat0 = _Time.yy * float2(_DissolveScroll.x, _DissolveScroll.y);
    f_u_xlat0 = frac(f_u_xlat0);
    f_u_xlat6 = (f_vs_TEXCOORD1.xy * _DissolveTex_ST.xy) + _DissolveTex_ST.zw;
    f_u_xlat0 += f_u_xlat6;
    float4 _72 = float4(_SingleChannel, _UseDissolveOffsetUV, _SingleChannel, _UseDissolveOffsetUV);
    f_u_xlatb6 = bool4(0.0f.xxxx.x < _72.x, 0.0f.xxxx.y < _72.y, 0.0f.xxxx.z < _72.z, 0.0f.xxxx.w < _72.w).xy;
    float2 _80;
    if (f_u_xlatb6.y)
    {
        _80 = f_u_xlat0;
    }
    else
    {
        _80 = f_vs_TEXCOORD1.xy;
    }
    f_u_xlat0 = _80;
    f_u_xlat16_0.x = _DissolveTex.Sample(sampler_DissolveTex, f_u_xlat0).x;
    f_u_xlat16_1 = (f_u_xlat16_0.x * 2.0f) + (-1.0f);
    f_u_xlat3 = (f_u_xlat16_1 * f_vs_TEXCOORD2.z) + f_vs_TEXCOORD1.x;
    float _119;
    if (f_u_xlatb6.y)
    {
        _119 = f_u_xlat3;
    }
    else
    {
        _119 = f_u_xlat16_0.x;
    }
    f_u_xlat16_1 = _119;
    f_u_xlat16_4 = (-f_u_xlat16_1) + 1.0f;
    f_u_xlatb0 = 0.0f >= _InvertDissolveTex;
    f_u_xlat16_1 = f_u_xlatb0 ? f_u_xlat16_4 : f_u_xlat16_1;
    f_u_xlat16_4 = f_u_xlat16_1 + _DissolveSmooth;
    f_u_xlat16_1 += (-_DissolveSmooth);
    f_u_xlat16_4 = (-f_u_xlat16_1) + f_u_xlat16_4;
    f_u_xlat16_4 = 1.0f / f_u_xlat16_4;
    f_u_xlat16_7 = (_DissolveSmooth * 2.0f) + 1.0f;
    f_u_xlat16_7 = (f_vs_TEXCOORD2.x * f_u_xlat16_7) + (-_DissolveSmooth);
    f_u_xlat16_10 = (-f_u_xlat16_1) + f_u_xlat16_7;
    f_u_xlat16_10 = f_u_xlat16_4 * f_u_xlat16_10;
    f_u_xlat16_10 = clamp(f_u_xlat16_10, 0.0f, 1.0f);
    f_u_xlat16_2 = (f_u_xlat16_10 * (-2.0f)) + 3.0f;
    f_u_xlat16_10 *= f_u_xlat16_10;
    f_u_xlat16_10 *= f_u_xlat16_2;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD1.xy).xw;
    float _194;
    if (f_u_xlatb6.x)
    {
        _194 = f_u_xlat16_0.x;
    }
    else
    {
        _194 = f_u_xlat16_0.y;
    }
    f_u_xlat16_2 = _194;
    f_u_xlat16_2 *= f_vs_COLOR0.w;
    f_u_xlat16_10 *= f_u_xlat16_2;
    f_u_xlatb0 = 0.0f < _DoubleDissolve;
    float _215;
    if (f_u_xlatb0)
    {
        _215 = f_vs_TEXCOORD2.y;
    }
    else
    {
        _215 = 0.0f;
    }
    f_u_xlat16_2 = _215;
    f_u_xlat16_7 += (-f_u_xlat16_2);
    f_u_xlatb0 = 0.0f < f_u_xlat16_2;
    f_u_xlat16_1 = (-f_u_xlat16_1) + f_u_xlat16_7;
    f_u_xlat16_1 = f_u_xlat16_4 * f_u_xlat16_1;
    f_u_xlat16_1 = clamp(f_u_xlat16_1, 0.0f, 1.0f);
    f_u_xlat16_4 = (f_u_xlat16_1 * (-2.0f)) + 3.0f;
    f_u_xlat16_1 *= f_u_xlat16_1;
    f_u_xlat16_1 = ((-f_u_xlat16_4) * f_u_xlat16_1) + f_u_xlat16_10;
    f_u_xlat16_1 = clamp(f_u_xlat16_1, 0.0f, 1.0f);
    f_u_xlat16_1 = f_u_xlatb0 ? f_u_xlat16_1 : f_u_xlat16_10;
    f_u_xlat16_1 = min(f_u_xlat16_1, 1.0f);
    f_u_xlat0.x = min(f_u_xlat16_1, _ShadowStrength);
    f_u_xlat16_7 = f_u_xlat0.x * 0.9375f;
    f_u_xlat1.z = f_u_xlat16_7;
    float2 _274 = hlslcc_FragCoord.xy * 0.25f.xx;
    f_u_xlat1.x = _274.x;
    f_u_xlat1.y = _274.y;
    f_u_xlat16_0.x = _DitherMaskLOD.Sample(sampler_DitherMaskLOD, f_u_xlat1).w;
    f_u_xlat16_2 = f_u_xlat16_0.x + (-0.00999999977648258209228515625f);
    f_u_xlatb0 = f_u_xlat16_2 < 0.0f;
    if (f_u_xlatb0)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_gl_FragCoord = stage_input.f_gl_FragCoord;
    f_gl_FragCoord.w = 1.0 / f_gl_FragCoord.w;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && defined(_CFXR_DITHERED_SHADOWS_ON) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && defined(_FLIPBOOK_BLENDING)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _ShadowStrength;
Texture3D<float4> _DitherMaskLOD;
SamplerState sampler_DitherMaskLOD;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD2;
static float4 v_in_TEXCOORD1;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_vs_TEXCOORD2 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD2 = v_in_TEXCOORD1;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _67 = v_u_xlat0;
    float3 _69 = v_u_xlat9.xxx * _67.xyz;
    v_u_xlat0.x = _69.x;
    v_u_xlat0.y = _69.y;
    v_u_xlat0.z = _69.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _116 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _116.x;
    v_u_xlat2.y = _116.y;
    v_u_xlat2.z = _116.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _132 = v_u_xlat2;
    float3 _134 = v_u_xlat9.xxx * _132.xyz;
    v_u_xlat2.x = _134.x;
    v_u_xlat2.y = _134.y;
    v_u_xlat2.z = _134.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _160 = v_u_xlat0;
    float3 _168 = ((-_160.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _168.x;
    v_u_xlat0.y = _168.y;
    v_u_xlat0.z = _168.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _184;
    if (v_u_xlatb9)
    {
        _184 = v_u_xlat0.xyz;
    }
    else
    {
        _184 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _184.x;
    v_u_xlat0.y = _184.y;
    v_u_xlat0.z = _184.z;
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
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float4 f_gl_FragCoord;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_TEXCOORD2;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD1 : TEXCOORD1;
    float4 f_vs_TEXCOORD2 : TEXCOORD2;
    float4 f_gl_FragCoord : SV_Position;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat16_0;
static bool f_u_xlatb6;
static float f_u_xlat16_1;
static float f_u_xlat16_4;
static float f_u_xlat0;
static float f_u_xlat16_7;
static float3 f_u_xlat1;
static float f_u_xlat16_2;
static bool f_u_xlatb0;

void f_frag_main()
{
    float4 hlslcc_FragCoord = float4(f_gl_FragCoord.xyz, 1.0f / f_gl_FragCoord.w);
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD1.zw).xw;
    f_u_xlatb6 = 0.0f < _SingleChannel;
    float _51;
    if (f_u_xlatb6)
    {
        _51 = f_u_xlat16_0.x;
    }
    else
    {
        _51 = f_u_xlat16_0.y;
    }
    f_u_xlat16_1 = _51;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD1.xy).xw;
    float _69;
    if (f_u_xlatb6)
    {
        _69 = f_u_xlat16_0.x;
    }
    else
    {
        _69 = f_u_xlat16_0.y;
    }
    f_u_xlat16_4 = _69;
    f_u_xlat0 = (-f_u_xlat16_4) + f_u_xlat16_1;
    f_u_xlat0 = (f_vs_TEXCOORD2.y * f_u_xlat0) + f_u_xlat16_4;
    f_u_xlat16_1 = f_u_xlat0 * f_vs_COLOR0.w;
    f_u_xlat16_1 = min(f_u_xlat16_1, 1.0f);
    f_u_xlat0 = min(f_u_xlat16_1, _ShadowStrength);
    f_u_xlat16_7 = f_u_xlat0 * 0.9375f;
    f_u_xlat1.z = f_u_xlat16_7;
    float2 _114 = hlslcc_FragCoord.xy * 0.25f.xx;
    f_u_xlat1.x = _114.x;
    f_u_xlat1.y = _114.y;
    f_u_xlat16_0.x = _DitherMaskLOD.Sample(sampler_DitherMaskLOD, f_u_xlat1).w;
    f_u_xlat16_2 = f_u_xlat16_0.x + (-0.00999999977648258209228515625f);
    f_u_xlatb0 = f_u_xlat16_2 < 0.0f;
    if (f_u_xlatb0)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_gl_FragCoord = stage_input.f_gl_FragCoord;
    f_gl_FragCoord.w = 1.0 / f_gl_FragCoord.w;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SHADOWS_CUBE) && defined(SHADOWS_DEPTH) && !defined(_ALPHABLEND_ON) && defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_DITHERED_SHADOWS_ON) && defined(_CFXR_OVERLAYTEX_1X) && defined(_CFXR_UV_DISTORTION) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
float4 _Time;
float4 _OverlayTex_Scroll;
Texture2D<float4> _OverlayTex;
SamplerState sampler_OverlayTex;
float4 _DistortScrolling;
float _UseUV2Distortion;
float _UVDistortionAdd;
float _SingleChannel;
Texture2D<float4> _DistortTex;
SamplerState sampler_DistortTex;
float _Distort;
float _FadeAlongU;
float _HdrMultiply;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _CFXR_OVERLAYBLEND;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD2;
static float4 v_in_TEXCOORD1;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_vs_TEXCOORD2 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;
static float v_u_xlat6;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD2 = v_in_TEXCOORD1;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _67 = v_u_xlat0;
    float3 _69 = v_u_xlat9.xxx * _67.xyz;
    v_u_xlat0.x = _69.x;
    v_u_xlat0.y = _69.y;
    v_u_xlat0.z = _69.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _116 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _116.x;
    v_u_xlat2.y = _116.y;
    v_u_xlat2.z = _116.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _132 = v_u_xlat2;
    float3 _134 = v_u_xlat9.xxx * _132.xyz;
    v_u_xlat2.x = _134.x;
    v_u_xlat2.y = _134.y;
    v_u_xlat2.z = _134.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _160 = v_u_xlat0;
    float3 _168 = ((-_160.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _168.x;
    v_u_xlat0.y = _168.y;
    v_u_xlat0.z = _168.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _184;
    if (v_u_xlatb9)
    {
        _184 = v_u_xlat0.xyz;
    }
    else
    {
        _184 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _184.x;
    v_u_xlat0.y = _184.y;
    v_u_xlat0.z = _184.z;
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
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
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

static float2 f_u_xlat0;
static float2 f_u_xlat10;
static float4 f_u_xlat16_0;
static float2 f_u_xlat5;
static bool3 f_u_xlatb1;
static float4 f_u_xlat1;
static float2 f_u_xlat16_5;
static float f_u_xlat15;
static bool3 f_u_xlatb2;
static float4 f_u_xlat16_3;
static float4 f_u_xlat16_4;
static float4 f_u_xlat16_1;
static float f_u_xlat16_19;
static float f_u_xlat16_9;

void f_frag_main()
{
    f_u_xlat0 = _Time.yy * _OverlayTex_Scroll.xy;
    f_u_xlat0 = frac(f_u_xlat0);
    f_u_xlat10 = f_vs_TEXCOORD1.zw + f_vs_TEXCOORD1.xy;
    f_u_xlat0 = (f_u_xlat10 * _OverlayTex_Scroll.zz) + f_u_xlat0;
    f_u_xlat16_0.x = _OverlayTex.Sample(sampler_OverlayTex, f_u_xlat0).x;
    f_u_xlat5 = _Time.yy * _DistortScrolling.xy;
    f_u_xlat5 = frac(f_u_xlat5);
    float4 _72 = float4(_UseUV2Distortion, _UVDistortionAdd, _SingleChannel, _UseUV2Distortion);
    f_u_xlatb1 = bool4(0.0f.xxxx.x < _72.x, 0.0f.xxxx.y < _72.y, 0.0f.xxxx.z < _72.z, 0.0f.xxxx.w < _72.w).xyz;
    float2 _81;
    if (f_u_xlatb1.x)
    {
        _81 = f_vs_TEXCOORD2.xy;
    }
    else
    {
        _81 = f_vs_TEXCOORD1.xy;
    }
    f_u_xlat1.x = _81.x;
    f_u_xlat1.w = _81.y;
    float4 _96 = f_u_xlat1;
    float2 _103 = (_96.xw * _DistortScrolling.zw) + f_vs_TEXCOORD1.zw;
    f_u_xlat1.x = _103.x;
    f_u_xlat1.w = _103.y;
    f_u_xlat5 += f_u_xlat1.xw;
    f_u_xlat16_5 = _DistortTex.Sample(sampler_DistortTex, f_u_xlat5).xy;
    float2 _124 = (f_u_xlat16_5 * 2.0f.xx) + (-1.0f).xx;
    f_u_xlat1.x = _124.x;
    f_u_xlat1.w = _124.y;
    f_u_xlat5 = f_u_xlat16_5 + (-f_vs_TEXCOORD1.xy);
    f_u_xlat5 = (_Distort.xx * f_u_xlat5) + f_vs_TEXCOORD1.xy;
    float4 _142 = f_u_xlat1;
    float2 _149 = (_142.xw * _Distort.xx) + f_vs_TEXCOORD1.xy;
    f_u_xlat1.x = _149.x;
    f_u_xlat1.w = _149.y;
    float2 _157;
    if (f_u_xlatb1.y)
    {
        _157 = f_u_xlat1.xw;
    }
    else
    {
        _157 = f_u_xlat5;
    }
    f_u_xlat5 = _157;
    float2 _169 = f_u_xlat5 + (-f_vs_TEXCOORD1.xy);
    f_u_xlat1.x = _169.x;
    f_u_xlat1.y = _169.y;
    f_u_xlat15 = f_vs_TEXCOORD1.y * 0.5f;
    float4 _182 = f_u_xlat1;
    float2 _187 = (f_u_xlat15.xx * _182.xy) + f_vs_TEXCOORD1.xy;
    f_u_xlat1.x = _187.x;
    f_u_xlat1.y = _187.y;
    float4 _198 = float4(_FadeAlongU, _HdrMultiply, _FadeAlongU, _FadeAlongU);
    bool2 _201 = bool4(0.0f.xxxx.x < _198.x, 0.0f.xxxx.y < _198.y, 0.0f.xxxx.z < _198.z, 0.0f.xxxx.w < _198.w).xy;
    f_u_xlatb2.x = _201.x;
    f_u_xlatb2.y = _201.y;
    float2 _208;
    if (f_u_xlatb2.x)
    {
        _208 = f_u_xlat1.xy;
    }
    else
    {
        _208 = f_u_xlat5;
    }
    f_u_xlat5 = _208;
    f_u_xlat16_3 = _MainTex.Sample(sampler_MainTex, f_u_xlat5);
    float3 _227;
    if (f_u_xlatb1.z)
    {
        _227 = 1.0f.xxx;
    }
    else
    {
        _227 = f_u_xlat16_3.xyz;
    }
    f_u_xlat16_4.x = _227.x;
    f_u_xlat16_4.y = _227.y;
    f_u_xlat16_4.z = _227.z;
    float _245;
    if (f_u_xlatb1.z)
    {
        _245 = f_u_xlat16_3.x;
    }
    else
    {
        _245 = f_u_xlat16_3.w;
    }
    f_u_xlat16_4.w = _245;
    f_u_xlat16_0 = f_u_xlat16_0.xxxx * f_u_xlat16_4;
    f_u_xlat16_1.x = f_u_xlat16_4.xyz.x;
    f_u_xlat16_1.y = f_u_xlat16_4.xyz.y;
    f_u_xlat16_1.z = f_u_xlat16_4.xyz.z;
    f_u_xlat16_4.x = f_u_xlat16_0.xyz.x;
    f_u_xlat16_4.y = f_u_xlat16_0.xyz.y;
    f_u_xlat16_4.z = f_u_xlat16_0.xyz.z;
    float4 _278 = _CFXR_OVERLAYBLEND.xxxx;
    bool2 _281 = bool4(_278.x == float4(2.0f, 0.0f, 1.0f, 0.0f).x, _278.y == float4(2.0f, 0.0f, 1.0f, 0.0f).y, _278.z == float4(2.0f, 0.0f, 1.0f, 0.0f).z, _278.w == float4(2.0f, 0.0f, 1.0f, 0.0f).w).xz;
    f_u_xlatb2.x = _281.x;
    f_u_xlatb2.z = _281.y;
    bool4 _290 = f_u_xlatb2.z.xxxx;
    f_u_xlat16_3 = float4(_290.x ? f_u_xlat16_4.x : f_u_xlat16_0.x, _290.y ? f_u_xlat16_4.y : f_u_xlat16_0.y, _290.z ? f_u_xlat16_4.z : f_u_xlat16_0.z, _290.w ? f_u_xlat16_4.w : f_u_xlat16_0.w);
    f_u_xlat16_1.w = f_u_xlat16_0.w;
    bool4 _299 = f_u_xlatb2.x.xxxx;
    f_u_xlat16_0 = float4(_299.x ? f_u_xlat16_1.x : f_u_xlat16_3.x, _299.y ? f_u_xlat16_1.y : f_u_xlat16_3.y, _299.z ? f_u_xlat16_1.z : f_u_xlat16_3.z, _299.w ? f_u_xlat16_1.w : f_u_xlat16_3.w);
    f_u_xlat16_0 *= f_vs_COLOR0;
    float3 _309 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlat16_4.x = _309.x;
    f_u_xlat16_4.y = _309.y;
    f_u_xlat16_4.z = _309.z;
    float3 _318;
    if (f_u_xlatb2.y)
    {
        _318 = f_u_xlat16_4.xyz;
    }
    else
    {
        _318 = f_u_xlat16_0.xyz;
    }
    f_u_xlat16_4.x = _318.x;
    f_u_xlat16_4.y = _318.y;
    f_u_xlat16_4.z = _318.z;
    f_u_xlat16_19 = min(f_u_xlat16_0.w, 1.0f);
    f_u_xlat16_9 = max(f_u_xlat16_4.z, f_u_xlat16_4.y);
    f_u_xlat16_4.x = max(f_u_xlat16_9, f_u_xlat16_4.x);
    f_u_xlat16_4.x = (f_u_xlat16_4.x * f_u_xlat16_19) + (-0.00999999977648258209228515625f);
    f_u_xlatb2.x = f_u_xlat16_4.x < 0.0f;
    if (f_u_xlatb2.x)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
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
#elif !defined(SHADOWS_CUBE) && defined(SHADOWS_DEPTH) && defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && defined(_CFXR_DISSOLVE) && defined(_CFXR_DITHERED_SHADOWS_ON) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
float4 _Time;
float2 _DissolveScroll;
float4 _DissolveTex_ST;
float _SingleChannel;
float _UseDissolveOffsetUV;
Texture2D<float4> _DissolveTex;
SamplerState sampler_DissolveTex;
float _InvertDissolveTex;
float _DissolveSmooth;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _DoubleDissolve;
float _ShadowStrength;
Texture3D<float4> _DitherMaskLOD;
SamplerState sampler_DitherMaskLOD;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD2;
static float4 v_in_TEXCOORD1;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_vs_TEXCOORD2 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;
static float v_u_xlat6;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD2 = v_in_TEXCOORD1;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _67 = v_u_xlat0;
    float3 _69 = v_u_xlat9.xxx * _67.xyz;
    v_u_xlat0.x = _69.x;
    v_u_xlat0.y = _69.y;
    v_u_xlat0.z = _69.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _116 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _116.x;
    v_u_xlat2.y = _116.y;
    v_u_xlat2.z = _116.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _132 = v_u_xlat2;
    float3 _134 = v_u_xlat9.xxx * _132.xyz;
    v_u_xlat2.x = _134.x;
    v_u_xlat2.y = _134.y;
    v_u_xlat2.z = _134.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _160 = v_u_xlat0;
    float3 _168 = ((-_160.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _168.x;
    v_u_xlat0.y = _168.y;
    v_u_xlat0.z = _168.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _184;
    if (v_u_xlatb9)
    {
        _184 = v_u_xlat0.xyz;
    }
    else
    {
        _184 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _184.x;
    v_u_xlat0.y = _184.y;
    v_u_xlat0.z = _184.z;
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
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float4 f_gl_FragCoord;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_TEXCOORD2;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD1 : TEXCOORD1;
    float4 f_vs_TEXCOORD2 : TEXCOORD2;
    float4 f_gl_FragCoord : SV_Position;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat0;
static float2 f_u_xlat6;
static bool2 f_u_xlatb6;
static float2 f_u_xlat16_0;
static float f_u_xlat16_1;
static float f_u_xlat3;
static float f_u_xlat16_4;
static bool f_u_xlatb0;
static float f_u_xlat16_7;
static float f_u_xlat16_10;
static float f_u_xlat16_2;
static float3 f_u_xlat1;

void f_frag_main()
{
    float4 hlslcc_FragCoord = float4(f_gl_FragCoord.xyz, 1.0f / f_gl_FragCoord.w);
    f_u_xlat0 = _Time.yy * float2(_DissolveScroll.x, _DissolveScroll.y);
    f_u_xlat0 = frac(f_u_xlat0);
    f_u_xlat6 = (f_vs_TEXCOORD1.xy * _DissolveTex_ST.xy) + _DissolveTex_ST.zw;
    f_u_xlat0 += f_u_xlat6;
    float4 _72 = float4(_SingleChannel, _UseDissolveOffsetUV, _SingleChannel, _UseDissolveOffsetUV);
    f_u_xlatb6 = bool4(0.0f.xxxx.x < _72.x, 0.0f.xxxx.y < _72.y, 0.0f.xxxx.z < _72.z, 0.0f.xxxx.w < _72.w).xy;
    float2 _80;
    if (f_u_xlatb6.y)
    {
        _80 = f_u_xlat0;
    }
    else
    {
        _80 = f_vs_TEXCOORD1.xy;
    }
    f_u_xlat0 = _80;
    f_u_xlat16_0.x = _DissolveTex.Sample(sampler_DissolveTex, f_u_xlat0).x;
    f_u_xlat16_1 = (f_u_xlat16_0.x * 2.0f) + (-1.0f);
    f_u_xlat3 = (f_u_xlat16_1 * f_vs_TEXCOORD2.z) + f_vs_TEXCOORD1.x;
    float _119;
    if (f_u_xlatb6.y)
    {
        _119 = f_u_xlat3;
    }
    else
    {
        _119 = f_u_xlat16_0.x;
    }
    f_u_xlat16_1 = _119;
    f_u_xlat16_4 = (-f_u_xlat16_1) + 1.0f;
    f_u_xlatb0 = 0.0f >= _InvertDissolveTex;
    f_u_xlat16_1 = f_u_xlatb0 ? f_u_xlat16_4 : f_u_xlat16_1;
    f_u_xlat16_4 = f_u_xlat16_1 + _DissolveSmooth;
    f_u_xlat16_1 += (-_DissolveSmooth);
    f_u_xlat16_4 = (-f_u_xlat16_1) + f_u_xlat16_4;
    f_u_xlat16_4 = 1.0f / f_u_xlat16_4;
    f_u_xlat16_7 = (_DissolveSmooth * 2.0f) + 1.0f;
    f_u_xlat16_7 = (f_vs_TEXCOORD2.x * f_u_xlat16_7) + (-_DissolveSmooth);
    f_u_xlat16_10 = (-f_u_xlat16_1) + f_u_xlat16_7;
    f_u_xlat16_10 = f_u_xlat16_4 * f_u_xlat16_10;
    f_u_xlat16_10 = clamp(f_u_xlat16_10, 0.0f, 1.0f);
    f_u_xlat16_2 = (f_u_xlat16_10 * (-2.0f)) + 3.0f;
    f_u_xlat16_10 *= f_u_xlat16_10;
    f_u_xlat16_10 *= f_u_xlat16_2;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD1.xy).xw;
    float _194;
    if (f_u_xlatb6.x)
    {
        _194 = f_u_xlat16_0.x;
    }
    else
    {
        _194 = f_u_xlat16_0.y;
    }
    f_u_xlat16_2 = _194;
    f_u_xlat16_2 *= f_vs_COLOR0.w;
    f_u_xlat16_10 *= f_u_xlat16_2;
    f_u_xlatb0 = 0.0f < _DoubleDissolve;
    float _215;
    if (f_u_xlatb0)
    {
        _215 = f_vs_TEXCOORD2.y;
    }
    else
    {
        _215 = 0.0f;
    }
    f_u_xlat16_2 = _215;
    f_u_xlat16_7 += (-f_u_xlat16_2);
    f_u_xlatb0 = 0.0f < f_u_xlat16_2;
    f_u_xlat16_1 = (-f_u_xlat16_1) + f_u_xlat16_7;
    f_u_xlat16_1 = f_u_xlat16_4 * f_u_xlat16_1;
    f_u_xlat16_1 = clamp(f_u_xlat16_1, 0.0f, 1.0f);
    f_u_xlat16_4 = (f_u_xlat16_1 * (-2.0f)) + 3.0f;
    f_u_xlat16_1 *= f_u_xlat16_1;
    f_u_xlat16_1 = ((-f_u_xlat16_4) * f_u_xlat16_1) + f_u_xlat16_10;
    f_u_xlat16_1 = clamp(f_u_xlat16_1, 0.0f, 1.0f);
    f_u_xlat16_1 = f_u_xlatb0 ? f_u_xlat16_1 : f_u_xlat16_10;
    f_u_xlat16_1 = min(f_u_xlat16_1, 1.0f);
    f_u_xlat0.x = min(f_u_xlat16_1, _ShadowStrength);
    f_u_xlat16_7 = f_u_xlat0.x * 0.9375f;
    f_u_xlat1.z = f_u_xlat16_7;
    float2 _274 = hlslcc_FragCoord.xy * 0.25f.xx;
    f_u_xlat1.x = _274.x;
    f_u_xlat1.y = _274.y;
    f_u_xlat16_0.x = _DitherMaskLOD.Sample(sampler_DitherMaskLOD, f_u_xlat1).w;
    f_u_xlat16_2 = f_u_xlat16_0.x + (-0.00999999977648258209228515625f);
    f_u_xlatb0 = f_u_xlat16_2 < 0.0f;
    if (f_u_xlatb0)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_gl_FragCoord = stage_input.f_gl_FragCoord;
    f_gl_FragCoord.w = 1.0 / f_gl_FragCoord.w;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SHADOWS_CUBE) && defined(SHADOWS_DEPTH) && defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && defined(_CFXR_DITHERED_SHADOWS_ON) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && defined(_FLIPBOOK_BLENDING)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _ShadowStrength;
Texture3D<float4> _DitherMaskLOD;
SamplerState sampler_DitherMaskLOD;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD2;
static float4 v_in_TEXCOORD1;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_vs_TEXCOORD2 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;
static float v_u_xlat6;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD2 = v_in_TEXCOORD1;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _67 = v_u_xlat0;
    float3 _69 = v_u_xlat9.xxx * _67.xyz;
    v_u_xlat0.x = _69.x;
    v_u_xlat0.y = _69.y;
    v_u_xlat0.z = _69.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _116 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _116.x;
    v_u_xlat2.y = _116.y;
    v_u_xlat2.z = _116.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _132 = v_u_xlat2;
    float3 _134 = v_u_xlat9.xxx * _132.xyz;
    v_u_xlat2.x = _134.x;
    v_u_xlat2.y = _134.y;
    v_u_xlat2.z = _134.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _160 = v_u_xlat0;
    float3 _168 = ((-_160.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _168.x;
    v_u_xlat0.y = _168.y;
    v_u_xlat0.z = _168.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _184;
    if (v_u_xlatb9)
    {
        _184 = v_u_xlat0.xyz;
    }
    else
    {
        _184 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _184.x;
    v_u_xlat0.y = _184.y;
    v_u_xlat0.z = _184.z;
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
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float4 f_gl_FragCoord;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_TEXCOORD2;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD1 : TEXCOORD1;
    float4 f_vs_TEXCOORD2 : TEXCOORD2;
    float4 f_gl_FragCoord : SV_Position;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat16_0;
static bool f_u_xlatb6;
static float f_u_xlat16_1;
static float f_u_xlat16_4;
static float f_u_xlat0;
static float f_u_xlat16_7;
static float3 f_u_xlat1;
static float f_u_xlat16_2;
static bool f_u_xlatb0;

void f_frag_main()
{
    float4 hlslcc_FragCoord = float4(f_gl_FragCoord.xyz, 1.0f / f_gl_FragCoord.w);
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD1.zw).xw;
    f_u_xlatb6 = 0.0f < _SingleChannel;
    float _51;
    if (f_u_xlatb6)
    {
        _51 = f_u_xlat16_0.x;
    }
    else
    {
        _51 = f_u_xlat16_0.y;
    }
    f_u_xlat16_1 = _51;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD1.xy).xw;
    float _69;
    if (f_u_xlatb6)
    {
        _69 = f_u_xlat16_0.x;
    }
    else
    {
        _69 = f_u_xlat16_0.y;
    }
    f_u_xlat16_4 = _69;
    f_u_xlat0 = (-f_u_xlat16_4) + f_u_xlat16_1;
    f_u_xlat0 = (f_vs_TEXCOORD2.y * f_u_xlat0) + f_u_xlat16_4;
    f_u_xlat16_1 = f_u_xlat0 * f_vs_COLOR0.w;
    f_u_xlat16_1 = min(f_u_xlat16_1, 1.0f);
    f_u_xlat0 = min(f_u_xlat16_1, _ShadowStrength);
    f_u_xlat16_7 = f_u_xlat0 * 0.9375f;
    f_u_xlat1.z = f_u_xlat16_7;
    float2 _114 = hlslcc_FragCoord.xy * 0.25f.xx;
    f_u_xlat1.x = _114.x;
    f_u_xlat1.y = _114.y;
    f_u_xlat16_0.x = _DitherMaskLOD.Sample(sampler_DitherMaskLOD, f_u_xlat1).w;
    f_u_xlat16_2 = f_u_xlat16_0.x + (-0.00999999977648258209228515625f);
    f_u_xlatb0 = f_u_xlat16_2 < 0.0f;
    if (f_u_xlatb0)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_gl_FragCoord = stage_input.f_gl_FragCoord;
    f_gl_FragCoord.w = 1.0 / f_gl_FragCoord.w;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && defined(_CFXR_DITHERED_SHADOWS_ON) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _ShadowStrength;
Texture3D<float4> _DitherMaskLOD;
SamplerState sampler_DitherMaskLOD;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD0;
static float4 v_in_TEXCOORD1;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_vs_TEXCOORD2;
static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD2 = v_in_TEXCOORD1;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _67 = v_u_xlat0;
    float3 _69 = v_u_xlat9.xxx * _67.xyz;
    v_u_xlat0.x = _69.x;
    v_u_xlat0.y = _69.y;
    v_u_xlat0.z = _69.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _116 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _116.x;
    v_u_xlat2.y = _116.y;
    v_u_xlat2.z = _116.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _132 = v_u_xlat2;
    float3 _134 = v_u_xlat9.xxx * _132.xyz;
    v_u_xlat2.x = _134.x;
    v_u_xlat2.y = _134.y;
    v_u_xlat2.z = _134.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _160 = v_u_xlat0;
    float3 _168 = ((-_160.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _168.x;
    v_u_xlat0.y = _168.y;
    v_u_xlat0.z = _168.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _184;
    if (v_u_xlatb9)
    {
        _184 = v_u_xlat0.xyz;
    }
    else
    {
        _184 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _184.x;
    v_u_xlat0.y = _184.y;
    v_u_xlat0.z = _184.z;
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
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float4 f_gl_FragCoord;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD1 : TEXCOORD1;
    float4 f_gl_FragCoord : SV_Position;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat16_0;
static bool f_u_xlatb6;
static float f_u_xlat16_1;
static float f_u_xlat0;
static float f_u_xlat16_7;
static float3 f_u_xlat1;
static float f_u_xlat16_2;
static bool f_u_xlatb0;

void f_frag_main()
{
    float4 hlslcc_FragCoord = float4(f_gl_FragCoord.xyz, 1.0f / f_gl_FragCoord.w);
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD1.xy).xw;
    f_u_xlatb6 = 0.0f < _SingleChannel;
    float _51;
    if (f_u_xlatb6)
    {
        _51 = f_u_xlat16_0.x;
    }
    else
    {
        _51 = f_u_xlat16_0.y;
    }
    f_u_xlat16_1 = _51;
    f_u_xlat16_1 *= f_vs_COLOR0.w;
    f_u_xlat16_1 = min(f_u_xlat16_1, 1.0f);
    f_u_xlat0 = min(f_u_xlat16_1, _ShadowStrength);
    f_u_xlat16_7 = f_u_xlat0 * 0.9375f;
    f_u_xlat1.z = f_u_xlat16_7;
    float2 _87 = hlslcc_FragCoord.xy * 0.25f.xx;
    f_u_xlat1.x = _87.x;
    f_u_xlat1.y = _87.y;
    f_u_xlat16_0.x = _DitherMaskLOD.Sample(sampler_DitherMaskLOD, f_u_xlat1).w;
    f_u_xlat16_2 = f_u_xlat16_0.x + (-0.00999999977648258209228515625f);
    f_u_xlatb0 = f_u_xlat16_2 < 0.0f;
    if (f_u_xlatb0)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_gl_FragCoord = stage_input.f_gl_FragCoord;
    f_gl_FragCoord.w = 1.0 / f_gl_FragCoord.w;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SHADOWS_CUBE) && defined(SHADOWS_DEPTH) && defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && defined(_CFXR_DITHERED_SHADOWS_ON) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _ShadowStrength;
Texture3D<float4> _DitherMaskLOD;
SamplerState sampler_DitherMaskLOD;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD0;
static float4 v_in_TEXCOORD1;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_vs_TEXCOORD2;
static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;
static float v_u_xlat6;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD2 = v_in_TEXCOORD1;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _67 = v_u_xlat0;
    float3 _69 = v_u_xlat9.xxx * _67.xyz;
    v_u_xlat0.x = _69.x;
    v_u_xlat0.y = _69.y;
    v_u_xlat0.z = _69.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _116 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _116.x;
    v_u_xlat2.y = _116.y;
    v_u_xlat2.z = _116.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _132 = v_u_xlat2;
    float3 _134 = v_u_xlat9.xxx * _132.xyz;
    v_u_xlat2.x = _134.x;
    v_u_xlat2.y = _134.y;
    v_u_xlat2.z = _134.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _160 = v_u_xlat0;
    float3 _168 = ((-_160.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _168.x;
    v_u_xlat0.y = _168.y;
    v_u_xlat0.z = _168.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _184;
    if (v_u_xlatb9)
    {
        _184 = v_u_xlat0.xyz;
    }
    else
    {
        _184 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _184.x;
    v_u_xlat0.y = _184.y;
    v_u_xlat0.z = _184.z;
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
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float4 f_gl_FragCoord;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD1 : TEXCOORD1;
    float4 f_gl_FragCoord : SV_Position;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat16_0;
static bool f_u_xlatb6;
static float f_u_xlat16_1;
static float f_u_xlat0;
static float f_u_xlat16_7;
static float3 f_u_xlat1;
static float f_u_xlat16_2;
static bool f_u_xlatb0;

void f_frag_main()
{
    float4 hlslcc_FragCoord = float4(f_gl_FragCoord.xyz, 1.0f / f_gl_FragCoord.w);
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD1.xy).xw;
    f_u_xlatb6 = 0.0f < _SingleChannel;
    float _51;
    if (f_u_xlatb6)
    {
        _51 = f_u_xlat16_0.x;
    }
    else
    {
        _51 = f_u_xlat16_0.y;
    }
    f_u_xlat16_1 = _51;
    f_u_xlat16_1 *= f_vs_COLOR0.w;
    f_u_xlat16_1 = min(f_u_xlat16_1, 1.0f);
    f_u_xlat0 = min(f_u_xlat16_1, _ShadowStrength);
    f_u_xlat16_7 = f_u_xlat0 * 0.9375f;
    f_u_xlat1.z = f_u_xlat16_7;
    float2 _87 = hlslcc_FragCoord.xy * 0.25f.xx;
    f_u_xlat1.x = _87.x;
    f_u_xlat1.y = _87.y;
    f_u_xlat16_0.x = _DitherMaskLOD.Sample(sampler_DitherMaskLOD, f_u_xlat1).w;
    f_u_xlat16_2 = f_u_xlat16_0.x + (-0.00999999977648258209228515625f);
    f_u_xlatb0 = f_u_xlat16_2 < 0.0f;
    if (f_u_xlatb0)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_gl_FragCoord = stage_input.f_gl_FragCoord;
    f_gl_FragCoord.w = 1.0 / f_gl_FragCoord.w;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(_ALPHABLEND_ON) && defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_DITHERED_SHADOWS_ON) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD0;
static float4 v_in_TEXCOORD1;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_vs_TEXCOORD2;
static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD2 = v_in_TEXCOORD1;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _67 = v_u_xlat0;
    float3 _69 = v_u_xlat9.xxx * _67.xyz;
    v_u_xlat0.x = _69.x;
    v_u_xlat0.y = _69.y;
    v_u_xlat0.z = _69.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _116 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _116.x;
    v_u_xlat2.y = _116.y;
    v_u_xlat2.z = _116.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _132 = v_u_xlat2;
    float3 _134 = v_u_xlat9.xxx * _132.xyz;
    v_u_xlat2.x = _134.x;
    v_u_xlat2.y = _134.y;
    v_u_xlat2.z = _134.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _160 = v_u_xlat0;
    float3 _168 = ((-_160.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _168.x;
    v_u_xlat0.y = _168.y;
    v_u_xlat0.z = _168.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _184;
    if (v_u_xlatb9)
    {
        _184 = v_u_xlat0.xyz;
    }
    else
    {
        _184 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _184.x;
    v_u_xlat0.y = _184.y;
    v_u_xlat0.z = _184.z;
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
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD1 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static bool f_u_xlatb1;
static float4 f_u_xlat16_2;
static float f_u_xlat16_11;
static float f_u_xlat16_5;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD1.xy);
    f_u_xlatb1 = 0.0f < _SingleChannel;
    float3 _33;
    if (f_u_xlatb1)
    {
        _33 = 1.0f.xxx;
    }
    else
    {
        _33 = f_u_xlat16_0.xyz;
    }
    f_u_xlat16_2.x = _33.x;
    f_u_xlat16_2.y = _33.y;
    f_u_xlat16_2.z = _33.z;
    float _55;
    if (f_u_xlatb1)
    {
        _55 = f_u_xlat16_0.x;
    }
    else
    {
        _55 = f_u_xlat16_0.w;
    }
    f_u_xlat16_2.w = _55;
    f_u_xlat16_0 = f_u_xlat16_2 * f_vs_COLOR0;
    float3 _75 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlat16_2.x = _75.x;
    f_u_xlat16_2.y = _75.y;
    f_u_xlat16_2.z = _75.z;
    f_u_xlatb1 = 0.0f < _HdrMultiply;
    float3 _85;
    if (f_u_xlatb1)
    {
        _85 = f_u_xlat16_2.xyz;
    }
    else
    {
        _85 = f_u_xlat16_0.xyz;
    }
    f_u_xlat16_2.x = _85.x;
    f_u_xlat16_2.y = _85.y;
    f_u_xlat16_2.z = _85.z;
    f_u_xlat16_11 = min(f_u_xlat16_0.w, 1.0f);
    f_u_xlat16_5 = max(f_u_xlat16_2.z, f_u_xlat16_2.y);
    f_u_xlat16_2.x = max(f_u_xlat16_5, f_u_xlat16_2.x);
    f_u_xlat16_2.x = (f_u_xlat16_2.x * f_u_xlat16_11) + (-0.00999999977648258209228515625f);
    f_u_xlatb1 = f_u_xlat16_2.x < 0.0f;
    if (f_u_xlatb1)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SHADOWS_CUBE) && defined(SHADOWS_DEPTH) && !defined(_ALPHABLEND_ON) && defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_DITHERED_SHADOWS_ON) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
float _HdrMultiply;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD0;
static float4 v_in_TEXCOORD1;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_vs_TEXCOORD2;
static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;
static float v_u_xlat6;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD2 = v_in_TEXCOORD1;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _67 = v_u_xlat0;
    float3 _69 = v_u_xlat9.xxx * _67.xyz;
    v_u_xlat0.x = _69.x;
    v_u_xlat0.y = _69.y;
    v_u_xlat0.z = _69.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _116 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _116.x;
    v_u_xlat2.y = _116.y;
    v_u_xlat2.z = _116.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _132 = v_u_xlat2;
    float3 _134 = v_u_xlat9.xxx * _132.xyz;
    v_u_xlat2.x = _134.x;
    v_u_xlat2.y = _134.y;
    v_u_xlat2.z = _134.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _160 = v_u_xlat0;
    float3 _168 = ((-_160.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _168.x;
    v_u_xlat0.y = _168.y;
    v_u_xlat0.z = _168.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _184;
    if (v_u_xlatb9)
    {
        _184 = v_u_xlat0.xyz;
    }
    else
    {
        _184 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _184.x;
    v_u_xlat0.y = _184.y;
    v_u_xlat0.z = _184.z;
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
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD1 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static bool f_u_xlatb1;
static float4 f_u_xlat16_2;
static float f_u_xlat16_11;
static float f_u_xlat16_5;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD1.xy);
    f_u_xlatb1 = 0.0f < _SingleChannel;
    float3 _33;
    if (f_u_xlatb1)
    {
        _33 = 1.0f.xxx;
    }
    else
    {
        _33 = f_u_xlat16_0.xyz;
    }
    f_u_xlat16_2.x = _33.x;
    f_u_xlat16_2.y = _33.y;
    f_u_xlat16_2.z = _33.z;
    float _55;
    if (f_u_xlatb1)
    {
        _55 = f_u_xlat16_0.x;
    }
    else
    {
        _55 = f_u_xlat16_0.w;
    }
    f_u_xlat16_2.w = _55;
    f_u_xlat16_0 = f_u_xlat16_2 * f_vs_COLOR0;
    float3 _75 = f_u_xlat16_0.xyz * _HdrMultiply.xxx;
    f_u_xlat16_2.x = _75.x;
    f_u_xlat16_2.y = _75.y;
    f_u_xlat16_2.z = _75.z;
    f_u_xlatb1 = 0.0f < _HdrMultiply;
    float3 _85;
    if (f_u_xlatb1)
    {
        _85 = f_u_xlat16_2.xyz;
    }
    else
    {
        _85 = f_u_xlat16_0.xyz;
    }
    f_u_xlat16_2.x = _85.x;
    f_u_xlat16_2.y = _85.y;
    f_u_xlat16_2.z = _85.z;
    f_u_xlat16_11 = min(f_u_xlat16_0.w, 1.0f);
    f_u_xlat16_5 = max(f_u_xlat16_2.z, f_u_xlat16_2.y);
    f_u_xlat16_2.x = max(f_u_xlat16_5, f_u_xlat16_2.x);
    f_u_xlat16_2.x = (f_u_xlat16_2.x * f_u_xlat16_11) + (-0.00999999977648258209228515625f);
    f_u_xlatb1 = f_u_xlat16_2.x < 0.0f;
    if (f_u_xlatb1)
    {
        discard;
    }
    f_SV_Target0 = 0.0f.xxxx;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(SHADOWS_CUBE) && defined(SHADOWS_DEPTH) && !defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_DITHERED_SHADOWS_ON) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD0;
static float4 v_in_TEXCOORD1;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_vs_TEXCOORD2;
static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;
static float v_u_xlat6;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD2 = v_in_TEXCOORD1;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _67 = v_u_xlat0;
    float3 _69 = v_u_xlat9.xxx * _67.xyz;
    v_u_xlat0.x = _69.x;
    v_u_xlat0.y = _69.y;
    v_u_xlat0.z = _69.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _116 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _116.x;
    v_u_xlat2.y = _116.y;
    v_u_xlat2.z = _116.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _132 = v_u_xlat2;
    float3 _134 = v_u_xlat9.xxx * _132.xyz;
    v_u_xlat2.x = _134.x;
    v_u_xlat2.y = _134.y;
    v_u_xlat2.z = _134.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _160 = v_u_xlat0;
    float3 _168 = ((-_160.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _168.x;
    v_u_xlat0.y = _168.y;
    v_u_xlat0.z = _168.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _184;
    if (v_u_xlatb9)
    {
        _184 = v_u_xlat0.xyz;
    }
    else
    {
        _184 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _184.x;
    v_u_xlat0.y = _184.y;
    v_u_xlat0.z = _184.z;
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
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD1 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat16_0;
static bool f_u_xlatb4;
static float f_u_xlat16_1;
static bool f_u_xlatb0;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD1.xy).xw;
    f_u_xlatb4 = 0.0f < _SingleChannel;
    float _34;
    if (f_u_xlatb4)
    {
        _34 = f_u_xlat16_0.x;
    }
    else
    {
        _34 = f_u_xlat16_0.y;
    }
    f_u_xlat16_1 = _34;
    f_u_xlat16_1 *= f_vs_COLOR0.w;
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
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(SHADOWS_CUBE) && !defined(SHADOWS_DEPTH) && !defined(_ALPHABLEND_ON) && !defined(_CFXR_ADDITIVE) && !defined(_CFXR_DISSOLVE) && !defined(_CFXR_DITHERED_SHADOWS_ON) && !defined(_CFXR_OVERLAYTEX_1X) && !defined(_CFXR_UV_DISTORTION) && !defined(_FLIPBOOK_BLENDING)
float4x4 unity_WorldToObject;
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _WorldSpaceLightPos0;
float4 unity_LightShadowBias;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _SingleChannel;
static float4 v_gl_Position;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;
static float4 v_in_TEXCOORD0;
static float4 v_in_TEXCOORD1;
static float3 v_in_NORMAL0;
static float4 v_in_POSITION0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float4 v_in_TEXCOORD1 : TEXCOORD1;
    float3 v_in_NORMAL0 : NORMAL0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
    float4 v_gl_Position : SV_Position;
};

static float4 v_vs_TEXCOORD2;
static float4 v_u_xlat0;
static float v_u_xlat9;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static bool v_u_xlatb9;

void v_vert_main()
{
    v_vs_COLOR0 = v_in_COLOR0;
    v_vs_TEXCOORD1 = v_in_TEXCOORD0.xyxy;
    v_vs_TEXCOORD2 = v_in_TEXCOORD1;
    v_u_xlat0.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat0.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat0.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat0.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _67 = v_u_xlat0;
    float3 _69 = v_u_xlat9.xxx * _67.xyz;
    v_u_xlat0.x = _69.x;
    v_u_xlat0.y = _69.y;
    v_u_xlat0.z = _69.z;
    v_u_xlat1 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[3] * v_in_POSITION0.wwww) + v_u_xlat1;
    float3 _116 = ((-v_u_xlat1.xyz) * _WorldSpaceLightPos0.www) + _WorldSpaceLightPos0.xyz;
    v_u_xlat2.x = _116.x;
    v_u_xlat2.y = _116.y;
    v_u_xlat2.z = _116.z;
    v_u_xlat9 = dot(v_u_xlat2.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = rsqrt(v_u_xlat9);
    float4 _132 = v_u_xlat2;
    float3 _134 = v_u_xlat9.xxx * _132.xyz;
    v_u_xlat2.x = _134.x;
    v_u_xlat2.y = _134.y;
    v_u_xlat2.z = _134.z;
    v_u_xlat9 = dot(v_u_xlat0.xyz, v_u_xlat2.xyz);
    v_u_xlat9 = ((-v_u_xlat9) * v_u_xlat9) + 1.0f;
    v_u_xlat9 = sqrt(v_u_xlat9);
    v_u_xlat9 *= unity_LightShadowBias.z;
    float4 _160 = v_u_xlat0;
    float3 _168 = ((-_160.xyz) * v_u_xlat9.xxx) + v_u_xlat1.xyz;
    v_u_xlat0.x = _168.x;
    v_u_xlat0.y = _168.y;
    v_u_xlat0.z = _168.z;
    v_u_xlatb9 = unity_LightShadowBias.z != 0.0f;
    float3 _184;
    if (v_u_xlatb9)
    {
        _184 = v_u_xlat0.xyz;
    }
    else
    {
        _184 = v_u_xlat1.xyz;
    }
    v_u_xlat0.x = _184.x;
    v_u_xlat0.y = _184.y;
    v_u_xlat0.z = _184.z;
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
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD1 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float2 f_u_xlat16_0;
static bool f_u_xlatb4;
static float f_u_xlat16_1;
static bool f_u_xlatb0;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD1.xy).xw;
    f_u_xlatb4 = 0.0f < _SingleChannel;
    float _34;
    if (f_u_xlatb4)
    {
        _34 = f_u_xlat16_0.x;
    }
    else
    {
        _34 = f_u_xlat16_0.y;
    }
    f_u_xlat16_1 = _34;
    f_u_xlat16_1 *= f_vs_COLOR0.w;
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
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
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
