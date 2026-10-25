Shader "TextMeshPro/Distance Field" {
Properties {
		_FaceTex ("Face Texture", 2D) = "white" {}
		_FaceUVSpeedX ("Face UV Speed X", Range(-5, 5)) = 0
		_FaceUVSpeedY ("Face UV Speed Y", Range(-5, 5)) = 0
		_FaceColor ("Face Color", Vector) = (1,1,1,1)
		_FaceDilate ("Face Dilate", Range(-1, 1)) = 0
		_OutlineColor ("Outline Color", Vector) = (0,0,0,1)
		_OutlineTex ("Outline Texture", 2D) = "white" {}
		_OutlineUVSpeedX ("Outline UV Speed X", Range(-5, 5)) = 0
		_OutlineUVSpeedY ("Outline UV Speed Y", Range(-5, 5)) = 0
		_OutlineWidth ("Outline Thickness", Range(0, 1)) = 0
		_OutlineSoftness ("Outline Softness", Range(0, 1)) = 0
		_Bevel ("Bevel", Range(0, 1)) = 0.5
		_BevelOffset ("Bevel Offset", Range(-0.5, 0.5)) = 0
		_BevelWidth ("Bevel Width", Range(-0.5, 0.5)) = 0
		_BevelClamp ("Bevel Clamp", Range(0, 1)) = 0
		_BevelRoundness ("Bevel Roundness", Range(0, 1)) = 0
		_LightAngle ("Light Angle", Range(0, 6.2831855)) = 3.1416
		_SpecularColor ("Specular", Vector) = (1,1,1,1)
		_SpecularPower ("Specular", Range(0, 4)) = 2
		_Reflectivity ("Reflectivity", Range(5, 15)) = 10
		_Diffuse ("Diffuse", Range(0, 1)) = 0.5
		_Ambient ("Ambient", Range(1, 0)) = 0.5
		_BumpMap ("Normal map", 2D) = "bump" {}
		_BumpOutline ("Bump Outline", Range(0, 1)) = 0
		_BumpFace ("Bump Face", Range(0, 1)) = 0
		_ReflectFaceColor ("Reflection Color", Vector) = (0,0,0,1)
		_ReflectOutlineColor ("Reflection Color", Vector) = (0,0,0,1)
		_Cube ("Reflection Cubemap", Cube) = "black" {}
		_EnvMatrixRotation ("Texture Rotation", Vector) = (0,0,0,0)
		_UnderlayColor ("Border Color", Vector) = (0,0,0,0.5)
		_UnderlayOffsetX ("Border OffsetX", Range(-1, 1)) = 0
		_UnderlayOffsetY ("Border OffsetY", Range(-1, 1)) = 0
		_UnderlayDilate ("Border Dilate", Range(-1, 1)) = 0
		_UnderlaySoftness ("Border Softness", Range(0, 1)) = 0
		_GlowColor ("Color", Vector) = (0,1,0,0.5)
		_GlowOffset ("Offset", Range(-1, 1)) = 0
		_GlowInner ("Inner", Range(0, 1)) = 0.05
		_GlowOuter ("Outer", Range(0, 1)) = 0.05
		_GlowPower ("Falloff", Range(1, 0)) = 0.75
		_WeightNormal ("Weight Normal", Float) = 0
		_WeightBold ("Weight Bold", Float) = 0.5
		_ShaderFlags ("Flags", Float) = 0
		_ScaleRatioA ("Scale RatioA", Float) = 1
		_ScaleRatioB ("Scale RatioB", Float) = 1
		_ScaleRatioC ("Scale RatioC", Float) = 1
		_MainTex ("Font Atlas", 2D) = "white" {}
		_TextureWidth ("Texture Width", Float) = 512
		_TextureHeight ("Texture Height", Float) = 512
		_GradientScale ("Gradient Scale", Float) = 5
		_ScaleX ("Scale X", Float) = 1
		_ScaleY ("Scale Y", Float) = 1
		_PerspectiveFilter ("Perspective Correction", Range(0, 1)) = 0.875
		_Sharpness ("Sharpness", Range(-1, 1)) = 0
		_VertexOffsetX ("Vertex OffsetX", Float) = 0
		_VertexOffsetY ("Vertex OffsetY", Float) = 0
		_MaskCoord ("Mask Coordinates", Vector) = (0,0,32767,32767)
		_ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
		_MaskSoftnessX ("Mask SoftnessX", Float) = 0
		_MaskSoftnessY ("Mask SoftnessY", Float) = 0
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_CullMode ("Cull Mode", Float) = 0
		_ColorMask ("Color Mask", Float) = 15
	}
// Recovered from the APK GLES programs; generated HLSL, not original author source.
SubShader {
Tags { "IgnoreProjector"="true" "Queue"="Transparent" "RenderType"="Transparent" }
Pass {
Name ""
Tags { "IgnoreProjector"="true" "Queue"="Transparent" "RenderType"="Transparent" }
Cull [_CullMode]
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
#pragma multi_compile _ UNDERLAY_ON
#pragma multi_compile _ UNITY_UI_ALPHACLIP
#pragma multi_compile _ UNITY_UI_CLIP_RECT
#if defined(UNDERLAY_ON) && defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float4x4 _EnvMatrix;
float _VertexOffsetX;
float _VertexOffsetY;
float3 _WorldSpaceCameraPos;
int _UIVertexColorAlwaysGammaSpace;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineWidth;
float _OutlineSoftness;
float4 _ClipRect;
float _MaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float _UnderlaySoftness;
float _UnderlayDilate;
float _UnderlayOffsetX;
float _UnderlayOffsetY;
float _ScaleRatioC;
float _TextureWidth;
float _TextureHeight;
float4 _UnderlayColor;
float4 _FaceTex_ST;
float4 _OutlineTex_ST;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _OutlineUVSpeedX;
float _OutlineUVSpeedY;
float4 _Time;
Texture2D<float4> _OutlineTex;
SamplerState sampler_OutlineTex;
float4 _OutlineColor;
float4 _FaceColor;
float _FaceUVSpeedX;
float _FaceUVSpeedY;
Texture2D<float4> _FaceTex;
SamplerState sampler_FaceTex;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD5;
static float2 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float2 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD2 : TEXCOORD3;
    float4 v_vs_TEXCOORD4 : TEXCOORD4;
    float4 v_vs_COLOR1 : TEXCOORD5;
    float4 v_vs_TEXCOORD5 : TEXCOORD6;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float4 v_u_xlat3;
static float3 v_u_xlat16_4;
static float3 v_u_xlat16_5;
static bool3 v_u_xlatb2;
static float2 v_u_xlat14;
static float v_u_xlat22;
static float3 v_u_xlat9;
static float v_u_xlat16;
static float3 v_u_xlat6;
static bool v_u_xlatb22;
static float3 v_vs_TEXCOORD3;
static float v_u_xlat7;

void v_vert_main()
{
    float2 _21 = v_in_POSITION0.xy + float2(_VertexOffsetX, _VertexOffsetY);
    v_u_xlat0.x = _21.x;
    v_u_xlat0.y = _21.y;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + transpose(unity_ObjectToWorld)[3];
    float4 _72 = v_u_xlat1;
    float3 _74 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + _72.xyz;
    v_u_xlat1.x = _74.x;
    v_u_xlat1.y = _74.y;
    v_u_xlat1.z = _74.z;
    float4 _82 = v_u_xlat1;
    float3 _88 = (-_82.xyz) + _WorldSpaceCameraPos;
    v_u_xlat1.x = _88.x;
    v_u_xlat1.y = _88.y;
    v_u_xlat1.z = _88.z;
    v_u_xlat3 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat3 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat3;
    v_u_xlat3 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat3;
    v_u_xlat2 = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat3;
    v_gl_Position = v_u_xlat2;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * 0.26588499546051025390625f.xxx) + 0.736584007740020751953125f.xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + (-0.00980184040963649749755859375f).xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + 0.0031969700939953327178955078125f.xxx;
    v_u_xlat16_5 = (v_in_COLOR0.xyz * 0.084971003234386444091796875f.xxx) + (-0.0001630290062166750431060791015625f).xxx;
    v_u_xlatb2 = bool4(v_in_COLOR0.xyzx.x < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).x, v_in_COLOR0.xyzx.y < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).y, v_in_COLOR0.xyzx.z < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).z, v_in_COLOR0.xyzx.w < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).w).xyz;
    float3 hlslcc_movcTemp = v_u_xlat16_4;
    float _182;
    if (v_u_xlatb2.x)
    {
        _182 = v_u_xlat16_5.x;
    }
    else
    {
        _182 = v_u_xlat16_4.x;
    }
    hlslcc_movcTemp.x = _182;
    float _194;
    if (v_u_xlatb2.y)
    {
        _194 = v_u_xlat16_5.y;
    }
    else
    {
        _194 = v_u_xlat16_4.y;
    }
    hlslcc_movcTemp.y = _194;
    float _206;
    if (v_u_xlatb2.z)
    {
        _206 = v_u_xlat16_5.z;
    }
    else
    {
        _206 = v_u_xlat16_4.z;
    }
    hlslcc_movcTemp.z = _206;
    v_u_xlat16_4 = hlslcc_movcTemp;
    float3 _222;
    if (_UIVertexColorAlwaysGammaSpace != 0)
    {
        _222 = v_u_xlat16_4;
    }
    else
    {
        _222 = v_in_COLOR0.xyz;
    }
    v_vs_COLOR0.x = _222.x;
    v_vs_COLOR0.y = _222.y;
    v_vs_COLOR0.z = _222.z;
    v_vs_COLOR0.w = v_in_COLOR0.w;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xy;
    v_u_xlat14 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat14 = (transpose(glstate_matrix_projection)[0].xy * _ScreenParams.xx) + v_u_xlat14;
    v_u_xlat14 = abs(v_u_xlat14) * float2(_ScaleX, _ScaleY);
    v_u_xlat14 = v_u_xlat2.ww / v_u_xlat14;
    v_u_xlat22 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat22 = rsqrt(v_u_xlat22);
    v_u_xlat2.x = abs(v_in_TEXCOORD0.w) * _GradientScale;
    v_u_xlat9.x = _Sharpness + 1.0f;
    v_u_xlat2.x = v_u_xlat9.x * v_u_xlat2.x;
    v_u_xlat9.x = v_u_xlat22 * v_u_xlat2.x;
    v_u_xlat16 = (-_PerspectiveFilter) + 1.0f;
    v_u_xlat16 *= abs(v_u_xlat9.x);
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + (-v_u_xlat16);
    v_u_xlat3.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat3.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat3.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat3.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    float4 _357 = v_u_xlat3;
    float3 _359 = v_u_xlat2.xxx * _357.xyz;
    v_u_xlat3.x = _359.x;
    v_u_xlat3.y = _359.y;
    v_u_xlat3.z = _359.z;
    v_u_xlat2.x = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    v_u_xlat6 = v_u_xlat1.xyz * v_u_xlat2.xxx;
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat6);
    v_u_xlat22 = (abs(v_u_xlat2.x) * v_u_xlat22) + v_u_xlat16;
    v_u_xlatb2.x = transpose(glstate_matrix_projection)[3].w == 0.0f;
    float _400;
    if (v_u_xlatb2.x)
    {
        _400 = v_u_xlat22;
    }
    else
    {
        _400 = v_u_xlat9.x;
    }
    v_u_xlat9.x = _400;
    v_u_xlatb22 = 0.0f >= v_in_TEXCOORD0.w;
    v_u_xlat22 = float(v_u_xlatb22);
    v_u_xlat2.x = (-_WeightNormal) + _WeightBold;
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + _WeightNormal;
    v_u_xlat22 = (v_u_xlat22 * 0.25f) + _FaceDilate;
    v_u_xlat22 *= _ScaleRatioA;
    v_u_xlat9.z = v_u_xlat22 * 0.5f;
    v_vs_TEXCOORD1.y = v_u_xlat9.xz.x;
    v_vs_TEXCOORD1.w = v_u_xlat9.xz.y;
    v_u_xlat2.x = 0.5f / v_u_xlat9.x;
    v_u_xlat16 = ((-_OutlineWidth) * _ScaleRatioA) + 1.0f;
    v_u_xlat16 = ((-_OutlineSoftness) * _ScaleRatioA) + v_u_xlat16;
    v_u_xlat16 = (v_u_xlat16 * 0.5f) + (-v_u_xlat2.x);
    v_vs_TEXCOORD1.x = ((-v_u_xlat22) * 0.5f) + v_u_xlat16;
    v_u_xlat22 = ((-v_u_xlat22) * 0.5f) + 0.5f;
    v_vs_TEXCOORD1.z = v_u_xlat2.x + v_u_xlat22;
    v_u_xlat3 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat3 = min(v_u_xlat3, 20000000000.0f.xxxx);
    float4 _496 = v_u_xlat0;
    float2 _504 = (_496.xy * 2.0f.xx) + (-v_u_xlat3.xy);
    v_u_xlat0.x = _504.x;
    v_u_xlat0.y = _504.y;
    float2 _515 = (-v_u_xlat3.zw) + v_u_xlat0.xy;
    v_vs_TEXCOORD2.x = _515.x;
    v_vs_TEXCOORD2.y = _515.y;
    float2 _530 = max(float2(_MaskSoftnessX, _MaskSoftnessY), float2(_UIMaskSoftnessX, _UIMaskSoftnessY));
    v_u_xlat0.x = _530.x;
    v_u_xlat0.y = _530.y;
    float4 _535 = v_u_xlat0;
    float2 _540 = (_535.xy * 0.25f.xx) + v_u_xlat14;
    v_u_xlat0.x = _540.x;
    v_u_xlat0.y = _540.y;
    float2 _547 = 0.25f.xx / v_u_xlat0.xy;
    v_vs_TEXCOORD2.z = _547.x;
    v_vs_TEXCOORD2.w = _547.y;
    float3 _558 = v_u_xlat1.yyy * transpose(_EnvMatrix)[1].xyz;
    v_u_xlat0.x = _558.x;
    v_u_xlat0.y = _558.y;
    v_u_xlat0.z = _558.z;
    float4 _571 = v_u_xlat0;
    float3 _573 = (transpose(_EnvMatrix)[0].xyz * v_u_xlat1.xxx) + _571.xyz;
    v_u_xlat0.x = _573.x;
    v_u_xlat0.y = _573.y;
    v_u_xlat0.z = _573.z;
    v_vs_TEXCOORD3 = (transpose(_EnvMatrix)[2].xyz * v_u_xlat1.zzz) + v_u_xlat0.xyz;
    v_u_xlat0 = float4(_UnderlaySoftness, _UnderlayDilate, _UnderlayOffsetX, _UnderlayOffsetY) * _ScaleRatioC.xxxx;
    v_u_xlat0.x = (v_u_xlat0.x * v_u_xlat9.x) + 1.0f;
    v_u_xlat0.x = v_u_xlat9.x / v_u_xlat0.x;
    v_u_xlat1.x = (v_u_xlat22 * v_u_xlat0.x) + (-0.5f);
    v_u_xlat7 = v_u_xlat0.x * v_u_xlat0.y;
    v_u_xlat14 = (-v_u_xlat0.zw) * _GradientScale.xx;
    v_u_xlat14 /= float2(_TextureWidth, _TextureHeight);
    float2 _646 = v_u_xlat14 + v_in_TEXCOORD0.xy;
    v_vs_TEXCOORD4.x = _646.x;
    v_vs_TEXCOORD4.y = _646.y;
    v_vs_TEXCOORD4.z = v_u_xlat0.x;
    v_vs_TEXCOORD4.w = ((-v_u_xlat7) * 0.5f) + v_u_xlat1.x;
    float3 _666 = _UnderlayColor.www * _UnderlayColor.xyz;
    v_u_xlat0.x = _666.x;
    v_u_xlat0.y = _666.y;
    v_u_xlat0.z = _666.z;
    v_vs_COLOR1.x = v_u_xlat0.xyz.x;
    v_vs_COLOR1.y = v_u_xlat0.xyz.y;
    v_vs_COLOR1.z = v_u_xlat0.xyz.z;
    v_vs_COLOR1.w = _UnderlayColor.w;
    float2 _695 = (v_in_TEXCOORD1 * _FaceTex_ST.xy) + _FaceTex_ST.zw;
    v_vs_TEXCOORD5.x = _695.x;
    v_vs_TEXCOORD5.y = _695.y;
    float2 _707 = (v_in_TEXCOORD1 * _OutlineTex_ST.xy) + _OutlineTex_ST.zw;
    v_vs_TEXCOORD5.z = _707.x;
    v_vs_TEXCOORD5.w = _707.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    stage_output.v_vs_COLOR1 = v_vs_COLOR1;
    stage_output.v_vs_TEXCOORD5 = v_vs_TEXCOORD5;
    return stage_output;
}
static float4 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD5;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD4;
static float4 f_vs_COLOR1;
static float4 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float2 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD2 : TEXCOORD3;
    float4 f_vs_TEXCOORD4 : TEXCOORD4;
    float4 f_vs_COLOR1 : TEXCOORD5;
    float4 f_vs_TEXCOORD5 : TEXCOORD6;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat0;
