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

        [Header(Ripple)]
        _RippleTex      ("Ripple RT",           2D)             = "gray" {}
        _RippleStr      ("Ripple Strength",     Range(0, 2))    = 0.4
        _Smoothness     ("Surface Smoothness",  Range(0, 1))    = 0.8
        _EdgeSoftness   ("Edge Softness",       Range(0, 0.1))  = 0.02
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
            #pragma target 3.5
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── Ripple texture (URP-style explicit declaration) ──────────
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
                float4 pos      : SV_POSITION;
                float4 col      : COLOR;
                float3 wNorm    : TEXCOORD0;
                float3 wPos     : TEXCOORD1;
                float2 rippleUV : TEXCOORD2;   // world-space XY → ripple RT
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _SpecPower;
                float  _SpecIntensity;
                float  _FresnelPow;
                float  _FresnelStr;
                float  _AmbientMin;
                float4 _RippleTex_ST;
                float  _RippleStr;
                float4x4 _RippleCamVP;
                float  _Smoothness;
                float  _EdgeSoftness;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(v.vertex.xyz);
                o.pos      = posInputs.positionCS;
                o.wPos     = posInputs.positionWS;
                o.col      = v.color * _Color;
                o.wNorm    = TransformObjectToWorldNormal(v.normal);

                // Project world position through RenderCam's VP matrix.
                // This gives the same clip-space position the RT camera used,
                // so UV aligns perfectly with what was rendered into ObjectsRT.
                float4 rippleClip = mul(_RippleCamVP, float4(posInputs.positionWS, 1.0));
                // Perspective divide → NDC [-1,1], then remap to UV [0,1]
                float2 ndc    = rippleClip.xy / rippleClip.w;
                o.rippleUV    = ndc * 0.5 + 0.5;
                o.rippleUV.y  = 1.0 - o.rippleUV.y;

                return o;
            }

            // ── Finite-difference normal from a greyscale height map ─────
            // Returns a tangent-space-like perturbation in world space.
            float3 RippleNormal(float2 uv, float strength)
            {
                // Step size — one texel in UV space at the chosen scale.
                // Using a fixed step keeps cost predictable regardless of RT size.
                float2 e = float2(0.005, 0.0);

                float hC  = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, uv          ).r;
                float hPX = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, uv + e.xy   ).r;
                float hPY = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, uv + e.yx   ).r;

                // Gradient → surface normal (right-hand, Z-up for XY-plane fluid)
                float3 n = float3((hC - hPX) * strength,
                                  (hC - hPY) * strength,
                                  1.0);
                return normalize(n);
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.wNorm);
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.wPos));

                // ── Sample ripple height ─────────────────────────────────
                float rippleH     = SAMPLE_TEXTURE2D(_RippleTex, sampler_RippleTex, i.rippleUV).r;
                float ripplePulse = saturate(abs(rippleH) * _RippleStr * 4.0);

                // ── Ripple normal perturbation ───────────────────────────
                float3 rippleN = RippleNormal(i.rippleUV, _RippleStr);

                float3 up    = abs(n.z) < 0.999 ? float3(0, 0, 1) : float3(1, 0, 0);
                float3 tangX = normalize(cross(up, n));
                float3 tangY = cross(n, tangX);
                n = normalize(tangX * rippleN.x + tangY * rippleN.y + n * rippleN.z);

                if (dot(n, viewDir) < 0)
                    n = -n;

                // ── Lighting ─────────────────────────────────────────────
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;

                // Smooth wrapped diffuse for soft, liquid appearance
                float NdotL = dot(n, lightDir);
                float wrappedNdotL = (NdotL + 1.0) * 0.5; // Wrap lighting
                wrappedNdotL = smoothstep(_AmbientMin, 1.0, wrappedNdotL); // Smooth transition
                float diffuse = lerp(0.8, 1.0, wrappedNdotL); // Keep colors brighter

                float3 halfDir = normalize(lightDir + viewDir);
                float  NdotH   = max(0, dot(n, halfDir));
                float  specPower = lerp(_SpecPower, _SpecPower * 2.0, _Smoothness);
                float  spec    = pow(NdotH, specPower) * _SpecIntensity * _Smoothness;

                // Enhanced Fresnel rim for wet, liquid edges
                float NdotV   = max(0, dot(n, viewDir));
                float fresnelBase = 1.0 - NdotV;
                float fresnel = smoothstep(0, 1, pow(fresnelBase, _FresnelPow)) * _FresnelStr;

                float3 color = i.col.rgb * diffuse + spec + fresnel * half3(0.6, 0.7, 0.8);

                // ── Direct ripple brightness — visible regardless of lighting angle ──
                color += ripplePulse * half3(0.5, 0.7, 1.0);

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
