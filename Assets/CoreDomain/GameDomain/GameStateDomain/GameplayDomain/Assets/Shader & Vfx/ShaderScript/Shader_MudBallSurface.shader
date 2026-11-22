Shader "Custom/MudBallSurface"
{
    // Bola de lama: superficie grumosa (deforma a esfera), granulado fino, partes molhadas brilhando
    // e um leve "mexer" da lama. Feito pra Sphere padrao da Unity.
    Properties
    {
        [Header(Cor)]
        _MudColor  ("Cor da Lama", Color) = (0.20, 0.13, 0.07, 1)
        _WetColor  ("Cor da Lama Molhada (nos buracos)", Color) = (0.11, 0.07, 0.04, 1)
        _DryColor  ("Cor dos Torroes Secos (nas pontas)", Color) = (0.34, 0.25, 0.16, 1)

        [Header(Brilho)]
        _WetSmoothness ("Brilho da Parte Molhada", Range(0, 1)) = 0.8
        _DrySmoothness ("Brilho da Parte Seca", Range(0, 1)) = 0.15
        _Wetness       ("Quantidade de Partes Molhadas", Range(0, 1)) = 0.55

        [Header(Formato Grumoso)]
        _LumpScale  ("Tamanho dos Grumos (maior = mais grumos)", Range(0.5, 8)) = 2.6
        _LumpAmount ("Forca dos Grumos", Range(0, 0.4)) = 0.14

        [Header(Granulado)]
        _DetailScale    ("Escala do Granulado", Range(5, 80)) = 28
        _DetailStrength ("Forca do Granulado", Range(0, 3)) = 1.0

        [Header(Lama Mexendo)]
        _WobbleSpeed  ("Velocidade", Range(0, 5)) = 1.2
        _WobbleAmount ("Forca", Range(0, 0.1)) = 0.025

        // muda o formato de cada bola (o MudBallThrower sorteia por bola)
        [HideInInspector] _Seed ("Seed", Float) = 0
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    float4 _MudColor, _WetColor, _DryColor;
    float  _WetSmoothness, _DrySmoothness, _Wetness;
    float  _LumpScale, _LumpAmount;
    float  _DetailScale, _DetailStrength;
    float  _WobbleSpeed, _WobbleAmount;
    float  _Seed;

    float hash31(float3 p)
    {
        p = frac(p * 0.3183099 + 0.1);
        p *= 17.0;
        return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
    }

    float vnoise3(float3 x)
    {
        float3 i = floor(x);
        float3 f = frac(x);
        f = f * f * (3.0 - 2.0 * f);
        return lerp(lerp(lerp(hash31(i + float3(0, 0, 0)), hash31(i + float3(1, 0, 0)), f.x),
                         lerp(hash31(i + float3(0, 1, 0)), hash31(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(hash31(i + float3(0, 0, 1)), hash31(i + float3(1, 0, 1)), f.x),
                         lerp(hash31(i + float3(0, 1, 1)), hash31(i + float3(1, 1, 1)), f.x), f.y), f.z);
    }

    float fbm3(float3 p)
    {
        float v = 0.0;
        float a = 0.5;
        for (int k = 0; k < 3; k++)
        {
            v += a * vnoise3(p);
            p = p * 2.07 + 13.1;
            a *= 0.5;
        }
        return v / 0.875;
    }

    float3 SeedOffset() { return float3(_Seed * 7.13, _Seed * 3.71, _Seed * 5.29); }

    // deslocamento (em fracao do raio) de um ponto da esfera na direcao dir
    float LumpHeight(float3 dir)
    {
        float3 so = SeedOffset();
        float lump = (fbm3(dir * _LumpScale + so) - 0.5) * 2.0 * _LumpAmount;
        float wob  = (vnoise3(dir * _LumpScale * 0.8 + so + float3(0, _Time.y * _WobbleSpeed, 0)) - 0.5) * 2.0 * _WobbleAmount;
        return lump + wob;
    }

    float3 DisplacedPoint(float3 dir, float radius)
    {
        return dir * radius * (1.0 + LumpHeight(dir));
    }

    // posicao e normal deformadas (em espaco do objeto)
    void DeformSphere(float3 positionOS, out float3 newPos, out float3 newNormal, out float height)
    {
        float radius = max(length(positionOS), 1e-5);
        float3 dir = positionOS / radius;

        float3 up = abs(dir.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
        float3 t1 = normalize(cross(dir, up));
        float3 t2 = cross(dir, t1);
        const float e = 0.02;

        float3 p0 = DisplacedPoint(dir, radius);
        float3 p1 = DisplacedPoint(normalize(dir + t1 * e), radius);
        float3 p2 = DisplacedPoint(normalize(dir + t2 * e), radius);

        newPos = p0;
        newNormal = normalize(cross(p1 - p0, p2 - p0));
        if (dot(newNormal, dir) < 0.0) newNormal = -newNormal;
        height = LumpHeight(dir);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float  height     : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 pos, nrm; float h;
                DeformSphere(IN.positionOS.xyz, pos, nrm, h);

                OUT.positionWS = TransformObjectToWorld(pos);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(nrm);
                OUT.positionOS = pos;
                OUT.height     = h;
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 so = SeedOffset();
                float3 pOS = IN.positionOS / max(length(IN.positionOS), 1e-5);

                // 0 = buraco (mais molhado/escuro), 1 = ponta do grumo (mais seco/claro)
                float cav = saturate(IN.height / max(_LumpAmount, 1e-3) * 0.5 + 0.5);

                // manchas molhadas: ruido + buracos
                float wetNoise = fbm3(pOS * 4.0 + so + 21.0);
                float wet = saturate(smoothstep(1.0 - _Wetness - 0.15, 1.0 - _Wetness + 0.15, wetNoise + (1.0 - cav) * 0.45));

                // ---------- granulado fino (relevo por derivadas, gira junto com a bola) ----------
                float grain = fbm3(pOS * _DetailScale + so * 3.0);
                float hDetail = grain * _DetailStrength * 0.004 * (1.0 - wet * 0.7); // lama molhada e mais lisa

                float3 N = normalize(IN.normalWS);
                float3 dpdx = ddx(IN.positionWS);
                float3 dpdy = ddy(IN.positionWS);
                float dhdx = ddx(hDetail);
                float dhdy = ddy(hDetail);
                float3 r1 = cross(dpdy, N);
                float3 r2 = cross(N, dpdx);
                float det = dot(dpdx, r1);
                float3 grad = sign(det) * (dhdx * r1 + dhdy * r2);
                N = normalize(abs(det) * N - grad);

                // ---------- cor ----------
                float3 albedo = lerp(_MudColor.rgb, _DryColor.rgb, smoothstep(0.55, 1.0, cav) * (1.0 - wet));
                albedo = lerp(albedo, _WetColor.rgb, wet * 0.8);
                albedo *= lerp(0.85, 1.1, grain); // variacao do granulado

                float smoothness = lerp(_DrySmoothness, _WetSmoothness, wet);

                // ---------- luz ----------
                float3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float atten = light.shadowAttenuation * light.distanceAttenuation;

                float NdotL = saturate(dot(N, light.direction));
                float ao = lerp(0.6, 1.0, cav); // buracos mais escuros
                float3 diffuse = albedo * (light.color * NdotL * atten + SampleSH(N) * ao);

                float specPower = exp2(10.0 * smoothness + 1.0);
                float3 H = normalize(light.direction + V);
                float spec = pow(saturate(dot(N, H)), specPower) * smoothness * smoothness * 2.0;

                float fresnel = 0.04 + 0.96 * pow(1.0 - saturate(dot(N, V)), 5.0);
                float3 refl = GlossyEnvironmentReflection(reflect(-V, N), 1.0 - smoothness, ao);

                float3 color = diffuse + light.color * spec * atten + refl * fresnel * smoothness;
                color = MixFog(color, IN.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            float4 ShadowVert(float4 positionOS : POSITION) : SV_POSITION
            {
                float3 pos, nrm; float h;
                DeformSphere(positionOS.xyz, pos, nrm, h);

                float3 positionWS = TransformObjectToWorld(pos);
                float3 normalWS   = TransformObjectToWorldNormal(nrm);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - positionWS);
            #else
                float3 lightDir = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            half4 ShadowFrag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            float4 DepthVert(float4 positionOS : POSITION) : SV_POSITION
            {
                float3 pos, nrm; float h;
                DeformSphere(positionOS.xyz, pos, nrm, h);
                return TransformObjectToHClip(pos);
            }

            half DepthFrag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex DNVert
            #pragma fragment DNFrag

            struct DNVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
            };

            DNVaryings DNVert(float4 positionOS : POSITION)
            {
                DNVaryings OUT;
                float3 pos, nrm; float h;
                DeformSphere(positionOS.xyz, pos, nrm, h);
                OUT.positionCS = TransformObjectToHClip(pos);
                OUT.normalWS   = TransformObjectToWorldNormal(nrm);
                return OUT;
            }

            half4 DNFrag(DNVaryings IN) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(IN.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
}
