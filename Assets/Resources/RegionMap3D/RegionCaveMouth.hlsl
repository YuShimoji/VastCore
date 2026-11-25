// Coordinates in metres relative to the cut in the cliff. The sky can still
// illuminate a horizontal recess after the vertical sky ray hits its ceiling.
TEXTURE2D(_CliffPhoto); SAMPLER(sampler_CliffPhoto);
TEXTURE2D(_CliffNormal); SAMPLER(sampler_CliffNormal);
TEXTURE2D(_Gravel); SAMPLER(sampler_Gravel);
TEXTURE2D(_Scree); SAMPLER(sampler_Scree);

half3 MouthSurface(float3 p, float3 n, float2 uv, half2 visibility, Light sun)
{
    float3 weights = pow(abs(n), 4);
    weights /= max(0.001, dot(weights, float3(1,1,1)));
    // Offset and rotate the grain on intersecting fracture planes.
    float3 q = p * 0.39;
    q.y += 0.12 * q.x + 0.07 * q.z;
    float2 x = q.zy, y = q.xz + 0.29, z = q.xy + 0.63;
    half3 face = SAMPLE_TEXTURE2D(_CliffPhoto,sampler_CliffPhoto,x).rgb * weights.x
               + SAMPLE_TEXTURE2D(_CliffPhoto,sampler_CliffPhoto,y).rgb * weights.y
               + SAMPLE_TEXTURE2D(_CliffPhoto,sampler_CliffPhoto,z).rgb * weights.z;
    half2 nx = UnpackNormal(SAMPLE_TEXTURE2D(_CliffNormal,sampler_CliffNormal,x)).xy;
    half2 ny = UnpackNormal(SAMPLE_TEXTURE2D(_CliffNormal,sampler_CliffNormal,y)).xy;
    half2 nz = UnpackNormal(SAMPLE_TEXTURE2D(_CliffNormal,sampler_CliffNormal,z)).xy;
    float3 slopes = weights.x * float3(0,nx.y,nx.x) + weights.y * float3(ny.x,0,ny.y)
                  + weights.z * float3(nz.x,nz.y,0);
    float outDist = dot(p, _Outward.xyz);
    half gravelAmount = smoothstep(0.55,0.92,n.y) * (1-smoothstep(0.1,2.5,p.y));
    half3 gravel = SAMPLE_TEXTURE2D(_Gravel,sampler_Gravel,p.xz * 0.52).rgb;
    half3 chips = SAMPLE_TEXTURE2D(_Gravel,sampler_Gravel,p.zx * 0.19 + 0.23).rgb;
    gravel = lerp(gravel,chips,0.22) * half3(0.95,0.90,0.87);
    float across = dot(p.xz, float2(_Outward.z,-_Outward.x));
    float pathEdge = abs(across + 0.75*sin(outDist*0.33)) + chips.r*1.7;
    half3 fines = SAMPLE_TEXTURE2D(_Scree,sampler_Scree,p.xz*0.68+0.13).rgb;
    gravel = lerp(gravel,fines,0.20+0.64*smoothstep(2.4,5.5,pathEdge));
    face *= half3(0.9,0.84,0.78);
    half3 albedo = lerp(face,gravel,gravelAmount);
    if (_Boulder > 0.5)
    {
        half3 scan = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;
        half luminance = dot(scan, half3(0.21,0.72,0.07));
        albedo = lerp(luminance.xxx, scan, 0.65) * lerp(half3(0.83,0.80,0.76),half3(1.12,1.09,1.02),saturate(face * 2));
        gravelAmount = 0;
    }
    n = normalize(n + 0.62 * (slopes - dot(slopes,n)*n) * (1-0.7*gravelAmount));

    float sky = visibility.x;
    if (_Boulder > 0.5) sky = smoothstep(1,9,outDist + 0.38*p.y);
    float intoRock = max(0, 7 - outDist);
    float opening = 1 / (1 + intoRock * intoRock * 0.012);
    half skyFacing = saturate(dot(n,normalize(_Outward.xyz + float3(0,0.65,0))));
    half3 fill = (0.15 + 0.16 * sky) * half3(0.80,0.89,1.0)
               + opening * (0.27 + 0.34 * skyFacing) * half3(0.79,0.88,1.0)
               + opening * saturate(-n.y) * half3(0.17,0.12,0.08);
    // Light arriving sideways through the opening reveals the first inner turn.
    float3 toOpening = _Outward.xyz * 6.6 + float3(0,1.6,0) - p;
    float distance2 = dot(toOpening,toOpening);
    float angle = saturate(dot(n,toOpening * rsqrt(max(distance2,0.001))));
    fill += (1-sky) * (0.31+0.69*angle) / (1+distance2/62) * half3(0.72,0.80,0.90);
    half sunlight = saturate(dot(n,sun.direction)) * sun.shadowAttenuation * visibility.y;
    return albedo * (fill + 0.68 * sun.color * sunlight);
}
