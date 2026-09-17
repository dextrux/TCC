Shader "Custom/CloudParticle"
{

    Properties
    {
        _MainTint   ("Cor Base (nuvem iluminada)", Color) = (1, 1, 1, 1)
        _ShadowTint ("Cor de Sombra (parte de baixo)", Color) = (0.55, 0.6, 0.72, 1)
        _NoiseScale ("Escala Base do Ruido", Float) = 3.5
        _Softness   ("Suavidade da Borda", Range(0.01, 1)) = 0.35
        _Density    ("Densidade", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR; // Start Color do Particle System
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            float4 _MainTint;
            float4 _ShadowTint;
            float  _NoiseScale;
            float  _Softness;
            float  _Density;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            float fbm(float2 p)
            {
                float total = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    total += noise(p * freq) * amp;
                    freq *= 2.0;
                    amp *= 0.5;
                }
                return total;
            }

            float2 rotateUV(float2 uv, float angle)
            {
                float2 centered = uv - 0.5;
                float s = sin(angle);
                float c = cos(angle);
                float2 rotated = float2(
                    centered.x * c - centered.y * s,
                    centered.x * s + centered.y * c
                );
                return rotated + 0.5;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float seedA = IN.color.r; // desloca o ruido
                float seedB = IN.color.g; // rotaciona + muda escala

                // rotaciona o UV usado no ruido -> muda a orientacao do "grumo"
                float angle = seedB * 6.28318; // 0 a 360 graus
                float2 noiseUV = rotateUV(IN.uv, angle);

                // escala do ruido tambem varia por particula (nuvens mais
                // "grumosas" ou mais "lisas" dependendo do seed)
                float scale = lerp(_NoiseScale * 0.6, _NoiseScale * 1.6, seedB);

                float2 flowUV = noiseUV * scale + seedA * 41.37;

                // forma base: oval, mais achatada, tambem levemente rotacionada
                float2 shapeUV = rotateUV(IN.uv, angle * 0.5);
                float2 centered = (shapeUV - 0.5) * float2(1.0, lerp(1.2, 1.8, seedA));
                float dist = length(centered);
                float baseShape = 1.0 - smoothstep(0.1, lerp(0.4, 0.55, seedB), dist);

                float n  = fbm(flowUV);
                float n2 = fbm(flowUV * 2.3 + seedA * 7.0) * 0.5;
                float cloudNoise = n + n2;

                float shape = baseShape * (0.4 + cloudNoise * 0.8);
                float alpha = smoothstep(1.0 - _Density, 1.0 - _Density + _Softness, shape);

                // sombra: parte de baixo mais escura (efeito de volume)
                float shade = smoothstep(0.15, 0.55, IN.uv.y);
                float3 col = lerp(_ShadowTint.rgb, _MainTint.rgb, saturate(shade + cloudNoise * 0.3));

                return half4(col * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
