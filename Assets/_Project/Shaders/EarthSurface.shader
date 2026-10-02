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
            Name "ForwardBase"
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                float4 tangent : TEXCOORD4;
            };

            sampler2D _MainTex;
            sampler2D _WaterMask;
            sampler2D _BumpMap;
            sampler2D _BordersTex;
            sampler2D _RegionIdTex;

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

            uniform float4 _LightColor0;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.tangent = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
                o.viewDir = normalize(_WorldSpaceCameraPos.xyz - o.worldPos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                fixed4 dayColor = tex2D(_MainTex, uv) * _DayColor * _Color;
                fixed4 waterMask = tex2D(_WaterMask, uv);
                float isWater = waterMask.r;

                // Distância da câmera para ativar resolução de solo (escala 50m)
                float camDist = length(_WorldSpaceCameraPos.xyz - i.worldPos);

                // Normal macro global da Terra
                half3 normalMap = UnpackNormal(tex2D(_BumpMap, uv));
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

                    fixed3 vegetColor = dayColor.rgb * fixed3(0.82, 1.18, 0.78) * colorMod;
                    fixed3 desertColor = dayColor.rgb * fixed3(1.12, 1.04, 0.88) * colorMod;

                    fixed3 detailedLand = lerp(dayColor.rgb * colorMod, vegetColor, isGreen);
                    detailedLand = lerp(detailedLand, desertColor, isDesert);

                    dayColor.rgb = lerp(dayColor.rgb, detailedLand, (1.0 - isWater) * mesoFactor);
                }

                // Cálculo do vetor normal em espaço mundial com base nas tangentes reais da esfera
                half3 worldTangent = i.tangent.xyz;
                half3 worldBinormal = cross(i.worldNormal, worldTangent) * i.tangent.w;
                half3 worldNorm = normalize(normalMap.x * worldTangent + normalMap.y * worldBinormal + normalMap.z * i.worldNormal);

                // Iluminação solar direcional protegida contra vetor nulo
                half3 lightDir = length(_WorldSpaceLightPos0.xyz) > 0.001 
                               ? normalize(_WorldSpaceLightPos0.xyz) 
                               : normalize(float3(0.5, 0.8, 0.3));
                half ndotl = max(0.0, dot(worldNorm, lightDir));

                // Reflexo especular (Sun glint nos oceanos)
                half3 h = normalize(lightDir + i.viewDir);
                half ndoth = max(0.0, dot(worldNorm, h));
                float smoothness = lerp(_LandSmoothness, _OceanSmoothness, isWater);
                float specPower = exp2(10.0 * smoothness + 1.0);
                float specular = pow(max(0.0, ndoth), specPower) * isWater * ndotl;

                // Atmosfera / Efeito Fresnel no horizonte do planeta
                half vdotn = 1.0 - saturate(dot(i.viewDir, worldNorm));
                half rim = pow(vdotn, _AtmospherePower) * _AtmosphereIntensity;
                // Diminui a atmosfera de borda quando a câmera está muito próxima do solo (escala de 50m)
                float closeGroundFactor = saturate(camDist / 100000.0);
                fixed4 atmosphere = _AtmosphereColor * rim * closeGroundFactor;

                // Iluminação ambiente espacial (não deixa o lado escuro 100% invisível)
                half3 ambient = ShadeSH9(float4(worldNorm, 1.0));
                if (length(ambient) < 0.05)
                    ambient = half3(0.04, 0.04, 0.07);

                fixed3 lightCol = _LightColor0.rgb;
                if (length(lightCol) < 0.01)
                    lightCol = half3(1.0, 0.98, 0.95);

                fixed3 diffuse = dayColor.rgb * (ndotl * lightCol + ambient);
                fixed3 spec = specular * _OceanColor.rgb * 2.0 * lightCol;
                fixed3 atmos = atmosphere.rgb * saturate(ndotl + 0.25);

                fixed3 finalColor = diffuse + spec + atmos;

                // Overlay de Linhas de Fronteira (Países e Estados/Províncias)
                fixed4 borderSample = tex2D(_BordersTex, uv);
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
                    fixed4 idPixel = tex2Dlod(_RegionIdTex, float4(snapUV, 0.0, 0.0));
                    
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

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }

    Fallback "Standard"
}
