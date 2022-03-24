//GLSL style mod
float mod(float x, float y) { return x - y * floor(x / y); }
float2 mod(float2 x, float2 y) { return x - y * floor(x / y); }
float3 mod(float3 x, float3 y) { return x - y * floor(x / y); }


float random2(float2 st) {
  return frac(sin(dot(st, float2(12.9898, 78.233))) * 43758.5453123);
}

float3 random3(float3 c) {
	float j = 4096.0*sin(dot(c,float3(17.0, 59.4, 15.0)));
	float3 r;
	r.z = frac(512.0*j);
	j *= .125;
	r.x = frac(512.0*j);
	j *= .125;
	r.y = frac(512.0*j);
	return r + r - 1.0;
}

float simplex3d(float3 p) {
  /* skew constants for 3d simplex functions */
  const float F3 =  0.3333333;
  const float G3 =  0.1666667;

  /* calculate s and x */
  p *= 10.0;
  float3 s = floor(p + dot(p, float3(F3,F3,F3)));
  float3 x = p - s + dot(s, float3(G3,G3,G3));

  /* calculate i1 and i2 */
  float3 e = step(0.0, x - x.yzx);
  float3 i1 = e*(1.0 - e.zxy);
  float3 i2 = 1.0 - e.zxy*(1.0 - e);

  /* x1, x2, x3 */
  float3 x1 = x - i1 + G3;
  float3 x2 = x - i2 + 2.0*G3;
  float3 x3 = x - 1.0 + 3.0*G3;

  /* calculate surflet weights */
  float4 w = float4(dot(x, x), dot(x1, x1), dot(x2, x2), dot(x3, x3));

  /* w fades from 0.6 at the center of the surflet to 0.0 at the margin */
  w = max(0.6 - w, 0.0);

  /* calculate surflet components */
  float4 d = float4(dot(random3(s), x),
                    dot(random3(s + i1), x1),
                    dot(random3(s + i2), x2),
                    dot(random3(s + 1.0), x3));

  /* multiply d by w^4 */
  w *= w;
  w *= w;
  d *= w;

  /* return the sum of the four surflets */
  return (d.x + d.y + d.z + d.w) * 16.0 + 0.5f;
}

float blendNoise(float3 p) {
    p.xz = mod(p.xz, 1.0);
    p.y += 0.11;
    
    float2 d = p.xz - 0.5;
    float w = max(d.x*d.x, d.y*d.y)*4.0;
    w *= w;
    w *= w;
    float3 q = p;
    if (q.x > 0.5) q.x = 1.0 - q.x;
    if (q.z > 0.5) q.z = 1.0 - q.z;
    if (q.z > q.x) q.xz = q.zx;

    return simplex3d(p)*(1.0 - w) + simplex3d(q)*w;
}