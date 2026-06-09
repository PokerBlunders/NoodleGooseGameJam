Shader "Custom/TriplanarGround_WithShadows"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Tiling ("Tiling", Float) = 1.0
        _OffsetX ("Offset X", Float) = 0
        _OffsetY ("Offset Y", Float) = 0
        _OffsetZ ("Offset Z", Float) = 0
        _BlendSharpness ("Blend Sharpness", Range(0.1,4)) = 1.0
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 0.8   // how dark shadows are
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float4 shadowCoord : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            float4 _Color;
            float _Tiling, _OffsetX, _OffsetY, _OffsetZ, _BlendSharpness;
            float _NormalStrength, _Metallic, _Smoothness, _ShadowStrength;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                output.worldTangent = TransformObjectToWorldDir(input.tangentOS.xyz);
                output.worldBitangent = cross(output.worldNormal, output.worldTangent) * input.tangentOS.w;
                output.shadowCoord = TransformWorldToShadowCoord(output.worldPos);
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
                return n;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 worldNormal = normalize(input.worldNormal);
                float3 blend = abs(worldNormal);
                blend /= (blend.x + blend.y + blend.z);
                blend = pow(blend, _BlendSharpness);
                blend /= (blend.x + blend.y + blend.z);

                float3 worldPos = input.worldPos * _Tiling;
                worldPos.x += _OffsetX;
                worldPos.y += _OffsetY;
                worldPos.z += _OffsetZ;

                half3 albedo = (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, worldPos.zy).rgb * blend.x +
                                SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, worldPos.xz).rgb * blend.y +
                                SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, worldPos.xy).rgb * blend.z) * _Color.rgb;

                half4 nX = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, worldPos.zy);
                half4 nY = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, worldPos.xz);
                half4 nZ = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, worldPos.xy);
                half3 normalX = UnpackNormalScale(nX, _NormalStrength);
                half3 normalY = UnpackNormalScale(nY, _NormalStrength);
                half3 normalZ = UnpackNormalScale(nZ, _NormalStrength);
                half3 normalTS = (normalX * blend.x + normalY * blend.y + normalZ * blend.z);

                half3 worldTangent = normalize(input.worldTangent);
                half3 worldBitangent = normalize(input.worldBitangent);
                half3 worldNormal2 = normalize(input.worldNormal);
                half3x3 TBN = half3x3(worldTangent, worldBitangent, worldNormal2);
                half3 sampledNormal = normalize(mul(normalTS, TBN));
                half3 finalNormal = normalize(lerp(worldNormal2, sampledNormal, _NormalStrength));

                Light mainLight = GetMainLight(input.shadowCoord);
                half3 lightDir = mainLight.direction;
                half3 lightColor = mainLight.color;
                half shadowAttenuation = lerp(1.0, mainLight.shadowAttenuation, _ShadowStrength);

                half3 diffuse = saturate(dot(finalNormal, lightDir));
                half3 viewDir = normalize(_WorldSpaceCameraPos - input.worldPos);
                half3 reflectDir = reflect(-lightDir, finalNormal);
                half3 specular = pow(saturate(dot(reflectDir, viewDir)), 1 / (1 - _Smoothness + 0.001)) * _Metallic;

                half3 finalColor = albedo * diffuse * lightColor * shadowAttenuation + specular * lightColor * shadowAttenuation;
                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        // ShadowCaster pass – so objects using this shader can also cast shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}