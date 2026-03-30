Shader "Medical/XRay_Density"
{
    Properties
    {
        _Color ("X-Ray Color", Color) = (0.2, 0.6, 1.0, 1)
        _Density ("Density Multiplier", Range(0.01, 3.0)) = 0.5
        _Alpha ("Base Opacity", Range(0.01, 1.0)) = 0.15
        _RimPower ("Rim Sharpness", Range(0.5, 6.0)) = 2.0
        _RimStrength ("Rim Brightness", Range(0.0, 3.0)) = 1.0
        _BackgroundDarken ("Background Tint", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Cull Off  // render both sides for volume density

        // Single pass: alpha blend so each layer partially covers the background
        Blend SrcAlpha OneMinusSrcAlpha

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
                float NdotV : TEXCOORD0;
            };

            fixed4 _Color;
            float _Density;
            float _Alpha;
            float _RimPower;
            float _RimStrength;
            float _BackgroundDarken;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
                o.NdotV = saturate(dot(v.normal, viewDir));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Facing ratio: surfaces facing camera = dense, glancing = thin
                float facing = i.NdotV;

                // Rim glow for edge readability
                float rim = pow(1.0 - facing, _RimPower) * _RimStrength;

                // Core x-ray brightness
                float coreBrightness = facing * _Density + rim;

                // Mix between a dark tint and the x-ray color
                fixed3 darkBase = _Color.rgb * 0.05;
                fixed3 brightXray = _Color.rgb * coreBrightness;
                fixed3 finalColor = darkBase + brightXray;

                // Alpha: enough to occlude background, varies with density
                float alpha = _Alpha + rim * 0.1;

                // Darken by mixing toward dark color
                finalColor = lerp(finalColor, finalColor * _BackgroundDarken, 0.5);

                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
}