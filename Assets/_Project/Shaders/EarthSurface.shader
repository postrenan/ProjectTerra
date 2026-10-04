Shader "ProjectTerra/EarthSurface"
{
    Properties
    {
        [Header(Color and Surface)]
        _Color ("Main Tint", Color) = (1, 1, 1, 1)
        _DayColor ("Day Color Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo Map (Day)", 2D) = "white" {}
        
        [Header(Water Mask)]
        _WaterMask ("Water / Specular Mask", 2D) = "black" {}
        _OceanSmoothness ("Ocean Smoothness", Range(0, 1)) = 0.92
        _LandSmoothness ("Land Smoothness", Range(0, 1)) = 0.15
        _OceanColor ("Ocean Deep Tint", Color) = (0.02, 0.15, 0.35, 1)

        [Header(Topography Normals)]
        _BumpMap ("Normal / Relief Map", 2D) = "bump" {}
        _BumpScale ("Bump Scale", Range(0, 5)) = 1.0

        [Header(Atmosphere Glow)]
        _AtmosphereColor ("Atmosphere Glow Tint", Color) = (0.35, 0.65, 1.0, 1)
        _AtmospherePower ("Atmosphere Rim Power", Range(0.5, 10)) = 3.5
        _AtmosphereIntensity ("Atmosphere Intensity", Range(0, 5)) = 1.5

        [Header(Ground Detail at 50m)]
        _DetailTiling ("Micro Detail Tiling", Float) = 4000.0
        _DetailNormalStrength ("Micro Normal Strength", Range(0, 2)) = 0.75
        _DetailColorStrength ("Micro Color Variation", Range(0, 1)) = 0.22

        [Header(Political Borders)]
        _BordersTex ("Political Borders (Alpha Overlay)", 2D) = "black" {}
        _BordersColor ("Borders Color Tint", Color) = (1, 0.95, 0.6, 1)
        _BordersFadeStart ("Borders Fade Start (km)", Float) = 22000.0
        _BordersFadeEnd ("Borders Fade Full (km)", Float) = 14000.0

        [Header(Selected Region Highlight)]
        _RegionIdTex ("Region ID Map (RGB)", 2D) = "black" {}
        _SelectedRegionId ("Selected Region ID", Float) = 0.0
        _SelectedHighlightColor ("Selected Highlight Color", Color) = (0.2, 0.8, 1.0, 0.5)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma multi_compile _ _NORMALMAP
            #pragma multi_compile _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_B
            #pragma multi_compile _ _METALLIC_TEXTURE_ALBEDO_CHANNEL_A _METALLIC_TEXTURE_ALBEDO_CHANNEL_B
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityCG.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
                float fogFactor : TEXCOORD5;
            };

            TEXTURE2D(_MainTex);
            TEXTURE2D(_WaterMask);
            TEXTURE2D(_BumpMap);
            TEXTURE2D(_BordersTex);
            TEXTURE2D(_RegionIdTex);
            SAMPLER(sampler_MainTex);
            SAMPLER(sampler_WaterMask);
            SAMPLER(sampler_BumpMap);
            SAMPLER(sampler_BordersTex);
            SAMPLER(sampler_RegionIdTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _DayColor;
            float4 _OceanColor;
            float4 _AtmosphereColor;
            float4 _BordersColor;
            float4 _SelectedHighlightColor;
            float _SelectedRegionId;
            float _BordersFadeStart;
            float _BordersFadeEnd;
            float _OceanSmoothness;
            float _LandSmoothness;
            float _BumpScale;
            float _AtmospherePower;
            float _AtmosphereIntensity;
            float _DetailTiling;
            float _DetailNormalStrength;
            float _DetailColorStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = GetNormalizedNormalWS(vertexInput.normalOS, input.normalOS, input.tangentOS.xyz);
                output.tangentWS = float4(GetTangentWS(vertexInput.normalOS, input.normalOS, input.tangentOS), input.tangentOS.w);
                output.viewDirWS = normalize(_WorldSpaceCameraPos.xyz - output.positionWS);
                output.uv = input.uv;
                output.fogFactor = 0.0;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                half4 dayColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _DayColor * _Color;
                half4 waterMask = SAMPLE_TEXTURE2D(_WaterMask, sampler_WaterMask, uv);
                float isWater = waterMask.r;

                // Distância da câmera para ativar resolução de solo (escala 50m)
                float camDist = length(_WorldSpaceCameraPos.xyz - input.positionWS);

                // Normal macro global da Terra
                half3 normalMap = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv));
                normalMap.xy *= _BumpScale;

                // Transição contínua multi-escala estilo Google Maps:
                // Meso-escala (500 km a 20 km) e Micro-escala (20 km a 50m)
                float mesoFactor = saturate(1.0 - (camDist - 20000.0) / 480000.0);
                float microFactor = saturate(1.0 - (camDist - 50.0) / 30000.0);

                if (mesoFactor > 0.001)
                {
                    // Amostragem multi-frequência para solo e relevo
                    float2 microUV = uv * _DetailTiling;
                    float2 mesoUV = uv * (_DetailTiling * 0.08);
                    float2 waveUV = uv * (_DetailTiling * 1.8) + _Time.x * 0.35;

                    float n1 = sin(microUV.x * 3.1415) * cos(microUV.y * 3.1415);
                    float n2 = sin(microUV.x * 8.3 + microUV.y * 6.7) * 0.5;
                    float n3 = cos(microUV.x * 19.4 - microUV.y * 15.6) * 0.25;
                    float fineNoise = (n1 + n2 + n3);

                    float mesoNoise = sin(mesoUV.x * 6.28) * cos(mesoUV.y * 6.28) * 0.5 +
                                      cos(mesoUV.x * 13.7 + mesoUV.y * 10.9) * 0.35;

                    // Micro-normais adaptadas
                    half2 landMicroNorm = half2(cos(microUV.x * 6.28), sin(microUV.y * 6.28)) * _DetailNormalStrength;
                    half2 oceanMicroNorm = half2(sin(waveUV.x * 4.5), cos(waveUV.y * 4.5)) * (_DetailNormalStrength * 1.2);
                    half2 finalMicro = lerp(landMicroNorm, oceanMicroNorm, isWater);

                    normalMap.xy += finalMicro * microFactor;

                    // Classificação de bioma para enriquecer cores e evitar pixelização a 50m
                    float isGreen = saturate(dayColor.g * 1.5 - (dayColor.r + dayColor.b) * 0.75); // Vegetação / floresta
                    float isDesert = saturate((dayColor.r + dayColor.g) * 0.85 - dayColor.b * 1.8); // Árido / deserto

                    float colorMod = 1.0 + (fineNoise * _DetailColorStrength * microFactor) + (mesoNoise * 0.08 * mesoFactor);

                    half3 vegetColor = dayColor.rgb * half3(0.82, 1.18, 0.78) * colorMod;
                    half3 desertColor = dayColor.rgb * half3(1.12, 1.04, 0.88) * colorMod;

                    half3 detailedLand = lerp(dayColor.rgb * colorMod, vegetColor, isGreen);
                    detailedLand = lerp(detailedLand, desertColor, isDesert);

                    dayColor.rgb = lerp(dayColor.rgb, detailedLand, (1.0 - isWater) * mesoFactor);
                }

                // Cálculo do vetor normal em espaço mundial com base nas tangentes reais da esfera
                half3 worldTangent = input.tangentWS.xyz;
                half3 worldBinormal = cross(input.normalWS, worldTangent) * input.tangentWS.w;
                half3 worldNorm = normalize(normalMap.x * worldTangent + normalMap.y * worldBinormal + normalMap.z * input.normalWS);

                // Iluminação solar direcional
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half3 lightColor = mainLight.color;
                half ndotl = max(0.0, dot(worldNorm, lightDir));

                // Reflexo especular (Sun glint nos oceanos)
                half3 h = normalize(lightDir + input.viewDirWS);
                half ndoth = max(0.0, dot(worldNorm, h));
                float smoothness = lerp(_LandSmoothness, _OceanSmoothness, isWater);
                float specPower = exp2(10.0 * smoothness + 1.0);
                float specular = pow(max(0.0, ndoth), specPower) * isWater * ndotl;

                // Atmosfera / Efeito Fresnel no horizonte do planeta
                half vdotn = 1.0 - saturate(dot(input.viewDirWS, worldNorm));
                half rim = pow(vdotn, _AtmospherePower) * _AtmosphereIntensity;
                // Diminui a atmosfera de borda quando a câmera está muito próxima do solo (escala de 50m)
                float closeGroundFactor = saturate(camDist / 100000.0);
                half4 atmosphere = _AtmosphereColor * rim * closeGroundFactor;

                // Iluminação ambiente (SH)
                half3 ambient = SampleSH9(float4(worldNorm, 1.0));
                if (length(ambient) < 0.05)
                    ambient = half3(0.04, 0.04, 0.07);

                half3 diffuse = dayColor.rgb * (ndotl * lightColor + ambient);
                half3 spec = specular * _OceanColor.rgb * 2.0 * lightColor;
                half3 atmos = atmosphere.rgb * saturate(ndotl + 0.25);

                half3 finalColor = diffuse + spec + atmos;

                // Overlay de Linhas de Fronteira (Países e Estados/Províncias)
                half4 borderSample = SAMPLE_TEXTURE2D(_BordersTex, sampler_BordersTex, uv);
                if (borderSample.a > 0.005)
                {
                    float camDistKm = camDist * 0.001; // converter para km
                    float borderFade = saturate((_BordersFadeStart - camDistKm) / max(1.0, _BordersFadeStart - _BordersFadeEnd));
                    
                    // Manter traço de fronteira nítido sem borrar em baixa altitude
                    float borderAlpha = saturate((borderSample.a - 0.08) * 2.5);
                    half3 borderGlow = borderSample.rgb * _BordersColor.rgb;
                    finalColor = lerp(finalColor, borderGlow, borderAlpha * borderFade);
                }

                // Destaque visual do território selecionado (País, Província ou Estado)
                if (_SelectedRegionId > 0.5)
                {
                    // Amostragem rigorosamente pontual e exata no centro do texel (4096 x 2048)
                    // Elimina completamente qualquer interpolação bilinear ou vazamento entre regiões vizinhas
                    float2 idUV = frac(uv);
                    float2 snapUV = (floor(idUV * float2(4096.0, 2048.0)) + 0.5) / float2(4096.0, 2048.0);
                    half4 idPixel = SAMPLE_TEXTURE2D_LOD(_RegionIdTex, sampler_RegionIdTex, float4(snapUV, 0.0, 0.0));
                    
                    int sampledId = (int)round(idPixel.r * 255.0) + ((int)round(idPixel.g * 255.0) * 256);
                    int targetId = (int)round(_SelectedRegionId);
                    
                    if (sampledId == targetId && targetId > 0)
                    {
                        float pulse = 0.85 + 0.15 * sin(_Time.y * 3.5);
                        half3 hlColor = _SelectedHighlightColor.rgb * pulse;
                        float hlAlpha = _SelectedHighlightColor.a;
                        // Iluminação holográfica nítida no contorno exato do território
                        finalColor = lerp(finalColor, finalColor * hlColor * 2.2 + hlColor * 0.35, hlAlpha);
                    }
                }

                // Fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDCG
        }

        // ShadowCaster Pass para URP
        Pass
        {
            Name "UniversalShadowCaster"
            Tags { "LightMode"="UniversalShadowCaster" }

            CGPROGRAM
            #pragma vertex vert_shadow
            #pragma fragment frag_shadow
            #pragma multi_compile_shadowcaster
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityCG.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 positionWS : TEXCOORD0;
            };

            TEXTURE2D(_RegionIdTex);
            SAMPLER(sampler_RegionIdTex);
            float _SelectedRegionId;

            Varyings vert_shadow(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                return output;
            }

            float4 frag_shadow(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDCG
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}