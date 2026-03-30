Shader "Custom/TransformWireframeVR"
{
    Properties
    {
        [Header(Edge Base)]
        [HDR] _EdgeColor ("Base Edge Color", Color) = (0.85, 0.85, 0.85, 1.0)
        _EdgeThickness ("Thickness (px)", Range(0.5, 6)) = 1.6
        _EdgeSmooth ("AA Width (px)", Range(0.1, 2)) = 0.7

        [Header(Axis Tint)]
        _AxisTint ("Axis Tint Strength", Range(0, 1)) = 0.72

        [Header(Marching Ants)]
        _DashDensity ("Dash Density (per unit)", Range(0.5,20)) = 4.0
        _DashRatio ("Dash Fill Ratio", Range(0.1,0.9))= 0.5
        _DashSpeed ("March Speed", Range(-20, 20))= 6.0
        _DashGap ("Gap Brightness", Range(0, 0.5)) = 0.08

        [Header(Corner Handles)]
        [HDR] _CornerColor ("Corner Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _CornerSize ("Handle Radius (px)", Range(0, 28)) = 11.0
        _CornerIntensity ("Handle Brightness", Range(0, 5)) = 3.0
        _CornerPulseSpeed ("Pulse Speed", Range(0, 10)) = 2.5

        [Header(Midpoint Scale Handles)]
        [HDR] _MidColor ("Midpoint Handle Color",  Color) = (1.0, 1.0, 1.0, 1.0)
        _MidSize ("Handle Radius (px)", Range(0, 20)) = 6.0
        _MidIntensity ("Handle Brightness", Range(0, 5)) = 2.0

        [Header(Fill and Rim)]
        _FillColor ("Inner Fill", Color) = (0.04, 0.18, 0.30, 0.03)
        [HDR] _RimColor ("Rim / Silhouette", Color) = (0.7, 0.85, 1.0,  1.0)
        _RimPower ("Rim Sharpness", Range(0.5, 8)) = 3.5
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" "IgnoreProjector"="True" }
        LOD 100

        // Pass 1 : Wireframe 
        Pass
        {
            Blend  SrcAlpha OneMinusSrcAlpha
            ZTest  LEqual
            ZWrite Off
            Cull   Back

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex   : POSITION;
                float3 bary     : TEXCOORD2;   // barycentric
                float4 eDir0    : TEXCOORD3;   // edge dir opposite v0
                float4 eDir1    : TEXCOORD4;   // edge dir opposite v1
                float4 eDir2    : TEXCOORD5;   // edge dir opposite v2
                float3 edgeT    : TEXCOORD6;   // edge T parameters
                float3 edgeLens : TEXCOORD7;   // edge lengths
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 bary     : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 eDir0    : TEXCOORD2;
                float3 eDir1    : TEXCOORD3;
                float3 eDir2    : TEXCOORD4;
                float3 edgeT    : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _EdgeColor;
            float  _EdgeThickness, _EdgeSmooth;
            float  _AxisTint;
            float  _DashDensity, _DashRatio, _DashSpeed, _DashGap;
            float4 _CornerColor;
            float  _CornerSize, _CornerIntensity, _CornerPulseSpeed;
            float4 _MidColor;
            float  _MidSize, _MidIntensity;
            float4 _FillColor;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos      = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.bary     = v.bary;
                o.eDir0    = v.eDir0.xyz;
                o.eDir1    = v.eDir1.xyz;
                o.eDir2    = v.eDir2.xyz;
                o.edgeT    = v.edgeT;

                return o;
            }

            // Axis colours: X=red, Y=green, Z=blue
            static const float3 AXIS_X = float3(1.00, 0.22, 0.18);
            static const float3 AXIS_Y = float3(0.30, 0.90, 0.25);
            static const float3 AXIS_Z = float3(0.22, 0.50, 1.00);

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Pixel-space edge distances via fwidth()
                // bary.x=0 on the edge opposite v0, so bary.x / fwidth(bary.x)
                // gives pixel distance to that edge. Fully per-eye correct.
                float3 bary = i.bary;
                float3 fw   = fwidth(bary);
                float3 dist = float3(
                    bary.x / max(fw.x, 0.00001),
                    bary.y / max(fw.y, 0.00001),
                    bary.z / max(fw.z, 0.00001));

                float minD = min(dist.x, min(dist.y, dist.z));

                // 1. AA edge mask
                float edgeAlpha = 1.0 - smoothstep(
                    _EdgeThickness - _EdgeSmooth,
                    _EdgeThickness + _EdgeSmooth,
                    minD);

                // 2. Closest-edge selection (branchless)
                float isE0 = step(dist.x, dist.y) * step(dist.x, dist.z);
                float isE1 = (1.0 - isE0) * step(dist.y, dist.z);
                float isE2 = 1.0 - isE0 - isE1;
                float3 closestDir = i.eDir0 * isE0 + i.eDir1 * isE1 + i.eDir2 * isE2;
                float  edgeT      = i.edgeT.x * isE0 + i.edgeT.y * isE1 + i.edgeT.z * isE2;

                // 3. Axis tinting 
                float3 absDir = abs(closestDir);
                float3 axisBlend = absDir / max(absDir.x + absDir.y + absDir.z, 0.0001);
                float3 axisColor = AXIS_X * axisBlend.x
                                 + AXIS_Y * axisBlend.y
                                 + AXIS_Z * axisBlend.z;
                float3 edgeRGB = lerp(_EdgeColor.rgb, axisColor, _AxisTint);

                // 4. Marching-ant dashes
                float edgeCoord = dot(i.worldPos, closestDir);
                float phase     = frac(edgeCoord * _DashDensity - _Time.y * _DashSpeed * 0.04);
                float dashMask  = smoothstep(0.0, 0.08, phase)
                                * smoothstep(_DashRatio + 0.08, _DashRatio, phase);
                float dashBright = lerp(_DashGap, 1.0, dashMask);
                edgeAlpha *= dashBright;

                // 5. Corner handles 
                float cv0 = (1.0 - smoothstep(0, _CornerSize, dist.y))
                          * (1.0 - smoothstep(0, _CornerSize, dist.z));
                float cv1 = (1.0 - smoothstep(0, _CornerSize, dist.x))
                          * (1.0 - smoothstep(0, _CornerSize, dist.z));
                float cv2 = (1.0 - smoothstep(0, _CornerSize, dist.x))
                          * (1.0 - smoothstep(0, _CornerSize, dist.y));
                float corner = saturate(cv0 + cv1 + cv2);
                float cornerPulse = 0.75 + 0.25 * sin(_Time.y * _CornerPulseSpeed);
                float3 cornerRGB  = _CornerColor.rgb * corner * _CornerIntensity * cornerPulse;

                // 6. Midpoint scale handles 
                float midOnE0 = (1.0 - smoothstep(0, _MidSize, dist.x))
                              * (1.0 - smoothstep(0, 0.12, abs(i.edgeT.x - 0.5)));
                float midOnE1 = (1.0 - smoothstep(0, _MidSize, dist.y))
                              * (1.0 - smoothstep(0, 0.12, abs(i.edgeT.y - 0.5)));
                float midOnE2 = (1.0 - smoothstep(0, _MidSize, dist.z))
                              * (1.0 - smoothstep(0, 0.12, abs(i.edgeT.z - 0.5)));
                float midpoint = saturate(midOnE0 + midOnE1 + midOnE2);
                float3 midRGB  = _MidColor.rgb * midpoint * _MidIntensity;

                // 7. Composition 
                float  handleFactor = saturate(corner + midpoint);
                float  onEdge       = max(edgeAlpha, handleFactor);
                float3 finalRGB     = lerp(_FillColor.rgb,
                                           edgeRGB + cornerRGB + midRGB,
                                           onEdge);
                float  finalA       = max(_FillColor.a, onEdge);

                return fixed4(finalRGB, finalA);
            }
            ENDCG
        }

        // Pass 2 : Additive fresnel silhouette rim
        Pass
        {
            Blend  One One
            ZTest  LEqual
            ZWrite Off
            Cull   Back

            CGPROGRAM
            #pragma vertex   vertRim
            #pragma fragment fragRim
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdataRim
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2fRim
            {
                float4 pos : SV_POSITION;
                float  rim : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _RimColor;
            float  _RimPower, _RimIntensity;

            v2fRim vertRim(appdataRim v)
            {
                v2fRim o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 wN = UnityObjectToWorldNormal(v.normal);
                float3 vD = normalize(WorldSpaceViewDir(v.vertex));
                o.rim = 1.0 - saturate(dot(wN, vD));
                return o;
            }

            fixed4 fragRim(v2fRim i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float r = pow(i.rim, _RimPower) * _RimIntensity;
                return fixed4(_RimColor.rgb * r, r);
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
