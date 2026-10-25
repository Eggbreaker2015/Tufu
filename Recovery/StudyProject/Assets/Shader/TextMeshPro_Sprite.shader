Shader "TextMeshPro/Sprite" {
Properties {
		_MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Vector) = (1,1,1,1)
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_CullMode ("Cull Mode", Float) = 0
		_ColorMask ("Color Mask", Float) = 15
		_ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}
// Recovered from the APK GLES programs; generated HLSL, not original author source.
SubShader {
Tags { "CanUseSpriteAtlas"="true" "IgnoreProjector"="true" "PreviewType"="Plane" "Queue"="Transparent" "RenderType"="Transparent" }
Pass {
Name "Default"
Tags { "CanUseSpriteAtlas"="true" "IgnoreProjector"="true" "PreviewType"="Plane" "Queue"="Transparent" "RenderType"="Transparent" }
Cull [_CullMode]
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
#pragma multi_compile _ UNITY_UI_CLIP_RECT
#if defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
int _UIVertexColorAlwaysGammaSpace;
float4 _Color;
float4 _MainTex_ST;
float4 _ScreenParams;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float4 _ClipRect;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _TextureSampleAdd;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD2;

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
    float4 v_vs_TEXCOORD2 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float3 v_u_xlat16_2;
static float3 v_u_xlat16_3;
static bool3 v_u_xlatb0;
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
    v_u_xlat0 = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_gl_Position = v_u_xlat0;
    v_u_xlat16_2 = (v_in_COLOR0.xyz * 0.26588499546051025390625f.xxx) + 0.736584007740020751953125f.xxx;
    v_u_xlat16_2 = (v_in_COLOR0.xyz * v_u_xlat16_2) + (-0.00980184040963649749755859375f).xxx;
    v_u_xlat16_2 = (v_in_COLOR0.xyz * v_u_xlat16_2) + 0.0031969700939953327178955078125f.xxx;
    v_u_xlat16_3 = (v_in_COLOR0.xyz * 0.084971003234386444091796875f.xxx) + (-0.0001630290062166750431060791015625f).xxx;
    v_u_xlatb0 = bool4(v_in_COLOR0.xyzx.x < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).x, v_in_COLOR0.xyzx.y < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).y, v_in_COLOR0.xyzx.z < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).z, v_in_COLOR0.xyzx.w < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).w).xyz;
    float3 hlslcc_movcTemp = v_u_xlat16_2;
    float _135;
    if (v_u_xlatb0.x)
    {
        _135 = v_u_xlat16_3.x;
    }
    else
    {
        _135 = v_u_xlat16_2.x;
    }
    hlslcc_movcTemp.x = _135;
    float _149;
    if (v_u_xlatb0.y)
    {
        _149 = v_u_xlat16_3.y;
    }
    else
    {
        _149 = v_u_xlat16_2.y;
    }
    hlslcc_movcTemp.y = _149;
    float _162;
    if (v_u_xlatb0.z)
    {
        _162 = v_u_xlat16_3.z;
    }
    else
    {
        _162 = v_u_xlat16_2.z;
    }
    hlslcc_movcTemp.z = _162;
    v_u_xlat16_2 = hlslcc_movcTemp;
    float3 _177;
    if (_UIVertexColorAlwaysGammaSpace != 0)
    {
        _177 = v_u_xlat16_2;
    }
    else
    {
        _177 = v_in_COLOR0.xyz;
    }
    v_u_xlat1.x = _177.x;
    v_u_xlat1.y = _177.y;
    v_u_xlat1.z = _177.z;
    v_u_xlat1.w = v_in_COLOR0.w;
    v_u_xlat1 *= _Color;
    v_vs_COLOR0 = v_u_xlat1;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_vs_TEXCOORD1 = v_in_POSITION0;
    float2 _224 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat0.x = _224.x;
    v_u_xlat0.y = _224.y;
    float4 _235 = v_u_xlat0;
    float2 _237 = (transpose(glstate_matrix_projection)[0].xy * _ScreenParams.xx) + _235.xy;
    v_u_xlat0.x = _237.x;
    v_u_xlat0.y = _237.y;
    float4 _242 = v_u_xlat0;
    float4 _244 = v_u_xlat0;
    float2 _247 = _242.ww / abs(_244.xy);
    v_u_xlat0.x = _247.x;
    v_u_xlat0.y = _247.y;
    float4 _261 = v_u_xlat0;
    float2 _264 = (float2(_UIMaskSoftnessX, _UIMaskSoftnessY) * 0.25f.xx) + abs(_261.xy);
    v_u_xlat0.x = _264.x;
    v_u_xlat0.y = _264.y;
    float2 _272 = 0.25f.xx / v_u_xlat0.xy;
    v_vs_TEXCOORD2.z = _272.x;
    v_vs_TEXCOORD2.w = _272.y;
    v_u_xlat0 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat0 = min(v_u_xlat0, 20000000000.0f.xxxx);
    float4 _292 = v_u_xlat0;
    float2 _295 = (v_in_POSITION0.xy * 2.0f.xx) + (-_292.xy);
    v_u_xlat0.x = _295.x;
    v_u_xlat0.y = _295.y;
    float2 _305 = (-v_u_xlat0.zw) + v_u_xlat0.xy;
    v_vs_TEXCOORD2.x = _305.x;
    v_vs_TEXCOORD2.y = _305.y;
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
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float4 f_vs_TEXCOORD2;
static float2 f_vs_TEXCOORD0;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float2 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat0;
static float f_u_xlat16_1;
static float4 f_u_xlat16_0;
static float f_u_xlat16_4;
static bool f_u_xlatb2;

