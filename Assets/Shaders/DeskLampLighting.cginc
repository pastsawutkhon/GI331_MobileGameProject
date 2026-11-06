#ifndef DESK_LAMP_LIGHTING_INCLUDED
#define DESK_LAMP_LIGHTING_INCLUDED

// Coordinates are normalized to the cropped lamp sprite, shared by metal and glow.
float LampLightBoundary(float x)
{
    float across = (x - 0.22) / 0.225;
    float rimCurve = sqrt(saturate(1.0 - across * across));
    float mouthEdge = 0.635 - 0.36 * (x - 0.22) + 0.072 * rimCurve;
    return lerp(mouthEdge, 0.48, smoothstep(0.42, 0.52, x));
}

float LampLightVisibility(float2 p)
{
    float edge = LampLightBoundary(p.x);
    return 1.0 - smoothstep(edge - 0.05, edge, p.y);
}

float LampFrontAssembly(float2 p)
{
    return step(LampLightBoundary(p.x), p.y);
}

#endif
