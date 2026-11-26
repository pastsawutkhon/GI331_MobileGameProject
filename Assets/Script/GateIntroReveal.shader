Shader "UI/Gate Intro Reveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Progress;
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(appdata_base input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord.xy;
                return output;
            }
            fixed4 frag(Varyings input) : SV_Target
            {
                float distanceFromCenter = length(input.uv * 2.0 - 1.0);
                float radius = lerp(1.42, -0.65, _Progress);
                float opacity = 1.0 - smoothstep(radius, radius + 0.65, distanceFromCenter);
                return fixed4(0, 0, 0, opacity);
            }
            ENDCG
        }
    }
}
