Shader "SpaceShooter/AnimatedFloor"
{
    // Scrolling cartoon tile floor for "flying through an office corridor".
    // - Flat base colour (palette-tinted).
    // - Tile grid with thin glowing seams.
    // - Running neon stripes that sweep toward the camera to sell speed.
    // - All procedural — no texture sampling, works on low-end mobile WebGL.
    Properties
    {
        _BaseColor    ("Base Color",    Color) = (0.06, 0.02, 0.10, 1)
        _SeamColor    ("Seam Color",    Color) = (1.0, 0.18, 0.55, 1)
        _AccentColor  ("Accent Color",  Color) = (1.0, 0.78, 0.25, 1)
        _TileScale    ("Tile Scale",    Float) = 6.0
        _SeamWidth    ("Seam Width",    Float) = 0.04
        _ScrollSpeed  ("Scroll Speed",  Float) = 0.55
        _StripeFreq   ("Stripe Freq",   Float) = 4.0
        _StripeSpeed  ("Stripe Speed",  Float) = 1.6
        _StripeStrength ("Stripe Str",  Float) = 0.45
        _GlowPower    ("Glow Power",    Float) = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" }
        Pass
        {
            Cull Off
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _BaseColor;
            float4 _SeamColor;
            float4 _AccentColor;
            float _TileScale;
            float _SeamWidth;
            float _ScrollSpeed;
            float _StripeFreq;
            float _StripeSpeed;
            float _StripeStrength;
            float _GlowPower;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Scroll UV so tiles move toward the camera over time.
                float2 uv = i.uv * _TileScale;
                uv.y += _Time.y * _ScrollSpeed;

                // Tile seams: thin bright lines on integer boundaries.
                float2 fuv  = frac(uv);
                float2 dist = min(fuv, 1.0 - fuv);
                float seam  = 1.0 - smoothstep(0.0, _SeamWidth, min(dist.x, dist.y));

                // Base colour — deep palette-tinted backdrop.
                float3 col = _BaseColor.rgb;

                // Add seam neon highlight with soft glow.
                float seamGlow = pow(seam, _GlowPower);
                col += _SeamColor.rgb * seam + _SeamColor.rgb * seamGlow * 0.35;

                // Running stripes — horizontal bands moving from horizon to player.
                float stripe = sin(i.uv.y * _StripeFreq * 6.28318 - _Time.y * _StripeSpeed * 6.28318);
                stripe = smoothstep(0.92, 1.0, stripe);
                col += _AccentColor.rgb * stripe * _StripeStrength;

                // Vignette toward horizon so the floor fades rather than
                // hitting the fog abruptly.
                float horizonFade = smoothstep(0.0, 0.2, i.uv.y);
                col *= horizonFade;

                return float4(col, 1);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
