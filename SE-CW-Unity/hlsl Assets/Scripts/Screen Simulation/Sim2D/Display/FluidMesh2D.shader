Shader "Fluid/FluidMesh2D"
{
    Properties
    {
        _Color          ("Color Tint",          Color)          = (1, 1, 1, 1)
        _SpecPower      ("Specular Power",      Range(1, 128))  = 32
        _SpecIntensity  ("Specular Intensity",  Range(0, 1))    = 0.5
        _FresnelPow     ("Fresnel Power",       Range(0.5, 5))  = 2.0
        _FresnelStr     ("Fresnel Intensity",   Range(0, 1))    = 0.3
        _AmbientMin     ("Ambient Minimum",     Range(0, 1))    = 0.35

        // Ripple inputs
        _RippleTex      ("Ripple Texture",      2D)             = "white" {}
        _RippleUVScale  ("Ripple UV Scale",     Vector)         = (1, 1, 0, 0)
        _RippleUVOffset ("Ripple UV Offset",    Vector)         = (0, 0, 0, 0)
        _RippleStrength ("Ripple Strength",     Range(0,1))     = 0.6
        _RippleSampleDelta ("Ripple Sample Delta", Float)       = 0.004
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FluidMeshForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_RippleTex);
            SAMPLER(sampler_RippleTex);

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float4 col   : COLOR;
                float3 wNorm : TEXCOORD0;
                float3 wPos  : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _SpecPower;
                float  _SpecIntensity;
                float  _FresnelPow;
                float  _FresnelStr;
                float  _AmbientMin;

                float4 _RippleUVScale;   // xy used
                float4 _RippleUVOffset;  // xy used
                float  _RippleStrength;
                float  _RippleSampleDelta;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(v.vertex.xyz);
                o.pos   = posInputs.positionCS;
                o.wPos  = posInputs.positionWS;
                o.col   = v.color * _Color;
                o.wNorm = TransformObjectToWorldNormal(v.normal);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.wNorm);
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.wPos));

                // Flip normal toward the camera so both sides shade correctly
                if (dot(n, viewDir) < 0)
                    n = -n;

                // Sample ripple texture (map world position → ripple UV)
                float2 uv = i.wPos.xz * _RippleUVScale.xy + _RippleUVOffset.xy;

                // small finite-difference to get surface gradient from ripple height
                float delta = max(_RippleSampleDelta, 1e-6);
                float h = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, uv).r;
                float hx = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, uv + float2(delta, 0)).r;
                float hy = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, uv + float2(0, delta)).r;

                float dhdx = (hx - h) / delta;
                float dhdy = (hy - h) / delta;

                // Build a local bump normal where y is "up"
                float3 bumpLocal = normalize(float3(-dhdx, 1.0, -dhdy));

                // Convert bumpLocal into world-space perturbation by constructing a tangent basis from the world normal
                float3 upRef = float3(0, 1, 0);
                float3 t = normalize(cross(upRef, n));
                if (any(isnan(t))) t = float3(1, 0, 0);
                float3 b = cross(n, t);

                // Compose the perturbed normal in world space
                float3 worldPerturbed = normalize(n * bumpLocal.y + t * bumpLocal.x + b * bumpLocal.z);

                // Blend between original and perturbed normal by strength
                n = normalize(lerp(n, worldPerturbed, _RippleStrength));

                // Use the main URP light direction if available, fallback to fixed
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;

                // Wrapped diffuse (soft, never fully dark)
                float NdotL = dot(n, lightDir) * 0.5 + 0.5;
                float diffuse = lerp(_AmbientMin, 1.0, NdotL);

                // Blinn-Phong specular
                float3 halfDir = normalize(lightDir + viewDir);
                float  NdotH   = max(0, dot(n, halfDir));
                float  spec    = pow(NdotH, _SpecPower) * _SpecIntensity;

                // Fresnel rim (bright edges for a wet look)
                float NdotV   = max(0, dot(n, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPow) * _FresnelStr;

                float3 color = i.col.rgb * diffuse + spec + fresnel * half3(0.6, 0.7, 0.8);
                return half4(color, i.col.a);
            }
            ENDHLSL
        }

        // Depth-only pass so the mesh interacts properly with URP effects
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata { float4 vertex : POSITION; };
            struct v2f_d  { float4 pos : SV_POSITION; };

            v2f_d DepthVert(appdata v)
            {
                v2f_d o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                return o;
            }
            half4 DepthFrag(v2f_d i) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}