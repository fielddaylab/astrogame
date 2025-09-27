#ifndef FD_DISSOLVEMATH
#define FD_DISSOLVEMATH

float DissolveGrayscale(float x, float y, sampler2D tex, float dissolveFactor)
{
    return 1.0 - saturate(tex2D(tex, float2(x, y)).r - dissolveFactor);
}

#endif // FD_DISSOLVEMATH