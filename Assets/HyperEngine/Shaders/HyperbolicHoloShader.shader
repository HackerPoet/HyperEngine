Shader "Custom/HyperbolicHoloShader" {
  Properties{
    _MainTex("Texture", 2D) = "white" {}
    _AOTex("AO Map", 2D) = "white" {}
    _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
    _Ambient("Ambient", Float) = 0.6
    _BoundaryAO("Boundary AO", Float) = 0.9
    _Noise("Noise", Color) = (0.0,0.0,0.0,0.0)
  }

  //Holographic shader
  SubShader{
    Tags{
      "Queue" = "Geometry"
      "HyperRenderType" = "Holo"
      "LightMode" = "ForwardBase"
    }
    Pass {
      Cull Front
      ZTest LEqual
    
      CGPROGRAM
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define HYPERBOLIC 1
      #define HOLO 1
      #include "HyperCore.cginc"
      ENDCG
    }
    Pass {
      Cull Back
      ZTest LEqual
      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha, One Zero

      CGPROGRAM
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define HYPERBOLIC 1
      #define HOLO 1
      #define TRANSPARENT 1
      #include "HyperCore.cginc"
      ENDCG
    }
  }
  CustomEditor "HyperbolicEditor"
}
