Shader "Custom/Lightning"
{

    Properties
    {
        [Header(Visual)]
        _BoltColor   ("Cor do Raio", Color) = (0.85, 0.9, 1, 1)
        _GlowColor   ("Cor do Brilho ao Redor", Color) = (0.5, 0.6, 1, 1)
        _BoltWidth   ("Largura do Raio", Range(0.005, 0.08)) = 0.02
        _GlowWidth   ("Largura do Brilho", Range(0.05, 0.5)) = 0.15
        _Branches    ("Quantidade de Ramificacoes", Range(0, 6)) = 3
        _Intensity   ("Intensidade Geral (brilho)", Range(0.2, 5)) = 1.5

        [Header(Timing)]
        _StrikeInterval ("Intervalo Medio Entre Raios (segundos)", Range(0.2, 30)) = 6
        _IntervalRandom ("Variacao Aleatoria do Intervalo (0-1)", Range(0, 1)) = 0.6
        _FlashDuration  ("Duracao de Cada Flash (segundos)", Range(0.05, 1)) = 0.25
        _DoubleFlashChance ("Chance de Flash Duplo (0-1)", Range(0, 1)) = 0.3

        [Header(Descida do Raio)]
        _StrikeSpeed ("Velocidade de Descida (topo para base)", Range(1, 200)) = 40
        _TravelSoftness ("Suavidade da Ponta", Range(0.001, 0.2)) = 0.04

        [Header(Flicker)]
        _FlickerAmount ("Intensidade do Tremular", Range(0, 1)) = 0.4
        _FlickerSpeed  ("Velocidade do Tremular", Range(1, 60)) = 25

        [Header(Outros)]
        _Seed ("Offset de Seed (varia entre varios relampagos)", Float) = 0
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
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            float4 _BoltColor;
            float4 _GlowColor;
            float  _BoltWidth;
            float  _GlowWidth;
            float  _Branches;
            float  _Intensity;

            float  _StrikeInterval;
            float  _IntervalRandom;
            float  _FlashDuration;
            float  _DoubleFlashChance;

            float  _StrikeSpeed;
            float  _TravelSoftness;

            float  _FlickerAmount;
            float  _FlickerSpeed;

            float  _Seed;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float hash11(float p)
            {
                return frac(sin(p * 127.1) * 43758.5453123);
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float boltPathX(float y, float seed, float freq, float amp)
            {
                float n  = hash21(float2(floor(y * freq), seed));
                float n2 = hash21(float2(floor(y * freq) + 1.0, seed));
                float t = frac(y * freq);
                float wobble = lerp(n, n2, t) - 0.5;
                return wobble * amp;
            }

            float drawBolt(float2 uv, float xOffset, float seed, float width)
            {
                float pathX = 0.5 + xOffset + boltPathX(uv.y, seed, 9.0, 0.25)
                                              + boltPathX(uv.y, seed + 3.7, 20.0, 0.08);
                float d = abs(uv.x - pathX);
                return smoothstep(width, width * 0.15, d);
            }

            // descer, 1 = ja desceu ate a base.
            float strikeEnvelope(float time, out float strikeSeed, out float growth)
            {
                float baseSeed = _Seed * 133.7;

                float cycleIndex = floor(time / _StrikeInterval);
                float localTime = time - cycleIndex * _StrikeInterval;

                strikeSeed = baseSeed + cycleIndex * 9.173;

                // desloca o inicio do flash dentro do ciclo, de forma aleatoria 
                // sem precisar acumular ciclos de tamanho diferente
                float jitter = hash11(strikeSeed + 1.0) * _IntervalRandom * (_StrikeInterval * 0.6);
                float flashStart = jitter;

                float env1 = 0.0;
                float g1 = 0.0;
                float t1 = localTime - flashStart;
                if (t1 >= 0.0 && t1 < _FlashDuration)
                {
                    float t = t1 / max(_FlashDuration, 0.001);
                    env1 = (1.0 - t) * (1.0 - t);
                }
                if (t1 >= 0.0)
                {
                    g1 = saturate(t1 * _StrikeSpeed);
                }

                float env2 = 0.0;
                float g2 = 0.0;
                float doubleRoll = hash11(strikeSeed + 50.0);
                if (doubleRoll < _DoubleFlashChance)
                {
                    float secondStart = flashStart + _FlashDuration * 1.6;
                    float t2 = localTime - secondStart;
                    if (t2 >= 0.0 && t2 < _FlashDuration * 0.7)
                    {
                        float tn = t2 / max(_FlashDuration * 0.7, 0.001);
                        env2 = (1.0 - tn) * (1.0 - tn) * 0.8;
                    }
                    if (t2 >= 0.0)
                    {
                        g2 = saturate(t2 * _StrikeSpeed);
                    }
                }

                // usa o crescimento do flash que estiver mais "ativo" no momento
                growth = (env1 >= env2) ? g1 : g2;

                return saturate(env1 + env2);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y;

                float strikeSeed;
                float growth;
                float envelope = strikeEnvelope(time, strikeSeed, growth);

                float flickerNoise = hash11(floor(time * _FlickerSpeed) + strikeSeed * 7.0);
                float flicker = lerp(1.0, flickerNoise, _FlickerAmount);
                envelope *= flicker;

                if (envelope <= 0.001)
                {
                    return half4(0, 0, 0, 0);
                }

                float2 uv = IN.uv;

                // frente da descida: comeca no topo (uv.y = 1) e desce para a base (uv.y = 0)
                float strikeFrontY = 1.0 - growth;
                float travelMask = smoothstep(strikeFrontY - _TravelSoftness, strikeFrontY + _TravelSoftness, uv.y);

                float bolt = drawBolt(uv, 0.0, strikeSeed, _BoltWidth) * travelMask;
                float glow = drawBolt(uv, 0.0, strikeSeed, _GlowWidth) * 0.5 * travelMask;

                [unroll]
                for (int i = 0; i < 6; i++)
                {
                    if (float(i) >= _Branches) break;

                    float fi = float(i);
                    float branchSeed = strikeSeed + fi * 17.13 + 5.0;
                    float startY = 0.3 + hash11(branchSeed) * 0.4;
                    float side = hash11(branchSeed + 1.0) > 0.5 ? 1.0 : -1.0;
                    float offset = side * (0.05 + hash11(branchSeed + 2.0) * 0.15);

                    float branchMask = smoothstep(startY, startY - 0.05, uv.y) *
                                        smoothstep(0.0, 0.1, uv.y);

                    float branchBolt = drawBolt(uv, offset, branchSeed, _BoltWidth * 0.6) * branchMask * travelMask;
                    float branchGlow = drawBolt(uv, offset, branchSeed, _GlowWidth * 0.7) * 0.4 * branchMask * travelMask;

                    bolt = max(bolt, branchBolt);
                    glow = max(glow, branchGlow);
                }

                float3 col = (_BoltColor.rgb * bolt + _GlowColor.rgb * glow) * _Intensity;
                float alpha = saturate(bolt + glow) * envelope;

                return half4(col * envelope, alpha);
            }
            ENDHLSL
        }
    }
}