static float f_u_xlat16_1;
static float f_u_xlat16_6;
static float4 f_u_xlat16_0;
static float f_u_xlat16_11;
static float4 f_u_xlat2;
static float3 f_u_xlat16_3;
static float4 f_u_xlat16_4;
static float2 f_u_xlat4;
static float4 f_u_xlat16_2;
static float f_u_xlat9;
static bool f_u_xlatb4;

void f_frag_main()
{
    f_u_xlat0.x = _OutlineWidth * _ScaleRatioA;
    f_u_xlat0.x *= f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = min(f_u_xlat0.x, 1.0f);
    f_u_xlat16_6 = f_u_xlat0.x * 0.5f;
    f_u_xlat16_1 = sqrt(f_u_xlat16_1);
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).w;
    f_u_xlat0.x = (-f_u_xlat16_0.x) + f_vs_TEXCOORD1.z;
    f_u_xlat16_11 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + f_u_xlat16_6;
    f_u_xlat16_11 = clamp(f_u_xlat16_11, 0.0f, 1.0f);
    f_u_xlat16_6 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + (-f_u_xlat16_6);
    f_u_xlat16_1 *= f_u_xlat16_11;
    float2 _99 = (float2(_OutlineUVSpeedX, _OutlineUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.zw;
    f_u_xlat0.x = _99.x;
    f_u_xlat0.y = _99.y;
    f_u_xlat16_0 = _OutlineTex.Sample(sampler_OutlineTex, f_u_xlat0.xy);
    f_u_xlat2 = f_u_xlat16_0 * _OutlineColor;
    f_u_xlat16_3 = f_vs_COLOR0.xyz * _FaceColor.xyz;
    float2 _134 = (float2(_FaceUVSpeedX, _FaceUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.xy;
    f_u_xlat0.x = _134.x;
    f_u_xlat0.y = _134.y;
    f_u_xlat16_4 = _FaceTex.Sample(sampler_FaceTex, f_u_xlat0.xy);
    float3 _148 = f_u_xlat16_3 * f_u_xlat16_4.xyz;
    f_u_xlat0.x = _148.x;
    f_u_xlat0.y = _148.y;
    f_u_xlat0.z = _148.z;
    f_u_xlat4.x = f_u_xlat16_4.w * _FaceColor.w;
    f_u_xlat16_3 = f_u_xlat0.xyz * f_u_xlat4.xxx;
    float3 _176 = (f_u_xlat2.xyz * f_u_xlat2.www) + (-f_u_xlat16_3);
    f_u_xlat16_2.x = _176.x;
    f_u_xlat16_2.y = _176.y;
    f_u_xlat16_2.z = _176.z;
    f_u_xlat16_2.w = (_OutlineColor.w * f_u_xlat16_0.w) + (-f_u_xlat4.x);
    f_u_xlat16_2 = f_u_xlat16_1.xxxx * f_u_xlat16_2;
    float3 _204 = (f_u_xlat0.xyz * f_u_xlat4.xxx) + f_u_xlat16_2.xyz;
    f_u_xlat16_0.x = _204.x;
    f_u_xlat16_0.y = _204.y;
    f_u_xlat16_0.z = _204.z;
    f_u_xlat16_0.w = (_FaceColor.w * f_u_xlat16_4.w) + f_u_xlat16_2.w;
    f_u_xlat4.x = _OutlineSoftness * _ScaleRatioA;
    f_u_xlat9 = f_u_xlat4.x * f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = (f_u_xlat4.x * f_vs_TEXCOORD1.y) + 1.0f;
    f_u_xlat16_6 = (f_u_xlat9 * 0.5f) + f_u_xlat16_6;
    f_u_xlat16_1 = f_u_xlat16_6 / f_u_xlat16_1;
    f_u_xlat16_1 = clamp(f_u_xlat16_1, 0.0f, 1.0f);
    f_u_xlat16_1 = (-f_u_xlat16_1) + 1.0f;
    f_u_xlat16_2 = f_u_xlat16_0 * f_u_xlat16_1.xxxx;
    f_u_xlat16_1 = ((-f_u_xlat16_0.w) * f_u_xlat16_1) + 1.0f;
    f_u_xlat16_4.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD4.xy).w;
    f_u_xlat4.x = (f_u_xlat16_4.x * f_vs_TEXCOORD4.z) + (-f_vs_TEXCOORD4.w);
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat0 = f_u_xlat4.xxxx * f_vs_COLOR1;
    f_u_xlat0 = (f_u_xlat0 * f_u_xlat16_1.xxxx) + f_u_xlat16_2;
    f_u_xlat4 = (-_ClipRect.xy) + _ClipRect.zw;
    f_u_xlat4 += (-abs(f_vs_TEXCOORD2.xy));
    f_u_xlat4 *= f_vs_TEXCOORD2.zw;
    f_u_xlat4 = clamp(f_u_xlat4, 0.0f.xx, 1.0f.xx);
    f_u_xlat16_1 = f_u_xlat4.y * f_u_xlat4.x;
    f_u_xlat16_6 = (f_u_xlat0.w * f_u_xlat16_1) + (-0.001000000047497451305389404296875f);
    f_u_xlat16_0 = f_u_xlat0 * f_u_xlat16_1.xxxx;
    f_SV_Target0 = f_u_xlat16_0 * f_vs_COLOR0.wwww;
    f_u_xlatb4 = f_u_xlat16_6 < 0.0f;
    if (f_u_xlatb4)
    {
        discard;
    }
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD5 = stage_input.f_vs_TEXCOORD5;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_vs_COLOR1 = stage_input.f_vs_COLOR1;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(UNDERLAY_ON) && defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float4x4 _EnvMatrix;
float _VertexOffsetX;
float _VertexOffsetY;
float3 _WorldSpaceCameraPos;
int _UIVertexColorAlwaysGammaSpace;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineWidth;
float _OutlineSoftness;
float4 _ClipRect;
float _MaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float _UnderlaySoftness;
float _UnderlayDilate;
float _UnderlayOffsetX;
float _UnderlayOffsetY;
float _ScaleRatioC;
float _TextureWidth;
float _TextureHeight;
float4 _UnderlayColor;
float4 _FaceTex_ST;
float4 _OutlineTex_ST;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _OutlineUVSpeedX;
float _OutlineUVSpeedY;
float4 _Time;
Texture2D<float4> _OutlineTex;
SamplerState sampler_OutlineTex;
float4 _OutlineColor;
float4 _FaceColor;
float _FaceUVSpeedX;
float _FaceUVSpeedY;
Texture2D<float4> _FaceTex;
SamplerState sampler_FaceTex;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD5;
static float2 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float2 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD4 : TEXCOORD3;
    float4 v_vs_COLOR1 : TEXCOORD4;
    float4 v_vs_TEXCOORD5 : TEXCOORD5;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float4 v_u_xlat3;
static float3 v_u_xlat16_4;
static float3 v_u_xlat16_5;
static bool3 v_u_xlatb2;
static float2 v_u_xlat14;
static float v_u_xlat22;
static float3 v_u_xlat9;
static float v_u_xlat16;
static float3 v_u_xlat6;
static bool v_u_xlatb22;
static float4 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;
static float v_u_xlat7;

void v_vert_main()
{
    float2 _21 = v_in_POSITION0.xy + float2(_VertexOffsetX, _VertexOffsetY);
    v_u_xlat0.x = _21.x;
    v_u_xlat0.y = _21.y;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + transpose(unity_ObjectToWorld)[3];
    float4 _72 = v_u_xlat1;
    float3 _74 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + _72.xyz;
    v_u_xlat1.x = _74.x;
    v_u_xlat1.y = _74.y;
    v_u_xlat1.z = _74.z;
    float4 _82 = v_u_xlat1;
    float3 _88 = (-_82.xyz) + _WorldSpaceCameraPos;
    v_u_xlat1.x = _88.x;
    v_u_xlat1.y = _88.y;
    v_u_xlat1.z = _88.z;
    v_u_xlat3 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat3 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat3;
    v_u_xlat3 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat3;
    v_u_xlat2 = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat3;
    v_gl_Position = v_u_xlat2;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * 0.26588499546051025390625f.xxx) + 0.736584007740020751953125f.xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + (-0.00980184040963649749755859375f).xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + 0.0031969700939953327178955078125f.xxx;
    v_u_xlat16_5 = (v_in_COLOR0.xyz * 0.084971003234386444091796875f.xxx) + (-0.0001630290062166750431060791015625f).xxx;
    v_u_xlatb2 = bool4(v_in_COLOR0.xyzx.x < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).x, v_in_COLOR0.xyzx.y < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).y, v_in_COLOR0.xyzx.z < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).z, v_in_COLOR0.xyzx.w < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).w).xyz;
    float3 hlslcc_movcTemp = v_u_xlat16_4;
    float _182;
    if (v_u_xlatb2.x)
    {
        _182 = v_u_xlat16_5.x;
    }
    else
    {
        _182 = v_u_xlat16_4.x;
    }
    hlslcc_movcTemp.x = _182;
    float _194;
    if (v_u_xlatb2.y)
    {
        _194 = v_u_xlat16_5.y;
    }
    else
    {
        _194 = v_u_xlat16_4.y;
    }
    hlslcc_movcTemp.y = _194;
    float _206;
    if (v_u_xlatb2.z)
    {
        _206 = v_u_xlat16_5.z;
    }
    else
    {
        _206 = v_u_xlat16_4.z;
    }
    hlslcc_movcTemp.z = _206;
    v_u_xlat16_4 = hlslcc_movcTemp;
    float3 _222;
    if (_UIVertexColorAlwaysGammaSpace != 0)
    {
        _222 = v_u_xlat16_4;
    }
    else
    {
        _222 = v_in_COLOR0.xyz;
    }
    v_vs_COLOR0.x = _222.x;
    v_vs_COLOR0.y = _222.y;
    v_vs_COLOR0.z = _222.z;
    v_vs_COLOR0.w = v_in_COLOR0.w;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xy;
    v_u_xlat14 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat14 = (transpose(glstate_matrix_projection)[0].xy * _ScreenParams.xx) + v_u_xlat14;
    v_u_xlat14 = abs(v_u_xlat14) * float2(_ScaleX, _ScaleY);
    v_u_xlat14 = v_u_xlat2.ww / v_u_xlat14;
    v_u_xlat22 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat22 = rsqrt(v_u_xlat22);
    v_u_xlat2.x = abs(v_in_TEXCOORD0.w) * _GradientScale;
    v_u_xlat9.x = _Sharpness + 1.0f;
    v_u_xlat2.x = v_u_xlat9.x * v_u_xlat2.x;
    v_u_xlat9.x = v_u_xlat22 * v_u_xlat2.x;
    v_u_xlat16 = (-_PerspectiveFilter) + 1.0f;
    v_u_xlat16 *= abs(v_u_xlat9.x);
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + (-v_u_xlat16);
    v_u_xlat3.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat3.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat3.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat3.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    float4 _357 = v_u_xlat3;
    float3 _359 = v_u_xlat2.xxx * _357.xyz;
    v_u_xlat3.x = _359.x;
    v_u_xlat3.y = _359.y;
    v_u_xlat3.z = _359.z;
    v_u_xlat2.x = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    v_u_xlat6 = v_u_xlat1.xyz * v_u_xlat2.xxx;
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat6);
    v_u_xlat22 = (abs(v_u_xlat2.x) * v_u_xlat22) + v_u_xlat16;
    v_u_xlatb2.x = transpose(glstate_matrix_projection)[3].w == 0.0f;
    float _400;
    if (v_u_xlatb2.x)
    {
        _400 = v_u_xlat22;
    }
    else
    {
        _400 = v_u_xlat9.x;
    }
    v_u_xlat9.x = _400;
    v_u_xlatb22 = 0.0f >= v_in_TEXCOORD0.w;
    v_u_xlat22 = float(v_u_xlatb22);
    v_u_xlat2.x = (-_WeightNormal) + _WeightBold;
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + _WeightNormal;
    v_u_xlat22 = (v_u_xlat22 * 0.25f) + _FaceDilate;
    v_u_xlat22 *= _ScaleRatioA;
    v_u_xlat9.z = v_u_xlat22 * 0.5f;
    v_vs_TEXCOORD1.y = v_u_xlat9.xz.x;
    v_vs_TEXCOORD1.w = v_u_xlat9.xz.y;
    v_u_xlat2.x = 0.5f / v_u_xlat9.x;
    v_u_xlat16 = ((-_OutlineWidth) * _ScaleRatioA) + 1.0f;
    v_u_xlat16 = ((-_OutlineSoftness) * _ScaleRatioA) + v_u_xlat16;
    v_u_xlat16 = (v_u_xlat16 * 0.5f) + (-v_u_xlat2.x);
    v_vs_TEXCOORD1.x = ((-v_u_xlat22) * 0.5f) + v_u_xlat16;
    v_u_xlat22 = ((-v_u_xlat22) * 0.5f) + 0.5f;
    v_vs_TEXCOORD1.z = v_u_xlat2.x + v_u_xlat22;
    v_u_xlat3 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat3 = min(v_u_xlat3, 20000000000.0f.xxxx);
    float4 _496 = v_u_xlat0;
    float2 _504 = (_496.xy * 2.0f.xx) + (-v_u_xlat3.xy);
    v_u_xlat0.x = _504.x;
    v_u_xlat0.y = _504.y;
    float2 _515 = (-v_u_xlat3.zw) + v_u_xlat0.xy;
    v_vs_TEXCOORD2.x = _515.x;
    v_vs_TEXCOORD2.y = _515.y;
    float2 _530 = max(float2(_MaskSoftnessX, _MaskSoftnessY), float2(_UIMaskSoftnessX, _UIMaskSoftnessY));
    v_u_xlat0.x = _530.x;
    v_u_xlat0.y = _530.y;
    float4 _535 = v_u_xlat0;
    float2 _540 = (_535.xy * 0.25f.xx) + v_u_xlat14;
    v_u_xlat0.x = _540.x;
    v_u_xlat0.y = _540.y;
    float2 _547 = 0.25f.xx / v_u_xlat0.xy;
    v_vs_TEXCOORD2.z = _547.x;
    v_vs_TEXCOORD2.w = _547.y;
    float3 _558 = v_u_xlat1.yyy * transpose(_EnvMatrix)[1].xyz;
    v_u_xlat0.x = _558.x;
    v_u_xlat0.y = _558.y;
    v_u_xlat0.z = _558.z;
    float4 _571 = v_u_xlat0;
    float3 _573 = (transpose(_EnvMatrix)[0].xyz * v_u_xlat1.xxx) + _571.xyz;
    v_u_xlat0.x = _573.x;
    v_u_xlat0.y = _573.y;
    v_u_xlat0.z = _573.z;
    v_vs_TEXCOORD3 = (transpose(_EnvMatrix)[2].xyz * v_u_xlat1.zzz) + v_u_xlat0.xyz;
    v_u_xlat0 = float4(_UnderlaySoftness, _UnderlayDilate, _UnderlayOffsetX, _UnderlayOffsetY) * _ScaleRatioC.xxxx;
    v_u_xlat0.x = (v_u_xlat0.x * v_u_xlat9.x) + 1.0f;
    v_u_xlat0.x = v_u_xlat9.x / v_u_xlat0.x;
    v_u_xlat1.x = (v_u_xlat22 * v_u_xlat0.x) + (-0.5f);
    v_u_xlat7 = v_u_xlat0.x * v_u_xlat0.y;
    v_u_xlat14 = (-v_u_xlat0.zw) * _GradientScale.xx;
    v_u_xlat14 /= float2(_TextureWidth, _TextureHeight);
    float2 _646 = v_u_xlat14 + v_in_TEXCOORD0.xy;
    v_vs_TEXCOORD4.x = _646.x;
    v_vs_TEXCOORD4.y = _646.y;
    v_vs_TEXCOORD4.z = v_u_xlat0.x;
    v_vs_TEXCOORD4.w = ((-v_u_xlat7) * 0.5f) + v_u_xlat1.x;
    float3 _666 = _UnderlayColor.www * _UnderlayColor.xyz;
    v_u_xlat0.x = _666.x;
    v_u_xlat0.y = _666.y;
    v_u_xlat0.z = _666.z;
    v_vs_COLOR1.x = v_u_xlat0.xyz.x;
    v_vs_COLOR1.y = v_u_xlat0.xyz.y;
    v_vs_COLOR1.z = v_u_xlat0.xyz.z;
    v_vs_COLOR1.w = _UnderlayColor.w;
    float2 _695 = (v_in_TEXCOORD1 * _FaceTex_ST.xy) + _FaceTex_ST.zw;
    v_vs_TEXCOORD5.x = _695.x;
    v_vs_TEXCOORD5.y = _695.y;
    float2 _707 = (v_in_TEXCOORD1 * _OutlineTex_ST.xy) + _OutlineTex_ST.zw;
    v_vs_TEXCOORD5.z = _707.x;
    v_vs_TEXCOORD5.w = _707.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    stage_output.v_vs_COLOR1 = v_vs_COLOR1;
    stage_output.v_vs_TEXCOORD5 = v_vs_TEXCOORD5;
    return stage_output;
}
static float4 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD5;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD4;
static float4 f_vs_COLOR1;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float2 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD4 : TEXCOORD3;
    float4 f_vs_COLOR1 : TEXCOORD4;
    float4 f_vs_TEXCOORD5 : TEXCOORD5;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat0;
