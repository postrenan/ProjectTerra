Shader "ProjectTerra/WaterSurface"
{
    Properties
    {
        [Header(Water Colors)]
        _ShallowColor ("Shallow Water Tint", Color) = (0.16, 0.58, 0.72, 0.78)
        _DeepColor ("Deep Water Tint", Color) = (0.05, 0.18, 0.38, 0.92)
        _FoamColor ("Crest Foam Tint", Color) = (0.92, 0.96, 1.0, 0.85)

        [Header(Wave Dynamics)]
        _WaveHeight ("Wave Height", Float) = 0.35
        _WaveSpeed ("Wave Speed", Float) = 1.3
        _Glossiness ("Surface Smoothness", Range(0, 1)) = 0.92
        _FresnelPower ("Fresnel Exponent", Range(1, 6)) = 3.5
        _FlowSpeed ("River Flow Speed", Float) = 0.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float waveFactor : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
            half4 _ShallowColor;
            half4 _DeepColor;
            half4 _FoamColor;
            float _WaveHeight;
            float _WaveSpeed;
            float _Glossiness;
            float _FresnelPower;
            float _FlowSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = GetNormalizedNormalWS(vertexInput.normalOS, input.normalOS, float3(0,0,0));
                output.uv = input.uv;
                output.fogFactor = 0.0;

                float3 worldPos = output.positionWS;
                float time = _Time.y * _WaveSpeed;

                // Ondas dinâmicas de Gerstner/senoidais na GPU (idênticas à simulação CPU do WaterSystem)
                float w1 = sin((worldPos.x * 0.08 + worldPos.z * 0.05) + time * 1.5) * _WaveHeight;
                float w2 = cos((worldPos.x * -0.04 + worldPos.z * 0.09) + time * 2.1) * (_WaveHeight * 0.55);
                float w3 = sin((worldPos.x * 0.15 - worldPos.z * 0.12) + time * 3.0) * (_WaveHeight * 0.22);
                float totalWave = w1 + w2 + w3;

                // Deslocamento vertical dos vértices
                output.positionWS.y += totalWave;
                output.positionCS = TransformWorldToHClip(output.positionWS);

                // Derivadas analíticas para cálculo da normal perturbada da onda
                float dw_dx = (0.08 * cos((worldPos.x * 0.08 + worldPos.z * 0.05) + time * 1.5) * _WaveHeight)
                            - (-0.04 * sin((worldPos.x * -0.04 + worldPos.z * 0.09) + time * 2.1) * (_WaveHeight * 0.55))
                            + (0.15 * cos((worldPos.x * 0.15 - worldPos.z * 0.12) + time * 3.0) * (_WaveHeight * 0.22));

                float dw_dz = (0.05 * cos((worldPos.x * 0.08 + worldPos.z * 0.05) + time * 1.5) * _WaveHeight)
                            - (0.09 * sin((worldPos.x * -0.04 + worldPos.z * 0.09) + time * 2.1) * (_WaveHeight * 0.55))
                            - (0.12 * cos((worldPos.x * 0.15 - worldPos.z * 0.12) + time * 3.0) * (_WaveHeight * 0.22));

                float3 perturbedNormal = normalize(float3(-dw_dx, 1.0, -dw_dz));
                output.normalWS = perturbedNormal;
                output.waveFactor = saturate((totalWave + _WaveHeight) / (2.0 * max(0.01, _WaveHeight)));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 V = normalize(_WorldSpaceCameraPos - input.positionWS);
                float3 N = normalize(input.normalWS);

                // Micro-ondulações procedurais na superfície (dupla frequência com fluxo de rio se _FlowSpeed > 0)
                float2 flowOffset = float2(0.0, _Time.y * _FlowSpeed);
                float2 uv1 = input.positionWS.xz * 0.25 + float2(_Time.x * 0.7, _Time.x * 0.4) + flowOffset;
                float2 uv2 = input.positionWS.xz * 0.50 - float2(_Time.x * 0.5, _Time.x * 0.8) + flowOffset * 1.3;
                float microRipple = sin(uv1.x * 6.28) * cos(uv1.y * 6.28) + sin(uv2.x * 6.28) * cos(uv2.y * 6.28);
                N.xz += microRipple * 0.06;
                N = normalize(N);

                // Iluminação direta da fonte principal (Sol)
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 lightColor = mainLight.color;
                float3 H = normalize(L + V);
                float NdotL = max(0.0, dot(N, L));
                float NdotH = max(0.0, dot(N, H));
                float NdotV = max(0.0, dot(N, V));

                // Efeito Fresnel: água parece mais refletiva em ângulos rasos
                float fresnel = pow(1.0 - NdotV, _FresnelPower);

                // Brilho especular do reflexo solar
                float specPower = 12.0 + _Glossiness * 180.0;
                float spec = pow(NdotH, specPower) * _Glossiness;
                float3 specularHighlight = spec * lightColor * 1.8;

                // Cor base da água interpolada entre águas rasas e profundas
                half4 waterColor = lerp(_ShallowColor, _DeepColor, saturate(0.35 + fresnel * 0.45));

                // Reflexo do céu e atmosfera
                float3 skyReflection = float3(0.55, 0.75, 0.98) * (fresnel * 0.65);
                waterColor.rgb += skyReflection;
                waterColor.rgb += specularHighlight;

                // Espuma branca na crista das ondas mais altas
                float foam = saturate((input.waveFactor - 0.78) / 0.22);
                waterColor.rgb = lerp(waterColor.rgb, _FoamColor.rgb, foam * _FoamColor.a);
                waterColor.a = max(waterColor.a, foam * 0.85);

                // Aplicação da atenuação de luz / sombras
                float shadowAtten = 1.0; // URP usa shadow sampling diferente
                waterColor.rgb *= (0.35 + 0.65 * shadowAtten * NdotL);

                // Fog
                waterColor.rgb = MixFog(waterColor.rgb, input.fogFactor);

                return waterColor;
            }
            ENDCG
        }
    }
    Fallback "Universal Render Pipeline/Particles/Lit"
}