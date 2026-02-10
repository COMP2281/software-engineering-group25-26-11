Shader "Instanced/Particle2D" {
	Properties {
		_SpecularPower ("Specular Power", Range(1, 64)) = 16
		_SpecularIntensity ("Specular Intensity", Range(0, 1)) = 0.4
		_FresnelPower ("Fresnel Power", Range(0.5, 5)) = 2.0
		_FresnelIntensity ("Fresnel Intensity", Range(0, 1)) = 0.3
		_DepthDarkening ("Depth Darkening", Range(0, 0.5)) = 0.2
		_VelocityStretch ("Velocity Stretch", Range(0, 2)) = 0.5
		_ParticleSize ("Particle Size Multiplier", Range(1, 10)) = 3.0
		_EdgeSharpness ("Edge Sharpness", Range(0, 1)) = 0.5
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

			// Realistic rendering properties
			float _SpecularPower;
			float _SpecularIntensity;
			float _FresnelPower;
			float _FresnelIntensity;
			float _DepthDarkening;
			float _VelocityStretch;
			float _ParticleSize;
			float _EdgeSharpness;

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
				float speed : TEXCOORD2;
				float2 velocityDir : TEXCOORD3;
			};

			v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
			{
				float2 velocity = Velocities[instanceID];
				float speed = length(velocity);
				float speedT = saturate(speed / velocityMax);
				float colT = speedT;

				// Get per-particle color
				float4 particleCol = ParticleColors[instanceID];

				// 2D sim position, mapped to local anchor space
                float2 p2 = Positions2D[instanceID];
                float2 mapped = p2 * _SimWorldScale + _SimWorldOffset.xy;

                // sim X/Y -> local X/Y (2D plane)
                float3 localCentre = float3(mapped.x, mapped.y, 0);

				// Velocity-based stretching
				float3 vertOffset = v.vertex.xyz;
				if (speed > 0.01)
				{
					float2 velDir = velocity / speed;
					// Stretch along velocity direction
					float stretchAmount = 1.0 + speedT * _VelocityStretch;
					float2 localVert2D = v.vertex.xy;
					float dotVel = dot(localVert2D, velDir);
					// Stretch component along velocity, keep perpendicular component
					float2 parallel = dotVel * velDir * stretchAmount;
					float2 perp = localVert2D - dotVel * velDir;
					// Slightly compress perpendicular to maintain volume
					perp *= 1.0 / sqrt(stretchAmount);
					vertOffset.xy = parallel + perp;
				}

				// add quad vertex offset in local space - _ParticleSize controls overlap/blur
				float3 localVertPos = localCentre + vertOffset * scale * _ParticleSize;

				// local anchor space -> world
				float4 worldPos = mul(_WorldAnchorMatrix, float4(localVertPos, 1));

				v2f o;
				o.uv = v.texcoord;
				o.pos = UnityWorldToClipPos(worldPos.xyz);
				o.speed = speedT;
				o.velocityDir = speed > 0.01 ? velocity / speed : float2(0, 1);
				
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
				
				// Adjustable edge sharpness - higher = sharper/less blurry
				float t = 1.0 - r;
				float softAlpha = t * t * (3.0 - 2.0 * t); // Smoothstep curve (soft)
				float hardAlpha = smoothstep(0.0, 0.3, t); // Sharper falloff
				float baseAlpha = lerp(softAlpha, hardAlpha, _EdgeSharpness);
				
				// === REALISTIC LIGHTING ===
				
				// Simulate a pseudo-3D normal from the 2D circle (hemisphere)
				float3 normal = float3(centreOffset.x, centreOffset.y, sqrt(max(0, 1.0 - sqrDst)));
				normal = normalize(normal);
				
				// Light direction (top-right, slightly forward)
				float3 lightDir = normalize(float3(0.5, 0.7, 0.8));
				
				// View direction (looking at screen)
				float3 viewDir = float3(0, 0, 1);
				
				// Diffuse lighting (soft)
				float NdotL = dot(normal, lightDir);
				float diffuse = NdotL * 0.3 + 0.7; // Wrapped diffuse for softer look
				
				// Specular highlight (shiny wet look)
				float3 halfDir = normalize(lightDir + viewDir);
				float NdotH = max(0, dot(normal, halfDir));
				float specular = pow(NdotH, _SpecularPower) * _SpecularIntensity;
				
				// Fresnel rim lighting (bright edges)
				float NdotV = max(0, dot(normal, viewDir));
				float fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelIntensity;
				
				// Depth darkening (darker in center for depth)
				float depth = 1.0 - _DepthDarkening * (1.0 - r);
				
				// Combine color with lighting
				float3 baseColor = i.colour;
				
				// Add slight color variation based on speed
				float3 highlightColor = lerp(baseColor, float3(1, 1, 1), 0.3);
				
				// Final color composition
				float3 litColor = baseColor * diffuse * depth;
				litColor += specular * highlightColor; // Specular adds brightness
				litColor += fresnel * highlightColor * 0.5; // Fresnel rim
				
				// Slightly boost saturation for vibrancy
				float luminance = dot(litColor, float3(0.299, 0.587, 0.114));
				litColor = lerp(float3(luminance, luminance, luminance), litColor, 1.1);
				
				// Alpha with softer edges
				float alpha = baseAlpha * (0.85 + 0.15 * NdotV); // Slightly more opaque facing camera
				
				// Premultiplied alpha output
				float3 finalColor = litColor * alpha;
				return float4(finalColor, alpha);
			}

			ENDCG
		}
	}
}