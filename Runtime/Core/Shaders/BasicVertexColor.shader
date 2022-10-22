
Shader "VoxelEngine/BasicVertexColor" 
{
	Properties
	{
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard vertex:vert
		#pragma target 3.0

		struct Input {
			float4 vertColor;
		};

		half _Glossiness;
		half _Metallic;

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.vertColor = v.color;
		}

		void surf(Input IN, inout SurfaceOutputStandard o) {
			o.Albedo = IN.vertColor.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
		}
		ENDCG
	}
		FallBack "Diffuse"
}