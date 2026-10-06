Shader "Custom/MirageDistortion"
{
    Properties
    {
        _MainTex        ("Textura", 2D) = "white" {}
        _Color          ("Tingimento", Color) = (1, 1, 1, 1)
        _VertexAmount   ("Distorcao dos Vertices", Range(0, 1)) = 0.15
        _VertexSpeed    ("Velocidade da Ondulacao (vertices)", Float) = 1.0
        _VertexFreq     ("Frequencia da Ondulacao (vertices)", Float) = 2.0
        _UVAmount       ("Distorcao da Textura (UV)", Range(0, 0.1)) = 0.02
        _UVSpeed        ("Velocidade da Distorcao (UV)", Float) = 1.5
        _Transparency   ("Transparencia Extra (0 = normal)", Range(0, 0.9)) = 0.15
        _RimColor       ("Cor da Borda (efeito etereo)", Color) = (0.6, 0.8, 1, 1)
        _RimPower       ("Intensidade da Borda", Range(0.5, 8)) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
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
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;
            float4 _Color;
            float  _VertexAmount;
            float  _VertexSpeed;
            float  _VertexFreq;
            float  _UVAmount;
            float  _UVSpeed;
            float  _Transparency;
            float4 _RimColor;
            float  _RimPower;

            float hash11(float p)
            {
                return frac(sin(p * 127.1) * 43758.5453123);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionOS = IN.positionOS.xyz;

                // ondula o vertice com base na altura local (Y) e no tempo,
                // como se o objeto estivesse tremulando/nao totalmente solido
                float t = _Time.y * _VertexSpeed;
                float wave = sin(positionOS.y * _VertexFreq + t) * _VertexAmount;
                float wave2 = cos(positionOS.y * _VertexFreq * 1.7 - t * 1.3) * _VertexAmount * 0.6;

                positionOS.x += wave;
                positionOS.z += wave2;

                float3 positionWS = TransformObjectToWorld(positionOS);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(positionWS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y * _UVSpeed;

                // distorce o UV da textura em pequenas ondas -> a "casca"
                // da arvore parece tremular/nao ser totalmente real
                float2 distortedUV = IN.uv;
                distortedUV.x += sin(IN.uv.y * 12.0 + t) * _UVAmount;
                distortedUV.y += cos(IN.uv.x * 12.0 - t * 1.3) * _UVAmount;

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);
                half4 col = texColor * _Color;

                // efeito de borda (rim) etereo, tipo fantasma/ilusao
                float3 normal = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);
                float rim = 1.0 - saturate(dot(normal, viewDir));
                rim = pow(rim, _RimPower);
                col.rgb += _RimColor.rgb * rim;

                // transparencia extra fixa, pra parecer "nao totalmente solido"
                col.a *= (1.0 - _Transparency);

                return col;
            }
            ENDHLSL
        }
    }
}
