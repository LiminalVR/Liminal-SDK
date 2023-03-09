// Diffuse shader which fades out when very close to the camera

Shader "Liminal/Controller" {
Properties {
    _MainTex ("Base (RGB)", 2D) = "white" {}
    _DitherMin("_DitherMin", Range(0,7)) = 0.3
    _DitherMax("_DitherMax", Range(0,7)) = 2
}
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 150


CGPROGRAM
#pragma surface surf Lambert noforwardadd

#include "Includes/Dither.cginc"

sampler2D _MainTex;

struct Input {
    float2 uv_MainTex;
    float4 screenPos;
    float3 worldPos;
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
    o.Albedo = c.rgb;
    o.Alpha = c.a;
    ClipByDither(IN.screenPos, IN.worldPos);
}

ENDCG
}

Fallback "Mobile/VertexLit"
}
