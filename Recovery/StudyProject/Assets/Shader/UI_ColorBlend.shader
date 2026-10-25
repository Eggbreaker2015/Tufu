Shader "UI/ColorBlend" {
Properties {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Vector) = (1,1,1,1)
		_BlendColor ("Blend Color", Vector) = (1,1,1,0)
		_BlendIntensity ("Blend Intensity", Range(0, 1)) = 1
		_BlendMode ("Blend Mode", Float) = 0
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}
// Recovered from the APK GLES programs; generated HLSL, not original author source.
SubShader {
Tags { "CanUseSpriteAtlas"="true" "IgnoreProjector"="true" "PreviewType"="Plane" "Queue"="Transparent" "RenderType"="Transparent" }
Pass {
Name "Default"
Tags { "CanUseSpriteAtlas"="true" "IgnoreProjector"="true" "PreviewType"="Plane" "Queue"="Transparent" "RenderType"="Transparent" }
Cull Off
ZWrite Off
ZTest [unity_GUIZTestMode]
Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
ColorMask [_ColorMask]
BlendOp Add, Add
Stencil {
Ref [_Stencil]
ReadMask [_StencilReadMask]
WriteMask [_StencilWriteMask]
Comp [_StencilComp]
Pass [_StencilOp]
Fail Keep
ZFail Keep
}
HLSLPROGRAM
#pragma target 4.5
#pragma vertex RecoveryVert
#pragma fragment RecoveryFrag
#pragma shader_feature_local _ UNITY_UI_ALPHACLIP
#pragma shader_feature_local _ UNITY_UI_CLIP_RECT
#if defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _Color;
float4 _MainTex_ST;
float4 _ClipRect;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _TextureSampleAdd;
float4 _BlendColor;
float _BlendMode;
float _BlendIntensity;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float2 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float2 v_vs_TEXCOORD0 : TEXCOORD1;
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
    v_u_xlat0 = v_in_COLOR0 * _Color;
    v_vs_COLOR0 = v_u_xlat0;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_vs_TEXCOORD1 = v_in_POSITION0;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float4 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float2 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static bool4 f_u_xlatb0;
static float4 f_u_xlat0;
static float4 f_u_xlat16_1;
static float4 f_u_xlat1;
static float4 f_u_xlat2;
static float3 f_u_xlat16_3;
static bool3 f_u_xlatb6;
static float3 f_u_xlat16_4;
static float3 f_u_xlat16_5;
static float f_u_xlat16_21;

void f_frag_main()
{
    bool2 _22 = bool4(f_vs_TEXCOORD1.xyxx.x >= _ClipRect.xyxx.x, f_vs_TEXCOORD1.xyxx.y >= _ClipRect.xyxx.y, f_vs_TEXCOORD1.xyxx.z >= _ClipRect.xyxx.z, f_vs_TEXCOORD1.xyxx.w >= _ClipRect.xyxx.w).xy;
    f_u_xlatb0.x = _22.x;
    f_u_xlatb0.y = _22.y;
    bool2 _36 = bool4(_ClipRect.zzzw.x >= f_vs_TEXCOORD1.xxxy.x, _ClipRect.zzzw.y >= f_vs_TEXCOORD1.xxxy.y, _ClipRect.zzzw.z >= f_vs_TEXCOORD1.xxxy.z, _ClipRect.zzzw.w >= f_vs_TEXCOORD1.xxxy.w).zw;
    f_u_xlatb0.z = _36.x;
    f_u_xlatb0.w = _36.y;
    f_u_xlat0.x = float(f_u_xlatb0.x);
    f_u_xlat0.y = float(f_u_xlatb0.y);
    f_u_xlat0.z = float(f_u_xlatb0.z);
    f_u_xlat0.w = float(f_u_xlatb0.w);
    float4 _65 = f_u_xlat0;
    float4 _67 = f_u_xlat0;
    float2 _69 = _65.zw * _67.xy;
    f_u_xlat0.x = _69.x;
    f_u_xlat0.y = _69.y;
    f_u_xlat0.x = f_u_xlat0.y * f_u_xlat0.x;
    f_u_xlat16_1 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0);
    f_u_xlat1 = f_u_xlat16_1 + _TextureSampleAdd;
    f_u_xlat2 = f_u_xlat1 * f_vs_COLOR0;
    f_u_xlat16_3.x = (f_u_xlat2.w * f_u_xlat0.x) + (-0.001000000047497451305389404296875f);
    f_u_xlat0.x *= f_u_xlat2.w;
    f_u_xlatb6.x = f_u_xlat16_3.x < 0.0f;
    if (f_u_xlatb6.x)
    {
        discard;
    }
    f_u_xlat16_3 = ((-f_u_xlat1.xyz) * f_vs_COLOR0.xyz) + 1.0f.xxx;
    f_u_xlat16_4 = f_u_xlat16_3 + f_u_xlat16_3;
    f_u_xlat16_5 = (-_BlendColor.xyz) + 1.0f.xxx;
    f_u_xlat16_4 = ((-f_u_xlat16_4) * f_u_xlat16_5) + 1.0f.xxx;
    f_u_xlat16_3 = ((-f_u_xlat16_3) * f_u_xlat16_5) + 1.0f.xxx;
    f_u_xlat16_5 = f_u_xlat2.xyz + f_u_xlat2.xyz;
    f_u_xlat16_5 *= _BlendColor.xyz;
    f_u_xlatb6 = bool4(f_u_xlat2.xyzz.x < 0.5f.xxxx.x, f_u_xlat2.xyzz.y < 0.5f.xxxx.y, f_u_xlat2.xyzz.z < 0.5f.xxxx.z, f_u_xlat2.xyzz.w < 0.5f.xxxx.w).xyz;
    float3 hlslcc_movcTemp = f_u_xlat16_4;
    float _178;
    if (f_u_xlatb6.x)
    {
        _178 = f_u_xlat16_5.x;
    }
    else
    {
        _178 = f_u_xlat16_4.x;
    }
    hlslcc_movcTemp.x = _178;
    float _190;
    if (f_u_xlatb6.y)
    {
        _190 = f_u_xlat16_5.y;
    }
    else
    {
        _190 = f_u_xlat16_4.y;
    }
    hlslcc_movcTemp.y = _190;
    float _202;
    if (f_u_xlatb6.z)
    {
        _202 = f_u_xlat16_5.z;
    }
    else
    {
        _202 = f_u_xlat16_4.z;
    }
    hlslcc_movcTemp.z = _202;
    f_u_xlat16_4 = hlslcc_movcTemp;
    f_u_xlat16_5 = (f_u_xlat1.xyz * f_vs_COLOR0.xyz) + _BlendColor.xyz;
    f_u_xlat16_5 = clamp(f_u_xlat16_5, 0.0f.xxx, 1.0f.xxx);
    float4 _228 = _BlendMode.xxxx;
    bool2 _233 = bool4(_228.x < float4(1.5f, 2.5f, 0.0f, 0.0f).x, _228.y < float4(1.5f, 2.5f, 0.0f, 0.0f).y, _228.z < float4(1.5f, 2.5f, 0.0f, 0.0f).z, _228.w < float4(1.5f, 2.5f, 0.0f, 0.0f).w).xy;
    f_u_xlatb6.x = _233.x;
    f_u_xlatb6.y = _233.y;
    float3 _240;
    if (f_u_xlatb6.y)
    {
        _240 = f_u_xlat16_5;
    }
    else
    {
        _240 = f_u_xlat16_3;
    }
    f_u_xlat16_3 = _240;
    float3 _249;
    if (f_u_xlatb6.x)
    {
        _249 = f_u_xlat16_4;
    }
    else
    {
        _249 = f_u_xlat16_3;
    }
    f_u_xlat16_3 = _249;
    f_u_xlatb6.x = _BlendMode >= 0.5f;
    float3 _261;
    if (f_u_xlatb6.x)
    {
        _261 = f_u_xlat16_3;
    }
    else
    {
        _261 = _BlendColor.xyz;
    }
    f_u_xlat16_3 = _261;
    f_u_xlat16_3 = ((-f_u_xlat1.xyz) * f_vs_COLOR0.xyz) + f_u_xlat16_3;
    f_u_xlat16_21 = _BlendColor.w * _BlendIntensity;
    f_u_xlat16_3 = (f_u_xlat16_21.xxx * f_u_xlat16_3) + f_u_xlat2.xyz;
    float3 _295 = f_u_xlat0.xxx * f_u_xlat16_3;
    f_SV_Target0.x = _295.x;
    f_SV_Target0.y = _295.y;
    f_SV_Target0.z = _295.z;
    f_SV_Target0.w = f_u_xlat0.x;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _Color;
float4 _MainTex_ST;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _TextureSampleAdd;
float4 _BlendColor;
float _BlendMode;
float _BlendIntensity;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float2 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float2 v_vs_TEXCOORD0 : TEXCOORD1;
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
    v_u_xlat0 = v_in_COLOR0 * _Color;
    v_vs_COLOR0 = v_u_xlat0;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_vs_TEXCOORD1 = v_in_POSITION0;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    return stage_output;
}
static float2 f_vs_TEXCOORD0;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float2 f_vs_TEXCOORD0 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float4 f_u_xlat0;
static float3 f_u_xlat16_1;
static bool2 f_u_xlatb2;
static float3 f_u_xlat16_3;
static float3 f_u_xlat16_4;
static float3 f_u_xlat16_5;
static float4 f_u_xlat4;
static bool3 f_u_xlatb8;
static bool f_u_xlatb18;
static float f_u_xlat16_19;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0);
    f_u_xlat0 = f_u_xlat16_0 + _TextureSampleAdd;
    f_u_xlat16_1.x = (f_u_xlat0.w * f_vs_COLOR0.w) + (-0.001000000047497451305389404296875f);
    f_u_xlatb2.x = f_u_xlat16_1.x < 0.0f;
    if (f_u_xlatb2.x)
    {
        discard;
    }
    f_u_xlat16_1 = (f_u_xlat0.xyz * f_vs_COLOR0.xyz) + _BlendColor.xyz;
    f_u_xlat16_1 = clamp(f_u_xlat16_1, 0.0f.xxx, 1.0f.xxx);
    f_u_xlat16_3 = ((-f_u_xlat0.xyz) * f_vs_COLOR0.xyz) + 1.0f.xxx;
    f_u_xlat16_4 = (-_BlendColor.xyz) + 1.0f.xxx;
    f_u_xlat16_5 = ((-f_u_xlat16_3) * f_u_xlat16_4) + 1.0f.xxx;
    f_u_xlat16_3 += f_u_xlat16_3;
    f_u_xlat16_3 = ((-f_u_xlat16_3) * f_u_xlat16_4) + 1.0f.xxx;
    float4 _104 = _BlendMode.xxxx;
    f_u_xlatb2 = bool4(_104.x < float4(1.5f, 2.5f, 0.0f, 0.0f).x, _104.y < float4(1.5f, 2.5f, 0.0f, 0.0f).y, _104.z < float4(1.5f, 2.5f, 0.0f, 0.0f).z, _104.w < float4(1.5f, 2.5f, 0.0f, 0.0f).w).xy;
    float3 _115;
    if (f_u_xlatb2.y)
    {
        _115 = f_u_xlat16_1;
    }
    else
    {
        _115 = f_u_xlat16_5;
    }
    f_u_xlat16_1 = _115;
    f_u_xlat4 = f_u_xlat0 * f_vs_COLOR0;
    f_u_xlat16_5 = f_u_xlat4.xyz + f_u_xlat4.xyz;
    f_u_xlat16_5 *= _BlendColor.xyz;
    f_u_xlatb8 = bool4(f_u_xlat4.xyzz.x < 0.5f.xxxx.x, f_u_xlat4.xyzz.y < 0.5f.xxxx.y, f_u_xlat4.xyzz.z < 0.5f.xxxx.z, f_u_xlat4.xyzz.w < 0.5f.xxxx.w).xyz;
    float3 hlslcc_movcTemp = f_u_xlat16_3;
    float _149;
    if (f_u_xlatb8.x)
    {
        _149 = f_u_xlat16_5.x;
    }
    else
    {
        _149 = f_u_xlat16_3.x;
    }
    hlslcc_movcTemp.x = _149;
    float _161;
    if (f_u_xlatb8.y)
    {
        _161 = f_u_xlat16_5.y;
    }
    else
    {
        _161 = f_u_xlat16_3.y;
    }
    hlslcc_movcTemp.y = _161;
    float _174;
    if (f_u_xlatb8.z)
    {
        _174 = f_u_xlat16_5.z;
    }
    else
    {
        _174 = f_u_xlat16_3.z;
    }
    hlslcc_movcTemp.z = _174;
    f_u_xlat16_3 = hlslcc_movcTemp;
    float3 _187;
    if (f_u_xlatb2.x)
    {
        _187 = f_u_xlat16_3;
    }
    else
    {
        _187 = f_u_xlat16_1;
    }
    f_u_xlat16_1 = _187;
    f_u_xlatb18 = _BlendMode >= 0.5f;
    float3 _198;
    if (f_u_xlatb18)
    {
        _198 = f_u_xlat16_1;
    }
    else
    {
        _198 = _BlendColor.xyz;
    }
    f_u_xlat16_1 = _198;
    f_u_xlat16_1 = ((-f_u_xlat0.xyz) * f_vs_COLOR0.xyz) + f_u_xlat16_1;
    f_u_xlat16_19 = _BlendColor.w * _BlendIntensity;
    f_u_xlat16_1 = (f_u_xlat16_19.xxx * f_u_xlat16_1) + f_u_xlat4.xyz;
    float3 _232 = f_u_xlat4.www * f_u_xlat16_1;
    f_SV_Target0.x = _232.x;
    f_SV_Target0.y = _232.y;
    f_SV_Target0.z = _232.z;
    f_SV_Target0.w = f_u_xlat4.w;
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
#elif !defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _Color;
float4 _MainTex_ST;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _TextureSampleAdd;
float4 _BlendColor;
float _BlendMode;
float _BlendIntensity;
float4 _ClipRect;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float2 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float2 v_vs_TEXCOORD0 : TEXCOORD1;
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
    v_u_xlat0 = v_in_COLOR0 * _Color;
    v_vs_COLOR0 = v_u_xlat0;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_vs_TEXCOORD1 = v_in_POSITION0;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float2 f_vs_TEXCOORD0;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD1;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float2 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float4 f_u_xlat0;
