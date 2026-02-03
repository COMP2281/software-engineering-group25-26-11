Shader "Fluid/FluidSurface2D"
{
    Properties
    {
        _MainTex ("Scene Texture", 2D) = "white" {}
        _DensityTex ("Density Texture", 2D) = "black" {}
        _FluidColor ("Fluid Color", Color) = (0.2, 0.5, 0.9, 0.9)
        _EdgeColor ("Edge Color", Color) = (0.4, 0.7, 1.0, 1.0)
        _DensityThreshold ("Density Threshold", Range(0, 1)) = 0.3
        _EdgeWidth ("Edge Width", Range(0, 0.5)) = 0.1
        _FresnelPower ("Fresnel Power", Range(0, 2)) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        
        // Pass 0: Render fluid surface standalone
        Pass
        {
            Name "FluidSurface"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            sampler2D _DensityTex;
            float4 _DensityTex_TexelSize;
            float4 _FluidColor;
            float4 _EdgeColor;
            float _DensityThreshold;
            float _EdgeWidth;
            float _FresnelPower;
            
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
                float density = tex2D(_DensityTex, i.uv).r;
                
                // Smooth threshold with edge
                float innerThreshold = _DensityThreshold;
                float outerThreshold = _DensityThreshold + _EdgeWidth;
                
                // Calculate alpha based on density
                float alpha = smoothstep(innerThreshold * 0.5, innerThreshold, density);
                
                // Calculate edge factor for edge highlighting
                float edgeFactor = smoothstep(innerThreshold, outerThreshold, density);
                edgeFactor = 1.0 - edgeFactor; // Invert so edge is at threshold boundary
                edgeFactor *= step(innerThreshold * 0.8, density); // Only show edge where there's fluid
                
                // Calculate gradient for pseudo-normal (for lighting effects)
                float2 texelSize = _DensityTex_TexelSize.xy;
                float dx = tex2D(_DensityTex, i.uv + float2(texelSize.x, 0)).r 
                         - tex2D(_DensityTex, i.uv - float2(texelSize.x, 0)).r;
                float dy = tex2D(_DensityTex, i.uv + float2(0, texelSize.y)).r 
                         - tex2D(_DensityTex, i.uv - float2(0, texelSize.y)).r;
                
                float2 gradient = float2(dx, dy);
                float gradMag = length(gradient);
                
                // Fresnel-like effect based on gradient magnitude
                float fresnel = pow(saturate(gradMag * 2), _FresnelPower);
                
                // Blend between fluid color and edge color
                float3 color = lerp(_FluidColor.rgb, _EdgeColor.rgb, edgeFactor * 0.5 + fresnel * 0.5);
                
                // Add some depth variation based on density
                float depthFactor = saturate((density - _DensityThreshold) / (1.0 - _DensityThreshold));
                color = lerp(color, color * 0.8, depthFactor * 0.3);
                
                // Final alpha
                alpha *= _FluidColor.a;
                
                return float4(color, alpha);
            }
            ENDCG
        }
        
        // Pass 1: Composite fluid over scene
        Pass
        {
            Name "Composite"
            ZWrite Off
            ZTest Always
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            sampler2D _DensityTex;
            float4 _DensityTex_TexelSize;
            float4 _FluidColor;
            float4 _EdgeColor;
            float _DensityThreshold;
            float _EdgeWidth;
            float _FresnelPower;
            
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
                float4 sceneColor = tex2D(_MainTex, i.uv);
                float density = tex2D(_DensityTex, i.uv).r;
                
                // Calculate fluid color (same as Pass 0)
                float innerThreshold = _DensityThreshold;
                float outerThreshold = _DensityThreshold + _EdgeWidth;
                
                float alpha = smoothstep(innerThreshold * 0.5, innerThreshold, density);
                
                float edgeFactor = smoothstep(innerThreshold, outerThreshold, density);
                edgeFactor = 1.0 - edgeFactor;
                edgeFactor *= step(innerThreshold * 0.8, density);
                
                float2 texelSize = _DensityTex_TexelSize.xy;
                float dx = tex2D(_DensityTex, i.uv + float2(texelSize.x, 0)).r 
                         - tex2D(_DensityTex, i.uv - float2(texelSize.x, 0)).r;
                float dy = tex2D(_DensityTex, i.uv + float2(0, texelSize.y)).r 
                         - tex2D(_DensityTex, i.uv - float2(0, texelSize.y)).r;
                
                float gradMag = length(float2(dx, dy));
                float fresnel = pow(saturate(gradMag * 2), _FresnelPower);
                
                float3 fluidCol = lerp(_FluidColor.rgb, _EdgeColor.rgb, edgeFactor * 0.5 + fresnel * 0.5);
                float depthFactor = saturate((density - _DensityThreshold) / (1.0 - _DensityThreshold));
                fluidCol = lerp(fluidCol, fluidCol * 0.8, depthFactor * 0.3);
                
                alpha *= _FluidColor.a;
                
                // Blend fluid over scene
                float3 finalColor = lerp(sceneColor.rgb, fluidCol, alpha);
                
                return float4(finalColor, 1);
            }
            ENDCG
        }
    }
}
