// Hash/noise helpers (Simplex-like gradient noise, barato)
float2 hash2(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
}

float gradientNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);

    float a = dot(hash2(i + float2(0,0)), f - float2(0,0));
    float b = dot(hash2(i + float2(1,0)), f - float2(1,0));
    float c = dot(hash2(i + float2(0,1)), f - float2(0,1));
    float d = dot(hash2(i + float2(1,1)), f - float2(1,1));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y) * 0.5 + 0.5;
}

// Voronoi simplificado (retorna distância ao ponto mais próximo)
float voronoi(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float minDist = 1.0;

    for (int y = -1; y <= 1; y++)
    for (int x = -1; x <= 1; x++)
    {
        float2 neighbor = float2(x, y);
        float2 point_ = hash2(i + neighbor) * 0.5 + 0.5;
        float2 diff = neighbor + point_ - f;
        float dist = length(diff);
        minDist = min(minDist, dist);
    }
    return minDist;
}

void CampfireFire_float(
    float2 UV, float Time,
    float DissolveSpeed, float2 DistortionSpeed,
    float DissolveScale, float DistortionScale,
    float FlameSharpness, float TurbulenceAmount,
    float EmissionIntensity,
    out float3 Color, out float Alpha)
{
    // 1) Máscara vertical: sólido embaixo, afina em cima
    float topFade = 1.0 - UV.y;
    float shapeMask = pow(saturate(topFade), FlameSharpness);

    // 2) Turbulência horizontal (a chama "dança")
    float waveX = sin(Time * 3.0 + UV.y * 6.0) * TurbulenceAmount;

    // 3) Distorção via gradient noise (scroll pra cima)
    float2 distortUV = UV * DistortionScale + float2(waveX, -Time * DistortionSpeed.y);
    float distortion = gradientNoise(distortUV);

    // 4) Voronoi como base do "corpo" da chama, escalado e distorcido, subindo
    float2 voronoiUV = UV * DissolveScale + float2(waveX * 0.5, -Time * DissolveSpeed);
    voronoiUV += (distortion - 0.5) * 0.4; // aplica a distorção do noise no voronoi
    float voro = voronoi(voronoiUV);
    float flameBody = pow(saturate(1.0 - voro), 2.0);

    // 5) Combina corpo + máscara vertical
    float fireAlpha = flameBody * shapeMask;
    fireAlpha = smoothstep(0.1, 0.6, fireAlpha);

    // 6) Cor: gradiente amarelo -> laranja -> vermelho -> preto, usando o alpha como eixo
    float3 c1 = float3(1.0, 0.95, 0.6);   // amarelo claro (base/pontos quentes)
    float3 c2 = float3(1.0, 0.45, 0.05);  // laranja
    float3 c3 = float3(0.5, 0.05, 0.02);  // vermelho escuro
    float3 c4 = float3(0.0, 0.0, 0.0);    // preto (bordas)

    float t = fireAlpha;
    float3 col;
    if (t < 0.33) col = lerp(c4, c3, t / 0.33);
    else if (t < 0.66) col = lerp(c3, c2, (t - 0.33) / 0.33);
    else col = lerp(c2, c1, (t - 0.66) / 0.34);

    Color = col * EmissionIntensity;
    Alpha = fireAlpha;
}