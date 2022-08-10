// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

// - No lighting
// - Ideal texel precision

Shader "Terrain Blended (Splat 4 + ToD) v2" {
	Properties {
		_Control0 ("Control (RGBA)", 2D) = "black" {}
		_Splat3 ("Ch (A)", 2D) = "white" {}
		_Splat2 ("Ch (B)", 2D) = "white" {}
		_Splat1 ("Ch (G)", 2D) = "white" {}
		_Splat0 ("Ch (R)", 2D) = "white" {}
	}
	
    SubShader {

		Tags { "SplatCount" = "4" "RenderType"="Opaque" "IgnoreProjector" = "False" "Queue"="Geometry-100" }

        Pass {
			 CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
            #pragma multi_compile _ BATCHED_LIGHTMAP

			#include "UnityCG.cginc"

			sampler2D _Control0;
			fixed4 _Control0_ST;
			sampler2D _Splat0,_Splat1,_Splat2,_Splat3;
			fixed4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;
            // TF_BEGIN
            UNITY_DECLARE_TEX2DARRAY(BatchedLightmap);
            float4 BatchedLightmap_ST;
            // TF_END

   			uniform fixed4 _TimeOfDayTerrainTint;

           	struct vertInput {
                fixed4 vertex	: POSITION;
                fixed2 texcoord	: TEXCOORD0;
#ifdef LIGHTMAP_ON
			  	fixed2 texcoord1 : TEXCOORD1;
#elif BATCHED_LIGHTMAP
                fixed3 texcoord1 : TEXCOORD1;
#endif
            };

			struct v2f {
				fixed4 pos : SV_POSITION;

                fixed2 uv_ctrl : TEXCOORD0;

				fixed4 uv_splat01 : TEXCOORD1;
  				fixed4 uv_splat23 : TEXCOORD2;

#ifdef LIGHTMAP_ON
				fixed2 lmap : TEXCOORD7;
#elif BATCHED_LIGHTMAP
                fixed3 lmap : TEXCOORD7;
#endif
				UNITY_FOG_COORDS(8)
			};

			v2f vert ( vertInput v ) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv_ctrl = TRANSFORM_TEX( v.texcoord, _Control0 );

				// stuff the uv for each channel into a variable
				o.uv_splat01.xy = TRANSFORM_TEX( v.texcoord, _Splat0 );
 				o.uv_splat01.zw = TRANSFORM_TEX( v.texcoord, _Splat1 );
				o.uv_splat23.xy = TRANSFORM_TEX( v.texcoord, _Splat2 );
 				o.uv_splat23.zw = TRANSFORM_TEX( v.texcoord, _Splat3 );

				UNITY_TRANSFER_FOG(o, o.pos);
#ifdef LIGHTMAP_ON
				o.lmap = ( v.texcoord1.xy * unity_LightmapST.xy ) + unity_LightmapST.zw;
#elif BATCHED_LIGHTMAP
                o.lmap = v.texcoord1;
#endif
				return o;
			}

			fixed4 frag ( v2f i ) : COLOR {
				// splat map 0
				fixed4 ctrl = tex2D( _Control0, i.uv_ctrl );
				fixed4 texR = tex2D( _Splat0, i.uv_splat01.xy );
				fixed4 texG = tex2D( _Splat1, i.uv_splat01.zw );
				fixed4 texB = tex2D( _Splat2, i.uv_splat23.xy );
				fixed4 texA = tex2D( _Splat3, i.uv_splat23.zw );
				fixed4 col = ((texR * ctrl.r) + (texG * ctrl.g) + (texB * ctrl.b)+ (texA * ctrl.a));

 #ifdef LIGHTMAP_ON
				col.rgb *= DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmap));
#elif BATCHED_LIGHTMAP
                col.rgb *= DecodeLightmap(UNITY_SAMPLE_TEX2DARRAY(BatchedLightmap, i.lmap));
#endif
				col.rgb *= _TimeOfDayTerrainTint.rgb;
				UNITY_APPLY_FOG(i.fogCoord, col);
				col.a = 1.0;
				return col;
			}

			ENDCG
		}
	}
    FallBack "Mobile/Diffuse"

}


