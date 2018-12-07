﻿Shader "Tiny/Particle2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SrcMode ("SrcMode", Float) = 5
        _DstMode ("DstMode", Float) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        ZWrite Off
        Cull Off
        Blend [_SrcMode] [_DstMode]
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                clip (col.a - 0.01);
                return col;
            }
            ENDCG
        }
    }
}
