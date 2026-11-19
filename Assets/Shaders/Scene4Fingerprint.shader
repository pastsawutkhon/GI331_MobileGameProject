Shader "UI/Scene4 Fingerprint Ink"
{
    Properties { [PerRendererData] _MainTex ("Fingerprint", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Vertex { float4 position : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            Varyings vert(Vertex v)
            {
                Varyings o;
                o.position = UnityObjectToClipPos(v.position);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(Varyings i) : SV_Target
            {
                return fixed4(i.color.rgb, i.color.a * tex2D(_MainTex, i.uv).a);
            }
            ENDCG
        }
    }
}
