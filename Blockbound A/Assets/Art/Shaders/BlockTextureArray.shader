Shader "Blockbound/BlockTextureArray"
{
    Properties
    {
        _BlockTextures("Block Textures", 2DArray) = "" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            Texture2DArray _BlockTextures;
            SamplerState sampler_BlockTextures;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float3 normal : TEXCOORD2;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv2 = v.uv2;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float layer = i.uv2.x;
                float tileScale = max(i.uv2.y, 1.0);

                float2 tiledUV = frac(i.uv * tileScale);
                fixed4 col = _BlockTextures.Sample(sampler_BlockTextures, float3(tiledUV, layer));

                float3 lightDir = normalize(float3(0.4, 1.0, 0.3));
                float ndl = saturate(dot(normalize(i.normal), lightDir));
                float faceLight = lerp(0.70, 1.0, ndl);

                col.rgb *= faceLight;
                col *= i.color;

                return col;
            }
            ENDHLSL
        }
    }
}