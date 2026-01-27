Shader "Unlit/Add"
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

            sampler2D _ObjectsRT;
            sampler2D _CurrentRT;
            float4 _ObjectsRT_ST;
            float4 _ObjectScale; // kept for API parity; not used here

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _ObjectsRT);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture (objects)
                fixed4 tex1 = tex2D(_ObjectsRT, i.uv);

                // Sample the ripple/current RT using the mesh UV directly.
                // CurrRT stores the ripple field in UV space, so we must sample with the same UV.
                fixed4 tex2 = tex2D(_CurrentRT, i.uv);

                return tex1 + tex2;
            }
            ENDCG
        }
    }
}