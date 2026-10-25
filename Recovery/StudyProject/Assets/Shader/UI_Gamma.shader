Shader "UI/Gamma" {
Properties {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Vector) = (1,1,1,1)
		_GammaIntensity ("Gamma Intensity", Range(0, 1)) = 1
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
Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
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
float _GammaIntensity;
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
static float f_u_xlat16_2;
static bool f_u_xlatb3;
static float f_u_xlat3;

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
    f_u_xlat1 *= f_vs_COLOR0;
    f_u_xlat16_2 = (f_u_xlat1.w * f_u_xlat0.x) + (-0.001000000047497451305389404296875f);
    f_u_xlat0.x *= f_u_xlat1.w;
    f_SV_Target0.x = f_u_xlat1.xyz.x;
    f_SV_Target0.y = f_u_xlat1.xyz.y;
    f_SV_Target0.z = f_u_xlat1.xyz.z;
    f_u_xlatb3 = f_u_xlat16_2 < 0.0f;
    if (f_u_xlatb3)
    {
        discard;
    }
    f_u_xlat3 = (f_u_xlat0.x * 0.305306017398834228515625f) + 0.6821711063385009765625f;
    f_u_xlat3 = (f_u_xlat0.x * f_u_xlat3) + 0.01252287812530994415283203125f;
    f_u_xlat16_2 = (f_u_xlat0.x * f_u_xlat3) + (-f_u_xlat0.x);
    f_SV_Target0.w = (_GammaIntensity * f_u_xlat16_2) + f_u_xlat0.x;
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
float _GammaIntensity;
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
static float f_u_xlat16_1;
static bool f_u_xlatb2;
static float f_u_xlat2;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0);
    f_u_xlat0 = f_u_xlat16_0 + _TextureSampleAdd;
    f_u_xlat16_1 = (f_u_xlat0.w * f_vs_COLOR0.w) + (-0.001000000047497451305389404296875f);
    f_u_xlat0 *= f_vs_COLOR0;
    f_u_xlatb2 = f_u_xlat16_1 < 0.0f;
    if (f_u_xlatb2)
    {
        discard;
    }
    f_u_xlat2 = (f_u_xlat0.w * 0.305306017398834228515625f) + 0.6821711063385009765625f;
    f_u_xlat2 = (f_u_xlat0.w * f_u_xlat2) + 0.01252287812530994415283203125f;
    f_u_xlat16_1 = (f_u_xlat0.w * f_u_xlat2) + (-f_u_xlat0.w);
    f_SV_Target0.w = (_GammaIntensity * f_u_xlat16_1) + f_u_xlat0.w;
    f_SV_Target0.x = f_u_xlat0.xyz.x;
    f_SV_Target0.y = f_u_xlat0.xyz.y;
    f_SV_Target0.z = f_u_xlat0.xyz.z;
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
float4 _ClipRect;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _TextureSampleAdd;
float _GammaIntensity;
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
static float f_u_xlat3;
static float f_u_xlat16_2;

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
    f_u_xlat1 *= f_vs_COLOR0;
    f_u_xlat0.x *= f_u_xlat1.w;
    f_SV_Target0.x = f_u_xlat1.xyz.x;
    f_SV_Target0.y = f_u_xlat1.xyz.y;
    f_SV_Target0.z = f_u_xlat1.xyz.z;
    f_u_xlat3 = (f_u_xlat0.x * 0.305306017398834228515625f) + 0.6821711063385009765625f;
    f_u_xlat3 = (f_u_xlat0.x * f_u_xlat3) + 0.01252287812530994415283203125f;
    f_u_xlat16_2 = (f_u_xlat0.x * f_u_xlat3) + (-f_u_xlat0.x);
    f_SV_Target0.w = (_GammaIntensity * f_u_xlat16_2) + f_u_xlat0.x;
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
#elif !defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _Color;
float4 _MainTex_ST;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _TextureSampleAdd;
float _GammaIntensity;
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
static float f_u_xlat1;
static float f_u_xlat16_2;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0);
    f_u_xlat0 = f_u_xlat16_0 + _TextureSampleAdd;
    f_u_xlat0 *= f_vs_COLOR0;
    f_u_xlat1 = (f_u_xlat0.w * 0.305306017398834228515625f) + 0.6821711063385009765625f;
    f_u_xlat1 = (f_u_xlat0.w * f_u_xlat1) + 0.01252287812530994415283203125f;
    f_u_xlat16_2 = (f_u_xlat0.w * f_u_xlat1) + (-f_u_xlat0.w);
    f_SV_Target0.w = (_GammaIntensity * f_u_xlat16_2) + f_u_xlat0.w;
    f_SV_Target0.x = f_u_xlat0.xyz.x;
    f_SV_Target0.y = f_u_xlat0.xyz.y;
    f_SV_Target0.z = f_u_xlat0.xyz.z;
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
