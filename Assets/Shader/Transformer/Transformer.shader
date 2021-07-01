Shader "Custom/TransformerShader"
{
    Properties
    {
        _OriginTex("Origin Texture", 2D) = "white" {}
        _TargetTex("Target Texture", 2D) = "white"{}
        _T("T", Range(0, 1.1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent"  "Queue"="Transparent"}
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
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 alpha : TEXCOORD1;

            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 alpha : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _OriginTex, _TargetTex;
            float4 _OriginTex_ST, _TargetTex_ST, _MainTex_ST;
            float _T;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _OriginTex);
                o.alpha = TRANSFORM_TEX(v.alpha, _OriginTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 colOrig = tex2D(_OriginTex, i.uv);
                fixed4 colTarget = tex2D(_TargetTex, i.uv);
                fixed4 color = lerp(colOrig, colTarget, _T);
                color.a = i.alpha.x;
                return color;
            }
            ENDCG
        }
    }
}
