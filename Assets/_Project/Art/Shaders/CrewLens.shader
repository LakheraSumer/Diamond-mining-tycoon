Shader "DiamondMine/CrewLens"
{
    Properties
    {
        _Color ("Lens tint and opacity", Color) = (0.7,0.85,0.9,0.2)
        _MainTex ("Albedo", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.88
        _Metallic ("Metallic", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        ZWrite Off
        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness,_Metallic;
        struct Input { float2 uv_MainTex; };
        void surf(Input i,inout SurfaceOutputStandard o)
        {
            fixed4 c=tex2D(_MainTex,i.uv_MainTex)*_Color;
            o.Albedo=c.rgb;o.Alpha=c.a;o.Metallic=_Metallic;o.Smoothness=_Glossiness;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
