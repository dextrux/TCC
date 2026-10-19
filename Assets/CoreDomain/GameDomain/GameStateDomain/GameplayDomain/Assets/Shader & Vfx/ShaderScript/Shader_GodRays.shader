Shader "Custom/URP/GodRays"
{
    Properties
    {
        _Tint          ("Shaft Tint", Color)                = (0.65, 0.78, 1.0, 1)
        _Intensity     ("Intensity", Range(0, 20))          = 3
        _Density       ("Fog Density", Range(0, 0.5))       = 0.04
        _MaxDistance   ("Max Distance", Range(5, 150))      = 40
        _Anisotropy    ("Anisotropy (g)", Range(0, 0.95))   = 0.55
        _BaseHeight    ("Fog Base Height", Float)           = 0
        _HeightFalloff ("Height Falloff", Range(0, 1))      = 0.0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "MoonShafts"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #define STEPS 48

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float  _Intensity;
                float  _Density;
                float  _MaxDistance;
                float  _Anisotropy;
                float  _BaseHeight;
                float  _HeightFalloff;
            CBUFFER_END

            float InterleavedGradientNoise(float2 p)
            {
                return frac(52.9829189 * frac(dot(p, float2(0.06711056, 0.00583715))));
            }

            // Henyey-Greenstein sem o 1/4pi 
            float HG(float cosTheta, float g)
            {
                float g2 = g * g;
                return (1.0 - g2) / pow(max(1.0 + g2 - 2.0 * g * cosTheta, 1e-4), 1.5);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                half4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float depth = SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif

                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float3 camPos   = GetCameraPositionWS();
                float3 toPixel  = worldPos - camPos;
                float  dist     = min(length(toPixel), _MaxDistance);
                float3 rayDir   = normalize(toPixel);

                float stepLen = dist / STEPS;
                float jitter  = InterleavedGradientNoise(i.positionCS.xy);
                float3 p      = camPos + rayDir * (stepLen * jitter);

                float acc = 0.0;

                [loop]
                for (int s = 0; s < STEPS; s++)
                {
                    p += rayDir * stepLen;

                    float4 shadowCoord = TransformWorldToShadowCoord(p);
                    float  lit         = MainLightRealtimeShadow(shadowCoord);
                    float  h           = exp(-max(p.y - _BaseHeight, 0.0) * _HeightFalloff);

                    acc += lit * h;
                }

                acc *= stepLen * _Density;

                Light  moon  = GetMainLight();
                float  phase = HG(dot(rayDir, moon.direction), _Anisotropy);

                float3 shafts = moon.color * _Tint.rgb * (acc * phase * _Intensity);

                return half4(scene.rgb + shafts, scene.a);
            }
            ENDHLSL
        }
    }
}