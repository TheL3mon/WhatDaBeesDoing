Shader "Custom/FieldContainerURP" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_Team0Color ("Team 0 Color", Color) = (1,1,1,1)
		_Team1Color ("Team 1 Color", Color) = (1,1,1,1)
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_HivePosition ("Hive Position", Float) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalRenderPipeline"}
		
		LOD 200

		Pass
		{
			HLSLPROGRAM
			Cull Front
			#pragma surface surf 
			#pragma Standard fullforwardshadows
			#pragma target 3.0


			struct Input {
			float3 worldPos;
			};

			half _Glossiness;
			half _Metallic;
			float _HivePosition;
			fixed4 _Color;
			fixed4 _Team0Color;
			fixed4 _Team1Color;


			void surf(Input IN, inout SurfaceOutputStandard o) {
				// Albedo comes from a texture tinted by color
				fixed4 c = _Color;
				c = lerp(c,_Team0Color,smoothstep(-_HivePosition,-_HivePosition - .5f,IN.worldPos.x));
				c = lerp(c,_Team1Color,smoothstep(_HivePosition,_HivePosition + .5f,IN.worldPos.x));
				o.Albedo = c.rgb;
				// Metallic and smoothness come from slider variables
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				o.Alpha = c.a;
			}
			//Standard fullforwardshadows vertex:vert addshadow
			ENDHLSL
		}
	}
	FallBack "Diffuse"
}
