Shader "Custom/ProceduralFireTorch"
{

    Properties
    {
        _ColorBottom ("Cor da Base", Color) = (1, 0.9, 0.3, 1)
        _ColorTop ("Cor da Ponta", Color) = (1, 0.15, 0, 1)
        _NoiseScale ("Escala do Ruido", Float) = 3.0
        _Speed ("Velocidade de Subida", Float) = 1.5
        _FlameHeight ("Altura da Chama (0-1)", Range(0.2, 1.0)) = 0.9
        _FlameWidth ("Largura da Base (0-1)", Range(0.05, 0.5)) = 0.28
        _Flicker ("Intensidade do Tremular", Range(0, 0.5)) = 0.15
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
            float4 _ColorTop;
            float  _NoiseScale;
            float  _Speed;
            float  _FlameHeight;
            float  _FlameWidth;
            float  _Flicker;

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

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv; // uv.y = 0 na base, 1 no topo
                float t = _Time.y * _Speed;

                // ruido subindo com o tempo -> movimento vertical de chama
                float n = noise(float2(uv.x * _NoiseScale, uv.y * _NoiseScale * 2.0 - t * 3.0));
                n += noise(float2(uv.x * _NoiseScale * 2.0, uv.y * _NoiseScale * 4.0 - t * 5.0)) * 0.5;

                // afunila a chama conforme sobe (fica comprida e fina pra cima)
                float taper = pow(saturate(uv.y), 1.6);
                float width = _FlameWidth * (1.0 - taper);
                float dist = abs(uv.x - 0.5);

                // distorce a silhueta com o ruido pra parecer chama de verdade
                float edge = width + n * 0.08 * (1.0 - uv.y * 0.5);
                float mask = smoothstep(edge, edge - 0.05, dist);

                // desaparece suavemente perto do topo
                float heightMask = smoothstep(_FlameHeight, _FlameHeight * 0.4, uv.y);
                mask *= heightMask;

                // tremular geral (flicker), reagindo ao tempo
                float flicker = 1.0 + (hash(float2(floor(t * 12.0), 0.0)) - 0.5) * _Flicker;
                mask *= flicker;

                // gradiente de cor: base quente (amarelo) -> ponta (vermelho/laranja)
                float3 col = lerp(_ColorBottom.rgb, _ColorTop.rgb, saturate(uv.y * 1.3));
                col += n * 0.15;

                float alpha = saturate(mask);
                return half4(col * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
