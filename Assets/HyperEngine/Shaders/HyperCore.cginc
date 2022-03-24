#include "UnityCG.cginc"
#include "UnityUI.cginc"
#include "Noise.cginc"

//Instanced parameters
UNITY_INSTANCING_BUFFER_START(Props)
UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
UNITY_DEFINE_INSTANCED_PROP(float, _Ambient)
UNITY_DEFINE_INSTANCED_PROP(float2, _Specular)
UNITY_DEFINE_INSTANCED_PROP(float, _BoundaryAO)
UNITY_DEFINE_INSTANCED_PROP(float4, _Noise)
UNITY_DEFINE_INSTANCED_PROP(float4x4, _HyperRot)
UNITY_DEFINE_INSTANCED_PROP(float4x4, _HyperTileRot)
UNITY_DEFINE_INSTANCED_PROP(float4x4, _HyperMapRot)
UNITY_DEFINE_INSTANCED_PROP(float, _TanKHeight)
UNITY_INSTANCING_BUFFER_END(Props)

//Non-instanced parameters
uniform float _Enable;
uniform float4 _Fog;
uniform float _FogInvDist;
uniform float _KleinV;
uniform float _Proj;
uniform float _CamHeight;
uniform float _DualLight;
uniform float4 _WarpParams;
uniform float4 _ClipRect;
sampler2D _MainTex;
sampler2D _AOTex;

//Macros for easier access
#define i_HyperPos i_HyperRot._m03_m13_m23
#define i_HyperTilePos i_HyperTileRot._m03_m13_m23
#define i_HyperMapPos i_HyperMapRot._m03_m13_m23

//Vertex input structure
struct vin {
  float4 vertex : POSITION;
  float3 normal : NORMAL;
  float4 texcoord : TEXCOORD0;
  float4 texcoord1 : TEXCOORD1;
  fixed4 color : COLOR;

  //GPU instancing
  UNITY_VERTEX_INPUT_INSTANCE_ID
};

//Fragment input structure
struct v2f {
  float4 pos : SV_POSITION;
  float4 origPos : TEXCOORD3;
  float3 worldPos : TEXCOORD1;
  centroid float4 uv : TEXCOORD0;
  float w_dot : TEXCOORD2;
  float3 n : NORMAL;

#ifdef TRANSPARENT
  float4 v_color : COLOR;
#ifdef USE_CLIP_RECT
  float4 vertex : TEXCOORD4;
#endif
#endif

  //GPU instancing
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};

//The curvature-dependent tangent
#if defined(HYPERBOLIC)
#define K -1
#define tan_k tanh
#elif defined(EUCLIDEAN)
#define K 0
#define tan_k(x) (x)
#else
#define K 1
#define tan_k tan
#endif

//Quaternion transform
float3 qtransform(float4 q, float3 v) {
  float3 t = cross(q.xyz, v); t += t;
  return v + q.w * t + cross(q.xyz, t);
}

//Mobius addition (with normal transform)
float3 mobius_add(float3 b, float3 a, inout float3 n) {
  float3 c = K * cross(a, b);
  float d = 1.0 - K * dot(a, b);
  float3 t = a + b;
  n = qtransform(normalize(float4(c, d)), n);
  return (t * d + cross(c, t)) / (d * d + dot(c,c));
}

//Gaussian sine
float gsin(float x, float b) {
  x = mod(x, 1.0) - 0.5;
  return exp(-b*b*x*x)*(0.2 + b*0.2*0.56418958);
}

float bump(float x, float a) {
  return smoothstep(0.0, 1.0, max(0.0, 1.0 - abs(a*x)));
}

float4 portal(float2 p, float sharp, float4 colorize) {
  p = abs(p*2.0 - 1.0);
  float t = -mod(_Time.z * 0.2, 1.0);
  float2 q = log(p*2.0);
  float B = 50.0 * sharp + 1.0;
  float a0 = clamp(1.0 + B * (0.25 - max(p.x, p.y)), 0.0, 1.0);
  a0 *= a0;
  float a1 = gsin(max(q.x, q.y)*3.0 + t, 20.0*sharp);
  float a2 = gsin(max(q.x, q.y)*1.0 + t, 6.0*sharp);
  float a3 = gsin(max(q.x, q.y)*0.5 + t, 3.0*sharp);
  float a4 = gsin(max(q.x, q.y)*0.3 + t, 2.0*sharp);
  float a = a0;
  float b = a0;
  a += a1;
  a += 0.9*a2;
  b += 0.75*a3;
  b += 0.6*a4;
  b = min((a + b)*0.5, 1.0);
  a = min(a, 1.0);
  if (colorize.r < 0.5) {
    return float4(clamp(a + b - 0.5, 0.0, 1.0), min(a + b, 1.0), b, 1.0);
  } else {
    return float4(a, b, min(a + b, 1.0), 1.0);
  }
}

