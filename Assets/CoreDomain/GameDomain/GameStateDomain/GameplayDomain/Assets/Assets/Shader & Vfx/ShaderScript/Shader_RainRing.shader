Shader "Custom/WaterSplashRing"
{
    // Splash circular pra usar num Particle System de "impacto" (disparado
    // quando a gota de chuva colide com o chao). Desenha um nucleo
    // brilhante no centro (flash do impacto) + um anel de onda (ripple)
    // com borda irregular (respingos/gotinhas voando pra fora).
    //
    // A ANIMACAO (crescer e desaparecer) e feita pelo proprio Particle
    // System via "Size over Lifetime" (cresce) e "Color over Lifetime"
    // (alpha caindo a 0) - o shader ja multiplica pela vertex color
    // (IN.color.a), entao esses modulos funcionam automaticamente.

    Properties
    {
        _CoreColor  ("Cor do Nucleo (flash)", Color) = (1, 1, 1, 1)
        _RingColor  ("Cor do Anel (ondulacao)", Color) = (0.8, 0.9, 1, 1)
        _RingRadius ("Raio do Anel (0-1)", Range(0.1, 0.9)) = 0.55
        _RingWidth  ("Largura do Anel", Range(0.02, 0.4)) = 0.12
        _CoreSize   ("Tamanho do Nucleo", Range(0, 0.4)) = 0.12
        _SpikeAmount ("Irregularidade dos Respingos", Range(0, 0.3)) = 0.12
        _SpikeCount  ("Quantidade de Respingos", Range(4, 24)) = 10
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha One
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
                float4 color      : COLOR; // Color over Lifetime chega aqui (inclusive alpha)
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            float4 _CoreColor;
            float4 _RingColor;
            float  _RingRadius;
            float  _RingWidth;
            float  _CoreSize;
            float  _SpikeAmount;
            float  _SpikeCount;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            float hash11(float p)
            {
                return frac(sin(p * 127.1) * 43758.5453123);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 centered = IN.uv - 0.5;
                float dist = length(centered) * 2.0; // 0 no centro, 1 na borda do quad
                float angle = atan2(centered.y, centered.x);

                // distorce a distancia com base no angulo -> borda do anel
                // vira "respingada" (pontas de gotinha), nao um circulo perfeito
                float spikeIndex = floor((angle + 3.14159) / (6.28318 / _SpikeCount));
                float spikeRandom = hash11(spikeIndex) - 0.5;
                float distJagged = dist + spikeRandom * _SpikeAmount;

                // anel de ondulacao (ripple)
                float ringOuter = smoothstep(_RingRadius + _RingWidth, _RingRadius, distJagged);
                float ringInner = smoothstep(_RingRadius - _RingWidth, _RingRadius, distJagged);
                float ring = saturate(ringOuter - (1.0 - ringInner));
                ring = smoothstep(_RingRadius - _RingWidth, _RingRadius, distJagged) *
                       smoothstep(_RingRadius + _RingWidth, _RingRadius, distJagged);

                // nucleo central (flash do impacto)
                float core = 1.0 - smoothstep(0.0, _CoreSize, dist);

                float shapeAlpha = saturate(ring + core);

                float3 col = lerp(_RingColor.rgb, _CoreColor.rgb, core);

                // multiplica pela vertex color -> Color over Lifetime controla
                // o fade, e Size over Lifetime controla o crescimento do splash
                float alpha = shapeAlpha * IN.color.a;

                return half4(col * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
