Shader "ProjectTerra/Clouds/Procedural"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}
        _Coverage ("Cloud Coverage", Range(0, 1)) = 0.5
        _Density ("Cloud Density", Range(0, 1)) = 0.5
        _ColorDay ("Cloud Color Day", Color) = (0.95, 0.95, 0.98, 1)
        _ColorNight ("Cloud Color Night", Color) = (0.35, 0.38, 0.45, 1)
        _ColorSunset ("Cloud Color Sunset", Color) = (1.0, 0.65, 0.35, 1)
        _SunDirection ("Sun Direction", Vector) = (0, 1, 0, 0)
        _TimeOfDay ("Time of Day", Float) = 12
        _CloudOffset ("Cloud Offset", Vector) = (0, 0, 0, 0)
        _CloudScale ("Cloud Scale", Float) = 0.0001
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Front

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Coverage;
            float _Density;
            fixed4 _ColorDay;
            fixed4 _ColorNight;
            fixed4 _ColorSunset;
            float4 _SunDirection;
            float _TimeOfDay;
            float2 _CloudOffset;
            float _CloudScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = normalize(mul(v.normal, (float3x3)unity_ObjectToWorld));
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            // Perlin noise 2D simples
            float2 hash22(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(float2(dot(p, float2(12.9898, 78.233)), dot(p, float2(39.346, 11.135))));
            }

            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep

                float a = hash22(i).x;
                float b = hash22(i + float2(1, 0)).x;
                float c = hash22(i + float2(0, 1)).x;
                float d = hash22(i + float2(1, 1)).x;

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 1.0;
                float frequency = 1.0;
                float maxAmplitude = 0.0;

                for (int i = 0; i < octaves; i++)
                {
                    value += noise2D(p * frequency) * amplitude;
                    maxAmplitude += amplitude;
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }

                return value / maxAmplitude;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UV baseado na posição mundial + offset do vento
                float2 uv = (i.worldPos.xz * _CloudScale) + _CloudOffset;

                // FBM (Fractional Brownian Motion) para nuvens
                float n = fbm(uv, 5);

                // Detalhe adicional mais rápido
                float detail = fbm(uv * 4.0 + _Time.y * 0.02, 3) * 0.3;

                float cloudValue = n + detail;

                // Soft threshold para bordas suaves
                float coverage = smoothstep(0.5 - _Coverage * 0.5, 0.5 + _Coverage * 0.5, cloudValue);
                float density = coverage * _Density;

                // Silver lining - iluminar bordas voltadas para o sol
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 sunDir = normalize(_SunDirection.xyz);
                float sunDot = dot(i.worldNormal, sunDir);
                float viewDot = dot(i.worldNormal, viewDir);
                float rim = pow(max(0.0, sunDot), 8.0) * max(0.0, viewDot) * 0.3;

                // Cor baseada na hora do dia
                float hour = _TimeOfDay;
                float elevationFactor = sin((hour / 24.0) * 6.283185 - 1.5708);
                bool isDay = elevationFactor > 0.04;
                bool isGoldenHour = abs(elevationFactor) <= 0.28;

                fixed4 cloudColor;
                if (isGoldenHour)
                {
                    float t = 1.0 - abs(elevationFactor) / 0.28;
                    cloudColor = lerp(_ColorDay, _ColorSunset, t);
                }
                else if (isDay)
                {
                    cloudColor = _ColorDay;
                }
                else
                {
                    cloudColor = _ColorNight;
                }

                fixed4 col = cloudColor;
                col.rgb += rim;
                col.a = density;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}