void f_frag_main()
{
    float2 _18 = (-_ClipRect.xy) + _ClipRect.zw;
    f_u_xlat0.x = _18.x;
    f_u_xlat0.y = _18.y;
    float4 _27 = f_u_xlat0;
    float2 _35 = _27.xy + (-abs(f_vs_TEXCOORD2.xy));
    f_u_xlat0.x = _35.x;
    f_u_xlat0.y = _35.y;
    float4 _40 = f_u_xlat0;
    float2 _44 = _40.xy * f_vs_TEXCOORD2.zw;
    f_u_xlat0.x = _44.x;
    f_u_xlat0.y = _44.y;
    float4 _49 = f_u_xlat0;
    float2 _55 = clamp(_49.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _55.x;
    f_u_xlat0.y = _55.y;
    f_u_xlat16_1 = f_u_xlat0.y * f_u_xlat0.x;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0);
    f_u_xlat0 = f_u_xlat16_0 + _TextureSampleAdd;
    f_u_xlat0 *= f_vs_COLOR0;
    f_u_xlat16_4 = (f_u_xlat0.w * f_u_xlat16_1) + (-0.001000000047497451305389404296875f);
    f_u_xlat16_0 = f_u_xlat16_1.xxxx * f_u_xlat0;
    f_SV_Target0 = f_u_xlat16_0;
    f_u_xlatb2 = f_u_xlat16_4 < 0.0f;
    if (f_u_xlatb2)
    {
        discard;
    }
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
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
float4x4 glstate_matrix_projection;
int _UIVertexColorAlwaysGammaSpace;
float4 _Color;
float4 _MainTex_ST;
float4 _ScreenParams;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float4 _ClipRect;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _TextureSampleAdd;
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
static float3 v_u_xlat16_2;
static float3 v_u_xlat16_3;
static bool3 v_u_xlatb0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat0 += transpose(unity_ObjectToWorld)[3];
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_u_xlat0 = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_gl_Position = v_u_xlat0;
    v_u_xlat16_2 = (v_in_COLOR0.xyz * 0.26588499546051025390625f.xxx) + 0.736584007740020751953125f.xxx;
    v_u_xlat16_2 = (v_in_COLOR0.xyz * v_u_xlat16_2) + (-0.00980184040963649749755859375f).xxx;
    v_u_xlat16_2 = (v_in_COLOR0.xyz * v_u_xlat16_2) + 0.0031969700939953327178955078125f.xxx;
    v_u_xlat16_3 = (v_in_COLOR0.xyz * 0.084971003234386444091796875f.xxx) + (-0.0001630290062166750431060791015625f).xxx;
    v_u_xlatb0 = bool4(v_in_COLOR0.xyzx.x < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).x, v_in_COLOR0.xyzx.y < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).y, v_in_COLOR0.xyzx.z < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).z, v_in_COLOR0.xyzx.w < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).w).xyz;
    float3 hlslcc_movcTemp = v_u_xlat16_2;
    float _135;
    if (v_u_xlatb0.x)
    {
        _135 = v_u_xlat16_3.x;
    }
    else
    {
        _135 = v_u_xlat16_2.x;
    }
    hlslcc_movcTemp.x = _135;
    float _149;
    if (v_u_xlatb0.y)
    {
        _149 = v_u_xlat16_3.y;
    }
    else
    {
        _149 = v_u_xlat16_2.y;
    }
    hlslcc_movcTemp.y = _149;
    float _162;
    if (v_u_xlatb0.z)
    {
        _162 = v_u_xlat16_3.z;
    }
    else
    {
        _162 = v_u_xlat16_2.z;
    }
    hlslcc_movcTemp.z = _162;
    v_u_xlat16_2 = hlslcc_movcTemp;
    float3 _177;
    if (_UIVertexColorAlwaysGammaSpace != 0)
    {
        _177 = v_u_xlat16_2;
    }
    else
    {
        _177 = v_in_COLOR0.xyz;
    }
    v_u_xlat1.x = _177.x;
    v_u_xlat1.y = _177.y;
    v_u_xlat1.z = _177.z;
    v_u_xlat1.w = v_in_COLOR0.w;
    v_u_xlat1 *= _Color;
    v_vs_COLOR0 = v_u_xlat1;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_vs_TEXCOORD1 = v_in_POSITION0;
    float2 _224 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat0.x = _224.x;
    v_u_xlat0.y = _224.y;
    float4 _235 = v_u_xlat0;
    float2 _237 = (transpose(glstate_matrix_projection)[0].xy * _ScreenParams.xx) + _235.xy;
    v_u_xlat0.x = _237.x;
    v_u_xlat0.y = _237.y;
    float4 _242 = v_u_xlat0;
    float4 _244 = v_u_xlat0;
    float2 _247 = _242.ww / abs(_244.xy);
    v_u_xlat0.x = _247.x;
    v_u_xlat0.y = _247.y;
    float4 _261 = v_u_xlat0;
    float2 _264 = (float2(_UIMaskSoftnessX, _UIMaskSoftnessY) * 0.25f.xx) + abs(_261.xy);
    v_u_xlat0.x = _264.x;
    v_u_xlat0.y = _264.y;
    float2 _272 = 0.25f.xx / v_u_xlat0.xy;
    v_vs_TEXCOORD2.z = _272.x;
    v_vs_TEXCOORD2.w = _272.y;
    v_u_xlat0 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat0 = min(v_u_xlat0, 20000000000.0f.xxxx);
    float4 _291 = v_u_xlat0;
    float2 _294 = (v_in_POSITION0.xy * 2.0f.xx) + (-_291.xy);
    v_u_xlat0.x = _294.x;
    v_u_xlat0.y = _294.y;
    float2 _304 = (-v_u_xlat0.zw) + v_u_xlat0.xy;
    v_vs_TEXCOORD2.x = _304.x;
    v_vs_TEXCOORD2.y = _304.y;
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
static bool f_u_xlatb0;

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0);
    f_u_xlat0 = f_u_xlat16_0 + _TextureSampleAdd;
    f_u_xlat16_1 = (f_u_xlat0.w * f_vs_COLOR0.w) + (-0.001000000047497451305389404296875f);
    f_u_xlat0 *= f_vs_COLOR0;
    f_SV_Target0 = f_u_xlat0;
    f_u_xlatb0 = f_u_xlat16_1 < 0.0f;
    if (f_u_xlatb0)
    {
        discard;
    }
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
float4x4 glstate_matrix_projection;
int _UIVertexColorAlwaysGammaSpace;
float4 _Color;
float4 _MainTex_ST;
float4 _ScreenParams;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float4 _ClipRect;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _TextureSampleAdd;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float2 v_vs_TEXCOORD0;
static float2 v_in_TEXCOORD0;
static float4 v_vs_TEXCOORD2;

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
    float4 v_vs_TEXCOORD2 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float3 v_u_xlat16_2;
