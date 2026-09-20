Shader "Custom/RunDust"
{
    Properties
    {
        _DustColor   ("Cor da Poeira/Terra", Color) = (0.55, 0.42, 0.28, 1)
        _NoiseScale  ("Escala do Ruido", Float) = 4.0
        _RiseSpeed   ("Velocidade de Subida do Ruido", Float) = 1.2
        _EdgeSoftness("Suavidade da Borda", Range(0.01, 1)) = 0.5
        _Density     ("Densidade", Range(0, 1)) = 0.6
        _SoftFadeDistance ("Fade contra Geometria (metros)", Range(0.01, 2)) = 0.3
        _Seed ("Offset de Seed", Float) = 0
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : TEXCOORD1;
                float4 screenPos   : TEXCOORD2;
                float  eyeDepth    : TEXCOORD3;
            };

            float4 _DustColor;
            float  _NoiseScale;
            float  _RiseSpeed;
            float  _EdgeSoftness;
            float  _Density;
            float  _SoftFadeDistance;
            float  _Seed;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float4 posWS = mul(unity_ObjectToWorld, IN.positionOS);
                OUT.positionHCS = TransformWorldToHClip(posWS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                OUT.eyeDepth = -TransformWorldToView(posWS.xyz).z;
                return OUT;
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            float fbm(float2 p)
            {
                float total = 0.0, amp = 0.5, freq = 1.0;
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    total += noise(p * freq) * amp;
                    freq *= 2.0;
                    amp *= 0.5;
                }
                return total;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y * _RiseSpeed;
                float2 uv = IN.uv - 0.5;

                // formato circular base (a "nuvem" de poeira)
                float radialDist = length(uv) * 2.0;
                float shape = smoothstep(1.0, 1.0 - _EdgeSoftness, radialDist);

                // ruido erosivo pra parecer poeira irregular, nao um circulo perfeito
                float n = fbm(uv * _NoiseScale + float2(_Seed, t));
                shape *= smoothstep(0.15, 0.75, n);

                float alpha = shape * _Density * IN.color.a;

                // soft particle contra o chao/geometria
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(IN.screenPos.xy / IN.screenPos.w), _ZBufferParams);
                float depthDiff = sceneDepth - IN.eyeDepth;
                alpha *= saturate(depthDiff / max(_SoftFadeDistance, 0.001));

                return half4(_DustColor.rgb * alpha, alpha);
            }
            ENDHLSL
        }
    }
}