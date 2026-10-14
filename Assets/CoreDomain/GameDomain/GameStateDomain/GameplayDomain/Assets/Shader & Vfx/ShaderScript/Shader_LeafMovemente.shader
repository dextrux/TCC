Shader "Custom/LeafMovement"
{
    Properties
    {
        [Header(Textura)]
        _MainTex   ("Textura da Folha (com Alpha)", 2D) = "white" {}
        _Cutoff    ("Alpha Cutoff", Range(0, 1)) = 0.4
        _Color     ("Tint", Color) = (1, 1, 1, 1)

        [Header(Vento Balanco Grande)]
        _LeafCellSize ("Tamanho Aprox de Uma Folha (pra separar seeds)", Range(0.02, 2)) = 0.15
        _WindStrength ("Forca do Vento", Range(0, 1)) = 0.25
        _WindSpeed    ("Velocidade do Vento", Range(0, 5)) = 1.0
        _WindScale    ("Escala Espacial do Vento (dessincroniza folhas distantes)", Float) = 0.3

        [Header(Flutter Tremor Rapido)]
        _FlutterStrength ("Forca do Tremor", Range(0, 0.3)) = 0.06
        _FlutterSpeed    ("Velocidade do Tremor", Range(1, 20)) = 8.0

        [Header(Luz)]
        _LightInfluence ("Influencia da Luz da Cena", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        Cull Off

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Color;
            float  _Cutoff;

            float _LeafCellSize;
            float _WindStrength;
            float _WindSpeed;
            float _WindScale;

            float _FlutterStrength;
            float _FlutterSpeed;

            float _LightInfluence;

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 posOS = IN.positionOS.xyz;

                // peso do balanco: 0 na base da folha (uv.y baixo), 1 na ponta
                float bendWeight = saturate(IN.uv.y * IN.uv.y);

                // usa a posicao LOCAL do vertice, arredondada pro tamanho aprox. de uma
                // folha, entao vertices da MESMA folha caem na mesma "celula" e ganham a
                // MESMA fase entre si, mas DIFERENTE de folhas vizinhas -- funciona mesmo
                // se todas as folhas estiverem combinadas num unico mesh
                float3 cellId = floor(posOS / max(_LeafCellSize, 0.001));
                float phase = hash21(cellId.xz + cellId.y * 13.17) * 6.2831;

                float t = _Time.y;

                // balanco grande e lento (vento)
                float windWave = sin(t * _WindSpeed + phase) * 0.6
                                + sin(t * _WindSpeed * 0.6 + phase * 1.7) * 0.4;
                float3 windOffset = float3(windWave, 0, windWave * 0.5) * _WindStrength * bendWeight;

                // tremor rapido (flutter), soma uma variacao em outra direcao
                float flutter = sin(t * _FlutterSpeed + phase * 3.1) * _FlutterStrength * bendWeight;
                windOffset.y += flutter;
                windOffset.z += flutter * 0.5;

                posOS.xyz += windOffset;

                OUT.positionHCS = TransformObjectToHClip(posOS);
                OUT.uv = IN.uv;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;
                clip(tex.a - _Cutoff);

                float3 normalWS = normalize(IN.normalWS);

                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction)) * 0.5 + 0.5;
                float3 ambient = SampleSH(normalWS);
                float3 lighting = mainLight.color * NdotL + ambient;

                float3 litColor = lerp(tex.rgb, tex.rgb * lighting, _LightInfluence);

                return half4(litColor, 1.0);
            }
            ENDHLSL
        }
    }
}