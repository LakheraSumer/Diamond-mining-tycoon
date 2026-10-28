Shader "DiamondMine/TimberSurface"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Range(0,1)) = 0.45
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.25
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250
        CGPROGRAM
        #include "UnityStandardUtils.cginc"
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        #pragma multi_compile_instancing
        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        half _Metallic, _Glossiness, _BumpScale;
        half4 _EmissionColor;
        struct Input { float2 uv_MainTex; float2 uv_BumpMap; fixed4 contactAO; };
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.contactAO=v.color;
        }
        void surf(Input i, inout SurfaceOutputStandard o)
        {
            fixed4 c=tex2D(_MainTex,i.uv_MainTex)*_Color;
            o.Albedo=c.rgb;
            o.Normal=UnpackScaleNormal(tex2D(_BumpMap,i.uv_BumpMap),_BumpScale);
            o.Metallic=_Metallic;
            o.Smoothness=_Glossiness;
            o.Occlusion=lerp(0.22,1.0,i.contactAO.r);
            o.Albedo*=lerp(0.60,1.0,i.contactAO.r);
            o.Emission=_EmissionColor.rgb;
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
