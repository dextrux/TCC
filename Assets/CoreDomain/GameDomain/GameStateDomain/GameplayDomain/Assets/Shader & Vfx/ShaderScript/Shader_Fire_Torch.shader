Shader "Custom/TorchFire3D"
{
    Properties
    {
        _ColorBottom ("Cor da Base", Color) = (1, 0.95, 0.4, 1)
        _ColorMid    ("Cor do Meio", Color) = (1, 0.5, 0.05, 1)
        _ColorTop    ("Cor da Ponta", Color) = (0.9, 0.1, 0.0, 1)
        _NoiseScale  ("Escala do Ruido", Float) = 3.2
        _Speed       ("Velocidade de Subida", Float) = 1.6
        _FlameHeight ("Altura da Chama (0-1)", Range(0.2, 1.0)) = 0.95
        _FlameWidth  ("Largura da Base (0-1)", Range(0.05, 0.5)) = 0.24
        _EdgeNoise   ("Irregularidade da Borda", Range(0, 0.5)) = 0.22
        _Turbulence  ("Turbulencia (camadas de ruido)", Range(0, 1)) = 0.6
        _WaveAmount  ("Ondulacao Horizontal", Range(0, 0.3)) = 0.1
        _Flicker     ("Intensidade do Tremular", Range(0, 0.5)) = 0.18
        _PhaseOffset ("Offset de Fase (varia por plano)", Range(0, 1)) = 0.0
        _Brightness  ("Brilho Extra", Range(0.5, 3.0)) = 1.4
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

            float4 _ColorBottom;
            float4 _ColorMid;
            float4 _ColorTop;
            float  _NoiseScale;
            float  _Speed;
            float  _FlameHeight;
            float  _FlameWidth;
            float  _EdgeNoise;
            float  _Turbulence;
            float  _WaveAmount;
            float  _Flicker;
            float  _PhaseOffset;
            float  _Brightness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
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

            // fbm: soma varias camadas de ruido em escalas diferentes,
            // isso e o que quebra o visual "liso/triangular"
            float fbm(float2 p)
            {
                float total = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    total += noise(p * freq) * amp;
                    freq *= 2.15;
                    amp *= 0.55;
                }
                return total;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv; // uv.y = 0 na base, 1 no topo
                float t = _Time.y * _Speed + _PhaseOffset * 100.0;

                // fbm subindo com o tempo -> movimento vertical turbulento
                float2 flowUV = float2(uv.x * _NoiseScale, uv.y * _NoiseScale * 1.8 - t * 3.0);
                float n = fbm(flowUV);
                float turbulence = fbm(flowUV * 1.7 + float2(t * 0.5, -t * 1.3)) * _Turbulence;
                n = lerp(n, n + turbulence, _Turbulence);

                // ondulacao horizontal: desloca o centro da chama em ondas,
                // diferente em cada altura -> quebra a simetria "certinha"
                float wave = sin(uv.y * 9.0 + t * 2.0) * _WaveAmount * uv.y;
                wave += (fbm(float2(uv.y * 4.0 - t * 1.5, _PhaseOffset * 10.0)) - 0.5) * _WaveAmount;

                // afunila a chama conforme sobe, mas o afunilamento tambem
                // "range" com ruido, em vez de ser uma curva perfeitamente lisa
                float taperNoise = (fbm(float2(uv.y * 3.0 - t * 0.8, _PhaseOffset * 5.0)) - 0.5) * 0.15;
                float taper = pow(saturate(uv.y + taperNoise), 1.6);
                float width = _FlameWidth * (1.0 - taper);

                float dist = abs((uv.x - 0.5) - wave);

                // borda irregular: soma ruido de alta frequencia na borda
                float edgeDetail = (n - 0.5) * _EdgeNoise * (1.0 - uv.y * 0.3);
                float edge = width + edgeDetail;
                float mask = smoothstep(edge, edge - 0.04, dist);

                // desaparece perto do topo, tambem com uma variacao (nao corta reto)
                float topNoise = fbm(float2(uv.x * 5.0, t * 1.2)) * 0.1;
                float heightMask = smoothstep(_FlameHeight + topNoise, _FlameHeight * 0.35, uv.y);
                mask *= heightMask;

                // tremular geral (flicker), ligado ao tempo/fase de cada plano
                float flicker = 1.0 + (hash(float2(floor(t * 12.0), _PhaseOffset)) - 0.5) * _Flicker;
                mask *= flicker;

                // gradiente de 3 cores: base -> meio -> ponta
                float3 col = lerp(_ColorBottom.rgb, _ColorMid.rgb, saturate(uv.y * 2.0));
                col = lerp(col, _ColorTop.rgb, saturate((uv.y - 0.5) * 2.0));
                col += n * 0.18;
                col *= _Brightness;

                float alpha = saturate(mask);
                return half4(col * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
