// Transform-selection wireframe for VR.
//
// Visual language borrowed from 3D editors (Blender, Unity Editor, CAD tools):
//   - Edges tinted by world-space axis alignment  (X=red, Y=green, Z=blue)
//     so the object reads as "has a transform gizmo on it"
//   - Marching-ant dashes travelling along edges   (universal "selected" cue)
//   - Corner handle dots at every vertex           (suggests grab points)
//   - Midpoint handle dots at edge centres         (suggests scale handles)
//   - Additive fresnel rim on Pass 2               (clear silhouette in stereo)
//
// NOTE: This shader reads best on a simple bounding-box cube child object.
// Apply it there rather than directly on the high-poly anatomy mesh.
Shader "Custom/TransformWireframeVR"
{
    Properties
    {
        [Header(Edge Base)]
        [HDR] _EdgeColor        ("Base Edge Color",       Color)         = (0.85, 0.85, 0.85, 1.0)
        _EdgeThickness          ("Thickness (px)",         Range(0.5, 6)) = 1.6
        _EdgeSmooth             ("AA Width (px)",          Range(0.1, 2)) = 0.7

        [Header(Axis Tint)]
        _AxisTint               ("Axis Tint Strength",    Range(0, 1))   = 0.72

        [Header(Marching Ants)]
        _DashDensity            ("Dash Density (per unit)",Range(0.5,20)) = 4.0
        _DashRatio              ("Dash Fill Ratio",        Range(0.1,0.9))= 0.5
        _DashSpeed              ("March Speed",            Range(-20, 20))= 6.0
        _DashGap                ("Gap Brightness",         Range(0, 0.5)) = 0.08

        [Header(Corner Handles)]
        [HDR] _CornerColor      ("Corner Color",           Color)         = (1.0, 1.0, 1.0, 1.0)
        _CornerSize             ("Handle Radius (px)",     Range(0, 28))  = 11.0
        _CornerIntensity        ("Handle Brightness",      Range(0, 5))   = 3.0
        _CornerPulseSpeed       ("Pulse Speed",            Range(0, 10))  = 2.5

        [Header(Midpoint Scale Handles)]
        [HDR] _MidColor         ("Midpoint Handle Color",  Color)         = (1.0, 1.0, 1.0, 1.0)
        _MidSize                ("Handle Radius (px)",     Range(0, 20))  = 6.0
        _MidIntensity           ("Handle Brightness",      Range(0, 5))   = 2.0

        [Header(Fill and Rim)]
        _FillColor              ("Inner Fill",             Color)         = (0.04, 0.18, 0.30, 0.03)
        [HDR] _RimColor         ("Rim / Silhouette",       Color)         = (0.7, 0.85, 1.0,  1.0)
        _RimPower               ("Rim Sharpness",          Range(0.5, 8)) = 3.5
        _RimIntensity           ("Rim Intensity",          Range(0, 2))   = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" "IgnoreProjector"="True" }
        LOD 100

        // ── Pass 1 : Wireframe ─────────────────────────────────────────────
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            Offset -1, -1

            CGPROGRAM
            #pragma vertex   vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2g
            {
                float4 pos      : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // dist.x = px-distance to edge (v1→v2),  eDir0 = world dir of that edge
            // dist.y = px-distance to edge (v0→v2),  eDir1 = world dir of that edge
            // dist.z = px-distance to edge (v0→v1),  eDir2 = world dir of that edge
            struct g2f
            {
                float4 pos      : SV_POSITION;
                float3 dist     : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 eDir0    : TEXCOORD2;
                float3 eDir1    : TEXCOORD3;
                float3 eDir2    : TEXCOORD4;
                // Normalised 0-1 parameter along each edge at this fragment,
                // used for midpoint handle detection.
                // x = param along e0,  y = param along e1,  z = param along e2
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

            v2g vert(appdata v)
            {
                v2g o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g inp[3], inout TriangleStream<g2f> triStream)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(inp[0]);

                float2 p0 = _ScreenParams.xy * (inp[0].pos.xy / inp[0].pos.w);
                float2 p1 = _ScreenParams.xy * (inp[1].pos.xy / inp[1].pos.w);
                float2 p2 = _ScreenParams.xy * (inp[2].pos.xy / inp[2].pos.w);

                float2 e0   = p2 - p1;
                float2 e1   = p2 - p0;
                float2 e2   = p1 - p0;
                float  area = abs(e1.x * e2.y - e1.y * e2.x);

                float3 d = float3(
                    area / length(e0),
                    area / length(e1),
                    area / length(e2));

                float3 w0 = inp[0].worldPos;
                float3 w1 = inp[1].worldPos;
                float3 w2 = inp[2].worldPos;

                // World-space unit direction of each edge
                float3 wDir0 = normalize(w2 - w1); // edge v1→v2  (opposite v0)
                float3 wDir1 = normalize(w2 - w0); // edge v0→v2  (opposite v1)
                float3 wDir2 = normalize(w1 - w0); // edge v0→v1  (opposite v2)

                // Precompute world lengths so the fragment can estimate
                // how far along the edge a fragment sits (for midpoint detection).
                float len0 = length(w2 - w1);
                float len1 = length(w2 - w0);
                float len2 = length(w1 - w0);

                g2f o;
                o.eDir0 = wDir0;
                o.eDir1 = wDir1;
                o.eDir2 = wDir2;

                // edgeT for vertex 0: it sits at the v0 end of edges 1 & 2,
                // and is not on edge 0 at all (t=0.5 as a neutral default there).
                // We store the 1D projection along each edge normalised to [0,1].
                float t0e0 = (len0 > 0.0001) ? saturate(dot(w0 - w1, wDir0) / len0) : 0.5;
                float t0e1 = (len1 > 0.0001) ? saturate(dot(w0 - w0, wDir1) / len1) : 0.0;
                float t0e2 = (len2 > 0.0001) ? saturate(dot(w0 - w0, wDir2) / len2) : 0.0;

                float t1e0 = (len0 > 0.0001) ? saturate(dot(w1 - w1, wDir0) / len0) : 0.0;
                float t1e1 = (len1 > 0.0001) ? saturate(dot(w1 - w0, wDir1) / len1) : 0.5;
                float t1e2 = (len2 > 0.0001) ? saturate(dot(w1 - w0, wDir2) / len2) : 1.0;

                float t2e0 = (len0 > 0.0001) ? saturate(dot(w2 - w1, wDir0) / len0) : 1.0;
                float t2e1 = (len1 > 0.0001) ? saturate(dot(w2 - w0, wDir1) / len1) : 1.0;
                float t2e2 = (len2 > 0.0001) ? saturate(dot(w2 - w0, wDir2) / len2) : 0.5;

                o.pos = inp[0].pos; o.worldPos = w0; o.dist = float3(d.x, 0, 0);
                o.edgeT = float3(t0e0, t0e1, t0e2);
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(inp[0], o); triStream.Append(o);

                o.pos = inp[1].pos; o.worldPos = w1; o.dist = float3(0, d.y, 0);
                o.edgeT = float3(t1e0, t1e1, t1e2);
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(inp[1], o); triStream.Append(o);

                o.pos = inp[2].pos; o.worldPos = w2; o.dist = float3(0, 0, d.z);
                o.edgeT = float3(t2e0, t2e1, t2e2);
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(inp[2], o); triStream.Append(o);
            }

            // Axis colours matching the universal transform gizmo convention
            // (Unity / Blender / Maya all use X=red, Y=green, Z=blue).
            static const float3 AXIS_X = float3(1.00, 0.22, 0.18);
            static const float3 AXIS_Y = float3(0.30, 0.90, 0.25);
            static const float3 AXIS_Z = float3(0.22, 0.50, 1.00);

            fixed4 frag(g2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float minD = min(i.dist.x, min(i.dist.y, i.dist.z));

                // ── 1. AA edge mask ───────────────────────────────────────
                float edgeAlpha = 1.0 - smoothstep(
                    _EdgeThickness - _EdgeSmooth,
                    _EdgeThickness + _EdgeSmooth,
                    minD);

                // ── 2. Closest-edge selection (branchless) ────────────────
                float isE0 = step(i.dist.x, i.dist.y) * step(i.dist.x, i.dist.z);
                float isE1 = (1.0 - isE0) * step(i.dist.y, i.dist.z);
                float isE2 = 1.0 - isE0 - isE1;
                float3 closestDir = i.eDir0 * isE0 + i.eDir1 * isE1 + i.eDir2 * isE2;
                float  edgeT      = i.edgeT.x * isE0 + i.edgeT.y * isE1 + i.edgeT.z * isE2;

                // ── 3. Axis tinting ───────────────────────────────────────
                // Blend toward the axis colour whose direction this edge aligns with most.
                float3 absDir = abs(closestDir);
                float3 axisBlend = absDir / max(absDir.x + absDir.y + absDir.z, 0.0001);
                float3 axisColor = AXIS_X * axisBlend.x
                                 + AXIS_Y * axisBlend.y
                                 + AXIS_Z * axisBlend.z;
                float3 edgeRGB = lerp(_EdgeColor.rgb, axisColor, _AxisTint);

                // ── 4. Marching-ant dashes ────────────────────────────────
                // A 1D coordinate along the edge (world-space projection) creates
                // regularly spaced dashes that march in a consistent direction.
                float edgeCoord = dot(i.worldPos, closestDir);
                float phase     = frac(edgeCoord * _DashDensity - _Time.y * _DashSpeed * 0.04);
                float dashMask  = smoothstep(0.0, 0.08, phase)
                                * smoothstep(_DashRatio + 0.08, _DashRatio, phase);
                // Gaps are dim but visible — the object silhouette stays readable.
                float dashBright = lerp(_DashGap, 1.0, dashMask);
                edgeAlpha *= dashBright;

                // ── 5. Corner handles ─────────────────────────────────────
                // A fragment is near vertex v0 when dist.y ≈ 0 AND dist.z ≈ 0,
                // near v1 when dist.x ≈ 0 AND dist.z ≈ 0, etc.
                float cv0 = (1.0 - smoothstep(0, _CornerSize, i.dist.y))
                          * (1.0 - smoothstep(0, _CornerSize, i.dist.z));
                float cv1 = (1.0 - smoothstep(0, _CornerSize, i.dist.x))
                          * (1.0 - smoothstep(0, _CornerSize, i.dist.z));
                float cv2 = (1.0 - smoothstep(0, _CornerSize, i.dist.x))
                          * (1.0 - smoothstep(0, _CornerSize, i.dist.y));
                float corner = saturate(cv0 + cv1 + cv2);
                // Gentle pulse so handles "breathe" — suggests interactivity
                float cornerPulse = 0.75 + 0.25 * sin(_Time.y * _CornerPulseSpeed);
                float3 cornerRGB  = _CornerColor.rgb * corner * _CornerIntensity * cornerPulse;

                // ── 6. Midpoint scale handles ─────────────────────────────
                // A fragment is near the midpoint of an edge when edgeT ≈ 0.5
                // and dist to that edge is small. Both must be true simultaneously.
                float midOnE0 = (1.0 - smoothstep(0, _MidSize, i.dist.x))
                              * (1.0 - smoothstep(0, 0.12, abs(i.edgeT.x - 0.5)));
                float midOnE1 = (1.0 - smoothstep(0, _MidSize, i.dist.y))
                              * (1.0 - smoothstep(0, 0.12, abs(i.edgeT.y - 0.5)));
                float midOnE2 = (1.0 - smoothstep(0, _MidSize, i.dist.z))
                              * (1.0 - smoothstep(0, 0.12, abs(i.edgeT.z - 0.5)));
                float midpoint = saturate(midOnE0 + midOnE1 + midOnE2);
                float3 midRGB  = _MidColor.rgb * midpoint * _MidIntensity;

                // ── 7. Composition ────────────────────────────────────────
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

        // ── Pass 2 : Additive fresnel silhouette rim ───────────────────────
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Back

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
