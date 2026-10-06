Shader "Custom/GroundFog"
{
    Properties
    {
        _FogColor       ("Cor da Nevoa", Color) = (0.85, 0.88, 0.92, 1)
        _NoiseScale     ("Escala do Ruido", Float) = 2.0
        _Speed1         ("Velocidade Camada 1", Vector) = (0.02, 0.01, 0, 0)
        _Speed2         ("Velocidade Camada 2", Vector) = (-0.015, 0.025, 0, 0)
        _Density        ("Densidade Geral", Range(0, 1)) = 0.5
        _EdgeFade       ("Fade nas Bordas do Quad", Range(0, 1)) = 0.4
        _LightInfluence ("Influencia da Luz", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK

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
                float3 positionWS  : TEXCOORD2;
            };

            float4 _FogColor;
            float  _NoiseScale;
            float4 _Speed1;
            float4 _Speed2;
            float  _Density;
            float  _EdgeFade;
            float  _LightInfluence;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
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
                float total = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    total += noise(p * freq) * amp;
                    freq *= 2.0;
                    amp *= 0.5;
                }
                return total;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y;

                float2 uv1 = IN.uv * _NoiseScale + _Speed1.xy * t;
                float2 uv2 = IN.uv * _NoiseScale * 1.6 + _Speed2.xy * t;

                float n1 = fbm(uv1);
                float n2 = fbm(uv2);
                float fog = (n1 * 0.6 + n2 * 0.4);

                float alpha = saturate(fog * _Density * 1.5);

                // fade suave nas bordas do quad
                float2 edgeDist = min(IN.uv, 1.0 - IN.uv);
                float edge = min(edgeDist.x, edgeDist.y);
                float edgeMask = smoothstep(0.0, _EdgeFade, edge);
                alpha *= edgeMask;

                // --- Influencia da luz da cena ---
                float3 normalWS = normalize(IN.normalWS);

                // Luz principal (direcional)
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction)) * 0.5 + 0.5; // suavizado, sem ficar preto do outro lado
                float3 mainLightContribution = mainLight.color * mainLight.distanceAttenuation * NdotL;

                // Luz ambiente (Spherical Harmonics / skybox)
                float3 ambient = SampleSH(normalWS);

                float3 sceneLight = mainLightContribution + ambient;

                // mistura a cor base da nevoa com a influencia da luz
                float3 litFogColor = lerp(_FogColor.rgb, _FogColor.rgb * sceneLight, _LightInfluence);

                return half4(litFogColor * alpha, alpha);
            }
            ENDHLSL
        }
    }
}