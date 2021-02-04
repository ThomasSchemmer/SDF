// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Unlit/SnowflakeShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent"}
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geo
            #pragma fragment frag

            #include "UnityCG.cginc"


            struct v2g
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal   : NORMAL;
                float3 t : TANGENT;
            };

            struct g2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal   : NORMAL;
                float3 t : TANGENT;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float lifetimeMax;

            struct Particle {
                float3 position;
                float3 direction;
                float size;
                float lifetime;
            };

            StructuredBuffer<Particle> particles;

            v2g vert (uint instanceID : SV_InstanceID, uint vertexID : SV_VertexID){
                v2g o;
                o.vertex = float4(particles[vertexID].position, 1);
                o.normal = UnityObjectToClipPos(o.vertex);
                o.uv = fixed2(0,0);
                o.t = fixed3(particles[vertexID].size, particles[vertexID].lifetime, 0);
                return o;
            }


            [maxvertexcount(6)]
            void geo(point v2g IN[1], inout TriangleStream<g2f> triStream) {
                //world space camera view
                float3 normal = mul((float3x3)unity_CameraToWorld, float3(0, 0, 1));
                //object space camera view, inverted and normalized
                normal = -normalize(mul((float3x3)unity_WorldToObject, normal));
                float3 up = float3(0, 1, 0) * IN[0].t.x;
                float3 right = normalize(cross(normal, up)) * IN[0].t.x;
                //all posX are object space
                fixed3 pos[4];
                fixed2 uvs[4];
                pos[0] = IN[0].vertex - up - right;
                pos[1] = IN[0].vertex - up + right;
                pos[2] = IN[0].vertex + up - right;
                pos[3] = IN[0].vertex + up + right;
                uvs[0] = fixed2(0, 0);
                uvs[1] = fixed2(0, 1);
                uvs[2] = fixed2(1, 0);
                uvs[3] = fixed2(1, 1);
                g2f OUTs[4];
                OUTs[0].vertex = UnityObjectToClipPos(pos[0]);
                OUTs[0].uv = uvs[0];
                OUTs[0].normal = IN[0].normal;
                OUTs[0].t = IN[0].t;
                OUTs[1].vertex = UnityObjectToClipPos(pos[1]);
                OUTs[1].uv = uvs[1];
                OUTs[1].normal = IN[0].normal;
                OUTs[1].t = IN[0].t;
                OUTs[2].vertex = UnityObjectToClipPos(pos[2]);
                OUTs[2].uv = uvs[2];
                OUTs[2].normal = IN[0].normal;
                OUTs[2].t = IN[0].t;
                OUTs[3].vertex = UnityObjectToClipPos(pos[3]);
                OUTs[3].uv = uvs[3];
                OUTs[3].normal = IN[0].normal;
                OUTs[3].t = IN[0].t;
                triStream.Append(OUTs[1]);
                triStream.Append(OUTs[0]);
                triStream.Append(OUTs[2]);
                triStream.RestartStrip();
                triStream.Append(OUTs[1]);
                triStream.Append(OUTs[2]);
                triStream.Append(OUTs[3]);
                triStream.RestartStrip();

            }

            fixed4 frag(g2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);
                color.w *= clamp(i.t.y / lifetimeMax + 0.1, 0, 1);
                return color;
            }
            ENDCG
        }
    }
}
