Shader "Hidden/FluidBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
        _BlurStrength ("Blur Strength", Float) = 0.8
    }
    
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass 0: Horizontal blur
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 offset = float2(_MainTex_TexelSize.x * _BlurSize, 0);
                
                float4 color = tex2D(_MainTex, uv) * 0.29411764;
                color += tex2D(_MainTex, uv + offset) * 0.23529411;
                color += tex2D(_MainTex, uv - offset) * 0.23529411;
                color += tex2D(_MainTex, uv + offset * 2) * 0.11764705;
                color += tex2D(_MainTex, uv - offset * 2) * 0.11764705;
                return color;
            }
            ENDCG
        }

        // Pass 1: Vertical blur
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 offset = float2(0, _MainTex_TexelSize.y * _BlurSize);
                
                float4 color = tex2D(_MainTex, uv) * 0.29411764;
                color += tex2D(_MainTex, uv + offset) * 0.23529411;
                color += tex2D(_MainTex, uv - offset) * 0.23529411;
                color += tex2D(_MainTex, uv + offset * 2) * 0.11764705;
                color += tex2D(_MainTex, uv - offset * 2) * 0.11764705;
                
                return color;
            }
            ENDCG
        }

        // Pass 2: Blend original with blurred
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _BlurTex;
            float _BlurStrength;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 original = tex2D(_MainTex, i.uv);
                float4 blurred = tex2D(_BlurTex, i.uv);
                return lerp(original, blurred, _BlurStrength);
            }
            ENDCG
        }
    }
}
