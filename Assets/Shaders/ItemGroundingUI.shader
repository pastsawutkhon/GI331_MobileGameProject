Shader "UI/Item Grounding"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Grass ("Foreground Grass", Float) = 0
        _ItemSurface ("Shaded Item", Float) = 0
        _TopShade ("Top Brightness", Range(0,1)) = 0.88
        _BottomShade ("Bottom Brightness", Range(0,1)) = 0.45
        _SpriteYBounds ("Sprite UV Y Bounds", Vector) = (0,1,0,0)
        _Region ("Grass Centre and Radius", Vector) = (0.19,0.27,0.12,0.07)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize, _Region, _ClipRect;
            fixed4 _Color, _TextureSampleAdd;
            float _Grass, _ItemSurface, _TopShade, _BottomShade;
            float4 _SpriteYBounds;
            v2f vert(appdata v)
            {
                v2f o; o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 source=tex2D(_MainTex,i.uv)+_TextureSampleAdd;
                fixed4 result=source*i.color;
                if (_Grass > 0.5)
                {
                    float2 p=(i.uv-_Region.xy)/_Region.zw;
                    float area=1.0-smoothstep(0.35,1.0,dot(p,p));
                    float lower=1.0-smoothstep(_Region.y-0.005,_Region.y+0.025,i.uv.y);
                    float light=dot(source.rgb,float3(0.2126,0.7152,0.0722));
                    // Reuse actual blades from the scene; dark gaps leave the tool visible.
                    float blades=smoothstep(0.0015,0.009,light);
                    // Dense grass occludes even in shadow; brighter blades extend above it.
                    float dense=1.0-smoothstep(_Region.y-0.024,_Region.y+0.012,i.uv.y);
                    result.a*=area*saturate(lower*blades+dense);
                }
                else if (_ItemSurface > 0.5)
                {
                    float height=saturate((i.uv.y-_SpriteYBounds.x)
                        /max(0.0001,_SpriteYBounds.y-_SpriteYBounds.x));
                    result.rgb*=lerp(_BottomShade,_TopShade,smoothstep(0.0,1.0,height));
                }
                else
                {
                    float2 d=_MainTex_TexelSize.xy*26.0;
                    float a=source.a*2.0;
                    a+=tex2D(_MainTex,i.uv+float2(d.x,0)).a;
                    a+=tex2D(_MainTex,i.uv-float2(d.x,0)).a;
                    a+=tex2D(_MainTex,i.uv+float2(0,d.y)).a;
                    a+=tex2D(_MainTex,i.uv-float2(0,d.y)).a;
                    a+=tex2D(_MainTex,i.uv+d).a+tex2D(_MainTex,i.uv-d).a;
                    a+=tex2D(_MainTex,i.uv+float2(d.x,-d.y)).a;
                    a+=tex2D(_MainTex,i.uv+float2(-d.x,d.y)).a;
                    result=fixed4(i.color.rgb,a*0.1*i.color.a);
                }
                #ifdef UNITY_UI_CLIP_RECT
                result.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a-0.001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
