Shader "Custom/H2xEShader" {
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
      #define HYPERBOLIC 1
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
      Blend SrcAlpha OneMinusSrcAlpha
    
      CGPROGRAM
      #pragma multi_compile __ BOUNDARY_BLEND
      #pragma multi_compile __ CAFE_LIGHT PORTAL WATER WAVY PLASMA GLOW IGNORE_TEX_COLOR
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define HYPERBOLIC 1
      #define IS_2D_MAP 1
      #include "HyperCore.cginc"
      ENDCG
    }
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
      ZTest LEqual
      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha, Zero One

      CGPROGRAM
      #pragma multi_compile __ BOUNDARY_BLEND
      #pragma multi_compile __ CAFE_LIGHT PORTAL WATER WAVY PLASMA GLOW IGNORE_TEX_COLOR
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define EUCLIDEAN 1
      #define IS_2D_MAP 1
      #define FORCE_EUCLID 1
      #define TRANSPARENT 1
      #include "HyperCore.cginc"
      ENDCG
    }
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

  //Force-Euclidean overlay shader
  SubShader{
    Tags{
      "Queue" = "Transparent+100"
      "HyperRenderType" = "ForceEuclidOverlay"
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
      #define EUCLIDEAN 1
      #define FORCE_EUCLID 1
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
      #define EUCLIDEAN 1
      #define FORCE_EUCLID 1
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
