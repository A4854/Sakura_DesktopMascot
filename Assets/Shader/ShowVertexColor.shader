Shader "Custom/ShowVertexColor"
{
    Properties
    {
        _r ("R", Range(0, 1)) = 1
        _g ("G", Range(0, 1)) = 1
        _b ("B", Range(0, 1)) = 1
        _a ("A", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex: POSITION;
                float4 color: COLOR;
            };

            struct v2f
            {
                float4 vertex: SV_POSITION;
                float4 color: COLOR;
            };
            
            float _r;
            float _g;
            float _b;
            float _a;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i): SV_Target
            {
                float4 col = i.color;
                col.r *= _r;
                col.g *= _g;
                col.b *= _b;
                col.a = lerp(1, col.a, _a);
                return col;
            }
            ENDCG
            
        }
    }
}
