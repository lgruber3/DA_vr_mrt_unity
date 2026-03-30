Shader "Custom/GPUParticleRender"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Blend SrcAlpha One 
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            #include "UnityCG.cginc"

            struct Particle
            {
                float3 scattered;
                float3 assembled;
                float3 drift;
                float3 position;
                float4 color;
                float  size;
            };

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            StructuredBuffer<Particle> _Particles;
        #endif

            float4x4 _LocalToWorld;

            void setup()
            {
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 center = float3(0, 0, 0);
                float  size   = 0.015;
                float4 color  = float4(1, 1, 1, 1);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                uint id = unity_InstanceID;
                Particle p = _Particles[id];
                center = p.position;
                size   = p.size;
                color  = p.color;
            #endif

                float3 worldCenter = mul(_LocalToWorld, float4(center, 1.0)).xyz;
                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp    = UNITY_MATRIX_V[1].xyz;

                float3 worldPos = worldCenter
                    + camRight * v.vertex.x * size
                    + camUp    * v.vertex.y * size;

                o.pos   = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.uv    = v.uv;
                o.color = color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 c = i.uv - 0.5;
                float dist = dot(c, c) * 4.0;
                float alpha = saturate(1.0 - dist);
                alpha *= alpha; 

                return i.color * float4(1, 1, 1, alpha);
            }
            ENDCG
        }
    }
}
