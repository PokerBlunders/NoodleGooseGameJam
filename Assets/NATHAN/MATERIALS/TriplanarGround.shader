Shader "Custom/TriplanarGround_WithFog"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Tiling ("Tiling", Float) = 1.0
        _OffsetX ("Offset X", Float) = 0
        _OffsetY ("Offset Y", Float) = 0
        _OffsetZ ("Offset Z", Float) = 0
        _BlendSharpness ("Blend Sharpness", Range(0.1,4)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldTangent : TEXCOORD2;
                float3 worldBitangent : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            float4 _Color;
            float _Tiling, _OffsetX, _OffsetY, _OffsetZ, _BlendSharpness;
            float _NormalStrength, _Metallic, _Smoothness;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                output.worldTangent = TransformObjectToWorldDir(input.tangentOS.xyz);
                output.worldBitangent = cross(output.worldNormal, output.worldTangent) * input.tangentOS.w;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half3 UnpackNormalScale(half4 packed, half strength)
            {
                #if defined(UNITY_NO_DXT5nm)
                    half3 n = packed.rgb * 2.0 - 1.0;
                #else
                    half3 n;
                    n.xy = packed.ag * 2.0 - 1.0;
                    n.z = sqrt(1.0 - saturate(dot(n.xy, n.xy)));
                #endif
                n.xy *= strength;
                return n;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Shadow coordinate
                float4 shadowCoord = TransformWorldToShadowCoord(input.worldPos);
                Light mainLight = GetMainLight(shadowCoord);
                half shadowAtten = mainLight.shadowAttenuation;

                // Triplanar weights
                float3 worldNormal = normalize(input.worldNormal);
                float3 blend = abs(worldNormal);
                blend /= (blend.x + blend.y + blend.z);
                blend = pow(blend, _BlendSharpness);
                blend /= (blend.x + blend.y + blend.z);

                float3 worldPos = input.worldPos * _Tiling;
                worldPos.x += _OffsetX;
                worldPos.y += _OffsetY;
                worldPos.z += _OffsetZ;

                // Albedo
                half3 albedoX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, worldPos.zy).rgb;
                half3 albedoY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, worldPos.xz).rgb;
                half3 albedoZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, worldPos.xy).rgb;
                half3 albedo = (albedoX * blend.x + albedoY * blend.y + albedoZ * blend.z) * _Color.rgb;

                // Normal
                half4 nX = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, worldPos.zy);
                half4 nY = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, worldPos.xz);
                half4 nZ = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, worldPos.xy);
                half3 normX = UnpackNormalScale(nX, _NormalStrength);
                half3 normY = UnpackNormalScale(nY, _NormalStrength);
                half3 normZ = UnpackNormalScale(nZ, _NormalStrength);
                half3 normalTS = (normX * blend.x + normY * blend.y + normZ * blend.z);
                half3x3 TBN = half3x3(input.worldTangent, input.worldBitangent, worldNormal);
                half3 finalNormal = normalize(mul(normalTS, TBN));

                // Lighting
                half3 lightDir = mainLight.direction;
                half3 lightColor = mainLight.color;
                half3 diffuse = saturate(dot(finalNormal, lightDir));
                half3 viewDir = normalize(_WorldSpaceCameraPos - input.worldPos);
                half3 reflectDir = reflect(-lightDir, finalNormal);
                half3 specular = pow(saturate(dot(reflectDir, viewDir)), 1 / (1 - _Smoothness + 0.001)) * _Metallic;

                half3 finalColor = albedo * diffuse * lightColor * shadowAtten;
                finalColor += specular * lightColor * shadowAtten;

                // Apply fog
                half3 foggedColor = MixFog(finalColor, input.fogFactor);
                return half4(foggedColor, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            half4 frag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}