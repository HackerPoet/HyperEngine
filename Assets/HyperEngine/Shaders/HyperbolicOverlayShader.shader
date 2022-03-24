Shader "Custom/HyperbolicOverlayShader" {
  Properties{
    _MainTex("Texture", 2D) = "white" {}
    _AOTex("AO Map", 2D) = "white" {}
    _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
    _Ambient("Ambient", Float) = 0.6
    _BoundaryAO("Boundary AO", Float) = 0.9
    _Noise("Noise", Color) = (0.0,0.0,0.0,0.0)

    [Enum(UnityEngine.Rendering.BlendMode)] _SrcMode("SrcMode", Float) = 0
    [Enum(UnityEngine.Rendering.BlendMode)] _DstMode("DstMode", Float) = 0
  }
  SubShader{
    Tags{
      "Queue" = "Transparent+100"
      "HyperRenderType" = "Overlay"
      "LightMode" = "ForwardBase"
    }
    Pass{
      Cull Back
      ColorMask 0
      ZWrite On
      ZTest Always

      CGPROGRAM
      #pragma multi_compile __ IGNORE_TEX_COLOR
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define HYPERBOLIC 1
      #include "HyperCore.cginc"
      ENDCG
    }
    Pass{
      Cull Back
      ZWrite On
      ZTest LEqual
      Blend [_SrcMode] [_DstMode]

      CGPROGRAM
      #pragma multi_compile __ IGNORE_TEX_COLOR
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define HYPERBOLIC 1
      #define TRANSPARENT 1
      #include "HyperCore.cginc"
      ENDCG
    }
  }
  CustomEditor "HyperbolicEditor"
}
