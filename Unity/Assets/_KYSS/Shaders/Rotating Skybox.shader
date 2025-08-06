// Made with Amplify Shader Editor v1.9.2.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Rotating Skybox"
{
	Properties
	{
		_TextureSample0("Texture Sample 0", 2D) = "white" {}
		_TimeScale("Time Scale", Float) = 0.0033
		_Metallic("Metallic", Range( 0 , 1)) = 0
		_Smoothness("Smoothness", Range( 0 , 1)) = 0
		_ColorTint("Color Tint", Color) = (0,0,0,0)
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha noshadow 
		struct Input
		{
			float3 viewDir;
		};

		uniform float4 _ColorTint;
		uniform sampler2D _TextureSample0;
		uniform float _TimeScale;
		uniform float _Metallic;
		uniform float _Smoothness;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Albedo = _ColorTint.rgb;
			float mulTime7 = _Time.y * _TimeScale;
			float2 _Vector0 = float2(1,-1);
			float2 _Vector1 = float2(0,1);
			float4 appendResult2 = (float4(( ( atan2( i.viewDir.x , i.viewDir.z ) / 6.28318548202515 ) + mulTime7 ) , (_Vector1.x + (( asin( i.viewDir.y ) / ( UNITY_PI * 0.5 ) ) - _Vector0.x) * (_Vector1.y - _Vector1.x) / (_Vector0.y - _Vector0.x)) , 0.0 , 0.0));
			o.Emission = tex2D( _TextureSample0, appendResult2.xy ).rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19202
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;-1042.383,-502.9211;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Rotating Skybox;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Front;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;False;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.SamplerNode;1;-1643.468,-475.1202;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;0;False;0;False;-1;d41cdb3234d571a4994065701d69b580;fe2b7a068dc864b4b8521007a0367856;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;10;-2967.479,-508.5643;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.PiNode;18;-2968.297,-358.4336;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;26;-2944.428,-273.3781;Inherit;False;Constant;_Float1;Float 1;1;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-2748.974,-297.6286;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ASinOpNode;13;-2727.194,-382.0321;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ATan2OpNode;12;-2725.892,-524.2113;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TauNode;30;-2566.613,-445.7333;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;20;-2573.192,-352.5135;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;23;-2591.268,-238.0161;Inherit;False;Constant;_Vector0;Vector 0;1;0;Create;True;0;0;0;False;0;False;1,-1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;31;-2590.742,-121.8494;Inherit;False;Constant;_Vector1;Vector 1;1;0;Create;True;0;0;0;False;0;False;0,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TFHCRemapNode;21;-2394.7,-204.9281;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;17;-2415.822,-525.1633;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;4;-2012.778,-525.0798;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-2398.396,-404.725;Inherit;False;Property;_TimeScale;Time Scale;1;0;Create;True;0;0;0;False;0;False;0.0033;0.0015;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;7;-2218.956,-407.9015;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;2;-1820.191,-451.9477;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;44;-1324.003,-380.7669;Inherit;False;Property;_Metallic;Metallic;2;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;45;-1320.703,-294.4669;Inherit;False;Property;_Smoothness;Smoothness;3;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;48;-1376.647,-671.0983;Inherit;False;Property;_ColorTint;Color Tint;4;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
WireConnection;0;0;48;0
WireConnection;0;2;1;0
WireConnection;0;3;44;0
WireConnection;0;4;45;0
WireConnection;1;1;2;0
WireConnection;25;0;18;0
WireConnection;25;1;26;0
WireConnection;13;0;10;2
WireConnection;12;0;10;1
WireConnection;12;1;10;3
WireConnection;20;0;13;0
WireConnection;20;1;25;0
WireConnection;21;0;20;0
WireConnection;21;1;23;1
WireConnection;21;2;23;2
WireConnection;21;3;31;1
WireConnection;21;4;31;2
WireConnection;17;0;12;0
WireConnection;17;1;30;0
WireConnection;4;0;17;0
WireConnection;4;1;7;0
WireConnection;7;0;8;0
WireConnection;2;0;4;0
WireConnection;2;1;21;0
ASEEND*/
//CHKSM=7FF07B46BB67A8432267913D3C99F17B4186AF4F