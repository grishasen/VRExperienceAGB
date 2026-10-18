#ifndef CAREZZA_PANORAMA_INCLUDED
#define CAREZZA_PANORAMA_INCLUDED

float CarezzaSideBlend(float degrees)
{
    return smoothstep(55,100,abs(degrees));
}

float2 CarezzaPanoramaUV(float degrees,float elevation)
{
    float angle=abs(degrees);
    float t=saturate((angle-55)/45);
    float extraDensity=1/.48-1;
    // Integrate the horizontal sampling density. Its derivative matches the vertical density,
    // so the smaller side ridge retains the rear panorama's tree proportions.
    float integral=45*(t*t*t-.5*t*t*t*t)+max(angle-100,0);
    float u=.5+sign(degrees)*(angle+extraDensity*integral)/120;
    // Mirror full-width artwork at its edges instead of stretching a narrow side crop.
    u=1-abs(frac(u*.5)*2-1);
    float density=1+extraDensity*(t*t*(3-2*t));
    return float2(u,elevation*density);
}

#endif
