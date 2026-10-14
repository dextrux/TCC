Shader "Custom/MudMarks"
{
    Properties
    {
        [Header(Base)]
        _MainTex ("Textura do Personagem (Albedo)", 2D) = "white" {}
        _Color   ("Tint", Color) = (1, 1, 1, 1)

        [Header(Lama)]
        _MudColor    ("Cor da Lama", Color) = (0.18, 0.12, 0.07, 1)
        _MudRoughness("Aspecto Fosco da Lama (0 = brilhante, 1 = fosca)", Range(0, 1)) = 0.9

        [Header(Formato das Manchas)]
        _MudRadius   ("Raio Medio de Cada Mancha", Range(0.02, 0.6)) = 0.18
        _EdgeSoftness("Suavidade/Irregularidade da Borda", Range(0.01, 1)) = 0.5
        _NoiseScale  ("Escala do Ruido da Borda", Float) = 12.0

        [Header(Onde a Lama Gruda)]
        _DownwardBias ("Preferencia por Superficies Voltadas pra Baixo (pes/parte de baixo)", Range(0, 1)) = 0.6

        [Header(Tempo de Vida das Marcas)]
        _MudDecayTime ("Tempo Ate a Marca Sumir Totalmente (segundos)", Range(1, 120)) = 25
        _MudFadeCurve ("Curva do Desvanecimento (1 = linear, >1 = fica forte e some rapido no fim)", Range(0.3, 4)) = 1.6

        [Header(Luz)]
        _LightInfluence ("Influencia da Luz da Cena", Range(0, 1)) = 0.9
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define MAX_CONTACTS 12

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Color;
            float4 _MudColor;
            float  _MudRoughness;

            float _MudRadius;
            float _EdgeSoftness;
            float _NoiseScale;

            float _DownwardBias;

            float _MudDecayTime;
            float _MudFadeCurve;

            float _LightInfluence;

            // preenchido pelo script: xyz = posicao mundial do contato, w = tempo (_Time.y) em que aconteceu
            float4 _ContactPoints[MAX_CONTACTS];
            int    _ContactCount;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            float hash31(float3 p)
            {
                return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453123);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

                float3 normalWS = normalize(IN.normalWS);

                // favorece superficies voltadas pra baixo (sola do pe, parte de baixo da perna)
                float downFacing = saturate(-dot(normalWS, float3(0, 1, 0)));
                float downMask = lerp(1.0, downFacing, _DownwardBias);

                float mudAccum = 0.0;
                float now = _Time.y;

                for (int i = 0; i < MAX_CONTACTS; i++)
                {
                    if (i >= _ContactCount) break;

                    float3 contactPos = _ContactPoints[i].xyz;
                    float  contactTime = _ContactPoints[i].w;

                    float age = now - contactTime;
                    float lifeT = saturate(age / max(_MudDecayTime, 0.01));
                    float fade = pow(1.0 - lifeT, _MudFadeCurve);

                    if (fade <= 0.001) continue;

                    float dist = distance(IN.positionWS, contactPos);

                    // pra cada mancha ter um formato organico e diferente das outras
                    float edgeNoise = hash31(IN.positionWS * _NoiseScale + float3(0, 0, i * 17.31));
                    float radius = _MudRadius * (0.8 + 0.4 * hash31(contactPos + i));
                    float noisyRadius = radius * (1.0 + (edgeNoise - 0.5) * _EdgeSoftness);

                    float mark = 1.0 - smoothstep(noisyRadius * 0.5, noisyRadius, dist);
                    mark *= fade * downMask;

                    // contatos se somam (mais pisadas = mais lama acumulada, ate saturar)
                    mudAccum += mark;
                }

                mudAccum = saturate(mudAccum);

                float3 litAlbedo;
                Light mainLight = GetMainLight();
                float3 ambient = SampleSH(normalWS);

                float NdotL = saturate(dot(normalWS, mainLight.direction)) * 0.5 + 0.5;
                float3 lighting = mainLight.color * NdotL + ambient;

                float3 baseLit = lerp(tex.rgb, tex.rgb * lighting, _LightInfluence);

                float3 mudLit = lerp(_MudColor.rgb, _MudColor.rgb * lighting, _LightInfluence * (1.0 - _MudRoughness * 0.5));

                float3 finalColor = lerp(baseLit, mudLit, mudAccum);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}