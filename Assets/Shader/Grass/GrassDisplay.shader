Shader "custom/GrassDisplay"
{
    Properties
    {
        _GrassColor("Grass Color", Color) = (0.2, 0.7, 0.1, 1)
        _Emission("Emission", Color) = (0.5,0.5,0.5,1)
    }
        SubShader
    {
       
        //originally the plan was to have a simple square mesh and have a cutout texture for the rounded top of the grass
        //unfortunately one would have to disable ZWriting/testing for that, in which each blade would be written into others, blending them
        //fortunately, there is a better way. Simply merge the vertices together according to their height (done in the compute shader)
        //this way we save us a texture read and a lot of headaches
        Pass
        {
            Tags{ "LightMode" = "Deferred"}

            LOD 100
            Cull Off

            CGPROGRAM
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc" // for _LightColor0
            #pragma vertex vert
            #pragma fragment frag
            
            struct Vertex {
                float3 position;
                float3 normal;
                float2 uv;
            };

            StructuredBuffer<Vertex> vertices;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR0;
                float3 worldPos : TEXCOORD1;
            };

            //we are deferring shading, so we need to save a lot more than just color
            //see https://catlikecoding.com/unity/tutorials/rendering/part-13/
            struct FragmentOut {
                float4 gBuffer0 : SV_Target0;   //Diffuse color (RGB), occlusion (A)
                float4 gBuffer1 : SV_Target1;   //Sepcular (RGB), Roughness (A)
                float4 gBuffer2 : SV_Target2;   //World Space Normal (RGB), unused (A)
                float4 gBuffer3 : SV_Target3;   //Emission + Lighting + Lightmaps
            };

            sampler2D _ShadowMapCopy;
            float4 _ShadowMapCopy_ST;

            float4 _GrassColor;
            float4 _Emission;
            float3 _lightDirection;
            float3 _CameraPos;

            v2f vert(uint id : SV_VertexID)
            {
                //standard lambert lighting
                float3 worldNormal = UnityObjectToWorldNormal(vertices[id].normal);
                float nl = 1 - max(0, dot(worldNormal, _lightDirection));
                v2f o;
                o.vertex = UnityObjectToClipPos(float4(vertices[id].position.xyz, 1));
                o.normal = worldNormal;
                o.worldPos = mul(unity_ObjectToWorld, float4(vertices[id].position, 1));
                o.color = nl * _GrassColor;
                o.uv = vertices[id].uv;
                return o;
            }

            //use a copied shadowmap from our single light source
            //assumes 4 cascading shadows!
            //for more info see https://shahriyarshahrabi.medium.com/custom-shadow-mapping-in-unity-c42a81e1bbf8
            float calcReceivedShadow(float3 worldPos) {
                float depth = distance(worldPos,_CameraPos);
                float4 near = float4(depth >= _LightSplitsNear);
                float4 far = float4(depth < _LightSplitsFar);
                float4 weights = near * far;
                //look up relative positions for each of the 4 shadowMap divisions
                float3 shadowCord0 = mul(unity_WorldToShadow[0], float4(worldPos, 1)).xyz;
                float3 shadowCord1 = mul(unity_WorldToShadow[1], float4(worldPos, 1)).xyz;
                float3 shadowCord2 = mul(unity_WorldToShadow[2], float4(worldPos, 1)).xyz;
                float3 shadowCord3 = mul(unity_WorldToShadow[3], float4(worldPos, 1)).xyz;
                //only one entry in weights is 1, we merged all the branches
                float3 coord = shadowCord0 * weights.x +
                        shadowCord1 * weights.y +
                        shadowCord2 * weights.z +
                        shadowCord3 * weights.w;
                //coord contains depth value 
                float shadow = tex2D(_ShadowMapCopy, coord.xy).r;
                return shadow < coord.z;
            }

            FragmentOut frag(v2f i) 
            {

                float4 color = calcReceivedShadow(i.worldPos) * i.color;
                FragmentOut output;
                float a = 1;
                output.gBuffer0 = float4(color.xyz, 1);
                output.gBuffer1 = float4(0.5, 0.5, 0.5, 0.5);
                output.gBuffer2 = float4(i.normal * 0.5 + 0.5, 1);
                output.gBuffer3 = float4(_Emission.rgb, 1);
                return output;

            }
            ENDCG
        }
            
        //shadow caster, basically copied from unity examples
        //pass along vertex data (clipspace) to fragment shader, where unity does shadow stuff
        Pass
        {
            Tags{ "RenderType" = "Opaque" "LightMode" = "ShadowCaster"}
            LOD 100

            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster

            struct Vertex {
                float3 position;
                float3 normal;
                float2 uv;
            };

            StructuredBuffer<Vertex> vertices;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float3 color : COLOR0;
            };

            float4 _GrassColor;

            v2f vert(uint id : SV_VertexID)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(float4(vertices[id].position.xyz, 1));
                o.normal = vertices[id].normal;
                o.uv = vertices[id].uv;
                o.color = _GrassColor;
                return o;
            }

            fixed4 frag(v2f i) : COLOR
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }

    }
}
