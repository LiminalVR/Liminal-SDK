// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Reflective/Diffuse" 
{
    Properties 
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _RampTex("Ramp", 2D) = "white" {}
        _ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
        _MainTex ("Base (RGB) RefStrength (A)", 2D) = "white" {}
        _Cube ("Reflection Cubemap", Cube) = "_Skybox" {}
        _ColorHorizon("Horizon Color", Color) = (0.5, 0.5, 0.5, 1)

        [Normal] _RippleTex("Ripple", 2D) = "white" {}

        _RippleSpeed("Ripple Speed", Float) = 0
        _RippleStrength("Ripple Strength", Float) = 0
        _Emission("Emission", Float) = 1.5
        _RippleDensity("Ripple Density", Float) = 0.5
    }
    SubShader 
    {
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Transparent"
        }

        ZWrite Off
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM

        #pragma surface surf Lambert no alpha:fade

        sampler2D _MainTex;
        samplerCUBE _Cube;

        sampler2D _RippleTex;
        float4 _RippleTex_ST;

        sampler2D _RampTex;
        float4 _RampTex_ST;

        fixed4 _Color;
        fixed4 _ReflectColor;
        fixed4 _ColorHorizon;

        fixed _RippleSpeed;
        fixed _RippleStrength;
        fixed _FadeDistance;
        fixed _FadeScaleX;
        fixed _Emission;
        fixed _RippleDensity;

        struct Input 
        {
            float2 uv_MainTex;
            float3 worldRefl;
            float2 uvRipple : TEXCOORD1;
            float2 uvRamp : TEXCOORD2;
        };

        void surf (Input IN, inout SurfaceOutput o) 
        {
            // This also work, not sure what's the difference.
            //IN.worldRefl.xz

            float2 rippleUv = TRANSFORM_TEX(IN.uv_MainTex, _RippleTex);
            rippleUv.xy *= -_RippleDensity;

            float2 uvr = rippleUv + float2(0, _Time.x * _RippleSpeed);
            fixed3 nrm = UnpackNormal(tex2D(_RippleTex, uvr));

            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);

            IN.worldRefl.xy += nrm.r * _RippleStrength;
            fixed4 reflcol = texCUBE (_Cube, IN.worldRefl);

            o.Emission = reflcol.rgb * _ReflectColor.rgb * _Emission;
            o.Alpha = reflcol.a * _ReflectColor.a;

            IN.uvRamp = TRANSFORM_TEX(IN.uv_MainTex, _RippleTex);

            fixed4 ramp = tex2D(_RampTex, IN.uvRamp);
            o.Emission = lerp(o.Emission, _ColorHorizon, 1 - ramp);
            o.Alpha *= ramp.rgb;

            //fixed2 delta = _WorldSpaceCameraPos.xz - i.worldPos.xz;
            //fixed dist = length(fixed2(delta.x * _FadeScaleX, delta.y));
        }

        ENDCG
    }
}
