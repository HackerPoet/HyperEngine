Shader "Custom/HyperbolicShadowShader" {
  Properties{
    _MainTex("Texture", 2D) = "white" {}
    _AOTex("AO Map", 2D) = "white" {}
    _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
    _Ambient("Ambient", Float) = 0.6
    _BoundaryAO("Boundary AO", Float) = 0.9
    _Noise("Noise", Color) = (0.0,0.0,0.0,0.0)
  }
  SubShader{
    Tags{
      "Queue" = "Geometry+400"
      "HyperRenderType" = "Shadow"
      "LightMode" = "ForwardBase"
    }
    Pass {
      Stencil {
        Ref 1
        Comp Always
        Pass Replace
      }
      Cull Front
      ZTest LEqual
      ZWrite Off
      ColorMask 0

      CGPROGRAM
      #pragma multi_compile __ CAFE_LIGHT PORTAL WATER WAVY PLASMA GLOW
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define HYPERBOLIC 1
      #include "HyperCore.cginc"
      ENDCG
    }
    Pass {
      Stencil {
        Ref 0
        Comp Equal
        Pass Keep
      }
      Cull Back
      ZTest LEqual
      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha, One Zero

      CGPROGRAM
      #pragma multi_compile __ CAFE_LIGHT PORTAL WATER WAVY PLASMA GLOW
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define HYPERBOLIC 1
      #include "HyperCore.cginc"
      ENDCG
    }
  }
  CustomEditor "HyperbolicEditor"
}
