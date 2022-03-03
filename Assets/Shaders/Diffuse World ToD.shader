Shader "Diffuse World (ToD)" {
	Properties {
		_MainTex ( "Main Texture (RGBA)", 2D ) = "white" {}
		//_Normal ("Normal Map", 2D) = "bump" {}
		//_SpecGlossMap("Specular", 2D) = "white" {}
	}
	
	SubShader {
		Tags {  "RenderType"="Opaque" "Queue"="Geometry" }
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

			v2f vert ( vertInput v ) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				UNITY_TRANSFER_FOG(o, o.pos);
#ifdef LIGHTMAP_ON
				o.lmap = ( v.texcoord1.xy * unity_LightmapST.xy ) + unity_LightmapST.zw;
#elif BATCHED_LIGHTMAP
                o.lmap = v.texcoord1;
#endif		
				return o;
			}

			inline fixed4 DecodeLightmapRGBA( fixed4 color ) {
				//color.r = max(0.1, color.r);
				//color.g = max(0.1, color.g);
				//color.b = max(0.1, color.b);
 				return 2.0 * color;
			}	

			fixed4 frag ( v2f i ) : COLOR {
				fixed4 col = tex2D( _MainTex, i.uv );
#ifdef LIGHTMAP_ON
			  	//col *= DecodeLightmapRGBA( UNITY_SAMPLE_TEX2D( unity_Lightmap, i.lmap ) );
				col.rgb *= DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lmap));
#elif BATCHED_LIGHTMAP
                col.rgb *= DecodeLightmap(UNITY_SAMPLE_TEX2DARRAY(BatchedLightmap, i.lmap));
#endif			
				col *= _TimeOfDayWorldTint;
				UNITY_APPLY_FOG( i.fogCoord, col );
				return col;
			}
			ENDCG 
		}	
	}
}


