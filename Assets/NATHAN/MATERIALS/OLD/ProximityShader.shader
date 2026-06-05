Shader "Custom/ProximitySurface"
{
    Properties
    {
        _ParticleTex ("Particle Texture", 2D) = "white" {}
        _Tiling ("Tiling", float) = 2.0
        _StartDistance ("Fade Start Distance", float) = 1.0
        _EndDistance ("Fade End Distance", float) = 3.0
        _FadePower ("Fade Power", Range(0.5, 5.0)) = 1.0
        _Intensity ("Particle Intensity", float) = 1.5
        _BlendSharpness ("Blend Sharpness", Range(1, 16)) = 8   // NEW: Controls seam visibility
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

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            sampler2D _ParticleTex;
            float _Tiling, _StartDistance, _EndDistance, _FadePower, _Intensity, _BlendSharpness;
            float3 _PlayerPosition;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dist = distance(_PlayerPosition, i.worldPos);
                float fade = 1 - saturate((dist - _StartDistance) / (_EndDistance - _StartDistance));
                fade = pow(fade, _FadePower);
                if (fade < 0.005) return fixed4(0,0,0,1);

                // Triplanar with sharper blend
                float3 worldPos = i.worldPos * _Tiling;
                float3 blendWeight = abs(i.worldNormal);
                // Sharper transition using power and then normalize
                blendWeight = pow(blendWeight, _BlendSharpness);
                blendWeight /= (blendWeight.x + blendWeight.y + blendWeight.z);

                // Sample each plane – add a small random offset per pixel to break repetition? Not needed.
                fixed4 texX = tex2D(_ParticleTex, worldPos.zy);
                fixed4 texY = tex2D(_ParticleTex, worldPos.xz);
                fixed4 texZ = tex2D(_ParticleTex, worldPos.xy);
                
                fixed4 triplanarTex = texX * blendWeight.x + texY * blendWeight.y + texZ * blendWeight.z;

                fixed4 col = fixed4(0,0,0,1);
                col.rgb = triplanarTex.rgb * _Intensity * fade;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}