static float f_u_xlat16_1;
static float f_u_xlat16_6;
static float4 f_u_xlat16_0;
static float f_u_xlat16_11;
static float4 f_u_xlat2;
static float3 f_u_xlat16_3;
static float4 f_u_xlat16_4;
static float f_u_xlat4;
static float4 f_u_xlat16_2;
static float f_u_xlat9;
static bool f_u_xlatb4;

void f_frag_main()
{
    f_u_xlat0.x = _OutlineWidth * _ScaleRatioA;
    f_u_xlat0.x *= f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = min(f_u_xlat0.x, 1.0f);
    f_u_xlat16_6 = f_u_xlat0.x * 0.5f;
    f_u_xlat16_1 = sqrt(f_u_xlat16_1);
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).w;
    f_u_xlat0.x = (-f_u_xlat16_0.x) + f_vs_TEXCOORD1.z;
    f_u_xlat16_11 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + f_u_xlat16_6;
    f_u_xlat16_11 = clamp(f_u_xlat16_11, 0.0f, 1.0f);
    f_u_xlat16_6 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + (-f_u_xlat16_6);
    f_u_xlat16_1 *= f_u_xlat16_11;
    float2 _99 = (float2(_OutlineUVSpeedX, _OutlineUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.zw;
    f_u_xlat0.x = _99.x;
    f_u_xlat0.y = _99.y;
    f_u_xlat16_0 = _OutlineTex.Sample(sampler_OutlineTex, f_u_xlat0.xy);
    f_u_xlat2 = f_u_xlat16_0 * _OutlineColor;
    f_u_xlat16_3 = f_vs_COLOR0.xyz * _FaceColor.xyz;
    float2 _134 = (float2(_FaceUVSpeedX, _FaceUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.xy;
    f_u_xlat0.x = _134.x;
    f_u_xlat0.y = _134.y;
    f_u_xlat16_4 = _FaceTex.Sample(sampler_FaceTex, f_u_xlat0.xy);
    float3 _148 = f_u_xlat16_3 * f_u_xlat16_4.xyz;
    f_u_xlat0.x = _148.x;
    f_u_xlat0.y = _148.y;
    f_u_xlat0.z = _148.z;
    f_u_xlat4 = f_u_xlat16_4.w * _FaceColor.w;
    f_u_xlat16_3 = f_u_xlat0.xyz * f_u_xlat4.xxx;
    float3 _174 = (f_u_xlat2.xyz * f_u_xlat2.www) + (-f_u_xlat16_3);
    f_u_xlat16_2.x = _174.x;
    f_u_xlat16_2.y = _174.y;
    f_u_xlat16_2.z = _174.z;
    f_u_xlat16_2.w = (_OutlineColor.w * f_u_xlat16_0.w) + (-f_u_xlat4);
    f_u_xlat16_2 = f_u_xlat16_1.xxxx * f_u_xlat16_2;
    float3 _201 = (f_u_xlat0.xyz * f_u_xlat4.xxx) + f_u_xlat16_2.xyz;
    f_u_xlat16_0.x = _201.x;
    f_u_xlat16_0.y = _201.y;
    f_u_xlat16_0.z = _201.z;
    f_u_xlat16_0.w = (_FaceColor.w * f_u_xlat16_4.w) + f_u_xlat16_2.w;
    f_u_xlat4 = _OutlineSoftness * _ScaleRatioA;
    f_u_xlat9 = f_u_xlat4 * f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = (f_u_xlat4 * f_vs_TEXCOORD1.y) + 1.0f;
    f_u_xlat16_6 = (f_u_xlat9 * 0.5f) + f_u_xlat16_6;
    f_u_xlat16_1 = f_u_xlat16_6 / f_u_xlat16_1;
    f_u_xlat16_1 = clamp(f_u_xlat16_1, 0.0f, 1.0f);
    f_u_xlat16_1 = (-f_u_xlat16_1) + 1.0f;
    f_u_xlat16_2 = f_u_xlat16_0 * f_u_xlat16_1.xxxx;
    f_u_xlat16_1 = ((-f_u_xlat16_0.w) * f_u_xlat16_1) + 1.0f;
    f_u_xlat16_4.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD4.xy).w;
    f_u_xlat4 = (f_u_xlat16_4.x * f_vs_TEXCOORD4.z) + (-f_vs_TEXCOORD4.w);
    f_u_xlat4 = clamp(f_u_xlat4, 0.0f, 1.0f);
    f_u_xlat0 = f_u_xlat4.xxxx * f_vs_COLOR1;
    f_u_xlat0 = (f_u_xlat0 * f_u_xlat16_1.xxxx) + f_u_xlat16_2;
    f_u_xlat16_1 = f_u_xlat0.w + (-0.001000000047497451305389404296875f);
    f_SV_Target0 = f_u_xlat0 * f_vs_COLOR0.wwww;
    f_u_xlatb4 = f_u_xlat16_1 < 0.0f;
    if (f_u_xlatb4)
    {
        discard;
    }
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD5 = stage_input.f_vs_TEXCOORD5;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_vs_COLOR1 = stage_input.f_vs_COLOR1;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(UNDERLAY_ON) && !defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float4x4 _EnvMatrix;
float _VertexOffsetX;
float _VertexOffsetY;
float3 _WorldSpaceCameraPos;
int _UIVertexColorAlwaysGammaSpace;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineWidth;
float _OutlineSoftness;
float4 _ClipRect;
float _MaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float _UnderlaySoftness;
float _UnderlayDilate;
float _UnderlayOffsetX;
float _UnderlayOffsetY;
float _ScaleRatioC;
float _TextureWidth;
float _TextureHeight;
float4 _UnderlayColor;
float4 _FaceTex_ST;
float4 _OutlineTex_ST;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _OutlineUVSpeedX;
float _OutlineUVSpeedY;
float4 _Time;
Texture2D<float4> _OutlineTex;
SamplerState sampler_OutlineTex;
float4 _OutlineColor;
float4 _FaceColor;
float _FaceUVSpeedX;
float _FaceUVSpeedY;
Texture2D<float4> _FaceTex;
SamplerState sampler_FaceTex;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD5;
static float2 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float2 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD2 : TEXCOORD3;
    float4 v_vs_TEXCOORD4 : TEXCOORD4;
    float4 v_vs_COLOR1 : TEXCOORD5;
    float4 v_vs_TEXCOORD5 : TEXCOORD6;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float4 v_u_xlat3;
static float3 v_u_xlat16_4;
static float3 v_u_xlat16_5;
static bool3 v_u_xlatb2;
static float2 v_u_xlat14;
static float v_u_xlat22;
static float3 v_u_xlat9;
static float v_u_xlat16;
static float3 v_u_xlat6;
static bool v_u_xlatb22;
static float3 v_vs_TEXCOORD3;
static float v_u_xlat7;

void v_vert_main()
{
    float2 _21 = v_in_POSITION0.xy + float2(_VertexOffsetX, _VertexOffsetY);
    v_u_xlat0.x = _21.x;
    v_u_xlat0.y = _21.y;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + transpose(unity_ObjectToWorld)[3];
    float4 _72 = v_u_xlat1;
    float3 _74 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + _72.xyz;
    v_u_xlat1.x = _74.x;
    v_u_xlat1.y = _74.y;
    v_u_xlat1.z = _74.z;
    float4 _82 = v_u_xlat1;
    float3 _88 = (-_82.xyz) + _WorldSpaceCameraPos;
    v_u_xlat1.x = _88.x;
    v_u_xlat1.y = _88.y;
    v_u_xlat1.z = _88.z;
    v_u_xlat3 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat3 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat3;
    v_u_xlat3 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat3;
    v_u_xlat2 = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat3;
    v_gl_Position = v_u_xlat2;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * 0.26588499546051025390625f.xxx) + 0.736584007740020751953125f.xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + (-0.00980184040963649749755859375f).xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + 0.0031969700939953327178955078125f.xxx;
    v_u_xlat16_5 = (v_in_COLOR0.xyz * 0.084971003234386444091796875f.xxx) + (-0.0001630290062166750431060791015625f).xxx;
    v_u_xlatb2 = bool4(v_in_COLOR0.xyzx.x < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).x, v_in_COLOR0.xyzx.y < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).y, v_in_COLOR0.xyzx.z < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).z, v_in_COLOR0.xyzx.w < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).w).xyz;
    float3 hlslcc_movcTemp = v_u_xlat16_4;
    float _182;
    if (v_u_xlatb2.x)
    {
        _182 = v_u_xlat16_5.x;
    }
    else
    {
        _182 = v_u_xlat16_4.x;
    }
    hlslcc_movcTemp.x = _182;
    float _194;
    if (v_u_xlatb2.y)
    {
        _194 = v_u_xlat16_5.y;
    }
    else
    {
        _194 = v_u_xlat16_4.y;
    }
    hlslcc_movcTemp.y = _194;
    float _206;
    if (v_u_xlatb2.z)
    {
        _206 = v_u_xlat16_5.z;
    }
    else
    {
        _206 = v_u_xlat16_4.z;
    }
    hlslcc_movcTemp.z = _206;
    v_u_xlat16_4 = hlslcc_movcTemp;
    float3 _222;
    if (_UIVertexColorAlwaysGammaSpace != 0)
    {
        _222 = v_u_xlat16_4;
    }
    else
    {
        _222 = v_in_COLOR0.xyz;
    }
    v_vs_COLOR0.x = _222.x;
    v_vs_COLOR0.y = _222.y;
    v_vs_COLOR0.z = _222.z;
    v_vs_COLOR0.w = v_in_COLOR0.w;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xy;
    v_u_xlat14 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat14 = (transpose(glstate_matrix_projection)[0].xy * _ScreenParams.xx) + v_u_xlat14;
    v_u_xlat14 = abs(v_u_xlat14) * float2(_ScaleX, _ScaleY);
    v_u_xlat14 = v_u_xlat2.ww / v_u_xlat14;
    v_u_xlat22 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat22 = rsqrt(v_u_xlat22);
    v_u_xlat2.x = abs(v_in_TEXCOORD0.w) * _GradientScale;
    v_u_xlat9.x = _Sharpness + 1.0f;
    v_u_xlat2.x = v_u_xlat9.x * v_u_xlat2.x;
    v_u_xlat9.x = v_u_xlat22 * v_u_xlat2.x;
    v_u_xlat16 = (-_PerspectiveFilter) + 1.0f;
    v_u_xlat16 *= abs(v_u_xlat9.x);
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + (-v_u_xlat16);
    v_u_xlat3.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat3.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat3.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat3.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    float4 _357 = v_u_xlat3;
    float3 _359 = v_u_xlat2.xxx * _357.xyz;
    v_u_xlat3.x = _359.x;
    v_u_xlat3.y = _359.y;
    v_u_xlat3.z = _359.z;
    v_u_xlat2.x = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    v_u_xlat6 = v_u_xlat1.xyz * v_u_xlat2.xxx;
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat6);
    v_u_xlat22 = (abs(v_u_xlat2.x) * v_u_xlat22) + v_u_xlat16;
    v_u_xlatb2.x = transpose(glstate_matrix_projection)[3].w == 0.0f;
    float _400;
    if (v_u_xlatb2.x)
    {
        _400 = v_u_xlat22;
    }
    else
    {
        _400 = v_u_xlat9.x;
    }
    v_u_xlat9.x = _400;
    v_u_xlatb22 = 0.0f >= v_in_TEXCOORD0.w;
    v_u_xlat22 = float(v_u_xlatb22);
    v_u_xlat2.x = (-_WeightNormal) + _WeightBold;
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + _WeightNormal;
    v_u_xlat22 = (v_u_xlat22 * 0.25f) + _FaceDilate;
    v_u_xlat22 *= _ScaleRatioA;
    v_u_xlat9.z = v_u_xlat22 * 0.5f;
    v_vs_TEXCOORD1.y = v_u_xlat9.xz.x;
    v_vs_TEXCOORD1.w = v_u_xlat9.xz.y;
    v_u_xlat2.x = 0.5f / v_u_xlat9.x;
    v_u_xlat16 = ((-_OutlineWidth) * _ScaleRatioA) + 1.0f;
    v_u_xlat16 = ((-_OutlineSoftness) * _ScaleRatioA) + v_u_xlat16;
    v_u_xlat16 = (v_u_xlat16 * 0.5f) + (-v_u_xlat2.x);
    v_vs_TEXCOORD1.x = ((-v_u_xlat22) * 0.5f) + v_u_xlat16;
    v_u_xlat22 = ((-v_u_xlat22) * 0.5f) + 0.5f;
    v_vs_TEXCOORD1.z = v_u_xlat2.x + v_u_xlat22;
    v_u_xlat3 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat3 = min(v_u_xlat3, 20000000000.0f.xxxx);
    float4 _496 = v_u_xlat0;
    float2 _504 = (_496.xy * 2.0f.xx) + (-v_u_xlat3.xy);
    v_u_xlat0.x = _504.x;
    v_u_xlat0.y = _504.y;
    float2 _515 = (-v_u_xlat3.zw) + v_u_xlat0.xy;
    v_vs_TEXCOORD2.x = _515.x;
    v_vs_TEXCOORD2.y = _515.y;
    float2 _530 = max(float2(_MaskSoftnessX, _MaskSoftnessY), float2(_UIMaskSoftnessX, _UIMaskSoftnessY));
    v_u_xlat0.x = _530.x;
    v_u_xlat0.y = _530.y;
    float4 _535 = v_u_xlat0;
    float2 _540 = (_535.xy * 0.25f.xx) + v_u_xlat14;
    v_u_xlat0.x = _540.x;
    v_u_xlat0.y = _540.y;
    float2 _547 = 0.25f.xx / v_u_xlat0.xy;
    v_vs_TEXCOORD2.z = _547.x;
    v_vs_TEXCOORD2.w = _547.y;
    float3 _558 = v_u_xlat1.yyy * transpose(_EnvMatrix)[1].xyz;
    v_u_xlat0.x = _558.x;
    v_u_xlat0.y = _558.y;
    v_u_xlat0.z = _558.z;
    float4 _571 = v_u_xlat0;
    float3 _573 = (transpose(_EnvMatrix)[0].xyz * v_u_xlat1.xxx) + _571.xyz;
    v_u_xlat0.x = _573.x;
    v_u_xlat0.y = _573.y;
    v_u_xlat0.z = _573.z;
    v_vs_TEXCOORD3 = (transpose(_EnvMatrix)[2].xyz * v_u_xlat1.zzz) + v_u_xlat0.xyz;
    v_u_xlat0 = float4(_UnderlaySoftness, _UnderlayDilate, _UnderlayOffsetX, _UnderlayOffsetY) * _ScaleRatioC.xxxx;
    v_u_xlat0.x = (v_u_xlat0.x * v_u_xlat9.x) + 1.0f;
    v_u_xlat0.x = v_u_xlat9.x / v_u_xlat0.x;
    v_u_xlat1.x = (v_u_xlat22 * v_u_xlat0.x) + (-0.5f);
    v_u_xlat7 = v_u_xlat0.x * v_u_xlat0.y;
    v_u_xlat14 = (-v_u_xlat0.zw) * _GradientScale.xx;
    v_u_xlat14 /= float2(_TextureWidth, _TextureHeight);
    float2 _646 = v_u_xlat14 + v_in_TEXCOORD0.xy;
    v_vs_TEXCOORD4.x = _646.x;
    v_vs_TEXCOORD4.y = _646.y;
    v_vs_TEXCOORD4.z = v_u_xlat0.x;
    v_vs_TEXCOORD4.w = ((-v_u_xlat7) * 0.5f) + v_u_xlat1.x;
    float3 _666 = _UnderlayColor.www * _UnderlayColor.xyz;
    v_u_xlat0.x = _666.x;
    v_u_xlat0.y = _666.y;
    v_u_xlat0.z = _666.z;
    v_vs_COLOR1.x = v_u_xlat0.xyz.x;
    v_vs_COLOR1.y = v_u_xlat0.xyz.y;
    v_vs_COLOR1.z = v_u_xlat0.xyz.z;
    v_vs_COLOR1.w = _UnderlayColor.w;
    float2 _695 = (v_in_TEXCOORD1 * _FaceTex_ST.xy) + _FaceTex_ST.zw;
    v_vs_TEXCOORD5.x = _695.x;
    v_vs_TEXCOORD5.y = _695.y;
    float2 _707 = (v_in_TEXCOORD1 * _OutlineTex_ST.xy) + _OutlineTex_ST.zw;
    v_vs_TEXCOORD5.z = _707.x;
    v_vs_TEXCOORD5.w = _707.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    stage_output.v_vs_COLOR1 = v_vs_COLOR1;
    stage_output.v_vs_TEXCOORD5 = v_vs_TEXCOORD5;
    return stage_output;
}
static float4 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD5;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD4;
static float4 f_vs_COLOR1;
static float4 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float2 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD2 : TEXCOORD3;
    float4 f_vs_TEXCOORD4 : TEXCOORD4;
    float4 f_vs_COLOR1 : TEXCOORD5;
    float4 f_vs_TEXCOORD5 : TEXCOORD6;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat0;
