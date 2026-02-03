Shader "Instanced/Particle2D" {
	Properties {
		
	}
	SubShader {

		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		// Use premultiplied alpha for order-independent color blending
		// This prevents color jitter when particles overlap in different orders
		Blend One OneMinusSrcAlpha
		ZWrite Off

		Pass {

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5

			#include "UnityCG.cginc"
			
			StructuredBuffer<float2> Positions2D;
			StructuredBuffer<float2> Velocities;
			StructuredBuffer<float2> DensityData;
			StructuredBuffer<float4> ParticleColors;
			float scale;
			float4 colA;
			Texture2D<float4> ColourMap;
			SamplerState linear_clamp_sampler;
			float velocityMax;

			// NEW: anchor matrix
			float4x4 _WorldAnchorMatrix;
			// NEW: world mapping from sim space -> local anchor space
			float4 _SimWorldOffset; // xy used
			float _SimWorldScale;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 colour : TEXCOORD1;
			};

			v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
			{
				float speed = length(Velocities[instanceID]);
				float speedT = saturate(speed / velocityMax);
				float colT = speedT;

				// Get per-particle color
				float4 particleCol = ParticleColors[instanceID];

				// 2D sim position, mapped to local anchor space
                float2 p2 = Positions2D[instanceID];
                float2 mapped = p2 * _SimWorldScale + _SimWorldOffset.xy;

                // sim X/Y -> local X/Y (2D plane)
                float3 localCentre = float3(mapped.x, mapped.y, 0);

				// add quad vertex offset in local space - scale up for more overlap
				float3 localVertPos = localCentre + v.vertex.xyz * scale * 5.0;

				// local anchor space -> world
				float4 worldPos = mul(_WorldAnchorMatrix, float4(localVertPos, 1));

				v2f o;
				o.uv = v.texcoord;
				o.pos = UnityWorldToClipPos(worldPos.xyz);
				// Use per-particle color if it's not default (1,1,1,1), otherwise use gradient
				if (particleCol.r < 0.99 || particleCol.g < 0.99 || particleCol.b < 0.99)
				{
					o.colour = particleCol.rgb;
				}
				else
				{
					o.colour = ColourMap.SampleLevel(linear_clamp_sampler, float2(colT, 0.5), 0);
				}

				return o;
			}


			float4 frag (v2f i) : SV_Target
			{
				float2 centreOffset = (i.uv.xy - 0.5) * 2;
				float sqrDst = dot(centreOffset, centreOffset);
				
				float r = sqrt(sqrDst);
				
				// Discard pixels outside the circle
				if (r > 1.0) discard;
				
				// Smooth falloff - soft edges for blending
				float t = 1.0 - r;
				float alpha = t * t * (3.0 - 2.0 * t); // Smoothstep curve
				
				// Premultiplied alpha: multiply color by alpha
				// This makes blending order-independent and prevents color jitter
				float3 colour = i.colour * alpha;
				return float4(colour, alpha);
			}

			ENDCG
		}
	}
}