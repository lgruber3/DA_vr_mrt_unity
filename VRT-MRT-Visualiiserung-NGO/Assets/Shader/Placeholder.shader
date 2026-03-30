Shader "Custom/Placeholder"
{
    Properties
    {
        _Color ("Base Color", Color) = (0.2, 0.6, 1.0, 0.3)
        _RimColor ("Rim Color", Color) = (0.3, 0.8, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 2.5
        _ScanLineSpeed ("Scan Line Speed", Range(0.1, 5.0)) = 1.0
        _ScanLineWidth ("Scan Line Width", Range(0.01, 0.2)) = 0.05
        _ScanLineDensity ("Scan Line Density", Range(5, 80)) = 30.0
        _ScanLineAlpha ("Scan Line Brightness", Range(0.0, 1.0)) = 0.6
        _PulseSpeed ("Pulse Speed", Range(0.1, 4.0)) = 1.5
        _PulseMin ("Pulse Min Alpha", Range(0.0, 1.0)) = 0.15
        _PulseMax ("Pulse Max Alpha", Range(0.0, 1.0)) = 0.45
        _WireframeBlend ("Wireframe Blend", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HOLOGRAM"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos       : SV_POSITION;
                float3 worldPos  : TEXCOORD0;
                float3 worldNorm : TEXCOORD1;
                float3 viewDir   : TEXCOORD2;
                float  objY      : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _RimColor;
            float  _RimPower;
            float  _ScanLineSpeed;
            float  _ScanLineWidth;
            float  _ScanLineDensity;
            float  _ScanLineAlpha;
            float  _PulseSpeed;
            float  _PulseMin;
            float  _PulseMax;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos       = UnityObjectToClipPos(v.vertex);
                o.worldPos  = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNorm = UnityObjectToWorldNormal(v.normal);
                o.viewDir   = normalize(WorldSpaceViewDir(v.vertex));
                o.objY      = v.vertex.y;        
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float rim = 1.0 - saturate(dot(normalize(i.viewDir), normalize(i.worldNorm)));
                rim = pow(rim, _RimPower);

                float scanY    = i.worldPos.y * _ScanLineDensity + _Time.y * _ScanLineSpeed;
                float scanLine = smoothstep(0.0, _ScanLineWidth, abs(frac(scanY) - 0.5));
                float scanMask = 1.0 - scanLine;                  

                float sweep     = frac(_Time.y * _ScanLineSpeed * 0.3);
                float objHeight = i.objY;                               
                float sweepDist = abs(frac(objHeight * 0.5 + 0.5) - sweep);
                float sweepMask = 1.0 - saturate(sweepDist / 0.06);

                float pulse = lerp(_PulseMin, _PulseMax,
                              (sin(_Time.y * _PulseSpeed * 6.2831) * 0.5 + 0.5));

                fixed4 col;
                col.rgb = lerp(_Color.rgb, _RimColor.rgb, rim);
                col.rgb += _RimColor.rgb * sweepMask * 0.7;       
                col.rgb += _RimColor.rgb * scanMask * _ScanLineAlpha * 0.3;

                float alpha = pulse + rim * 0.6 + scanMask * _ScanLineAlpha * 0.15 + sweepMask * 0.35;
                col.a = saturate(alpha);

                return col;
            }
            ENDCG
        }

        Pass
        {
            Name "RIM_GLOW"
            Blend One One         
            ZWrite Off
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 worldNorm: TEXCOORD0;
                float3 viewDir  : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _RimColor;
            float  _RimPower;
            float  _PulseSpeed;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.worldNorm= UnityObjectToWorldNormal(v.normal);
                o.viewDir  = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float rim   = 1.0 - saturate(dot(normalize(i.viewDir), normalize(i.worldNorm)));
                rim = pow(rim, _RimPower * 1.2);
                float pulse = sin(_Time.y * _PulseSpeed * 6.2831) * 0.5 + 0.5;
                return _RimColor * rim * 0.35 * (0.6 + 0.4 * pulse);
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