static float f_u_xlat16_1;
static float f_u_xlat16_6;
static float4 f_u_xlat16_0;
static float f_u_xlat16_11;
static float4 f_u_xlat2;
static float3 f_u_xlat16_3;
static float4 f_u_xlat16_4;
static float2 f_u_xlat4;
static float4 f_u_xlat16_2;
static float f_u_xlat9;

void f_frag_main()
{
    f_u_xlat0.x = _OutlineWidth * _ScaleRatioA;
    f_u_xlat0.x *= f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = min(f_u_xlat0.x, 1.0f);
    f_u_xlat16_6 = f_u_xlat0.x * 0.5f;
    f_u_xlat16_1 = sqrt(f_u_xlat16_1);
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).w;
    f_u_xlat0.x = (-f_u_xlat16_0.x) + f_vs_TEXCOORD1.z;
    f_u_xlat16_11 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + f_u_xlat16_6;
    f_u_xlat16_11 = clamp(f_u_xlat16_11, 0.0f, 1.0f);
    f_u_xlat16_6 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + (-f_u_xlat16_6);
    f_u_xlat16_1 *= f_u_xlat16_11;
    float2 _99 = (float2(_OutlineUVSpeedX, _OutlineUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.zw;
    f_u_xlat0.x = _99.x;
    f_u_xlat0.y = _99.y;
    f_u_xlat16_0 = _OutlineTex.Sample(sampler_OutlineTex, f_u_xlat0.xy);
    f_u_xlat2 = f_u_xlat16_0 * _OutlineColor;
    f_u_xlat16_3 = f_vs_COLOR0.xyz * _FaceColor.xyz;
    float2 _134 = (float2(_FaceUVSpeedX, _FaceUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.xy;
    f_u_xlat0.x = _134.x;
    f_u_xlat0.y = _134.y;
    f_u_xlat16_4 = _FaceTex.Sample(sampler_FaceTex, f_u_xlat0.xy);
    float3 _148 = f_u_xlat16_3 * f_u_xlat16_4.xyz;
    f_u_xlat0.x = _148.x;
    f_u_xlat0.y = _148.y;
    f_u_xlat0.z = _148.z;
    f_u_xlat4.x = f_u_xlat16_4.w * _FaceColor.w;
    f_u_xlat16_3 = f_u_xlat0.xyz * f_u_xlat4.xxx;
    float3 _176 = (f_u_xlat2.xyz * f_u_xlat2.www) + (-f_u_xlat16_3);
    f_u_xlat16_2.x = _176.x;
    f_u_xlat16_2.y = _176.y;
    f_u_xlat16_2.z = _176.z;
    f_u_xlat16_2.w = (_OutlineColor.w * f_u_xlat16_0.w) + (-f_u_xlat4.x);
    f_u_xlat16_2 = f_u_xlat16_1.xxxx * f_u_xlat16_2;
    float3 _204 = (f_u_xlat0.xyz * f_u_xlat4.xxx) + f_u_xlat16_2.xyz;
    f_u_xlat16_0.x = _204.x;
    f_u_xlat16_0.y = _204.y;
    f_u_xlat16_0.z = _204.z;
    f_u_xlat16_0.w = (_FaceColor.w * f_u_xlat16_4.w) + f_u_xlat16_2.w;
    f_u_xlat4.x = _OutlineSoftness * _ScaleRatioA;
    f_u_xlat9 = f_u_xlat4.x * f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = (f_u_xlat4.x * f_vs_TEXCOORD1.y) + 1.0f;
    f_u_xlat16_6 = (f_u_xlat9 * 0.5f) + f_u_xlat16_6;
    f_u_xlat16_1 = f_u_xlat16_6 / f_u_xlat16_1;
    f_u_xlat16_1 = clamp(f_u_xlat16_1, 0.0f, 1.0f);
    f_u_xlat16_1 = (-f_u_xlat16_1) + 1.0f;
    f_u_xlat16_2 = f_u_xlat16_0 * f_u_xlat16_1.xxxx;
    f_u_xlat16_1 = ((-f_u_xlat16_0.w) * f_u_xlat16_1) + 1.0f;
    f_u_xlat16_4.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD4.xy).w;
    f_u_xlat4.x = (f_u_xlat16_4.x * f_vs_TEXCOORD4.z) + (-f_vs_TEXCOORD4.w);
    f_u_xlat4.x = clamp(f_u_xlat4.x, 0.0f, 1.0f);
    f_u_xlat0 = f_u_xlat4.xxxx * f_vs_COLOR1;
    f_u_xlat0 = (f_u_xlat0 * f_u_xlat16_1.xxxx) + f_u_xlat16_2;
    f_u_xlat4 = (-_ClipRect.xy) + _ClipRect.zw;
    f_u_xlat4 += (-abs(f_vs_TEXCOORD2.xy));
    f_u_xlat4 *= f_vs_TEXCOORD2.zw;
    f_u_xlat4 = clamp(f_u_xlat4, 0.0f.xx, 1.0f.xx);
    f_u_xlat16_1 = f_u_xlat4.y * f_u_xlat4.x;
    f_u_xlat16_0 = f_u_xlat0 * f_u_xlat16_1.xxxx;
    f_SV_Target0 = f_u_xlat16_0 * f_vs_COLOR0.wwww;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD5 = stage_input.f_vs_TEXCOORD5;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_vs_COLOR1 = stage_input.f_vs_COLOR1;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(UNDERLAY_ON) && defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float4x4 _EnvMatrix;
float _VertexOffsetX;
float _VertexOffsetY;
float3 _WorldSpaceCameraPos;
int _UIVertexColorAlwaysGammaSpace;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineWidth;
float _OutlineSoftness;
float4 _ClipRect;
float _MaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float4 _FaceTex_ST;
float4 _OutlineTex_ST;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _OutlineUVSpeedX;
float _OutlineUVSpeedY;
float4 _Time;
Texture2D<float4> _OutlineTex;
SamplerState sampler_OutlineTex;
float4 _OutlineColor;
float4 _FaceColor;
float _FaceUVSpeedX;
float _FaceUVSpeedY;
Texture2D<float4> _FaceTex;
SamplerState sampler_FaceTex;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;
static float4 v_vs_TEXCOORD5;
static float2 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float2 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD2 : TEXCOORD3;
    float4 v_vs_TEXCOORD5 : TEXCOORD4;
    float4 v_gl_Position : SV_Position;
};

static float3 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float4 v_u_xlat3;
static float3 v_u_xlat16_4;
static float3 v_u_xlat16_5;
static bool3 v_u_xlatb2;
static float2 v_u_xlat14;
static float v_u_xlat22;
static float3 v_u_xlat9;
static float v_u_xlat16;
static float3 v_u_xlat6;
static bool v_u_xlatb22;
static float3 v_vs_TEXCOORD3;

void v_vert_main()
{
    float2 _22 = v_in_POSITION0.xy + float2(_VertexOffsetX, _VertexOffsetY);
    v_u_xlat0.x = _22.x;
    v_u_xlat0.y = _22.y;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + transpose(unity_ObjectToWorld)[3];
    float4 _73 = v_u_xlat1;
    float3 _75 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + _73.xyz;
    v_u_xlat1.x = _75.x;
    v_u_xlat1.y = _75.y;
    v_u_xlat1.z = _75.z;
    float4 _83 = v_u_xlat1;
    float3 _89 = (-_83.xyz) + _WorldSpaceCameraPos;
    v_u_xlat1.x = _89.x;
    v_u_xlat1.y = _89.y;
    v_u_xlat1.z = _89.z;
    v_u_xlat3 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat3 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat3;
    v_u_xlat3 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat3;
    v_u_xlat2 = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat3;
    v_gl_Position = v_u_xlat2;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * 0.26588499546051025390625f.xxx) + 0.736584007740020751953125f.xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + (-0.00980184040963649749755859375f).xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + 0.0031969700939953327178955078125f.xxx;
    v_u_xlat16_5 = (v_in_COLOR0.xyz * 0.084971003234386444091796875f.xxx) + (-0.0001630290062166750431060791015625f).xxx;
    v_u_xlatb2 = bool4(v_in_COLOR0.xyzx.x < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).x, v_in_COLOR0.xyzx.y < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).y, v_in_COLOR0.xyzx.z < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).z, v_in_COLOR0.xyzx.w < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).w).xyz;
    float3 hlslcc_movcTemp = v_u_xlat16_4;
    float _182;
    if (v_u_xlatb2.x)
    {
        _182 = v_u_xlat16_5.x;
    }
    else
    {
        _182 = v_u_xlat16_4.x;
    }
    hlslcc_movcTemp.x = _182;
    float _194;
    if (v_u_xlatb2.y)
    {
        _194 = v_u_xlat16_5.y;
    }
    else
    {
        _194 = v_u_xlat16_4.y;
    }
    hlslcc_movcTemp.y = _194;
    float _206;
    if (v_u_xlatb2.z)
    {
        _206 = v_u_xlat16_5.z;
    }
    else
    {
        _206 = v_u_xlat16_4.z;
    }
    hlslcc_movcTemp.z = _206;
    v_u_xlat16_4 = hlslcc_movcTemp;
    float3 _222;
    if (_UIVertexColorAlwaysGammaSpace != 0)
    {
        _222 = v_u_xlat16_4;
    }
    else
    {
        _222 = v_in_COLOR0.xyz;
    }
    v_vs_COLOR0.x = _222.x;
    v_vs_COLOR0.y = _222.y;
    v_vs_COLOR0.z = _222.z;
    v_vs_COLOR0.w = v_in_COLOR0.w;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xy;
    v_u_xlat14 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat14 = (transpose(glstate_matrix_projection)[0].xy * _ScreenParams.xx) + v_u_xlat14;
    v_u_xlat14 = abs(v_u_xlat14) * float2(_ScaleX, _ScaleY);
    v_u_xlat14 = v_u_xlat2.ww / v_u_xlat14;
    v_u_xlat22 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat22 = rsqrt(v_u_xlat22);
    v_u_xlat2.x = abs(v_in_TEXCOORD0.w) * _GradientScale;
    v_u_xlat9.x = _Sharpness + 1.0f;
    v_u_xlat2.x = v_u_xlat9.x * v_u_xlat2.x;
    v_u_xlat9.x = v_u_xlat22 * v_u_xlat2.x;
    v_u_xlat16 = (-_PerspectiveFilter) + 1.0f;
    v_u_xlat16 *= abs(v_u_xlat9.x);
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + (-v_u_xlat16);
    v_u_xlat3.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat3.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat3.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat3.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    float4 _357 = v_u_xlat3;
    float3 _359 = v_u_xlat2.xxx * _357.xyz;
    v_u_xlat3.x = _359.x;
    v_u_xlat3.y = _359.y;
    v_u_xlat3.z = _359.z;
    v_u_xlat2.x = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    v_u_xlat6 = v_u_xlat1.xyz * v_u_xlat2.xxx;
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat6);
    v_u_xlat22 = (abs(v_u_xlat2.x) * v_u_xlat22) + v_u_xlat16;
    v_u_xlatb2.x = transpose(glstate_matrix_projection)[3].w == 0.0f;
    float _400;
    if (v_u_xlatb2.x)
    {
        _400 = v_u_xlat22;
    }
    else
    {
        _400 = v_u_xlat9.x;
    }
    v_u_xlat9.x = _400;
    v_u_xlatb22 = 0.0f >= v_in_TEXCOORD0.w;
    v_u_xlat22 = float(v_u_xlatb22);
    v_u_xlat2.x = (-_WeightNormal) + _WeightBold;
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + _WeightNormal;
    v_u_xlat22 = (v_u_xlat22 * 0.25f) + _FaceDilate;
    v_u_xlat22 *= _ScaleRatioA;
    v_u_xlat9.z = v_u_xlat22 * 0.5f;
    v_vs_TEXCOORD1.y = v_u_xlat9.xz.x;
    v_vs_TEXCOORD1.w = v_u_xlat9.xz.y;
    v_u_xlat2.x = 0.5f / v_u_xlat9.x;
    v_u_xlat9.x = ((-_OutlineWidth) * _ScaleRatioA) + 1.0f;
    v_u_xlat9.x = ((-_OutlineSoftness) * _ScaleRatioA) + v_u_xlat9.x;
    v_u_xlat9.x = (v_u_xlat9.x * 0.5f) + (-v_u_xlat2.x);
    v_vs_TEXCOORD1.x = ((-v_u_xlat22) * 0.5f) + v_u_xlat9.x;
    v_u_xlat22 = ((-v_u_xlat22) * 0.5f) + 0.5f;
    v_vs_TEXCOORD1.z = v_u_xlat2.x + v_u_xlat22;
    v_u_xlat2 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat2 = min(v_u_xlat2, 20000000000.0f.xxxx);
    float3 _502 = v_u_xlat0;
    float2 _510 = (_502.xy * 2.0f.xx) + (-v_u_xlat2.xy);
    v_u_xlat0.x = _510.x;
    v_u_xlat0.y = _510.y;
    float2 _521 = (-v_u_xlat2.zw) + v_u_xlat0.xy;
    v_vs_TEXCOORD2.x = _521.x;
    v_vs_TEXCOORD2.y = _521.y;
    float2 _536 = max(float2(_MaskSoftnessX, _MaskSoftnessY), float2(_UIMaskSoftnessX, _UIMaskSoftnessY));
    v_u_xlat0.x = _536.x;
    v_u_xlat0.y = _536.y;
    float3 _541 = v_u_xlat0;
    float2 _546 = (_541.xy * 0.25f.xx) + v_u_xlat14;
    v_u_xlat0.x = _546.x;
    v_u_xlat0.y = _546.y;
    float2 _553 = 0.25f.xx / v_u_xlat0.xy;
    v_vs_TEXCOORD2.z = _553.x;
    v_vs_TEXCOORD2.w = _553.y;
    v_u_xlat0 = v_u_xlat1.yyy * transpose(_EnvMatrix)[1].xyz;
    v_u_xlat0 = (transpose(_EnvMatrix)[0].xyz * v_u_xlat1.xxx) + v_u_xlat0;
    v_vs_TEXCOORD3 = (transpose(_EnvMatrix)[2].xyz * v_u_xlat1.zzz) + v_u_xlat0;
    float2 _592 = (v_in_TEXCOORD1 * _FaceTex_ST.xy) + _FaceTex_ST.zw;
    v_vs_TEXCOORD5.x = _592.x;
    v_vs_TEXCOORD5.y = _592.y;
    float2 _604 = (v_in_TEXCOORD1 * _OutlineTex_ST.xy) + _OutlineTex_ST.zw;
    v_vs_TEXCOORD5.z = _604.x;
    v_vs_TEXCOORD5.w = _604.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD5 = v_vs_TEXCOORD5;
    return stage_output;
}
static float2 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_TEXCOORD5;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float2 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD2 : TEXCOORD3;
    float4 f_vs_TEXCOORD5 : TEXCOORD4;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat5;
