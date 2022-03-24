Shader "Custom/ForceEuclidHoloShader" {
  Properties{
    _MainTex("Texture", 2D) = "white" {}
    _AOTex("AO Map", 2D) = "white" {}
    _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
    _Ambient("Ambient", Float) = 0.6
    _BoundaryAO("Boundary AO", Float) = 0.9
    _Noise("Noise", Color) = (0.0,0.0,0.0,0.0)
  }

  //Force-Euclidean holo shader
  SubShader{
    Tags{
      "Queue" = "Transparent"
      "HyperRenderType" = "ForceEuclidHolo"
      "LightMode" = "ForwardBase"
    }
    Pass {
      Cull Front
      ZTest LEqual
      ZWrite On

      CGPROGRAM
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define EUCLIDEAN 1
      #define FORCE_EUCLID 1
      #define HOLO 1
      #include "HyperCore.cginc"
      ENDCG
    }
    Pass {
      Cull Back
      ZTest LEqual
      ZWrite On
      Blend SrcAlpha OneMinusSrcAlpha, One Zero

      CGPROGRAM
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define EUCLIDEAN 1
      #define FORCE_EUCLID 1
      #define HOLO 1
      #define TRANSPARENT 1
      #include "HyperCore.cginc"
      ENDCG
    }
  }
  CustomEditor "HyperbolicEditor"
}
