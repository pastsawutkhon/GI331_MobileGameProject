Shader "UI/Desk Lamp Layered Metal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Lamp Sprite", 2D) = "white" {}
        _SpriteRect ("Sprite UV Bounds", Vector) = (0,0,1,1)
        _Layer ("Rear / Arm Base / Front Rim / Light", Float) = 0
        _DstBlend ("Destination Blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha [_DstBlend]
        ColorMask RGB
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "DeskLampLighting.cginc"
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            sampler2D _MainTex;
            float4 _SpriteRect;
            float _Layer;
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float4 source=tex2D(_MainTex,i.uv);
                float2 p=(i.uv-_SpriteRect.xy)/max(_SpriteRect.zw,float2(0.0001,0.0001));
                float head=step(0.49,p.y)*(1.0-step(0.47,p.x));
                float front=LampFrontAssembly(p);
                float4 result=source*i.color;
                if (_Layer < 0.5) result.a*=head*(1.0-front);
                else if (_Layer < 1.5) result.a*=(1.0-head)*(1.0-front);
                else if (_Layer < 2.5) result.a*=front;
                else
                {
                    float2 reflector=(p-float2(0.25,0.665))/float2(0.23,0.13);
                    float2 foot=(p-float2(0.47,0.10))/float2(0.32,0.12);
                    float reflected=exp(-dot(reflector,reflector)*2.0);
                    float baseLight=exp(-dot(foot,foot)*1.7);
                    // Albedo retains rust and metal texture instead of a flat glow over the base.
                    result.rgb*=1.35*reflected+0.85*baseLight;
                    result.a*=LampLightVisibility(p);
                }
                return result;
            }
            ENDCG
        }
    }
}
