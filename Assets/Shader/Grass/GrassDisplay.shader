Shader "custom/GrassDisplay"
{
    Properties
    {
        _AlphaTex ("Texture", 2D) = "white" {}
        _GrassColor("Grass Color", Color) = (0.2, 0.7, 0.1, 1)
    }
        SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent"  "LightMode" = "ForwardBase" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag

            StructuredBuffer<float3> vertices;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;      
            };

            sampler2D _AlphaTex;
            float4 _AlphaTex_ST;
            float4 _GrassColor;

            v2f vert (uint id : SV_VertexID)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(float4(vertices[id].xyz, 1));
                //uv mapping is dependent on vertex id, see compute shader for reference
                o.uv = float2((id % 10) > 4 ? 0.0 : 1.0, (id % 5.0) / 4.0);
                o.uv = TRANSFORM_TEX(o.uv, _AlphaTex); 
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float a = tex2D(_AlphaTex, i.uv).w;
                return float4(_GrassColor.xyz, a);
            }
            ENDCG
        }

        // shadow caster rendering pass, implemented manually
        // using macros from UnityCG.cginc
        Pass
        {
            Tags {"LightMode" = "ShadowCaster"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
