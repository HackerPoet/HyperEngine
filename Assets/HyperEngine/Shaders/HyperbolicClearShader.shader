Shader "Custom/HyperbolicClearShader" {
  Properties{
    _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
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
      ZWrite On
      Blend SrcAlpha OneMinusSrcAlpha
    
      CGPROGRAM
      #pragma multi_compile_instancing
      #pragma vertex vert
      #pragma fragment frag
      #include "Clear.cginc"
      ENDCG
    }
  }
  CustomEditor "HyperbolicEditor"
}
