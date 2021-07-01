Shader "Custom/ImageEffectShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        [HideInInspector]_Data("Data", 3D) = ""{}
        [HideInInspector]_BoundsMin("Min Bounds", Vector) = (0,0,0,0)
        [HideInInspector]_BoundsMax("Max Bounds", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewDir : NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BoundsMin, _BoundsMax;

            //returns (distance to box, distance inside box)
            float2 rayBoxDist(float3 rayOrigin, float3 rayDir) {
                float3 t0 = (_BoundsMin - rayOrigin) / rayDir;
                float3 t1 = (_BoundsMax - rayOrigin) / rayDir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);

                float dstA = max(max(tmin.x, tmin.y), tmin.z);
                float dstB = min(min(tmax.x, tmax.y), tmax.z);

                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);
                return float2(dstToBox, dstInsideBox);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.viewDir = normalize(mul(unity_ObjectToWorld, v.vertex) - _WorldSpaceCameraPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float2 info = rayBoxDist(_WorldSpaceCameraPos, i.viewDir);
                return info.y > 0 ? col : 0;
            }
            ENDCG
        }
    }
}