v2f vert(vin v) {
  //Define output structure
  v2f o;

  //VR setup
  UNITY_SETUP_INSTANCE_ID(v);
  UNITY_INITIALIZE_OUTPUT(v2f, o);
  UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  UNITY_TRANSFER_INSTANCE_ID(v, o);

  //Get standard unit point
  float4 w_pos = mul(unity_ObjectToWorld, v.vertex);
  float3 n = UnityObjectToWorldNormal(v.normal);
  o.origPos = w_pos;

#if WATER
  float3 water_n = float3(1.0, 0.0, 0.0);
#endif

  //Clip rectangles and vertex coloring are only used for transparent materials
#ifdef TRANSPARENT
  o.v_color = v.color;
#ifdef USE_CLIP_RECT
  o.vertex = v.vertex;
#endif
#endif

  float w_dot = 0.0;
  float4x4 i_HyperRot = UNITY_ACCESS_INSTANCED_PROP(Props, _HyperRot);
  if (_Enable > 0.0) {
    //Extract the translation out of the view matrix
    float4x4 matV = UNITY_MATRIX_V;
    matV._m03_m13_m23 = 0.0;
    float3 vrPos = mul(UNITY_MATRIX_V._m03_m13_m23, matV);
#ifndef FORCE_EUCLID
    vrPos *= _CamHeight / 1.5;
#endif

    //Convert from Unit coordinates to Klein coordinates
    //NOTE: Don't do this when using "ForceEuclid" shaders
#ifndef EUCLIDEAN
    w_pos.xyz = w_pos.xyz * _KleinV;
#endif

#if WATER
    //Modify height of water effect (hard-coded)
    float3 g_pos = w_pos.xyz / (sqrt(1.0 + K * dot(w_pos.xyz, w_pos.xyz)) + 1.0);
    float4x4 i_HyperTileRot = UNITY_ACCESS_INSTANCED_PROP(Props, _HyperTileRot);
    g_pos = mobius_add(g_pos, i_HyperTilePos, n);
    g_pos = mul(i_HyperTileRot, g_pos);
    float xi = g_pos.x * 2000.0;
    float dy = cos(xi - _Time.z)*0.01;
    float dx = sin(xi + _Time.z);
    dy += (dot(g_pos, g_pos) - 0.97)*6.0;
    w_pos.y += dy;
    water_n = normalize(float3(dx, 5.0, 0.0));
    o.uv.y = w_pos.y;
#elif WAVY
    float x_t = sin(w_pos.x*10.0) * 190.0;
    float y_t = sin(w_pos.y) * 200.0;
    float z_t = cos(w_pos.z*7.0) * 150.0;
    float3 dwave = float3(y_t - _Time.y*3.0, x_t + z_t + _Time.y*2.5, y_t + _Time.y*2.1);
    w_pos.x += sin(dwave.x)*clamp(-w_pos.y*0.01, 0.0, 0.005);
    w_pos.y += sin(dwave.y)*0.002;
    w_pos.z += sin(dwave.z)*clamp(-w_pos.y*0.01, 0.0, 0.005);
    dwave = float3(x_t + z_t + _Time.y*2.5, 0.0f, x_t - z_t + _Time.y*1.5);
    n = normalize(n + cos(dwave)*0.5);
#elif GLITCH
    float4 waveMag = UNITY_ACCESS_INSTANCED_PROP(Props, _Color).a * 0.1;
    float3 dwave = random3(w_pos + _Time.xyz);
    float waveEdge = max(max(abs(o.origPos.x), abs(o.origPos.y)), abs(o.origPos.z));
    waveEdge = clamp((1.0 - waveEdge) * 20.0, 0.0, 1.0);
    w_pos.xyz += dwave * (waveEdge * waveMag);
#elif PLASMA
    float plasma_offset = random2(w_pos.xz + _Time.xy);
    w_pos.xyz += n * plasma_offset * 0.005f;
#endif

    //Apply TanK stretching if applicable
    float i_TanKHeight = UNITY_ACCESS_INSTANCED_PROP(Props, _TanKHeight);
    if (i_TanKHeight > 0.0) {
      w_pos.y = tan_k(w_pos.y) * sqrt(1.0 + K * dot(w_pos.xz, w_pos.xz));
    }

    //Convert from Klein coordinates to Poincaré coordinates
    w_pos.xyz /= sqrt(K * (K + dot(w_pos.xyz, w_pos.xyz))) + 1.0;

#if IS_2D_MAP
    //Apply 2D hyper-rotation to the coordinates
    float4x4 i_HyperMapRot = UNITY_ACCESS_INSTANCED_PROP(Props, _HyperMapRot);
    w_pos.xyz = mobius_add(w_pos.xyz, i_HyperMapPos, n);
    w_pos.xyz = mul(i_HyperMapRot, w_pos.xyz);

    //Project the 3D point to the y=0 plane
    float d = 0.5f * (1.0 + K * dot(w_pos.xyz, w_pos.xyz));
    w_pos.xz /= 1.0 - d + sqrt(d * d - K * w_pos.y * w_pos.y);

    //Projection depends on map projection
    w_dot = dot(w_pos.xz, w_pos.xz);
#ifndef EUCLIDEAN
    w_pos.xyz /= max(1.0 + _Proj * w_dot, 0.0001);
#endif

    //Apply view projection
    o.pos = mul(UNITY_MATRIX_VP, w_pos);
#else
    //Apply 3D hyper-rotation to the coordinates
    w_pos.xyz = mobius_add(w_pos.xyz, i_HyperPos, n);
    w_pos.xyz = mul(i_HyperRot, w_pos.xyz);
    n = mul(i_HyperRot, n);
    w_pos.xyz = mobius_add(w_pos.xyz, vrPos, n);
    w_dot = dot(w_pos.xyz, w_pos.xyz);

    //Invert coordinates to render the far hemisphere
#if TOP_HEMISPHERE
    w_pos.xyz /= w_dot;
#endif

    //Extra hack for trippy warping effects
    if (_WarpParams[1] != 0.0 || _WarpParams[2] != 0.0 || _WarpParams[3] != 0.0) {
      w_pos.y += _WarpParams[0];
      w_pos.y *= (1.0 + w_dot * _WarpParams[2]) / (1.0 + w_dot * _WarpParams[1]);
      w_pos.xz *= 1.0 + w_dot * _WarpParams[3];
      w_pos.y -= _WarpParams[0];
    }

    //Project to Beltrami-Klein coordinates when using H3
#ifdef HYPERBOLIC
    w_pos.w *= 1.0 + w_dot;
#endif

    //Apply view projection
    o.pos = mul(UNITY_MATRIX_P, mul(matV, w_pos));
#endif
  } else {
    //Apply regular, euclidean view projection in Unity editor
    o.pos = mul(UNITY_MATRIX_VP, w_pos);
  }

  o.worldPos = w_pos.xyz;
  o.w_dot = w_dot;

#if WATER
  o.n = water_n;
#elif PLASMA
  o.n = mul(UNITY_MATRIX_V, n);
#elif GLOW
  o.n = mul(UNITY_MATRIX_V, n);
#elif HOLO
  o.n = mul(UNITY_MATRIX_V, n);
#elif PORTAL
  o.n = mul(UNITY_MATRIX_V, w_pos.xyz);
#elif CLOUD
  o.n = normalize(v.normal);
#else
  o.n = n;
#endif

  //Water special effect overrides the UV to utilize centroid, so don't set it for water.
  //Otherwise, combine both float2 UV maps into a single float4 centroid UV.
#ifndef WATER
  float4 i_MainTex_ST = UNITY_ACCESS_INSTANCED_PROP(Props, _MainTex_ST);
  o.uv = float4(v.texcoord1.xy*i_MainTex_ST.xy + i_MainTex_ST.zw, v.texcoord.xy);
#endif
  return o;
}

