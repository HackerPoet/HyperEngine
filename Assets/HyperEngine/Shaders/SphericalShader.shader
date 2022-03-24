Shader "Custom/SphericalShader" {
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
      #pragma multi_compile __ CAFE_LIGHT PORTAL WATER WAVY PLASMA GLOW GLITCH
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define BOTTOM_HEMISPHERE 1
      #include "HyperCore.cginc"
      ENDCG
    }
    Pass {
      Cull Back
      ZTest LEqual
    
      CGPROGRAM
      #pragma multi_compile __ BOUNDARY_BLEND
      #pragma multi_compile __ CAFE_LIGHT PORTAL WATER WAVY PLASMA GLOW GLITCH
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define TOP_HEMISPHERE 1
      #include "HyperCore.cginc"
      ENDCG
    }
  }

  //Shadow Volume Shader
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
      #define BOTTOM_HEMISPHERE 1
      #include "HyperCore.cginc"
      ENDCG
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
      #define TOP_HEMISPHERE 1
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
      #pragma multi_compile __ BOUNDARY_BLEND
      #pragma multi_compile __ CAFE_LIGHT PORTAL WATER WAVY PLASMA GLOW
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define BOTTOM_HEMISPHERE 1
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
      #pragma multi_compile __ BOUNDARY_BLEND
      #pragma multi_compile __ CAFE_LIGHT PORTAL WATER WAVY PLASMA GLOW
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define TOP_HEMISPHERE 1
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
      #pragma multi_compile __ CAFE_LIGHT PORTAL WATER WAVY PLASMA GLOW IGNORE_TEX_COLOR GLITCH
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define BOTTOM_HEMISPHERE 1
      #define TRANSPARENT 1
      #include "HyperCore.cginc"
      ENDCG
    }
    Pass {
      Cull Back
      ZTest LEqual
      ZWrite Off
      Blend [_SrcMode] [_DstMode], [_SrcModeA] [_DstModeA]
    
      CGPROGRAM
      #pragma multi_compile __ CAFE_LIGHT PORTAL WATER WAVY PLASMA GLOW GLITCH
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define TOP_HEMISPHERE 1
      #define TRANSPARENT 1
      #include "HyperCore.cginc"
      ENDCG
    }
  }

  //Overlay Shader
  SubShader{
    Tags{
      "Queue" = "Transparent+100"
      "HyperRenderType" = "Overlay"
      "LightMode" = "ForwardBase"
    }
    Pass{
      Cull Back
      Lighting Off
      ZWrite Off
      ZTest Always
      Blend [_SrcMode] [_DstMode]

      CGPROGRAM
      #pragma multi_compile __ IGNORE_TEX_COLOR
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #define BOTTOM_HEMISPHERE 1
      #define TRANSPARENT 1
      #include "HyperCore.cginc"
      ENDCG
    }
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
      #define BOTTOM_HEMISPHERE 1
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
      #define BOTTOM_HEMISPHERE 1
      #define HOLO 1
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
  CustomEditor "HyperbolicEditor"
}