static float3 v_u_xlat16_3;
static bool3 v_u_xlatb0;
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
    v_u_xlat0 = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_gl_Position = v_u_xlat0;
    v_u_xlat16_2 = (v_in_COLOR0.xyz * 0.26588499546051025390625f.xxx) + 0.736584007740020751953125f.xxx;
    v_u_xlat16_2 = (v_in_COLOR0.xyz * v_u_xlat16_2) + (-0.00980184040963649749755859375f).xxx;
    v_u_xlat16_2 = (v_in_COLOR0.xyz * v_u_xlat16_2) + 0.0031969700939953327178955078125f.xxx;
    v_u_xlat16_3 = (v_in_COLOR0.xyz * 0.084971003234386444091796875f.xxx) + (-0.0001630290062166750431060791015625f).xxx;
    v_u_xlatb0 = bool4(v_in_COLOR0.xyzx.x < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).x, v_in_COLOR0.xyzx.y < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).y, v_in_COLOR0.xyzx.z < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).z, v_in_COLOR0.xyzx.w < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).w).xyz;
    float3 hlslcc_movcTemp = v_u_xlat16_2;
    float _135;
    if (v_u_xlatb0.x)
    {
        _135 = v_u_xlat16_3.x;
    }
    else
    {
        _135 = v_u_xlat16_2.x;
    }
    hlslcc_movcTemp.x = _135;
    float _149;
    if (v_u_xlatb0.y)
    {
        _149 = v_u_xlat16_3.y;
    }
    else
    {
        _149 = v_u_xlat16_2.y;
    }
    hlslcc_movcTemp.y = _149;
    float _162;
    if (v_u_xlatb0.z)
    {
        _162 = v_u_xlat16_3.z;
    }
    else
    {
        _162 = v_u_xlat16_2.z;
    }
    hlslcc_movcTemp.z = _162;
    v_u_xlat16_2 = hlslcc_movcTemp;
    float3 _177;
    if (_UIVertexColorAlwaysGammaSpace != 0)
    {
        _177 = v_u_xlat16_2;
    }
    else
    {
        _177 = v_in_COLOR0.xyz;
    }
    v_u_xlat1.x = _177.x;
    v_u_xlat1.y = _177.y;
    v_u_xlat1.z = _177.z;
    v_u_xlat1.w = v_in_COLOR0.w;
    v_u_xlat1 *= _Color;
    v_vs_COLOR0 = v_u_xlat1;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_vs_TEXCOORD1 = v_in_POSITION0;
    float2 _224 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat0.x = _224.x;
    v_u_xlat0.y = _224.y;
    float4 _235 = v_u_xlat0;
    float2 _237 = (transpose(glstate_matrix_projection)[0].xy * _ScreenParams.xx) + _235.xy;
    v_u_xlat0.x = _237.x;
    v_u_xlat0.y = _237.y;
    float4 _242 = v_u_xlat0;
    float4 _244 = v_u_xlat0;
    float2 _247 = _242.ww / abs(_244.xy);
    v_u_xlat0.x = _247.x;
    v_u_xlat0.y = _247.y;
    float4 _261 = v_u_xlat0;
    float2 _264 = (float2(_UIMaskSoftnessX, _UIMaskSoftnessY) * 0.25f.xx) + abs(_261.xy);
    v_u_xlat0.x = _264.x;
    v_u_xlat0.y = _264.y;
    float2 _272 = 0.25f.xx / v_u_xlat0.xy;
    v_vs_TEXCOORD2.z = _272.x;
    v_vs_TEXCOORD2.w = _272.y;
    v_u_xlat0 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat0 = min(v_u_xlat0, 20000000000.0f.xxxx);
    float4 _292 = v_u_xlat0;
    float2 _295 = (v_in_POSITION0.xy * 2.0f.xx) + (-_292.xy);
    v_u_xlat0.x = _295.x;
    v_u_xlat0.y = _295.y;
    float2 _305 = (-v_u_xlat0.zw) + v_u_xlat0.xy;
    v_vs_TEXCOORD2.x = _305.x;
    v_vs_TEXCOORD2.y = _305.y;
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
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float4 f_vs_TEXCOORD2;
static float2 f_vs_TEXCOORD0;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float2 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD2 : TEXCOORD2;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat0;
static float f_u_xlat16_1;
static float4 f_u_xlat16_0;