static float3 f_u_xlat0;
static bool f_u_xlatb5;
static float f_u_xlat16_1;
static float f_u_xlat16_6;
static float f_u_xlat16_11;
static float4 f_u_xlat2;
static float3 f_u_xlat16_3;
static float4 f_u_xlat16_4;
static float2 f_u_xlat4;
static float4 f_u_xlat16_2;
static float f_u_xlat9;
static bool f_u_xlatb4;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).w;
    f_u_xlat5 = f_u_xlat16_0.x + (-f_vs_TEXCOORD1.x);
    f_u_xlat0.x = (-f_u_xlat16_0.x) + f_vs_TEXCOORD1.z;
    f_u_xlatb5 = f_u_xlat5 < 0.0f;
    if (f_u_xlatb5)
    {
        discard;
    }
    f_u_xlat5 = _OutlineWidth * _ScaleRatioA;
    f_u_xlat5 *= f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = min(f_u_xlat5, 1.0f);
    f_u_xlat16_6 = f_u_xlat5 * 0.5f;
    f_u_xlat16_1 = sqrt(f_u_xlat16_1);
    f_u_xlat16_11 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + f_u_xlat16_6;
    f_u_xlat16_11 = clamp(f_u_xlat16_11, 0.0f, 1.0f);
    f_u_xlat16_6 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + (-f_u_xlat16_6);
    f_u_xlat16_1 *= f_u_xlat16_11;
    float2 _112 = (float2(_OutlineUVSpeedX, _OutlineUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.zw;
    f_u_xlat0.x = _112.x;
    f_u_xlat0.y = _112.y;
    f_u_xlat16_0 = _OutlineTex.Sample(sampler_OutlineTex, f_u_xlat0.xy);
    f_u_xlat2 = f_u_xlat16_0 * _OutlineColor;
    f_u_xlat16_3 = f_vs_COLOR0.xyz * _FaceColor.xyz;
    float2 _145 = (float2(_FaceUVSpeedX, _FaceUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.xy;
    f_u_xlat0.x = _145.x;
    f_u_xlat0.y = _145.y;
    f_u_xlat16_4 = _FaceTex.Sample(sampler_FaceTex, f_u_xlat0.xy);
    f_u_xlat0 = f_u_xlat16_3 * f_u_xlat16_4.xyz;
    f_u_xlat4.x = f_u_xlat16_4.w * _FaceColor.w;
    f_u_xlat16_3 = f_u_xlat0 * f_u_xlat4.xxx;
    float3 _180 = (f_u_xlat2.xyz * f_u_xlat2.www) + (-f_u_xlat16_3);
    f_u_xlat16_2.x = _180.x;
    f_u_xlat16_2.y = _180.y;
    f_u_xlat16_2.z = _180.z;
    f_u_xlat16_2.w = (_OutlineColor.w * f_u_xlat16_0.w) + (-f_u_xlat4.x);
    f_u_xlat16_2 = f_u_xlat16_1.xxxx * f_u_xlat16_2;
    float3 _207 = (f_u_xlat0 * f_u_xlat4.xxx) + f_u_xlat16_2.xyz;
    f_u_xlat16_0.x = _207.x;
    f_u_xlat16_0.y = _207.y;
    f_u_xlat16_0.z = _207.z;
    f_u_xlat16_0.w = (_FaceColor.w * f_u_xlat16_4.w) + f_u_xlat16_2.w;
    f_u_xlat4.x = _OutlineSoftness * _ScaleRatioA;
    f_u_xlat9 = f_u_xlat4.x * f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = (f_u_xlat4.x * f_vs_TEXCOORD1.y) + 1.0f;
    f_u_xlat16_6 = (f_u_xlat9 * 0.5f) + f_u_xlat16_6;
    f_u_xlat16_1 = f_u_xlat16_6 / f_u_xlat16_1;
    f_u_xlat16_1 = clamp(f_u_xlat16_1, 0.0f, 1.0f);
    f_u_xlat16_1 = (-f_u_xlat16_1) + 1.0f;
    f_u_xlat16_0 *= f_u_xlat16_1.xxxx;
    f_u_xlat4 = (-_ClipRect.xy) + _ClipRect.zw;
    f_u_xlat4 += (-abs(f_vs_TEXCOORD2.xy));
    f_u_xlat4 *= f_vs_TEXCOORD2.zw;
    f_u_xlat4 = clamp(f_u_xlat4, 0.0f.xx, 1.0f.xx);
    f_u_xlat16_1 = f_u_xlat4.y * f_u_xlat4.x;
    f_u_xlat16_6 = (f_u_xlat16_0.w * f_u_xlat16_1) + (-0.001000000047497451305389404296875f);
    f_u_xlat16_0 *= f_u_xlat16_1.xxxx;
    f_SV_Target0 = f_u_xlat16_0 * f_vs_COLOR0.wwww;
    f_u_xlatb4 = f_u_xlat16_6 < 0.0f;
    if (f_u_xlatb4)
    {
        discard;
    }
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD5 = stage_input.f_vs_TEXCOORD5;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(UNDERLAY_ON) && !defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float4x4 _EnvMatrix;
float _VertexOffsetX;
float _VertexOffsetY;
float3 _WorldSpaceCameraPos;
int _UIVertexColorAlwaysGammaSpace;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineWidth;
float _OutlineSoftness;
float4 _ClipRect;
float _MaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float _UnderlaySoftness;
float _UnderlayDilate;
float _UnderlayOffsetX;
float _UnderlayOffsetY;
float _ScaleRatioC;
float _TextureWidth;
float _TextureHeight;
float4 _UnderlayColor;
float4 _FaceTex_ST;
float4 _OutlineTex_ST;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _OutlineUVSpeedX;
float _OutlineUVSpeedY;
float4 _Time;
Texture2D<float4> _OutlineTex;
SamplerState sampler_OutlineTex;
float4 _OutlineColor;
float4 _FaceColor;
float _FaceUVSpeedX;
float _FaceUVSpeedY;
Texture2D<float4> _FaceTex;
SamplerState sampler_FaceTex;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD4;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD5;
static float2 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float2 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD4 : TEXCOORD3;
    float4 v_vs_COLOR1 : TEXCOORD4;
    float4 v_vs_TEXCOORD5 : TEXCOORD5;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float4 v_u_xlat3;
static float3 v_u_xlat16_4;
static float3 v_u_xlat16_5;
static bool3 v_u_xlatb2;
static float2 v_u_xlat14;
static float v_u_xlat22;
static float3 v_u_xlat9;
static float v_u_xlat16;
static float3 v_u_xlat6;
static bool v_u_xlatb22;
static float4 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;
static float v_u_xlat7;

void v_vert_main()
{
    float2 _21 = v_in_POSITION0.xy + float2(_VertexOffsetX, _VertexOffsetY);
    v_u_xlat0.x = _21.x;
    v_u_xlat0.y = _21.y;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + transpose(unity_ObjectToWorld)[3];
    float4 _72 = v_u_xlat1;
    float3 _74 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + _72.xyz;
    v_u_xlat1.x = _74.x;
    v_u_xlat1.y = _74.y;
    v_u_xlat1.z = _74.z;
    float4 _82 = v_u_xlat1;
    float3 _88 = (-_82.xyz) + _WorldSpaceCameraPos;
    v_u_xlat1.x = _88.x;
    v_u_xlat1.y = _88.y;
    v_u_xlat1.z = _88.z;
    v_u_xlat3 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat3 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat3;
    v_u_xlat3 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat3;
    v_u_xlat2 = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat3;
    v_gl_Position = v_u_xlat2;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * 0.26588499546051025390625f.xxx) + 0.736584007740020751953125f.xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + (-0.00980184040963649749755859375f).xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + 0.0031969700939953327178955078125f.xxx;
    v_u_xlat16_5 = (v_in_COLOR0.xyz * 0.084971003234386444091796875f.xxx) + (-0.0001630290062166750431060791015625f).xxx;
    v_u_xlatb2 = bool4(v_in_COLOR0.xyzx.x < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).x, v_in_COLOR0.xyzx.y < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).y, v_in_COLOR0.xyzx.z < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).z, v_in_COLOR0.xyzx.w < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).w).xyz;
    float3 hlslcc_movcTemp = v_u_xlat16_4;
    float _182;
    if (v_u_xlatb2.x)
    {
        _182 = v_u_xlat16_5.x;
    }
    else
    {
        _182 = v_u_xlat16_4.x;
    }
    hlslcc_movcTemp.x = _182;
    float _194;
    if (v_u_xlatb2.y)
    {
        _194 = v_u_xlat16_5.y;
    }
    else
    {
        _194 = v_u_xlat16_4.y;
    }
    hlslcc_movcTemp.y = _194;
    float _206;
    if (v_u_xlatb2.z)
    {
        _206 = v_u_xlat16_5.z;
    }
    else
    {
        _206 = v_u_xlat16_4.z;
    }
    hlslcc_movcTemp.z = _206;
    v_u_xlat16_4 = hlslcc_movcTemp;
    float3 _222;
    if (_UIVertexColorAlwaysGammaSpace != 0)
    {
        _222 = v_u_xlat16_4;
    }
    else
    {
        _222 = v_in_COLOR0.xyz;
    }
    v_vs_COLOR0.x = _222.x;
    v_vs_COLOR0.y = _222.y;
    v_vs_COLOR0.z = _222.z;
    v_vs_COLOR0.w = v_in_COLOR0.w;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xy;
    v_u_xlat14 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat14 = (transpose(glstate_matrix_projection)[0].xy * _ScreenParams.xx) + v_u_xlat14;
    v_u_xlat14 = abs(v_u_xlat14) * float2(_ScaleX, _ScaleY);
    v_u_xlat14 = v_u_xlat2.ww / v_u_xlat14;
    v_u_xlat22 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat22 = rsqrt(v_u_xlat22);
    v_u_xlat2.x = abs(v_in_TEXCOORD0.w) * _GradientScale;
    v_u_xlat9.x = _Sharpness + 1.0f;
    v_u_xlat2.x = v_u_xlat9.x * v_u_xlat2.x;
    v_u_xlat9.x = v_u_xlat22 * v_u_xlat2.x;
    v_u_xlat16 = (-_PerspectiveFilter) + 1.0f;
    v_u_xlat16 *= abs(v_u_xlat9.x);
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + (-v_u_xlat16);
    v_u_xlat3.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat3.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat3.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat3.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    float4 _357 = v_u_xlat3;
    float3 _359 = v_u_xlat2.xxx * _357.xyz;
    v_u_xlat3.x = _359.x;
    v_u_xlat3.y = _359.y;
    v_u_xlat3.z = _359.z;
    v_u_xlat2.x = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    v_u_xlat6 = v_u_xlat1.xyz * v_u_xlat2.xxx;
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat6);
    v_u_xlat22 = (abs(v_u_xlat2.x) * v_u_xlat22) + v_u_xlat16;
    v_u_xlatb2.x = transpose(glstate_matrix_projection)[3].w == 0.0f;
    float _400;
    if (v_u_xlatb2.x)
    {
        _400 = v_u_xlat22;
    }
    else
    {
        _400 = v_u_xlat9.x;
    }
    v_u_xlat9.x = _400;
    v_u_xlatb22 = 0.0f >= v_in_TEXCOORD0.w;
    v_u_xlat22 = float(v_u_xlatb22);
    v_u_xlat2.x = (-_WeightNormal) + _WeightBold;
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + _WeightNormal;
    v_u_xlat22 = (v_u_xlat22 * 0.25f) + _FaceDilate;
    v_u_xlat22 *= _ScaleRatioA;
    v_u_xlat9.z = v_u_xlat22 * 0.5f;
    v_vs_TEXCOORD1.y = v_u_xlat9.xz.x;
    v_vs_TEXCOORD1.w = v_u_xlat9.xz.y;
    v_u_xlat2.x = 0.5f / v_u_xlat9.x;
    v_u_xlat16 = ((-_OutlineWidth) * _ScaleRatioA) + 1.0f;
    v_u_xlat16 = ((-_OutlineSoftness) * _ScaleRatioA) + v_u_xlat16;
    v_u_xlat16 = (v_u_xlat16 * 0.5f) + (-v_u_xlat2.x);
    v_vs_TEXCOORD1.x = ((-v_u_xlat22) * 0.5f) + v_u_xlat16;
    v_u_xlat22 = ((-v_u_xlat22) * 0.5f) + 0.5f;
    v_vs_TEXCOORD1.z = v_u_xlat2.x + v_u_xlat22;
    v_u_xlat3 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat3 = min(v_u_xlat3, 20000000000.0f.xxxx);
    float4 _496 = v_u_xlat0;
    float2 _504 = (_496.xy * 2.0f.xx) + (-v_u_xlat3.xy);
    v_u_xlat0.x = _504.x;
    v_u_xlat0.y = _504.y;
    float2 _515 = (-v_u_xlat3.zw) + v_u_xlat0.xy;
    v_vs_TEXCOORD2.x = _515.x;
    v_vs_TEXCOORD2.y = _515.y;
    float2 _530 = max(float2(_MaskSoftnessX, _MaskSoftnessY), float2(_UIMaskSoftnessX, _UIMaskSoftnessY));
    v_u_xlat0.x = _530.x;
    v_u_xlat0.y = _530.y;
    float4 _535 = v_u_xlat0;
    float2 _540 = (_535.xy * 0.25f.xx) + v_u_xlat14;
    v_u_xlat0.x = _540.x;
    v_u_xlat0.y = _540.y;
    float2 _547 = 0.25f.xx / v_u_xlat0.xy;
    v_vs_TEXCOORD2.z = _547.x;
    v_vs_TEXCOORD2.w = _547.y;
    float3 _558 = v_u_xlat1.yyy * transpose(_EnvMatrix)[1].xyz;
    v_u_xlat0.x = _558.x;
    v_u_xlat0.y = _558.y;
    v_u_xlat0.z = _558.z;
    float4 _571 = v_u_xlat0;
    float3 _573 = (transpose(_EnvMatrix)[0].xyz * v_u_xlat1.xxx) + _571.xyz;
    v_u_xlat0.x = _573.x;
    v_u_xlat0.y = _573.y;
    v_u_xlat0.z = _573.z;
    v_vs_TEXCOORD3 = (transpose(_EnvMatrix)[2].xyz * v_u_xlat1.zzz) + v_u_xlat0.xyz;
    v_u_xlat0 = float4(_UnderlaySoftness, _UnderlayDilate, _UnderlayOffsetX, _UnderlayOffsetY) * _ScaleRatioC.xxxx;
    v_u_xlat0.x = (v_u_xlat0.x * v_u_xlat9.x) + 1.0f;
    v_u_xlat0.x = v_u_xlat9.x / v_u_xlat0.x;
    v_u_xlat1.x = (v_u_xlat22 * v_u_xlat0.x) + (-0.5f);
    v_u_xlat7 = v_u_xlat0.x * v_u_xlat0.y;
    v_u_xlat14 = (-v_u_xlat0.zw) * _GradientScale.xx;
    v_u_xlat14 /= float2(_TextureWidth, _TextureHeight);
    float2 _646 = v_u_xlat14 + v_in_TEXCOORD0.xy;
    v_vs_TEXCOORD4.x = _646.x;
    v_vs_TEXCOORD4.y = _646.y;
    v_vs_TEXCOORD4.z = v_u_xlat0.x;
    v_vs_TEXCOORD4.w = ((-v_u_xlat7) * 0.5f) + v_u_xlat1.x;
    float3 _666 = _UnderlayColor.www * _UnderlayColor.xyz;
    v_u_xlat0.x = _666.x;
    v_u_xlat0.y = _666.y;
    v_u_xlat0.z = _666.z;
    v_vs_COLOR1.x = v_u_xlat0.xyz.x;
    v_vs_COLOR1.y = v_u_xlat0.xyz.y;
    v_vs_COLOR1.z = v_u_xlat0.xyz.z;
    v_vs_COLOR1.w = _UnderlayColor.w;
    float2 _695 = (v_in_TEXCOORD1 * _FaceTex_ST.xy) + _FaceTex_ST.zw;
    v_vs_TEXCOORD5.x = _695.x;
    v_vs_TEXCOORD5.y = _695.y;
    float2 _707 = (v_in_TEXCOORD1 * _OutlineTex_ST.xy) + _OutlineTex_ST.zw;
    v_vs_TEXCOORD5.z = _707.x;
    v_vs_TEXCOORD5.w = _707.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    stage_output.v_vs_COLOR1 = v_vs_COLOR1;
    stage_output.v_vs_TEXCOORD5 = v_vs_TEXCOORD5;
    return stage_output;
}
static float4 f_vs_TEXCOORD1;
static float2 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD5;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD4;
static float4 f_vs_COLOR1;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float2 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD4 : TEXCOORD3;
    float4 f_vs_COLOR1 : TEXCOORD4;
    float4 f_vs_TEXCOORD5 : TEXCOORD5;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat0;
