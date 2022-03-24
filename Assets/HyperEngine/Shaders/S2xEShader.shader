Shader "Custom/S2xEShader" {
  Properties{
    _MainTex("Texture", 2D) = "white" {}
    _AOTex("AO Map", 2D) = "white" {}
    _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
    _Ambient("Ambient", Float) = 0.6
    _BoundaryAO("Boundary AO", Float) = 0.9
    _Noise("Noise", Color) = (0.0,0.0,0.0,0.0)
  }

  //Opaque shader
  SubShader{
    Tags{
      "Queue" = "Geometry"
      "HyperRenderType" = "Opaque"
      "LightMode" = "ForwardBase"
    }
    Pass {
      Cull Back
      ZTest LEqual
    
      CGPROGRAM
      #pragma multi_compile __ BOUNDARY_BLEND
      #pragma multi_compile __ CAFE_LIGHT PORTAL WATER WAVY PLASMA GLOW
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define S2xE 1
      #define IS_2D_MAP 1
      #include "HyperCore.cginc"
      ENDCG
    }
  }

  //Transparent Shader
  SubShader{
    Tags{
      "Queue" = "Transparent"
      "HyperRenderType" = "Transparent"
      "LightMode" = "ForwardBase"
    }
    Pass {
      Cull Back
      ZTest LEqual
      ZWrite Off
      Blend [_SrcMode] [_DstMode], [_SrcModeA] [_DstModeA]
    
      CGPROGRAM
      #pragma multi_compile __ BOUNDARY_BLEND
      #pragma multi_compile __ CAFE_LIGHT PORTAL WATER WAVY PLASMA GLOW IGNORE_TEX_COLOR
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define S2xE 1
      #define IS_2D_MAP 1
      #define TRANSPARENT 1
      #include "HyperCore.cginc"
      ENDCG
    }
  }

  //Clear shader
  SubShader{
    Tags{
      "Queue" = "Transparent+10"
      "HyperRenderType" = "Clear"
    }
    Pass {
      Cull Back
      ZTest Always
      Blend SrcAlpha OneMinusSrcAlpha

      CGPROGRAM
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #include "Clear.cginc"
      ENDCG
    }
  }
}
