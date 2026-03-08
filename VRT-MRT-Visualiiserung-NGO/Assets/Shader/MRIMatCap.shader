Shader "Medical/Clinical_MatCap"
{
    Properties
    {
        _MainTex ("MatCap Texture (Sphere)", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
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

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 capUV : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                
                // Magic happens here: Convert normal to View Space
                float3 worldNorm = UnityObjectToWorldNormal(v.normal);
                float3 viewNorm = mul((float3x3)UNITY_MATRIX_V, worldNorm);
                
                // Map Normal (-1 to 1) to UV space (0 to 1)
                o.capUV = viewNorm.xy * 0.5 + 0.5;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.capUV);
                return tex * _Color;
            }
            ENDCG
        }
    }
}