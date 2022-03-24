#include "UnityCG.cginc"

//Non-instanced parameters
float4 _Color;

//Vertex input structure
struct vin {
  float4 vertex : POSITION;

  //GPU instancing
  UNITY_VERTEX_INPUT_INSTANCE_ID
};

//Fragment input structure
struct v2f {
  float4 pos : SV_POSITION;

  //GPU instancing
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert(vin v) {
  //Define output structure
  v2f o;

  //VR setup
  UNITY_SETUP_INSTANCE_ID(v);
  UNITY_INITIALIZE_OUTPUT(v2f, o);
  UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  UNITY_TRANSFER_INSTANCE_ID(v, o);

  //Ignore the view matrix, always center over camera origin
  o.pos = mul(UNITY_MATRIX_P, mul(unity_ObjectToWorld, v.vertex));
  return o;
}

void frag(v2f i, out fixed4 color : SV_Target) {
  UNITY_SETUP_INSTANCE_ID(i);
  UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

  color = _Color;
}
