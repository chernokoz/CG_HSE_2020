Shader "0_Custom/Cubemap"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0, 0, 0, 1)
        _Roughness ("Roughness", Range(0.03, 1)) = 1
        _Cube ("Cubemap", CUBE) = "" {}
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
            #include "UnityLightingCommon.cginc"
            
            #define EPS 1e-7

            struct appdata
            {
                float4 vertex : POSITION;
                fixed3 normal : NORMAL;
            };

            struct v2f
            {
                float4 clip : SV_POSITION;
                float4 pos : TEXCOORD1;
                fixed3 normal : NORMAL;
            };

            float4 _BaseColor;
            float _Roughness;
            
            samplerCUBE _Cube;
            half4 _Cube_HDR;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.clip = UnityObjectToClipPos(v.vertex);
                o.pos = mul(UNITY_MATRIX_M, v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            uint Hash(uint s)
            {
                s ^= 2747636419u;
                s *= 2654435769u;
                s ^= s >> 16;
                s *= 2654435769u;
                s ^= s >> 16;
                s *= 2654435769u;
                return s;
            }
            
            float Random(uint seed)
            {
                return float(Hash(seed)) / 4294967295.0; // 2^32-1
            }
            
            float3 SampleColor(float3 direction)
            {   
                half4 tex = texCUBE(_Cube, direction);
                return DecodeHDR(tex, _Cube_HDR).rgb;
            }
            
            float Sqr(float x)
            {
                return x * x;
            }
            
            // Calculated according to NDF of Cook-Torrance
            float GetSpecularBRDF(float3 viewDir, float3 lightDir, float3 normalDir)
            {
                float3 halfwayVector = normalize(viewDir + lightDir);               
                
                float a = Sqr(_Roughness);
                float a2 = Sqr(a);
                float NDotH2 = Sqr(dot(normalDir, halfwayVector));
                
                return a2 / (UNITY_PI * Sqr(NDotH2 * (a2 - 1) + 1));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.normal);
                
                float3 viewDirection = normalize(_WorldSpaceCameraPos - i.pos.xyz);
                
                // Replace this specular calculation by Montecarlo.
                // Normalize the BRDF in such a way, that integral over a hemysphere of (BRDF * dot(normal, w')) == 1
                // TIP: use Random(i) to get a pseudo-random value.

                int n = 3000;
                float3 sum = float3(0, 0, 0);
                float3 sum1 = float3(0, 0, 0);

                float alpha;
                float cosTheta;
                float sinTheta;
                float3 local_direction;
                float3 absolute_direction;
                float3 direction;

                float3 new_y = normal;
                float3 new_x = normalize(cross(normal, float3(0, 1, 0)));
                float3 new_z = cross(new_x, new_y);

                for (int i = 0; i < n; i++)
                {
                    alpha = Random(i) * 2 * UNITY_PI;
                    cosTheta = Random(i + n);
                    sinTheta = sqrt(1 - Sqr(cosTheta));
                    local_direction.x = sinTheta * cos(alpha);
                    local_direction.y = cosTheta;
                    local_direction.z = sinTheta * sin(alpha);
                    absolute_direction = local_direction.x * new_x + local_direction.y * new_y + local_direction.z * new_z;
                    direction = normalize(absolute_direction);
                    sum += SampleColor(direction) * GetSpecularBRDF(viewDirection, direction, normal) * cosTheta;
                    sum1 += GetSpecularBRDF(viewDirection, direction, normal) * cosTheta;
                }

                float3 res = sum  / sum1;


                return fixed4(res, 1);
            }
            ENDCG
        }
    }
}
