// Made with Amplify Shader Editor v1.9.2.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Cause/Rotating Skybox"
{
	Properties
	{
		_RotatingTexture("Rotating Texture", 2D) = "white" {}
		_StaticTexture("Static Texture", 2D) = "white" {}
		_RotationSpeed("Rotation Speed", Float) = 0.0033
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
		uniform sampler2D _StaticTexture;
		uniform sampler2D _RotatingTexture;
		uniform float _RotationSpeed;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Albedo = _ColorTint.rgb;
			float temp_output_17_0 = ( atan2( i.viewDir.x , i.viewDir.z ) / 6.28318548202515 );
			float2 _Vector0 = float2(1,-1);
			float2 _Vector1 = float2(0,1);
			float temp_output_21_0 = (_Vector1.x + (( asin( i.viewDir.y ) / ( UNITY_PI * 0.5 ) ) - _Vector0.x) * (_Vector1.y - _Vector1.x) / (_Vector0.y - _Vector0.x));
			float4 appendResult52 = (float4(temp_output_17_0 , temp_output_21_0 , 0.0 , 0.0));
			float mulTime7 = _Time.y * _RotationSpeed;
			float4 appendResult2 = (float4(( mulTime7 + temp_output_17_0 ) , temp_output_21_0 , 0.0 , 0.0));
			float4 tex2DNode1 = tex2D( _RotatingTexture, appendResult2.xy );
			float4 appendResult55 = (float4(tex2DNode1.r , tex2DNode1.g , tex2DNode1.b , 0.0));
			float4 lerpResult56 = lerp( tex2D( _StaticTexture, appendResult52.xy ) , appendResult55 , tex2DNode1.a);
			o.Emission = lerpResult56.xyz;
			o.Alpha = 1;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19202
Node;AmplifyShaderEditor.CommentaryNode;58;-1871.51,-462.782;Inherit;False;533.1217;277;Static Texture;2;52;51;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;57;-2027.099,-118.459;Inherit;False;717.5167;345.1447;Rotating Texture;3;2;1;55;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;50;-3608.906,-479.8621;Inherit;False;833.5972;613.3139;Skybox View Projection;12;10;18;26;25;13;12;30;20;23;31;21;17;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;49;-2734.691,157.3631;Inherit;False;639.8237;163.332;Add Rotation on the X Axis;3;8;7;4;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;4;-2268.69,207.3631;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;7;-2482.573,212.2442;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;10;-3558.088,-413.2631;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.PiNode;18;-3558.906,-263.1324;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-3339.583,-202.3275;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ASinOpNode;13;-3317.803,-286.7309;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ATan2OpNode;12;-3316.501,-428.9101;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TauNode;30;-3157.222,-350.4321;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;20;-3163.8,-257.2123;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;23;-3181.877,-142.715;Inherit;False;Constant;_Vector0;Vector 0;1;0;Create;True;0;0;0;False;0;False;1,-1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;31;-3181.351,-26.54822;Inherit;False;Constant;_Vector1;Vector 1;1;0;Create;True;0;0;0;False;0;False;0,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TFHCRemapNode;21;-2985.309,-109.6269;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;17;-3006.431,-429.8621;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;26;-3535.037,-178.077;Inherit;False;Constant;_Float1;Float 1;1;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-2665.298,214.7738;Inherit;False;Property;_RotationSpeed;Rotation Speed;2;0;Create;True;0;0;0;False;0;False;0.0033;0.01;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;2;-1977.099,23.72493;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SamplerNode;1;-1820.309,-0.3142434;Inherit;True;Property;_RotatingTexture;Rotating Texture;0;0;Create;True;0;0;0;False;0;False;-1;d41cdb3234d571a4994065701d69b580;674aede34c33efa41a16489757bd5945;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;55;-1487.582,-68.45895;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DynamicAppendNode;52;-1821.509,-402.6095;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SamplerNode;51;-1651.723,-412.782;Inherit;True;Property;_StaticTexture;Static Texture;1;0;Create;True;0;0;0;False;0;False;-1;d41cdb3234d571a4994065701d69b580;fe2b7a068dc864b4b8521007a0367856;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;48;-1214.374,-653.9183;Inherit;False;Property;_ColorTint;Color Tint;3;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;56;-1178.137,-294.6307;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;-858.9217,-508.3488;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Cause/Rotating Skybox;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Front;0;False;;0;False;;False;0;False;;0;False;;False;0;Opaque;0.5;True;False;0;False;Opaque;;Geometry;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;4;0;7;0
WireConnection;4;1;17;0
WireConnection;7;0;8;0
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
WireConnection;2;0;4;0
WireConnection;2;1;21;0
WireConnection;1;1;2;0
WireConnection;55;0;1;1
WireConnection;55;1;1;2
WireConnection;55;2;1;3
WireConnection;52;0;17;0
WireConnection;52;1;21;0
WireConnection;51;1;52;0
WireConnection;56;0;51;0
WireConnection;56;1;55;0
WireConnection;56;2;1;4
WireConnection;0;0;48;0
WireConnection;0;2;56;0
ASEEND*/
//CHKSM=954CC3F76829556642863E9A7152DEDB1EF9B6ED