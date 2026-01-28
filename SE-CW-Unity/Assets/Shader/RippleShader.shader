Shader "Unlit/RippleShader"
{
    Properties
    {
        // object scale passed from script (x = U axis scale, y = V axis scale)
        _ObjectScale("Object Scale", Vector) = (1,1,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _PrevRT;
            sampler2D _CurrentRT;
            float4 _CurrentRT_TexelSize;
            float4 _ObjectScale; // x = U scale, y = V scale

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Use division by object scale so UV offsets correspond to equal world distances.
                // Protect against zero scale by maxing with a small epsilon.
                float2 safeScale = max(_ObjectScale.xy, float2(1e-6, 1e-6));
                float2 texel = _CurrentRT_TexelSize.xy / safeScale;

                float2 uv = i.uv;
                float speed = 1.5f; // Increased from 1.0 for faster ripples

                float p10 = tex2D(_CurrentRT, uv - float2(texel.x, 0.0) * speed).x;
                float p01 = tex2D(_CurrentRT, uv - float2(0.0, texel.y) * speed).x;
                float p21 = tex2D(_CurrentRT, uv + float2(texel.x, 0.0) * speed).x;
                float p12 = tex2D(_CurrentRT, uv + float2(0.0, texel.y) * speed).x;

                float p11 = tex2D(_PrevRT, uv).x;

                float d = (p10 + p01 + p21 + p12)/2 - p11;
                d *= 0.99f;

                // return as a grayscale texture
                return fixed4(d, d, d, 1.0);
            }
            ENDCG
        }
    }
}