void f_frag_main()
{
    float2 _18 = (-_ClipRect.xy) + _ClipRect.zw;
    f_u_xlat0.x = _18.x;
    f_u_xlat0.y = _18.y;
    float4 _27 = f_u_xlat0;
    float2 _35 = _27.xy + (-abs(f_vs_TEXCOORD2.xy));
    f_u_xlat0.x = _35.x;
    f_u_xlat0.y = _35.y;
    float4 _40 = f_u_xlat0;
    float2 _44 = _40.xy * f_vs_TEXCOORD2.zw;
    f_u_xlat0.x = _44.x;
    f_u_xlat0.y = _44.y;
    float4 _49 = f_u_xlat0;
    float2 _55 = clamp(_49.xy, 0.0f.xx, 1.0f.xx);
    f_u_xlat0.x = _55.x;
    f_u_xlat0.y = _55.y;
    f_u_xlat16_1 = f_u_xlat0.y * f_u_xlat0.x;
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0);
    f_u_xlat0 = f_u_xlat16_0 + _TextureSampleAdd;
    f_u_xlat0 *= f_vs_COLOR0;
    f_SV_Target0 = f_u_xlat16_1.xxxx * f_u_xlat0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
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
float4x4 glstate_matrix_projection;
int _UIVertexColorAlwaysGammaSpace;
float4 _Color;
float4 _MainTex_ST;
float4 _ScreenParams;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float4 _ClipRect;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _TextureSampleAdd;
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
static float3 v_u_xlat16_2;
static float3 v_u_xlat16_3;
static bool3 v_u_xlatb0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;

