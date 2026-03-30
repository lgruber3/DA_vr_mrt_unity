Shader "Medical/Clinical_MatCap_Pro"
{
    Properties
    {
        [Header(MatCap)]
        _MatCap ("MatCap Texture", 2D) = "white" {}
        _MatCapStrength ("MatCap Strength", Range(0,2)) = 1.0

        [Header(Surface)]
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0,2)) = 1.0

        [Header(Rim Highlight)]
        _RimColor ("Rim Color", Color)  = (0.6, 0.85, 1.0, 1)
        _RimPower ("Rim Sharpness", Range(0.5,8)) = 3.0
        _RimStrength ("Rim Strength", Range(0,1)) = 0.35

        [Header(Specular)]
        _SpecColor2 ("Specular Tint", Color)  = (1,1,1,1)
        _SpecPower ("Specular Boost", Range(0,3)) = 1.0

        [Header(Tint and Overlay)]
        _Color ("Base Tint", Color)  = (1,1,1,1)
        _DetailTex ("Detail / Markup Overlay", 2D) = "white" {}
        _DetailStrength ("Detail Blend Strength", Range(0,1)) = 0.0

        [Header(Vertex Color)]
        _VertexColorBlend("Vertex Color Blend", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "UnityStandardUtils.cginc" 

            sampler2D _MatCap;
            sampler2D _BumpMap;
            sampler2D _DetailTex;

            half4  _Color;
            half4  _RimColor;
            half4  _SpecColor2;
            half   _MatCapStrength;
            half   _BumpScale;
            half   _RimPower;
            half   _RimStrength;
            half   _SpecPower;
            half   _DetailStrength;
            half   _VertexColorBlend;

            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;  
                float2 uv      : TEXCOORD0;
                half4  color   : COLOR;  
            };

            struct v2f
            {
                float4 pos       : SV_POSITION;
                float2 uv        : TEXCOORD0;
                float3 tspace0   : TEXCOORD1;
                float3 tspace1   : TEXCOORD2;
                float3 tspace2   : TEXCOORD3;
                float3 viewDir   : TEXCOORD4;  
                half4  vcolor    : COLOR;
                UNITY_FOG_COORDS(5)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos    = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                o.vcolor = v.color;

                // World-space TBN matrix for normal mapping
                float3 worldNormal  = UnityObjectToWorldNormal(v.normal);
                float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                float3 worldBinorm  = cross(worldNormal, worldTangent)
                                      * v.tangent.w                   // handedness
                                      * unity_WorldTransformParams.w; // flip fix

                o.tspace0 = float3(worldTangent.x, worldBinorm.x, worldNormal.x);
                o.tspace1 = float3(worldTangent.y, worldBinorm.y, worldNormal.y);
                o.tspace2 = float3(worldTangent.z, worldBinorm.z, worldNormal.z);

                // World-space view direction (for rim + matcap)
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);

                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // 1. Decode normal map and bring into world space
                half3 tn = UnpackScaleNormal(tex2D(_BumpMap, i.uv), _BumpScale);
                half3 worldN;
                worldN.x = dot(i.tspace0, tn);
                worldN.y = dot(i.tspace1, tn);
                worldN.z = dot(i.tspace2, tn);
                worldN = normalize(worldN);

                // 2. MatCap UV from view-space normal
                half3 viewN = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, worldN));

                // Slight bias pulls UVs away from the harsh edge of the sphere cap
                half2 capUV = viewN.xy * 0.495 + 0.5;

                half4 matcap = tex2D(_MatCap, capUV);
                half  lum    = dot(matcap.rgb, half3(0.299, 0.587, 0.114)); // grayscale
                matcap.rgb   = half3(lum, lum, lum) * _MatCapStrength * _SpecPower;     

                // 3. Fresnel rim
                half  fresnel = pow(1.0 - saturate(dot(worldN, i.viewDir)), _RimPower);
                half3 rim     = _RimColor.rgb * fresnel * _RimStrength;

                // 4. Detail
                half4 detail = tex2D(_DetailTex, i.uv);
                half3 detailBlend = lerp(half3(1,1,1), detail.rgb, _DetailStrength * detail.a);

                // 5. Vertex color blend
                half3 vertCol = lerp(half3(1,1,1), i.vcolor.rgb, _VertexColorBlend);

                                half3 col = matcap.rgb
                          * _Color.rgb
                          * detailBlend
                          * vertCol
                          + rim;

                half4 final = half4(col, _Color.a);
                UNITY_APPLY_FOG(i.fogCoord, final);
                return final;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}