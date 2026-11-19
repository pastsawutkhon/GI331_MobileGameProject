Shader "UI/Scene5 Mannequin Chest"
{
    Properties {
        [PerRendererData] _MainTex ("Room", 2D) = "white" {}
        _DoorOpen ("Door swing", Range(0,1)) = 0
        _Chest ("Organ artwork", 2D) = "white" {}
        _Released ("Unrestrained pose", 2D) = "white" {}
        _KeyTex ("Key", 2D) = "white" {}
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
            struct Vertex { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Varyings { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            Varyings vert(Vertex v) {
                Varyings o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o;
            }
            sampler2D _MainTex, _Chest, _Released, _KeyTex;
            float4 _Organ0, _Organ1, _Organ2, _Move0, _Move1, _Move2, _KeyRect;
            float _DoorOpen, _Free, _Kick, _CloseUp, _KeyVisibility;
            float contour(float2 uv, float4 organ) {
                float2 p=(uv-organ.xy)/organ.zw;
                return 1-smoothstep(.80,1,dot(p,p));
            }
            void drawPlaced(inout fixed3 rgb, float2 uv, float4 organ, float4 move) {
                if(move.z<.5) return;
                float2 src=uv-move.xy;
                float mask=contour(src,organ);
                if(mask<=0) return;
                rgb=lerp(rgb,tex2D(_Chest,src).rgb*1.18,mask);
            }
            fixed4 frag(Varyings i):SV_Target {
                float2 uv=(i.uv-.5)*(1-.038*abs(_Kick))+.5+float2(.008,-.011)*_Kick;
                fixed4 col=tex2D(_MainTex,uv);
                if(_Free>.5) {
                    float armRegion=_CloseUp>.5 ? smoothstep(.565,.685,uv.x) : 1;
                    col.rgb=lerp(col.rgb,tex2D(_Released,uv).rgb,armRegion);
                }
                // Swing the door leaf about its left jamb, keeping the surrounding frame.
                if (_CloseUp < .5 && _DoorOpen > 0) {
                    float4 bounds = float4(.700,.500,.153,.225);
                    float2 door = (uv-bounds.xy)/bounds.zw;
                    if (all(door>=0) && all(door<=1)) {
                        float width=lerp(1,.16,_DoorOpen);
                        float2 leaf=float2(door.x/width,door.y);
                        leaf.y=(leaf.y-.5)/(1-.12*_DoorOpen*saturate(leaf.x))+.5;
                        col.rgb=fixed3(.016,.012,.009);
                        if(all(leaf>=0) && all(leaf<=1)) {
                            float2 face=bounds.xy+leaf*bounds.zw;
                            col.rgb=lerp(tex2D(_MainTex,face).rgb,tex2D(_Released,face).rgb,_Free)*(1-.36*_DoorOpen);
                        }
                    }
                }
                // Invert the bed's projected axes in the wide room artwork.
                float2 p=uv;
                if(_CloseUp<.5) {
                    float2 d=uv-float2(.33,.43);
                    p=float2(.15*d.x-.15*d.y,.016*d.x+.24*d.y)/.0384;
                    if(any(p<0)||any(p>1)) return col*i.color;
                }
                float3 mask=float3(contour(p,_Organ0),contour(p,_Organ1),contour(p,_Organ2));
                float3 moved=float3(_Move0.z,_Move1.z,_Move2.z);
                float3 vacant=mask*moved;
                col.rgb*=1-.90*max(vacant.x,max(vacant.y,vacant.z));
                if(_KeyVisibility>.001) {
                    float2 keyUV=(p-_KeyRect.xy)/_KeyRect.zw;
                    if(all(keyUV>=0)&&all(keyUV<=1)) {
                        fixed4 key=tex2D(_KeyTex,keyUV);
                        float3 cover=mask*(1-moved);
                        float visible=1-max(cover.x,max(cover.y,cover.z));
                        col.rgb=lerp(col.rgb,key.rgb,key.a*visible*_KeyVisibility);
                    }
                }
                drawPlaced(col.rgb,p,_Organ0,_Move0);
                drawPlaced(col.rgb,p,_Organ1,_Move1);
                drawPlaced(col.rgb,p,_Organ2,_Move2);
                return col*i.color;
            }
            ENDCG
        }
    }
}
