Shader "Unlit/ProfileAnalyzerShader" {
Properties {
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		_ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
	}
// Recovered from the APK GLES programs; generated HLSL, not original author source.
SubShader {
Tags { "CanUseSpriteAtlas"="true" "IgnoreProjector"="true" "PreviewType"="Plane" "Queue"="Transparent" "RenderType"="Transparent" }
Pass {
Name ""
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
#pragma multi_compile _ UNITY_UI_ALPHACLIP
#if defined(UNITY_UI_ALPHACLIP)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _ClipRect;
int _UseClipRect;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
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
    v_vs_TEXCOORD1 = v_in_POSITION0;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
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

static bool4 f_u_xlatb0;
static float4 f_u_xlat0;
static float f_u_xlat16_1;
static float f_u_xlat16_3;

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
    f_u_xlat0.x *= f_vs_COLOR0.w;
    float _96;
    if (_UseClipRect != 0)
    {
        _96 = f_u_xlat0.x;
    }
    else
    {
        _96 = f_vs_COLOR0.w;
    }
    f_u_xlat16_1 = _96;
    f_u_xlat16_3 = f_u_xlat16_1 + (-0.001000000047497451305389404296875f);
    f_SV_Target0.w = f_u_xlat16_1;
    f_u_xlatb0.x = f_u_xlat16_3 < 0.0f;
    if (f_u_xlatb0.x)
    {
        discard;
    }
    f_SV_Target0.x = f_vs_COLOR0.xyz.x;
    f_SV_Target0.y = f_vs_COLOR0.xyz.y;
    f_SV_Target0.z = f_vs_COLOR0.xyz.z;
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
#elif !defined(UNITY_UI_ALPHACLIP)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4 _ClipRect;
int _UseClipRect;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_vs_COLOR0;
static float4 v_in_COLOR0;
static float4 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float4 v_in_COLOR0 : COLOR0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD1 : TEXCOORD1;
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
    v_vs_TEXCOORD1 = v_in_POSITION0;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
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

static bool4 f_u_xlatb0;
static float4 f_u_xlat0;

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
    f_u_xlat0.x *= f_vs_COLOR0.w;
    float _97;
    if (_UseClipRect != 0)
    {
        _97 = f_u_xlat0.x;
    }
    else
    {
        _97 = f_vs_COLOR0.w;
    }
    f_SV_Target0.w = _97;
    f_SV_Target0.x = f_vs_COLOR0.xyz.x;
    f_SV_Target0.y = f_vs_COLOR0.xyz.y;
    f_SV_Target0.z = f_vs_COLOR0.xyz.z;
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
