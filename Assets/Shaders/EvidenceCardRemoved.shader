Shader "UI/Evidence Card Removed"
{
    Properties {
        [PerRendererData] _MainTex ("Evidence room", 2D) = "white" {}
        _CardRemoved ("Card removed", Range(0,1)) = 0
        _NightVision ("Night vision", Range(0,1)) = 0
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Attributes { float4 position:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Varyings { float4 position:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;
            float _NightVision, _CardRemoved;
            Varyings vert(Attributes v) {
                Varyings o; o.position=UnityObjectToClipPos(v.position); o.uv=v.uv; o.color=v.color; return o;
            }
            fixed3 paperAt(float2 pixel) {
                return tex2D(_MainTex,float2(pixel.x/572.0,1-pixel.y/1024.0)).rgb;
            }
            fixed4 frag(Varyings i):SV_Target {
                fixed4 col=tex2D(_MainTex,i.uv);
                float2 p=float2(i.uv.x*572,(1-i.uv.y)*1024);
                // Reconstruct the small card footprint using the lighting on both sides
                // of the same sheet. Retain fine fibres from the paper beside the card.
                float2 edge=min(p-float2(464,505),float2(502,553)-p);
                float mixAmount=_CardRemoved*smoothstep(0,5,min(edge.x,edge.y));
                if(mixAmount>0) {
                    fixed3 left=paperAt(float2(465,p.y)), right=paperAt(float2(501,p.y));
                    float2 samplePos=float2(460+4*sin(p.x*.21),p.y);
                    fixed3 detail=paperAt(samplePos)-paperAt(samplePos+float2(1.8,1.4));
                    fixed3 paper=lerp(left,right,saturate((p.x-465)/36))+.45*detail;
                    col.rgb=lerp(col.rgb,paper,mixAmount);
                }
                float luminance = dot(col.rgb, float3(.299, .587, .114));
                fixed3 night = saturate((luminance * 2.35 + .035) * float3(.30, 1, .36));
                col.rgb = lerp(col.rgb, night, _NightVision);
                return col*i.color;
            }
            ENDCG
        }
    }
}