void v_vert_main()
{
    v_u_xlat0 = v_in_POSITION0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[0] * v_in_POSITION0.xxxx) + v_u_xlat0;
    v_u_xlat0 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat0;
    v_u_xlat0 += transpose(unity_ObjectToWorld)[3];
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat1 = (transpose(unity_MatrixVP)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_MatrixVP)[2] * v_u_xlat0.zzzz) + v_u_xlat1;
    v_u_xlat0 = (transpose(unity_MatrixVP)[3] * v_u_xlat0.wwww) + v_u_xlat1;
    v_gl_Position = v_u_xlat0;
    v_u_xlat16_2 = (v_in_COLOR0.xyz * 0.26588499546051025390625f.xxx) + 0.736584007740020751953125f.xxx;
    v_u_xlat16_2 = (v_in_COLOR0.xyz * v_u_xlat16_2) + (-0.00980184040963649749755859375f).xxx;
    v_u_xlat16_2 = (v_in_COLOR0.xyz * v_u_xlat16_2) + 0.0031969700939953327178955078125f.xxx;
    v_u_xlat16_3 = (v_in_COLOR0.xyz * 0.084971003234386444091796875f.xxx) + (-0.0001630290062166750431060791015625f).xxx;
    v_u_xlatb0 = bool4(v_in_COLOR0.xyzx.x < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).x, v_in_COLOR0.xyzx.y < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).y, v_in_COLOR0.xyzx.z < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).z, v_in_COLOR0.xyzx.w < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).w).xyz;
    float3 hlslcc_movcTemp = v_u_xlat16_2;
    float _135;
    if (v_u_xlatb0.x)
    {
        _135 = v_u_xlat16_3.x;
    }
    else
    {
        _135 = v_u_xlat16_2.x;
    }
    hlslcc_movcTemp.x = _135;
    float _149;
    if (v_u_xlatb0.y)
    {
        _149 = v_u_xlat16_3.y;
    }
    else
    {
        _149 = v_u_xlat16_2.y;
    }
    hlslcc_movcTemp.y = _149;
    float _162;
    if (v_u_xlatb0.z)
    {
        _162 = v_u_xlat16_3.z;
    }
    else
    {
        _162 = v_u_xlat16_2.z;
    }
    hlslcc_movcTemp.z = _162;
    v_u_xlat16_2 = hlslcc_movcTemp;
    float3 _177;
    if (_UIVertexColorAlwaysGammaSpace != 0)
    {
        _177 = v_u_xlat16_2;
    }
    else
    {
        _177 = v_in_COLOR0.xyz;
    }
    v_u_xlat1.x = _177.x;
    v_u_xlat1.y = _177.y;
    v_u_xlat1.z = _177.z;
    v_u_xlat1.w = v_in_COLOR0.w;
    v_u_xlat1 *= _Color;
    v_vs_COLOR0 = v_u_xlat1;
    v_vs_TEXCOORD0 = (v_in_TEXCOORD0 * _MainTex_ST.xy) + _MainTex_ST.zw;
    v_vs_TEXCOORD1 = v_in_POSITION0;
    float2 _224 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat0.x = _224.x;
    v_u_xlat0.y = _224.y;
    float4 _235 = v_u_xlat0;
    float2 _237 = (transpose(glstate_matrix_projection)[0].xy * _ScreenParams.xx) + _235.xy;
    v_u_xlat0.x = _237.x;
    v_u_xlat0.y = _237.y;
    float4 _242 = v_u_xlat0;
    float4 _244 = v_u_xlat0;
    float2 _247 = _242.ww / abs(_244.xy);
    v_u_xlat0.x = _247.x;
    v_u_xlat0.y = _247.y;
    float4 _261 = v_u_xlat0;
    float2 _264 = (float2(_UIMaskSoftnessX, _UIMaskSoftnessY) * 0.25f.xx) + abs(_261.xy);
    v_u_xlat0.x = _264.x;
    v_u_xlat0.y = _264.y;
    float2 _272 = 0.25f.xx / v_u_xlat0.xy;
    v_vs_TEXCOORD2.z = _272.x;
    v_vs_TEXCOORD2.w = _272.y;
    v_u_xlat0 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat0 = min(v_u_xlat0, 20000000000.0f.xxxx);
    float4 _291 = v_u_xlat0;
    float2 _294 = (v_in_POSITION0.xy * 2.0f.xx) + (-_291.xy);
    v_u_xlat0.x = _294.x;
    v_u_xlat0.y = _294.y;
    float2 _304 = (-v_u_xlat0.zw) + v_u_xlat0.xy;
    v_vs_TEXCOORD2.x = _304.x;
    v_vs_TEXCOORD2.y = _304.y;
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

void f_frag_main()
{
    f_u_xlat16_0 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0);
    f_u_xlat0 = f_u_xlat16_0 + _TextureSampleAdd;
    f_u_xlat0 *= f_vs_COLOR0;
    f_SV_Target0 = f_u_xlat0;
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
