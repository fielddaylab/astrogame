#ifndef ASTRO_WAVEFORM_INCLUDES
#define ASTRO_WAVEFORM_INCLUDES

#define WAVE_INPUT(offset, timeMult, xMult) ((offset) + ((time) * (timeMult)) + ((x) * (xMult)))

float WaveStatic(float time, float x)
{
    return 0.4 * sin(WAVE_INPUT(0, 4, 25))
        + 0.15 * cos(WAVE_INPUT(0.45, -13.3, -64))
        + 0.1 * sin(WAVE_INPUT(0.879, 1, -71));
}

float WaveTexture(float time, float x, sampler2D tex)
{
    return 2 * tex2D(tex, float2(x + time, 0.5)).r - 1;
}

float SdfConstantY(float originY, float y, float thickness)
{
    return abs(y - originY) - thickness;
}

float SdfCircle(float2 origin, float2 pos, float radius)
{
    return length(pos - origin) - radius;
}

float SdfCircle(fixed2 origin, fixed2 pos, float radius)
{
    return length(pos - origin) - radius;
}

#define SdfAABlend(a, b, distance) lerp(a, b, clamp(1.0 - (distance), 0.0, 1.0))

#endif // ASTRO_WAVEFORM_INCLUDES