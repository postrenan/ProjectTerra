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
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityCG.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float _Coverage;
            float _Density;
            float4 _ColorDay;
            float4 _ColorNight;
            float4 _ColorSunset;
            float4 _SunDirection;
            float _TimeOfDay;
            float2 _CloudOffset;
            float _CloudScale;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.worldPos = vertexInput.positionWS;
                output.worldNormal = GetNormalizedNormalWS(vertexInput.normalOS, input.normalOS, float3(0,0,0));
                output.uv = input.uv;
                output.fogFactor = 0.0;
                return output;
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

            half4 frag(Varyings input) : SV_Target
            {
                // UV baseado na posição mundial + offset do vento
                float2 uv = (input.worldPos.xz * _CloudScale) + _CloudOffset;

                // FBM (Fractional Brownian Motion) para nuvens
                float n = fbm(uv, 5);

                // Detalhe adicional mais rápido
                float detail = fbm(uv * 4.0 + _Time.y * 0.02, 3) * 0.3;

                float cloudValue = n + detail;

                // Soft threshold para bordas suaves
                float coverage = smoothstep(0.5 - _Coverage * 0.5, 0.5 + _Coverage * 0.5, cloudValue);
                float density = coverage * _Density;

                // Silver lining - iluminar bordas voltadas para o sol
                float3 viewDir = normalize(_WorldSpaceCameraPos - input.worldPos);
                float3 sunDir = normalize(_SunDirection.xyz);
                float sunDot = dot(input.worldNormal, sunDir);
                float viewDot = dot(input.worldNormal, viewDir);
                float rim = pow(max(0.0, sunDot), 8.0) * max(0.0, viewDot) * 0.3;

                // Cor baseada na hora do dia
                float hour = _TimeOfDay;
                float elevationFactor = sin((hour / 24.0) * 6.283185 - 1.5708);
                bool isDay = elevationFactor > 0.04;
                bool isGoldenHour = abs(elevationFactor) <= 0.28;

                half4 cloudColor;
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

                half4 col = cloudColor;
                col.rgb += rim;
                col.a = density;

                // Fog
                col.rgb = MixFog(col.rgb, input.fogFactor);

                return col;
            }
            ENDCG
        }
    }
    Fallback "Universal Render Pipeline/Particles/Lit"
}