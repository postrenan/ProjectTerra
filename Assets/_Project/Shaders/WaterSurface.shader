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
            Name "ForwardBase"
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            fixed4 _ShallowColor;
            fixed4 _DeepColor;
            fixed4 _FoamColor;
            float _WaveHeight;
            float _WaveSpeed;
            float _Glossiness;
            float _FresnelPower;
            float _FlowSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD3;
                float waveFactor : TEXCOORD4;
                LIGHTING_COORDS(5, 6)
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float time = _Time.y * _WaveSpeed;

                // Ondas dinâmicas de Gerstner/senoidais na GPU (idênticas à simulação CPU do WaterSystem)
                float w1 = sin((worldPos.x * 0.08 + worldPos.z * 0.05) + time * 1.5) * _WaveHeight;
                float w2 = cos((worldPos.x * -0.04 + worldPos.z * 0.09) + time * 2.1) * (_WaveHeight * 0.55);
                float w3 = sin((worldPos.x * 0.15 - worldPos.z * 0.12) + time * 3.0) * (_WaveHeight * 0.22);
                float totalWave = w1 + w2 + w3;

                // Deslocamento vertical dos vértices
                worldPos.y += totalWave;

                // Derivadas analíticas para cálculo da normal perturbada da onda
                float dw_dx = (0.08 * cos((worldPos.x * 0.08 + worldPos.z * 0.05) + time * 1.5) * _WaveHeight)
                            - (-0.04 * sin((worldPos.x * -0.04 + worldPos.z * 0.09) + time * 2.1) * (_WaveHeight * 0.55))
                            + (0.15 * cos((worldPos.x * 0.15 - worldPos.z * 0.12) + time * 3.0) * (_WaveHeight * 0.22));

                float dw_dz = (0.05 * cos((worldPos.x * 0.08 + worldPos.z * 0.05) + time * 1.5) * _WaveHeight)
                            - (0.09 * sin((worldPos.x * -0.04 + worldPos.z * 0.09) + time * 2.1) * (_WaveHeight * 0.55))
                            - (0.12 * cos((worldPos.x * 0.15 - worldPos.z * 0.12) + time * 3.0) * (_WaveHeight * 0.22));

                float3 perturbedNormal = normalize(float3(-dw_dx, 1.0, -dw_dz));

                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.worldPos = worldPos;
                o.worldNormal = perturbedNormal;
                o.uv = v.uv;
                o.waveFactor = saturate((totalWave + _WaveHeight) / (2.0 * max(0.01, _WaveHeight)));

                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 N = normalize(i.worldNormal);

                // Micro-ondulações procedurais na superfície (dupla frequência com fluxo de rio se _FlowSpeed > 0)
                float2 flowOffset = float2(0.0, _Time.y * _FlowSpeed);
                float2 uv1 = i.worldPos.xz * 0.25 + float2(_Time.x * 0.7, _Time.x * 0.4) + flowOffset;
                float2 uv2 = i.worldPos.xz * 0.50 - float2(_Time.x * 0.5, _Time.x * 0.8) + flowOffset * 1.3;
                float microRipple = sin(uv1.x * 6.28) * cos(uv1.y * 6.28) + sin(uv2.x * 6.28) * cos(uv2.y * 6.28);
                N.xz += microRipple * 0.06;
                N = normalize(N);

                // Iluminação direta da fonte principal (Sol)
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 H = normalize(L + V);
                float NdotL = max(0.0, dot(N, L));
                float NdotH = max(0.0, dot(N, H));
                float NdotV = max(0.0, dot(N, V));

                // Efeito Fresnel: água parece mais refletiva em ângulos rasos
                float fresnel = pow(1.0 - NdotV, _FresnelPower);

                // Brilho especular do reflexo solar
                float specPower = 12.0 + _Glossiness * 180.0;
                float spec = pow(NdotH, specPower) * _Glossiness;
                float3 specularHighlight = spec * _LightColor0.rgb * 1.8;

                // Cor base da água interpolada entre águas rasas e profundas
                fixed4 waterColor = lerp(_ShallowColor, _DeepColor, saturate(0.35 + fresnel * 0.45));

                // Reflexo do céu e atmosfera
                float3 skyReflection = float3(0.55, 0.75, 0.98) * (fresnel * 0.65);
                waterColor.rgb += skyReflection;
                waterColor.rgb += specularHighlight;

                // Espuma branca na crista das ondas mais altas
                float foam = saturate((i.waveFactor - 0.78) / 0.22);
                waterColor.rgb = lerp(waterColor.rgb, _FoamColor.rgb, foam * _FoamColor.a);
                waterColor.a = max(waterColor.a, foam * 0.85);

                // Aplicação da atenuação de luz / sombras
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
                waterColor.rgb *= (0.35 + 0.65 * atten * NdotL);

                return waterColor;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}
