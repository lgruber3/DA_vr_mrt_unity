
Shader "Medical/Surface_Curvature"
{
    Properties
    {
        [Header(Thermal Colormap)]
        _ColdColor  ("Cold  (edge-on / recessed)", Color) = (0.05, 0.20, 0.90, 1)
        _MidColor   ("Mid   (45-degree / neutral)", Color) = (0.10, 0.85, 0.40, 1)
        _WarmColor  ("Warm  (facing / prominent)",  Color) = (0.95, 0.18, 0.05, 1)

        [Header(Shading)]
        _Contrast        ("Gradient Contrast",    Range(0.5, 6.0)) = 2.0
        _AmbientStrength ("Ambient Fill",         Range(0.0, 1.0)) = 0.25
        _LightDir        ("Key Light Direction",  Vector)          = (0.5, 1.0, 0.5, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 viewDir     : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _ColdColor;
            fixed4 _MidColor;
            fixed4 _WarmColor;
            float  _Contrast;
            float  _AmbientStrength;
            float4 _LightDir;

            // Branchless two-segment lerp: cold → mid → warm as t goes 0 → 1
            fixed3 ThermalRamp(float t)
            {
                fixed3 lo = lerp(_ColdColor.rgb, _MidColor.rgb,  saturate(t * 2.0));
                fixed3 hi = lerp(_MidColor.rgb,  _WarmColor.rgb, saturate(t * 2.0 - 1.0));
                return lerp(lo, hi, step(0.5, t));
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos         = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir     = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 N = normalize(i.worldNormal);
                float3 V = normalize(i.viewDir);

                // View-facing factor: 1 = surface faces viewer, 0 = edge-on
                float facing = saturate(dot(N, V));

                // Contrast curve — sharpens the gradient so mid-range anatomy
                // stands out rather than everything looking uniformly warm.
                float t = pow(facing, _Contrast);

                // Lambert diffuse for shape legibility — without this the
                // colour alone makes depth hard to read in VR.
                float3 L       = normalize(_LightDir.xyz);
                float  diffuse = saturate(dot(N, L));
                float  light   = _AmbientStrength + (1.0 - _AmbientStrength) * diffuse;

                fixed3 color = ThermalRamp(t) * light;
                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
