// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Water v3"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "white" {}
		_WaterColor("Water Color", Color) = (1,1,1,0)
		_WaterSpeed("Water Speed", Vector) = (0,0,0,0)
		_RSpecularGGloss("R(Specular), G(Gloss)", Color) = (0,0,0,0)
		_FoamColors("Foam Colors", 2D) = "white" {}
		_FoamTint("Foam Tint", Color) = (0,0,0,0)
		_Mask("Mask (UV2)", 2D) = "white" {}
		_FoamSpeed("Foam Speed", Vector) = (0,0,0,0)
		_Foam1("Foam 1", 2D) = "white" {}
		_TileFoam1("Tile Foam 1", Vector) = (1,1,0,0)
		_Foam2("Foam 2", 2D) = "white" {}
		_TileFoam2("Tile Foam 2", Vector) = (1,1,0,0)
		_EmissionColor("Emission", Color) = (0,0,0,0)
		_EmisisonIntensity("Emisison Intensity", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma exclude_renderers xbox360 xboxone ps4 psp2 n3ds wiiu 
		#pragma surface surf Lambert keepalpha exclude_path:deferred nodynlightmap 
		struct Input
		{
			half2 uv2_texcoord2;
			half2 uv_texcoord;
		};

		uniform half4 _FoamTint;
		uniform sampler2D _FoamColors;
		uniform sampler2D _Mask;
		uniform half4 _Mask_ST;
		uniform sampler2D _Foam1;
		uniform half2 _TileFoam1;
		uniform half2 _FoamSpeed;
		uniform sampler2D _Foam2;
		uniform half2 _TileFoam2;
		uniform half2 _WaterSpeed;
		uniform half4 _WaterColor;
		uniform sampler2D _MainTex;
		uniform half4 _EmissionColor;
		uniform half _EmisisonIntensity;
		uniform half4 _RSpecularGGloss;

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv2_Mask = i.uv2_texcoord2 * _Mask_ST.xy + _Mask_ST.zw;
			float2 panner48 = ( 1.0 * _Time.y * _FoamSpeed + i.uv_texcoord);
			float2 temp_output_52_0 = ( _TileFoam1 * panner48 );
			float2 panner43 = ( 1.0 * _Time.y * _WaterSpeed + i.uv_texcoord);
			o.Albedo = ( ( _FoamTint * tex2D( _FoamColors, ( tex2D( _Mask, uv2_Mask ) * ( tex2D( _Foam1, temp_output_52_0 ).g + tex2D( _Foam2, ( _TileFoam2 * panner43 ) ).r ) ).rg ) ) + ( _WaterColor * tex2D( _MainTex, temp_output_52_0 ) ) ).rgb;
			o.Emission = ( _EmissionColor * _EmisisonIntensity ).rgb;
			o.Specular = _RSpecularGGloss.r;
			o.Gloss = _RSpecularGGloss.g;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Mobile/Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=16700
815.3334;190;1241;949;1509.719;1423.922;2.091614;True;True
Node;AmplifyShaderEditor.TextureCoordinatesNode;4;-1865.652,-572.5695;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;54;-1545.243,-726.9449;Half;False;Property;_FoamSpeed;Foam Speed;7;0;Create;True;0;0;False;0;0,0;0.01,0.2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;55;-1541.855,-340.0891;Half;False;Property;_WaterSpeed;Water Speed;2;0;Create;True;0;0;False;0;0,0;-0.05,0.15;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PannerNode;43;-1203.674,-378.3274;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;48;-1208.378,-750.467;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;66;-1533.386,-484.1319;Half;False;Property;_TileFoam2;Tile Foam 2;11;0;Create;True;0;0;False;0;1,1;4,0.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;51;-1535.388,-861.8098;Half;False;Property;_TileFoam1;Tile Foam 1;9;0;Create;True;0;0;False;0;1,1;4,0.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;67;-963.9323,-466.9471;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;52;-954.257,-839.9739;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;61;-730.3976,-542.3585;Float;True;Property;_Foam1;Foam 1;8;0;Create;False;0;0;False;0;924c8b3c62967454da7204c050c650e5;924c8b3c62967454da7204c050c650e5;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;62;-730.0219,-336.9706;Float;True;Property;_Foam2;Foam 2;10;0;Create;False;0;0;False;0;588602dce7e1ba349a91e8743fd5ba06;588602dce7e1ba349a91e8743fd5ba06;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;63;-376.8743,-395.2681;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;39;-363.3047,-813.4491;Float;True;Property;_Mask;Mask (UV2);6;0;Create;False;0;0;False;0;9842c2c875cc46641a16575e2048c87d;9842c2c875cc46641a16575e2048c87d;True;1;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;41;-7.241516,-607.5286;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;65;179.043,-770.6882;Float;True;Property;_FoamColors;Foam Colors;4;0;Create;True;0;0;False;0;96cf493088390c642a06017205c966e2;96cf493088390c642a06017205c966e2;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;1;177.1299,-378.3607;Float;True;Property;_MainTex;Main Texture;0;0;Create;False;0;0;False;0;3120cb4becf67854d8f913458584430a;3120cb4becf67854d8f913458584430a;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;71;265.8364,-563.35;Half;False;Property;_WaterColor;Water Color;1;0;Create;True;0;0;False;0;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;72;273.6749,-948.8818;Half;False;Property;_FoamTint;Foam Tint;5;0;Create;True;0;0;False;0;0,0,0,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;74;266.0607,209.6335;Half;False;Property;_EmisisonIntensity;Emisison Intensity;13;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;73;609.6888,-851.0519;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;69;535.7576,-476.3952;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;68;275.3395,31.20056;Half;False;Property;_EmissionColor;Emission;12;0;Create;False;0;0;False;0;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;70;562.7265,66.80637;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;12;259.1689,-162.6353;Half;False;Property;_RSpecularGGloss;R(Specular), G(Gloss);3;0;Create;True;0;0;False;0;0,0,0,0;0.254717,0.254717,0.254717,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;37;724.8207,-492.4414;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1282.239,-301.9455;Half;False;True;2;Half;ASEMaterialInspector;0;0;Lambert;Water v3;False;False;False;False;False;False;False;True;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;False;0;False;Opaque;;Geometry;ForwardOnly;True;True;True;True;True;True;True;False;False;False;False;False;False;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;Mobile/Diffuse;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;43;0;4;0
WireConnection;43;2;55;0
WireConnection;48;0;4;0
WireConnection;48;2;54;0
WireConnection;67;0;66;0
WireConnection;67;1;43;0
WireConnection;52;0;51;0
WireConnection;52;1;48;0
WireConnection;61;1;52;0
WireConnection;62;1;67;0
WireConnection;63;0;61;2
WireConnection;63;1;62;1
WireConnection;41;0;39;0
WireConnection;41;1;63;0
WireConnection;65;1;41;0
WireConnection;1;1;52;0
WireConnection;73;0;72;0
WireConnection;73;1;65;0
WireConnection;69;0;71;0
WireConnection;69;1;1;0
WireConnection;70;0;68;0
WireConnection;70;1;74;0
WireConnection;37;0;73;0
WireConnection;37;1;69;0
WireConnection;0;0;37;0
WireConnection;0;2;70;0
WireConnection;0;3;12;1
WireConnection;0;4;12;2
ASEEND*/
//CHKSM=F27878E5C894AB4DCF5C13D1E15CE22289004402