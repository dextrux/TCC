Shader "Hidden/LightningFlashPost"
{
    Properties
    {
        [Header(Cor do Flash)]
        _FlashColor ("Cor do Flash", Color) = (0.9, 0.93, 1.0, 1)
        _FlashIntensity ("Intensidade Maxima do Flash (0-1)", Range(0, 1)) = 0.35

        [Header(Timing)]
        _StrikeInterval ("Intervalo Medio Entre Raios (segundos)", Range(0.2, 30)) = 6
        _IntervalRandom ("Variacao Aleatoria do Intervalo (0-1)", Range(0, 1)) = 0.6
        _FlashDuration  ("Duracao de Cada Flash (segundos)", Range(0.05, 1)) = 0.25
        _DoubleFlashChance ("Chance de Flash Duplo (0-1)", Range(0, 1)) = 0.3
        _Seed ("Seed (mesmo valor do material do Raio)", Float) = 0

        [Header(Sincronia com a Descida do Raio)]
        _StrikeDelay ("Atraso Antes do Flash (segundos)", Range(0, 1)) = 0.025

        [Header(Suavidade)]
        _FlashSoftness ("Suaviza a Curva do Flash (1 = padrao)", Range(0.3, 3)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "LightningFlash"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_BlitTexture);

            float4 _FlashColor;
            float  _FlashIntensity;

            float  _StrikeInterval;
            float  _IntervalRandom;
            float  _FlashDuration;
            float  _DoubleFlashChance;
            float  _Seed;

            float  _StrikeDelay;
            float  _FlashSoftness;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float hash11(float p)
            {
                return frac(sin(p * 127.1) * 43758.5453123);
            }

            float strikeEnvelope(float time)
            {
                float baseSeed = _Seed * 133.7;

                float cycleIndex = floor(time / _StrikeInterval);
                float localTime = time - cycleIndex * _StrikeInterval;

                float strikeSeed = baseSeed + cycleIndex * 9.173;

                float jitter = hash11(strikeSeed + 1.0) * _IntervalRandom * (_StrikeInterval * 0.6);
                float flashStart = jitter + _StrikeDelay;

                float env1 = 0.0;
                float t1 = localTime - flashStart;
                if (t1 >= 0.0 && t1 < _FlashDuration)
                {
                    float t = t1 / max(_FlashDuration, 0.001);
                    env1 = pow(1.0 - t, 2.0 * _FlashSoftness);
                }

                float env2 = 0.0;
                float doubleRoll = hash11(strikeSeed + 50.0);
                if (doubleRoll < _DoubleFlashChance)
                {
                    float secondStart = flashStart + _FlashDuration * 1.6;
                    float t2 = localTime - secondStart;
                    if (t2 >= 0.0 && t2 < _FlashDuration * 0.7)
                    {
                        float tn = t2 / max(_FlashDuration * 0.7, 0.001);
                        env2 = pow(1.0 - tn, 2.0 * _FlashSoftness) * 0.8;
                    }
                }

                return saturate(env1 + env2);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float envelope = strikeEnvelope(_Time.y);
                float amount = envelope * _FlashIntensity;

                // clareia a cena existente em direcao a cor do flash
                half3 result = lerp(sceneColor.rgb, _FlashColor.rgb, amount);

                return half4(result, sceneColor.a);
            }
            ENDHLSL
        }
    }
}