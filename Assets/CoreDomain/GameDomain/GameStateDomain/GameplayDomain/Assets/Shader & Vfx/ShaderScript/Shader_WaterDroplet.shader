Shader "Custom/WaterDroplet"
{
    Properties
    {
        _Color         ("Cor da Agua", Color) = (0.6, 0.8, 1, 0.7)
        _HighlightColor ("Cor do Brilho", Color) = (1, 1, 1, 1)
        _HighlightSize  ("Tamanho do Brilho", Range(0.05, 0.4)) = 0.15
        _HighlightPos   ("Posicao do Brilho (0-1)", Vector) = (0.35, 0.65, 0, 0)
        _EdgeSoftness   ("Suavidade da Borda", Range(0.02, 0.5)) = 0.15
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

            float4 _Color;
            float4 _HighlightColor;
            float  _HighlightSize;
            float4 _HighlightPos;
            float  _EdgeSoftness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float dist = length(IN.uv - 0.5) * 2.0;
                float alpha = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, dist);
                alpha *= _Color.a;

                float hDist = length(IN.uv - _HighlightPos.xy);
                float highlight = 1.0 - smoothstep(0.0, _HighlightSize, hDist);

                float3 col = lerp(_Color.rgb, _HighlightColor.rgb, highlight);

                return half4(col * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