static float3 f_u_xlat16_1;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_3;
static float4 f_u_xlat3;
static float3 f_u_xlat16_4;
static bool3 f_u_xlatb5;
static bool f_u_xlatb18;
static float f_u_xlat16_19;
static bool4 f_u_xlatb0;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0);
    f_u_xlat0 = f_u_xlat16_0 + _TextureSampleAdd;
    f_u_xlat16_1 = ((-f_u_xlat0.xyz) * f_vs_COLOR0.xyz) + 1.0f.xxx;
    f_u_xlat16_2 = f_u_xlat16_1 + f_u_xlat16_1;
    f_u_xlat16_3 = (-_BlendColor.xyz) + 1.0f.xxx;
    f_u_xlat16_2 = ((-f_u_xlat16_2) * f_u_xlat16_3) + 1.0f.xxx;
    f_u_xlat16_1 = ((-f_u_xlat16_1) * f_u_xlat16_3) + 1.0f.xxx;
    f_u_xlat3 = f_u_xlat0 * f_vs_COLOR0;
    f_u_xlat16_4 = f_u_xlat3.xyz + f_u_xlat3.xyz;
    f_u_xlat16_4 *= _BlendColor.xyz;
    f_u_xlatb5 = bool4(f_u_xlat3.xyzx.x < float4(0.5f, 0.5f, 0.5f, 0.0f).x, f_u_xlat3.xyzx.y < float4(0.5f, 0.5f, 0.5f, 0.0f).y, f_u_xlat3.xyzx.z < float4(0.5f, 0.5f, 0.5f, 0.0f).z, f_u_xlat3.xyzx.w < float4(0.5f, 0.5f, 0.5f, 0.0f).w).xyz;
    float3 hlslcc_movcTemp = f_u_xlat16_2;
    float _95;
    if (f_u_xlatb5.x)
    {
        _95 = f_u_xlat16_4.x;
    }
    else
    {
        _95 = f_u_xlat16_2.x;
    }
    hlslcc_movcTemp.x = _95;
    float _109;
    if (f_u_xlatb5.y)
    {
        _109 = f_u_xlat16_4.y;
    }
    else
    {
        _109 = f_u_xlat16_2.y;
    }
    hlslcc_movcTemp.y = _109;
    float _122;
    if (f_u_xlatb5.z)
    {
        _122 = f_u_xlat16_4.z;
    }
    else
    {
        _122 = f_u_xlat16_2.z;
    }
    hlslcc_movcTemp.z = _122;
    f_u_xlat16_2 = hlslcc_movcTemp;
    f_u_xlat16_4 = (f_u_xlat0.xyz * f_vs_COLOR0.xyz) + _BlendColor.xyz;
    f_u_xlat16_4 = clamp(f_u_xlat16_4, 0.0f.xxx, 1.0f.xxx);
    float4 _148 = _BlendMode.xxxx;
    bool2 _154 = bool4(_148.x < float4(1.5f, 2.5f, 0.0f, 0.0f).x, _148.y < float4(1.5f, 2.5f, 0.0f, 0.0f).y, _148.z < float4(1.5f, 2.5f, 0.0f, 0.0f).z, _148.w < float4(1.5f, 2.5f, 0.0f, 0.0f).w).xy;
    f_u_xlatb5.x = _154.x;
    f_u_xlatb5.y = _154.y;
    float3 _161;
    if (f_u_xlatb5.y)
    {
        _161 = f_u_xlat16_4;
    }
    else
    {
        _161 = f_u_xlat16_1;
    }
    f_u_xlat16_1 = _161;
    float3 _170;
    if (f_u_xlatb5.x)
    {
        _170 = f_u_xlat16_2;
    }
    else
    {
        _170 = f_u_xlat16_1;
    }
    f_u_xlat16_1 = _170;
    f_u_xlatb18 = _BlendMode >= 0.5f;
    float3 _181;
    if (f_u_xlatb18)
    {
        _181 = f_u_xlat16_1;
    }
    else
    {
        _181 = _BlendColor.xyz;
    }
    f_u_xlat16_1 = _181;
    f_u_xlat16_1 = ((-f_u_xlat0.xyz) * f_vs_COLOR0.xyz) + f_u_xlat16_1;
    f_u_xlat16_19 = _BlendColor.w * _BlendIntensity;
    f_u_xlat16_1 = (f_u_xlat16_19.xxx * f_u_xlat16_1) + f_u_xlat3.xyz;
    bool2 _220 = bool4(f_vs_TEXCOORD1.xyxx.x >= _ClipRect.xyxx.x, f_vs_TEXCOORD1.xyxx.y >= _ClipRect.xyxx.y, f_vs_TEXCOORD1.xyxx.z >= _ClipRect.xyxx.z, f_vs_TEXCOORD1.xyxx.w >= _ClipRect.xyxx.w).xy;
    f_u_xlatb0.x = _220.x;
    f_u_xlatb0.y = _220.y;
    bool2 _230 = bool4(_ClipRect.zzzw.x >= f_vs_TEXCOORD1.xxxy.x, _ClipRect.zzzw.y >= f_vs_TEXCOORD1.xxxy.y, _ClipRect.zzzw.z >= f_vs_TEXCOORD1.xxxy.z, _ClipRect.zzzw.w >= f_vs_TEXCOORD1.xxxy.w).zw;
    f_u_xlatb0.z = _230.x;
    f_u_xlatb0.w = _230.y;
    f_u_xlat0.x = float(f_u_xlatb0.x);
    f_u_xlat0.y = float(f_u_xlatb0.y);
    f_u_xlat0.z = float(f_u_xlatb0.z);
    f_u_xlat0.w = float(f_u_xlatb0.w);
    float4 _251 = f_u_xlat0;
    float4 _253 = f_u_xlat0;
    float2 _255 = _251.zw * _253.xy;
    f_u_xlat0.x = _255.x;
    f_u_xlat0.y = _255.y;
    f_u_xlat0.x = f_u_xlat0.y * f_u_xlat0.x;
    f_u_xlat0.x *= f_u_xlat3.w;
    float3 _277 = f_u_xlat0.xxx * f_u_xlat16_1;
    f_SV_Target0.x = _277.x;
    f_SV_Target0.y = _277.y;
    f_SV_Target0.z = _277.z;
    f_SV_Target0.w = f_u_xlat0.x;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _Color;