static float f_u_xlat16_1;
static float f_u_xlat16_6;
static float4 f_u_xlat16_0;
static float f_u_xlat16_11;
static float4 f_u_xlat2;
static float3 f_u_xlat16_3;
static float4 f_u_xlat16_4;
static float f_u_xlat4;
static float4 f_u_xlat16_2;
static float f_u_xlat9;

void f_frag_main()
{
    f_u_xlat0.x = _OutlineWidth * _ScaleRatioA;
    f_u_xlat0.x *= f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = min(f_u_xlat0.x, 1.0f);
    f_u_xlat16_6 = f_u_xlat0.x * 0.5f;
    f_u_xlat16_1 = sqrt(f_u_xlat16_1);
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).w;
    f_u_xlat0.x = (-f_u_xlat16_0.x) + f_vs_TEXCOORD1.z;
    f_u_xlat16_11 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + f_u_xlat16_6;
    f_u_xlat16_11 = clamp(f_u_xlat16_11, 0.0f, 1.0f);
    f_u_xlat16_6 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + (-f_u_xlat16_6);
    f_u_xlat16_1 *= f_u_xlat16_11;
    float2 _99 = (float2(_OutlineUVSpeedX, _OutlineUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.zw;
    f_u_xlat0.x = _99.x;
    f_u_xlat0.y = _99.y;
    f_u_xlat16_0 = _OutlineTex.Sample(sampler_OutlineTex, f_u_xlat0.xy);
    f_u_xlat2 = f_u_xlat16_0 * _OutlineColor;
    f_u_xlat16_3 = f_vs_COLOR0.xyz * _FaceColor.xyz;
    float2 _134 = (float2(_FaceUVSpeedX, _FaceUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.xy;
    f_u_xlat0.x = _134.x;
    f_u_xlat0.y = _134.y;
    f_u_xlat16_4 = _FaceTex.Sample(sampler_FaceTex, f_u_xlat0.xy);
    float3 _148 = f_u_xlat16_3 * f_u_xlat16_4.xyz;
    f_u_xlat0.x = _148.x;
    f_u_xlat0.y = _148.y;
    f_u_xlat0.z = _148.z;
    f_u_xlat4 = f_u_xlat16_4.w * _FaceColor.w;
    f_u_xlat16_3 = f_u_xlat0.xyz * f_u_xlat4.xxx;
    float3 _174 = (f_u_xlat2.xyz * f_u_xlat2.www) + (-f_u_xlat16_3);
    f_u_xlat16_2.x = _174.x;
    f_u_xlat16_2.y = _174.y;
    f_u_xlat16_2.z = _174.z;
    f_u_xlat16_2.w = (_OutlineColor.w * f_u_xlat16_0.w) + (-f_u_xlat4);
    f_u_xlat16_2 = f_u_xlat16_1.xxxx * f_u_xlat16_2;
    float3 _201 = (f_u_xlat0.xyz * f_u_xlat4.xxx) + f_u_xlat16_2.xyz;
    f_u_xlat16_0.x = _201.x;
    f_u_xlat16_0.y = _201.y;
    f_u_xlat16_0.z = _201.z;
    f_u_xlat16_0.w = (_FaceColor.w * f_u_xlat16_4.w) + f_u_xlat16_2.w;
    f_u_xlat4 = _OutlineSoftness * _ScaleRatioA;
    f_u_xlat9 = f_u_xlat4 * f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = (f_u_xlat4 * f_vs_TEXCOORD1.y) + 1.0f;
    f_u_xlat16_6 = (f_u_xlat9 * 0.5f) + f_u_xlat16_6;
    f_u_xlat16_1 = f_u_xlat16_6 / f_u_xlat16_1;
    f_u_xlat16_1 = clamp(f_u_xlat16_1, 0.0f, 1.0f);
    f_u_xlat16_1 = (-f_u_xlat16_1) + 1.0f;
    f_u_xlat16_2 = f_u_xlat16_0 * f_u_xlat16_1.xxxx;
    f_u_xlat16_1 = ((-f_u_xlat16_0.w) * f_u_xlat16_1) + 1.0f;
    f_u_xlat16_4.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD4.xy).w;
    f_u_xlat4 = (f_u_xlat16_4.x * f_vs_TEXCOORD4.z) + (-f_vs_TEXCOORD4.w);
    f_u_xlat4 = clamp(f_u_xlat4, 0.0f, 1.0f);
    f_u_xlat0 = f_u_xlat4.xxxx * f_vs_COLOR1;
    f_u_xlat0 = (f_u_xlat0 * f_u_xlat16_1.xxxx) + f_u_xlat16_2;
    f_SV_Target0 = f_u_xlat0 * f_vs_COLOR0.wwww;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD5 = stage_input.f_vs_TEXCOORD5;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_vs_COLOR1 = stage_input.f_vs_COLOR1;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(UNDERLAY_ON) && defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float4x4 _EnvMatrix;
float _VertexOffsetX;
float _VertexOffsetY;
float3 _WorldSpaceCameraPos;
int _UIVertexColorAlwaysGammaSpace;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineWidth;
float _OutlineSoftness;
float4 _ClipRect;
float _MaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float4 _FaceTex_ST;
float4 _OutlineTex_ST;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _OutlineUVSpeedX;
float _OutlineUVSpeedY;
float4 _Time;
Texture2D<float4> _OutlineTex;
SamplerState sampler_OutlineTex;
float4 _OutlineColor;
float4 _FaceColor;
float _FaceUVSpeedX;
float _FaceUVSpeedY;
Texture2D<float4> _FaceTex;
SamplerState sampler_FaceTex;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD5;
static float2 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float2 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD5 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float3 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float4 v_u_xlat3;
static float3 v_u_xlat16_4;
static float3 v_u_xlat16_5;
static bool3 v_u_xlatb2;
static float2 v_u_xlat14;
static float v_u_xlat22;
static float3 v_u_xlat9;
static float v_u_xlat16;
static float3 v_u_xlat6;
static bool v_u_xlatb22;
static float4 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;

void v_vert_main()
{
    float2 _22 = v_in_POSITION0.xy + float2(_VertexOffsetX, _VertexOffsetY);
    v_u_xlat0.x = _22.x;
    v_u_xlat0.y = _22.y;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + transpose(unity_ObjectToWorld)[3];
    float4 _73 = v_u_xlat1;
    float3 _75 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + _73.xyz;
    v_u_xlat1.x = _75.x;
    v_u_xlat1.y = _75.y;
    v_u_xlat1.z = _75.z;
    float4 _83 = v_u_xlat1;
    float3 _89 = (-_83.xyz) + _WorldSpaceCameraPos;
    v_u_xlat1.x = _89.x;
    v_u_xlat1.y = _89.y;
    v_u_xlat1.z = _89.z;
    v_u_xlat3 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat3 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat3;
    v_u_xlat3 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat3;
    v_u_xlat2 = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat3;
    v_gl_Position = v_u_xlat2;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * 0.26588499546051025390625f.xxx) + 0.736584007740020751953125f.xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + (-0.00980184040963649749755859375f).xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + 0.0031969700939953327178955078125f.xxx;
    v_u_xlat16_5 = (v_in_COLOR0.xyz * 0.084971003234386444091796875f.xxx) + (-0.0001630290062166750431060791015625f).xxx;
    v_u_xlatb2 = bool4(v_in_COLOR0.xyzx.x < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).x, v_in_COLOR0.xyzx.y < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).y, v_in_COLOR0.xyzx.z < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).z, v_in_COLOR0.xyzx.w < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).w).xyz;
    float3 hlslcc_movcTemp = v_u_xlat16_4;
    float _182;
    if (v_u_xlatb2.x)
    {
        _182 = v_u_xlat16_5.x;
    }
    else
    {
        _182 = v_u_xlat16_4.x;
    }
    hlslcc_movcTemp.x = _182;
    float _194;
    if (v_u_xlatb2.y)
    {
        _194 = v_u_xlat16_5.y;
    }
    else
    {
        _194 = v_u_xlat16_4.y;
    }
    hlslcc_movcTemp.y = _194;
    float _206;
    if (v_u_xlatb2.z)
    {
        _206 = v_u_xlat16_5.z;
    }
    else
    {
        _206 = v_u_xlat16_4.z;
    }
    hlslcc_movcTemp.z = _206;
    v_u_xlat16_4 = hlslcc_movcTemp;
    float3 _222;
    if (_UIVertexColorAlwaysGammaSpace != 0)
    {
        _222 = v_u_xlat16_4;
    }
    else
    {
        _222 = v_in_COLOR0.xyz;
    }
    v_vs_COLOR0.x = _222.x;
    v_vs_COLOR0.y = _222.y;
    v_vs_COLOR0.z = _222.z;
    v_vs_COLOR0.w = v_in_COLOR0.w;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xy;
    v_u_xlat14 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat14 = (transpose(glstate_matrix_projection)[0].xy * _ScreenParams.xx) + v_u_xlat14;
    v_u_xlat14 = abs(v_u_xlat14) * float2(_ScaleX, _ScaleY);
    v_u_xlat14 = v_u_xlat2.ww / v_u_xlat14;
    v_u_xlat22 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat22 = rsqrt(v_u_xlat22);
    v_u_xlat2.x = abs(v_in_TEXCOORD0.w) * _GradientScale;
    v_u_xlat9.x = _Sharpness + 1.0f;
    v_u_xlat2.x = v_u_xlat9.x * v_u_xlat2.x;
    v_u_xlat9.x = v_u_xlat22 * v_u_xlat2.x;
    v_u_xlat16 = (-_PerspectiveFilter) + 1.0f;
    v_u_xlat16 *= abs(v_u_xlat9.x);
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + (-v_u_xlat16);
    v_u_xlat3.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat3.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat3.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat3.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    float4 _357 = v_u_xlat3;
    float3 _359 = v_u_xlat2.xxx * _357.xyz;
    v_u_xlat3.x = _359.x;
    v_u_xlat3.y = _359.y;
    v_u_xlat3.z = _359.z;
    v_u_xlat2.x = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    v_u_xlat6 = v_u_xlat1.xyz * v_u_xlat2.xxx;
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat6);
    v_u_xlat22 = (abs(v_u_xlat2.x) * v_u_xlat22) + v_u_xlat16;
    v_u_xlatb2.x = transpose(glstate_matrix_projection)[3].w == 0.0f;
    float _400;
    if (v_u_xlatb2.x)
    {
        _400 = v_u_xlat22;
    }
    else
    {
        _400 = v_u_xlat9.x;
    }
    v_u_xlat9.x = _400;
    v_u_xlatb22 = 0.0f >= v_in_TEXCOORD0.w;
    v_u_xlat22 = float(v_u_xlatb22);
    v_u_xlat2.x = (-_WeightNormal) + _WeightBold;
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + _WeightNormal;
    v_u_xlat22 = (v_u_xlat22 * 0.25f) + _FaceDilate;
    v_u_xlat22 *= _ScaleRatioA;
    v_u_xlat9.z = v_u_xlat22 * 0.5f;
    v_vs_TEXCOORD1.y = v_u_xlat9.xz.x;
    v_vs_TEXCOORD1.w = v_u_xlat9.xz.y;
    v_u_xlat2.x = 0.5f / v_u_xlat9.x;
    v_u_xlat9.x = ((-_OutlineWidth) * _ScaleRatioA) + 1.0f;
    v_u_xlat9.x = ((-_OutlineSoftness) * _ScaleRatioA) + v_u_xlat9.x;
    v_u_xlat9.x = (v_u_xlat9.x * 0.5f) + (-v_u_xlat2.x);
    v_vs_TEXCOORD1.x = ((-v_u_xlat22) * 0.5f) + v_u_xlat9.x;
    v_u_xlat22 = ((-v_u_xlat22) * 0.5f) + 0.5f;
    v_vs_TEXCOORD1.z = v_u_xlat2.x + v_u_xlat22;
    v_u_xlat2 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat2 = min(v_u_xlat2, 20000000000.0f.xxxx);
    float3 _502 = v_u_xlat0;
    float2 _510 = (_502.xy * 2.0f.xx) + (-v_u_xlat2.xy);
    v_u_xlat0.x = _510.x;
    v_u_xlat0.y = _510.y;
    float2 _521 = (-v_u_xlat2.zw) + v_u_xlat0.xy;
    v_vs_TEXCOORD2.x = _521.x;
    v_vs_TEXCOORD2.y = _521.y;
    float2 _536 = max(float2(_MaskSoftnessX, _MaskSoftnessY), float2(_UIMaskSoftnessX, _UIMaskSoftnessY));
    v_u_xlat0.x = _536.x;
    v_u_xlat0.y = _536.y;
    float3 _541 = v_u_xlat0;
    float2 _546 = (_541.xy * 0.25f.xx) + v_u_xlat14;
    v_u_xlat0.x = _546.x;
    v_u_xlat0.y = _546.y;
    float2 _553 = 0.25f.xx / v_u_xlat0.xy;
    v_vs_TEXCOORD2.z = _553.x;
    v_vs_TEXCOORD2.w = _553.y;
    v_u_xlat0 = v_u_xlat1.yyy * transpose(_EnvMatrix)[1].xyz;
    v_u_xlat0 = (transpose(_EnvMatrix)[0].xyz * v_u_xlat1.xxx) + v_u_xlat0;
    v_vs_TEXCOORD3 = (transpose(_EnvMatrix)[2].xyz * v_u_xlat1.zzz) + v_u_xlat0;
    float2 _592 = (v_in_TEXCOORD1 * _FaceTex_ST.xy) + _FaceTex_ST.zw;
    v_vs_TEXCOORD5.x = _592.x;
    v_vs_TEXCOORD5.y = _592.y;
    float2 _604 = (v_in_TEXCOORD1 * _OutlineTex_ST.xy) + _OutlineTex_ST.zw;
    v_vs_TEXCOORD5.z = _604.x;
    v_vs_TEXCOORD5.w = _604.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD5 = v_vs_TEXCOORD5;
    return stage_output;
}
static float2 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_TEXCOORD5;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float2 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD5 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat5;
static float3 f_u_xlat0;
static bool f_u_xlatb5;
static float f_u_xlat16_1;
static float f_u_xlat16_6;
static float f_u_xlat16_11;
static float4 f_u_xlat2;
static float4 f_u_xlat16_3;
static float4 f_u_xlat16_4;
static float f_u_xlat4;
static float4 f_u_xlat16_2;
static bool f_u_xlatb4;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).w;
    f_u_xlat5 = f_u_xlat16_0.x + (-f_vs_TEXCOORD1.x);
    f_u_xlat0.x = (-f_u_xlat16_0.x) + f_vs_TEXCOORD1.z;
    f_u_xlatb5 = f_u_xlat5 < 0.0f;
    if (f_u_xlatb5)
    {
        discard;
    }
    f_u_xlat5 = _OutlineWidth * _ScaleRatioA;
    f_u_xlat5 *= f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = min(f_u_xlat5, 1.0f);
    f_u_xlat16_6 = f_u_xlat5 * 0.5f;
    f_u_xlat16_1 = sqrt(f_u_xlat16_1);
    f_u_xlat16_11 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + f_u_xlat16_6;
    f_u_xlat16_11 = clamp(f_u_xlat16_11, 0.0f, 1.0f);
    f_u_xlat16_6 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + (-f_u_xlat16_6);
    f_u_xlat16_1 *= f_u_xlat16_11;
    float2 _112 = (float2(_OutlineUVSpeedX, _OutlineUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.zw;
    f_u_xlat0.x = _112.x;
    f_u_xlat0.y = _112.y;
    f_u_xlat16_0 = _OutlineTex.Sample(sampler_OutlineTex, f_u_xlat0.xy);
    f_u_xlat2 = f_u_xlat16_0 * _OutlineColor;
    float3 _134 = f_vs_COLOR0.xyz * _FaceColor.xyz;
    f_u_xlat16_3.x = _134.x;
    f_u_xlat16_3.y = _134.y;
    f_u_xlat16_3.z = _134.z;
    float2 _151 = (float2(_FaceUVSpeedX, _FaceUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.xy;
    f_u_xlat0.x = _151.x;
    f_u_xlat0.y = _151.y;
    f_u_xlat16_4 = _FaceTex.Sample(sampler_FaceTex, f_u_xlat0.xy);
    f_u_xlat0 = f_u_xlat16_3.xyz * f_u_xlat16_4.xyz;
    f_u_xlat4 = f_u_xlat16_4.w * _FaceColor.w;
    float3 _176 = f_u_xlat0 * f_u_xlat4.xxx;
    f_u_xlat16_3.x = _176.x;
    f_u_xlat16_3.y = _176.y;
    f_u_xlat16_3.z = _176.z;
    float3 _192 = (f_u_xlat2.xyz * f_u_xlat2.www) + (-f_u_xlat16_3.xyz);
    f_u_xlat16_2.x = _192.x;
    f_u_xlat16_2.y = _192.y;
    f_u_xlat16_2.z = _192.z;
    f_u_xlat16_2.w = (_OutlineColor.w * f_u_xlat16_0.w) + (-f_u_xlat4);
    f_u_xlat16_2 = f_u_xlat16_1.xxxx * f_u_xlat16_2;
    f_u_xlat16_3.w = (_FaceColor.w * f_u_xlat16_4.w) + f_u_xlat16_2.w;
    float3 _227 = (f_u_xlat0 * f_u_xlat4.xxx) + f_u_xlat16_2.xyz;
    f_u_xlat16_3.x = _227.x;
    f_u_xlat16_3.y = _227.y;
    f_u_xlat16_3.z = _227.z;
    f_u_xlat0.x = _OutlineSoftness * _ScaleRatioA;
    f_u_xlat5 = f_u_xlat0.x * f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + 1.0f;
    f_u_xlat16_6 = (f_u_xlat5 * 0.5f) + f_u_xlat16_6;
    f_u_xlat16_1 = f_u_xlat16_6 / f_u_xlat16_1;
    f_u_xlat16_1 = clamp(f_u_xlat16_1, 0.0f, 1.0f);
    f_u_xlat16_1 = (-f_u_xlat16_1) + 1.0f;
    f_u_xlat16_6 = (f_u_xlat16_3.w * f_u_xlat16_1) + (-0.001000000047497451305389404296875f);
    f_u_xlat16_0 = f_u_xlat16_1.xxxx * f_u_xlat16_3;
    f_SV_Target0 = f_u_xlat16_0 * f_vs_COLOR0.wwww;
    f_u_xlatb4 = f_u_xlat16_6 < 0.0f;
    if (f_u_xlatb4)
    {
        discard;
    }
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD5 = stage_input.f_vs_TEXCOORD5;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(UNDERLAY_ON) && !defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float4x4 _EnvMatrix;
float _VertexOffsetX;
float _VertexOffsetY;
float3 _WorldSpaceCameraPos;
int _UIVertexColorAlwaysGammaSpace;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineWidth;
float _OutlineSoftness;
float4 _ClipRect;
float _MaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float4 _FaceTex_ST;
float4 _OutlineTex_ST;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _OutlineUVSpeedX;
float _OutlineUVSpeedY;
float4 _Time;
Texture2D<float4> _OutlineTex;
SamplerState sampler_OutlineTex;
float4 _OutlineColor;
float4 _FaceColor;
float _FaceUVSpeedX;
float _FaceUVSpeedY;
Texture2D<float4> _FaceTex;
SamplerState sampler_FaceTex;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;
static float4 v_vs_TEXCOORD5;
static float2 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float2 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD2 : TEXCOORD3;
    float4 v_vs_TEXCOORD5 : TEXCOORD4;
    float4 v_gl_Position : SV_Position;
};

static float3 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float4 v_u_xlat3;
static float3 v_u_xlat16_4;
static float3 v_u_xlat16_5;
static bool3 v_u_xlatb2;
static float2 v_u_xlat14;
static float v_u_xlat22;
static float3 v_u_xlat9;
static float v_u_xlat16;
static float3 v_u_xlat6;
static bool v_u_xlatb22;
static float3 v_vs_TEXCOORD3;

void v_vert_main()
{
    float2 _22 = v_in_POSITION0.xy + float2(_VertexOffsetX, _VertexOffsetY);
    v_u_xlat0.x = _22.x;
    v_u_xlat0.y = _22.y;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + transpose(unity_ObjectToWorld)[3];
    float4 _73 = v_u_xlat1;
    float3 _75 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + _73.xyz;
    v_u_xlat1.x = _75.x;
    v_u_xlat1.y = _75.y;
    v_u_xlat1.z = _75.z;
    float4 _83 = v_u_xlat1;
    float3 _89 = (-_83.xyz) + _WorldSpaceCameraPos;
    v_u_xlat1.x = _89.x;
    v_u_xlat1.y = _89.y;
    v_u_xlat1.z = _89.z;
    v_u_xlat3 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat3 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat3;
    v_u_xlat3 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat3;
    v_u_xlat2 = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat3;
    v_gl_Position = v_u_xlat2;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * 0.26588499546051025390625f.xxx) + 0.736584007740020751953125f.xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + (-0.00980184040963649749755859375f).xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + 0.0031969700939953327178955078125f.xxx;
    v_u_xlat16_5 = (v_in_COLOR0.xyz * 0.084971003234386444091796875f.xxx) + (-0.0001630290062166750431060791015625f).xxx;
    v_u_xlatb2 = bool4(v_in_COLOR0.xyzx.x < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).x, v_in_COLOR0.xyzx.y < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).y, v_in_COLOR0.xyzx.z < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).z, v_in_COLOR0.xyzx.w < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).w).xyz;
    float3 hlslcc_movcTemp = v_u_xlat16_4;
    float _182;
    if (v_u_xlatb2.x)
    {
        _182 = v_u_xlat16_5.x;
    }
    else
    {
        _182 = v_u_xlat16_4.x;
    }
    hlslcc_movcTemp.x = _182;
    float _194;
    if (v_u_xlatb2.y)
    {
        _194 = v_u_xlat16_5.y;
    }
    else
    {
        _194 = v_u_xlat16_4.y;
    }
    hlslcc_movcTemp.y = _194;
    float _206;
    if (v_u_xlatb2.z)
    {
        _206 = v_u_xlat16_5.z;
    }
    else
    {
        _206 = v_u_xlat16_4.z;
    }
    hlslcc_movcTemp.z = _206;
    v_u_xlat16_4 = hlslcc_movcTemp;
    float3 _222;
    if (_UIVertexColorAlwaysGammaSpace != 0)
    {
        _222 = v_u_xlat16_4;
    }
    else
    {
        _222 = v_in_COLOR0.xyz;
    }
    v_vs_COLOR0.x = _222.x;
    v_vs_COLOR0.y = _222.y;
    v_vs_COLOR0.z = _222.z;
    v_vs_COLOR0.w = v_in_COLOR0.w;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xy;
    v_u_xlat14 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat14 = (transpose(glstate_matrix_projection)[0].xy * _ScreenParams.xx) + v_u_xlat14;
    v_u_xlat14 = abs(v_u_xlat14) * float2(_ScaleX, _ScaleY);
    v_u_xlat14 = v_u_xlat2.ww / v_u_xlat14;
    v_u_xlat22 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat22 = rsqrt(v_u_xlat22);
    v_u_xlat2.x = abs(v_in_TEXCOORD0.w) * _GradientScale;
    v_u_xlat9.x = _Sharpness + 1.0f;
    v_u_xlat2.x = v_u_xlat9.x * v_u_xlat2.x;
    v_u_xlat9.x = v_u_xlat22 * v_u_xlat2.x;
    v_u_xlat16 = (-_PerspectiveFilter) + 1.0f;
    v_u_xlat16 *= abs(v_u_xlat9.x);
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + (-v_u_xlat16);
    v_u_xlat3.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat3.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat3.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat3.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    float4 _357 = v_u_xlat3;
    float3 _359 = v_u_xlat2.xxx * _357.xyz;
    v_u_xlat3.x = _359.x;
    v_u_xlat3.y = _359.y;
    v_u_xlat3.z = _359.z;
    v_u_xlat2.x = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    v_u_xlat6 = v_u_xlat1.xyz * v_u_xlat2.xxx;
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat6);
    v_u_xlat22 = (abs(v_u_xlat2.x) * v_u_xlat22) + v_u_xlat16;
    v_u_xlatb2.x = transpose(glstate_matrix_projection)[3].w == 0.0f;
    float _400;
    if (v_u_xlatb2.x)
    {
        _400 = v_u_xlat22;
    }
    else
    {
        _400 = v_u_xlat9.x;
    }
    v_u_xlat9.x = _400;
    v_u_xlatb22 = 0.0f >= v_in_TEXCOORD0.w;
    v_u_xlat22 = float(v_u_xlatb22);
    v_u_xlat2.x = (-_WeightNormal) + _WeightBold;
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + _WeightNormal;
    v_u_xlat22 = (v_u_xlat22 * 0.25f) + _FaceDilate;
    v_u_xlat22 *= _ScaleRatioA;
    v_u_xlat9.z = v_u_xlat22 * 0.5f;
    v_vs_TEXCOORD1.y = v_u_xlat9.xz.x;
    v_vs_TEXCOORD1.w = v_u_xlat9.xz.y;
    v_u_xlat2.x = 0.5f / v_u_xlat9.x;
    v_u_xlat9.x = ((-_OutlineWidth) * _ScaleRatioA) + 1.0f;
    v_u_xlat9.x = ((-_OutlineSoftness) * _ScaleRatioA) + v_u_xlat9.x;
    v_u_xlat9.x = (v_u_xlat9.x * 0.5f) + (-v_u_xlat2.x);
    v_vs_TEXCOORD1.x = ((-v_u_xlat22) * 0.5f) + v_u_xlat9.x;
    v_u_xlat22 = ((-v_u_xlat22) * 0.5f) + 0.5f;
    v_vs_TEXCOORD1.z = v_u_xlat2.x + v_u_xlat22;
    v_u_xlat2 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat2 = min(v_u_xlat2, 20000000000.0f.xxxx);
    float3 _502 = v_u_xlat0;
    float2 _510 = (_502.xy * 2.0f.xx) + (-v_u_xlat2.xy);
    v_u_xlat0.x = _510.x;
    v_u_xlat0.y = _510.y;
    float2 _521 = (-v_u_xlat2.zw) + v_u_xlat0.xy;
    v_vs_TEXCOORD2.x = _521.x;
    v_vs_TEXCOORD2.y = _521.y;
    float2 _536 = max(float2(_MaskSoftnessX, _MaskSoftnessY), float2(_UIMaskSoftnessX, _UIMaskSoftnessY));
    v_u_xlat0.x = _536.x;
    v_u_xlat0.y = _536.y;
    float3 _541 = v_u_xlat0;
    float2 _546 = (_541.xy * 0.25f.xx) + v_u_xlat14;
    v_u_xlat0.x = _546.x;
    v_u_xlat0.y = _546.y;
    float2 _553 = 0.25f.xx / v_u_xlat0.xy;
    v_vs_TEXCOORD2.z = _553.x;
    v_vs_TEXCOORD2.w = _553.y;
    v_u_xlat0 = v_u_xlat1.yyy * transpose(_EnvMatrix)[1].xyz;
    v_u_xlat0 = (transpose(_EnvMatrix)[0].xyz * v_u_xlat1.xxx) + v_u_xlat0;
    v_vs_TEXCOORD3 = (transpose(_EnvMatrix)[2].xyz * v_u_xlat1.zzz) + v_u_xlat0;
    float2 _592 = (v_in_TEXCOORD1 * _FaceTex_ST.xy) + _FaceTex_ST.zw;
    v_vs_TEXCOORD5.x = _592.x;
    v_vs_TEXCOORD5.y = _592.y;
    float2 _604 = (v_in_TEXCOORD1 * _OutlineTex_ST.xy) + _OutlineTex_ST.zw;
    v_vs_TEXCOORD5.z = _604.x;
    v_vs_TEXCOORD5.w = _604.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD5 = v_vs_TEXCOORD5;
    return stage_output;
}
static float2 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_TEXCOORD5;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float2 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD2 : TEXCOORD3;
    float4 f_vs_TEXCOORD5 : TEXCOORD4;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat5;
