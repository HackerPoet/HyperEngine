Shader "Custom/ForceEuclidShader" {
  Properties{
    _MainTex("Texture", 2D) = "white" {}
    _AOTex("AO Map", 2D) = "white" {}
    _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
    _Ambient("Ambient", Float) = 0.6
    _BoundaryAO("Boundary AO", Float) = 0.9
    _Noise("Noise", Color) = (0.0,0.0,0.0,0.0)

    _ZWrite("ZWrite", Int) = 0.0
    [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
    [Enum(UnityEngine.Rendering.BlendMode)] _SrcMode("SrcMode", Float) = 0
    [Enum(UnityEngine.Rendering.BlendMode)] _DstMode("DstMode", Float) = 0
    [Enum(UnityEngine.Rendering.BlendMode)] _SrcModeA("SrcModeA", Float) = 0
    [Enum(UnityEngine.Rendering.BlendMode)] _DstModeA("DstModeA", Float) = 0
  }

  //Force-Euclidean shader
  SubShader{
    Tags{
      "Queue" = "Transparent"
      "HyperRenderType" = "ForceEuclid"
      "LightMode" = "ForwardBase"
    }
    Pass {
      Cull Back
      ZTest [_ZTest]
      ZWrite [_ZWrite]
      Blend [_SrcMode] [_DstMode], [_SrcModeA] [_DstModeA]

      CGPROGRAM
      #pragma multi_compile __ IGNORE_TEX_COLOR
      #pragma multi_compile __ USE_CLIP_RECT
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define EUCLIDEAN 1
      #define FORCE_EUCLID 1
      #define TRANSPARENT 1
      #include "HyperCore.cginc"
      ENDCG
    }
  }
  CustomEditor "HyperbolicEditor"
}
