#ifndef ASTRO_WAVEFORM_INCLUDES
#define ASTRO_WAVEFORM_INCLUDES

#define WAVE_INPUT(offset, timeMult, xMult) ((offset) + ((time) * (timeMult)) + ((x) * (xMult)))

float WaveStatic(float time, float x)
{
    return 0.4 * sin(WAVE_INPUT(0, 4.1561, 25))
        - 0.15 * cos(WAVE_INPUT(0.45, -1.133, -64))
        + 0.1 * sin(WAVE_INPUT(0.979, 0.2, -.7))
        + 0.14 * cos(WAVE_INPUT(0.213, 16, 3.145));
}

float WaveTexture(float time, float x, float y, sampler2D tex)
{
    return 2 * tex2D(tex, float2(x, y + time)).r - 1;
}

float WaveTextureHalf(float time, float x, float y, sampler2D tex)
{
    return tex2D(tex, float2(x, y + time)).r;
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

#define SdfAABlend(colorA, colorB, distance) lerp(colorA, colorB, (colorB).a * saturate(1.0 - (distance)))

#endif // ASTRO_WAVEFORM_INCLUDES