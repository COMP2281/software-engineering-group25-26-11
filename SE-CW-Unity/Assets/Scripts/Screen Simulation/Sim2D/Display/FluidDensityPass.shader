Shader "Fluid/DensityPass2D"
{
    Properties
    {
        _ParticleRadius ("Particle Radius", Float) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        
        // Additive blending to accumulate density
        Blend One One
        ZWrite Off
        ZTest Always
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            
            #include "UnityCG.cginc"
            
            StructuredBuffer<float2> Positions2D;
            
            float _ParticleRadius;
            float4x4 _WorldAnchorMatrix;
            float4 _SimWorldOffset;
            float _SimWorldScale;
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            v2f vert(appdata_full v, uint instanceID : SV_InstanceID)
            {
                // Get particle position in sim space
                float2 p2 = Positions2D[instanceID];
                float2 mapped = p2 * _SimWorldScale + _SimWorldOffset.xy;
                
                // Convert to local space (2D plane)
                float3 localCentre = float3(mapped.x, mapped.y, 0);
                
                // Add vertex offset scaled by particle radius
                float3 localVertPos = localCentre + v.vertex.xyz * _ParticleRadius;
                
                // Transform to world space
                float4 worldPos = mul(_WorldAnchorMatrix, float4(localVertPos, 1));
                
                v2f o;
                o.pos = UnityWorldToClipPos(worldPos.xyz);
                o.uv = v.texcoord;
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                // Calculate distance from center of particle
                float2 centreOffset = (i.uv - 0.5) * 2.0;
                float sqrDst = dot(centreOffset, centreOffset);
                
                // SPH-style smooth kernel (Poly6-like falloff)
                // This creates smooth density accumulation
                float r = sqrt(sqrDst);
                
                // Smooth falloff using cubic function
                float density = 0;
                if (r < 1.0)
                {
                    // Poly6 kernel approximation: (1 - r^2)^3
                    float t = 1.0 - sqrDst;
                    density = t * t * t;
                }
                
                return float4(density, density, density, 1);
            }
            ENDCG
        }
    }
}
