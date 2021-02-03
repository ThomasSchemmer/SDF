Shader "custom/SDF"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ShapeColor("Shape Color", Color) = (0,0,0,0)
        _ShadowColor("Shadow Color", Color) = (0,0,0,0)
        _BounceColor("Bounce Color", Color) = (0,0,0,0)
        [HideInInspector]
        _CameraPosition("Cam Position", Vector) = (0,0,0,0)
        [HideInInspector]
        _CameraDirection("Cam Direction", Vector) = (0,0,0,0)
        [HideInInspector]
        _SunDirection("Sun Direction", Vector) = (0,0,0,0)
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
                float4 dir : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 dir : TANGENT;

            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _CameraPosition, _CameraDirection, _SunDirection;
            float4 _ShapeColor, _ShadowColor, _BounceColor;
            float4x4 rotMatrix;


            float3 rotateAboutAxis(float3 In)
            {
                return mul(rotMatrix, In);
            }


            float sphere(fixed3 p, float s)
            {
                return length(p) - s;
            }

            float cone(fixed3 p, float h, float r1, float r2)
            {
                fixed2 q = fixed2(length(p.xz), p.y);
                fixed2 k1 = fixed2(r2, h);
                fixed2 k2 = fixed2(r2 - r1, 2.0 * h);
                fixed2 ca = fixed2(q.x - min(q.x, (q.y < 0.0) ? r1 : r2), abs(q.y) - h);
                fixed2 cb = q - k1 + k2 * clamp(dot(k1 - q, k2) / dot(k2, k2), 0.0, 1.0);
                float s = (cb.x < 0.0 && ca.y < 0.0) ? -1.0 : 1.0;
                float d = s * sqrt(min(dot(ca, ca), dot(cb, cb)));

                return d;
            }

            float cylinder(fixed3 p, float ra, float rb, float h)
            {
                fixed2 d = fixed2(length(p.xz) - 2.0 * ra + rb, abs(p.y) - h);
                return min(max(d.x, d.y), 0.0) + length(max(d, 0.0)) - rb;
            }

            float box(fixed3 p, fixed3 b)
            {
                fixed3 q = abs(p) - b;
                return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
            }

            float roundBox(fixed3 p, fixed3 b, fixed r)
            {
                fixed3 q = abs(p) - b;
                return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0) - r;
            }

            float column(fixed3 p) {

                float rad = 0.5;
                rad -= 0.02 * (0.5 + 0.5 * sin(16 * atan2(p.x, p.z)));
                float d = cone(p, 4, rad, rad - 0.2);
                d = min(d, cylinder(p + fixed3(0, 0.2, 0), 0.25, 0.05, 0.015));
                d = min(d, cylinder(p + fixed3(0, 0.4, 0), 0.3, 0.1, 0.05));
                d = min(d, cylinder(p + fixed3(0, 0.5, 0), 0.4, 0.05, 0.02));
                d = min(d, cylinder(p - fixed3(0, 4, 0), 0.175, 0.05, 0.01));
                d = min(d, cylinder(p - fixed3(0, 4.1, 0), 0.17, 0.05, 0.01));
                float off = rad - 0.1 + ((p.y - 4.35) * 0.5) / 2;
                d = min(d, box(p - fixed3(0, 4.35, 0), fixed3(min(0.4, off), 0.2, min(off * 1.1, 0.5))));
                d = min(d, box(p - fixed3(0, 4.5, 0), fixed3(0.4, 0.05, 0.5)));
                return d;
            }

            float prism(fixed3 p, fixed2 h)
            {
                fixed3 q = abs(p);
                return max(q.z - h.y, max(q.x * 0.866025 + p.y * 0.5, -p.y) - h.x * 0.5);
            }


            float roof(fixed3 p) {

                float rad =  0.02*(0.5 + 0.5 * sin(256 * atan2(p.x, p.z))); 
                float d = box(p - fixed3(rad, 4.75, rad), fixed3(10.5, 0.25, 6.5)) ;
                d = min(d, d);
                return d;
            }

            fixed opRepColumn(in fixed3 p, in float c, in fixed3 l)
            {
                fixed3 q = p - c * clamp(round(p / c), -l, l);
                return column(q);
            }

            fixed opRepRB(in fixed3 p, in float c, in fixed3 l)
            {
                fixed3 q = p - c * clamp(round(p / c), -l, l);
                return roundBox(q, fixed3(0.7, 0.2, 0.7), 0.05);
            }

            float map(fixed3 p) {
                float dg = p.y + 1.5;
                //float d = column(p);
                float d = opRepColumn(p, 2, fixed3(5, 0, 3));
                d = max(d, -box(p, fixed3(9, 5, 5)));
                d = min(d, opRepRB(p + fixed3(0, 0.75, 0), 1.5, fixed3(7, 0, 4)));
                d = min(d, opRepRB(p + fixed3(0, 1.25, 0), 1.5, fixed3(8, 0, 5)));
                d = min(d, box(p, fixed3(7, 4.5, 3)));
                d = min(d, roof(p));
                d = min(d, dg);
                return d;

            }

            fixed4 normal(fixed3 p) {
                fixed2 off = fixed2(0.001, 0);
                fixed gx = map(p + off.xyy) - map(p - off.xyy);
                fixed gy = map(p + off.yxy) - map(p - off.yxy);
                fixed gz = map(p + off.yyx) - map(p - off.yyx);
                return normalize(fixed4(gx, gy, gz, 0));
            }

            fixed castRay(fixed3 ro, fixed3 rd) {
                fixed t = 0;
                fixed3 pos;
                for (int i = 0; i < 100; i++) {
                    pos = ro + t * rd;
                    fixed d = map(pos);
                    if (t > 50)
                        break;
                    if (d < 0.001)
                        break;
                    t += d;
                }
                if (t > 50)
                    return -1;
                return t;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.dir = v.dir;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                //-0.5 to 0.5
                float2 p = (input.uv - fixed2(0.5, 0.5)) * 2;

                fixed t = castRay(_CameraPosition, input.dir);

                float4 color = fixed4(0.2, 0.2, 0.8, 1);                //base color
                color -= fixed4(0.7, 0.7, 0.7, 1) * input.dir.y;        //horizon

                if (t > 0) {
                    //base shapes
                    color = _ShapeColor;                  
                    fixed3 pos = _CameraPosition + t * input.dir;
                    fixed3 norm = normal(pos);
                    //sun highlight
                    fixed sh = clamp(dot(norm, -_SunDirection), 0, 1);   
                    color = sh * color + (1 - sh) * _ShadowColor;
                    //shadow cast
                    fixed sc = step(castRay(pos + norm * 0.001, -_SunDirection), 0.0);
                    color = sc * color + (1-sc) * _ShadowColor;                                   
                    //bounce light
                    color += _BounceColor * clamp(dot(norm, fixed3(0.0, -1.0, 0.0)), 0, 1);    
                   
                }

                return color;
            }
            ENDCG
        }
    }
}
