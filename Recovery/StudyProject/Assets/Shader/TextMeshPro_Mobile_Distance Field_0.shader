Shader "TextMeshPro/Mobile/Distance Field" {
Properties {
		_FaceColor ("Face Color", Vector) = (1,1,1,1)
		_FaceDilate ("Face Dilate", Range(-1, 1)) = 0
		_OutlineColor ("Outline Color", Vector) = (0,0,0,1)
		_OutlineWidth ("Outline Thickness", Range(0, 1)) = 0
		_OutlineSoftness ("Outline Softness", Range(0, 1)) = 0
		_UnderlayColor ("Border Color", Vector) = (0,0,0,0.5)
		_UnderlayOffsetX ("Border OffsetX", Range(-1, 1)) = 0
		_UnderlayOffsetY ("Border OffsetY", Range(-1, 1)) = 0
		_UnderlayDilate ("Border Dilate", Range(-1, 1)) = 0
		_UnderlaySoftness ("Border Softness", Range(0, 1)) = 0
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
#pragma multi_compile _ OUTLINE_ON
#pragma multi_compile _ UNDERLAY_ON
#pragma multi_compile _ UNITY_UI_ALPHACLIP
#pragma multi_compile _ UNITY_UI_CLIP_RECT
#if defined(OUTLINE_ON) && defined(UNDERLAY_ON) && defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float _UnderlaySoftness;
float _ScaleRatioC;
float _UnderlayDilate;
float _UnderlayOffsetX;
float _TextureWidth;
float _UnderlayOffsetY;
float _TextureHeight;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _UnderlayColor;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;
static float4 v_vs_TEXCOORD3;
static float2 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_COLOR1 : TEXCOORD1;
    float4 v_vs_TEXCOORD0 : TEXCOORD2;
    float4 v_vs_TEXCOORD1 : TEXCOORD3;
    float4 v_vs_TEXCOORD2 : TEXCOORD4;
    float4 v_vs_TEXCOORD3 : TEXCOORD5;
    float2 v_vs_TEXCOORD4 : TEXCOORD6;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat16;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat14;
static float2 v_u_xlat29;
static float2 v_u_xlat30;
static float v_u_xlat42;
static float v_u_xlat43;
static bool v_u_xlatb29;
static float4 v_u_xlat6;
static float v_u_xlat15;
static bool v_u_xlatb43;
static int v_u_xlati30;
static int v_u_xlati43;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static bool3 v_u_xlatb4;
static float4 v_u_xlat16_4;
static float v_u_xlat46;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_46;
static float v_u_xlat44;
static float v_u_xlat11;
static float v_u_xlat25;
static float2 v_u_xlat12;
static float v_u_xlat26;
static float2 v_u_xlat40;
static float2 v_u_xlat35;
static float4 v_u_xlat7;
static float v_u_xlat20;
static float v_u_xlat34;
static float2 v_u_xlat16_30;
static float2 v_u_xlat13;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;
static float2 v_u_xlat16_11;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0.x = 0.0f;
    v_u_xlatb0 = v_u_xlat0.x >= v_in_TEXCOORD0.w;
    v_u_xlat0.x = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat16 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat16;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat14 = v_u_xlat3.ww;
    v_u_xlat29 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat30 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat29 += v_u_xlat30;
    v_u_xlat30 = -v_u_xlat29;
    v_u_xlat29 = max(v_u_xlat29, v_u_xlat30);
    v_u_xlat29 *= float2(_ScaleX, _ScaleY);
    v_u_xlat14 /= v_u_xlat29;
    v_u_xlat42 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat42 = rsqrt(v_u_xlat42);
    v_u_xlat29.x = -v_in_TEXCOORD0.w;
    v_u_xlat29.x = max(v_u_xlat29.x, v_in_TEXCOORD0.w);
    v_u_xlat29.x *= _GradientScale;
    v_u_xlat43 = 1.0f;
    v_u_xlat43 += _Sharpness;
    v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
    v_u_xlat42 *= v_u_xlat29.x;
    v_u_xlat29.x = 0.0f;
    v_u_xlatb29 = v_u_xlat29.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb29)
    {
        v_u_xlat29.x = -v_u_xlat42;
        v_u_xlat29.x = max(v_u_xlat42, v_u_xlat29.x);
        v_u_xlat43 = 1.0f;
        v_u_xlat30.x = -_PerspectiveFilter;
        v_u_xlat43 += v_u_xlat30.x;
        v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat43 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat43 = rsqrt(v_u_xlat43);
        float3 _293 = v_u_xlat43.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _293.x;
        v_u_xlat4.y = _293.y;
        v_u_xlat4.z = _293.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _306 = v_u_xlat1;
        v_u_xlat1.x = _306.xy.x;
        v_u_xlat1.y = _306.xy.y;
        float3 _317 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _317.x;
        v_u_xlat5.y = _317.y;
        v_u_xlat5.z = _317.z;
        float3 _330 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _330.x;
        v_u_xlat6.y = _330.y;
        v_u_xlat6.z = _330.z;
        float4 _337 = v_u_xlat5;
        float3 _341 = _337.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _341.x;
        v_u_xlat5.y = _341.y;
        v_u_xlat5.z = _341.z;
        float3 _353 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _353.x;
        v_u_xlat6.y = _353.y;
        v_u_xlat6.z = _353.z;
        float4 _360 = v_u_xlat5;
        float3 _364 = _360.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _364.x;
        v_u_xlat5.y = _364.y;
        v_u_xlat5.z = _364.z;
        float4 _371 = v_u_xlat1;
        float3 _376 = _371.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _376.x;
        v_u_xlat1.y = _376.y;
        v_u_xlat1.w = _376.z;
        float4 _383 = v_u_xlat1;
        float3 _387 = _383.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _387.x;
        v_u_xlat1.y = _387.y;
        v_u_xlat1.w = _387.z;
        float4 _394 = v_u_xlat1;
        v_u_xlat1.x = _394.xyw.x;
        v_u_xlat1.y = _394.xyw.y;
        v_u_xlat1.w = _394.xyw.z;
        float4 _402 = v_u_xlat1;
        float3 _404 = -_402.xyw;
        v_u_xlat1.x = _404.x;
        v_u_xlat1.y = _404.y;
        v_u_xlat1.w = _404.z;
        float4 _411 = v_u_xlat1;
        float3 _416 = _411.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _416.x;
        v_u_xlat1.y = _416.y;
        v_u_xlat1.w = _416.z;
        float4 _423 = v_u_xlat1;
        v_u_xlat1.x = _423.xyw.x;
        v_u_xlat1.y = _423.xyw.y;
        v_u_xlat1.w = _423.xyw.z;
        v_u_xlat30.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat30.x = rsqrt(v_u_xlat30.x);
        float4 _441 = v_u_xlat1;
        float3 _445 = _441.xyw * v_u_xlat30.xxx;
        v_u_xlat1.x = _445.x;
        v_u_xlat1.y = _445.y;
        v_u_xlat1.w = _445.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat15 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat15, v_u_xlat1.x);
        v_u_xlat15 = -v_u_xlat29.x;
        v_u_xlat15 = v_u_xlat42 + v_u_xlat15;
        v_u_xlat1.x = v_u_xlat15 * v_u_xlat1.x;
        v_u_xlat42 = v_u_xlat1.x + v_u_xlat29.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0.x *= v_u_xlat1.x;
    v_u_xlat0.x += _WeightNormal;
    v_u_xlat0.x /= 4.0f;
    v_u_xlat0.x += _FaceDilate;
    v_u_xlat0.x *= _ScaleRatioA;
    v_u_xlat0.x *= 0.5f;
    v_u_xlat42 = v_u_xlat42;
    v_u_xlat1.x = 1.0f;
    v_u_xlat15 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat15 = v_u_xlat42 * v_u_xlat15;
    v_u_xlat1.x = v_u_xlat15 + v_u_xlat1.x;
    v_u_xlat1.x = v_u_xlat42 / v_u_xlat1.x;
    v_u_xlat15 = -v_u_xlat0.x;
    v_u_xlat15 += 0.5f;
    v_u_xlat15 = v_u_xlat1.x * v_u_xlat15;
    v_u_xlat29.x = -0.5f;
    v_u_xlat15 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat29.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat29.x *= 0.5f;
    v_u_xlat29.x = v_u_xlat1.x * v_u_xlat29.x;
    int4 _581 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb43 = any(bool4(int4(0, 0, 0, 0).x != _581.x, int4(0, 0, 0, 0).y != _581.y, int4(0, 0, 0, 0).z != _581.z, int4(0, 0, 0, 0).w != _581.w));
    v_u_xlati30 = 0;
    int param = v_u_xlati30;
    v_u_xlati30 = v_op_not(param);
    v_u_xlati43 = v_u_xlatb43 ? v_u_xlati30 : 0;
    if (v_u_xlati43 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _615 = v_u_xlat16_8;
        float3 _617 = v_u_xlat16_7.xyz * _615.xyz;
        v_u_xlat16_8.x = _617.x;
        v_u_xlat16_8.y = _617.y;
        v_u_xlat16_8.z = _617.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _631 = v_u_xlat16_8;
        float3 _634 = _631.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _634.x;
        v_u_xlat16_8.y = _634.y;
        v_u_xlat16_8.z = _634.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb4 = bool4(v_u_xlat16_7.xyzx.x < v_u_xlat16_10.xyzx.x, v_u_xlat16_7.xyzx.y < v_u_xlat16_10.xyzx.y, v_u_xlat16_7.xyzx.z < v_u_xlat16_10.xyzx.z, v_u_xlat16_7.xyzx.w < v_u_xlat16_10.xyzx.w).xyz;
        float _698;
        if (v_u_xlatb4.x)
        {
            _698 = v_u_xlat16_8.x;
        }
        else
        {
            _698 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _698;
        float _710;
        if (v_u_xlatb4.y)
        {
            _710 = v_u_xlat16_8.y;
        }
        else
        {
            _710 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _710;
        float _722;
        if (v_u_xlatb4.z)
        {
            _722 = v_u_xlat16_8.z;
        }
        else
        {
            _722 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _722;
        float4 _732 = v_u_xlat16_4;
        v_u_xlat16_4.x = _732.xyz.x;
        v_u_xlat16_4.y = _732.xyz.y;
        v_u_xlat16_4.z = _732.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat46 = 1.0f;
    v_u_xlat16_4.w = v_u_xlat46;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _762 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _762.x;
    v_u_xlat16_6.y = _762.y;
    v_u_xlat16_6.z = _762.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat30.x = v_u_xlat16_7.w;
    v_u_xlat46 = v_u_xlat16_4.w * v_u_xlat30.x;
    v_u_xlat16_46 = v_u_xlat46;
    float3 _785 = v_u_xlat16_46.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _785.x;
    v_u_xlat16_4.y = _785.y;
    v_u_xlat16_4.z = _785.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_46;
    v_u_xlat30.x = 2.0f;
    v_u_xlat30.x = v_u_xlat29.x * v_u_xlat30.x;
    v_u_xlat30.x = min(v_u_xlat30.x, 1.0f);
    v_u_xlat30.x = sqrt(v_u_xlat30.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat30.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat30.x = 1.0f;
    v_u_xlat44 = _UnderlaySoftness * _ScaleRatioC;
    v_u_xlat44 = v_u_xlat42 * v_u_xlat44;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat11 = v_u_xlat42 / v_u_xlat30.x;
    v_u_xlat30.x = -v_u_xlat0.x;
    v_u_xlat30.x += 0.5f;
    v_u_xlat30.x = v_u_xlat11 * v_u_xlat30.x;
    v_u_xlat44 = -0.5f;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat44 = _UnderlayDilate * _ScaleRatioC;
    v_u_xlat44 *= 0.5f;
    v_u_xlat44 = v_u_xlat11 * v_u_xlat44;
    v_u_xlat44 = -v_u_xlat44;
    v_u_xlat25 = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat30.x = _UnderlayOffsetX * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat12.x = v_u_xlat30.x / _TextureWidth;
    v_u_xlat30.x = _UnderlayOffsetY * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat26 = v_u_xlat30.x / _TextureHeight;
    v_u_xlat12.x = v_u_xlat12.x;
    v_u_xlat12.y = v_u_xlat26;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat30 = -v_u_xlat5.xy;
    v_u_xlat30 += v_u_xlat2.xy;
    v_u_xlat40 = -v_u_xlat5.xy;
    v_u_xlat40 = v_u_xlat5.zw + v_u_xlat40;
    v_u_xlat35 = v_u_xlat30 / v_u_xlat40;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat35.x;
    v_u_xlat7.w = v_u_xlat35.y;
    v_u_xlat30.x = -v_u_xlat29.x;
    v_u_xlat20 = v_u_xlat15 + v_u_xlat30.x;
    v_u_xlat34 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat16_6.x = v_u_xlat1.x;
    v_u_xlat16_6.y = v_u_xlat20;
    v_u_xlat16_6.z = v_u_xlat34;
    v_u_xlat16_6.w = v_u_xlat15;
    v_u_xlat30.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat44 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_30.x = v_u_xlat30.x;
    v_u_xlat16_30.y = v_u_xlat44;
    v_u_xlat40 = 2.0f.xx;
    v_u_xlat40 = v_u_xlat2.xy * v_u_xlat40;
    v_u_xlat13 = -v_u_xlat5.xy;
    v_u_xlat40 += v_u_xlat13;
    v_u_xlat13 = -v_u_xlat5.zw;
    float2 _1052 = v_u_xlat40 + v_u_xlat13;
    v_u_xlat1.x = _1052.x;
    v_u_xlat1.y = _1052.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _1061 = v_u_xlat16_9;
    float2 _1063 = v_u_xlat16_30 * _1061.xy;
    v_u_xlat16_9.x = _1063.x;
    v_u_xlat16_9.y = _1063.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat40 = v_u_xlat14 + v_u_xlat9;
    v_u_xlat29 = 0.25f.xx / v_u_xlat40;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat29.x;
    v_u_xlat16_1.w = v_u_xlat29.y;
    float2 _1092 = v_u_xlat12 + v_in_TEXCOORD0.xy;
    v_u_xlat0.x = _1092.x;
    v_u_xlat0.y = _1092.y;
    v_u_xlat0.z = v_in_COLOR0.w;
    v_u_xlat0.w = 0.0f;
    v_u_xlat16_11.x = v_u_xlat11;
    v_u_xlat16_11.y = v_u_xlat25;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
    v_vs_TEXCOORD3 = v_u_xlat0;
    v_vs_TEXCOORD4 = v_u_xlat16_11;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_COLOR1 = v_vs_COLOR1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR1;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD4;
static float4 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_COLOR1 : TEXCOORD1;
    float4 f_vs_TEXCOORD0 : TEXCOORD2;
    float4 f_vs_TEXCOORD1 : TEXCOORD3;
    float4 f_vs_TEXCOORD2 : TEXCOORD4;
    float4 f_vs_TEXCOORD3 : TEXCOORD5;
    float2 f_vs_TEXCOORD4 : TEXCOORD6;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat8;
static float4 f_u_xlat0;
static float4 f_u_xlat16_1;
static float4 f_u_xlat16_2;
static float2 f_u_xlat16_3;
static float f_u_xlat16_4;
static float f_u_xlat12;
static float f_u_xlat4;
static float4 f_u_xlat1;
static float4 f_u_xlat2;
static float f_u_xlat16_11;
static float2 f_u_xlat5;
static float2 f_u_xlat3;
static float2 f_u_xlat21;
static float2 f_u_xlat16_5;
static float f_u_xlat16_6;
static float f_u_xlat6;
static bool f_u_xlatb7;
static int f_u_xlati7;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat8 = f_vs_TEXCOORD1.x;
    f_u_xlat0.x = f_u_xlat8 * f_u_xlat16_0.x;
    f_u_xlat16_0.x = f_u_xlat0.x;
    f_u_xlat16_1.x = -f_vs_TEXCOORD1.z;
    f_u_xlat16_1.x = f_u_xlat16_0.x + f_u_xlat16_1.x;
    f_u_xlat16_1.x = max(f_u_xlat16_1.x, 0.0f);
    f_u_xlat16_1.x = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_2 = -f_vs_COLOR1;
    f_u_xlat16_2 += f_vs_COLOR0;
    f_u_xlat16_1 = f_u_xlat16_1.xxxx * f_u_xlat16_2;
    f_u_xlat16_1 += f_vs_COLOR1;
    f_u_xlat16_3.x = -f_vs_TEXCOORD1.y;
    f_u_xlat16_3.x = f_u_xlat16_0.x + f_u_xlat16_3.x;
    f_u_xlat16_3.x = max(f_u_xlat16_3.x, 0.0f);
    f_u_xlat16_3.x = min(f_u_xlat16_3.x, 1.0f);
    f_u_xlat16_0 = f_u_xlat16_1 * f_u_xlat16_3.xxxx;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD3.xy).w;
    f_u_xlat12 = f_vs_TEXCOORD4.x;
    f_u_xlat4 = f_u_xlat12 * f_u_xlat16_4;
    f_u_xlat16_4 = f_u_xlat4;
    float3 _127 = _UnderlayColor.www * _UnderlayColor.xyz;
    f_u_xlat16_1.x = _127.x;
    f_u_xlat16_1.y = _127.y;
    f_u_xlat16_1.z = _127.z;
    f_u_xlat1.x = f_u_xlat16_1.xyz.x;
    f_u_xlat1.y = f_u_xlat16_1.xyz.y;
    f_u_xlat1.z = f_u_xlat16_1.xyz.z;
    f_u_xlat1.w = _UnderlayColor.w;
    f_u_xlat16_3.x = -f_vs_TEXCOORD4.y;
    f_u_xlat16_3.x += f_u_xlat16_4;
    f_u_xlat16_3.x = max(f_u_xlat16_3.x, 0.0f);
    f_u_xlat16_2 = min(f_u_xlat16_3.xxxx, 1.0f.xxxx);
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat16_3.x = 1.0f;
    f_u_xlat16_11 = -f_u_xlat16_0.w;
    f_u_xlat16_2 = f_u_xlat16_11.xxxx + f_u_xlat16_3.xxxx;
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 = f_u_xlat1 + f_u_xlat0;
    f_u_xlat16_0 = f_u_xlat0;
    f_u_xlat5 = -_ClipRect.xy;
    f_u_xlat5 += _ClipRect.zw;
    f_u_xlat16_3 = -f_vs_TEXCOORD2.xy;
    f_u_xlat16_3 = max(f_u_xlat16_3, f_vs_TEXCOORD2.xy);
    f_u_xlat3 = f_u_xlat16_3;
    f_u_xlat21 = -f_u_xlat3;
    f_u_xlat5 = f_u_xlat21 + f_u_xlat5;
    f_u_xlat21 = f_vs_TEXCOORD2.zw;
    f_u_xlat5 = f_u_xlat21 * f_u_xlat5;
    f_u_xlat5 = max(f_u_xlat5, 0.0f.xx);
    f_u_xlat5 = min(f_u_xlat5, 1.0f.xx);
    f_u_xlat16_5 = f_u_xlat5;
    f_u_xlat16_6 = f_u_xlat16_5.y * f_u_xlat16_5.x;
    f_u_xlat16_0 *= f_u_xlat16_6.xxxx;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 *= f_vs_TEXCOORD3.zzzz;
    f_u_xlat16_0 = f_u_xlat0;
    f_u_xlat16_6 = 0.001000000047497451305389404296875f;
    f_u_xlat16_6 = -f_u_xlat16_6;
    f_u_xlat16_6 = f_u_xlat16_0.w + f_u_xlat16_6;
    f_u_xlat6 = f_u_xlat16_6;
    f_u_xlatb7 = f_u_xlat6 < 0.0f;
    f_u_xlati7 = f_u_xlatb7 ? (-1) : 0;
    if (f_u_xlati7 != 0)
    {
        discard;
    }
    f_SV_Target0 = f_u_xlat16_0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR1 = stage_input.f_vs_COLOR1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(OUTLINE_ON) && defined(UNDERLAY_ON) && defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float _UnderlaySoftness;
float _ScaleRatioC;
float _UnderlayDilate;
float _UnderlayOffsetX;
float _TextureWidth;
float _UnderlayOffsetY;
float _TextureHeight;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _UnderlayColor;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD3;
static float2 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_COLOR1 : TEXCOORD1;
    float4 v_vs_TEXCOORD0 : TEXCOORD2;
    float4 v_vs_TEXCOORD1 : TEXCOORD3;
    float4 v_vs_TEXCOORD3 : TEXCOORD4;
    float2 v_vs_TEXCOORD4 : TEXCOORD5;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat16;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat14;
static float2 v_u_xlat29;
static float2 v_u_xlat30;
static float v_u_xlat42;
static float v_u_xlat43;
static bool v_u_xlatb29;
static float4 v_u_xlat6;
static float v_u_xlat15;
static bool v_u_xlatb43;
static int v_u_xlati30;
static int v_u_xlati43;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static bool3 v_u_xlatb4;
static float4 v_u_xlat16_4;
static float v_u_xlat46;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_46;
static float v_u_xlat44;
static float v_u_xlat11;
static float v_u_xlat25;
static float2 v_u_xlat12;
static float v_u_xlat26;
static float2 v_u_xlat40;
static float2 v_u_xlat35;
static float4 v_u_xlat7;
static float v_u_xlat20;
static float v_u_xlat34;
static float2 v_u_xlat16_30;
static float2 v_u_xlat13;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;
static float2 v_u_xlat16_11;
static float4 v_vs_TEXCOORD2;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0.x = 0.0f;
    v_u_xlatb0 = v_u_xlat0.x >= v_in_TEXCOORD0.w;
    v_u_xlat0.x = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat16 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat16;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat14 = v_u_xlat3.ww;
    v_u_xlat29 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat30 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat29 += v_u_xlat30;
    v_u_xlat30 = -v_u_xlat29;
    v_u_xlat29 = max(v_u_xlat29, v_u_xlat30);
    v_u_xlat29 *= float2(_ScaleX, _ScaleY);
    v_u_xlat14 /= v_u_xlat29;
    v_u_xlat42 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat42 = rsqrt(v_u_xlat42);
    v_u_xlat29.x = -v_in_TEXCOORD0.w;
    v_u_xlat29.x = max(v_u_xlat29.x, v_in_TEXCOORD0.w);
    v_u_xlat29.x *= _GradientScale;
    v_u_xlat43 = 1.0f;
    v_u_xlat43 += _Sharpness;
    v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
    v_u_xlat42 *= v_u_xlat29.x;
    v_u_xlat29.x = 0.0f;
    v_u_xlatb29 = v_u_xlat29.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb29)
    {
        v_u_xlat29.x = -v_u_xlat42;
        v_u_xlat29.x = max(v_u_xlat42, v_u_xlat29.x);
        v_u_xlat43 = 1.0f;
        v_u_xlat30.x = -_PerspectiveFilter;
        v_u_xlat43 += v_u_xlat30.x;
        v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat43 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat43 = rsqrt(v_u_xlat43);
        float3 _293 = v_u_xlat43.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _293.x;
        v_u_xlat4.y = _293.y;
        v_u_xlat4.z = _293.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _306 = v_u_xlat1;
        v_u_xlat1.x = _306.xy.x;
        v_u_xlat1.y = _306.xy.y;
        float3 _317 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _317.x;
        v_u_xlat5.y = _317.y;
        v_u_xlat5.z = _317.z;
        float3 _330 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _330.x;
        v_u_xlat6.y = _330.y;
        v_u_xlat6.z = _330.z;
        float4 _337 = v_u_xlat5;
        float3 _341 = _337.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _341.x;
        v_u_xlat5.y = _341.y;
        v_u_xlat5.z = _341.z;
        float3 _353 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _353.x;
        v_u_xlat6.y = _353.y;
        v_u_xlat6.z = _353.z;
        float4 _360 = v_u_xlat5;
        float3 _364 = _360.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _364.x;
        v_u_xlat5.y = _364.y;
        v_u_xlat5.z = _364.z;
        float4 _371 = v_u_xlat1;
        float3 _376 = _371.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _376.x;
        v_u_xlat1.y = _376.y;
        v_u_xlat1.w = _376.z;
        float4 _383 = v_u_xlat1;
        float3 _387 = _383.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _387.x;
        v_u_xlat1.y = _387.y;
        v_u_xlat1.w = _387.z;
        float4 _394 = v_u_xlat1;
        v_u_xlat1.x = _394.xyw.x;
        v_u_xlat1.y = _394.xyw.y;
        v_u_xlat1.w = _394.xyw.z;
        float4 _402 = v_u_xlat1;
        float3 _404 = -_402.xyw;
        v_u_xlat1.x = _404.x;
        v_u_xlat1.y = _404.y;
        v_u_xlat1.w = _404.z;
        float4 _411 = v_u_xlat1;
        float3 _416 = _411.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _416.x;
        v_u_xlat1.y = _416.y;
        v_u_xlat1.w = _416.z;
        float4 _423 = v_u_xlat1;
        v_u_xlat1.x = _423.xyw.x;
        v_u_xlat1.y = _423.xyw.y;
        v_u_xlat1.w = _423.xyw.z;
        v_u_xlat30.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat30.x = rsqrt(v_u_xlat30.x);
        float4 _441 = v_u_xlat1;
        float3 _445 = _441.xyw * v_u_xlat30.xxx;
        v_u_xlat1.x = _445.x;
        v_u_xlat1.y = _445.y;
        v_u_xlat1.w = _445.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat15 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat15, v_u_xlat1.x);
        v_u_xlat15 = -v_u_xlat29.x;
        v_u_xlat15 = v_u_xlat42 + v_u_xlat15;
        v_u_xlat1.x = v_u_xlat15 * v_u_xlat1.x;
        v_u_xlat42 = v_u_xlat1.x + v_u_xlat29.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0.x *= v_u_xlat1.x;
    v_u_xlat0.x += _WeightNormal;
    v_u_xlat0.x /= 4.0f;
    v_u_xlat0.x += _FaceDilate;
    v_u_xlat0.x *= _ScaleRatioA;
    v_u_xlat0.x *= 0.5f;
    v_u_xlat42 = v_u_xlat42;
    v_u_xlat1.x = 1.0f;
    v_u_xlat15 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat15 = v_u_xlat42 * v_u_xlat15;
    v_u_xlat1.x = v_u_xlat15 + v_u_xlat1.x;
    v_u_xlat1.x = v_u_xlat42 / v_u_xlat1.x;
    v_u_xlat15 = -v_u_xlat0.x;
    v_u_xlat15 += 0.5f;
    v_u_xlat15 = v_u_xlat1.x * v_u_xlat15;
    v_u_xlat29.x = -0.5f;
    v_u_xlat15 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat29.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat29.x *= 0.5f;
    v_u_xlat29.x = v_u_xlat1.x * v_u_xlat29.x;
    int4 _581 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb43 = any(bool4(int4(0, 0, 0, 0).x != _581.x, int4(0, 0, 0, 0).y != _581.y, int4(0, 0, 0, 0).z != _581.z, int4(0, 0, 0, 0).w != _581.w));
    v_u_xlati30 = 0;
    int param = v_u_xlati30;
    v_u_xlati30 = v_op_not(param);
    v_u_xlati43 = v_u_xlatb43 ? v_u_xlati30 : 0;
    if (v_u_xlati43 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _615 = v_u_xlat16_8;
        float3 _617 = v_u_xlat16_7.xyz * _615.xyz;
        v_u_xlat16_8.x = _617.x;
        v_u_xlat16_8.y = _617.y;
        v_u_xlat16_8.z = _617.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _631 = v_u_xlat16_8;
        float3 _634 = _631.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _634.x;
        v_u_xlat16_8.y = _634.y;
        v_u_xlat16_8.z = _634.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb4 = bool4(v_u_xlat16_7.xyzx.x < v_u_xlat16_10.xyzx.x, v_u_xlat16_7.xyzx.y < v_u_xlat16_10.xyzx.y, v_u_xlat16_7.xyzx.z < v_u_xlat16_10.xyzx.z, v_u_xlat16_7.xyzx.w < v_u_xlat16_10.xyzx.w).xyz;
        float _698;
        if (v_u_xlatb4.x)
        {
            _698 = v_u_xlat16_8.x;
        }
        else
        {
            _698 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _698;
        float _710;
        if (v_u_xlatb4.y)
        {
            _710 = v_u_xlat16_8.y;
        }
        else
        {
            _710 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _710;
        float _722;
        if (v_u_xlatb4.z)
        {
            _722 = v_u_xlat16_8.z;
        }
        else
        {
            _722 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _722;
        float4 _732 = v_u_xlat16_4;
        v_u_xlat16_4.x = _732.xyz.x;
        v_u_xlat16_4.y = _732.xyz.y;
        v_u_xlat16_4.z = _732.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat46 = 1.0f;
    v_u_xlat16_4.w = v_u_xlat46;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _762 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _762.x;
    v_u_xlat16_6.y = _762.y;
    v_u_xlat16_6.z = _762.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat30.x = v_u_xlat16_7.w;
    v_u_xlat46 = v_u_xlat16_4.w * v_u_xlat30.x;
    v_u_xlat16_46 = v_u_xlat46;
    float3 _785 = v_u_xlat16_46.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _785.x;
    v_u_xlat16_4.y = _785.y;
    v_u_xlat16_4.z = _785.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_46;
    v_u_xlat30.x = 2.0f;
    v_u_xlat30.x = v_u_xlat29.x * v_u_xlat30.x;
    v_u_xlat30.x = min(v_u_xlat30.x, 1.0f);
    v_u_xlat30.x = sqrt(v_u_xlat30.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat30.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat30.x = 1.0f;
    v_u_xlat44 = _UnderlaySoftness * _ScaleRatioC;
    v_u_xlat44 = v_u_xlat42 * v_u_xlat44;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat11 = v_u_xlat42 / v_u_xlat30.x;
    v_u_xlat30.x = -v_u_xlat0.x;
    v_u_xlat30.x += 0.5f;
    v_u_xlat30.x = v_u_xlat11 * v_u_xlat30.x;
    v_u_xlat44 = -0.5f;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat44 = _UnderlayDilate * _ScaleRatioC;
    v_u_xlat44 *= 0.5f;
    v_u_xlat44 = v_u_xlat11 * v_u_xlat44;
    v_u_xlat44 = -v_u_xlat44;
    v_u_xlat25 = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat30.x = _UnderlayOffsetX * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat12.x = v_u_xlat30.x / _TextureWidth;
    v_u_xlat30.x = _UnderlayOffsetY * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat26 = v_u_xlat30.x / _TextureHeight;
    v_u_xlat12.x = v_u_xlat12.x;
    v_u_xlat12.y = v_u_xlat26;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat30 = -v_u_xlat5.xy;
    v_u_xlat30 += v_u_xlat2.xy;
    v_u_xlat40 = -v_u_xlat5.xy;
    v_u_xlat40 = v_u_xlat5.zw + v_u_xlat40;
    v_u_xlat35 = v_u_xlat30 / v_u_xlat40;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat35.x;
    v_u_xlat7.w = v_u_xlat35.y;
    v_u_xlat30.x = -v_u_xlat29.x;
    v_u_xlat20 = v_u_xlat15 + v_u_xlat30.x;
    v_u_xlat34 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat16_6.x = v_u_xlat1.x;
    v_u_xlat16_6.y = v_u_xlat20;
    v_u_xlat16_6.z = v_u_xlat34;
    v_u_xlat16_6.w = v_u_xlat15;
    v_u_xlat30.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat44 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_30.x = v_u_xlat30.x;
    v_u_xlat16_30.y = v_u_xlat44;
    v_u_xlat40 = 2.0f.xx;
    v_u_xlat40 = v_u_xlat2.xy * v_u_xlat40;
    v_u_xlat13 = -v_u_xlat5.xy;
    v_u_xlat40 += v_u_xlat13;
    v_u_xlat13 = -v_u_xlat5.zw;
    float2 _1052 = v_u_xlat40 + v_u_xlat13;
    v_u_xlat1.x = _1052.x;
    v_u_xlat1.y = _1052.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _1061 = v_u_xlat16_9;
    float2 _1063 = v_u_xlat16_30 * _1061.xy;
    v_u_xlat16_9.x = _1063.x;
    v_u_xlat16_9.y = _1063.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat40 = v_u_xlat14 + v_u_xlat9;
    v_u_xlat29 = 0.25f.xx / v_u_xlat40;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat29.x;
    v_u_xlat16_1.w = v_u_xlat29.y;
    float2 _1092 = v_u_xlat12 + v_in_TEXCOORD0.xy;
    v_u_xlat0.x = _1092.x;
    v_u_xlat0.y = _1092.y;
    v_u_xlat0.z = v_in_COLOR0.w;
    v_u_xlat0.w = 0.0f;
    v_u_xlat16_11.x = v_u_xlat11;
    v_u_xlat16_11.y = v_u_xlat25;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
    v_vs_TEXCOORD3 = v_u_xlat0;
    v_vs_TEXCOORD4 = v_u_xlat16_11;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_COLOR1 = v_vs_COLOR1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR1;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD4;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_COLOR1 : TEXCOORD1;
    float4 f_vs_TEXCOORD0 : TEXCOORD2;
    float4 f_vs_TEXCOORD1 : TEXCOORD3;
    float4 f_vs_TEXCOORD3 : TEXCOORD4;
    float2 f_vs_TEXCOORD4 : TEXCOORD5;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat6;
static float4 f_u_xlat0;
static float3 f_u_xlat16_1;
static float f_u_xlat16_7;
static float4 f_u_xlat16_2;
static float f_u_xlat16_3;
static float f_u_xlat9;
static float f_u_xlat3;
static float4 f_u_xlat1;
static float4 f_u_xlat2;
static float f_u_xlat16_4;
static float f_u_xlat16_10;
static float f_u_xlat4;
static bool f_u_xlatb5;
static int f_u_xlati5;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat6 = f_vs_TEXCOORD1.x;
    f_u_xlat0.x = f_u_xlat6 * f_u_xlat16_0.x;
    f_u_xlat16_1.x = f_u_xlat0.x;
    f_u_xlat16_7 = -f_vs_TEXCOORD1.z;
    f_u_xlat16_7 += f_u_xlat16_1.x;
    f_u_xlat16_7 = max(f_u_xlat16_7, 0.0f);
    f_u_xlat16_7 = min(f_u_xlat16_7, 1.0f);
    f_u_xlat16_0 = -f_vs_COLOR1;
    f_u_xlat16_0 += f_vs_COLOR0;
    f_u_xlat16_0 *= f_u_xlat16_7.xxxx;
    f_u_xlat16_0 += f_vs_COLOR1;
    f_u_xlat16_2.x = -f_vs_TEXCOORD1.y;
    f_u_xlat16_2.x = f_u_xlat16_1.x + f_u_xlat16_2.x;
    f_u_xlat16_2.x = max(f_u_xlat16_2.x, 0.0f);
    f_u_xlat16_2.x = min(f_u_xlat16_2.x, 1.0f);
    f_u_xlat16_0 *= f_u_xlat16_2.xxxx;
    f_u_xlat16_3 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD3.xy).w;
    f_u_xlat9 = f_vs_TEXCOORD4.x;
    f_u_xlat3 = f_u_xlat9 * f_u_xlat16_3;
    f_u_xlat16_3 = f_u_xlat3;
    f_u_xlat16_1 = _UnderlayColor.www * _UnderlayColor.xyz;
    f_u_xlat1.x = f_u_xlat16_1.x;
    f_u_xlat1.y = f_u_xlat16_1.y;
    f_u_xlat1.z = f_u_xlat16_1.z;
    f_u_xlat1.w = _UnderlayColor.w;
    f_u_xlat16_2.x = -f_vs_TEXCOORD4.y;
    f_u_xlat16_2.x += f_u_xlat16_3;
    f_u_xlat16_2.x = max(f_u_xlat16_2.x, 0.0f);
    f_u_xlat16_2 = min(f_u_xlat16_2.xxxx, 1.0f.xxxx);
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat16_4 = 1.0f;
    f_u_xlat16_10 = -f_u_xlat16_0.w;
    f_u_xlat16_2 = f_u_xlat16_10.xxxx + f_u_xlat16_4.xxxx;
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 = f_u_xlat1 + f_u_xlat0;
    f_u_xlat16_0 = f_u_xlat0;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 *= f_vs_TEXCOORD3.zzzz;
    f_u_xlat16_0 = f_u_xlat0;
    f_u_xlat16_4 = 0.001000000047497451305389404296875f;
    f_u_xlat16_4 = -f_u_xlat16_4;
    f_u_xlat16_4 = f_u_xlat16_0.w + f_u_xlat16_4;
    f_u_xlat4 = f_u_xlat16_4;
    f_u_xlatb5 = f_u_xlat4 < 0.0f;
    f_u_xlati5 = f_u_xlatb5 ? (-1) : 0;
    if (f_u_xlati5 != 0)
    {
        discard;
    }
    f_SV_Target0 = f_u_xlat16_0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR1 = stage_input.f_vs_COLOR1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(OUTLINE_ON) && defined(UNDERLAY_ON) && !defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float _UnderlaySoftness;
float _ScaleRatioC;
float _UnderlayDilate;
float _UnderlayOffsetX;
float _TextureWidth;
float _UnderlayOffsetY;
float _TextureHeight;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _UnderlayColor;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;
static float4 v_vs_TEXCOORD3;
static float2 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_COLOR1 : TEXCOORD1;
    float4 v_vs_TEXCOORD0 : TEXCOORD2;
    float4 v_vs_TEXCOORD1 : TEXCOORD3;
    float4 v_vs_TEXCOORD2 : TEXCOORD4;
    float4 v_vs_TEXCOORD3 : TEXCOORD5;
    float2 v_vs_TEXCOORD4 : TEXCOORD6;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat16;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat14;
static float2 v_u_xlat29;
static float2 v_u_xlat30;
static float v_u_xlat42;
static float v_u_xlat43;
static bool v_u_xlatb29;
static float4 v_u_xlat6;
static float v_u_xlat15;
static bool v_u_xlatb43;
static int v_u_xlati30;
static int v_u_xlati43;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static bool3 v_u_xlatb4;
static float4 v_u_xlat16_4;
static float v_u_xlat46;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_46;
static float v_u_xlat44;
static float v_u_xlat11;
static float v_u_xlat25;
static float2 v_u_xlat12;
static float v_u_xlat26;
static float2 v_u_xlat40;
static float2 v_u_xlat35;
static float4 v_u_xlat7;
static float v_u_xlat20;
static float v_u_xlat34;
static float2 v_u_xlat16_30;
static float2 v_u_xlat13;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;
static float2 v_u_xlat16_11;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0.x = 0.0f;
    v_u_xlatb0 = v_u_xlat0.x >= v_in_TEXCOORD0.w;
    v_u_xlat0.x = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat16 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat16;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat14 = v_u_xlat3.ww;
    v_u_xlat29 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat30 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat29 += v_u_xlat30;
    v_u_xlat30 = -v_u_xlat29;
    v_u_xlat29 = max(v_u_xlat29, v_u_xlat30);
    v_u_xlat29 *= float2(_ScaleX, _ScaleY);
    v_u_xlat14 /= v_u_xlat29;
    v_u_xlat42 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat42 = rsqrt(v_u_xlat42);
    v_u_xlat29.x = -v_in_TEXCOORD0.w;
    v_u_xlat29.x = max(v_u_xlat29.x, v_in_TEXCOORD0.w);
    v_u_xlat29.x *= _GradientScale;
    v_u_xlat43 = 1.0f;
    v_u_xlat43 += _Sharpness;
    v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
    v_u_xlat42 *= v_u_xlat29.x;
    v_u_xlat29.x = 0.0f;
    v_u_xlatb29 = v_u_xlat29.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb29)
    {
        v_u_xlat29.x = -v_u_xlat42;
        v_u_xlat29.x = max(v_u_xlat42, v_u_xlat29.x);
        v_u_xlat43 = 1.0f;
        v_u_xlat30.x = -_PerspectiveFilter;
        v_u_xlat43 += v_u_xlat30.x;
        v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat43 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat43 = rsqrt(v_u_xlat43);
        float3 _293 = v_u_xlat43.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _293.x;
        v_u_xlat4.y = _293.y;
        v_u_xlat4.z = _293.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _306 = v_u_xlat1;
        v_u_xlat1.x = _306.xy.x;
        v_u_xlat1.y = _306.xy.y;
        float3 _317 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _317.x;
        v_u_xlat5.y = _317.y;
        v_u_xlat5.z = _317.z;
        float3 _330 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _330.x;
        v_u_xlat6.y = _330.y;
        v_u_xlat6.z = _330.z;
        float4 _337 = v_u_xlat5;
        float3 _341 = _337.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _341.x;
        v_u_xlat5.y = _341.y;
        v_u_xlat5.z = _341.z;
        float3 _353 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _353.x;
        v_u_xlat6.y = _353.y;
        v_u_xlat6.z = _353.z;
        float4 _360 = v_u_xlat5;
        float3 _364 = _360.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _364.x;
        v_u_xlat5.y = _364.y;
        v_u_xlat5.z = _364.z;
        float4 _371 = v_u_xlat1;
        float3 _376 = _371.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _376.x;
        v_u_xlat1.y = _376.y;
        v_u_xlat1.w = _376.z;
        float4 _383 = v_u_xlat1;
        float3 _387 = _383.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _387.x;
        v_u_xlat1.y = _387.y;
        v_u_xlat1.w = _387.z;
        float4 _394 = v_u_xlat1;
        v_u_xlat1.x = _394.xyw.x;
        v_u_xlat1.y = _394.xyw.y;
        v_u_xlat1.w = _394.xyw.z;
        float4 _402 = v_u_xlat1;
        float3 _404 = -_402.xyw;
        v_u_xlat1.x = _404.x;
        v_u_xlat1.y = _404.y;
        v_u_xlat1.w = _404.z;
        float4 _411 = v_u_xlat1;
        float3 _416 = _411.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _416.x;
        v_u_xlat1.y = _416.y;
        v_u_xlat1.w = _416.z;
        float4 _423 = v_u_xlat1;
        v_u_xlat1.x = _423.xyw.x;
        v_u_xlat1.y = _423.xyw.y;
        v_u_xlat1.w = _423.xyw.z;
        v_u_xlat30.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat30.x = rsqrt(v_u_xlat30.x);
        float4 _441 = v_u_xlat1;
        float3 _445 = _441.xyw * v_u_xlat30.xxx;
        v_u_xlat1.x = _445.x;
        v_u_xlat1.y = _445.y;
        v_u_xlat1.w = _445.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat15 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat15, v_u_xlat1.x);
        v_u_xlat15 = -v_u_xlat29.x;
        v_u_xlat15 = v_u_xlat42 + v_u_xlat15;
        v_u_xlat1.x = v_u_xlat15 * v_u_xlat1.x;
        v_u_xlat42 = v_u_xlat1.x + v_u_xlat29.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0.x *= v_u_xlat1.x;
    v_u_xlat0.x += _WeightNormal;
    v_u_xlat0.x /= 4.0f;
    v_u_xlat0.x += _FaceDilate;
    v_u_xlat0.x *= _ScaleRatioA;
    v_u_xlat0.x *= 0.5f;
    v_u_xlat42 = v_u_xlat42;
    v_u_xlat1.x = 1.0f;
    v_u_xlat15 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat15 = v_u_xlat42 * v_u_xlat15;
    v_u_xlat1.x = v_u_xlat15 + v_u_xlat1.x;
    v_u_xlat1.x = v_u_xlat42 / v_u_xlat1.x;
    v_u_xlat15 = -v_u_xlat0.x;
    v_u_xlat15 += 0.5f;
    v_u_xlat15 = v_u_xlat1.x * v_u_xlat15;
    v_u_xlat29.x = -0.5f;
    v_u_xlat15 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat29.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat29.x *= 0.5f;
    v_u_xlat29.x = v_u_xlat1.x * v_u_xlat29.x;
    int4 _581 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb43 = any(bool4(int4(0, 0, 0, 0).x != _581.x, int4(0, 0, 0, 0).y != _581.y, int4(0, 0, 0, 0).z != _581.z, int4(0, 0, 0, 0).w != _581.w));
    v_u_xlati30 = 0;
    int param = v_u_xlati30;
    v_u_xlati30 = v_op_not(param);
    v_u_xlati43 = v_u_xlatb43 ? v_u_xlati30 : 0;
    if (v_u_xlati43 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _615 = v_u_xlat16_8;
        float3 _617 = v_u_xlat16_7.xyz * _615.xyz;
        v_u_xlat16_8.x = _617.x;
        v_u_xlat16_8.y = _617.y;
        v_u_xlat16_8.z = _617.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _631 = v_u_xlat16_8;
        float3 _634 = _631.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _634.x;
        v_u_xlat16_8.y = _634.y;
        v_u_xlat16_8.z = _634.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb4 = bool4(v_u_xlat16_7.xyzx.x < v_u_xlat16_10.xyzx.x, v_u_xlat16_7.xyzx.y < v_u_xlat16_10.xyzx.y, v_u_xlat16_7.xyzx.z < v_u_xlat16_10.xyzx.z, v_u_xlat16_7.xyzx.w < v_u_xlat16_10.xyzx.w).xyz;
        float _698;
        if (v_u_xlatb4.x)
        {
            _698 = v_u_xlat16_8.x;
        }
        else
        {
            _698 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _698;
        float _710;
        if (v_u_xlatb4.y)
        {
            _710 = v_u_xlat16_8.y;
        }
        else
        {
            _710 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _710;
        float _722;
        if (v_u_xlatb4.z)
        {
            _722 = v_u_xlat16_8.z;
        }
        else
        {
            _722 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _722;
        float4 _732 = v_u_xlat16_4;
        v_u_xlat16_4.x = _732.xyz.x;
        v_u_xlat16_4.y = _732.xyz.y;
        v_u_xlat16_4.z = _732.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat46 = 1.0f;
    v_u_xlat16_4.w = v_u_xlat46;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _762 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _762.x;
    v_u_xlat16_6.y = _762.y;
    v_u_xlat16_6.z = _762.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat30.x = v_u_xlat16_7.w;
    v_u_xlat46 = v_u_xlat16_4.w * v_u_xlat30.x;
    v_u_xlat16_46 = v_u_xlat46;
    float3 _785 = v_u_xlat16_46.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _785.x;
    v_u_xlat16_4.y = _785.y;
    v_u_xlat16_4.z = _785.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_46;
    v_u_xlat30.x = 2.0f;
    v_u_xlat30.x = v_u_xlat29.x * v_u_xlat30.x;
    v_u_xlat30.x = min(v_u_xlat30.x, 1.0f);
    v_u_xlat30.x = sqrt(v_u_xlat30.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat30.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat30.x = 1.0f;
    v_u_xlat44 = _UnderlaySoftness * _ScaleRatioC;
    v_u_xlat44 = v_u_xlat42 * v_u_xlat44;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat11 = v_u_xlat42 / v_u_xlat30.x;
    v_u_xlat30.x = -v_u_xlat0.x;
    v_u_xlat30.x += 0.5f;
    v_u_xlat30.x = v_u_xlat11 * v_u_xlat30.x;
    v_u_xlat44 = -0.5f;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat44 = _UnderlayDilate * _ScaleRatioC;
    v_u_xlat44 *= 0.5f;
    v_u_xlat44 = v_u_xlat11 * v_u_xlat44;
    v_u_xlat44 = -v_u_xlat44;
    v_u_xlat25 = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat30.x = _UnderlayOffsetX * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat12.x = v_u_xlat30.x / _TextureWidth;
    v_u_xlat30.x = _UnderlayOffsetY * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat26 = v_u_xlat30.x / _TextureHeight;
    v_u_xlat12.x = v_u_xlat12.x;
    v_u_xlat12.y = v_u_xlat26;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat30 = -v_u_xlat5.xy;
    v_u_xlat30 += v_u_xlat2.xy;
    v_u_xlat40 = -v_u_xlat5.xy;
    v_u_xlat40 = v_u_xlat5.zw + v_u_xlat40;
    v_u_xlat35 = v_u_xlat30 / v_u_xlat40;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat35.x;
    v_u_xlat7.w = v_u_xlat35.y;
    v_u_xlat30.x = -v_u_xlat29.x;
    v_u_xlat20 = v_u_xlat15 + v_u_xlat30.x;
    v_u_xlat34 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat16_6.x = v_u_xlat1.x;
    v_u_xlat16_6.y = v_u_xlat20;
    v_u_xlat16_6.z = v_u_xlat34;
    v_u_xlat16_6.w = v_u_xlat15;
    v_u_xlat30.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat44 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_30.x = v_u_xlat30.x;
    v_u_xlat16_30.y = v_u_xlat44;
    v_u_xlat40 = 2.0f.xx;
    v_u_xlat40 = v_u_xlat2.xy * v_u_xlat40;
    v_u_xlat13 = -v_u_xlat5.xy;
    v_u_xlat40 += v_u_xlat13;
    v_u_xlat13 = -v_u_xlat5.zw;
    float2 _1052 = v_u_xlat40 + v_u_xlat13;
    v_u_xlat1.x = _1052.x;
    v_u_xlat1.y = _1052.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _1061 = v_u_xlat16_9;
    float2 _1063 = v_u_xlat16_30 * _1061.xy;
    v_u_xlat16_9.x = _1063.x;
    v_u_xlat16_9.y = _1063.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat40 = v_u_xlat14 + v_u_xlat9;
    v_u_xlat29 = 0.25f.xx / v_u_xlat40;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat29.x;
    v_u_xlat16_1.w = v_u_xlat29.y;
    float2 _1092 = v_u_xlat12 + v_in_TEXCOORD0.xy;
    v_u_xlat0.x = _1092.x;
    v_u_xlat0.y = _1092.y;
    v_u_xlat0.z = v_in_COLOR0.w;
    v_u_xlat0.w = 0.0f;
    v_u_xlat16_11.x = v_u_xlat11;
    v_u_xlat16_11.y = v_u_xlat25;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
    v_vs_TEXCOORD3 = v_u_xlat0;
    v_vs_TEXCOORD4 = v_u_xlat16_11;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_COLOR1 = v_vs_COLOR1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR1;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD4;
static float4 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_COLOR1 : TEXCOORD1;
    float4 f_vs_TEXCOORD0 : TEXCOORD2;
    float4 f_vs_TEXCOORD1 : TEXCOORD3;
    float4 f_vs_TEXCOORD2 : TEXCOORD4;
    float4 f_vs_TEXCOORD3 : TEXCOORD5;
    float2 f_vs_TEXCOORD4 : TEXCOORD6;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat7;
static float4 f_u_xlat0;
static float4 f_u_xlat16_1;
static float4 f_u_xlat16_2;
static float2 f_u_xlat16_3;
static float f_u_xlat16_4;
static float f_u_xlat11;
static float f_u_xlat4;
static float4 f_u_xlat1;
static float4 f_u_xlat2;
static float f_u_xlat16_10;
static float2 f_u_xlat5;
static float2 f_u_xlat3;
static float2 f_u_xlat19;
static float2 f_u_xlat16_5;
static float f_u_xlat16_6;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat7 = f_vs_TEXCOORD1.x;
    f_u_xlat0.x = f_u_xlat7 * f_u_xlat16_0.x;
    f_u_xlat16_0.x = f_u_xlat0.x;
    f_u_xlat16_1.x = -f_vs_TEXCOORD1.z;
    f_u_xlat16_1.x = f_u_xlat16_0.x + f_u_xlat16_1.x;
    f_u_xlat16_1.x = max(f_u_xlat16_1.x, 0.0f);
    f_u_xlat16_1.x = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_2 = -f_vs_COLOR1;
    f_u_xlat16_2 += f_vs_COLOR0;
    f_u_xlat16_1 = f_u_xlat16_1.xxxx * f_u_xlat16_2;
    f_u_xlat16_1 += f_vs_COLOR1;
    f_u_xlat16_3.x = -f_vs_TEXCOORD1.y;
    f_u_xlat16_3.x = f_u_xlat16_0.x + f_u_xlat16_3.x;
    f_u_xlat16_3.x = max(f_u_xlat16_3.x, 0.0f);
    f_u_xlat16_3.x = min(f_u_xlat16_3.x, 1.0f);
    f_u_xlat16_0 = f_u_xlat16_1 * f_u_xlat16_3.xxxx;
    f_u_xlat16_4 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD3.xy).w;
    f_u_xlat11 = f_vs_TEXCOORD4.x;
    f_u_xlat4 = f_u_xlat11 * f_u_xlat16_4;
    f_u_xlat16_4 = f_u_xlat4;
    float3 _127 = _UnderlayColor.www * _UnderlayColor.xyz;
    f_u_xlat16_1.x = _127.x;
    f_u_xlat16_1.y = _127.y;
    f_u_xlat16_1.z = _127.z;
    f_u_xlat1.x = f_u_xlat16_1.xyz.x;
    f_u_xlat1.y = f_u_xlat16_1.xyz.y;
    f_u_xlat1.z = f_u_xlat16_1.xyz.z;
    f_u_xlat1.w = _UnderlayColor.w;
    f_u_xlat16_3.x = -f_vs_TEXCOORD4.y;
    f_u_xlat16_3.x += f_u_xlat16_4;
    f_u_xlat16_3.x = max(f_u_xlat16_3.x, 0.0f);
    f_u_xlat16_2 = min(f_u_xlat16_3.xxxx, 1.0f.xxxx);
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat16_3.x = 1.0f;
    f_u_xlat16_10 = -f_u_xlat16_0.w;
    f_u_xlat16_2 = f_u_xlat16_10.xxxx + f_u_xlat16_3.xxxx;
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 = f_u_xlat1 + f_u_xlat0;
    f_u_xlat16_0 = f_u_xlat0;
    f_u_xlat5 = -_ClipRect.xy;
    f_u_xlat5 += _ClipRect.zw;
    f_u_xlat16_3 = -f_vs_TEXCOORD2.xy;
    f_u_xlat16_3 = max(f_u_xlat16_3, f_vs_TEXCOORD2.xy);
    f_u_xlat3 = f_u_xlat16_3;
    f_u_xlat19 = -f_u_xlat3;
    f_u_xlat5 = f_u_xlat19 + f_u_xlat5;
    f_u_xlat19 = f_vs_TEXCOORD2.zw;
    f_u_xlat5 = f_u_xlat19 * f_u_xlat5;
    f_u_xlat5 = max(f_u_xlat5, 0.0f.xx);
    f_u_xlat5 = min(f_u_xlat5, 1.0f.xx);
    f_u_xlat16_5 = f_u_xlat5;
    f_u_xlat16_6 = f_u_xlat16_5.y * f_u_xlat16_5.x;
    f_u_xlat16_0 *= f_u_xlat16_6.xxxx;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 *= f_vs_TEXCOORD3.zzzz;
    f_u_xlat16_0 = f_u_xlat0;
    f_SV_Target0 = f_u_xlat16_0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR1 = stage_input.f_vs_COLOR1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(OUTLINE_ON) && !defined(UNDERLAY_ON) && defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_COLOR1 : TEXCOORD1;
    float4 v_vs_TEXCOORD0 : TEXCOORD2;
    float4 v_vs_TEXCOORD1 : TEXCOORD3;
    float4 v_vs_TEXCOORD2 : TEXCOORD4;
    float4 v_gl_Position : SV_Position;
};

static float v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat14;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat12;
static float2 v_u_xlat25;
static float2 v_u_xlat26;
static float v_u_xlat36;
static float v_u_xlat37;
static bool v_u_xlatb25;
static float4 v_u_xlat6;
static float v_u_xlat13;
static bool3 v_u_xlatb13;
static int v_u_xlati25;
static int v_u_xlati13;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static float4 v_u_xlat16_4;
static float v_u_xlat40;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_40;
static float2 v_u_xlat11;
static float2 v_u_xlat31;
static float4 v_u_xlat7;
static float v_u_xlat18;
static float v_u_xlat30;
static float v_u_xlat38;
static float2 v_u_xlat16_26;
static float2 v_u_xlat35;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0 = 0.0f;
    v_u_xlatb0 = v_u_xlat0 >= v_in_TEXCOORD0.w;
    v_u_xlat0 = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat14 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat14;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat12 = v_u_xlat3.ww;
    v_u_xlat25 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat26 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat25 += v_u_xlat26;
    v_u_xlat26 = -v_u_xlat25;
    v_u_xlat25 = max(v_u_xlat25, v_u_xlat26);
    v_u_xlat25 *= float2(_ScaleX, _ScaleY);
    v_u_xlat12 /= v_u_xlat25;
    v_u_xlat36 = dot(v_u_xlat12, v_u_xlat12);
    v_u_xlat36 = rsqrt(v_u_xlat36);
    v_u_xlat25.x = -v_in_TEXCOORD0.w;
    v_u_xlat25.x = max(v_u_xlat25.x, v_in_TEXCOORD0.w);
    v_u_xlat25.x *= _GradientScale;
    v_u_xlat37 = 1.0f;
    v_u_xlat37 += _Sharpness;
    v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
    v_u_xlat36 *= v_u_xlat25.x;
    v_u_xlat25.x = 0.0f;
    v_u_xlatb25 = v_u_xlat25.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb25)
    {
        v_u_xlat25.x = -v_u_xlat36;
        v_u_xlat25.x = max(v_u_xlat36, v_u_xlat25.x);
        v_u_xlat37 = 1.0f;
        v_u_xlat26.x = -_PerspectiveFilter;
        v_u_xlat37 += v_u_xlat26.x;
        v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat37 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat37 = rsqrt(v_u_xlat37);
        float3 _290 = v_u_xlat37.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _290.x;
        v_u_xlat4.y = _290.y;
        v_u_xlat4.z = _290.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _303 = v_u_xlat1;
        v_u_xlat1.x = _303.xy.x;
        v_u_xlat1.y = _303.xy.y;
        float3 _314 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _314.x;
        v_u_xlat5.y = _314.y;
        v_u_xlat5.z = _314.z;
        float3 _327 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _327.x;
        v_u_xlat6.y = _327.y;
        v_u_xlat6.z = _327.z;
        float4 _334 = v_u_xlat5;
        float3 _338 = _334.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _338.x;
        v_u_xlat5.y = _338.y;
        v_u_xlat5.z = _338.z;
        float3 _350 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _350.x;
        v_u_xlat6.y = _350.y;
        v_u_xlat6.z = _350.z;
        float4 _357 = v_u_xlat5;
        float3 _361 = _357.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _361.x;
        v_u_xlat5.y = _361.y;
        v_u_xlat5.z = _361.z;
        float4 _368 = v_u_xlat1;
        float3 _373 = _368.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _373.x;
        v_u_xlat1.y = _373.y;
        v_u_xlat1.w = _373.z;
        float4 _380 = v_u_xlat1;
        float3 _384 = _380.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _384.x;
        v_u_xlat1.y = _384.y;
        v_u_xlat1.w = _384.z;
        float4 _391 = v_u_xlat1;
        v_u_xlat1.x = _391.xyw.x;
        v_u_xlat1.y = _391.xyw.y;
        v_u_xlat1.w = _391.xyw.z;
        float4 _399 = v_u_xlat1;
        float3 _401 = -_399.xyw;
        v_u_xlat1.x = _401.x;
        v_u_xlat1.y = _401.y;
        v_u_xlat1.w = _401.z;
        float4 _408 = v_u_xlat1;
        float3 _413 = _408.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _413.x;
        v_u_xlat1.y = _413.y;
        v_u_xlat1.w = _413.z;
        float4 _420 = v_u_xlat1;
        v_u_xlat1.x = _420.xyw.x;
        v_u_xlat1.y = _420.xyw.y;
        v_u_xlat1.w = _420.xyw.z;
        v_u_xlat26.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat26.x = rsqrt(v_u_xlat26.x);
        float4 _438 = v_u_xlat1;
        float3 _442 = _438.xyw * v_u_xlat26.xxx;
        v_u_xlat1.x = _442.x;
        v_u_xlat1.y = _442.y;
        v_u_xlat1.w = _442.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat13 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat13, v_u_xlat1.x);
        v_u_xlat13 = -v_u_xlat25.x;
        v_u_xlat13 = v_u_xlat36 + v_u_xlat13;
        v_u_xlat1.x = v_u_xlat13 * v_u_xlat1.x;
        v_u_xlat36 = v_u_xlat1.x + v_u_xlat25.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0 *= v_u_xlat1.x;
    v_u_xlat0 += _WeightNormal;
    v_u_xlat0 /= 4.0f;
    v_u_xlat0 += _FaceDilate;
    v_u_xlat0 *= _ScaleRatioA;
    v_u_xlat0 *= 0.5f;
    v_u_xlat1.x = 1.0f;
    v_u_xlat13 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat13 = v_u_xlat36 * v_u_xlat13;
    v_u_xlat1.x = v_u_xlat13 + v_u_xlat1.x;
    v_u_xlat36 /= v_u_xlat1.x;
    v_u_xlat0 = -v_u_xlat0;
    v_u_xlat0 += 0.5f;
    v_u_xlat0 = v_u_xlat36 * v_u_xlat0;
    v_u_xlat1.x = -0.5f;
    v_u_xlat0 += v_u_xlat1.x;
    v_u_xlat1.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat1.x *= 0.5f;
    v_u_xlat1.x = v_u_xlat36 * v_u_xlat1.x;
    int4 _563 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb13.x = any(bool4(int4(0, 0, 0, 0).x != _563.x, int4(0, 0, 0, 0).y != _563.y, int4(0, 0, 0, 0).z != _563.z, int4(0, 0, 0, 0).w != _563.w));
    v_u_xlati25 = 0;
    int param = v_u_xlati25;
    v_u_xlati25 = v_op_not(param);
    v_u_xlati13 = v_u_xlatb13.x ? v_u_xlati25 : 0;
    if (v_u_xlati13 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _599 = v_u_xlat16_8;
        float3 _601 = v_u_xlat16_7.xyz * _599.xyz;
        v_u_xlat16_8.x = _601.x;
        v_u_xlat16_8.y = _601.y;
        v_u_xlat16_8.z = _601.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _615 = v_u_xlat16_8;
        float3 _618 = _615.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _618.x;
        v_u_xlat16_8.y = _618.y;
        v_u_xlat16_8.z = _618.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb13 = bool4(v_u_xlat16_7.xyzz.x < v_u_xlat16_10.xyzz.x, v_u_xlat16_7.xyzz.y < v_u_xlat16_10.xyzz.y, v_u_xlat16_7.xyzz.z < v_u_xlat16_10.xyzz.z, v_u_xlat16_7.xyzz.w < v_u_xlat16_10.xyzz.w).xyz;
        float _679;
        if (v_u_xlatb13.x)
        {
            _679 = v_u_xlat16_8.x;
        }
        else
        {
            _679 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _679;
        float _691;
        if (v_u_xlatb13.y)
        {
            _691 = v_u_xlat16_8.y;
        }
        else
        {
            _691 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _691;
        float _703;
        if (v_u_xlatb13.z)
        {
            _703 = v_u_xlat16_8.z;
        }
        else
        {
            _703 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _703;
        float4 _713 = v_u_xlat16_4;
        v_u_xlat16_4.x = _713.xyz.x;
        v_u_xlat16_4.y = _713.xyz.y;
        v_u_xlat16_4.z = _713.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat40 = v_in_COLOR0.w;
    v_u_xlat16_4.w = v_u_xlat40;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _745 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _745.x;
    v_u_xlat16_6.y = _745.y;
    v_u_xlat16_6.z = _745.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat26.x = v_u_xlat16_7.w;
    v_u_xlat40 = v_u_xlat16_4.w * v_u_xlat26.x;
    v_u_xlat16_40 = v_u_xlat40;
    float3 _768 = v_u_xlat16_40.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _768.x;
    v_u_xlat16_4.y = _768.y;
    v_u_xlat16_4.z = _768.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_40;
    v_u_xlat26.x = 2.0f;
    v_u_xlat26.x = v_u_xlat1.x * v_u_xlat26.x;
    v_u_xlat26.x = min(v_u_xlat26.x, 1.0f);
    v_u_xlat26.x = sqrt(v_u_xlat26.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat26.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat26 = -v_u_xlat5.xy;
    v_u_xlat26 += v_u_xlat2.xy;
    v_u_xlat11 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat5.zw + v_u_xlat11;
    v_u_xlat31 = v_u_xlat26 / v_u_xlat11;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat31.x;
    v_u_xlat7.w = v_u_xlat31.y;
    v_u_xlat26.x = -v_u_xlat1.x;
    v_u_xlat18 = v_u_xlat0 + v_u_xlat26.x;
    v_u_xlat30 = v_u_xlat0 + v_u_xlat1.x;
    v_u_xlat16_6.x = v_u_xlat36;
    v_u_xlat16_6.y = v_u_xlat18;
    v_u_xlat16_6.z = v_u_xlat30;
    v_u_xlat16_6.w = v_u_xlat0;
    v_u_xlat26.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat38 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_26.x = v_u_xlat26.x;
    v_u_xlat16_26.y = v_u_xlat38;
    v_u_xlat11 = 2.0f.xx;
    v_u_xlat11 = v_u_xlat2.xy * v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.zw;
    float2 _935 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat1.x = _935.x;
    v_u_xlat1.y = _935.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _944 = v_u_xlat16_9;
    float2 _946 = v_u_xlat16_26 * _944.xy;
    v_u_xlat16_9.x = _946.x;
    v_u_xlat16_9.y = _946.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat11 = v_u_xlat12 + v_u_xlat9;
    v_u_xlat25 = 0.25f.xx / v_u_xlat11;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat25.x;
    v_u_xlat16_1.w = v_u_xlat25.y;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_COLOR1 = v_vs_COLOR1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR1;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_COLOR1 : TEXCOORD1;
    float4 f_vs_TEXCOORD0 : TEXCOORD2;
    float4 f_vs_TEXCOORD1 : TEXCOORD3;
    float4 f_vs_TEXCOORD2 : TEXCOORD4;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat5;
static float f_u_xlat0;
static float4 f_u_xlat16_1;
static float4 f_u_xlat16_2;
static float2 f_u_xlat3;
static float2 f_u_xlat1;
static float2 f_u_xlat13;
static float2 f_u_xlat16_3;
static float f_u_xlat2;
static bool f_u_xlatb4;
static int f_u_xlati4;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat5 = f_vs_TEXCOORD1.x;
    f_u_xlat0 = f_u_xlat5 * f_u_xlat16_0.x;
    f_u_xlat16_0.x = f_u_xlat0;
    f_u_xlat16_1.x = -f_vs_TEXCOORD1.z;
    f_u_xlat16_1.x = f_u_xlat16_0.x + f_u_xlat16_1.x;
    f_u_xlat16_1.x = max(f_u_xlat16_1.x, 0.0f);
    f_u_xlat16_1.x = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_2 = -f_vs_COLOR1;
    f_u_xlat16_2 += f_vs_COLOR0;
    f_u_xlat16_1 = f_u_xlat16_1.xxxx * f_u_xlat16_2;
    f_u_xlat16_1 += f_vs_COLOR1;
    f_u_xlat16_2.x = -f_vs_TEXCOORD1.y;
    f_u_xlat16_2.x = f_u_xlat16_0.x + f_u_xlat16_2.x;
    f_u_xlat16_2.x = max(f_u_xlat16_2.x, 0.0f);
    f_u_xlat16_2.x = min(f_u_xlat16_2.x, 1.0f);
    f_u_xlat16_0 = f_u_xlat16_1 * f_u_xlat16_2.xxxx;
    f_u_xlat3 = -_ClipRect.xy;
    f_u_xlat3 += _ClipRect.zw;
    float2 _113 = -f_vs_TEXCOORD2.xy;
    f_u_xlat16_1.x = _113.x;
    f_u_xlat16_1.y = _113.y;
    float4 _118 = f_u_xlat16_1;
    float2 _122 = max(_118.xy, f_vs_TEXCOORD2.xy);
    f_u_xlat16_1.x = _122.x;
    f_u_xlat16_1.y = _122.y;
    f_u_xlat1 = f_u_xlat16_1.xy;
    f_u_xlat13 = -f_u_xlat1;
    f_u_xlat3 = f_u_xlat13 + f_u_xlat3;
    f_u_xlat13 = f_vs_TEXCOORD2.zw;
    f_u_xlat3 = f_u_xlat13 * f_u_xlat3;
    f_u_xlat3 = max(f_u_xlat3, 0.0f.xx);
    f_u_xlat3 = min(f_u_xlat3, 1.0f.xx);
    f_u_xlat16_3 = f_u_xlat3;
    f_u_xlat16_2.x = f_u_xlat16_3.y * f_u_xlat16_3.x;
    f_u_xlat16_0 *= f_u_xlat16_2.xxxx;
    f_u_xlat16_2.x = 0.001000000047497451305389404296875f;
    f_u_xlat16_2.x = -f_u_xlat16_2.x;
    f_u_xlat16_2.x = f_u_xlat16_0.w + f_u_xlat16_2.x;
    f_u_xlat2 = f_u_xlat16_2.x;
    f_u_xlatb4 = f_u_xlat2 < 0.0f;
    f_u_xlati4 = f_u_xlatb4 ? (-1) : 0;
    if (f_u_xlati4 != 0)
    {
        discard;
    }
    f_SV_Target0 = f_u_xlat16_0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR1 = stage_input.f_vs_COLOR1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(OUTLINE_ON) && defined(UNDERLAY_ON) && defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float _UnderlaySoftness;
float _ScaleRatioC;
float _UnderlayDilate;
float _UnderlayOffsetX;
float _TextureWidth;
float _UnderlayOffsetY;
float _TextureHeight;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _UnderlayColor;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;
static float4 v_vs_TEXCOORD3;
static float2 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD2 : TEXCOORD3;
    float4 v_vs_TEXCOORD3 : TEXCOORD4;
    float2 v_vs_TEXCOORD4 : TEXCOORD5;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat16;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat14;
static float2 v_u_xlat29;
static float2 v_u_xlat30;
static float v_u_xlat42;
static float v_u_xlat43;
static bool v_u_xlatb29;
static float4 v_u_xlat6;
static float v_u_xlat15;
static bool v_u_xlatb43;
static int v_u_xlati30;
static int v_u_xlati43;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static bool3 v_u_xlatb4;
static float4 v_u_xlat16_4;
static float v_u_xlat46;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_46;
static float v_u_xlat44;
static float v_u_xlat11;
static float v_u_xlat25;
static float2 v_u_xlat12;
static float v_u_xlat26;
static float2 v_u_xlat40;
static float2 v_u_xlat35;
static float4 v_u_xlat7;
static float v_u_xlat20;
static float v_u_xlat34;
static float2 v_u_xlat16_30;
static float2 v_u_xlat13;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;
static float2 v_u_xlat16_11;
static float4 v_vs_COLOR1;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0.x = 0.0f;
    v_u_xlatb0 = v_u_xlat0.x >= v_in_TEXCOORD0.w;
    v_u_xlat0.x = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat16 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat16;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat14 = v_u_xlat3.ww;
    v_u_xlat29 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat30 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat29 += v_u_xlat30;
    v_u_xlat30 = -v_u_xlat29;
    v_u_xlat29 = max(v_u_xlat29, v_u_xlat30);
    v_u_xlat29 *= float2(_ScaleX, _ScaleY);
    v_u_xlat14 /= v_u_xlat29;
    v_u_xlat42 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat42 = rsqrt(v_u_xlat42);
    v_u_xlat29.x = -v_in_TEXCOORD0.w;
    v_u_xlat29.x = max(v_u_xlat29.x, v_in_TEXCOORD0.w);
    v_u_xlat29.x *= _GradientScale;
    v_u_xlat43 = 1.0f;
    v_u_xlat43 += _Sharpness;
    v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
    v_u_xlat42 *= v_u_xlat29.x;
    v_u_xlat29.x = 0.0f;
    v_u_xlatb29 = v_u_xlat29.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb29)
    {
        v_u_xlat29.x = -v_u_xlat42;
        v_u_xlat29.x = max(v_u_xlat42, v_u_xlat29.x);
        v_u_xlat43 = 1.0f;
        v_u_xlat30.x = -_PerspectiveFilter;
        v_u_xlat43 += v_u_xlat30.x;
        v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat43 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat43 = rsqrt(v_u_xlat43);
        float3 _293 = v_u_xlat43.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _293.x;
        v_u_xlat4.y = _293.y;
        v_u_xlat4.z = _293.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _306 = v_u_xlat1;
        v_u_xlat1.x = _306.xy.x;
        v_u_xlat1.y = _306.xy.y;
        float3 _317 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _317.x;
        v_u_xlat5.y = _317.y;
        v_u_xlat5.z = _317.z;
        float3 _330 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _330.x;
        v_u_xlat6.y = _330.y;
        v_u_xlat6.z = _330.z;
        float4 _337 = v_u_xlat5;
        float3 _341 = _337.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _341.x;
        v_u_xlat5.y = _341.y;
        v_u_xlat5.z = _341.z;
        float3 _353 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _353.x;
        v_u_xlat6.y = _353.y;
        v_u_xlat6.z = _353.z;
        float4 _360 = v_u_xlat5;
        float3 _364 = _360.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _364.x;
        v_u_xlat5.y = _364.y;
        v_u_xlat5.z = _364.z;
        float4 _371 = v_u_xlat1;
        float3 _376 = _371.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _376.x;
        v_u_xlat1.y = _376.y;
        v_u_xlat1.w = _376.z;
        float4 _383 = v_u_xlat1;
        float3 _387 = _383.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _387.x;
        v_u_xlat1.y = _387.y;
        v_u_xlat1.w = _387.z;
        float4 _394 = v_u_xlat1;
        v_u_xlat1.x = _394.xyw.x;
        v_u_xlat1.y = _394.xyw.y;
        v_u_xlat1.w = _394.xyw.z;
        float4 _402 = v_u_xlat1;
        float3 _404 = -_402.xyw;
        v_u_xlat1.x = _404.x;
        v_u_xlat1.y = _404.y;
        v_u_xlat1.w = _404.z;
        float4 _411 = v_u_xlat1;
        float3 _416 = _411.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _416.x;
        v_u_xlat1.y = _416.y;
        v_u_xlat1.w = _416.z;
        float4 _423 = v_u_xlat1;
        v_u_xlat1.x = _423.xyw.x;
        v_u_xlat1.y = _423.xyw.y;
        v_u_xlat1.w = _423.xyw.z;
        v_u_xlat30.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat30.x = rsqrt(v_u_xlat30.x);
        float4 _441 = v_u_xlat1;
        float3 _445 = _441.xyw * v_u_xlat30.xxx;
        v_u_xlat1.x = _445.x;
        v_u_xlat1.y = _445.y;
        v_u_xlat1.w = _445.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat15 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat15, v_u_xlat1.x);
        v_u_xlat15 = -v_u_xlat29.x;
        v_u_xlat15 = v_u_xlat42 + v_u_xlat15;
        v_u_xlat1.x = v_u_xlat15 * v_u_xlat1.x;
        v_u_xlat42 = v_u_xlat1.x + v_u_xlat29.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0.x *= v_u_xlat1.x;
    v_u_xlat0.x += _WeightNormal;
    v_u_xlat0.x /= 4.0f;
    v_u_xlat0.x += _FaceDilate;
    v_u_xlat0.x *= _ScaleRatioA;
    v_u_xlat0.x *= 0.5f;
    v_u_xlat42 = v_u_xlat42;
    v_u_xlat1.x = 1.0f;
    v_u_xlat15 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat15 = v_u_xlat42 * v_u_xlat15;
    v_u_xlat1.x = v_u_xlat15 + v_u_xlat1.x;
    v_u_xlat1.x = v_u_xlat42 / v_u_xlat1.x;
    v_u_xlat15 = -v_u_xlat0.x;
    v_u_xlat15 += 0.5f;
    v_u_xlat15 = v_u_xlat1.x * v_u_xlat15;
    v_u_xlat29.x = -0.5f;
    v_u_xlat15 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat29.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat29.x *= 0.5f;
    v_u_xlat29.x = v_u_xlat1.x * v_u_xlat29.x;
    int4 _581 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb43 = any(bool4(int4(0, 0, 0, 0).x != _581.x, int4(0, 0, 0, 0).y != _581.y, int4(0, 0, 0, 0).z != _581.z, int4(0, 0, 0, 0).w != _581.w));
    v_u_xlati30 = 0;
    int param = v_u_xlati30;
    v_u_xlati30 = v_op_not(param);
    v_u_xlati43 = v_u_xlatb43 ? v_u_xlati30 : 0;
    if (v_u_xlati43 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _615 = v_u_xlat16_8;
        float3 _617 = v_u_xlat16_7.xyz * _615.xyz;
        v_u_xlat16_8.x = _617.x;
        v_u_xlat16_8.y = _617.y;
        v_u_xlat16_8.z = _617.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _631 = v_u_xlat16_8;
        float3 _634 = _631.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _634.x;
        v_u_xlat16_8.y = _634.y;
        v_u_xlat16_8.z = _634.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb4 = bool4(v_u_xlat16_7.xyzx.x < v_u_xlat16_10.xyzx.x, v_u_xlat16_7.xyzx.y < v_u_xlat16_10.xyzx.y, v_u_xlat16_7.xyzx.z < v_u_xlat16_10.xyzx.z, v_u_xlat16_7.xyzx.w < v_u_xlat16_10.xyzx.w).xyz;
        float _698;
        if (v_u_xlatb4.x)
        {
            _698 = v_u_xlat16_8.x;
        }
        else
        {
            _698 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _698;
        float _710;
        if (v_u_xlatb4.y)
        {
            _710 = v_u_xlat16_8.y;
        }
        else
        {
            _710 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _710;
        float _722;
        if (v_u_xlatb4.z)
        {
            _722 = v_u_xlat16_8.z;
        }
        else
        {
            _722 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _722;
        float4 _732 = v_u_xlat16_4;
        v_u_xlat16_4.x = _732.xyz.x;
        v_u_xlat16_4.y = _732.xyz.y;
        v_u_xlat16_4.z = _732.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat46 = 1.0f;
    v_u_xlat16_4.w = v_u_xlat46;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _762 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _762.x;
    v_u_xlat16_6.y = _762.y;
    v_u_xlat16_6.z = _762.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat30.x = v_u_xlat16_7.w;
    v_u_xlat46 = v_u_xlat16_4.w * v_u_xlat30.x;
    v_u_xlat16_46 = v_u_xlat46;
    float3 _785 = v_u_xlat16_46.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _785.x;
    v_u_xlat16_4.y = _785.y;
    v_u_xlat16_4.z = _785.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_46;
    v_u_xlat30.x = 2.0f;
    v_u_xlat30.x = v_u_xlat29.x * v_u_xlat30.x;
    v_u_xlat30.x = min(v_u_xlat30.x, 1.0f);
    v_u_xlat30.x = sqrt(v_u_xlat30.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat30.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat30.x = 1.0f;
    v_u_xlat44 = _UnderlaySoftness * _ScaleRatioC;
    v_u_xlat44 = v_u_xlat42 * v_u_xlat44;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat11 = v_u_xlat42 / v_u_xlat30.x;
    v_u_xlat30.x = -v_u_xlat0.x;
    v_u_xlat30.x += 0.5f;
    v_u_xlat30.x = v_u_xlat11 * v_u_xlat30.x;
    v_u_xlat44 = -0.5f;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat44 = _UnderlayDilate * _ScaleRatioC;
    v_u_xlat44 *= 0.5f;
    v_u_xlat44 = v_u_xlat11 * v_u_xlat44;
    v_u_xlat44 = -v_u_xlat44;
    v_u_xlat25 = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat30.x = _UnderlayOffsetX * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat12.x = v_u_xlat30.x / _TextureWidth;
    v_u_xlat30.x = _UnderlayOffsetY * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat26 = v_u_xlat30.x / _TextureHeight;
    v_u_xlat12.x = v_u_xlat12.x;
    v_u_xlat12.y = v_u_xlat26;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat30 = -v_u_xlat5.xy;
    v_u_xlat30 += v_u_xlat2.xy;
    v_u_xlat40 = -v_u_xlat5.xy;
    v_u_xlat40 = v_u_xlat5.zw + v_u_xlat40;
    v_u_xlat35 = v_u_xlat30 / v_u_xlat40;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat35.x;
    v_u_xlat7.w = v_u_xlat35.y;
    v_u_xlat30.x = -v_u_xlat29.x;
    v_u_xlat20 = v_u_xlat15 + v_u_xlat30.x;
    v_u_xlat34 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat16_6.x = v_u_xlat1.x;
    v_u_xlat16_6.y = v_u_xlat20;
    v_u_xlat16_6.z = v_u_xlat34;
    v_u_xlat16_6.w = v_u_xlat15;
    v_u_xlat30.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat44 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_30.x = v_u_xlat30.x;
    v_u_xlat16_30.y = v_u_xlat44;
    v_u_xlat40 = 2.0f.xx;
    v_u_xlat40 = v_u_xlat2.xy * v_u_xlat40;
    v_u_xlat13 = -v_u_xlat5.xy;
    v_u_xlat40 += v_u_xlat13;
    v_u_xlat13 = -v_u_xlat5.zw;
    float2 _1052 = v_u_xlat40 + v_u_xlat13;
    v_u_xlat1.x = _1052.x;
    v_u_xlat1.y = _1052.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _1061 = v_u_xlat16_9;
    float2 _1063 = v_u_xlat16_30 * _1061.xy;
    v_u_xlat16_9.x = _1063.x;
    v_u_xlat16_9.y = _1063.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat40 = v_u_xlat14 + v_u_xlat9;
    v_u_xlat29 = 0.25f.xx / v_u_xlat40;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat29.x;
    v_u_xlat16_1.w = v_u_xlat29.y;
    float2 _1092 = v_u_xlat12 + v_in_TEXCOORD0.xy;
    v_u_xlat0.x = _1092.x;
    v_u_xlat0.y = _1092.y;
    v_u_xlat0.z = v_in_COLOR0.w;
    v_u_xlat0.w = 0.0f;
    v_u_xlat16_11.x = v_u_xlat11;
    v_u_xlat16_11.y = v_u_xlat25;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
    v_vs_TEXCOORD3 = v_u_xlat0;
    v_vs_TEXCOORD4 = v_u_xlat16_11;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD4;
static float4 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD2 : TEXCOORD3;
    float4 f_vs_TEXCOORD3 : TEXCOORD4;
    float2 f_vs_TEXCOORD4 : TEXCOORD5;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat9;
static float4 f_u_xlat0;
static float3 f_u_xlat16_1;
static float4 f_u_xlat16_2;
static float f_u_xlat16_3;
static float f_u_xlat4;
static float f_u_xlat16_4;
static float4 f_u_xlat1;
static float2 f_u_xlat16_5;
static float4 f_u_xlat2;
static float f_u_xlat16_14;
static float2 f_u_xlat6;
static float2 f_u_xlat5;
static float2 f_u_xlat24;
static float2 f_u_xlat16_6;
static float f_u_xlat16_7;
static float f_u_xlat7;
static bool f_u_xlatb8;
static int f_u_xlati8;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat9 = f_vs_TEXCOORD1.x;
    f_u_xlat0.x = f_u_xlat9 * f_u_xlat16_0.x;
    f_u_xlat16_0.x = f_u_xlat0.x;
    f_u_xlat16_1.x = -f_vs_TEXCOORD1.w;
    f_u_xlat16_1.x = f_u_xlat16_0.x + f_u_xlat16_1.x;
    f_u_xlat16_1.x = max(f_u_xlat16_1.x, 0.0f);
    f_u_xlat16_2.x = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_0 = f_u_xlat16_2.xxxx * f_vs_COLOR0;
    f_u_xlat16_3 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD3.xy).w;
    f_u_xlat4 = f_vs_TEXCOORD4.x;
    f_u_xlat4 = f_u_xlat16_3 * f_u_xlat4;
    f_u_xlat16_4 = f_u_xlat4;
    f_u_xlat16_1 = _UnderlayColor.www * _UnderlayColor.xyz;
    f_u_xlat1.x = f_u_xlat16_1.x;
    f_u_xlat1.y = f_u_xlat16_1.y;
    f_u_xlat1.z = f_u_xlat16_1.z;
    f_u_xlat1.w = _UnderlayColor.w;
    f_u_xlat16_5.x = -f_vs_TEXCOORD4.y;
    f_u_xlat16_5.x = f_u_xlat16_4 + f_u_xlat16_5.x;
    f_u_xlat16_5.x = max(f_u_xlat16_5.x, 0.0f);
    f_u_xlat16_2 = min(f_u_xlat16_5.xxxx, 1.0f.xxxx);
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat16_5.x = 1.0f;
    f_u_xlat16_14 = -f_u_xlat16_0.w;
    f_u_xlat16_2 = f_u_xlat16_14.xxxx + f_u_xlat16_5.xxxx;
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 = f_u_xlat1 + f_u_xlat0;
    f_u_xlat16_0 = f_u_xlat0;
    f_u_xlat6 = -_ClipRect.xy;
    f_u_xlat6 += _ClipRect.zw;
    f_u_xlat16_5 = -f_vs_TEXCOORD2.xy;
    f_u_xlat16_5 = max(f_u_xlat16_5, f_vs_TEXCOORD2.xy);
    f_u_xlat5 = f_u_xlat16_5;
    f_u_xlat24 = -f_u_xlat5;
    f_u_xlat6 = f_u_xlat24 + f_u_xlat6;
    f_u_xlat24 = f_vs_TEXCOORD2.zw;
    f_u_xlat6 = f_u_xlat24 * f_u_xlat6;
    f_u_xlat6 = max(f_u_xlat6, 0.0f.xx);
    f_u_xlat6 = min(f_u_xlat6, 1.0f.xx);
    f_u_xlat16_6 = f_u_xlat6;
    f_u_xlat16_7 = f_u_xlat16_6.y * f_u_xlat16_6.x;
    f_u_xlat16_0 *= f_u_xlat16_7.xxxx;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 *= f_vs_TEXCOORD3.zzzz;
    f_u_xlat16_0 = f_u_xlat0;
    f_u_xlat16_7 = 0.001000000047497451305389404296875f;
    f_u_xlat16_7 = -f_u_xlat16_7;
    f_u_xlat16_7 = f_u_xlat16_0.w + f_u_xlat16_7;
    f_u_xlat7 = f_u_xlat16_7;
    f_u_xlatb8 = f_u_xlat7 < 0.0f;
    f_u_xlati8 = f_u_xlatb8 ? (-1) : 0;
    if (f_u_xlati8 != 0)
    {
        discard;
    }
    f_SV_Target0 = f_u_xlat16_0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(OUTLINE_ON) && defined(UNDERLAY_ON) && !defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float _UnderlaySoftness;
float _ScaleRatioC;
float _UnderlayDilate;
float _UnderlayOffsetX;
float _TextureWidth;
float _UnderlayOffsetY;
float _TextureHeight;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _UnderlayColor;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD3;
static float2 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_COLOR1 : TEXCOORD1;
    float4 v_vs_TEXCOORD0 : TEXCOORD2;
    float4 v_vs_TEXCOORD1 : TEXCOORD3;
    float4 v_vs_TEXCOORD3 : TEXCOORD4;
    float2 v_vs_TEXCOORD4 : TEXCOORD5;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat16;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat14;
static float2 v_u_xlat29;
static float2 v_u_xlat30;
static float v_u_xlat42;
static float v_u_xlat43;
static bool v_u_xlatb29;
static float4 v_u_xlat6;
static float v_u_xlat15;
static bool v_u_xlatb43;
static int v_u_xlati30;
static int v_u_xlati43;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static bool3 v_u_xlatb4;
static float4 v_u_xlat16_4;
static float v_u_xlat46;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_46;
static float v_u_xlat44;
static float v_u_xlat11;
static float v_u_xlat25;
static float2 v_u_xlat12;
static float v_u_xlat26;
static float2 v_u_xlat40;
static float2 v_u_xlat35;
static float4 v_u_xlat7;
static float v_u_xlat20;
static float v_u_xlat34;
static float2 v_u_xlat16_30;
static float2 v_u_xlat13;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;
static float2 v_u_xlat16_11;
static float4 v_vs_TEXCOORD2;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0.x = 0.0f;
    v_u_xlatb0 = v_u_xlat0.x >= v_in_TEXCOORD0.w;
    v_u_xlat0.x = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat16 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat16;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat14 = v_u_xlat3.ww;
    v_u_xlat29 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat30 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat29 += v_u_xlat30;
    v_u_xlat30 = -v_u_xlat29;
    v_u_xlat29 = max(v_u_xlat29, v_u_xlat30);
    v_u_xlat29 *= float2(_ScaleX, _ScaleY);
    v_u_xlat14 /= v_u_xlat29;
    v_u_xlat42 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat42 = rsqrt(v_u_xlat42);
    v_u_xlat29.x = -v_in_TEXCOORD0.w;
    v_u_xlat29.x = max(v_u_xlat29.x, v_in_TEXCOORD0.w);
    v_u_xlat29.x *= _GradientScale;
    v_u_xlat43 = 1.0f;
    v_u_xlat43 += _Sharpness;
    v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
    v_u_xlat42 *= v_u_xlat29.x;
    v_u_xlat29.x = 0.0f;
    v_u_xlatb29 = v_u_xlat29.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb29)
    {
        v_u_xlat29.x = -v_u_xlat42;
        v_u_xlat29.x = max(v_u_xlat42, v_u_xlat29.x);
        v_u_xlat43 = 1.0f;
        v_u_xlat30.x = -_PerspectiveFilter;
        v_u_xlat43 += v_u_xlat30.x;
        v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat43 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat43 = rsqrt(v_u_xlat43);
        float3 _293 = v_u_xlat43.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _293.x;
        v_u_xlat4.y = _293.y;
        v_u_xlat4.z = _293.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _306 = v_u_xlat1;
        v_u_xlat1.x = _306.xy.x;
        v_u_xlat1.y = _306.xy.y;
        float3 _317 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _317.x;
        v_u_xlat5.y = _317.y;
        v_u_xlat5.z = _317.z;
        float3 _330 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _330.x;
        v_u_xlat6.y = _330.y;
        v_u_xlat6.z = _330.z;
        float4 _337 = v_u_xlat5;
        float3 _341 = _337.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _341.x;
        v_u_xlat5.y = _341.y;
        v_u_xlat5.z = _341.z;
        float3 _353 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _353.x;
        v_u_xlat6.y = _353.y;
        v_u_xlat6.z = _353.z;
        float4 _360 = v_u_xlat5;
        float3 _364 = _360.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _364.x;
        v_u_xlat5.y = _364.y;
        v_u_xlat5.z = _364.z;
        float4 _371 = v_u_xlat1;
        float3 _376 = _371.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _376.x;
        v_u_xlat1.y = _376.y;
        v_u_xlat1.w = _376.z;
        float4 _383 = v_u_xlat1;
        float3 _387 = _383.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _387.x;
        v_u_xlat1.y = _387.y;
        v_u_xlat1.w = _387.z;
        float4 _394 = v_u_xlat1;
        v_u_xlat1.x = _394.xyw.x;
        v_u_xlat1.y = _394.xyw.y;
        v_u_xlat1.w = _394.xyw.z;
        float4 _402 = v_u_xlat1;
        float3 _404 = -_402.xyw;
        v_u_xlat1.x = _404.x;
        v_u_xlat1.y = _404.y;
        v_u_xlat1.w = _404.z;
        float4 _411 = v_u_xlat1;
        float3 _416 = _411.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _416.x;
        v_u_xlat1.y = _416.y;
        v_u_xlat1.w = _416.z;
        float4 _423 = v_u_xlat1;
        v_u_xlat1.x = _423.xyw.x;
        v_u_xlat1.y = _423.xyw.y;
        v_u_xlat1.w = _423.xyw.z;
        v_u_xlat30.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat30.x = rsqrt(v_u_xlat30.x);
        float4 _441 = v_u_xlat1;
        float3 _445 = _441.xyw * v_u_xlat30.xxx;
        v_u_xlat1.x = _445.x;
        v_u_xlat1.y = _445.y;
        v_u_xlat1.w = _445.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat15 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat15, v_u_xlat1.x);
        v_u_xlat15 = -v_u_xlat29.x;
        v_u_xlat15 = v_u_xlat42 + v_u_xlat15;
        v_u_xlat1.x = v_u_xlat15 * v_u_xlat1.x;
        v_u_xlat42 = v_u_xlat1.x + v_u_xlat29.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0.x *= v_u_xlat1.x;
    v_u_xlat0.x += _WeightNormal;
    v_u_xlat0.x /= 4.0f;
    v_u_xlat0.x += _FaceDilate;
    v_u_xlat0.x *= _ScaleRatioA;
    v_u_xlat0.x *= 0.5f;
    v_u_xlat42 = v_u_xlat42;
    v_u_xlat1.x = 1.0f;
    v_u_xlat15 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat15 = v_u_xlat42 * v_u_xlat15;
    v_u_xlat1.x = v_u_xlat15 + v_u_xlat1.x;
    v_u_xlat1.x = v_u_xlat42 / v_u_xlat1.x;
    v_u_xlat15 = -v_u_xlat0.x;
    v_u_xlat15 += 0.5f;
    v_u_xlat15 = v_u_xlat1.x * v_u_xlat15;
    v_u_xlat29.x = -0.5f;
    v_u_xlat15 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat29.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat29.x *= 0.5f;
    v_u_xlat29.x = v_u_xlat1.x * v_u_xlat29.x;
    int4 _581 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb43 = any(bool4(int4(0, 0, 0, 0).x != _581.x, int4(0, 0, 0, 0).y != _581.y, int4(0, 0, 0, 0).z != _581.z, int4(0, 0, 0, 0).w != _581.w));
    v_u_xlati30 = 0;
    int param = v_u_xlati30;
    v_u_xlati30 = v_op_not(param);
    v_u_xlati43 = v_u_xlatb43 ? v_u_xlati30 : 0;
    if (v_u_xlati43 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _615 = v_u_xlat16_8;
        float3 _617 = v_u_xlat16_7.xyz * _615.xyz;
        v_u_xlat16_8.x = _617.x;
        v_u_xlat16_8.y = _617.y;
        v_u_xlat16_8.z = _617.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _631 = v_u_xlat16_8;
        float3 _634 = _631.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _634.x;
        v_u_xlat16_8.y = _634.y;
        v_u_xlat16_8.z = _634.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb4 = bool4(v_u_xlat16_7.xyzx.x < v_u_xlat16_10.xyzx.x, v_u_xlat16_7.xyzx.y < v_u_xlat16_10.xyzx.y, v_u_xlat16_7.xyzx.z < v_u_xlat16_10.xyzx.z, v_u_xlat16_7.xyzx.w < v_u_xlat16_10.xyzx.w).xyz;
        float _698;
        if (v_u_xlatb4.x)
        {
            _698 = v_u_xlat16_8.x;
        }
        else
        {
            _698 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _698;
        float _710;
        if (v_u_xlatb4.y)
        {
            _710 = v_u_xlat16_8.y;
        }
        else
        {
            _710 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _710;
        float _722;
        if (v_u_xlatb4.z)
        {
            _722 = v_u_xlat16_8.z;
        }
        else
        {
            _722 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _722;
        float4 _732 = v_u_xlat16_4;
        v_u_xlat16_4.x = _732.xyz.x;
        v_u_xlat16_4.y = _732.xyz.y;
        v_u_xlat16_4.z = _732.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat46 = 1.0f;
    v_u_xlat16_4.w = v_u_xlat46;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _762 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _762.x;
    v_u_xlat16_6.y = _762.y;
    v_u_xlat16_6.z = _762.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat30.x = v_u_xlat16_7.w;
    v_u_xlat46 = v_u_xlat16_4.w * v_u_xlat30.x;
    v_u_xlat16_46 = v_u_xlat46;
    float3 _785 = v_u_xlat16_46.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _785.x;
    v_u_xlat16_4.y = _785.y;
    v_u_xlat16_4.z = _785.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_46;
    v_u_xlat30.x = 2.0f;
    v_u_xlat30.x = v_u_xlat29.x * v_u_xlat30.x;
    v_u_xlat30.x = min(v_u_xlat30.x, 1.0f);
    v_u_xlat30.x = sqrt(v_u_xlat30.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat30.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat30.x = 1.0f;
    v_u_xlat44 = _UnderlaySoftness * _ScaleRatioC;
    v_u_xlat44 = v_u_xlat42 * v_u_xlat44;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat11 = v_u_xlat42 / v_u_xlat30.x;
    v_u_xlat30.x = -v_u_xlat0.x;
    v_u_xlat30.x += 0.5f;
    v_u_xlat30.x = v_u_xlat11 * v_u_xlat30.x;
    v_u_xlat44 = -0.5f;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat44 = _UnderlayDilate * _ScaleRatioC;
    v_u_xlat44 *= 0.5f;
    v_u_xlat44 = v_u_xlat11 * v_u_xlat44;
    v_u_xlat44 = -v_u_xlat44;
    v_u_xlat25 = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat30.x = _UnderlayOffsetX * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat12.x = v_u_xlat30.x / _TextureWidth;
    v_u_xlat30.x = _UnderlayOffsetY * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat26 = v_u_xlat30.x / _TextureHeight;
    v_u_xlat12.x = v_u_xlat12.x;
    v_u_xlat12.y = v_u_xlat26;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat30 = -v_u_xlat5.xy;
    v_u_xlat30 += v_u_xlat2.xy;
    v_u_xlat40 = -v_u_xlat5.xy;
    v_u_xlat40 = v_u_xlat5.zw + v_u_xlat40;
    v_u_xlat35 = v_u_xlat30 / v_u_xlat40;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat35.x;
    v_u_xlat7.w = v_u_xlat35.y;
    v_u_xlat30.x = -v_u_xlat29.x;
    v_u_xlat20 = v_u_xlat15 + v_u_xlat30.x;
    v_u_xlat34 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat16_6.x = v_u_xlat1.x;
    v_u_xlat16_6.y = v_u_xlat20;
    v_u_xlat16_6.z = v_u_xlat34;
    v_u_xlat16_6.w = v_u_xlat15;
    v_u_xlat30.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat44 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_30.x = v_u_xlat30.x;
    v_u_xlat16_30.y = v_u_xlat44;
    v_u_xlat40 = 2.0f.xx;
    v_u_xlat40 = v_u_xlat2.xy * v_u_xlat40;
    v_u_xlat13 = -v_u_xlat5.xy;
    v_u_xlat40 += v_u_xlat13;
    v_u_xlat13 = -v_u_xlat5.zw;
    float2 _1052 = v_u_xlat40 + v_u_xlat13;
    v_u_xlat1.x = _1052.x;
    v_u_xlat1.y = _1052.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _1061 = v_u_xlat16_9;
    float2 _1063 = v_u_xlat16_30 * _1061.xy;
    v_u_xlat16_9.x = _1063.x;
    v_u_xlat16_9.y = _1063.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat40 = v_u_xlat14 + v_u_xlat9;
    v_u_xlat29 = 0.25f.xx / v_u_xlat40;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat29.x;
    v_u_xlat16_1.w = v_u_xlat29.y;
    float2 _1092 = v_u_xlat12 + v_in_TEXCOORD0.xy;
    v_u_xlat0.x = _1092.x;
    v_u_xlat0.y = _1092.y;
    v_u_xlat0.z = v_in_COLOR0.w;
    v_u_xlat0.w = 0.0f;
    v_u_xlat16_11.x = v_u_xlat11;
    v_u_xlat16_11.y = v_u_xlat25;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
    v_vs_TEXCOORD3 = v_u_xlat0;
    v_vs_TEXCOORD4 = v_u_xlat16_11;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_COLOR1 = v_vs_COLOR1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR1;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD4;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_COLOR1 : TEXCOORD1;
    float4 f_vs_TEXCOORD0 : TEXCOORD2;
    float4 f_vs_TEXCOORD1 : TEXCOORD3;
    float4 f_vs_TEXCOORD3 : TEXCOORD4;
    float2 f_vs_TEXCOORD4 : TEXCOORD5;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat5;
static float4 f_u_xlat0;
static float3 f_u_xlat16_1;
static float f_u_xlat16_6;
static float4 f_u_xlat16_2;
static float f_u_xlat16_3;
static float f_u_xlat8;
static float f_u_xlat3;
static float4 f_u_xlat1;
static float4 f_u_xlat2;
static float f_u_xlat16_4;
static float f_u_xlat16_9;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat5 = f_vs_TEXCOORD1.x;
    f_u_xlat0.x = f_u_xlat5 * f_u_xlat16_0.x;
    f_u_xlat16_1.x = f_u_xlat0.x;
    f_u_xlat16_6 = -f_vs_TEXCOORD1.z;
    f_u_xlat16_6 += f_u_xlat16_1.x;
    f_u_xlat16_6 = max(f_u_xlat16_6, 0.0f);
    f_u_xlat16_6 = min(f_u_xlat16_6, 1.0f);
    f_u_xlat16_0 = -f_vs_COLOR1;
    f_u_xlat16_0 += f_vs_COLOR0;
    f_u_xlat16_0 *= f_u_xlat16_6.xxxx;
    f_u_xlat16_0 += f_vs_COLOR1;
    f_u_xlat16_2.x = -f_vs_TEXCOORD1.y;
    f_u_xlat16_2.x = f_u_xlat16_1.x + f_u_xlat16_2.x;
    f_u_xlat16_2.x = max(f_u_xlat16_2.x, 0.0f);
    f_u_xlat16_2.x = min(f_u_xlat16_2.x, 1.0f);
    f_u_xlat16_0 *= f_u_xlat16_2.xxxx;
    f_u_xlat16_3 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD3.xy).w;
    f_u_xlat8 = f_vs_TEXCOORD4.x;
    f_u_xlat3 = f_u_xlat8 * f_u_xlat16_3;
    f_u_xlat16_3 = f_u_xlat3;
    f_u_xlat16_1 = _UnderlayColor.www * _UnderlayColor.xyz;
    f_u_xlat1.x = f_u_xlat16_1.x;
    f_u_xlat1.y = f_u_xlat16_1.y;
    f_u_xlat1.z = f_u_xlat16_1.z;
    f_u_xlat1.w = _UnderlayColor.w;
    f_u_xlat16_2.x = -f_vs_TEXCOORD4.y;
    f_u_xlat16_2.x += f_u_xlat16_3;
    f_u_xlat16_2.x = max(f_u_xlat16_2.x, 0.0f);
    f_u_xlat16_2 = min(f_u_xlat16_2.xxxx, 1.0f.xxxx);
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat16_4 = 1.0f;
    f_u_xlat16_9 = -f_u_xlat16_0.w;
    f_u_xlat16_2 = f_u_xlat16_9.xxxx + f_u_xlat16_4.xxxx;
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 = f_u_xlat1 + f_u_xlat0;
    f_u_xlat16_0 = f_u_xlat0;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 *= f_vs_TEXCOORD3.zzzz;
    f_u_xlat16_0 = f_u_xlat0;
    f_SV_Target0 = f_u_xlat16_0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR1 = stage_input.f_vs_COLOR1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(OUTLINE_ON) && !defined(UNDERLAY_ON) && defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_COLOR1 : TEXCOORD1;
    float4 v_vs_TEXCOORD0 : TEXCOORD2;
    float4 v_vs_TEXCOORD1 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat14;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat12;
static float2 v_u_xlat25;
static float2 v_u_xlat26;
static float v_u_xlat36;
static float v_u_xlat37;
static bool v_u_xlatb25;
static float4 v_u_xlat6;
static float v_u_xlat13;
static bool3 v_u_xlatb13;
static int v_u_xlati25;
static int v_u_xlati13;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static float4 v_u_xlat16_4;
static float v_u_xlat40;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_40;
static float2 v_u_xlat11;
static float2 v_u_xlat31;
static float4 v_u_xlat7;
static float v_u_xlat18;
static float v_u_xlat30;
static float v_u_xlat38;
static float2 v_u_xlat16_26;
static float2 v_u_xlat35;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;
static float4 v_vs_TEXCOORD2;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0 = 0.0f;
    v_u_xlatb0 = v_u_xlat0 >= v_in_TEXCOORD0.w;
    v_u_xlat0 = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat14 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat14;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat12 = v_u_xlat3.ww;
    v_u_xlat25 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat26 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat25 += v_u_xlat26;
    v_u_xlat26 = -v_u_xlat25;
    v_u_xlat25 = max(v_u_xlat25, v_u_xlat26);
    v_u_xlat25 *= float2(_ScaleX, _ScaleY);
    v_u_xlat12 /= v_u_xlat25;
    v_u_xlat36 = dot(v_u_xlat12, v_u_xlat12);
    v_u_xlat36 = rsqrt(v_u_xlat36);
    v_u_xlat25.x = -v_in_TEXCOORD0.w;
    v_u_xlat25.x = max(v_u_xlat25.x, v_in_TEXCOORD0.w);
    v_u_xlat25.x *= _GradientScale;
    v_u_xlat37 = 1.0f;
    v_u_xlat37 += _Sharpness;
    v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
    v_u_xlat36 *= v_u_xlat25.x;
    v_u_xlat25.x = 0.0f;
    v_u_xlatb25 = v_u_xlat25.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb25)
    {
        v_u_xlat25.x = -v_u_xlat36;
        v_u_xlat25.x = max(v_u_xlat36, v_u_xlat25.x);
        v_u_xlat37 = 1.0f;
        v_u_xlat26.x = -_PerspectiveFilter;
        v_u_xlat37 += v_u_xlat26.x;
        v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat37 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat37 = rsqrt(v_u_xlat37);
        float3 _290 = v_u_xlat37.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _290.x;
        v_u_xlat4.y = _290.y;
        v_u_xlat4.z = _290.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _303 = v_u_xlat1;
        v_u_xlat1.x = _303.xy.x;
        v_u_xlat1.y = _303.xy.y;
        float3 _314 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _314.x;
        v_u_xlat5.y = _314.y;
        v_u_xlat5.z = _314.z;
        float3 _327 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _327.x;
        v_u_xlat6.y = _327.y;
        v_u_xlat6.z = _327.z;
        float4 _334 = v_u_xlat5;
        float3 _338 = _334.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _338.x;
        v_u_xlat5.y = _338.y;
        v_u_xlat5.z = _338.z;
        float3 _350 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _350.x;
        v_u_xlat6.y = _350.y;
        v_u_xlat6.z = _350.z;
        float4 _357 = v_u_xlat5;
        float3 _361 = _357.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _361.x;
        v_u_xlat5.y = _361.y;
        v_u_xlat5.z = _361.z;
        float4 _368 = v_u_xlat1;
        float3 _373 = _368.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _373.x;
        v_u_xlat1.y = _373.y;
        v_u_xlat1.w = _373.z;
        float4 _380 = v_u_xlat1;
        float3 _384 = _380.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _384.x;
        v_u_xlat1.y = _384.y;
        v_u_xlat1.w = _384.z;
        float4 _391 = v_u_xlat1;
        v_u_xlat1.x = _391.xyw.x;
        v_u_xlat1.y = _391.xyw.y;
        v_u_xlat1.w = _391.xyw.z;
        float4 _399 = v_u_xlat1;
        float3 _401 = -_399.xyw;
        v_u_xlat1.x = _401.x;
        v_u_xlat1.y = _401.y;
        v_u_xlat1.w = _401.z;
        float4 _408 = v_u_xlat1;
        float3 _413 = _408.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _413.x;
        v_u_xlat1.y = _413.y;
        v_u_xlat1.w = _413.z;
        float4 _420 = v_u_xlat1;
        v_u_xlat1.x = _420.xyw.x;
        v_u_xlat1.y = _420.xyw.y;
        v_u_xlat1.w = _420.xyw.z;
        v_u_xlat26.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat26.x = rsqrt(v_u_xlat26.x);
        float4 _438 = v_u_xlat1;
        float3 _442 = _438.xyw * v_u_xlat26.xxx;
        v_u_xlat1.x = _442.x;
        v_u_xlat1.y = _442.y;
        v_u_xlat1.w = _442.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat13 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat13, v_u_xlat1.x);
        v_u_xlat13 = -v_u_xlat25.x;
        v_u_xlat13 = v_u_xlat36 + v_u_xlat13;
        v_u_xlat1.x = v_u_xlat13 * v_u_xlat1.x;
        v_u_xlat36 = v_u_xlat1.x + v_u_xlat25.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0 *= v_u_xlat1.x;
    v_u_xlat0 += _WeightNormal;
    v_u_xlat0 /= 4.0f;
    v_u_xlat0 += _FaceDilate;
    v_u_xlat0 *= _ScaleRatioA;
    v_u_xlat0 *= 0.5f;
    v_u_xlat1.x = 1.0f;
    v_u_xlat13 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat13 = v_u_xlat36 * v_u_xlat13;
    v_u_xlat1.x = v_u_xlat13 + v_u_xlat1.x;
    v_u_xlat36 /= v_u_xlat1.x;
    v_u_xlat0 = -v_u_xlat0;
    v_u_xlat0 += 0.5f;
    v_u_xlat0 = v_u_xlat36 * v_u_xlat0;
    v_u_xlat1.x = -0.5f;
    v_u_xlat0 += v_u_xlat1.x;
    v_u_xlat1.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat1.x *= 0.5f;
    v_u_xlat1.x = v_u_xlat36 * v_u_xlat1.x;
    int4 _563 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb13.x = any(bool4(int4(0, 0, 0, 0).x != _563.x, int4(0, 0, 0, 0).y != _563.y, int4(0, 0, 0, 0).z != _563.z, int4(0, 0, 0, 0).w != _563.w));
    v_u_xlati25 = 0;
    int param = v_u_xlati25;
    v_u_xlati25 = v_op_not(param);
    v_u_xlati13 = v_u_xlatb13.x ? v_u_xlati25 : 0;
    if (v_u_xlati13 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _599 = v_u_xlat16_8;
        float3 _601 = v_u_xlat16_7.xyz * _599.xyz;
        v_u_xlat16_8.x = _601.x;
        v_u_xlat16_8.y = _601.y;
        v_u_xlat16_8.z = _601.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _615 = v_u_xlat16_8;
        float3 _618 = _615.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _618.x;
        v_u_xlat16_8.y = _618.y;
        v_u_xlat16_8.z = _618.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb13 = bool4(v_u_xlat16_7.xyzz.x < v_u_xlat16_10.xyzz.x, v_u_xlat16_7.xyzz.y < v_u_xlat16_10.xyzz.y, v_u_xlat16_7.xyzz.z < v_u_xlat16_10.xyzz.z, v_u_xlat16_7.xyzz.w < v_u_xlat16_10.xyzz.w).xyz;
        float _679;
        if (v_u_xlatb13.x)
        {
            _679 = v_u_xlat16_8.x;
        }
        else
        {
            _679 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _679;
        float _691;
        if (v_u_xlatb13.y)
        {
            _691 = v_u_xlat16_8.y;
        }
        else
        {
            _691 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _691;
        float _703;
        if (v_u_xlatb13.z)
        {
            _703 = v_u_xlat16_8.z;
        }
        else
        {
            _703 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _703;
        float4 _713 = v_u_xlat16_4;
        v_u_xlat16_4.x = _713.xyz.x;
        v_u_xlat16_4.y = _713.xyz.y;
        v_u_xlat16_4.z = _713.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat40 = v_in_COLOR0.w;
    v_u_xlat16_4.w = v_u_xlat40;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _745 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _745.x;
    v_u_xlat16_6.y = _745.y;
    v_u_xlat16_6.z = _745.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat26.x = v_u_xlat16_7.w;
    v_u_xlat40 = v_u_xlat16_4.w * v_u_xlat26.x;
    v_u_xlat16_40 = v_u_xlat40;
    float3 _768 = v_u_xlat16_40.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _768.x;
    v_u_xlat16_4.y = _768.y;
    v_u_xlat16_4.z = _768.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_40;
    v_u_xlat26.x = 2.0f;
    v_u_xlat26.x = v_u_xlat1.x * v_u_xlat26.x;
    v_u_xlat26.x = min(v_u_xlat26.x, 1.0f);
    v_u_xlat26.x = sqrt(v_u_xlat26.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat26.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat26 = -v_u_xlat5.xy;
    v_u_xlat26 += v_u_xlat2.xy;
    v_u_xlat11 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat5.zw + v_u_xlat11;
    v_u_xlat31 = v_u_xlat26 / v_u_xlat11;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat31.x;
    v_u_xlat7.w = v_u_xlat31.y;
    v_u_xlat26.x = -v_u_xlat1.x;
    v_u_xlat18 = v_u_xlat0 + v_u_xlat26.x;
    v_u_xlat30 = v_u_xlat0 + v_u_xlat1.x;
    v_u_xlat16_6.x = v_u_xlat36;
    v_u_xlat16_6.y = v_u_xlat18;
    v_u_xlat16_6.z = v_u_xlat30;
    v_u_xlat16_6.w = v_u_xlat0;
    v_u_xlat26.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat38 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_26.x = v_u_xlat26.x;
    v_u_xlat16_26.y = v_u_xlat38;
    v_u_xlat11 = 2.0f.xx;
    v_u_xlat11 = v_u_xlat2.xy * v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.zw;
    float2 _935 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat1.x = _935.x;
    v_u_xlat1.y = _935.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _944 = v_u_xlat16_9;
    float2 _946 = v_u_xlat16_26 * _944.xy;
    v_u_xlat16_9.x = _946.x;
    v_u_xlat16_9.y = _946.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat11 = v_u_xlat12 + v_u_xlat9;
    v_u_xlat25 = 0.25f.xx / v_u_xlat11;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat25.x;
    v_u_xlat16_1.w = v_u_xlat25.y;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_COLOR1 = v_vs_COLOR1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_COLOR1 : TEXCOORD1;
    float4 f_vs_TEXCOORD0 : TEXCOORD2;
    float4 f_vs_TEXCOORD1 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat4;
static float f_u_xlat0;
static float4 f_u_xlat16_1;
static float4 f_u_xlat16_2;
static float f_u_xlat1;
static bool f_u_xlatb3;
static int f_u_xlati3;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat4 = f_vs_TEXCOORD1.x;
    f_u_xlat0 = f_u_xlat4 * f_u_xlat16_0.x;
    f_u_xlat16_0.x = f_u_xlat0;
    f_u_xlat16_1.x = -f_vs_TEXCOORD1.z;
    f_u_xlat16_1.x = f_u_xlat16_0.x + f_u_xlat16_1.x;
    f_u_xlat16_1.x = max(f_u_xlat16_1.x, 0.0f);
    f_u_xlat16_1.x = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_2 = -f_vs_COLOR1;
    f_u_xlat16_2 += f_vs_COLOR0;
    f_u_xlat16_1 = f_u_xlat16_1.xxxx * f_u_xlat16_2;
    f_u_xlat16_1 += f_vs_COLOR1;
    f_u_xlat16_2.x = -f_vs_TEXCOORD1.y;
    f_u_xlat16_2.x = f_u_xlat16_0.x + f_u_xlat16_2.x;
    f_u_xlat16_2.x = max(f_u_xlat16_2.x, 0.0f);
    f_u_xlat16_2.x = min(f_u_xlat16_2.x, 1.0f);
    f_u_xlat16_0 = f_u_xlat16_1 * f_u_xlat16_2.xxxx;
    f_u_xlat16_1.x = 0.001000000047497451305389404296875f;
    f_u_xlat16_1.x = -f_u_xlat16_1.x;
    f_u_xlat16_1.x = f_u_xlat16_0.w + f_u_xlat16_1.x;
    f_u_xlat1 = f_u_xlat16_1.x;
    f_u_xlatb3 = f_u_xlat1 < 0.0f;
    f_u_xlati3 = f_u_xlatb3 ? (-1) : 0;
    if (f_u_xlati3 != 0)
    {
        discard;
    }
    f_SV_Target0 = f_u_xlat16_0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR1 = stage_input.f_vs_COLOR1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(OUTLINE_ON) && defined(UNDERLAY_ON) && defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float _UnderlaySoftness;
float _ScaleRatioC;
float _UnderlayDilate;
float _UnderlayOffsetX;
float _TextureWidth;
float _UnderlayOffsetY;
float _TextureHeight;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _UnderlayColor;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD3;
static float2 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD3 : TEXCOORD3;
    float2 v_vs_TEXCOORD4 : TEXCOORD4;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat16;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat14;
static float2 v_u_xlat29;
static float2 v_u_xlat30;
static float v_u_xlat42;
static float v_u_xlat43;
static bool v_u_xlatb29;
static float4 v_u_xlat6;
static float v_u_xlat15;
static bool v_u_xlatb43;
static int v_u_xlati30;
static int v_u_xlati43;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static bool3 v_u_xlatb4;
static float4 v_u_xlat16_4;
static float v_u_xlat46;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_46;
static float v_u_xlat44;
static float v_u_xlat11;
static float v_u_xlat25;
static float2 v_u_xlat12;
static float v_u_xlat26;
static float2 v_u_xlat40;
static float2 v_u_xlat35;
static float4 v_u_xlat7;
static float v_u_xlat20;
static float v_u_xlat34;
static float2 v_u_xlat16_30;
static float2 v_u_xlat13;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;
static float2 v_u_xlat16_11;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD2;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0.x = 0.0f;
    v_u_xlatb0 = v_u_xlat0.x >= v_in_TEXCOORD0.w;
    v_u_xlat0.x = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat16 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat16;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat14 = v_u_xlat3.ww;
    v_u_xlat29 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat30 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat29 += v_u_xlat30;
    v_u_xlat30 = -v_u_xlat29;
    v_u_xlat29 = max(v_u_xlat29, v_u_xlat30);
    v_u_xlat29 *= float2(_ScaleX, _ScaleY);
    v_u_xlat14 /= v_u_xlat29;
    v_u_xlat42 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat42 = rsqrt(v_u_xlat42);
    v_u_xlat29.x = -v_in_TEXCOORD0.w;
    v_u_xlat29.x = max(v_u_xlat29.x, v_in_TEXCOORD0.w);
    v_u_xlat29.x *= _GradientScale;
    v_u_xlat43 = 1.0f;
    v_u_xlat43 += _Sharpness;
    v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
    v_u_xlat42 *= v_u_xlat29.x;
    v_u_xlat29.x = 0.0f;
    v_u_xlatb29 = v_u_xlat29.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb29)
    {
        v_u_xlat29.x = -v_u_xlat42;
        v_u_xlat29.x = max(v_u_xlat42, v_u_xlat29.x);
        v_u_xlat43 = 1.0f;
        v_u_xlat30.x = -_PerspectiveFilter;
        v_u_xlat43 += v_u_xlat30.x;
        v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat43 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat43 = rsqrt(v_u_xlat43);
        float3 _293 = v_u_xlat43.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _293.x;
        v_u_xlat4.y = _293.y;
        v_u_xlat4.z = _293.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _306 = v_u_xlat1;
        v_u_xlat1.x = _306.xy.x;
        v_u_xlat1.y = _306.xy.y;
        float3 _317 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _317.x;
        v_u_xlat5.y = _317.y;
        v_u_xlat5.z = _317.z;
        float3 _330 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _330.x;
        v_u_xlat6.y = _330.y;
        v_u_xlat6.z = _330.z;
        float4 _337 = v_u_xlat5;
        float3 _341 = _337.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _341.x;
        v_u_xlat5.y = _341.y;
        v_u_xlat5.z = _341.z;
        float3 _353 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _353.x;
        v_u_xlat6.y = _353.y;
        v_u_xlat6.z = _353.z;
        float4 _360 = v_u_xlat5;
        float3 _364 = _360.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _364.x;
        v_u_xlat5.y = _364.y;
        v_u_xlat5.z = _364.z;
        float4 _371 = v_u_xlat1;
        float3 _376 = _371.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _376.x;
        v_u_xlat1.y = _376.y;
        v_u_xlat1.w = _376.z;
        float4 _383 = v_u_xlat1;
        float3 _387 = _383.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _387.x;
        v_u_xlat1.y = _387.y;
        v_u_xlat1.w = _387.z;
        float4 _394 = v_u_xlat1;
        v_u_xlat1.x = _394.xyw.x;
        v_u_xlat1.y = _394.xyw.y;
        v_u_xlat1.w = _394.xyw.z;
        float4 _402 = v_u_xlat1;
        float3 _404 = -_402.xyw;
        v_u_xlat1.x = _404.x;
        v_u_xlat1.y = _404.y;
        v_u_xlat1.w = _404.z;
        float4 _411 = v_u_xlat1;
        float3 _416 = _411.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _416.x;
        v_u_xlat1.y = _416.y;
        v_u_xlat1.w = _416.z;
        float4 _423 = v_u_xlat1;
        v_u_xlat1.x = _423.xyw.x;
        v_u_xlat1.y = _423.xyw.y;
        v_u_xlat1.w = _423.xyw.z;
        v_u_xlat30.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat30.x = rsqrt(v_u_xlat30.x);
        float4 _441 = v_u_xlat1;
        float3 _445 = _441.xyw * v_u_xlat30.xxx;
        v_u_xlat1.x = _445.x;
        v_u_xlat1.y = _445.y;
        v_u_xlat1.w = _445.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat15 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat15, v_u_xlat1.x);
        v_u_xlat15 = -v_u_xlat29.x;
        v_u_xlat15 = v_u_xlat42 + v_u_xlat15;
        v_u_xlat1.x = v_u_xlat15 * v_u_xlat1.x;
        v_u_xlat42 = v_u_xlat1.x + v_u_xlat29.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0.x *= v_u_xlat1.x;
    v_u_xlat0.x += _WeightNormal;
    v_u_xlat0.x /= 4.0f;
    v_u_xlat0.x += _FaceDilate;
    v_u_xlat0.x *= _ScaleRatioA;
    v_u_xlat0.x *= 0.5f;
    v_u_xlat42 = v_u_xlat42;
    v_u_xlat1.x = 1.0f;
    v_u_xlat15 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat15 = v_u_xlat42 * v_u_xlat15;
    v_u_xlat1.x = v_u_xlat15 + v_u_xlat1.x;
    v_u_xlat1.x = v_u_xlat42 / v_u_xlat1.x;
    v_u_xlat15 = -v_u_xlat0.x;
    v_u_xlat15 += 0.5f;
    v_u_xlat15 = v_u_xlat1.x * v_u_xlat15;
    v_u_xlat29.x = -0.5f;
    v_u_xlat15 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat29.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat29.x *= 0.5f;
    v_u_xlat29.x = v_u_xlat1.x * v_u_xlat29.x;
    int4 _581 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb43 = any(bool4(int4(0, 0, 0, 0).x != _581.x, int4(0, 0, 0, 0).y != _581.y, int4(0, 0, 0, 0).z != _581.z, int4(0, 0, 0, 0).w != _581.w));
    v_u_xlati30 = 0;
    int param = v_u_xlati30;
    v_u_xlati30 = v_op_not(param);
    v_u_xlati43 = v_u_xlatb43 ? v_u_xlati30 : 0;
    if (v_u_xlati43 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _615 = v_u_xlat16_8;
        float3 _617 = v_u_xlat16_7.xyz * _615.xyz;
        v_u_xlat16_8.x = _617.x;
        v_u_xlat16_8.y = _617.y;
        v_u_xlat16_8.z = _617.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _631 = v_u_xlat16_8;
        float3 _634 = _631.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _634.x;
        v_u_xlat16_8.y = _634.y;
        v_u_xlat16_8.z = _634.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb4 = bool4(v_u_xlat16_7.xyzx.x < v_u_xlat16_10.xyzx.x, v_u_xlat16_7.xyzx.y < v_u_xlat16_10.xyzx.y, v_u_xlat16_7.xyzx.z < v_u_xlat16_10.xyzx.z, v_u_xlat16_7.xyzx.w < v_u_xlat16_10.xyzx.w).xyz;
        float _698;
        if (v_u_xlatb4.x)
        {
            _698 = v_u_xlat16_8.x;
        }
        else
        {
            _698 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _698;
        float _710;
        if (v_u_xlatb4.y)
        {
            _710 = v_u_xlat16_8.y;
        }
        else
        {
            _710 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _710;
        float _722;
        if (v_u_xlatb4.z)
        {
            _722 = v_u_xlat16_8.z;
        }
        else
        {
            _722 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _722;
        float4 _732 = v_u_xlat16_4;
        v_u_xlat16_4.x = _732.xyz.x;
        v_u_xlat16_4.y = _732.xyz.y;
        v_u_xlat16_4.z = _732.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat46 = 1.0f;
    v_u_xlat16_4.w = v_u_xlat46;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _762 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _762.x;
    v_u_xlat16_6.y = _762.y;
    v_u_xlat16_6.z = _762.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat30.x = v_u_xlat16_7.w;
    v_u_xlat46 = v_u_xlat16_4.w * v_u_xlat30.x;
    v_u_xlat16_46 = v_u_xlat46;
    float3 _785 = v_u_xlat16_46.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _785.x;
    v_u_xlat16_4.y = _785.y;
    v_u_xlat16_4.z = _785.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_46;
    v_u_xlat30.x = 2.0f;
    v_u_xlat30.x = v_u_xlat29.x * v_u_xlat30.x;
    v_u_xlat30.x = min(v_u_xlat30.x, 1.0f);
    v_u_xlat30.x = sqrt(v_u_xlat30.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat30.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat30.x = 1.0f;
    v_u_xlat44 = _UnderlaySoftness * _ScaleRatioC;
    v_u_xlat44 = v_u_xlat42 * v_u_xlat44;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat11 = v_u_xlat42 / v_u_xlat30.x;
    v_u_xlat30.x = -v_u_xlat0.x;
    v_u_xlat30.x += 0.5f;
    v_u_xlat30.x = v_u_xlat11 * v_u_xlat30.x;
    v_u_xlat44 = -0.5f;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat44 = _UnderlayDilate * _ScaleRatioC;
    v_u_xlat44 *= 0.5f;
    v_u_xlat44 = v_u_xlat11 * v_u_xlat44;
    v_u_xlat44 = -v_u_xlat44;
    v_u_xlat25 = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat30.x = _UnderlayOffsetX * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat12.x = v_u_xlat30.x / _TextureWidth;
    v_u_xlat30.x = _UnderlayOffsetY * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat26 = v_u_xlat30.x / _TextureHeight;
    v_u_xlat12.x = v_u_xlat12.x;
    v_u_xlat12.y = v_u_xlat26;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat30 = -v_u_xlat5.xy;
    v_u_xlat30 += v_u_xlat2.xy;
    v_u_xlat40 = -v_u_xlat5.xy;
    v_u_xlat40 = v_u_xlat5.zw + v_u_xlat40;
    v_u_xlat35 = v_u_xlat30 / v_u_xlat40;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat35.x;
    v_u_xlat7.w = v_u_xlat35.y;
    v_u_xlat30.x = -v_u_xlat29.x;
    v_u_xlat20 = v_u_xlat15 + v_u_xlat30.x;
    v_u_xlat34 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat16_6.x = v_u_xlat1.x;
    v_u_xlat16_6.y = v_u_xlat20;
    v_u_xlat16_6.z = v_u_xlat34;
    v_u_xlat16_6.w = v_u_xlat15;
    v_u_xlat30.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat44 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_30.x = v_u_xlat30.x;
    v_u_xlat16_30.y = v_u_xlat44;
    v_u_xlat40 = 2.0f.xx;
    v_u_xlat40 = v_u_xlat2.xy * v_u_xlat40;
    v_u_xlat13 = -v_u_xlat5.xy;
    v_u_xlat40 += v_u_xlat13;
    v_u_xlat13 = -v_u_xlat5.zw;
    float2 _1052 = v_u_xlat40 + v_u_xlat13;
    v_u_xlat1.x = _1052.x;
    v_u_xlat1.y = _1052.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _1061 = v_u_xlat16_9;
    float2 _1063 = v_u_xlat16_30 * _1061.xy;
    v_u_xlat16_9.x = _1063.x;
    v_u_xlat16_9.y = _1063.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat40 = v_u_xlat14 + v_u_xlat9;
    v_u_xlat29 = 0.25f.xx / v_u_xlat40;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat29.x;
    v_u_xlat16_1.w = v_u_xlat29.y;
    float2 _1092 = v_u_xlat12 + v_in_TEXCOORD0.xy;
    v_u_xlat0.x = _1092.x;
    v_u_xlat0.y = _1092.y;
    v_u_xlat0.z = v_in_COLOR0.w;
    v_u_xlat0.w = 0.0f;
    v_u_xlat16_11.x = v_u_xlat11;
    v_u_xlat16_11.y = v_u_xlat25;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
    v_vs_TEXCOORD3 = v_u_xlat0;
    v_vs_TEXCOORD4 = v_u_xlat16_11;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD4;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD3 : TEXCOORD3;
    float2 f_vs_TEXCOORD4 : TEXCOORD4;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat6;
static float4 f_u_xlat0;
static float3 f_u_xlat16_1;
static float f_u_xlat16_7;
static float4 f_u_xlat16_2;
static float f_u_xlat16_3;
static float f_u_xlat9;
static float f_u_xlat3;
static float4 f_u_xlat1;
static float f_u_xlat16_4;
static float4 f_u_xlat2;
static float f_u_xlat16_10;
static float f_u_xlat4;
static bool f_u_xlatb5;
static int f_u_xlati5;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat6 = f_vs_TEXCOORD1.x;
    f_u_xlat0.x = f_u_xlat6 * f_u_xlat16_0.x;
    f_u_xlat16_1.x = f_u_xlat0.x;
    f_u_xlat16_7 = -f_vs_TEXCOORD1.w;
    f_u_xlat16_1.x = f_u_xlat16_7 + f_u_xlat16_1.x;
    f_u_xlat16_1.x = max(f_u_xlat16_1.x, 0.0f);
    f_u_xlat16_2.x = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_0 = f_u_xlat16_2.xxxx * f_vs_COLOR0;
    f_u_xlat16_3 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD3.xy).w;
    f_u_xlat9 = f_vs_TEXCOORD4.x;
    f_u_xlat3 = f_u_xlat9 * f_u_xlat16_3;
    f_u_xlat16_3 = f_u_xlat3;
    f_u_xlat16_1 = _UnderlayColor.www * _UnderlayColor.xyz;
    f_u_xlat1.x = f_u_xlat16_1.x;
    f_u_xlat1.y = f_u_xlat16_1.y;
    f_u_xlat1.z = f_u_xlat16_1.z;
    f_u_xlat1.w = _UnderlayColor.w;
    f_u_xlat16_4 = -f_vs_TEXCOORD4.y;
    f_u_xlat16_4 = f_u_xlat16_3 + f_u_xlat16_4;
    f_u_xlat16_4 = max(f_u_xlat16_4, 0.0f);
    f_u_xlat16_2 = min(f_u_xlat16_4.xxxx, 1.0f.xxxx);
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat16_4 = 1.0f;
    f_u_xlat16_10 = -f_u_xlat16_0.w;
    f_u_xlat16_2 = f_u_xlat16_10.xxxx + f_u_xlat16_4.xxxx;
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 = f_u_xlat1 + f_u_xlat0;
    f_u_xlat16_0 = f_u_xlat0;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 *= f_vs_TEXCOORD3.zzzz;
    f_u_xlat16_0 = f_u_xlat0;
    f_u_xlat16_4 = 0.001000000047497451305389404296875f;
    f_u_xlat16_4 = -f_u_xlat16_4;
    f_u_xlat16_4 = f_u_xlat16_0.w + f_u_xlat16_4;
    f_u_xlat4 = f_u_xlat16_4;
    f_u_xlatb5 = f_u_xlat4 < 0.0f;
    f_u_xlati5 = f_u_xlatb5 ? (-1) : 0;
    if (f_u_xlati5 != 0)
    {
        discard;
    }
    f_SV_Target0 = f_u_xlat16_0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(OUTLINE_ON) && !defined(UNDERLAY_ON) && !defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_COLOR1 : TEXCOORD1;
    float4 v_vs_TEXCOORD0 : TEXCOORD2;
    float4 v_vs_TEXCOORD1 : TEXCOORD3;
    float4 v_vs_TEXCOORD2 : TEXCOORD4;
    float4 v_gl_Position : SV_Position;
};

static float v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat14;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat12;
static float2 v_u_xlat25;
static float2 v_u_xlat26;
static float v_u_xlat36;
static float v_u_xlat37;
static bool v_u_xlatb25;
static float4 v_u_xlat6;
static float v_u_xlat13;
static bool3 v_u_xlatb13;
static int v_u_xlati25;
static int v_u_xlati13;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static float4 v_u_xlat16_4;
static float v_u_xlat40;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_40;
static float2 v_u_xlat11;
static float2 v_u_xlat31;
static float4 v_u_xlat7;
static float v_u_xlat18;
static float v_u_xlat30;
static float v_u_xlat38;
static float2 v_u_xlat16_26;
static float2 v_u_xlat35;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0 = 0.0f;
    v_u_xlatb0 = v_u_xlat0 >= v_in_TEXCOORD0.w;
    v_u_xlat0 = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat14 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat14;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat12 = v_u_xlat3.ww;
    v_u_xlat25 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat26 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat25 += v_u_xlat26;
    v_u_xlat26 = -v_u_xlat25;
    v_u_xlat25 = max(v_u_xlat25, v_u_xlat26);
    v_u_xlat25 *= float2(_ScaleX, _ScaleY);
    v_u_xlat12 /= v_u_xlat25;
    v_u_xlat36 = dot(v_u_xlat12, v_u_xlat12);
    v_u_xlat36 = rsqrt(v_u_xlat36);
    v_u_xlat25.x = -v_in_TEXCOORD0.w;
    v_u_xlat25.x = max(v_u_xlat25.x, v_in_TEXCOORD0.w);
    v_u_xlat25.x *= _GradientScale;
    v_u_xlat37 = 1.0f;
    v_u_xlat37 += _Sharpness;
    v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
    v_u_xlat36 *= v_u_xlat25.x;
    v_u_xlat25.x = 0.0f;
    v_u_xlatb25 = v_u_xlat25.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb25)
    {
        v_u_xlat25.x = -v_u_xlat36;
        v_u_xlat25.x = max(v_u_xlat36, v_u_xlat25.x);
        v_u_xlat37 = 1.0f;
        v_u_xlat26.x = -_PerspectiveFilter;
        v_u_xlat37 += v_u_xlat26.x;
        v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat37 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat37 = rsqrt(v_u_xlat37);
        float3 _290 = v_u_xlat37.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _290.x;
        v_u_xlat4.y = _290.y;
        v_u_xlat4.z = _290.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _303 = v_u_xlat1;
        v_u_xlat1.x = _303.xy.x;
        v_u_xlat1.y = _303.xy.y;
        float3 _314 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _314.x;
        v_u_xlat5.y = _314.y;
        v_u_xlat5.z = _314.z;
        float3 _327 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _327.x;
        v_u_xlat6.y = _327.y;
        v_u_xlat6.z = _327.z;
        float4 _334 = v_u_xlat5;
        float3 _338 = _334.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _338.x;
        v_u_xlat5.y = _338.y;
        v_u_xlat5.z = _338.z;
        float3 _350 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _350.x;
        v_u_xlat6.y = _350.y;
        v_u_xlat6.z = _350.z;
        float4 _357 = v_u_xlat5;
        float3 _361 = _357.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _361.x;
        v_u_xlat5.y = _361.y;
        v_u_xlat5.z = _361.z;
        float4 _368 = v_u_xlat1;
        float3 _373 = _368.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _373.x;
        v_u_xlat1.y = _373.y;
        v_u_xlat1.w = _373.z;
        float4 _380 = v_u_xlat1;
        float3 _384 = _380.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _384.x;
        v_u_xlat1.y = _384.y;
        v_u_xlat1.w = _384.z;
        float4 _391 = v_u_xlat1;
        v_u_xlat1.x = _391.xyw.x;
        v_u_xlat1.y = _391.xyw.y;
        v_u_xlat1.w = _391.xyw.z;
        float4 _399 = v_u_xlat1;
        float3 _401 = -_399.xyw;
        v_u_xlat1.x = _401.x;
        v_u_xlat1.y = _401.y;
        v_u_xlat1.w = _401.z;
        float4 _408 = v_u_xlat1;
        float3 _413 = _408.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _413.x;
        v_u_xlat1.y = _413.y;
        v_u_xlat1.w = _413.z;
        float4 _420 = v_u_xlat1;
        v_u_xlat1.x = _420.xyw.x;
        v_u_xlat1.y = _420.xyw.y;
        v_u_xlat1.w = _420.xyw.z;
        v_u_xlat26.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat26.x = rsqrt(v_u_xlat26.x);
        float4 _438 = v_u_xlat1;
        float3 _442 = _438.xyw * v_u_xlat26.xxx;
        v_u_xlat1.x = _442.x;
        v_u_xlat1.y = _442.y;
        v_u_xlat1.w = _442.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat13 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat13, v_u_xlat1.x);
        v_u_xlat13 = -v_u_xlat25.x;
        v_u_xlat13 = v_u_xlat36 + v_u_xlat13;
        v_u_xlat1.x = v_u_xlat13 * v_u_xlat1.x;
        v_u_xlat36 = v_u_xlat1.x + v_u_xlat25.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0 *= v_u_xlat1.x;
    v_u_xlat0 += _WeightNormal;
    v_u_xlat0 /= 4.0f;
    v_u_xlat0 += _FaceDilate;
    v_u_xlat0 *= _ScaleRatioA;
    v_u_xlat0 *= 0.5f;
    v_u_xlat1.x = 1.0f;
    v_u_xlat13 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat13 = v_u_xlat36 * v_u_xlat13;
    v_u_xlat1.x = v_u_xlat13 + v_u_xlat1.x;
    v_u_xlat36 /= v_u_xlat1.x;
    v_u_xlat0 = -v_u_xlat0;
    v_u_xlat0 += 0.5f;
    v_u_xlat0 = v_u_xlat36 * v_u_xlat0;
    v_u_xlat1.x = -0.5f;
    v_u_xlat0 += v_u_xlat1.x;
    v_u_xlat1.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat1.x *= 0.5f;
    v_u_xlat1.x = v_u_xlat36 * v_u_xlat1.x;
    int4 _563 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb13.x = any(bool4(int4(0, 0, 0, 0).x != _563.x, int4(0, 0, 0, 0).y != _563.y, int4(0, 0, 0, 0).z != _563.z, int4(0, 0, 0, 0).w != _563.w));
    v_u_xlati25 = 0;
    int param = v_u_xlati25;
    v_u_xlati25 = v_op_not(param);
    v_u_xlati13 = v_u_xlatb13.x ? v_u_xlati25 : 0;
    if (v_u_xlati13 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _599 = v_u_xlat16_8;
        float3 _601 = v_u_xlat16_7.xyz * _599.xyz;
        v_u_xlat16_8.x = _601.x;
        v_u_xlat16_8.y = _601.y;
        v_u_xlat16_8.z = _601.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _615 = v_u_xlat16_8;
        float3 _618 = _615.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _618.x;
        v_u_xlat16_8.y = _618.y;
        v_u_xlat16_8.z = _618.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb13 = bool4(v_u_xlat16_7.xyzz.x < v_u_xlat16_10.xyzz.x, v_u_xlat16_7.xyzz.y < v_u_xlat16_10.xyzz.y, v_u_xlat16_7.xyzz.z < v_u_xlat16_10.xyzz.z, v_u_xlat16_7.xyzz.w < v_u_xlat16_10.xyzz.w).xyz;
        float _679;
        if (v_u_xlatb13.x)
        {
            _679 = v_u_xlat16_8.x;
        }
        else
        {
            _679 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _679;
        float _691;
        if (v_u_xlatb13.y)
        {
            _691 = v_u_xlat16_8.y;
        }
        else
        {
            _691 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _691;
        float _703;
        if (v_u_xlatb13.z)
        {
            _703 = v_u_xlat16_8.z;
        }
        else
        {
            _703 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _703;
        float4 _713 = v_u_xlat16_4;
        v_u_xlat16_4.x = _713.xyz.x;
        v_u_xlat16_4.y = _713.xyz.y;
        v_u_xlat16_4.z = _713.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat40 = v_in_COLOR0.w;
    v_u_xlat16_4.w = v_u_xlat40;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _745 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _745.x;
    v_u_xlat16_6.y = _745.y;
    v_u_xlat16_6.z = _745.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat26.x = v_u_xlat16_7.w;
    v_u_xlat40 = v_u_xlat16_4.w * v_u_xlat26.x;
    v_u_xlat16_40 = v_u_xlat40;
    float3 _768 = v_u_xlat16_40.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _768.x;
    v_u_xlat16_4.y = _768.y;
    v_u_xlat16_4.z = _768.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_40;
    v_u_xlat26.x = 2.0f;
    v_u_xlat26.x = v_u_xlat1.x * v_u_xlat26.x;
    v_u_xlat26.x = min(v_u_xlat26.x, 1.0f);
    v_u_xlat26.x = sqrt(v_u_xlat26.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat26.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat26 = -v_u_xlat5.xy;
    v_u_xlat26 += v_u_xlat2.xy;
    v_u_xlat11 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat5.zw + v_u_xlat11;
    v_u_xlat31 = v_u_xlat26 / v_u_xlat11;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat31.x;
    v_u_xlat7.w = v_u_xlat31.y;
    v_u_xlat26.x = -v_u_xlat1.x;
    v_u_xlat18 = v_u_xlat0 + v_u_xlat26.x;
    v_u_xlat30 = v_u_xlat0 + v_u_xlat1.x;
    v_u_xlat16_6.x = v_u_xlat36;
    v_u_xlat16_6.y = v_u_xlat18;
    v_u_xlat16_6.z = v_u_xlat30;
    v_u_xlat16_6.w = v_u_xlat0;
    v_u_xlat26.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat38 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_26.x = v_u_xlat26.x;
    v_u_xlat16_26.y = v_u_xlat38;
    v_u_xlat11 = 2.0f.xx;
    v_u_xlat11 = v_u_xlat2.xy * v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.zw;
    float2 _935 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat1.x = _935.x;
    v_u_xlat1.y = _935.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _944 = v_u_xlat16_9;
    float2 _946 = v_u_xlat16_26 * _944.xy;
    v_u_xlat16_9.x = _946.x;
    v_u_xlat16_9.y = _946.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat11 = v_u_xlat12 + v_u_xlat9;
    v_u_xlat25 = 0.25f.xx / v_u_xlat11;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat25.x;
    v_u_xlat16_1.w = v_u_xlat25.y;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_COLOR1 = v_vs_COLOR1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR1;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_COLOR1 : TEXCOORD1;
    float4 f_vs_TEXCOORD0 : TEXCOORD2;
    float4 f_vs_TEXCOORD1 : TEXCOORD3;
    float4 f_vs_TEXCOORD2 : TEXCOORD4;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat4;
static float f_u_xlat0;
static float4 f_u_xlat16_1;
static float4 f_u_xlat16_2;
static float2 f_u_xlat3;
static float2 f_u_xlat1;
static float2 f_u_xlat11;
static float2 f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat4 = f_vs_TEXCOORD1.x;
    f_u_xlat0 = f_u_xlat4 * f_u_xlat16_0.x;
    f_u_xlat16_0.x = f_u_xlat0;
    f_u_xlat16_1.x = -f_vs_TEXCOORD1.z;
    f_u_xlat16_1.x = f_u_xlat16_0.x + f_u_xlat16_1.x;
    f_u_xlat16_1.x = max(f_u_xlat16_1.x, 0.0f);
    f_u_xlat16_1.x = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_2 = -f_vs_COLOR1;
    f_u_xlat16_2 += f_vs_COLOR0;
    f_u_xlat16_1 = f_u_xlat16_1.xxxx * f_u_xlat16_2;
    f_u_xlat16_1 += f_vs_COLOR1;
    f_u_xlat16_2.x = -f_vs_TEXCOORD1.y;
    f_u_xlat16_2.x = f_u_xlat16_0.x + f_u_xlat16_2.x;
    f_u_xlat16_2.x = max(f_u_xlat16_2.x, 0.0f);
    f_u_xlat16_2.x = min(f_u_xlat16_2.x, 1.0f);
    f_u_xlat16_0 = f_u_xlat16_1 * f_u_xlat16_2.xxxx;
    f_u_xlat3 = -_ClipRect.xy;
    f_u_xlat3 += _ClipRect.zw;
    float2 _113 = -f_vs_TEXCOORD2.xy;
    f_u_xlat16_1.x = _113.x;
    f_u_xlat16_1.y = _113.y;
    float4 _118 = f_u_xlat16_1;
    float2 _122 = max(_118.xy, f_vs_TEXCOORD2.xy);
    f_u_xlat16_1.x = _122.x;
    f_u_xlat16_1.y = _122.y;
    f_u_xlat1 = f_u_xlat16_1.xy;
    f_u_xlat11 = -f_u_xlat1;
    f_u_xlat3 = f_u_xlat11 + f_u_xlat3;
    f_u_xlat11 = f_vs_TEXCOORD2.zw;
    f_u_xlat3 = f_u_xlat11 * f_u_xlat3;
    f_u_xlat3 = max(f_u_xlat3, 0.0f.xx);
    f_u_xlat3 = min(f_u_xlat3, 1.0f.xx);
    f_u_xlat16_3 = f_u_xlat3;
    f_u_xlat16_2.x = f_u_xlat16_3.y * f_u_xlat16_3.x;
    f_u_xlat16_0 *= f_u_xlat16_2.xxxx;
    f_SV_Target0 = f_u_xlat16_0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR1 = stage_input.f_vs_COLOR1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(OUTLINE_ON) && defined(UNDERLAY_ON) && !defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float _UnderlaySoftness;
float _ScaleRatioC;
float _UnderlayDilate;
float _UnderlayOffsetX;
float _TextureWidth;
float _UnderlayOffsetY;
float _TextureHeight;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _UnderlayColor;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;
static float4 v_vs_TEXCOORD3;
static float2 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD2 : TEXCOORD3;
    float4 v_vs_TEXCOORD3 : TEXCOORD4;
    float2 v_vs_TEXCOORD4 : TEXCOORD5;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat16;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat14;
static float2 v_u_xlat29;
static float2 v_u_xlat30;
static float v_u_xlat42;
static float v_u_xlat43;
static bool v_u_xlatb29;
static float4 v_u_xlat6;
static float v_u_xlat15;
static bool v_u_xlatb43;
static int v_u_xlati30;
static int v_u_xlati43;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static bool3 v_u_xlatb4;
static float4 v_u_xlat16_4;
static float v_u_xlat46;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_46;
static float v_u_xlat44;
static float v_u_xlat11;
static float v_u_xlat25;
static float2 v_u_xlat12;
static float v_u_xlat26;
static float2 v_u_xlat40;
static float2 v_u_xlat35;
static float4 v_u_xlat7;
static float v_u_xlat20;
static float v_u_xlat34;
static float2 v_u_xlat16_30;
static float2 v_u_xlat13;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;
static float2 v_u_xlat16_11;
static float4 v_vs_COLOR1;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0.x = 0.0f;
    v_u_xlatb0 = v_u_xlat0.x >= v_in_TEXCOORD0.w;
    v_u_xlat0.x = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat16 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat16;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat14 = v_u_xlat3.ww;
    v_u_xlat29 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat30 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat29 += v_u_xlat30;
    v_u_xlat30 = -v_u_xlat29;
    v_u_xlat29 = max(v_u_xlat29, v_u_xlat30);
    v_u_xlat29 *= float2(_ScaleX, _ScaleY);
    v_u_xlat14 /= v_u_xlat29;
    v_u_xlat42 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat42 = rsqrt(v_u_xlat42);
    v_u_xlat29.x = -v_in_TEXCOORD0.w;
    v_u_xlat29.x = max(v_u_xlat29.x, v_in_TEXCOORD0.w);
    v_u_xlat29.x *= _GradientScale;
    v_u_xlat43 = 1.0f;
    v_u_xlat43 += _Sharpness;
    v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
    v_u_xlat42 *= v_u_xlat29.x;
    v_u_xlat29.x = 0.0f;
    v_u_xlatb29 = v_u_xlat29.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb29)
    {
        v_u_xlat29.x = -v_u_xlat42;
        v_u_xlat29.x = max(v_u_xlat42, v_u_xlat29.x);
        v_u_xlat43 = 1.0f;
        v_u_xlat30.x = -_PerspectiveFilter;
        v_u_xlat43 += v_u_xlat30.x;
        v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat43 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat43 = rsqrt(v_u_xlat43);
        float3 _293 = v_u_xlat43.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _293.x;
        v_u_xlat4.y = _293.y;
        v_u_xlat4.z = _293.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _306 = v_u_xlat1;
        v_u_xlat1.x = _306.xy.x;
        v_u_xlat1.y = _306.xy.y;
        float3 _317 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _317.x;
        v_u_xlat5.y = _317.y;
        v_u_xlat5.z = _317.z;
        float3 _330 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _330.x;
        v_u_xlat6.y = _330.y;
        v_u_xlat6.z = _330.z;
        float4 _337 = v_u_xlat5;
        float3 _341 = _337.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _341.x;
        v_u_xlat5.y = _341.y;
        v_u_xlat5.z = _341.z;
        float3 _353 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _353.x;
        v_u_xlat6.y = _353.y;
        v_u_xlat6.z = _353.z;
        float4 _360 = v_u_xlat5;
        float3 _364 = _360.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _364.x;
        v_u_xlat5.y = _364.y;
        v_u_xlat5.z = _364.z;
        float4 _371 = v_u_xlat1;
        float3 _376 = _371.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _376.x;
        v_u_xlat1.y = _376.y;
        v_u_xlat1.w = _376.z;
        float4 _383 = v_u_xlat1;
        float3 _387 = _383.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _387.x;
        v_u_xlat1.y = _387.y;
        v_u_xlat1.w = _387.z;
        float4 _394 = v_u_xlat1;
        v_u_xlat1.x = _394.xyw.x;
        v_u_xlat1.y = _394.xyw.y;
        v_u_xlat1.w = _394.xyw.z;
        float4 _402 = v_u_xlat1;
        float3 _404 = -_402.xyw;
        v_u_xlat1.x = _404.x;
        v_u_xlat1.y = _404.y;
        v_u_xlat1.w = _404.z;
        float4 _411 = v_u_xlat1;
        float3 _416 = _411.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _416.x;
        v_u_xlat1.y = _416.y;
        v_u_xlat1.w = _416.z;
        float4 _423 = v_u_xlat1;
        v_u_xlat1.x = _423.xyw.x;
        v_u_xlat1.y = _423.xyw.y;
        v_u_xlat1.w = _423.xyw.z;
        v_u_xlat30.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat30.x = rsqrt(v_u_xlat30.x);
        float4 _441 = v_u_xlat1;
        float3 _445 = _441.xyw * v_u_xlat30.xxx;
        v_u_xlat1.x = _445.x;
        v_u_xlat1.y = _445.y;
        v_u_xlat1.w = _445.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat15 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat15, v_u_xlat1.x);
        v_u_xlat15 = -v_u_xlat29.x;
        v_u_xlat15 = v_u_xlat42 + v_u_xlat15;
        v_u_xlat1.x = v_u_xlat15 * v_u_xlat1.x;
        v_u_xlat42 = v_u_xlat1.x + v_u_xlat29.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0.x *= v_u_xlat1.x;
    v_u_xlat0.x += _WeightNormal;
    v_u_xlat0.x /= 4.0f;
    v_u_xlat0.x += _FaceDilate;
    v_u_xlat0.x *= _ScaleRatioA;
    v_u_xlat0.x *= 0.5f;
    v_u_xlat42 = v_u_xlat42;
    v_u_xlat1.x = 1.0f;
    v_u_xlat15 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat15 = v_u_xlat42 * v_u_xlat15;
    v_u_xlat1.x = v_u_xlat15 + v_u_xlat1.x;
    v_u_xlat1.x = v_u_xlat42 / v_u_xlat1.x;
    v_u_xlat15 = -v_u_xlat0.x;
    v_u_xlat15 += 0.5f;
    v_u_xlat15 = v_u_xlat1.x * v_u_xlat15;
    v_u_xlat29.x = -0.5f;
    v_u_xlat15 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat29.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat29.x *= 0.5f;
    v_u_xlat29.x = v_u_xlat1.x * v_u_xlat29.x;
    int4 _581 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb43 = any(bool4(int4(0, 0, 0, 0).x != _581.x, int4(0, 0, 0, 0).y != _581.y, int4(0, 0, 0, 0).z != _581.z, int4(0, 0, 0, 0).w != _581.w));
    v_u_xlati30 = 0;
    int param = v_u_xlati30;
    v_u_xlati30 = v_op_not(param);
    v_u_xlati43 = v_u_xlatb43 ? v_u_xlati30 : 0;
    if (v_u_xlati43 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _615 = v_u_xlat16_8;
        float3 _617 = v_u_xlat16_7.xyz * _615.xyz;
        v_u_xlat16_8.x = _617.x;
        v_u_xlat16_8.y = _617.y;
        v_u_xlat16_8.z = _617.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _631 = v_u_xlat16_8;
        float3 _634 = _631.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _634.x;
        v_u_xlat16_8.y = _634.y;
        v_u_xlat16_8.z = _634.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb4 = bool4(v_u_xlat16_7.xyzx.x < v_u_xlat16_10.xyzx.x, v_u_xlat16_7.xyzx.y < v_u_xlat16_10.xyzx.y, v_u_xlat16_7.xyzx.z < v_u_xlat16_10.xyzx.z, v_u_xlat16_7.xyzx.w < v_u_xlat16_10.xyzx.w).xyz;
        float _698;
        if (v_u_xlatb4.x)
        {
            _698 = v_u_xlat16_8.x;
        }
        else
        {
            _698 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _698;
        float _710;
        if (v_u_xlatb4.y)
        {
            _710 = v_u_xlat16_8.y;
        }
        else
        {
            _710 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _710;
        float _722;
        if (v_u_xlatb4.z)
        {
            _722 = v_u_xlat16_8.z;
        }
        else
        {
            _722 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _722;
        float4 _732 = v_u_xlat16_4;
        v_u_xlat16_4.x = _732.xyz.x;
        v_u_xlat16_4.y = _732.xyz.y;
        v_u_xlat16_4.z = _732.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat46 = 1.0f;
    v_u_xlat16_4.w = v_u_xlat46;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _762 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _762.x;
    v_u_xlat16_6.y = _762.y;
    v_u_xlat16_6.z = _762.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat30.x = v_u_xlat16_7.w;
    v_u_xlat46 = v_u_xlat16_4.w * v_u_xlat30.x;
    v_u_xlat16_46 = v_u_xlat46;
    float3 _785 = v_u_xlat16_46.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _785.x;
    v_u_xlat16_4.y = _785.y;
    v_u_xlat16_4.z = _785.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_46;
    v_u_xlat30.x = 2.0f;
    v_u_xlat30.x = v_u_xlat29.x * v_u_xlat30.x;
    v_u_xlat30.x = min(v_u_xlat30.x, 1.0f);
    v_u_xlat30.x = sqrt(v_u_xlat30.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat30.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat30.x = 1.0f;
    v_u_xlat44 = _UnderlaySoftness * _ScaleRatioC;
    v_u_xlat44 = v_u_xlat42 * v_u_xlat44;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat11 = v_u_xlat42 / v_u_xlat30.x;
    v_u_xlat30.x = -v_u_xlat0.x;
    v_u_xlat30.x += 0.5f;
    v_u_xlat30.x = v_u_xlat11 * v_u_xlat30.x;
    v_u_xlat44 = -0.5f;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat44 = _UnderlayDilate * _ScaleRatioC;
    v_u_xlat44 *= 0.5f;
    v_u_xlat44 = v_u_xlat11 * v_u_xlat44;
    v_u_xlat44 = -v_u_xlat44;
    v_u_xlat25 = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat30.x = _UnderlayOffsetX * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat12.x = v_u_xlat30.x / _TextureWidth;
    v_u_xlat30.x = _UnderlayOffsetY * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat26 = v_u_xlat30.x / _TextureHeight;
    v_u_xlat12.x = v_u_xlat12.x;
    v_u_xlat12.y = v_u_xlat26;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat30 = -v_u_xlat5.xy;
    v_u_xlat30 += v_u_xlat2.xy;
    v_u_xlat40 = -v_u_xlat5.xy;
    v_u_xlat40 = v_u_xlat5.zw + v_u_xlat40;
    v_u_xlat35 = v_u_xlat30 / v_u_xlat40;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat35.x;
    v_u_xlat7.w = v_u_xlat35.y;
    v_u_xlat30.x = -v_u_xlat29.x;
    v_u_xlat20 = v_u_xlat15 + v_u_xlat30.x;
    v_u_xlat34 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat16_6.x = v_u_xlat1.x;
    v_u_xlat16_6.y = v_u_xlat20;
    v_u_xlat16_6.z = v_u_xlat34;
    v_u_xlat16_6.w = v_u_xlat15;
    v_u_xlat30.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat44 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_30.x = v_u_xlat30.x;
    v_u_xlat16_30.y = v_u_xlat44;
    v_u_xlat40 = 2.0f.xx;
    v_u_xlat40 = v_u_xlat2.xy * v_u_xlat40;
    v_u_xlat13 = -v_u_xlat5.xy;
    v_u_xlat40 += v_u_xlat13;
    v_u_xlat13 = -v_u_xlat5.zw;
    float2 _1052 = v_u_xlat40 + v_u_xlat13;
    v_u_xlat1.x = _1052.x;
    v_u_xlat1.y = _1052.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _1061 = v_u_xlat16_9;
    float2 _1063 = v_u_xlat16_30 * _1061.xy;
    v_u_xlat16_9.x = _1063.x;
    v_u_xlat16_9.y = _1063.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat40 = v_u_xlat14 + v_u_xlat9;
    v_u_xlat29 = 0.25f.xx / v_u_xlat40;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat29.x;
    v_u_xlat16_1.w = v_u_xlat29.y;
    float2 _1092 = v_u_xlat12 + v_in_TEXCOORD0.xy;
    v_u_xlat0.x = _1092.x;
    v_u_xlat0.y = _1092.y;
    v_u_xlat0.z = v_in_COLOR0.w;
    v_u_xlat0.w = 0.0f;
    v_u_xlat16_11.x = v_u_xlat11;
    v_u_xlat16_11.y = v_u_xlat25;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
    v_vs_TEXCOORD3 = v_u_xlat0;
    v_vs_TEXCOORD4 = v_u_xlat16_11;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD4;
static float4 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD2 : TEXCOORD3;
    float4 f_vs_TEXCOORD3 : TEXCOORD4;
    float2 f_vs_TEXCOORD4 : TEXCOORD5;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat8;
static float4 f_u_xlat0;
static float3 f_u_xlat16_1;
static float4 f_u_xlat16_2;
static float f_u_xlat16_3;
static float f_u_xlat4;
static float f_u_xlat16_4;
static float4 f_u_xlat1;
static float2 f_u_xlat16_5;
static float4 f_u_xlat2;
static float f_u_xlat16_13;
static float2 f_u_xlat6;
static float2 f_u_xlat5;
static float2 f_u_xlat22;
static float2 f_u_xlat16_6;
static float f_u_xlat16_7;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat8 = f_vs_TEXCOORD1.x;
    f_u_xlat0.x = f_u_xlat8 * f_u_xlat16_0.x;
    f_u_xlat16_0.x = f_u_xlat0.x;
    f_u_xlat16_1.x = -f_vs_TEXCOORD1.w;
    f_u_xlat16_1.x = f_u_xlat16_0.x + f_u_xlat16_1.x;
    f_u_xlat16_1.x = max(f_u_xlat16_1.x, 0.0f);
    f_u_xlat16_2.x = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_0 = f_u_xlat16_2.xxxx * f_vs_COLOR0;
    f_u_xlat16_3 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD3.xy).w;
    f_u_xlat4 = f_vs_TEXCOORD4.x;
    f_u_xlat4 = f_u_xlat16_3 * f_u_xlat4;
    f_u_xlat16_4 = f_u_xlat4;
    f_u_xlat16_1 = _UnderlayColor.www * _UnderlayColor.xyz;
    f_u_xlat1.x = f_u_xlat16_1.x;
    f_u_xlat1.y = f_u_xlat16_1.y;
    f_u_xlat1.z = f_u_xlat16_1.z;
    f_u_xlat1.w = _UnderlayColor.w;
    f_u_xlat16_5.x = -f_vs_TEXCOORD4.y;
    f_u_xlat16_5.x = f_u_xlat16_4 + f_u_xlat16_5.x;
    f_u_xlat16_5.x = max(f_u_xlat16_5.x, 0.0f);
    f_u_xlat16_2 = min(f_u_xlat16_5.xxxx, 1.0f.xxxx);
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat16_5.x = 1.0f;
    f_u_xlat16_13 = -f_u_xlat16_0.w;
    f_u_xlat16_2 = f_u_xlat16_13.xxxx + f_u_xlat16_5.xxxx;
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 = f_u_xlat1 + f_u_xlat0;
    f_u_xlat16_0 = f_u_xlat0;
    f_u_xlat6 = -_ClipRect.xy;
    f_u_xlat6 += _ClipRect.zw;
    f_u_xlat16_5 = -f_vs_TEXCOORD2.xy;
    f_u_xlat16_5 = max(f_u_xlat16_5, f_vs_TEXCOORD2.xy);
    f_u_xlat5 = f_u_xlat16_5;
    f_u_xlat22 = -f_u_xlat5;
    f_u_xlat6 = f_u_xlat22 + f_u_xlat6;
    f_u_xlat22 = f_vs_TEXCOORD2.zw;
    f_u_xlat6 = f_u_xlat22 * f_u_xlat6;
    f_u_xlat6 = max(f_u_xlat6, 0.0f.xx);
    f_u_xlat6 = min(f_u_xlat6, 1.0f.xx);
    f_u_xlat16_6 = f_u_xlat6;
    f_u_xlat16_7 = f_u_xlat16_6.y * f_u_xlat16_6.x;
    f_u_xlat16_0 *= f_u_xlat16_7.xxxx;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 *= f_vs_TEXCOORD3.zzzz;
    f_u_xlat16_0 = f_u_xlat0;
    f_SV_Target0 = f_u_xlat16_0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(OUTLINE_ON) && !defined(UNDERLAY_ON) && defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD2 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat14;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat12;
static float2 v_u_xlat25;
static float2 v_u_xlat26;
static float v_u_xlat36;
static float v_u_xlat37;
static bool v_u_xlatb25;
static float4 v_u_xlat6;
static float v_u_xlat13;
static bool3 v_u_xlatb13;
static int v_u_xlati25;
static int v_u_xlati13;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static float4 v_u_xlat16_4;
static float v_u_xlat40;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_40;
static float2 v_u_xlat11;
static float2 v_u_xlat31;
static float4 v_u_xlat7;
static float v_u_xlat18;
static float v_u_xlat30;
static float v_u_xlat38;
static float2 v_u_xlat16_26;
static float2 v_u_xlat35;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;
static float4 v_vs_COLOR1;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0 = 0.0f;
    v_u_xlatb0 = v_u_xlat0 >= v_in_TEXCOORD0.w;
    v_u_xlat0 = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat14 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat14;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat12 = v_u_xlat3.ww;
    v_u_xlat25 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat26 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat25 += v_u_xlat26;
    v_u_xlat26 = -v_u_xlat25;
    v_u_xlat25 = max(v_u_xlat25, v_u_xlat26);
    v_u_xlat25 *= float2(_ScaleX, _ScaleY);
    v_u_xlat12 /= v_u_xlat25;
    v_u_xlat36 = dot(v_u_xlat12, v_u_xlat12);
    v_u_xlat36 = rsqrt(v_u_xlat36);
    v_u_xlat25.x = -v_in_TEXCOORD0.w;
    v_u_xlat25.x = max(v_u_xlat25.x, v_in_TEXCOORD0.w);
    v_u_xlat25.x *= _GradientScale;
    v_u_xlat37 = 1.0f;
    v_u_xlat37 += _Sharpness;
    v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
    v_u_xlat36 *= v_u_xlat25.x;
    v_u_xlat25.x = 0.0f;
    v_u_xlatb25 = v_u_xlat25.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb25)
    {
        v_u_xlat25.x = -v_u_xlat36;
        v_u_xlat25.x = max(v_u_xlat36, v_u_xlat25.x);
        v_u_xlat37 = 1.0f;
        v_u_xlat26.x = -_PerspectiveFilter;
        v_u_xlat37 += v_u_xlat26.x;
        v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat37 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat37 = rsqrt(v_u_xlat37);
        float3 _290 = v_u_xlat37.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _290.x;
        v_u_xlat4.y = _290.y;
        v_u_xlat4.z = _290.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _303 = v_u_xlat1;
        v_u_xlat1.x = _303.xy.x;
        v_u_xlat1.y = _303.xy.y;
        float3 _314 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _314.x;
        v_u_xlat5.y = _314.y;
        v_u_xlat5.z = _314.z;
        float3 _327 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _327.x;
        v_u_xlat6.y = _327.y;
        v_u_xlat6.z = _327.z;
        float4 _334 = v_u_xlat5;
        float3 _338 = _334.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _338.x;
        v_u_xlat5.y = _338.y;
        v_u_xlat5.z = _338.z;
        float3 _350 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _350.x;
        v_u_xlat6.y = _350.y;
        v_u_xlat6.z = _350.z;
        float4 _357 = v_u_xlat5;
        float3 _361 = _357.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _361.x;
        v_u_xlat5.y = _361.y;
        v_u_xlat5.z = _361.z;
        float4 _368 = v_u_xlat1;
        float3 _373 = _368.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _373.x;
        v_u_xlat1.y = _373.y;
        v_u_xlat1.w = _373.z;
        float4 _380 = v_u_xlat1;
        float3 _384 = _380.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _384.x;
        v_u_xlat1.y = _384.y;
        v_u_xlat1.w = _384.z;
        float4 _391 = v_u_xlat1;
        v_u_xlat1.x = _391.xyw.x;
        v_u_xlat1.y = _391.xyw.y;
        v_u_xlat1.w = _391.xyw.z;
        float4 _399 = v_u_xlat1;
        float3 _401 = -_399.xyw;
        v_u_xlat1.x = _401.x;
        v_u_xlat1.y = _401.y;
        v_u_xlat1.w = _401.z;
        float4 _408 = v_u_xlat1;
        float3 _413 = _408.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _413.x;
        v_u_xlat1.y = _413.y;
        v_u_xlat1.w = _413.z;
        float4 _420 = v_u_xlat1;
        v_u_xlat1.x = _420.xyw.x;
        v_u_xlat1.y = _420.xyw.y;
        v_u_xlat1.w = _420.xyw.z;
        v_u_xlat26.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat26.x = rsqrt(v_u_xlat26.x);
        float4 _438 = v_u_xlat1;
        float3 _442 = _438.xyw * v_u_xlat26.xxx;
        v_u_xlat1.x = _442.x;
        v_u_xlat1.y = _442.y;
        v_u_xlat1.w = _442.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat13 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat13, v_u_xlat1.x);
        v_u_xlat13 = -v_u_xlat25.x;
        v_u_xlat13 = v_u_xlat36 + v_u_xlat13;
        v_u_xlat1.x = v_u_xlat13 * v_u_xlat1.x;
        v_u_xlat36 = v_u_xlat1.x + v_u_xlat25.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0 *= v_u_xlat1.x;
    v_u_xlat0 += _WeightNormal;
    v_u_xlat0 /= 4.0f;
    v_u_xlat0 += _FaceDilate;
    v_u_xlat0 *= _ScaleRatioA;
    v_u_xlat0 *= 0.5f;
    v_u_xlat1.x = 1.0f;
    v_u_xlat13 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat13 = v_u_xlat36 * v_u_xlat13;
    v_u_xlat1.x = v_u_xlat13 + v_u_xlat1.x;
    v_u_xlat36 /= v_u_xlat1.x;
    v_u_xlat0 = -v_u_xlat0;
    v_u_xlat0 += 0.5f;
    v_u_xlat0 = v_u_xlat36 * v_u_xlat0;
    v_u_xlat1.x = -0.5f;
    v_u_xlat0 += v_u_xlat1.x;
    v_u_xlat1.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat1.x *= 0.5f;
    v_u_xlat1.x = v_u_xlat36 * v_u_xlat1.x;
    int4 _563 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb13.x = any(bool4(int4(0, 0, 0, 0).x != _563.x, int4(0, 0, 0, 0).y != _563.y, int4(0, 0, 0, 0).z != _563.z, int4(0, 0, 0, 0).w != _563.w));
    v_u_xlati25 = 0;
    int param = v_u_xlati25;
    v_u_xlati25 = v_op_not(param);
    v_u_xlati13 = v_u_xlatb13.x ? v_u_xlati25 : 0;
    if (v_u_xlati13 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _599 = v_u_xlat16_8;
        float3 _601 = v_u_xlat16_7.xyz * _599.xyz;
        v_u_xlat16_8.x = _601.x;
        v_u_xlat16_8.y = _601.y;
        v_u_xlat16_8.z = _601.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _615 = v_u_xlat16_8;
        float3 _618 = _615.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _618.x;
        v_u_xlat16_8.y = _618.y;
        v_u_xlat16_8.z = _618.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb13 = bool4(v_u_xlat16_7.xyzz.x < v_u_xlat16_10.xyzz.x, v_u_xlat16_7.xyzz.y < v_u_xlat16_10.xyzz.y, v_u_xlat16_7.xyzz.z < v_u_xlat16_10.xyzz.z, v_u_xlat16_7.xyzz.w < v_u_xlat16_10.xyzz.w).xyz;
        float _679;
        if (v_u_xlatb13.x)
        {
            _679 = v_u_xlat16_8.x;
        }
        else
        {
            _679 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _679;
        float _691;
        if (v_u_xlatb13.y)
        {
            _691 = v_u_xlat16_8.y;
        }
        else
        {
            _691 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _691;
        float _703;
        if (v_u_xlatb13.z)
        {
            _703 = v_u_xlat16_8.z;
        }
        else
        {
            _703 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _703;
        float4 _713 = v_u_xlat16_4;
        v_u_xlat16_4.x = _713.xyz.x;
        v_u_xlat16_4.y = _713.xyz.y;
        v_u_xlat16_4.z = _713.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat40 = v_in_COLOR0.w;
    v_u_xlat16_4.w = v_u_xlat40;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _745 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _745.x;
    v_u_xlat16_6.y = _745.y;
    v_u_xlat16_6.z = _745.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat26.x = v_u_xlat16_7.w;
    v_u_xlat40 = v_u_xlat16_4.w * v_u_xlat26.x;
    v_u_xlat16_40 = v_u_xlat40;
    float3 _768 = v_u_xlat16_40.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _768.x;
    v_u_xlat16_4.y = _768.y;
    v_u_xlat16_4.z = _768.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_40;
    v_u_xlat26.x = 2.0f;
    v_u_xlat26.x = v_u_xlat1.x * v_u_xlat26.x;
    v_u_xlat26.x = min(v_u_xlat26.x, 1.0f);
    v_u_xlat26.x = sqrt(v_u_xlat26.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat26.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat26 = -v_u_xlat5.xy;
    v_u_xlat26 += v_u_xlat2.xy;
    v_u_xlat11 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat5.zw + v_u_xlat11;
    v_u_xlat31 = v_u_xlat26 / v_u_xlat11;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat31.x;
    v_u_xlat7.w = v_u_xlat31.y;
    v_u_xlat26.x = -v_u_xlat1.x;
    v_u_xlat18 = v_u_xlat0 + v_u_xlat26.x;
    v_u_xlat30 = v_u_xlat0 + v_u_xlat1.x;
    v_u_xlat16_6.x = v_u_xlat36;
    v_u_xlat16_6.y = v_u_xlat18;
    v_u_xlat16_6.z = v_u_xlat30;
    v_u_xlat16_6.w = v_u_xlat0;
    v_u_xlat26.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat38 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_26.x = v_u_xlat26.x;
    v_u_xlat16_26.y = v_u_xlat38;
    v_u_xlat11 = 2.0f.xx;
    v_u_xlat11 = v_u_xlat2.xy * v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.zw;
    float2 _935 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat1.x = _935.x;
    v_u_xlat1.y = _935.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _944 = v_u_xlat16_9;
    float2 _946 = v_u_xlat16_26 * _944.xy;
    v_u_xlat16_9.x = _946.x;
    v_u_xlat16_9.y = _946.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat11 = v_u_xlat12 + v_u_xlat9;
    v_u_xlat25 = 0.25f.xx / v_u_xlat11;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat25.x;
    v_u_xlat16_1.w = v_u_xlat25.y;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD2 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat5;
static float f_u_xlat0;
static float2 f_u_xlat16_1;
static float2 f_u_xlat2;
static float2 f_u_xlat1;
static float2 f_u_xlat12;
static float2 f_u_xlat16_2;
static float f_u_xlat16_3;
static float f_u_xlat3;
static bool f_u_xlatb4;
static int f_u_xlati4;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat5 = f_vs_TEXCOORD1.x;
    f_u_xlat0 = f_u_xlat5 * f_u_xlat16_0.x;
    f_u_xlat16_0.x = f_u_xlat0;
    f_u_xlat16_1.x = -f_vs_TEXCOORD1.w;
    f_u_xlat16_1.x = f_u_xlat16_0.x + f_u_xlat16_1.x;
    f_u_xlat16_1.x = max(f_u_xlat16_1.x, 0.0f);
    f_u_xlat16_1.x = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_0 = f_u_xlat16_1.xxxx * f_vs_COLOR0;
    f_u_xlat2 = -_ClipRect.xy;
    f_u_xlat2 += _ClipRect.zw;
    f_u_xlat16_1 = -f_vs_TEXCOORD2.xy;
    f_u_xlat16_1 = max(f_u_xlat16_1, f_vs_TEXCOORD2.xy);
    f_u_xlat1 = f_u_xlat16_1;
    f_u_xlat12 = -f_u_xlat1;
    f_u_xlat2 = f_u_xlat12 + f_u_xlat2;
    f_u_xlat12 = f_vs_TEXCOORD2.zw;
    f_u_xlat2 = f_u_xlat12 * f_u_xlat2;
    f_u_xlat2 = max(f_u_xlat2, 0.0f.xx);
    f_u_xlat2 = min(f_u_xlat2, 1.0f.xx);
    f_u_xlat16_2 = f_u_xlat2;
    f_u_xlat16_3 = f_u_xlat16_2.y * f_u_xlat16_2.x;
    f_u_xlat16_0 *= f_u_xlat16_3.xxxx;
    f_u_xlat16_3 = 0.001000000047497451305389404296875f;
    f_u_xlat16_3 = -f_u_xlat16_3;
    f_u_xlat16_3 = f_u_xlat16_0.w + f_u_xlat16_3;
    f_u_xlat3 = f_u_xlat16_3;
    f_u_xlatb4 = f_u_xlat3 < 0.0f;
    f_u_xlati4 = f_u_xlatb4 ? (-1) : 0;
    if (f_u_xlati4 != 0)
    {
        discard;
    }
    f_SV_Target0 = f_u_xlat16_0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif defined(OUTLINE_ON) && !defined(UNDERLAY_ON) && !defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_COLOR1 : TEXCOORD1;
    float4 v_vs_TEXCOORD0 : TEXCOORD2;
    float4 v_vs_TEXCOORD1 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat14;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat12;
static float2 v_u_xlat25;
static float2 v_u_xlat26;
static float v_u_xlat36;
static float v_u_xlat37;
static bool v_u_xlatb25;
static float4 v_u_xlat6;
static float v_u_xlat13;
static bool3 v_u_xlatb13;
static int v_u_xlati25;
static int v_u_xlati13;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static float4 v_u_xlat16_4;
static float v_u_xlat40;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_40;
static float2 v_u_xlat11;
static float2 v_u_xlat31;
static float4 v_u_xlat7;
static float v_u_xlat18;
static float v_u_xlat30;
static float v_u_xlat38;
static float2 v_u_xlat16_26;
static float2 v_u_xlat35;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;
static float4 v_vs_TEXCOORD2;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0 = 0.0f;
    v_u_xlatb0 = v_u_xlat0 >= v_in_TEXCOORD0.w;
    v_u_xlat0 = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat14 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat14;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat12 = v_u_xlat3.ww;
    v_u_xlat25 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat26 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat25 += v_u_xlat26;
    v_u_xlat26 = -v_u_xlat25;
    v_u_xlat25 = max(v_u_xlat25, v_u_xlat26);
    v_u_xlat25 *= float2(_ScaleX, _ScaleY);
    v_u_xlat12 /= v_u_xlat25;
    v_u_xlat36 = dot(v_u_xlat12, v_u_xlat12);
    v_u_xlat36 = rsqrt(v_u_xlat36);
    v_u_xlat25.x = -v_in_TEXCOORD0.w;
    v_u_xlat25.x = max(v_u_xlat25.x, v_in_TEXCOORD0.w);
    v_u_xlat25.x *= _GradientScale;
    v_u_xlat37 = 1.0f;
    v_u_xlat37 += _Sharpness;
    v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
    v_u_xlat36 *= v_u_xlat25.x;
    v_u_xlat25.x = 0.0f;
    v_u_xlatb25 = v_u_xlat25.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb25)
    {
        v_u_xlat25.x = -v_u_xlat36;
        v_u_xlat25.x = max(v_u_xlat36, v_u_xlat25.x);
        v_u_xlat37 = 1.0f;
        v_u_xlat26.x = -_PerspectiveFilter;
        v_u_xlat37 += v_u_xlat26.x;
        v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat37 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat37 = rsqrt(v_u_xlat37);
        float3 _290 = v_u_xlat37.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _290.x;
        v_u_xlat4.y = _290.y;
        v_u_xlat4.z = _290.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _303 = v_u_xlat1;
        v_u_xlat1.x = _303.xy.x;
        v_u_xlat1.y = _303.xy.y;
        float3 _314 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _314.x;
        v_u_xlat5.y = _314.y;
        v_u_xlat5.z = _314.z;
        float3 _327 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _327.x;
        v_u_xlat6.y = _327.y;
        v_u_xlat6.z = _327.z;
        float4 _334 = v_u_xlat5;
        float3 _338 = _334.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _338.x;
        v_u_xlat5.y = _338.y;
        v_u_xlat5.z = _338.z;
        float3 _350 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _350.x;
        v_u_xlat6.y = _350.y;
        v_u_xlat6.z = _350.z;
        float4 _357 = v_u_xlat5;
        float3 _361 = _357.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _361.x;
        v_u_xlat5.y = _361.y;
        v_u_xlat5.z = _361.z;
        float4 _368 = v_u_xlat1;
        float3 _373 = _368.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _373.x;
        v_u_xlat1.y = _373.y;
        v_u_xlat1.w = _373.z;
        float4 _380 = v_u_xlat1;
        float3 _384 = _380.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _384.x;
        v_u_xlat1.y = _384.y;
        v_u_xlat1.w = _384.z;
        float4 _391 = v_u_xlat1;
        v_u_xlat1.x = _391.xyw.x;
        v_u_xlat1.y = _391.xyw.y;
        v_u_xlat1.w = _391.xyw.z;
        float4 _399 = v_u_xlat1;
        float3 _401 = -_399.xyw;
        v_u_xlat1.x = _401.x;
        v_u_xlat1.y = _401.y;
        v_u_xlat1.w = _401.z;
        float4 _408 = v_u_xlat1;
        float3 _413 = _408.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _413.x;
        v_u_xlat1.y = _413.y;
        v_u_xlat1.w = _413.z;
        float4 _420 = v_u_xlat1;
        v_u_xlat1.x = _420.xyw.x;
        v_u_xlat1.y = _420.xyw.y;
        v_u_xlat1.w = _420.xyw.z;
        v_u_xlat26.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat26.x = rsqrt(v_u_xlat26.x);
        float4 _438 = v_u_xlat1;
        float3 _442 = _438.xyw * v_u_xlat26.xxx;
        v_u_xlat1.x = _442.x;
        v_u_xlat1.y = _442.y;
        v_u_xlat1.w = _442.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat13 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat13, v_u_xlat1.x);
        v_u_xlat13 = -v_u_xlat25.x;
        v_u_xlat13 = v_u_xlat36 + v_u_xlat13;
        v_u_xlat1.x = v_u_xlat13 * v_u_xlat1.x;
        v_u_xlat36 = v_u_xlat1.x + v_u_xlat25.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0 *= v_u_xlat1.x;
    v_u_xlat0 += _WeightNormal;
    v_u_xlat0 /= 4.0f;
    v_u_xlat0 += _FaceDilate;
    v_u_xlat0 *= _ScaleRatioA;
    v_u_xlat0 *= 0.5f;
    v_u_xlat1.x = 1.0f;
    v_u_xlat13 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat13 = v_u_xlat36 * v_u_xlat13;
    v_u_xlat1.x = v_u_xlat13 + v_u_xlat1.x;
    v_u_xlat36 /= v_u_xlat1.x;
    v_u_xlat0 = -v_u_xlat0;
    v_u_xlat0 += 0.5f;
    v_u_xlat0 = v_u_xlat36 * v_u_xlat0;
    v_u_xlat1.x = -0.5f;
    v_u_xlat0 += v_u_xlat1.x;
    v_u_xlat1.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat1.x *= 0.5f;
    v_u_xlat1.x = v_u_xlat36 * v_u_xlat1.x;
    int4 _563 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb13.x = any(bool4(int4(0, 0, 0, 0).x != _563.x, int4(0, 0, 0, 0).y != _563.y, int4(0, 0, 0, 0).z != _563.z, int4(0, 0, 0, 0).w != _563.w));
    v_u_xlati25 = 0;
    int param = v_u_xlati25;
    v_u_xlati25 = v_op_not(param);
    v_u_xlati13 = v_u_xlatb13.x ? v_u_xlati25 : 0;
    if (v_u_xlati13 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _599 = v_u_xlat16_8;
        float3 _601 = v_u_xlat16_7.xyz * _599.xyz;
        v_u_xlat16_8.x = _601.x;
        v_u_xlat16_8.y = _601.y;
        v_u_xlat16_8.z = _601.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _615 = v_u_xlat16_8;
        float3 _618 = _615.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _618.x;
        v_u_xlat16_8.y = _618.y;
        v_u_xlat16_8.z = _618.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb13 = bool4(v_u_xlat16_7.xyzz.x < v_u_xlat16_10.xyzz.x, v_u_xlat16_7.xyzz.y < v_u_xlat16_10.xyzz.y, v_u_xlat16_7.xyzz.z < v_u_xlat16_10.xyzz.z, v_u_xlat16_7.xyzz.w < v_u_xlat16_10.xyzz.w).xyz;
        float _679;
        if (v_u_xlatb13.x)
        {
            _679 = v_u_xlat16_8.x;
        }
        else
        {
            _679 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _679;
        float _691;
        if (v_u_xlatb13.y)
        {
            _691 = v_u_xlat16_8.y;
        }
        else
        {
            _691 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _691;
        float _703;
        if (v_u_xlatb13.z)
        {
            _703 = v_u_xlat16_8.z;
        }
        else
        {
            _703 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _703;
        float4 _713 = v_u_xlat16_4;
        v_u_xlat16_4.x = _713.xyz.x;
        v_u_xlat16_4.y = _713.xyz.y;
        v_u_xlat16_4.z = _713.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat40 = v_in_COLOR0.w;
    v_u_xlat16_4.w = v_u_xlat40;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _745 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _745.x;
    v_u_xlat16_6.y = _745.y;
    v_u_xlat16_6.z = _745.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat26.x = v_u_xlat16_7.w;
    v_u_xlat40 = v_u_xlat16_4.w * v_u_xlat26.x;
    v_u_xlat16_40 = v_u_xlat40;
    float3 _768 = v_u_xlat16_40.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _768.x;
    v_u_xlat16_4.y = _768.y;
    v_u_xlat16_4.z = _768.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_40;
    v_u_xlat26.x = 2.0f;
    v_u_xlat26.x = v_u_xlat1.x * v_u_xlat26.x;
    v_u_xlat26.x = min(v_u_xlat26.x, 1.0f);
    v_u_xlat26.x = sqrt(v_u_xlat26.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat26.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat26 = -v_u_xlat5.xy;
    v_u_xlat26 += v_u_xlat2.xy;
    v_u_xlat11 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat5.zw + v_u_xlat11;
    v_u_xlat31 = v_u_xlat26 / v_u_xlat11;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat31.x;
    v_u_xlat7.w = v_u_xlat31.y;
    v_u_xlat26.x = -v_u_xlat1.x;
    v_u_xlat18 = v_u_xlat0 + v_u_xlat26.x;
    v_u_xlat30 = v_u_xlat0 + v_u_xlat1.x;
    v_u_xlat16_6.x = v_u_xlat36;
    v_u_xlat16_6.y = v_u_xlat18;
    v_u_xlat16_6.z = v_u_xlat30;
    v_u_xlat16_6.w = v_u_xlat0;
    v_u_xlat26.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat38 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_26.x = v_u_xlat26.x;
    v_u_xlat16_26.y = v_u_xlat38;
    v_u_xlat11 = 2.0f.xx;
    v_u_xlat11 = v_u_xlat2.xy * v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.zw;
    float2 _935 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat1.x = _935.x;
    v_u_xlat1.y = _935.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _944 = v_u_xlat16_9;
    float2 _946 = v_u_xlat16_26 * _944.xy;
    v_u_xlat16_9.x = _946.x;
    v_u_xlat16_9.y = _946.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat11 = v_u_xlat12 + v_u_xlat9;
    v_u_xlat25 = 0.25f.xx / v_u_xlat11;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat25.x;
    v_u_xlat16_1.w = v_u_xlat25.y;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_COLOR1 = v_vs_COLOR1;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR1;
static float4 f_vs_COLOR0;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_COLOR1 : TEXCOORD1;
    float4 f_vs_TEXCOORD0 : TEXCOORD2;
    float4 f_vs_TEXCOORD1 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat3;
static float f_u_xlat0;
static float4 f_u_xlat16_1;
static float4 f_u_xlat16_2;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat3 = f_vs_TEXCOORD1.x;
    f_u_xlat0 = f_u_xlat3 * f_u_xlat16_0.x;
    f_u_xlat16_0.x = f_u_xlat0;
    f_u_xlat16_1.x = -f_vs_TEXCOORD1.z;
    f_u_xlat16_1.x = f_u_xlat16_0.x + f_u_xlat16_1.x;
    f_u_xlat16_1.x = max(f_u_xlat16_1.x, 0.0f);
    f_u_xlat16_1.x = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_2 = -f_vs_COLOR1;
    f_u_xlat16_2 += f_vs_COLOR0;
    f_u_xlat16_1 = f_u_xlat16_1.xxxx * f_u_xlat16_2;
    f_u_xlat16_1 += f_vs_COLOR1;
    f_u_xlat16_2.x = -f_vs_TEXCOORD1.y;
    f_u_xlat16_2.x = f_u_xlat16_0.x + f_u_xlat16_2.x;
    f_u_xlat16_2.x = max(f_u_xlat16_2.x, 0.0f);
    f_u_xlat16_2.x = min(f_u_xlat16_2.x, 1.0f);
    f_u_xlat16_0 = f_u_xlat16_1 * f_u_xlat16_2.xxxx;
    f_SV_Target0 = f_u_xlat16_0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR1 = stage_input.f_vs_COLOR1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(OUTLINE_ON) && defined(UNDERLAY_ON) && !defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float _UnderlaySoftness;
float _ScaleRatioC;
float _UnderlayDilate;
float _UnderlayOffsetX;
float _TextureWidth;
float _UnderlayOffsetY;
float _TextureHeight;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
float4 _UnderlayColor;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD3;
static float2 v_vs_TEXCOORD4;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD3 : TEXCOORD3;
    float2 v_vs_TEXCOORD4 : TEXCOORD4;
    float4 v_gl_Position : SV_Position;
};

static float4 v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat16;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat14;
static float2 v_u_xlat29;
static float2 v_u_xlat30;
static float v_u_xlat42;
static float v_u_xlat43;
static bool v_u_xlatb29;
static float4 v_u_xlat6;
static float v_u_xlat15;
static bool v_u_xlatb43;
static int v_u_xlati30;
static int v_u_xlati43;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static bool3 v_u_xlatb4;
static float4 v_u_xlat16_4;
static float v_u_xlat46;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_46;
static float v_u_xlat44;
static float v_u_xlat11;
static float v_u_xlat25;
static float2 v_u_xlat12;
static float v_u_xlat26;
static float2 v_u_xlat40;
static float2 v_u_xlat35;
static float4 v_u_xlat7;
static float v_u_xlat20;
static float v_u_xlat34;
static float2 v_u_xlat16_30;
static float2 v_u_xlat13;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;
static float2 v_u_xlat16_11;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD2;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0.x = 0.0f;
    v_u_xlatb0 = v_u_xlat0.x >= v_in_TEXCOORD0.w;
    v_u_xlat0.x = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat16 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat16;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat14 = v_u_xlat3.ww;
    v_u_xlat29 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat30 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat29 += v_u_xlat30;
    v_u_xlat30 = -v_u_xlat29;
    v_u_xlat29 = max(v_u_xlat29, v_u_xlat30);
    v_u_xlat29 *= float2(_ScaleX, _ScaleY);
    v_u_xlat14 /= v_u_xlat29;
    v_u_xlat42 = dot(v_u_xlat14, v_u_xlat14);
    v_u_xlat42 = rsqrt(v_u_xlat42);
    v_u_xlat29.x = -v_in_TEXCOORD0.w;
    v_u_xlat29.x = max(v_u_xlat29.x, v_in_TEXCOORD0.w);
    v_u_xlat29.x *= _GradientScale;
    v_u_xlat43 = 1.0f;
    v_u_xlat43 += _Sharpness;
    v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
    v_u_xlat42 *= v_u_xlat29.x;
    v_u_xlat29.x = 0.0f;
    v_u_xlatb29 = v_u_xlat29.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb29)
    {
        v_u_xlat29.x = -v_u_xlat42;
        v_u_xlat29.x = max(v_u_xlat42, v_u_xlat29.x);
        v_u_xlat43 = 1.0f;
        v_u_xlat30.x = -_PerspectiveFilter;
        v_u_xlat43 += v_u_xlat30.x;
        v_u_xlat29.x = v_u_xlat43 * v_u_xlat29.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat43 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat43 = rsqrt(v_u_xlat43);
        float3 _293 = v_u_xlat43.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _293.x;
        v_u_xlat4.y = _293.y;
        v_u_xlat4.z = _293.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _306 = v_u_xlat1;
        v_u_xlat1.x = _306.xy.x;
        v_u_xlat1.y = _306.xy.y;
        float3 _317 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _317.x;
        v_u_xlat5.y = _317.y;
        v_u_xlat5.z = _317.z;
        float3 _330 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _330.x;
        v_u_xlat6.y = _330.y;
        v_u_xlat6.z = _330.z;
        float4 _337 = v_u_xlat5;
        float3 _341 = _337.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _341.x;
        v_u_xlat5.y = _341.y;
        v_u_xlat5.z = _341.z;
        float3 _353 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _353.x;
        v_u_xlat6.y = _353.y;
        v_u_xlat6.z = _353.z;
        float4 _360 = v_u_xlat5;
        float3 _364 = _360.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _364.x;
        v_u_xlat5.y = _364.y;
        v_u_xlat5.z = _364.z;
        float4 _371 = v_u_xlat1;
        float3 _376 = _371.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _376.x;
        v_u_xlat1.y = _376.y;
        v_u_xlat1.w = _376.z;
        float4 _383 = v_u_xlat1;
        float3 _387 = _383.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _387.x;
        v_u_xlat1.y = _387.y;
        v_u_xlat1.w = _387.z;
        float4 _394 = v_u_xlat1;
        v_u_xlat1.x = _394.xyw.x;
        v_u_xlat1.y = _394.xyw.y;
        v_u_xlat1.w = _394.xyw.z;
        float4 _402 = v_u_xlat1;
        float3 _404 = -_402.xyw;
        v_u_xlat1.x = _404.x;
        v_u_xlat1.y = _404.y;
        v_u_xlat1.w = _404.z;
        float4 _411 = v_u_xlat1;
        float3 _416 = _411.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _416.x;
        v_u_xlat1.y = _416.y;
        v_u_xlat1.w = _416.z;
        float4 _423 = v_u_xlat1;
        v_u_xlat1.x = _423.xyw.x;
        v_u_xlat1.y = _423.xyw.y;
        v_u_xlat1.w = _423.xyw.z;
        v_u_xlat30.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat30.x = rsqrt(v_u_xlat30.x);
        float4 _441 = v_u_xlat1;
        float3 _445 = _441.xyw * v_u_xlat30.xxx;
        v_u_xlat1.x = _445.x;
        v_u_xlat1.y = _445.y;
        v_u_xlat1.w = _445.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat15 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat15, v_u_xlat1.x);
        v_u_xlat15 = -v_u_xlat29.x;
        v_u_xlat15 = v_u_xlat42 + v_u_xlat15;
        v_u_xlat1.x = v_u_xlat15 * v_u_xlat1.x;
        v_u_xlat42 = v_u_xlat1.x + v_u_xlat29.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0.x *= v_u_xlat1.x;
    v_u_xlat0.x += _WeightNormal;
    v_u_xlat0.x /= 4.0f;
    v_u_xlat0.x += _FaceDilate;
    v_u_xlat0.x *= _ScaleRatioA;
    v_u_xlat0.x *= 0.5f;
    v_u_xlat42 = v_u_xlat42;
    v_u_xlat1.x = 1.0f;
    v_u_xlat15 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat15 = v_u_xlat42 * v_u_xlat15;
    v_u_xlat1.x = v_u_xlat15 + v_u_xlat1.x;
    v_u_xlat1.x = v_u_xlat42 / v_u_xlat1.x;
    v_u_xlat15 = -v_u_xlat0.x;
    v_u_xlat15 += 0.5f;
    v_u_xlat15 = v_u_xlat1.x * v_u_xlat15;
    v_u_xlat29.x = -0.5f;
    v_u_xlat15 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat29.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat29.x *= 0.5f;
    v_u_xlat29.x = v_u_xlat1.x * v_u_xlat29.x;
    int4 _581 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb43 = any(bool4(int4(0, 0, 0, 0).x != _581.x, int4(0, 0, 0, 0).y != _581.y, int4(0, 0, 0, 0).z != _581.z, int4(0, 0, 0, 0).w != _581.w));
    v_u_xlati30 = 0;
    int param = v_u_xlati30;
    v_u_xlati30 = v_op_not(param);
    v_u_xlati43 = v_u_xlatb43 ? v_u_xlati30 : 0;
    if (v_u_xlati43 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _615 = v_u_xlat16_8;
        float3 _617 = v_u_xlat16_7.xyz * _615.xyz;
        v_u_xlat16_8.x = _617.x;
        v_u_xlat16_8.y = _617.y;
        v_u_xlat16_8.z = _617.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _631 = v_u_xlat16_8;
        float3 _634 = _631.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _634.x;
        v_u_xlat16_8.y = _634.y;
        v_u_xlat16_8.z = _634.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb4 = bool4(v_u_xlat16_7.xyzx.x < v_u_xlat16_10.xyzx.x, v_u_xlat16_7.xyzx.y < v_u_xlat16_10.xyzx.y, v_u_xlat16_7.xyzx.z < v_u_xlat16_10.xyzx.z, v_u_xlat16_7.xyzx.w < v_u_xlat16_10.xyzx.w).xyz;
        float _698;
        if (v_u_xlatb4.x)
        {
            _698 = v_u_xlat16_8.x;
        }
        else
        {
            _698 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _698;
        float _710;
        if (v_u_xlatb4.y)
        {
            _710 = v_u_xlat16_8.y;
        }
        else
        {
            _710 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _710;
        float _722;
        if (v_u_xlatb4.z)
        {
            _722 = v_u_xlat16_8.z;
        }
        else
        {
            _722 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _722;
        float4 _732 = v_u_xlat16_4;
        v_u_xlat16_4.x = _732.xyz.x;
        v_u_xlat16_4.y = _732.xyz.y;
        v_u_xlat16_4.z = _732.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat46 = 1.0f;
    v_u_xlat16_4.w = v_u_xlat46;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _762 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _762.x;
    v_u_xlat16_6.y = _762.y;
    v_u_xlat16_6.z = _762.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat30.x = v_u_xlat16_7.w;
    v_u_xlat46 = v_u_xlat16_4.w * v_u_xlat30.x;
    v_u_xlat16_46 = v_u_xlat46;
    float3 _785 = v_u_xlat16_46.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _785.x;
    v_u_xlat16_4.y = _785.y;
    v_u_xlat16_4.z = _785.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_46;
    v_u_xlat30.x = 2.0f;
    v_u_xlat30.x = v_u_xlat29.x * v_u_xlat30.x;
    v_u_xlat30.x = min(v_u_xlat30.x, 1.0f);
    v_u_xlat30.x = sqrt(v_u_xlat30.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat30.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat30.x = 1.0f;
    v_u_xlat44 = _UnderlaySoftness * _ScaleRatioC;
    v_u_xlat44 = v_u_xlat42 * v_u_xlat44;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat11 = v_u_xlat42 / v_u_xlat30.x;
    v_u_xlat30.x = -v_u_xlat0.x;
    v_u_xlat30.x += 0.5f;
    v_u_xlat30.x = v_u_xlat11 * v_u_xlat30.x;
    v_u_xlat44 = -0.5f;
    v_u_xlat30.x = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat44 = _UnderlayDilate * _ScaleRatioC;
    v_u_xlat44 *= 0.5f;
    v_u_xlat44 = v_u_xlat11 * v_u_xlat44;
    v_u_xlat44 = -v_u_xlat44;
    v_u_xlat25 = v_u_xlat44 + v_u_xlat30.x;
    v_u_xlat30.x = _UnderlayOffsetX * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat12.x = v_u_xlat30.x / _TextureWidth;
    v_u_xlat30.x = _UnderlayOffsetY * _ScaleRatioC;
    v_u_xlat30.x = -v_u_xlat30.x;
    v_u_xlat30.x *= _GradientScale;
    v_u_xlat26 = v_u_xlat30.x / _TextureHeight;
    v_u_xlat12.x = v_u_xlat12.x;
    v_u_xlat12.y = v_u_xlat26;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat30 = -v_u_xlat5.xy;
    v_u_xlat30 += v_u_xlat2.xy;
    v_u_xlat40 = -v_u_xlat5.xy;
    v_u_xlat40 = v_u_xlat5.zw + v_u_xlat40;
    v_u_xlat35 = v_u_xlat30 / v_u_xlat40;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat35.x;
    v_u_xlat7.w = v_u_xlat35.y;
    v_u_xlat30.x = -v_u_xlat29.x;
    v_u_xlat20 = v_u_xlat15 + v_u_xlat30.x;
    v_u_xlat34 = v_u_xlat29.x + v_u_xlat15;
    v_u_xlat16_6.x = v_u_xlat1.x;
    v_u_xlat16_6.y = v_u_xlat20;
    v_u_xlat16_6.z = v_u_xlat34;
    v_u_xlat16_6.w = v_u_xlat15;
    v_u_xlat30.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat44 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_30.x = v_u_xlat30.x;
    v_u_xlat16_30.y = v_u_xlat44;
    v_u_xlat40 = 2.0f.xx;
    v_u_xlat40 = v_u_xlat2.xy * v_u_xlat40;
    v_u_xlat13 = -v_u_xlat5.xy;
    v_u_xlat40 += v_u_xlat13;
    v_u_xlat13 = -v_u_xlat5.zw;
    float2 _1052 = v_u_xlat40 + v_u_xlat13;
    v_u_xlat1.x = _1052.x;
    v_u_xlat1.y = _1052.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _1061 = v_u_xlat16_9;
    float2 _1063 = v_u_xlat16_30 * _1061.xy;
    v_u_xlat16_9.x = _1063.x;
    v_u_xlat16_9.y = _1063.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat40 = v_u_xlat14 + v_u_xlat9;
    v_u_xlat29 = 0.25f.xx / v_u_xlat40;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat29.x;
    v_u_xlat16_1.w = v_u_xlat29.y;
    float2 _1092 = v_u_xlat12 + v_in_TEXCOORD0.xy;
    v_u_xlat0.x = _1092.x;
    v_u_xlat0.y = _1092.y;
    v_u_xlat0.z = v_in_COLOR0.w;
    v_u_xlat0.w = 0.0f;
    v_u_xlat16_11.x = v_u_xlat11;
    v_u_xlat16_11.y = v_u_xlat25;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
    v_vs_TEXCOORD3 = v_u_xlat0;
    v_vs_TEXCOORD4 = v_u_xlat16_11;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD3 = v_vs_TEXCOORD3;
    stage_output.v_vs_TEXCOORD4 = v_vs_TEXCOORD4;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD3;
static float2 f_vs_TEXCOORD4;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD3 : TEXCOORD3;
    float2 f_vs_TEXCOORD4 : TEXCOORD4;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat5;
static float4 f_u_xlat0;
static float3 f_u_xlat16_1;
static float f_u_xlat16_6;
static float4 f_u_xlat16_2;
static float f_u_xlat16_3;
static float f_u_xlat8;
static float f_u_xlat3;
static float4 f_u_xlat1;
static float f_u_xlat16_4;
static float4 f_u_xlat2;
static float f_u_xlat16_9;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat5 = f_vs_TEXCOORD1.x;
    f_u_xlat0.x = f_u_xlat5 * f_u_xlat16_0.x;
    f_u_xlat16_1.x = f_u_xlat0.x;
    f_u_xlat16_6 = -f_vs_TEXCOORD1.w;
    f_u_xlat16_1.x = f_u_xlat16_6 + f_u_xlat16_1.x;
    f_u_xlat16_1.x = max(f_u_xlat16_1.x, 0.0f);
    f_u_xlat16_2.x = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_0 = f_u_xlat16_2.xxxx * f_vs_COLOR0;
    f_u_xlat16_3 = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD3.xy).w;
    f_u_xlat8 = f_vs_TEXCOORD4.x;
    f_u_xlat3 = f_u_xlat8 * f_u_xlat16_3;
    f_u_xlat16_3 = f_u_xlat3;
    f_u_xlat16_1 = _UnderlayColor.www * _UnderlayColor.xyz;
    f_u_xlat1.x = f_u_xlat16_1.x;
    f_u_xlat1.y = f_u_xlat16_1.y;
    f_u_xlat1.z = f_u_xlat16_1.z;
    f_u_xlat1.w = _UnderlayColor.w;
    f_u_xlat16_4 = -f_vs_TEXCOORD4.y;
    f_u_xlat16_4 = f_u_xlat16_3 + f_u_xlat16_4;
    f_u_xlat16_4 = max(f_u_xlat16_4, 0.0f);
    f_u_xlat16_2 = min(f_u_xlat16_4.xxxx, 1.0f.xxxx);
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat16_4 = 1.0f;
    f_u_xlat16_9 = -f_u_xlat16_0.w;
    f_u_xlat16_2 = f_u_xlat16_9.xxxx + f_u_xlat16_4.xxxx;
    f_u_xlat2 = f_u_xlat16_2;
    f_u_xlat1 *= f_u_xlat2;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 = f_u_xlat1 + f_u_xlat0;
    f_u_xlat16_0 = f_u_xlat0;
    f_u_xlat0 = f_u_xlat16_0;
    f_u_xlat0 *= f_vs_TEXCOORD3.zzzz;
    f_u_xlat16_0 = f_u_xlat0;
    f_SV_Target0 = f_u_xlat16_0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD3 = stage_input.f_vs_TEXCOORD3;
    f_vs_TEXCOORD4 = stage_input.f_vs_TEXCOORD4;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(OUTLINE_ON) && !defined(UNDERLAY_ON) && defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat14;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat12;
static float2 v_u_xlat25;
static float2 v_u_xlat26;
static float v_u_xlat36;
static float v_u_xlat37;
static bool v_u_xlatb25;
static float4 v_u_xlat6;
static float v_u_xlat13;
static bool3 v_u_xlatb13;
static int v_u_xlati25;
static int v_u_xlati13;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static float4 v_u_xlat16_4;
static float v_u_xlat40;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_40;
static float2 v_u_xlat11;
static float2 v_u_xlat31;
static float4 v_u_xlat7;
static float v_u_xlat18;
static float v_u_xlat30;
static float v_u_xlat38;
static float2 v_u_xlat16_26;
static float2 v_u_xlat35;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD2;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0 = 0.0f;
    v_u_xlatb0 = v_u_xlat0 >= v_in_TEXCOORD0.w;
    v_u_xlat0 = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat14 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat14;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat12 = v_u_xlat3.ww;
    v_u_xlat25 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat26 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat25 += v_u_xlat26;
    v_u_xlat26 = -v_u_xlat25;
    v_u_xlat25 = max(v_u_xlat25, v_u_xlat26);
    v_u_xlat25 *= float2(_ScaleX, _ScaleY);
    v_u_xlat12 /= v_u_xlat25;
    v_u_xlat36 = dot(v_u_xlat12, v_u_xlat12);
    v_u_xlat36 = rsqrt(v_u_xlat36);
    v_u_xlat25.x = -v_in_TEXCOORD0.w;
    v_u_xlat25.x = max(v_u_xlat25.x, v_in_TEXCOORD0.w);
    v_u_xlat25.x *= _GradientScale;
    v_u_xlat37 = 1.0f;
    v_u_xlat37 += _Sharpness;
    v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
    v_u_xlat36 *= v_u_xlat25.x;
    v_u_xlat25.x = 0.0f;
    v_u_xlatb25 = v_u_xlat25.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb25)
    {
        v_u_xlat25.x = -v_u_xlat36;
        v_u_xlat25.x = max(v_u_xlat36, v_u_xlat25.x);
        v_u_xlat37 = 1.0f;
        v_u_xlat26.x = -_PerspectiveFilter;
        v_u_xlat37 += v_u_xlat26.x;
        v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat37 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat37 = rsqrt(v_u_xlat37);
        float3 _290 = v_u_xlat37.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _290.x;
        v_u_xlat4.y = _290.y;
        v_u_xlat4.z = _290.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _303 = v_u_xlat1;
        v_u_xlat1.x = _303.xy.x;
        v_u_xlat1.y = _303.xy.y;
        float3 _314 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _314.x;
        v_u_xlat5.y = _314.y;
        v_u_xlat5.z = _314.z;
        float3 _327 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _327.x;
        v_u_xlat6.y = _327.y;
        v_u_xlat6.z = _327.z;
        float4 _334 = v_u_xlat5;
        float3 _338 = _334.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _338.x;
        v_u_xlat5.y = _338.y;
        v_u_xlat5.z = _338.z;
        float3 _350 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _350.x;
        v_u_xlat6.y = _350.y;
        v_u_xlat6.z = _350.z;
        float4 _357 = v_u_xlat5;
        float3 _361 = _357.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _361.x;
        v_u_xlat5.y = _361.y;
        v_u_xlat5.z = _361.z;
        float4 _368 = v_u_xlat1;
        float3 _373 = _368.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _373.x;
        v_u_xlat1.y = _373.y;
        v_u_xlat1.w = _373.z;
        float4 _380 = v_u_xlat1;
        float3 _384 = _380.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _384.x;
        v_u_xlat1.y = _384.y;
        v_u_xlat1.w = _384.z;
        float4 _391 = v_u_xlat1;
        v_u_xlat1.x = _391.xyw.x;
        v_u_xlat1.y = _391.xyw.y;
        v_u_xlat1.w = _391.xyw.z;
        float4 _399 = v_u_xlat1;
        float3 _401 = -_399.xyw;
        v_u_xlat1.x = _401.x;
        v_u_xlat1.y = _401.y;
        v_u_xlat1.w = _401.z;
        float4 _408 = v_u_xlat1;
        float3 _413 = _408.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _413.x;
        v_u_xlat1.y = _413.y;
        v_u_xlat1.w = _413.z;
        float4 _420 = v_u_xlat1;
        v_u_xlat1.x = _420.xyw.x;
        v_u_xlat1.y = _420.xyw.y;
        v_u_xlat1.w = _420.xyw.z;
        v_u_xlat26.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat26.x = rsqrt(v_u_xlat26.x);
        float4 _438 = v_u_xlat1;
        float3 _442 = _438.xyw * v_u_xlat26.xxx;
        v_u_xlat1.x = _442.x;
        v_u_xlat1.y = _442.y;
        v_u_xlat1.w = _442.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat13 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat13, v_u_xlat1.x);
        v_u_xlat13 = -v_u_xlat25.x;
        v_u_xlat13 = v_u_xlat36 + v_u_xlat13;
        v_u_xlat1.x = v_u_xlat13 * v_u_xlat1.x;
        v_u_xlat36 = v_u_xlat1.x + v_u_xlat25.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0 *= v_u_xlat1.x;
    v_u_xlat0 += _WeightNormal;
    v_u_xlat0 /= 4.0f;
    v_u_xlat0 += _FaceDilate;
    v_u_xlat0 *= _ScaleRatioA;
    v_u_xlat0 *= 0.5f;
    v_u_xlat1.x = 1.0f;
    v_u_xlat13 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat13 = v_u_xlat36 * v_u_xlat13;
    v_u_xlat1.x = v_u_xlat13 + v_u_xlat1.x;
    v_u_xlat36 /= v_u_xlat1.x;
    v_u_xlat0 = -v_u_xlat0;
    v_u_xlat0 += 0.5f;
    v_u_xlat0 = v_u_xlat36 * v_u_xlat0;
    v_u_xlat1.x = -0.5f;
    v_u_xlat0 += v_u_xlat1.x;
    v_u_xlat1.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat1.x *= 0.5f;
    v_u_xlat1.x = v_u_xlat36 * v_u_xlat1.x;
    int4 _563 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb13.x = any(bool4(int4(0, 0, 0, 0).x != _563.x, int4(0, 0, 0, 0).y != _563.y, int4(0, 0, 0, 0).z != _563.z, int4(0, 0, 0, 0).w != _563.w));
    v_u_xlati25 = 0;
    int param = v_u_xlati25;
    v_u_xlati25 = v_op_not(param);
    v_u_xlati13 = v_u_xlatb13.x ? v_u_xlati25 : 0;
    if (v_u_xlati13 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _599 = v_u_xlat16_8;
        float3 _601 = v_u_xlat16_7.xyz * _599.xyz;
        v_u_xlat16_8.x = _601.x;
        v_u_xlat16_8.y = _601.y;
        v_u_xlat16_8.z = _601.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _615 = v_u_xlat16_8;
        float3 _618 = _615.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _618.x;
        v_u_xlat16_8.y = _618.y;
        v_u_xlat16_8.z = _618.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb13 = bool4(v_u_xlat16_7.xyzz.x < v_u_xlat16_10.xyzz.x, v_u_xlat16_7.xyzz.y < v_u_xlat16_10.xyzz.y, v_u_xlat16_7.xyzz.z < v_u_xlat16_10.xyzz.z, v_u_xlat16_7.xyzz.w < v_u_xlat16_10.xyzz.w).xyz;
        float _679;
        if (v_u_xlatb13.x)
        {
            _679 = v_u_xlat16_8.x;
        }
        else
        {
            _679 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _679;
        float _691;
        if (v_u_xlatb13.y)
        {
            _691 = v_u_xlat16_8.y;
        }
        else
        {
            _691 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _691;
        float _703;
        if (v_u_xlatb13.z)
        {
            _703 = v_u_xlat16_8.z;
        }
        else
        {
            _703 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _703;
        float4 _713 = v_u_xlat16_4;
        v_u_xlat16_4.x = _713.xyz.x;
        v_u_xlat16_4.y = _713.xyz.y;
        v_u_xlat16_4.z = _713.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat40 = v_in_COLOR0.w;
    v_u_xlat16_4.w = v_u_xlat40;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _745 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _745.x;
    v_u_xlat16_6.y = _745.y;
    v_u_xlat16_6.z = _745.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat26.x = v_u_xlat16_7.w;
    v_u_xlat40 = v_u_xlat16_4.w * v_u_xlat26.x;
    v_u_xlat16_40 = v_u_xlat40;
    float3 _768 = v_u_xlat16_40.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _768.x;
    v_u_xlat16_4.y = _768.y;
    v_u_xlat16_4.z = _768.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_40;
    v_u_xlat26.x = 2.0f;
    v_u_xlat26.x = v_u_xlat1.x * v_u_xlat26.x;
    v_u_xlat26.x = min(v_u_xlat26.x, 1.0f);
    v_u_xlat26.x = sqrt(v_u_xlat26.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat26.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat26 = -v_u_xlat5.xy;
    v_u_xlat26 += v_u_xlat2.xy;
    v_u_xlat11 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat5.zw + v_u_xlat11;
    v_u_xlat31 = v_u_xlat26 / v_u_xlat11;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat31.x;
    v_u_xlat7.w = v_u_xlat31.y;
    v_u_xlat26.x = -v_u_xlat1.x;
    v_u_xlat18 = v_u_xlat0 + v_u_xlat26.x;
    v_u_xlat30 = v_u_xlat0 + v_u_xlat1.x;
    v_u_xlat16_6.x = v_u_xlat36;
    v_u_xlat16_6.y = v_u_xlat18;
    v_u_xlat16_6.z = v_u_xlat30;
    v_u_xlat16_6.w = v_u_xlat0;
    v_u_xlat26.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat38 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_26.x = v_u_xlat26.x;
    v_u_xlat16_26.y = v_u_xlat38;
    v_u_xlat11 = 2.0f.xx;
    v_u_xlat11 = v_u_xlat2.xy * v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.zw;
    float2 _935 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat1.x = _935.x;
    v_u_xlat1.y = _935.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _944 = v_u_xlat16_9;
    float2 _946 = v_u_xlat16_26 * _944.xy;
    v_u_xlat16_9.x = _946.x;
    v_u_xlat16_9.y = _946.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat11 = v_u_xlat12 + v_u_xlat9;
    v_u_xlat25 = 0.25f.xx / v_u_xlat11;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat25.x;
    v_u_xlat16_1.w = v_u_xlat25.y;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
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
static float f_u_xlat3;
static float f_u_xlat0;
static float f_u_xlat16_1;
static float f_u_xlat1;
static bool f_u_xlatb2;
static int f_u_xlati2;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat3 = f_vs_TEXCOORD1.x;
    f_u_xlat0 = f_u_xlat3 * f_u_xlat16_0.x;
    f_u_xlat16_0.x = f_u_xlat0;
    f_u_xlat16_1 = -f_vs_TEXCOORD1.w;
    f_u_xlat16_1 = f_u_xlat16_0.x + f_u_xlat16_1;
    f_u_xlat16_1 = max(f_u_xlat16_1, 0.0f);
    f_u_xlat16_1 = min(f_u_xlat16_1, 1.0f);
    f_u_xlat16_0 = f_u_xlat16_1.xxxx * f_vs_COLOR0;
    f_u_xlat16_1 = 0.001000000047497451305389404296875f;
    f_u_xlat16_1 = -f_u_xlat16_1;
    f_u_xlat16_1 = f_u_xlat16_0.w + f_u_xlat16_1;
    f_u_xlat1 = f_u_xlat16_1;
    f_u_xlatb2 = f_u_xlat1 < 0.0f;
    f_u_xlati2 = f_u_xlatb2 ? (-1) : 0;
    if (f_u_xlati2 != 0)
    {
        discard;
    }
    f_SV_Target0 = f_u_xlat16_0;
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
#elif !defined(OUTLINE_ON) && !defined(UNDERLAY_ON) && !defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;
static float4 v_vs_TEXCOORD2;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_vs_TEXCOORD2 : TEXCOORD3;
    float4 v_gl_Position : SV_Position;
};

static float v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat14;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat12;
static float2 v_u_xlat25;
static float2 v_u_xlat26;
static float v_u_xlat36;
static float v_u_xlat37;
static bool v_u_xlatb25;
static float4 v_u_xlat6;
static float v_u_xlat13;
static bool3 v_u_xlatb13;
static int v_u_xlati25;
static int v_u_xlati13;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static float4 v_u_xlat16_4;
static float v_u_xlat40;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_40;
static float2 v_u_xlat11;
static float2 v_u_xlat31;
static float4 v_u_xlat7;
static float v_u_xlat18;
static float v_u_xlat30;
static float v_u_xlat38;
static float2 v_u_xlat16_26;
static float2 v_u_xlat35;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;
static float4 v_vs_COLOR1;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0 = 0.0f;
    v_u_xlatb0 = v_u_xlat0 >= v_in_TEXCOORD0.w;
    v_u_xlat0 = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat14 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat14;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat12 = v_u_xlat3.ww;
    v_u_xlat25 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat26 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat25 += v_u_xlat26;
    v_u_xlat26 = -v_u_xlat25;
    v_u_xlat25 = max(v_u_xlat25, v_u_xlat26);
    v_u_xlat25 *= float2(_ScaleX, _ScaleY);
    v_u_xlat12 /= v_u_xlat25;
    v_u_xlat36 = dot(v_u_xlat12, v_u_xlat12);
    v_u_xlat36 = rsqrt(v_u_xlat36);
    v_u_xlat25.x = -v_in_TEXCOORD0.w;
    v_u_xlat25.x = max(v_u_xlat25.x, v_in_TEXCOORD0.w);
    v_u_xlat25.x *= _GradientScale;
    v_u_xlat37 = 1.0f;
    v_u_xlat37 += _Sharpness;
    v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
    v_u_xlat36 *= v_u_xlat25.x;
    v_u_xlat25.x = 0.0f;
    v_u_xlatb25 = v_u_xlat25.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb25)
    {
        v_u_xlat25.x = -v_u_xlat36;
        v_u_xlat25.x = max(v_u_xlat36, v_u_xlat25.x);
        v_u_xlat37 = 1.0f;
        v_u_xlat26.x = -_PerspectiveFilter;
        v_u_xlat37 += v_u_xlat26.x;
        v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat37 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat37 = rsqrt(v_u_xlat37);
        float3 _290 = v_u_xlat37.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _290.x;
        v_u_xlat4.y = _290.y;
        v_u_xlat4.z = _290.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _303 = v_u_xlat1;
        v_u_xlat1.x = _303.xy.x;
        v_u_xlat1.y = _303.xy.y;
        float3 _314 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _314.x;
        v_u_xlat5.y = _314.y;
        v_u_xlat5.z = _314.z;
        float3 _327 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _327.x;
        v_u_xlat6.y = _327.y;
        v_u_xlat6.z = _327.z;
        float4 _334 = v_u_xlat5;
        float3 _338 = _334.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _338.x;
        v_u_xlat5.y = _338.y;
        v_u_xlat5.z = _338.z;
        float3 _350 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _350.x;
        v_u_xlat6.y = _350.y;
        v_u_xlat6.z = _350.z;
        float4 _357 = v_u_xlat5;
        float3 _361 = _357.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _361.x;
        v_u_xlat5.y = _361.y;
        v_u_xlat5.z = _361.z;
        float4 _368 = v_u_xlat1;
        float3 _373 = _368.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _373.x;
        v_u_xlat1.y = _373.y;
        v_u_xlat1.w = _373.z;
        float4 _380 = v_u_xlat1;
        float3 _384 = _380.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _384.x;
        v_u_xlat1.y = _384.y;
        v_u_xlat1.w = _384.z;
        float4 _391 = v_u_xlat1;
        v_u_xlat1.x = _391.xyw.x;
        v_u_xlat1.y = _391.xyw.y;
        v_u_xlat1.w = _391.xyw.z;
        float4 _399 = v_u_xlat1;
        float3 _401 = -_399.xyw;
        v_u_xlat1.x = _401.x;
        v_u_xlat1.y = _401.y;
        v_u_xlat1.w = _401.z;
        float4 _408 = v_u_xlat1;
        float3 _413 = _408.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _413.x;
        v_u_xlat1.y = _413.y;
        v_u_xlat1.w = _413.z;
        float4 _420 = v_u_xlat1;
        v_u_xlat1.x = _420.xyw.x;
        v_u_xlat1.y = _420.xyw.y;
        v_u_xlat1.w = _420.xyw.z;
        v_u_xlat26.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat26.x = rsqrt(v_u_xlat26.x);
        float4 _438 = v_u_xlat1;
        float3 _442 = _438.xyw * v_u_xlat26.xxx;
        v_u_xlat1.x = _442.x;
        v_u_xlat1.y = _442.y;
        v_u_xlat1.w = _442.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat13 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat13, v_u_xlat1.x);
        v_u_xlat13 = -v_u_xlat25.x;
        v_u_xlat13 = v_u_xlat36 + v_u_xlat13;
        v_u_xlat1.x = v_u_xlat13 * v_u_xlat1.x;
        v_u_xlat36 = v_u_xlat1.x + v_u_xlat25.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0 *= v_u_xlat1.x;
    v_u_xlat0 += _WeightNormal;
    v_u_xlat0 /= 4.0f;
    v_u_xlat0 += _FaceDilate;
    v_u_xlat0 *= _ScaleRatioA;
    v_u_xlat0 *= 0.5f;
    v_u_xlat1.x = 1.0f;
    v_u_xlat13 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat13 = v_u_xlat36 * v_u_xlat13;
    v_u_xlat1.x = v_u_xlat13 + v_u_xlat1.x;
    v_u_xlat36 /= v_u_xlat1.x;
    v_u_xlat0 = -v_u_xlat0;
    v_u_xlat0 += 0.5f;
    v_u_xlat0 = v_u_xlat36 * v_u_xlat0;
    v_u_xlat1.x = -0.5f;
    v_u_xlat0 += v_u_xlat1.x;
    v_u_xlat1.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat1.x *= 0.5f;
    v_u_xlat1.x = v_u_xlat36 * v_u_xlat1.x;
    int4 _563 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb13.x = any(bool4(int4(0, 0, 0, 0).x != _563.x, int4(0, 0, 0, 0).y != _563.y, int4(0, 0, 0, 0).z != _563.z, int4(0, 0, 0, 0).w != _563.w));
    v_u_xlati25 = 0;
    int param = v_u_xlati25;
    v_u_xlati25 = v_op_not(param);
    v_u_xlati13 = v_u_xlatb13.x ? v_u_xlati25 : 0;
    if (v_u_xlati13 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _599 = v_u_xlat16_8;
        float3 _601 = v_u_xlat16_7.xyz * _599.xyz;
        v_u_xlat16_8.x = _601.x;
        v_u_xlat16_8.y = _601.y;
        v_u_xlat16_8.z = _601.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _615 = v_u_xlat16_8;
        float3 _618 = _615.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _618.x;
        v_u_xlat16_8.y = _618.y;
        v_u_xlat16_8.z = _618.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb13 = bool4(v_u_xlat16_7.xyzz.x < v_u_xlat16_10.xyzz.x, v_u_xlat16_7.xyzz.y < v_u_xlat16_10.xyzz.y, v_u_xlat16_7.xyzz.z < v_u_xlat16_10.xyzz.z, v_u_xlat16_7.xyzz.w < v_u_xlat16_10.xyzz.w).xyz;
        float _679;
        if (v_u_xlatb13.x)
        {
            _679 = v_u_xlat16_8.x;
        }
        else
        {
            _679 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _679;
        float _691;
        if (v_u_xlatb13.y)
        {
            _691 = v_u_xlat16_8.y;
        }
        else
        {
            _691 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _691;
        float _703;
        if (v_u_xlatb13.z)
        {
            _703 = v_u_xlat16_8.z;
        }
        else
        {
            _703 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _703;
        float4 _713 = v_u_xlat16_4;
        v_u_xlat16_4.x = _713.xyz.x;
        v_u_xlat16_4.y = _713.xyz.y;
        v_u_xlat16_4.z = _713.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat40 = v_in_COLOR0.w;
    v_u_xlat16_4.w = v_u_xlat40;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _745 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _745.x;
    v_u_xlat16_6.y = _745.y;
    v_u_xlat16_6.z = _745.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat26.x = v_u_xlat16_7.w;
    v_u_xlat40 = v_u_xlat16_4.w * v_u_xlat26.x;
    v_u_xlat16_40 = v_u_xlat40;
    float3 _768 = v_u_xlat16_40.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _768.x;
    v_u_xlat16_4.y = _768.y;
    v_u_xlat16_4.z = _768.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_40;
    v_u_xlat26.x = 2.0f;
    v_u_xlat26.x = v_u_xlat1.x * v_u_xlat26.x;
    v_u_xlat26.x = min(v_u_xlat26.x, 1.0f);
    v_u_xlat26.x = sqrt(v_u_xlat26.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat26.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat26 = -v_u_xlat5.xy;
    v_u_xlat26 += v_u_xlat2.xy;
    v_u_xlat11 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat5.zw + v_u_xlat11;
    v_u_xlat31 = v_u_xlat26 / v_u_xlat11;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat31.x;
    v_u_xlat7.w = v_u_xlat31.y;
    v_u_xlat26.x = -v_u_xlat1.x;
    v_u_xlat18 = v_u_xlat0 + v_u_xlat26.x;
    v_u_xlat30 = v_u_xlat0 + v_u_xlat1.x;
    v_u_xlat16_6.x = v_u_xlat36;
    v_u_xlat16_6.y = v_u_xlat18;
    v_u_xlat16_6.z = v_u_xlat30;
    v_u_xlat16_6.w = v_u_xlat0;
    v_u_xlat26.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat38 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_26.x = v_u_xlat26.x;
    v_u_xlat16_26.y = v_u_xlat38;
    v_u_xlat11 = 2.0f.xx;
    v_u_xlat11 = v_u_xlat2.xy * v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.zw;
    float2 _935 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat1.x = _935.x;
    v_u_xlat1.y = _935.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _944 = v_u_xlat16_9;
    float2 _946 = v_u_xlat16_26 * _944.xy;
    v_u_xlat16_9.x = _946.x;
    v_u_xlat16_9.y = _946.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat11 = v_u_xlat12 + v_u_xlat9;
    v_u_xlat25 = 0.25f.xx / v_u_xlat11;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat25.x;
    v_u_xlat16_1.w = v_u_xlat25.y;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
    v_vert_main();
    v_SPIRV_Cross_Output stage_output;
    stage_output.v_gl_Position = v_gl_Position;
    stage_output.v_vs_COLOR0 = v_vs_COLOR0;
    stage_output.v_vs_TEXCOORD0 = v_vs_TEXCOORD0;
    stage_output.v_vs_TEXCOORD1 = v_vs_TEXCOORD1;
    stage_output.v_vs_TEXCOORD2 = v_vs_TEXCOORD2;
    return stage_output;
}
static float4 f_vs_TEXCOORD0;
static float4 f_vs_TEXCOORD1;
static float4 f_vs_COLOR0;
static float4 f_vs_TEXCOORD2;
static float4 f_SV_Target0;

struct f_SPIRV_Cross_Input
{
    float4 f_vs_COLOR0 : TEXCOORD0;
    float4 f_vs_TEXCOORD0 : TEXCOORD1;
    float4 f_vs_TEXCOORD1 : TEXCOORD2;
    float4 f_vs_TEXCOORD2 : TEXCOORD3;
};

struct f_SPIRV_Cross_Output
{
    float4 f_SV_Target0 : SV_Target0;
};

static float4 f_u_xlat16_0;
static float f_u_xlat4;
static float f_u_xlat0;
static float2 f_u_xlat16_1;
static float2 f_u_xlat2;
static float2 f_u_xlat1;
static float2 f_u_xlat10;
static float2 f_u_xlat16_2;
static float f_u_xlat16_3;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat4 = f_vs_TEXCOORD1.x;
    f_u_xlat0 = f_u_xlat4 * f_u_xlat16_0.x;
    f_u_xlat16_0.x = f_u_xlat0;
    f_u_xlat16_1.x = -f_vs_TEXCOORD1.w;
    f_u_xlat16_1.x = f_u_xlat16_0.x + f_u_xlat16_1.x;
    f_u_xlat16_1.x = max(f_u_xlat16_1.x, 0.0f);
    f_u_xlat16_1.x = min(f_u_xlat16_1.x, 1.0f);
    f_u_xlat16_0 = f_u_xlat16_1.xxxx * f_vs_COLOR0;
    f_u_xlat2 = -_ClipRect.xy;
    f_u_xlat2 += _ClipRect.zw;
    f_u_xlat16_1 = -f_vs_TEXCOORD2.xy;
    f_u_xlat16_1 = max(f_u_xlat16_1, f_vs_TEXCOORD2.xy);
    f_u_xlat1 = f_u_xlat16_1;
    f_u_xlat10 = -f_u_xlat1;
    f_u_xlat2 = f_u_xlat10 + f_u_xlat2;
    f_u_xlat10 = f_vs_TEXCOORD2.zw;
    f_u_xlat2 = f_u_xlat10 * f_u_xlat2;
    f_u_xlat2 = max(f_u_xlat2, 0.0f.xx);
    f_u_xlat2 = min(f_u_xlat2, 1.0f.xx);
    f_u_xlat16_2 = f_u_xlat2;
    f_u_xlat16_3 = f_u_xlat16_2.y * f_u_xlat16_2.x;
    f_u_xlat16_0 *= f_u_xlat16_3.xxxx;
    f_SV_Target0 = f_u_xlat16_0;
}

f_SPIRV_Cross_Output RecoveryFrag(f_SPIRV_Cross_Input stage_input)
{
    f_vs_TEXCOORD0 = stage_input.f_vs_TEXCOORD0;
    f_vs_TEXCOORD1 = stage_input.f_vs_TEXCOORD1;
    f_vs_COLOR0 = stage_input.f_vs_COLOR0;
    f_vs_TEXCOORD2 = stage_input.f_vs_TEXCOORD2;
    f_frag_main();
    f_SPIRV_Cross_Output stage_output;
    stage_output.f_SV_Target0 = f_SV_Target0;
    return stage_output;
}
#elif !defined(OUTLINE_ON) && !defined(UNDERLAY_ON) && !defined(UNITY_UI_ALPHACLIP) && !defined(UNITY_UI_CLIP_RECT)
float4x4 unity_ObjectToWorld;
float4x4 unity_MatrixVP;
float4x4 glstate_matrix_projection;
float4x4 unity_WorldToObject;
float _VertexOffsetX;
float _VertexOffsetY;
float4 _ScreenParams;
float _ScaleX;
float _ScaleY;
float _GradientScale;
float _Sharpness;
float _PerspectiveFilter;
float3 _WorldSpaceCameraPos;
float _WeightNormal;
float _WeightBold;
float _FaceDilate;
float _ScaleRatioA;
float _OutlineSoftness;
float _OutlineWidth;
int _UIVertexColorAlwaysGammaSpace;
float4 _FaceColor;
float4 _OutlineColor;
float4 _ClipRect;
float _MaskSoftnessX;
float _UIMaskSoftnessX;
float _MaskSoftnessY;
float _UIMaskSoftnessY;
Texture2D<float4> _MainTex;
SamplerState sampler_MainTex;
static float4 v_gl_Position;
static float4 v_in_TEXCOORD0;
static float4 v_in_POSITION0;
static float3 v_in_NORMAL0;
static float4 v_in_COLOR0;
static float4 v_vs_COLOR0;
static float4 v_vs_TEXCOORD0;
static float4 v_vs_TEXCOORD1;

struct v_SPIRV_Cross_Input
{
    float4 v_in_POSITION0 : POSITION0;
    float3 v_in_NORMAL0 : NORMAL0;
    float4 v_in_COLOR0 : COLOR0;
    float4 v_in_TEXCOORD0 : TEXCOORD0;
};

struct v_SPIRV_Cross_Output
{
    float4 v_vs_COLOR0 : TEXCOORD0;
    float4 v_vs_TEXCOORD0 : TEXCOORD1;
    float4 v_vs_TEXCOORD1 : TEXCOORD2;
    float4 v_gl_Position : SV_Position;
};

static float v_u_xlat0;
static bool v_u_xlatb0;
static float4 v_u_xlat1;
static float3 v_u_xlat2;
static float v_u_xlat14;
static float4 v_u_xlat3;
static float4 v_u_xlat4;
static float4 v_u_xlat5;
static float2 v_u_xlat12;
static float2 v_u_xlat25;
static float2 v_u_xlat26;
static float v_u_xlat36;
static float v_u_xlat37;
static bool v_u_xlatb25;
static float4 v_u_xlat6;
static float v_u_xlat13;
static bool3 v_u_xlatb13;
static int v_u_xlati25;
static int v_u_xlati13;
static float4 v_u_xlat16_7;
static float4 v_u_xlat16_8;
static float3 v_u_xlat16_9;
static float3 v_u_xlat16_10;
static float4 v_u_xlat16_4;
static float v_u_xlat40;
static float4 v_u_xlat16_5;
static float4 v_u_xlat16_6;
static float v_u_xlat16_40;
static float2 v_u_xlat11;
static float2 v_u_xlat31;
static float4 v_u_xlat7;
static float v_u_xlat18;
static float v_u_xlat30;
static float v_u_xlat38;
static float2 v_u_xlat16_26;
static float2 v_u_xlat35;
static float2 v_u_xlat9;
static float4 v_u_xlat16_1;
static float4 v_vs_COLOR1;
static float4 v_vs_TEXCOORD2;

int v_op_not(int value)
{
    return (-value) - 1;
}

void v_vert_main()
{
    v_u_xlat0 = 0.0f;
    v_u_xlatb0 = v_u_xlat0 >= v_in_TEXCOORD0.w;
    v_u_xlat0 = float(v_u_xlatb0);
    v_u_xlat1 = v_in_POSITION0.zwxy;
    v_u_xlat2.x = v_u_xlat1.z + _VertexOffsetX;
    v_u_xlat14 = v_u_xlat1.w + _VertexOffsetY;
    v_u_xlat2.x = v_u_xlat2.x;
    v_u_xlat2.y = v_u_xlat14;
    v_u_xlat2.z = v_u_xlat1.x;
    v_u_xlat2 = v_u_xlat2;
    v_u_xlat3 = v_u_xlat2.xxxx * transpose(unity_ObjectToWorld)[0];
    v_u_xlat4 = v_u_xlat2.yyyy * transpose(unity_ObjectToWorld)[1];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat2.zzzz * transpose(unity_ObjectToWorld)[2];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = transpose(unity_ObjectToWorld)[3] * 1.0f.xxxx;
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat4 = v_u_xlat3.xxxx * transpose(unity_MatrixVP)[0];
    v_u_xlat5 = v_u_xlat3.yyyy * transpose(unity_MatrixVP)[1];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat5 = v_u_xlat3.zzzz * transpose(unity_MatrixVP)[2];
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat3 = v_u_xlat3.wwww * transpose(unity_MatrixVP)[3];
    v_u_xlat3 += v_u_xlat4;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat12 = v_u_xlat3.ww;
    v_u_xlat25 = _ScreenParams.xx * transpose(glstate_matrix_projection)[0].xy;
    v_u_xlat26 = _ScreenParams.yy * transpose(glstate_matrix_projection)[1].xy;
    v_u_xlat25 += v_u_xlat26;
    v_u_xlat26 = -v_u_xlat25;
    v_u_xlat25 = max(v_u_xlat25, v_u_xlat26);
    v_u_xlat25 *= float2(_ScaleX, _ScaleY);
    v_u_xlat12 /= v_u_xlat25;
    v_u_xlat36 = dot(v_u_xlat12, v_u_xlat12);
    v_u_xlat36 = rsqrt(v_u_xlat36);
    v_u_xlat25.x = -v_in_TEXCOORD0.w;
    v_u_xlat25.x = max(v_u_xlat25.x, v_in_TEXCOORD0.w);
    v_u_xlat25.x *= _GradientScale;
    v_u_xlat37 = 1.0f;
    v_u_xlat37 += _Sharpness;
    v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
    v_u_xlat36 *= v_u_xlat25.x;
    v_u_xlat25.x = 0.0f;
    v_u_xlatb25 = v_u_xlat25.x == transpose(glstate_matrix_projection)[3].w;
    if (v_u_xlatb25)
    {
        v_u_xlat25.x = -v_u_xlat36;
        v_u_xlat25.x = max(v_u_xlat36, v_u_xlat25.x);
        v_u_xlat37 = 1.0f;
        v_u_xlat26.x = -_PerspectiveFilter;
        v_u_xlat37 += v_u_xlat26.x;
        v_u_xlat25.x = v_u_xlat37 * v_u_xlat25.x;
        v_u_xlat4.x = v_in_NORMAL0.x;
        v_u_xlat4.y = v_in_NORMAL0.y;
        v_u_xlat4.z = v_in_NORMAL0.z;
        v_u_xlat5.x = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[0].xyz);
        v_u_xlat5.y = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[1].xyz);
        v_u_xlat5.z = dot(v_u_xlat4.xyz, transpose(unity_WorldToObject)[2].xyz);
        v_u_xlat37 = dot(v_u_xlat5.xyz, v_u_xlat5.xyz);
        v_u_xlat37 = rsqrt(v_u_xlat37);
        float3 _290 = v_u_xlat37.xxx * v_u_xlat5.xyz;
        v_u_xlat4.x = _290.x;
        v_u_xlat4.y = _290.y;
        v_u_xlat4.z = _290.z;
        v_u_xlat2.x = v_u_xlat2.x;
        v_u_xlat2.y = v_u_xlat2.y;
        float4 _303 = v_u_xlat1;
        v_u_xlat1.x = _303.xy.x;
        v_u_xlat1.y = _303.xy.y;
        float3 _314 = v_u_xlat2.xxx * transpose(unity_ObjectToWorld)[0].xyz;
        v_u_xlat5.x = _314.x;
        v_u_xlat5.y = _314.y;
        v_u_xlat5.z = _314.z;
        float3 _327 = v_u_xlat2.yyy * transpose(unity_ObjectToWorld)[1].xyz;
        v_u_xlat6.x = _327.x;
        v_u_xlat6.y = _327.y;
        v_u_xlat6.z = _327.z;
        float4 _334 = v_u_xlat5;
        float3 _338 = _334.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _338.x;
        v_u_xlat5.y = _338.y;
        v_u_xlat5.z = _338.z;
        float3 _350 = v_u_xlat1.xxx * transpose(unity_ObjectToWorld)[2].xyz;
        v_u_xlat6.x = _350.x;
        v_u_xlat6.y = _350.y;
        v_u_xlat6.z = _350.z;
        float4 _357 = v_u_xlat5;
        float3 _361 = _357.xyz + v_u_xlat6.xyz;
        v_u_xlat5.x = _361.x;
        v_u_xlat5.y = _361.y;
        v_u_xlat5.z = _361.z;
        float4 _368 = v_u_xlat1;
        float3 _373 = _368.yyy * transpose(unity_ObjectToWorld)[3].xyz;
        v_u_xlat1.x = _373.x;
        v_u_xlat1.y = _373.y;
        v_u_xlat1.w = _373.z;
        float4 _380 = v_u_xlat1;
        float3 _384 = _380.xyw + v_u_xlat5.xyz;
        v_u_xlat1.x = _384.x;
        v_u_xlat1.y = _384.y;
        v_u_xlat1.w = _384.z;
        float4 _391 = v_u_xlat1;
        v_u_xlat1.x = _391.xyw.x;
        v_u_xlat1.y = _391.xyw.y;
        v_u_xlat1.w = _391.xyw.z;
        float4 _399 = v_u_xlat1;
        float3 _401 = -_399.xyw;
        v_u_xlat1.x = _401.x;
        v_u_xlat1.y = _401.y;
        v_u_xlat1.w = _401.z;
        float4 _408 = v_u_xlat1;
        float3 _413 = _408.xyw + _WorldSpaceCameraPos;
        v_u_xlat1.x = _413.x;
        v_u_xlat1.y = _413.y;
        v_u_xlat1.w = _413.z;
        float4 _420 = v_u_xlat1;
        v_u_xlat1.x = _420.xyw.x;
        v_u_xlat1.y = _420.xyw.y;
        v_u_xlat1.w = _420.xyw.z;
        v_u_xlat26.x = dot(v_u_xlat1.xyw, v_u_xlat1.xyw);
        v_u_xlat26.x = rsqrt(v_u_xlat26.x);
        float4 _438 = v_u_xlat1;
        float3 _442 = _438.xyw * v_u_xlat26.xxx;
        v_u_xlat1.x = _442.x;
        v_u_xlat1.y = _442.y;
        v_u_xlat1.w = _442.z;
        v_u_xlat1.x = dot(v_u_xlat4.xyz, v_u_xlat1.xyw);
        v_u_xlat13 = -v_u_xlat1.x;
        v_u_xlat1.x = max(v_u_xlat13, v_u_xlat1.x);
        v_u_xlat13 = -v_u_xlat25.x;
        v_u_xlat13 = v_u_xlat36 + v_u_xlat13;
        v_u_xlat1.x = v_u_xlat13 * v_u_xlat1.x;
        v_u_xlat36 = v_u_xlat1.x + v_u_xlat25.x;
    }
    v_u_xlat1.x = -_WeightNormal;
    v_u_xlat1.x += _WeightBold;
    v_u_xlat0 *= v_u_xlat1.x;
    v_u_xlat0 += _WeightNormal;
    v_u_xlat0 /= 4.0f;
    v_u_xlat0 += _FaceDilate;
    v_u_xlat0 *= _ScaleRatioA;
    v_u_xlat0 *= 0.5f;
    v_u_xlat1.x = 1.0f;
    v_u_xlat13 = _OutlineSoftness * _ScaleRatioA;
    v_u_xlat13 = v_u_xlat36 * v_u_xlat13;
    v_u_xlat1.x = v_u_xlat13 + v_u_xlat1.x;
    v_u_xlat36 /= v_u_xlat1.x;
    v_u_xlat0 = -v_u_xlat0;
    v_u_xlat0 += 0.5f;
    v_u_xlat0 = v_u_xlat36 * v_u_xlat0;
    v_u_xlat1.x = -0.5f;
    v_u_xlat0 += v_u_xlat1.x;
    v_u_xlat1.x = _OutlineWidth * _ScaleRatioA;
    v_u_xlat1.x *= 0.5f;
    v_u_xlat1.x = v_u_xlat36 * v_u_xlat1.x;
    int4 _563 = _UIVertexColorAlwaysGammaSpace.xxxx;
    v_u_xlatb13.x = any(bool4(int4(0, 0, 0, 0).x != _563.x, int4(0, 0, 0, 0).y != _563.y, int4(0, 0, 0, 0).z != _563.z, int4(0, 0, 0, 0).w != _563.w));
    v_u_xlati25 = 0;
    int param = v_u_xlati25;
    v_u_xlati25 = v_op_not(param);
    v_u_xlati13 = v_u_xlatb13.x ? v_u_xlati25 : 0;
    if (v_u_xlati13 != 0)
    {
        v_u_xlat16_7.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_7.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_7.z = v_in_COLOR0.xyz.z;
        v_u_xlat16_8.x = 0.084971003234386444091796875f;
        v_u_xlat16_8.y = 0.084971003234386444091796875f;
        v_u_xlat16_8.z = 0.084971003234386444091796875f;
        float4 _599 = v_u_xlat16_8;
        float3 _601 = v_u_xlat16_7.xyz * _599.xyz;
        v_u_xlat16_8.x = _601.x;
        v_u_xlat16_8.y = _601.y;
        v_u_xlat16_8.z = _601.z;
        v_u_xlat16_9.x = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.y = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9.z = 0.0001630290062166750431060791015625f;
        v_u_xlat16_9 = -v_u_xlat16_9;
        float4 _615 = v_u_xlat16_8;
        float3 _618 = _615.xyz + v_u_xlat16_9;
        v_u_xlat16_8.x = _618.x;
        v_u_xlat16_8.y = _618.y;
        v_u_xlat16_8.z = _618.z;
        v_u_xlat16_9.x = 0.26588499546051025390625f;
        v_u_xlat16_9.y = 0.26588499546051025390625f;
        v_u_xlat16_9.z = 0.26588499546051025390625f;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.736584007740020751953125f;
        v_u_xlat16_10.y = 0.736584007740020751953125f;
        v_u_xlat16_10.z = 0.736584007740020751953125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.00980184040963649749755859375f;
        v_u_xlat16_10.y = 0.00980184040963649749755859375f;
        v_u_xlat16_10.z = 0.00980184040963649749755859375f;
        v_u_xlat16_10 = -v_u_xlat16_10;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_9 = v_u_xlat16_7.xyz * v_u_xlat16_9;
        v_u_xlat16_10.x = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.y = 0.0031969700939953327178955078125f;
        v_u_xlat16_10.z = 0.0031969700939953327178955078125f;
        v_u_xlat16_9 += v_u_xlat16_10;
        v_u_xlat16_10.x = 0.07254900038242340087890625f;
        v_u_xlat16_10.y = 0.07254900038242340087890625f;
        v_u_xlat16_10.z = 0.07254900038242340087890625f;
        v_u_xlatb13 = bool4(v_u_xlat16_7.xyzz.x < v_u_xlat16_10.xyzz.x, v_u_xlat16_7.xyzz.y < v_u_xlat16_10.xyzz.y, v_u_xlat16_7.xyzz.z < v_u_xlat16_10.xyzz.z, v_u_xlat16_7.xyzz.w < v_u_xlat16_10.xyzz.w).xyz;
        float _679;
        if (v_u_xlatb13.x)
        {
            _679 = v_u_xlat16_8.x;
        }
        else
        {
            _679 = v_u_xlat16_9.x;
        }
        v_u_xlat16_4.x = _679;
        float _691;
        if (v_u_xlatb13.y)
        {
            _691 = v_u_xlat16_8.y;
        }
        else
        {
            _691 = v_u_xlat16_9.y;
        }
        v_u_xlat16_4.y = _691;
        float _703;
        if (v_u_xlatb13.z)
        {
            _703 = v_u_xlat16_8.z;
        }
        else
        {
            _703 = v_u_xlat16_9.z;
        }
        v_u_xlat16_4.z = _703;
        float4 _713 = v_u_xlat16_4;
        v_u_xlat16_4.x = _713.xyz.x;
        v_u_xlat16_4.y = _713.xyz.y;
        v_u_xlat16_4.z = _713.xyz.z;
    }
    else
    {
        v_u_xlat16_4.x = v_in_COLOR0.xyz.x;
        v_u_xlat16_4.y = v_in_COLOR0.xyz.y;
        v_u_xlat16_4.z = v_in_COLOR0.xyz.z;
    }
    v_u_xlat40 = v_in_COLOR0.w;
    v_u_xlat16_4.w = v_u_xlat40;
    v_u_xlat16_5 = v_u_xlat16_4 * _FaceColor;
    float3 _745 = v_u_xlat16_5.www * v_u_xlat16_5.xyz;
    v_u_xlat16_6.x = _745.x;
    v_u_xlat16_6.y = _745.y;
    v_u_xlat16_6.z = _745.z;
    v_u_xlat16_7 = _OutlineColor;
    v_u_xlat26.x = v_u_xlat16_7.w;
    v_u_xlat40 = v_u_xlat16_4.w * v_u_xlat26.x;
    v_u_xlat16_40 = v_u_xlat40;
    float3 _768 = v_u_xlat16_40.xxx * v_u_xlat16_7.xyz;
    v_u_xlat16_4.x = _768.x;
    v_u_xlat16_4.y = _768.y;
    v_u_xlat16_4.z = _768.z;
    v_u_xlat6.x = v_u_xlat16_6.xyz.x;
    v_u_xlat6.y = v_u_xlat16_6.xyz.y;
    v_u_xlat6.z = v_u_xlat16_6.xyz.z;
    v_u_xlat6.w = v_u_xlat16_5.w;
    v_u_xlat4.x = v_u_xlat16_4.xyz.x;
    v_u_xlat4.y = v_u_xlat16_4.xyz.y;
    v_u_xlat4.z = v_u_xlat16_4.xyz.z;
    v_u_xlat4.w = v_u_xlat16_40;
    v_u_xlat26.x = 2.0f;
    v_u_xlat26.x = v_u_xlat1.x * v_u_xlat26.x;
    v_u_xlat26.x = min(v_u_xlat26.x, 1.0f);
    v_u_xlat26.x = sqrt(v_u_xlat26.x);
    v_u_xlat5 = -v_u_xlat6;
    v_u_xlat4 += v_u_xlat5;
    v_u_xlat4 = v_u_xlat26.xxxx * v_u_xlat4;
    v_u_xlat4 += v_u_xlat6;
    v_u_xlat16_4 = v_u_xlat4;
    v_u_xlat5 = max(_ClipRect, (-20000000000.0f).xxxx);
    v_u_xlat5 = min(v_u_xlat5, 20000000000.0f.xxxx);
    v_u_xlat26 = -v_u_xlat5.xy;
    v_u_xlat26 += v_u_xlat2.xy;
    v_u_xlat11 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat5.zw + v_u_xlat11;
    v_u_xlat31 = v_u_xlat26 / v_u_xlat11;
    v_u_xlat3 = v_u_xlat3;
    v_u_xlat16_8.x = v_u_xlat6.xyz.x;
    v_u_xlat16_8.y = v_u_xlat6.xyz.y;
    v_u_xlat16_8.z = v_u_xlat6.xyz.z;
    v_u_xlat16_8.w = v_u_xlat6.w;
    v_u_xlat16_4 = v_u_xlat16_4;
    v_u_xlat7.x = v_in_TEXCOORD0.xy.x;
    v_u_xlat7.y = v_in_TEXCOORD0.xy.y;
    v_u_xlat7.z = v_u_xlat31.x;
    v_u_xlat7.w = v_u_xlat31.y;
    v_u_xlat26.x = -v_u_xlat1.x;
    v_u_xlat18 = v_u_xlat0 + v_u_xlat26.x;
    v_u_xlat30 = v_u_xlat0 + v_u_xlat1.x;
    v_u_xlat16_6.x = v_u_xlat36;
    v_u_xlat16_6.y = v_u_xlat18;
    v_u_xlat16_6.z = v_u_xlat30;
    v_u_xlat16_6.w = v_u_xlat0;
    v_u_xlat26.x = max(_MaskSoftnessX, _UIMaskSoftnessX);
    v_u_xlat38 = max(_MaskSoftnessY, _UIMaskSoftnessY);
    v_u_xlat16_26.x = v_u_xlat26.x;
    v_u_xlat16_26.y = v_u_xlat38;
    v_u_xlat11 = 2.0f.xx;
    v_u_xlat11 = v_u_xlat2.xy * v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.xy;
    v_u_xlat11 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat35 = -v_u_xlat5.zw;
    float2 _935 = v_u_xlat35 + v_u_xlat11;
    v_u_xlat1.x = _935.x;
    v_u_xlat1.y = _935.y;
    v_u_xlat16_9.x = 0.25f;
    v_u_xlat16_9.y = 0.25f;
    float3 _944 = v_u_xlat16_9;
    float2 _946 = v_u_xlat16_26 * _944.xy;
    v_u_xlat16_9.x = _946.x;
    v_u_xlat16_9.y = _946.y;
    v_u_xlat9 = v_u_xlat16_9.xy;
    v_u_xlat11 = v_u_xlat12 + v_u_xlat9;
    v_u_xlat25 = 0.25f.xx / v_u_xlat11;
    v_u_xlat16_1.x = v_u_xlat1.xy.x;
    v_u_xlat16_1.y = v_u_xlat1.xy.y;
    v_u_xlat16_1.z = v_u_xlat25.x;
    v_u_xlat16_1.w = v_u_xlat25.y;
    v_gl_Position = v_u_xlat3;
    v_vs_COLOR0 = v_u_xlat16_8;
    v_vs_COLOR1 = v_u_xlat16_4;
    v_vs_TEXCOORD0 = v_u_xlat7;
    v_vs_TEXCOORD1 = v_u_xlat16_6;
    v_vs_TEXCOORD2 = v_u_xlat16_1;
}

v_SPIRV_Cross_Output RecoveryVert(v_SPIRV_Cross_Input stage_input)
{
    v_in_TEXCOORD0 = stage_input.v_in_TEXCOORD0;
    v_in_POSITION0 = stage_input.v_in_POSITION0;
    v_in_NORMAL0 = stage_input.v_in_NORMAL0;
    v_in_COLOR0 = stage_input.v_in_COLOR0;
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
static float f_u_xlat2;
static float f_u_xlat0;
static float f_u_xlat16_1;

void f_frag_main()
{
    f_u_xlat16_0.x = _MainTex.Sample(sampler_MainTex, f_vs_TEXCOORD0.xy).w;
    f_u_xlat2 = f_vs_TEXCOORD1.x;
    f_u_xlat0 = f_u_xlat2 * f_u_xlat16_0.x;
    f_u_xlat16_0.x = f_u_xlat0;
    f_u_xlat16_1 = -f_vs_TEXCOORD1.w;
    f_u_xlat16_1 = f_u_xlat16_0.x + f_u_xlat16_1;
    f_u_xlat16_1 = max(f_u_xlat16_1, 0.0f);
    f_u_xlat16_1 = min(f_u_xlat16_1, 1.0f);
    f_u_xlat16_0 = f_u_xlat16_1.xxxx * f_vs_COLOR0;
    f_SV_Target0 = f_u_xlat16_0;
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
#else
#error This keyword combination was not present in the shipped APK shader.
#endif
ENDHLSL
}
}
}