//Depth buffer trick for hypersphere
#if TOP_HEMISPHERE
void frag(v2f i, out fixed4 color : SV_Target, out float depth : SV_Depth) {
#ifdef UNITY_REVERSED_Z
  depth = 0.5 - 0.5*i.pos.z;
#else
  depth = 1.0 - 0.5*i.pos.z;
#endif
#elif BOTTOM_HEMISPHERE
void frag(v2f i, out fixed4 color : SV_Target, out float depth : SV_Depth) {
#ifdef UNITY_REVERSED_Z
  depth = 0.5 + 0.5*i.pos.z;
#else
  depth = 0.5*i.pos.z;
#endif
#else
void frag(v2f i, out fixed4 color : SV_Target) {
#endif
  UNITY_SETUP_INSTANCE_ID(i);
  UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

  //Do not extend water underground
#if WATER
  if (i.uv.y < 0.0) discard;
#endif

  //NOTE: Even though w_dot is strictly a positive value, MSAA may extrapolate it
  //to a negative number, so the max function is used here to clamp it.
  float w_dot = max(i.w_dot, 0.0);

#if IS_2D_MAP
  //Discard double-covered triangles in spherical geometry
#if S2xE
  if (w_dot * max(abs(_Proj), 0.01) > 1.0) discard;
#endif
#else
  //Far things should be drawn on the opposite hemisphere, so we can discard them here
#if TOP_HEMISPHERE
  if (w_dot < 0.5) { discard; }
#elif BOTTOM_HEMISPHERE
  if (w_dot > 2.0) { discard; }
#endif

  //Fog effect
#if defined(HYPERBOLIC) || defined(EUCLIDEAN)
  float fog_a = max(1.0 - _FogInvDist * w_dot, 0.0);
  fog_a = _Fog.a * (1.0 - fog_a);
#else
  float fog_a = _Fog.a * w_dot / (2.0 + K * w_dot);
  fog_a *= fog_a;
  fog_a *= fog_a;
#endif
#endif

  //Ambient occlusion
  float ao = tex2D(_AOTex, i.uv.zw).r;
#if BOUNDARY_BLEND
  float i_BoundaryAO = UNITY_ACCESS_INSTANCED_PROP(Props, _BoundaryAO);
  float a1 = clamp(min(5.0*(max(abs(i.origPos.x), abs(i.origPos.z)) - 0.8), 1.0 - i.origPos.y*5.0), 0.0, 1.0);
  ao = (1.0 - a1)*ao + a1 * i_BoundaryAO;
#endif

  //Cafe lighting
#if CAFE_LIGHT
  float absDist = 20.0 * (abs(1.0 - abs(i.origPos.x)) + abs(1.0 - abs(i.origPos.z)));
  ao *= absDist / (absDist + 1.0);
#endif

  //Compute normal
  float3 n = normalize(i.n);

  float4 i_Color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
#if PORTAL
  float4 col = portal(i.uv.xy, 0.5, i_Color);
#else
#if IGNORE_TEX_COLOR
  float4 col = i_Color;
  col.a *= tex2D(_MainTex, i.uv.xy).a;
#else
  float4 col = i_Color * tex2D(_MainTex, i.uv.xy);
#endif
#ifdef TRANSPARENT
#ifdef USE_CLIP_RECT
  col.a *= UnityGet2DClipping(i.vertex.xy, _ClipRect);
#endif
  col *= i.v_color;
#endif
  //Add 3D noise texture (cyclic per-tile)
  float4 i_Noise = UNITY_ACCESS_INSTANCED_PROP(Props, _Noise);
#if HOLO
  float noise = i_Noise.a * blendNoise(i.origPos.xyz*float3(0.4,2.0,0.4) + 0.5);
#else
  float noise = i_Noise.a * blendNoise(i.origPos.xyz*0.5 + 0.5);
  col.rgb = i_Noise.rgb * noise + col.rgb * (1.0 - noise);
#endif
  //NOTE: Clamp here is needed due to numerical precision issues causing speckling
#if CLOUD
  float3 diffuse = clamp(0.5 + 0.5*n.y, 0.0, 1.0);
#else
  float3 diffuse = clamp(0.5 + 0.5*dot(n, _WorldSpaceLightPos0.xyz), 0.0, 1.0);
#endif
  float i_Ambient = UNITY_ACCESS_INSTANCED_PROP(Props, _Ambient);
#ifndef TRANSPARENT
  if (_DualLight > 0.0) {
    float diffuse2 = clamp(0.5 + 0.5*dot((-n.x, n.y, -n.z), _WorldSpaceLightPos0.xyz), 0.0, 1.0);
    diffuse = float3(diffuse.x, diffuse2, (diffuse.x + diffuse2)*0.5);
    i_Ambient *= 0.5f;
  }
#endif
  diffuse = i_Ambient + (1.0 - i_Ambient)*diffuse;

  col.rgb *= ao * diffuse;
#endif

#if WATER
  float wash = clamp(i.uv.y*200.0, 0.0, 1.0);
  color = float4(col.rgb*wash + (1.0 - wash), 1.0);
#elif PLASMA
  color = i_Color;
  color.a *= 1.0 - abs(n.z);
#elif GLOW
  float nz = n.z * n.z;
  col.a *= nz * nz;
  color = col;
#elif HOLO
  float nz = abs(n.z) + (noise - 0.3);
  col.r += 0.05 * bump(nz - 0.15, 7.0) + 0.33 * bump(nz - 0.7, 4.0);
  col.g += 0.3 * bump(nz - 0.5, 3.0);
  col.b += 0.5 * bump(nz - 0.25, 4.0);
  color = col;
#else
  color = col;

#ifndef IS_2D_MAP
  //Add specular
  float3 viewDirection = normalize(i.worldPos.xyz);
  float3 lightDirection = _WorldSpaceLightPos0.xyz;
  float2 i_Specular = UNITY_ACCESS_INSTANCED_PROP(Props, _Specular);
  float specular = i_Specular.x * pow(max(0.001, dot(reflect(lightDirection, n), viewDirection)), i_Specular.y + 1.0);
  color.rgb += specular;

#ifndef FORCE_EUCLID
  //Blend in the fog
  color.rgb = color.rgb*(1.0 - fog_a) + _Fog.rgb*fog_a;
#endif
#endif
#endif
}
