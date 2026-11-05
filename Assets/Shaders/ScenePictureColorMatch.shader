Shader "UI/Scene Picture Color Match"
{
    Properties
    {
        [PerRendererData] _MainTex ("Picture", 2D) = "white" {}
        _Gain ("RGB Gain", Vector) = (1,1,1,1)
        _Offset ("RGB Offset", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;
            float4 _Gain, _Offset;
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float4 color=tex2D(_MainTex,i.uv);
                #ifndef UNITY_COLORSPACE_GAMMA
                color.rgb=LinearToGammaSpace(color.rgb);
                #endif
                color.rgb=saturate(color.rgb*_Gain.rgb+_Offset.rgb);
                #ifndef UNITY_COLORSPACE_GAMMA
                color.rgb=GammaToLinearSpace(color.rgb);
                #endif
                return color*i.color;
            }
            ENDCG
        }
    }
}
