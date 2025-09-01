// Made with Amplify Shader Editor v1.9.2.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Cause + Christi / Light Beam Atmospheric"
{
	Properties
	{
		_ColorsBars("Colors Bars", 2D) = "white" {}
		_AlphaGradientBlackontop("Alpha Gradient (Black on top)", 2D) = "white" {}
		_AtmosphereTexture("Atmosphere Texture", 2D) = "white" {}
		_ColorBarTimeScale("Color Bar Time Scale", Float) = -0.25
		_AtmosphereTimeScale("Atmosphere Time Scale", Vector) = (0,0,0,0)
		_Opacity("Opacity", Range( 0 , 1)) = 0
		_AtmosphereHaze("Atmosphere Haze", Range( 0 , 2)) = 1
		_ColorTint("Color Tint", Color) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float4 _ColorTint;
		uniform sampler2D _ColorsBars;
		uniform float _ColorBarTimeScale;
		uniform float _Opacity;
		uniform sampler2D _AlphaGradientBlackontop;
		uniform float4 _AlphaGradientBlackontop_ST;
		uniform sampler2D _AtmosphereTexture;
		uniform float2 _AtmosphereTimeScale;
		uniform float _AtmosphereHaze;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float mulTime20 = _Time.y * _ColorBarTimeScale;
			float4 appendResult24 = (float4(( i.uv_texcoord.x + mulTime20 ) , i.uv_texcoord.y , 0.0 , 0.0));
			o.Emission = ( _ColorTint + tex2D( _ColorsBars, appendResult24.xy ) ).rgb;
			float2 uv_AlphaGradientBlackontop = i.uv_texcoord * _AlphaGradientBlackontop_ST.xy + _AlphaGradientBlackontop_ST.zw;
			float mulTime33 = _Time.y * _AtmosphereTimeScale.x;
			float mulTime45 = _Time.y * _AtmosphereTimeScale.y;
			float4 appendResult29 = (float4(( i.uv_texcoord.x + mulTime33 ) , ( i.uv_texcoord.y + mulTime45 ) , 0.0 , 0.0));
			o.Alpha = ( ( _Opacity * ( tex2D( _AlphaGradientBlackontop, uv_AlphaGradientBlackontop ).r * ( tex2D( _AtmosphereTexture, appendResult29.xy ).r + _AtmosphereHaze ) ) ) + step( i.uv_texcoord.y , 0.1 ) );
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard alpha:fade keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19202
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;144.7341,-205.9926;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Cause + Christi / Light Beam Atmospheric;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Transparent;0.5;True;True;0;False;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.WireNode;50;-701.2189,446.1355;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;51;-721.9436,642.2253;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-615.9514,289.8;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;-428.4918,140.3841;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;37;-417.7114,384.4688;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;39;-601.5212,470.2841;Inherit;False;Constant;_Float2;Float 2;3;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;26;-172.2503,256.2406;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;46;-725.4774,143.8727;Inherit;False;Property;_Opacity;Opacity;5;0;Create;True;0;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;20;-802.2226,-88.60104;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;19;-835.7937,-222.0078;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;24;-426.4108,-198.6458;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleAddOpNode;22;-563.8408,-236.282;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-1103.767,-89.7186;Inherit;False;Property;_ColorBarTimeScale;Color Bar Time Scale;3;0;Create;True;0;0;0;False;0;False;-0.25;-0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;30;-1722.062,352.1963;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;31;-1722.339,459.1914;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;35;-2067.659,329.7967;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;29;-1568.463,385.7964;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleTimeNode;45;-2065.557,558.6148;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;44;-2360.558,418.6148;Inherit;False;Property;_AtmosphereTimeScale;Atmosphere Time Scale;4;0;Create;True;0;0;0;False;0;False;0,0;0.025,-0.025;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleTimeNode;33;-2062.461,472.397;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;52;-1815.878,651.7935;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;27;-1344.375,170.3295;Inherit;True;Property;_AlphaGradientBlackontop;Alpha Gradient (Black on top);1;0;Create;True;0;0;0;False;0;False;-1;7fa174d06d02a8e4d8636f1becb91841;7fa174d06d02a8e4d8636f1becb91841;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;28;-1345.375,371.3295;Inherit;True;Property;_AtmosphereTexture;Atmosphere Texture;2;0;Create;True;0;0;0;False;0;False;-1;d30ab184761bb9e4391b1cea96642d02;d30ab184761bb9e4391b1cea96642d02;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;56;-996.2156,479.1631;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;53;-1304.767,563.4778;Inherit;False;Property;_AtmosphereHaze;Atmosphere Haze;6;0;Create;True;0;0;0;False;0;False;1;2;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;8;-253.5727,-221.6769;Inherit;True;Property;_ColorsBars;Colors Bars;0;0;Create;True;0;0;0;False;0;False;-1;1019cf5b255bb6a43aaf3dede550ed07;1019cf5b255bb6a43aaf3dede550ed07;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;57;-302.5132,-485.1852;Inherit;False;Property;_ColorTint;Color Tint;7;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;60;24.48682,-359.1852;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
WireConnection;0;2;60;0
WireConnection;0;9;26;0
WireConnection;50;0;51;0
WireConnection;51;0;52;0
WireConnection;25;0;27;1
WireConnection;25;1;56;0
WireConnection;47;0;46;0
WireConnection;47;1;25;0
WireConnection;37;0;50;0
WireConnection;37;1;39;0
WireConnection;26;0;47;0
WireConnection;26;1;37;0
WireConnection;20;0;21;0
WireConnection;24;0;22;0
WireConnection;24;1;19;2
WireConnection;22;0;19;1
WireConnection;22;1;20;0
WireConnection;30;0;35;1
WireConnection;30;1;33;0
WireConnection;31;0;35;2
WireConnection;31;1;45;0
WireConnection;29;0;30;0
WireConnection;29;1;31;0
WireConnection;45;0;44;2
WireConnection;33;0;44;1
WireConnection;52;0;35;2
WireConnection;28;1;29;0
WireConnection;56;0;28;1
WireConnection;56;1;53;0
WireConnection;8;1;24;0
WireConnection;60;0;57;0
WireConnection;60;1;8;0
ASEEND*/
//CHKSM=BB77B0AF794BD83B2E5C2EEB043A9A6234F2F561