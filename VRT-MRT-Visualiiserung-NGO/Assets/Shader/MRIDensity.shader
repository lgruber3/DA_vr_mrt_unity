Shader "Medical/XRay_Density"
{
    Properties
    {
        _Color ("X-Ray Color", Color) = (0.2, 0.6, 1.0, 1)
        _Density ("Density Multiplier", Range(0.01, 2.0)) = 0.2
    }
    SubShader
    {
        // Transparent Queue, ignore Depth writing so we see "through"
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Blend One One // Additive Blending: Color = Source + Destination

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
                float intensity : TEXCOORD0;
            };

            fixed4 _Color;
            float _Density;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                
                // Calculate view angle: perpendicular surfaces look thinner
                float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
                o.intensity = saturate(dot(v.normal, viewDir));
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Return a very dim color that "stacks" to become bright
                return _Color * i.intensity * _Density;
            }
            ENDCG
        }
    }
}