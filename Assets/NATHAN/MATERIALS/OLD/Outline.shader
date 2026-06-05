Shader "Custom/URPOutlineWorldSpace"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width (world units)", Range(0, 0.5)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        // Outline pass (world-space extrusion)
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _OutlineWidth;
            float4 _OutlineColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Get world position and normal
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 worldNormal = TransformObjectToWorldNormal(input.normalOS);

                // Extrude along world normal by a fixed world-space distance
                float3 newWorldPos = worldPos + worldNormal * _OutlineWidth;

                // Transform back to clip space
                output.positionCS = TransformWorldToHClip(newWorldPos);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // Base color pass (same as before)
        Pass
        {
            Name "Base"
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            sampler2D _MainTex;
            float4 _Color;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = tex2D(_MainTex, input.uv) * _Color;
                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Simple Lit"
}