Shader "Custom/InstancedBillboardTest"
{
	Properties
	{
		//_Colour("Particle Colour", Color) = (1,1,1,1)
		
		// Distances from the centre are normalised s.t. 1 is the edge of the quad, 0 is the centre
		_SDF_RMin("SDF Circle Inner", float) = 0.5
		_SDF_RMax("SDF Circle Outer", float) = 0.9
	}
	SubShader
	{
		Tags
		{
			"Queue"="Transparent" "RenderType"="Transparent"
		}
		LOD 100

		Blend One One
		ZWrite Off
		Cull Off

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			float3 _CameraUp;
			float3 _CameraPos;
			float _SDF_RMin;
			float _SDF_RMax;

			UNITY_INSTANCING_BUFFER_START( Props )
				UNITY_DEFINE_INSTANCED_PROP( float4, _Colour )
				UNITY_DEFINE_INSTANCED_PROP( float4, _Position )
				UNITY_DEFINE_INSTANCED_PROP( float, _SizeMultiplier )
			UNITY_INSTANCING_BUFFER_END( Props )

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Variance
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Variance vert( Attributes v )
			{
				Variance o;
				ZERO_INITIALIZE(Variance, o);
				
				UNITY_SETUP_INSTANCE_ID( v );

				const float epsilon = 0.01;

				float3 instanceCentre_WS = UNITY_ACCESS_INSTANCED_PROP( Props, _Position.xyz );
				float sizeMult = UNITY_ACCESS_INSTANCED_PROP( Props, _SizeMultiplier );
				
				float3 fwd_WS = normalize( _CameraPos - instanceCentre_WS );
				float3 safeWorldCrossInput = 1.0 - fwd_WS[1] > epsilon ?
					float3(0,1,0) : // Safe to cross with world up
					float3(0,0,1);	// Unsafe to cross with world up (therefore safe with world fwd)
				float3 right_WS = normalize( cross( safeWorldCrossInput, fwd_WS ) );
				float3 up_WS = cross( fwd_WS, right_WS );
				
				float3 vertexPos_WS = instanceCentre_WS
					+ v.positionOS.x * sizeMult * right_WS
					+ v.positionOS.y * sizeMult * up_WS;


				o.positionCS = TransformWorldToHClip( vertexPos_WS );
				o.uv = v.uv;

				UNITY_TRANSFER_INSTANCE_ID(v, o);
				
				return o;
			}

			half4 frag( Variance i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( i );
				float4 col = UNITY_ACCESS_INSTANCED_PROP( Props, _Colour );

				float2 uv = i.uv - 0.5;
				float distT = length( uv ) * 2.0; // 0..1 distance from centre
				float alpha = 1.0 - smoothstep( _SDF_RMin, _SDF_RMax, distT );

				return col * alpha * col.a;
			}
			ENDHLSL
		}
	}
}