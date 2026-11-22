Shader "Custom/MudSplat"
{
    // Mancha de lama "esparramada" (splat) desenhada num quad colado na superficie.
    // O eixo Y do quad aponta na direcao em que a bola estava indo: os respingos vao mais pra frente.
    Properties
    {
        [Header(Cor)]
        _MudColor   ("Cor da Lama Molhada", Color) = (0.17, 0.11, 0.06, 1)
        _DryColor   ("Cor da Lama Secando (ao sumir)", Color) = (0.33, 0.25, 0.16, 1)
        _Smoothness ("Brilho da Lama Molhada", Range(0, 1)) = 0.7

        [Header(Formato)]
        _BlobSize     ("Tamanho da Mancha Central", Range(0.1, 0.7)) = 0.36
        _Irregularity ("Irregularidade da Borda", Range(0, 1)) = 0.55
        _Streaks      ("Quantidade de Respingos Longos", Range(0, 12)) = 7
        _Droplets     ("Quantidade de Gotas Soltas", Range(0, 24)) = 14
        _Forward      ("Respingos pra Frente (direcao do arremesso)", Range(0, 1)) = 0.55

        [Header(Volume)]
        _BumpStrength ("Volume da Lama", Range(0, 3)) = 1.2

        // controlados pelo script MudSplat
        [HideInInspector] _Seed ("Seed", Float) = 0
        [HideInInspector] _Fade ("Fade", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent-5"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            float4 _MudColor, _DryColor;
            float  _Smoothness;
            float  _BlobSize, _Irregularity, _Streaks, _Droplets, _Forward;
            float  _BumpStrength;
            float  _Seed, _Fade;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = IN.uv;
                OUT.fogFactor  = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            // ---------- ruido ----------
            float hash11(float p)
            {
                return frac(sin(p * 127.1 + 31.7) * 43758.5453123);
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // uniao suave (as gotas "grudam" na mancha em vez de so encostar)
            float smax(float a, float b, float k)
            {
                float h = saturate(0.5 + 0.5 * (a - b) / k);
                return lerp(b, a, h) + k * h * (1.0 - h);
            }

            // angulo aleatorio, puxado pra frente (+Y do quad) conforme _Forward
            float biasedAngle(float h)
            {
                float freeAngle    = h * TWO_PI;
                float forwardAngle = HALF_PI + (h - 0.5) * PI * 1.1;
                return lerp(freeAngle, forwardAngle, _Forward);
            }

            // "campo" da mancha: > 0 dentro da lama, < 0 fora. Quanto maior, mais grossa a lama.
            float splatField(float2 p, float seed)
            {
                float r   = length(p);
                float ang = atan2(p.y, p.x);

                // mancha central com borda irregular (ruido ao longo do circulo, sem emenda)
                float edgeN = vnoise(float2(cos(ang), sin(ang)) * 2.2 + seed * 3.7);
                float blobR = _BlobSize * (1.0 + (edgeN - 0.5) * _Irregularity);
                float field = blobR - r;

                // respingos longos: "lagrimas" saindo do centro, finas na ponta, com gota no fim
                [loop]
                for (int j = 0; j < 12; j++)
                {
                    if (j >= (int)_Streaks) break;
                    float fj = j + seed * 11.0;
                    float h1 = hash11(fj * 3.1);
                    float h2 = hash11(fj * 5.7 + 1.3);
                    float h3 = hash11(fj * 7.9 + 2.1);

                    float  a   = biasedAngle(h1);
                    float2 dir = float2(cos(a), sin(a));
                    float  len = min(_BlobSize * (1.1 + h2 * 1.4), 0.85);
                    float  w   = 0.025 + h3 * 0.04;

                    float t     = saturate(dot(p, dir) / len);
                    float dist  = length(p - dir * len * t);
                    float width = lerp(w * 1.7, w * 0.45, t);
                    field = smax(field, width - dist, 0.03);

                    float2 tip = dir * (len + w * 1.6);
                    field = smax(field, w * 1.2 - length(p - tip), 0.02);
                }

                // gotas soltas em volta
                [loop]
                for (int k = 0; k < 24; k++)
                {
                    if (k >= (int)_Droplets) break;
                    float fk = k + seed * 17.0 + 50.0;
                    float h1 = hash11(fk * 2.3);
                    float h2 = hash11(fk * 4.1 + 0.7);
                    float h3 = hash11(fk * 6.3 + 1.9);

                    float  a   = biasedAngle(h1);
                    float  d   = min(_BlobSize * 1.25 + h2 * 0.5, 0.9);
                    float2 c   = float2(cos(a), sin(a)) * d;
                    float  rad = 0.012 + h3 * 0.035 * (1.0 - d * 0.6);
                    field = max(field, rad - length(p - c));
                }

                // granulado da borda
                field += (vnoise(p * 14.0 + seed * 9.1) - 0.5) * 0.025 * _Irregularity;
                return field;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.uv * 2.0 - 1.0;
                float seed = _Seed;

                float field = splatField(p, seed);

                // ao sumir, a mancha "seca" e encolhe pelas bordas
                float shrink = (1.0 - saturate(_Fade)) * 0.07;
                float f = field - shrink;

                float aa = max(fwidth(f), 0.0005);
                float mask = smoothstep(-aa, aa, f);
                mask *= smoothstep(1.0, 0.94, max(abs(p.x), abs(p.y))); // nunca corta na borda do quad
                mask *= smoothstep(0.0, 0.25, _Fade);

                // ---------- volume (relevo pela "espessura" da lama) ----------
                float thick = smoothstep(0.0, 0.14, f);
                float h = thick * _BumpStrength * 0.02;

                float3 N = normalize(IN.normalWS);
                float3 dpdx = ddx(IN.positionWS);
                float3 dpdy = ddy(IN.positionWS);
                float dhdx = ddx(h);
                float dhdy = ddy(h);
                float3 r1 = cross(dpdy, N);
                float3 r2 = cross(N, dpdx);
                float det = dot(dpdx, r1);
                float3 grad = sign(det) * (dhdx * r1 + dhdy * r2);
                N = normalize(abs(det) * N - grad);

                // ---------- cor ----------
                float3 wet = _MudColor.rgb * lerp(1.25, 0.8, thick); // mais escura onde e grossa
                float3 albedo = lerp(_DryColor.rgb, wet, saturate(_Fade * 1.3));
                float smoothness = _Smoothness * saturate(_Fade * 1.2) * lerp(0.6, 1.0, thick);

                // ---------- luz ----------
                float3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float atten = light.shadowAttenuation * light.distanceAttenuation;

                float NdotL = saturate(dot(N, light.direction));
                float3 diffuse = albedo * (light.color * NdotL * atten + SampleSH(N));

                float specPower = exp2(10.0 * smoothness + 1.0);
                float3 H = normalize(light.direction + V);
                float spec = pow(saturate(dot(N, H)), specPower) * smoothness * smoothness * 1.5;

                float fresnel = 0.04 + 0.96 * pow(1.0 - saturate(dot(N, V)), 5.0);
                float3 refl = GlossyEnvironmentReflection(reflect(-V, N), 1.0 - smoothness, 1.0);

                float3 color = diffuse + light.color * spec * atten + refl * fresnel * smoothness * 0.5;
                color = MixFog(color, IN.fogFactor);

                // descarta so no fim, pra nao atrapalhar as derivadas (ddx/ddy) do relevo
                clip(mask - 0.001);
                return half4(color, mask);
            }
            ENDHLSL
        }
    }
}
