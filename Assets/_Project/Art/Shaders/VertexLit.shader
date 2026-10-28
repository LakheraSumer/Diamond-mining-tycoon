Shader "DiamondMine/VertexLit"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Lambert
        fixed4 _Color;
        struct Input
        {
            float4 color : COLOR;
        };
        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = IN.color.rgb * _Color.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
