Shader "Unlit/RippleStamp"
{
    // Stamps a smooth circular displacement bump onto an existing RenderTexture.
    // Graphics.Blit(null, TempRT, StampMat) — source is unused; CurrRT is
    // passed as a material property so the full existing wave state is preserved
    // and the bump is added on top.
    //
    // Distance is computed in WORLD SPACE so the stamp is always a circle on the
    // physical surface regardless of the UV aspect ratio (e.g. an 8x3 surface
    // would produce a UV ellipse with the old UV-space distance).
    Properties
    {
        _CurrentRT   ("Current RT",   2D)     = "black" {}
        _HitUV       ("Hit UV",       Vector) = (0.5, 0.5, 0, 0)
        _Radius      ("Radius",       Float)  = 0.03
        _Strength    ("Strength",     Float)  = 1.0
        _WorldWidth  ("World Width",  Float)  = 8.0
        _WorldHeight ("World Height", Float)  = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _CurrentRT;
            float4    _HitUV;
            float     _Radius;
            float     _Strength;
            float     _WorldWidth;
            float     _WorldHeight;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Preserve existing wave height.
                float existing = tex2D(_CurrentRT, i.uv).r;

                // Convert UV offset to world-space offset so the stamp is a
                // circle in world space rather than a circle in UV space.
                float2 diff      = i.uv - _HitUV.xy;
                float2 worldDiff = diff * float2(_WorldWidth, _WorldHeight);
                float  worldDist = length(worldDiff);

                // _Radius is expressed as a fraction of the shorter world axis
                // so the stamp size stays visually consistent when the surface
                // is resized.
                float worldRadius = _Radius * min(_WorldWidth, _WorldHeight);

                float t    = 1.0 - saturate(worldDist / max(worldRadius, 0.0001));
                float bump = _Strength * (t * t);   // quadratic falloff → smooth edges

                return existing + bump;
            }
            ENDCG
        }
    }
}