float4 _MainTex_ST;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _TextureSampleAdd;
float4 _BlendColor;
float _BlendMode;
float _BlendIntensity;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
    float2 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float2 v_vs_TEXCOORD0 : TEXCOORD1;
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
    v_u_xlat0 = v_in_COLOR0 * _Color;
    v_vs_COLOR0 = v_u_xlat0;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_vs_TEXCOORD1 = v_in_POSITION0;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    return stage_output;
}
static float2 f_vs_TEXCOORD0;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float2 f_vs_TEXCOORD0 : TEXCOORD1;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float4 f_u_xlat0;
static float3 f_u_xlat16_1;
static float3 f_u_xlat16_2;
static float3 f_u_xlat16_3;
static float4 f_u_xlat3;
static float3 f_u_xlat16_4;
static bool3 f_u_xlatb5;
static bool f_u_xlatb18;
static float f_u_xlat16_19;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0);
    f_u_xlat0 = f_u_xlat16_0 + _TextureSampleAdd;
    f_u_xlat16_1 = ((-f_u_xlat0.xyz) * f_vs_COLOR0.xyz) + 1.0f.xxx;
    f_u_xlat16_2 = f_u_xlat16_1 + f_u_xlat16_1;
    f_u_xlat16_3 = (-_BlendColor.xyz) + 1.0f.xxx;
    f_u_xlat16_2 = ((-f_u_xlat16_2) * f_u_xlat16_3) + 1.0f.xxx;
    f_u_xlat16_1 = ((-f_u_xlat16_1) * f_u_xlat16_3) + 1.0f.xxx;
    f_u_xlat3 = f_u_xlat0 * f_vs_COLOR0;
    f_u_xlat16_4 = f_u_xlat3.xyz + f_u_xlat3.xyz;
    f_u_xlat16_4 *= _BlendColor.xyz;
    f_u_xlatb5 = bool4(f_u_xlat3.xyzx.x < float4(0.5f, 0.5f, 0.5f, 0.0f).x, f_u_xlat3.xyzx.y < float4(0.5f, 0.5f, 0.5f, 0.0f).y, f_u_xlat3.xyzx.z < float4(0.5f, 0.5f, 0.5f, 0.0f).z, f_u_xlat3.xyzx.w < float4(0.5f, 0.5f, 0.5f, 0.0f).w).xyz;
    float3 hlslcc_movcTemp = f_u_xlat16_2;
    float _95;
    if (f_u_xlatb5.x)
    {
        _95 = f_u_xlat16_4.x;
    }
    else
    {
        _95 = f_u_xlat16_2.x;
    }
    hlslcc_movcTemp.x = _95;
    float _109;
    if (f_u_xlatb5.y)
    {
        _109 = f_u_xlat16_4.y;
    }
    else
    {
        _109 = f_u_xlat16_2.y;
    }
    hlslcc_movcTemp.y = _109;
    float _122;
    if (f_u_xlatb5.z)
    {
        _122 = f_u_xlat16_4.z;
    }
    else
    {
        _122 = f_u_xlat16_2.z;
    }
    hlslcc_movcTemp.z = _122;
    f_u_xlat16_2 = hlslcc_movcTemp;
    f_u_xlat16_4 = (f_u_xlat0.xyz * f_vs_COLOR0.xyz) + _BlendColor.xyz;
    f_u_xlat16_4 = clamp(f_u_xlat16_4, 0.0f.xxx, 1.0f.xxx);
    float4 _148 = _BlendMode.xxxx;
    bool2 _154 = bool4(_148.x < float4(1.5f, 2.5f, 0.0f, 0.0f).x, _148.y < float4(1.5f, 2.5f, 0.0f, 0.0f).y, _148.z < float4(1.5f, 2.5f, 0.0f, 0.0f).z, _148.w < float4(1.5f, 2.5f, 0.0f, 0.0f).w).xy;
    f_u_xlatb5.x = _154.x;
    f_u_xlatb5.y = _154.y;
    float3 _161;
    if (f_u_xlatb5.y)
    {
        _161 = f_u_xlat16_4;
    }
    else
    {
        _161 = f_u_xlat16_1;
    }
    f_u_xlat16_1 = _161;
    float3 _170;
    if (f_u_xlatb5.x)
    {
        _170 = f_u_xlat16_2;
    }
    else
    {
        _170 = f_u_xlat16_1;
    }
    f_u_xlat16_1 = _170;
    f_u_xlatb18 = _BlendMode >= 0.5f;
    float3 _181;
    if (f_u_xlatb18)
    {
        _181 = f_u_xlat16_1;
    }
    else
    {
        _181 = _BlendColor.xyz;
    }
    f_u_xlat16_1 = _181;
    f_u_xlat16_1 = ((-f_u_xlat0.xyz) * f_vs_COLOR0.xyz) + f_u_xlat16_1;
    f_u_xlat16_19 = _BlendColor.w * _BlendIntensity;
    f_u_xlat16_1 = (f_u_xlat16_19.xxx * f_u_xlat16_1) + f_u_xlat3.xyz;
    float3 _216 = f_u_xlat3.www * f_u_xlat16_1;
    f_SV_Target0.x = _216.x;
    f_SV_Target0.y = _216.y;
    f_SV_Target0.z = _216.z;
    f_SV_Target0.w = f_u_xlat3.w;
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
}
}
