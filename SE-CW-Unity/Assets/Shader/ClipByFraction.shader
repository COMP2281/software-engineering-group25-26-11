Shader "Custom/ClipByFraction"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _ClipWidth ("Clip Width (0-1)", Range(0,1)) = 1.0
        _ClipHeight ("Clip Height (0-1)", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 localPos : TEXCOORD1; // object-space position
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _ClipWidth;
            float _ClipHeight;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.localPos = v.vertex.xyz; // pass raw object-space position
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Assumes mesh is centered at origin.
                // Discard pixels outside the clip fraction on each axis.
                // e.g. _ClipWidth=0.5 shows only the middle 50% in X.

                float halfW = _ClipWidth * 0.5;
                float halfH = _ClipHeight * 0.5;

                clip(halfW - abs(i.localPos.x)); // discard if |x| > halfW
                clip(halfH - abs(i.localPos.y)); // discard if |y| > halfH

                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
}