static float3 f_u_xlat0;
static bool f_u_xlatb5;
static float f_u_xlat16_1;
static float f_u_xlat16_6;
static float f_u_xlat16_11;
static float4 f_u_xlat2;
static float3 f_u_xlat16_3;
static float4 f_u_xlat16_4;
static float2 f_u_xlat4;
static float4 f_u_xlat16_2;
static float f_u_xlat9;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).w;
    f_u_xlat5 = f_u_xlat16_0.x + (-f_vs_TEXCOORD1.x);
    f_u_xlat0.x = (-f_u_xlat16_0.x) + f_vs_TEXCOORD1.z;
    f_u_xlatb5 = f_u_xlat5 < 0.0f;
    if (f_u_xlatb5)
    {
        discard;
    }
    f_u_xlat5 = _OutlineWidth * _ScaleRatioA;
    f_u_xlat5 *= f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = min(f_u_xlat5, 1.0f);
    f_u_xlat16_6 = f_u_xlat5 * 0.5f;
    f_u_xlat16_1 = sqrt(f_u_xlat16_1);
    f_u_xlat16_11 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + f_u_xlat16_6;
    f_u_xlat16_11 = clamp(f_u_xlat16_11, 0.0f, 1.0f);
    f_u_xlat16_6 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + (-f_u_xlat16_6);
    f_u_xlat16_1 *= f_u_xlat16_11;
    float2 _112 = (float2(_OutlineUVSpeedX, _OutlineUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.zw;
    f_u_xlat0.x = _112.x;
    f_u_xlat0.y = _112.y;
    f_u_xlat16_0 = _OutlineTex.Sample(sampler_OutlineTex, f_u_xlat0.xy);
    f_u_xlat2 = f_u_xlat16_0 * _OutlineColor;
    f_u_xlat16_3 = f_vs_COLOR0.xyz * _FaceColor.xyz;
    float2 _145 = (float2(_FaceUVSpeedX, _FaceUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.xy;
    f_u_xlat0.x = _145.x;
    f_u_xlat0.y = _145.y;
    f_u_xlat16_4 = _FaceTex.Sample(sampler_FaceTex, f_u_xlat0.xy);
    f_u_xlat0 = f_u_xlat16_3 * f_u_xlat16_4.xyz;
    f_u_xlat4.x = f_u_xlat16_4.w * _FaceColor.w;
    f_u_xlat16_3 = f_u_xlat0 * f_u_xlat4.xxx;
    float3 _180 = (f_u_xlat2.xyz * f_u_xlat2.www) + (-f_u_xlat16_3);
    f_u_xlat16_2.x = _180.x;
    f_u_xlat16_2.y = _180.y;
    f_u_xlat16_2.z = _180.z;
    f_u_xlat16_2.w = (_OutlineColor.w * f_u_xlat16_0.w) + (-f_u_xlat4.x);
    f_u_xlat16_2 = f_u_xlat16_1.xxxx * f_u_xlat16_2;
    float3 _207 = (f_u_xlat0 * f_u_xlat4.xxx) + f_u_xlat16_2.xyz;
    f_u_xlat16_0.x = _207.x;
    f_u_xlat16_0.y = _207.y;
    f_u_xlat16_0.z = _207.z;
    f_u_xlat16_0.w = (_FaceColor.w * f_u_xlat16_4.w) + f_u_xlat16_2.w;
    f_u_xlat4.x = _OutlineSoftness * _ScaleRatioA;
    f_u_xlat9 = f_u_xlat4.x * f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = (f_u_xlat4.x * f_vs_TEXCOORD1.y) + 1.0f;
    f_u_xlat16_6 = (f_u_xlat9 * 0.5f) + f_u_xlat16_6;
    f_u_xlat16_1 = f_u_xlat16_6 / f_u_xlat16_1;
    f_u_xlat16_1 = clamp(f_u_xlat16_1, 0.0f, 1.0f);
    f_u_xlat16_1 = (-f_u_xlat16_1) + 1.0f;
    f_u_xlat16_0 *= f_u_xlat16_1.xxxx;
    f_u_xlat4 = (-_ClipRect.xy) + _ClipRect.zw;
    f_u_xlat4 += (-abs(f_vs_TEXCOORD2.xy));
    f_u_xlat4 *= f_vs_TEXCOORD2.zw;
    f_u_xlat4 = clamp(f_u_xlat4, 0.0f.xx, 1.0f.xx);
    f_u_xlat16_1 = f_u_xlat4.y * f_u_xlat4.x;
    f_u_xlat16_0 *= f_u_xlat16_1.xxxx;
    f_SV_Target0 = f_u_xlat16_0 * f_vs_COLOR0.wwww;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD5 = stage_input.f_vs_TEXCOORD5;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(UNDERLAY_ON) && !defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float4x4 _EnvMatrix;
float _VertexOffsetX;
float _VertexOffsetY;
float3 _WorldSpaceCameraPos;
int _UIVertexColorAlwaysGammaSpace;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineWidth;
float _OutlineSoftness;
float4 _ClipRect;
float _MaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
float4 _FaceTex_ST;
float4 _OutlineTex_ST;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float _OutlineUVSpeedX;
float _OutlineUVSpeedY;
float4 _Time;
Texture2D<float4> _OutlineTex;
SamplerState sampler_OutlineTex;
float4 _OutlineColor;
float4 _FaceColor;
float _FaceUVSpeedX;
float _FaceUVSpeedY;
Texture2D<float4> _FaceTex;
SamplerState sampler_FaceTex;
static float4 v_gl_Position;
static float4 v_in_POSITION0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float2 v_vs_TEXCOORD0;
static float4 v_in_TEXCOORD0;
static float3 v_in_NORMAL0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD5;
static float2 v_in_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
    float2 v_in_TEXCOORD1 : TEXCOORD1;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float2 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD5 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float3 v_u_xlat0;
static float4 v_u_xlat1;
static float4 v_u_xlat2;
static float4 v_u_xlat3;
static float3 v_u_xlat16_4;
static float3 v_u_xlat16_5;
static bool3 v_u_xlatb2;
static float2 v_u_xlat14;
static float v_u_xlat22;
static float3 v_u_xlat9;
static float v_u_xlat16;
static float3 v_u_xlat6;
static bool v_u_xlatb22;
static float4 v_vs_TEXCOORD2;
static float3 v_vs_TEXCOORD3;

void v_vert_main()
{
    float2 _22 = v_in_POSITION0.xy + float2(_VertexOffsetX, _VertexOffsetY);
    v_u_xlat0.x = _22.x;
    v_u_xlat0.y = _22.y;
    v_u_xlat1 = v_u_xlat0.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[0] * v_u_xlat0.xxxx) + v_u_xlat1;
    v_u_xlat1 = (transpose(unity_ObjectToWorld)[2] * v_in_POSITION0.zzzz) + v_u_xlat1;
    v_u_xlat2 = v_u_xlat1 + transpose(unity_ObjectToWorld)[3];
    float4 _73 = v_u_xlat1;
    float3 _75 = (transpose(unity_ObjectToWorld)[3].xyz * v_in_POSITION0.www) + _73.xyz;
    v_u_xlat1.x = _75.x;
    v_u_xlat1.y = _75.y;
    v_u_xlat1.z = _75.z;
    float4 _83 = v_u_xlat1;
    float3 _89 = (-_83.xyz) + _WorldSpaceCameraPos;
    v_u_xlat1.x = _89.x;
    v_u_xlat1.y = _89.y;
    v_u_xlat1.z = _89.z;
    v_u_xlat3 = v_u_xlat2.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat3 = (transpose(unity_MatrixVP)[0] * v_u_xlat2.xxxx) + v_u_xlat3;
    v_u_xlat3 = (transpose(unity_MatrixVP)[2] * v_u_xlat2.zzzz) + v_u_xlat3;
    v_u_xlat2 = (transpose(unity_MatrixVP)[3] * v_u_xlat2.wwww) + v_u_xlat3;
    v_gl_Position = v_u_xlat2;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * 0.26588499546051025390625f.xxx) + 0.736584007740020751953125f.xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + (-0.00980184040963649749755859375f).xxx;
    v_u_xlat16_4 = (v_in_COLOR0.xyz * v_u_xlat16_4) + 0.0031969700939953327178955078125f.xxx;
    v_u_xlat16_5 = (v_in_COLOR0.xyz * 0.084971003234386444091796875f.xxx) + (-0.0001630290062166750431060791015625f).xxx;
    v_u_xlatb2 = bool4(v_in_COLOR0.xyzx.x < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).x, v_in_COLOR0.xyzx.y < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).y, v_in_COLOR0.xyzx.z < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).z, v_in_COLOR0.xyzx.w < float4(0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.07254900038242340087890625f, 0.0f).w).xyz;
    float3 hlslcc_movcTemp = v_u_xlat16_4;
    float _182;
    if (v_u_xlatb2.x)
    {
        _182 = v_u_xlat16_5.x;
    }
    else
    {
        _182 = v_u_xlat16_4.x;
    }
    hlslcc_movcTemp.x = _182;
    float _194;
    if (v_u_xlatb2.y)
    {
        _194 = v_u_xlat16_5.y;
    }
    else
    {
        _194 = v_u_xlat16_4.y;
    }
    hlslcc_movcTemp.y = _194;
    float _206;
    if (v_u_xlatb2.z)
    {
        _206 = v_u_xlat16_5.z;
    }
    else
    {
        _206 = v_u_xlat16_4.z;
    }
    hlslcc_movcTemp.z = _206;
    v_u_xlat16_4 = hlslcc_movcTemp;
    float3 _222;
    if (_UIVertexColorAlwaysGammaSpace != 0)
    {
        _222 = v_u_xlat16_4;
    }
    else
    {
        _222 = v_in_COLOR0.xyz;
    }
    v_vs_COLOR0.x = _222.x;
    v_vs_COLOR0.y = _222.y;
    v_vs_COLOR0.z = _222.z;
    v_vs_COLOR0.w = v_in_COLOR0.w;
    v_vs_TEXCOORD0 = v_in_TEXCOORD0.xy;
    v_u_xlat14 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat14 = (transpose(glstate_matrix_projection)[0].xy * _ScreenParams.xx) + v_u_xlat14;
    v_u_xlat14 = abs(v_u_xlat14) * float2(_ScaleX, _ScaleY);
    v_u_xlat14 = v_u_xlat2.ww / v_u_xlat14;
    v_u_xlat22 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat22 = rsqrt(v_u_xlat22);
    v_u_xlat2.x = abs(v_in_TEXCOORD0.w) * _GradientScale;
    v_u_xlat9.x = _Sharpness + 1.0f;
    v_u_xlat2.x = v_u_xlat9.x * v_u_xlat2.x;
    v_u_xlat9.x = v_u_xlat22 * v_u_xlat2.x;
    v_u_xlat16 = (-_PerspectiveFilter) + 1.0f;
    v_u_xlat16 *= abs(v_u_xlat9.x);
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + (-v_u_xlat16);
    v_u_xlat3.x = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[0].xyz);
    v_u_xlat3.y = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[1].xyz);
    v_u_xlat3.z = dot(v_in_NORMAL0, transpose(unity_WorldToObject)[2].xyz);
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat3.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    float4 _357 = v_u_xlat3;
    float3 _359 = v_u_xlat2.xxx * _357.xyz;
    v_u_xlat3.x = _359.x;
    v_u_xlat3.y = _359.y;
    v_u_xlat3.z = _359.z;
    v_u_xlat2.x = dot(v_u_xlat1.xyz, v_u_xlat1.xyz);
    v_u_xlat2.x = rsqrt(v_u_xlat2.x);
    v_u_xlat6 = v_u_xlat1.xyz * v_u_xlat2.xxx;
    v_u_xlat2.x = dot(v_u_xlat3.xyz, v_u_xlat6);
    v_u_xlat22 = (abs(v_u_xlat2.x) * v_u_xlat22) + v_u_xlat16;
    v_u_xlatb2.x = transpose(glstate_matrix_projection)[3].w == 0.0f;
    float _400;
    if (v_u_xlatb2.x)
    {
        _400 = v_u_xlat22;
    }
    else
    {
        _400 = v_u_xlat9.x;
    }
    v_u_xlat9.x = _400;
    v_u_xlatb22 = 0.0f >= v_in_TEXCOORD0.w;
    v_u_xlat22 = float(v_u_xlatb22);
    v_u_xlat2.x = (-_WeightNormal) + _WeightBold;
    v_u_xlat22 = (v_u_xlat22 * v_u_xlat2.x) + _WeightNormal;
    v_u_xlat22 = (v_u_xlat22 * 0.25f) + _FaceDilate;
    v_u_xlat22 *= _ScaleRatioA;
    v_u_xlat9.z = v_u_xlat22 * 0.5f;
    v_vs_TEXCOORD1.y = v_u_xlat9.xz.x;
    v_vs_TEXCOORD1.w = v_u_xlat9.xz.y;
    v_u_xlat2.x = 0.5f / v_u_xlat9.x;
    v_u_xlat9.x = ((-_OutlineWidth) * _ScaleRatioA) + 1.0f;
    v_u_xlat9.x = ((-_OutlineSoftness) * _ScaleRatioA) + v_u_xlat9.x;
    v_u_xlat9.x = (v_u_xlat9.x * 0.5f) + (-v_u_xlat2.x);
    v_vs_TEXCOORD1.x = ((-v_u_xlat22) * 0.5f) + v_u_xlat9.x;
    v_u_xlat22 = ((-v_u_xlat22) * 0.5f) + 0.5f;
    v_vs_TEXCOORD1.z = v_u_xlat2.x + v_u_xlat22;
    v_u_xlat2 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat2 = min(v_u_xlat2, 20000000000.0f.xxxx);
    float3 _502 = v_u_xlat0;
    float2 _510 = (_502.xy * 2.0f.xx) + (-v_u_xlat2.xy);
    v_u_xlat0.x = _510.x;
    v_u_xlat0.y = _510.y;
    float2 _521 = (-v_u_xlat2.zw) + v_u_xlat0.xy;
    v_vs_TEXCOORD2.x = _521.x;
    v_vs_TEXCOORD2.y = _521.y;
    float2 _536 = max(float2(_MaskSoftnessX, _MaskSoftnessY), float2(_UIMaskSoftnessX, _UIMaskSoftnessY));
    v_u_xlat0.x = _536.x;
    v_u_xlat0.y = _536.y;
    float3 _541 = v_u_xlat0;
    float2 _546 = (_541.xy * 0.25f.xx) + v_u_xlat14;
    v_u_xlat0.x = _546.x;
    v_u_xlat0.y = _546.y;
    float2 _553 = 0.25f.xx / v_u_xlat0.xy;
    v_vs_TEXCOORD2.z = _553.x;
    v_vs_TEXCOORD2.w = _553.y;
    v_u_xlat0 = v_u_xlat1.yyy * transpose(_EnvMatrix)[1].xyz;
    v_u_xlat0 = (transpose(_EnvMatrix)[0].xyz * v_u_xlat1.xxx) + v_u_xlat0;
    v_vs_TEXCOORD3 = (transpose(_EnvMatrix)[2].xyz * v_u_xlat1.zzz) + v_u_xlat0;
    float2 _592 = (v_in_TEXCOORD1 * _FaceTex_ST.xy) + _FaceTex_ST.zw;
    v_vs_TEXCOORD5.x = _592.x;
    v_vs_TEXCOORD5.y = _592.y;
    float2 _604 = (v_in_TEXCOORD1 * _OutlineTex_ST.xy) + _OutlineTex_ST.zw;
    v_vs_TEXCOORD5.z = _604.x;
    v_vs_TEXCOORD5.w = _604.y;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_TEXCOORD1 = stage_input.v_in_TEXCOORD1;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD5 = v_vs_TEXCOORD5;
    return stage_output;
}
static float2 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_TEXCOORD5;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float2 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD5 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat5;
static float3 f_u_xlat0;
static bool f_u_xlatb5;
static float f_u_xlat16_1;
static float f_u_xlat16_6;
static float f_u_xlat16_11;
static float4 f_u_xlat2;
static float3 f_u_xlat16_3;
static float4 f_u_xlat16_4;
static float f_u_xlat4;
static float4 f_u_xlat16_2;
static float f_u_xlat9;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0).w;
    f_u_xlat5 = f_u_xlat16_0.x + (-f_vs_TEXCOORD1.x);
    f_u_xlat0.x = (-f_u_xlat16_0.x) + f_vs_TEXCOORD1.z;
    f_u_xlatb5 = f_u_xlat5 < 0.0f;
    if (f_u_xlatb5)
    {
        discard;
    }
    f_u_xlat5 = _OutlineWidth * _ScaleRatioA;
    f_u_xlat5 *= f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = min(f_u_xlat5, 1.0f);
    f_u_xlat16_6 = f_u_xlat5 * 0.5f;
    f_u_xlat16_1 = sqrt(f_u_xlat16_1);
    f_u_xlat16_11 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + f_u_xlat16_6;
    f_u_xlat16_11 = clamp(f_u_xlat16_11, 0.0f, 1.0f);
    f_u_xlat16_6 = (f_u_xlat0.x * f_vs_TEXCOORD1.y) + (-f_u_xlat16_6);
    f_u_xlat16_1 *= f_u_xlat16_11;
    float2 _112 = (float2(_OutlineUVSpeedX, _OutlineUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.zw;
    f_u_xlat0.x = _112.x;
    f_u_xlat0.y = _112.y;
    f_u_xlat16_0 = _OutlineTex.Sample(sampler_OutlineTex, f_u_xlat0.xy);
    f_u_xlat2 = f_u_xlat16_0 * _OutlineColor;
    f_u_xlat16_3 = f_vs_COLOR0.xyz * _FaceColor.xyz;
    float2 _145 = (float2(_FaceUVSpeedX, _FaceUVSpeedY) * _Time.yy) + f_vs_TEXCOORD5.xy;
    f_u_xlat0.x = _145.x;
    f_u_xlat0.y = _145.y;
    f_u_xlat16_4 = _FaceTex.Sample(sampler_FaceTex, f_u_xlat0.xy);
    f_u_xlat0 = f_u_xlat16_3 * f_u_xlat16_4.xyz;
    f_u_xlat4 = f_u_xlat16_4.w * _FaceColor.w;
    f_u_xlat16_3 = f_u_xlat0 * f_u_xlat4.xxx;
    float3 _178 = (f_u_xlat2.xyz * f_u_xlat2.www) + (-f_u_xlat16_3);
    f_u_xlat16_2.x = _178.x;
    f_u_xlat16_2.y = _178.y;
    f_u_xlat16_2.z = _178.z;
    f_u_xlat16_2.w = (_OutlineColor.w * f_u_xlat16_0.w) + (-f_u_xlat4);
    f_u_xlat16_2 = f_u_xlat16_1.xxxx * f_u_xlat16_2;
    float3 _204 = (f_u_xlat0 * f_u_xlat4.xxx) + f_u_xlat16_2.xyz;
    f_u_xlat16_0.x = _204.x;
    f_u_xlat16_0.y = _204.y;
    f_u_xlat16_0.z = _204.z;
    f_u_xlat16_0.w = (_FaceColor.w * f_u_xlat16_4.w) + f_u_xlat16_2.w;
    f_u_xlat4 = _OutlineSoftness * _ScaleRatioA;
    f_u_xlat9 = f_u_xlat4 * f_vs_TEXCOORD1.y;
    f_u_xlat16_1 = (f_u_xlat4 * f_vs_TEXCOORD1.y) + 1.0f;
    f_u_xlat16_6 = (f_u_xlat9 * 0.5f) + f_u_xlat16_6;
    f_u_xlat16_1 = f_u_xlat16_6 / f_u_xlat16_1;
    f_u_xlat16_1 = clamp(f_u_xlat16_1, 0.0f, 1.0f);
    f_u_xlat16_1 = (-f_u_xlat16_1) + 1.0f;
    f_u_xlat16_0 *= f_u_xlat16_1.xxxx;
    f_SV_Target0 = f_u_xlat16_0 * f_vs_COLOR0.wwww;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_TEXCOORD5 = stage_input.f_vs_TEXCOORD5;
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
