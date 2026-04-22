Shader "Hidden/SpaceShooter/PostFX"
{
    Properties
    {
        _MainTex        ("Source", 2D)    = "white" {}
        _BloomStrength  ("Bloom",    Float) = 0.22
        _ChromaStrength ("Chroma",   Float) = 0.0012
        _VignetteStart  ("VigStart", Float) = 0.45
        _VignetteEnd    ("VigEnd",   Float) = 1.10
        _VignetteColor  ("VigColor", Color) = (0,0,0,1)
        _GrainStrength  ("Grain",    Float) = 0.015
        _PosterizeSteps ("PosterizeSteps", Float) = 8
        _Saturation     ("Saturation", Float) = 1.25
        _Time2          ("Time",     Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;
            float     _BloomStrength;
            float     _ChromaStrength;
            float     _VignetteStart;
            float     _VignetteEnd;
            float4    _VignetteColor;
            float     _GrainStrength;
            float     _PosterizeSteps;
            float     _Saturation;
            float     _Time2;

            float hash(float2 p){ return frac(sin(dot(p, float2(41.13, 289.97))) * 43758.5453); }

            // 9-tap gaussian bloom at moderate radius, isolating bright pixels.
            float3 bloomSample(float2 uv)
            {
                float2 ts = _MainTex_TexelSize.xy * 3.0;
                float3 sum = 0;
                sum += tex2D(_MainTex, uv + float2(-2,-2)*ts).rgb;
                sum += tex2D(_MainTex, uv + float2( 2,-2)*ts).rgb;
                sum += tex2D(_MainTex, uv + float2(-2, 2)*ts).rgb;
                sum += tex2D(_MainTex, uv + float2( 2, 2)*ts).rgb;
                sum += tex2D(_MainTex, uv + float2( 0,-2)*ts).rgb;
                sum += tex2D(_MainTex, uv + float2( 0, 2)*ts).rgb;
                sum += tex2D(_MainTex, uv + float2(-2, 0)*ts).rgb;
                sum += tex2D(_MainTex, uv + float2( 2, 0)*ts).rgb;
                sum += tex2D(_MainTex, uv).rgb;
                sum /= 9.0;
                // Keep only the bright part (soft knee at ~0.7)
                float luma = dot(sum, float3(0.299, 0.587, 0.114));
                float k = saturate((luma - 0.65) / 0.35);
                return sum * k;
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float2 c  = uv - 0.5;

                // Chromatic aberration — sample R/G/B with radial offset.
                float chroma = _ChromaStrength;
                float3 col;
                col.r = tex2D(_MainTex, uv + c * chroma).r;
                col.g = tex2D(_MainTex, uv).g;
                col.b = tex2D(_MainTex, uv - c * chroma).b;

                // Bloom — sample once, add additive.
                col += bloomSample(uv) * _BloomStrength;

                // Cartoon saturation boost (luma-preserving)
                float luma = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(float3(luma, luma, luma), col, _Saturation);

                // Posterize — quantize colour channels to N bands for
                // flat cel-shaded look. _PosterizeSteps >= 32 effectively
                // disables.
                if (_PosterizeSteps > 1.0 && _PosterizeSteps < 32.0)
                {
                    col = floor(col * _PosterizeSteps) / _PosterizeSteps;
                }

                // Soft highlight clip to keep bright areas from blowing out
                col = 1.0 - exp(-col * 1.1);

                // Vignette — radial darken
                float d = length(c) * 1.41421356;
                float v = smoothstep(_VignetteEnd, _VignetteStart, d);
                col = lerp(_VignetteColor.rgb, col, v);

                // Gentle film grain — keeps flat surfaces alive.
                float n = hash(uv * 1024.0 + _Time2 * 40.0) - 0.5;
                col += n * _GrainStrength;

                return float4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
