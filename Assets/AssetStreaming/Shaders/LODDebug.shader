Shader "LOD Debug" {
    Properties{
        _MainTex("Main Texture (RGBA)", 2D) = "white" {}
        _LODState("LOD State", Float) = 0.0
    }

        SubShader{
            Tags {  "RenderType" = "Opaque" "Queue" = "Geometry" }
            LOD 100

            Pass {
                Cull Back
                Zwrite On
                Lighting Off

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #pragma multi_compile_fwdbase
                #pragma multi_compile_fog
                #pragma multi_compile _ BATCHED_LIGHTMAP

                #include "UnityCG.cginc"	// --> /Applications/Unity/Unity.app/Contents/CGIncludes/UnityCG.cginc

                sampler2D _MainTex;
                float4 _MainTex_ST;
                uniform fixed4 _TimeOfDayWorldTint;
                // TF_BEGIN
                UNITY_DECLARE_TEX2DARRAY(BatchedLightmap);
                float4 BatchedLightmap_ST;
                float _LODState;
                // TF_END

                struct vertInput {
                    float4 vertex	: POSITION;
                    float2 texcoord	: TEXCOORD0;
    #ifdef LIGHTMAP_ON
                    float2 texcoord1: TEXCOORD1;
    #elif BATCHED_LIGHTMAP
                    float3 texcoord1: TEXCOORD1;
    #endif
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    half2 uv : TEXCOORD0;
    #ifdef LIGHTMAP_ON
                    half2 lmap : TEXCOORD1;
    #elif BATCHED_LIGHTMAP
                    half3 lmap : TEXCOORD7;
    #endif		
                    UNITY_FOG_COORDS(2)
                };

                v2f vert(vertInput v) {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                    UNITY_TRANSFER_FOG(o, o.pos);
    #ifdef LIGHTMAP_ON
                    o.lmap = (v.texcoord1.xy * unity_LightmapST.xy) + unity_LightmapST.zw;
    #elif BATCHED_LIGHTMAP
                    o.lmap = v.texcoord1;
    #endif		
                    return o;
                }

                inline fixed4 DecodeLightmapRGBA(fixed4 color) {
                    return 2.0 * color;
                }

            fixed4 frag(v2f i) : COLOR {
                fixed4 col = fixed4(0.5, 1, 0.5, 1); // LOD0
                if(_LODState == 1.0f) // LOD1
                    col = fixed4(0.5, 0.5, 1, 1);
                else if (_LODState == 2.0f) // LOD2
                    col = fixed4(1, 0.5, 0.5, 1);
                else if (_LODState == 3.0f) // Loading
                    col = fixed4(1, 1, 1, 1);

#ifdef LIGHTMAP_ON
                col.rgb *= DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmap));
#elif BATCHED_LIGHTMAP
                col.rgb *= DecodeLightmap(UNITY_SAMPLE_TEX2DARRAY(BatchedLightmap, i.lmap));
#endif
                return col;
            }
            ENDCG
        }
    }
}


