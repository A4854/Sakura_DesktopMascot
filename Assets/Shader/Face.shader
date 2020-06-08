Shader "Jason Ma/Face"
{
    Properties
    {
        [Queue] _RenderQueue ( "Queue", int) = 2001
        _MainTex ("Texture", 2D) = "white" { }
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Offset ("Offset", float) = -1
        _AlphaScale ("Alpha Scale", Range(0, 10)) = 1
    }
    SubShader
    {
        Tags { "LightMode" = "ForwardOnly" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100
        Offset [_Offset], 0
        ZWrite on
        
        Pass
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv: TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex: SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _AlphaScale;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            float4 frag(v2f i): SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv) * _Color;
                col.a *= _AlphaScale;
                col = saturate(col);
                return col;
            }
            ENDCG
            
        }
    }
    CustomEditor "JTRP.ShaderDrawer.LWGUI"
}
