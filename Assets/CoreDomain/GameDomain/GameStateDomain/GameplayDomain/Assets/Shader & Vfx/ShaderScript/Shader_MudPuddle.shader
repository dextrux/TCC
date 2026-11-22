Shader "Custom/MudPuddle"
{
    Properties
    {
        [Header(Cores)]
        _MudColor   ("Cor da Lama (centro)", Color) = (0.16, 0.11, 0.06, 1)
        _EdgeColor  ("Cor da Borda (lama mais seca)", Color) = (0.30, 0.22, 0.13, 1)
        _WaterTint  ("Cor da Agua por Cima", Color) = (0.09, 0.07, 0.05, 1)

        [Header(Formato da Poca)]
        _Coverage           ("Tamanho da Poca", Range(0.05, 1)) = 0.55
        _EdgeSoftness       ("Suavidade da Borda", Range(0.001, 0.3)) = 0.05
        _ShapeNoiseScale    ("Escala do Ruido do Formato", Float) = 3
        _ShapeNoiseStrength ("Irregularidade do Formato", Range(0, 1)) = 0.45
        _Seed               ("Semente (muda o formato)", Float) = 0

        [Header(Agua)]
        _WaterAmount   ("Quantidade de Agua no Centro", Range(0, 1)) = 0.6
        _WetSmoothness ("Brilho da Agua", Range(0, 1)) = 0.92
        _MudSmoothness ("Brilho da Lama", Range(0, 1)) = 0.3

        [Header(Relevo da Lama)]
        _BumpScale    ("Escala do Relevo", Float) = 18
        _BumpStrength ("Forca do Relevo", Range(0, 2)) = 0.6

        [Header(Ondulacao da Agua)]
        _RippleSpeed    ("Velocidade", Float) = 0.4
        _RippleStrength ("Forca", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent-10"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

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
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float4 tangentWS  : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
            };

            float4 _MudColor, _EdgeColor, _WaterTint;
            float  _Coverage, _EdgeSoftness, _ShapeNoiseScale, _ShapeNoiseStrength, _Seed;
            float  _WaterAmount, _WetSmoothness, _MudSmoothness;
            float  _BumpScale, _BumpStrength;
            float  _RippleSpeed, _RippleStrength;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrm = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = nrm.normalWS;
                OUT.tangentWS  = float4(nrm.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.uv         = IN.uv;
                OUT.fogFactor  = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            // ---------- ruido ----------
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

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 4; k++)
                {
                    v += a * vnoise(p);
                    p = p * 2.03 + 17.1;
                    a *= 0.5;
                }
                return v;
            }

            // altura do relevo: lama granulada fora da agua, ondinhas dentro da agua
            float HeightAt(float2 uv, float water, float t)
            {
                float mudH = fbm(uv * _BumpScale) * _BumpStrength * (1.0 - water);
                float r1 = vnoise(uv * 40.0 + t * float2(1.0, 0.3));
                float r2 = vnoise(uv * 53.0 - t * float2(0.4, 1.0));
                float rippleH = (r1 + r2) * _RippleStrength * water;
                return mudH + rippleH;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // cada poca tem formato diferente: semente + posicao do objeto no mundo
                float3 objPos = TransformObjectToWorld(float3(0, 0, 0));
                float2 seedOffset = float2(_Seed * 13.7, _Seed * 7.3) + objPos.xz * 3.1;

                // ---------- formato ----------
                float2 c = uv * 2.0 - 1.0;
                float  r = length(c);
                float  n = fbm(uv * _ShapeNoiseScale + seedOffset);
                float  shape = (1.0 - r) + (n - 0.5) * _ShapeNoiseStrength;

                float threshold = 1.0 - _Coverage;
                float mask = smoothstep(threshold - _EdgeSoftness, threshold + _EdgeSoftness, shape);
                mask *= smoothstep(1.0, 0.85, r); // nunca encosta na borda do quad

                if (mask <= 0.001) discard;

                // 0 na borda, 1 no centro
                float depth = saturate((shape - threshold) / max(1.0 - threshold, 0.0001));

                // agua acumulada no centro, com contorno irregular
                float waterNoise = fbm(uv * _ShapeNoiseScale * 2.0 + seedOffset + 5.3);
                float water = smoothstep(1.0 - _WaterAmount, 1.0 - _WaterAmount + 0.25, depth + (waterNoise - 0.5) * 0.3);
                water *= step(0.001, _WaterAmount);

                // ---------- cor ----------
                float3 albedo = lerp(_EdgeColor.rgb, _MudColor.rgb, smoothstep(0.0, 0.35, depth));
                albedo = lerp(albedo, _WaterTint.rgb, water * 0.7);

                // ---------- normal ----------
                float t = _Time.y * _RippleSpeed;
                float e = 0.003;
                float h  = HeightAt(uv, water, t);
                float hx = HeightAt(uv + float2(e, 0), water, t);
                float hy = HeightAt(uv + float2(0, e), water, t);
                float3 normalTS = normalize(float3(-(hx - h) / e * 0.03, -(hy - h) / e * 0.03, 1.0));

                float3 normalWS  = normalize(IN.normalWS);
                float3 tangentWS = normalize(IN.tangentWS.xyz);
                float3 bitangent = cross(normalWS, tangentWS) * IN.tangentWS.w;
                float3 N = normalize(TransformTangentToWorld(normalTS, half3x3(tangentWS, bitangent, normalWS)));

                // ---------- luz ----------
                float3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float atten = light.shadowAttenuation * light.distanceAttenuation;

                float NdotL = saturate(dot(N, light.direction));
                float3 diffuse = albedo * (light.color * NdotL * atten + SampleSH(N));

                float smoothness = lerp(_MudSmoothness, _WetSmoothness, water);
                float specPower  = exp2(10.0 * smoothness + 1.0);
                float3 H = normalize(light.direction + V);
                float spec = pow(saturate(dot(N, H)), specPower) * smoothness * smoothness * 2.0;
                float3 specular = light.color * spec * atten;

                // reflexo do ceu/ambiente na agua
                float fresnel = 0.04 + 0.96 * pow(1.0 - saturate(dot(N, V)), 5.0);
                float3 reflection = GlossyEnvironmentReflection(reflect(-V, N), 1.0 - smoothness, 1.0);
                float3 color = diffuse + specular + reflection * fresnel * lerp(0.15, 1.0, water);

                color = MixFog(color, IN.fogFactor);
                return half4(color, mask);
            }
            ENDHLSL
        }
    }
}
