Shader "Fluid/GaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZWrite Off
        ZTest Always
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _BlurDirection;
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            v2f vert(appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                // 9-tap Gaussian blur
                // Weights for sigma ~= 1.5
                float weights[9] = {
                    0.0162162162,
                    0.0540540541,
                    0.1216216216,
                    0.1945945946,
                    0.2270270270,
                    0.1945945946,
                    0.1216216216,
                    0.0540540541,
                    0.0162162162
                };
                
                float2 direction = _BlurDirection.xy;
                float result = 0;
                
                for (int j = 0; j < 9; j++)
                {
                    float2 offset = direction * (j - 4);
                    result += tex2D(_MainTex, i.uv + offset).r * weights[j];
                }
                
                return float4(result, result, result, 1);
            }
            ENDCG
        }
    }
}
