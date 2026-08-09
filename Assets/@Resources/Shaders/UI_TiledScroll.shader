Shader "UI/TiledScroll"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // 스크롤 속도 (초당 UV 이동량)
        _ScrollSpeed ("Scroll Speed (XY)", Vector) = (0.05, 0.02, 0, 0)

        // 타일링 배율 (패턴 밀도)
        _Tiling ("Tiling (XY)", Vector) = (1, 1, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _ScrollSpeed;
            float4 _Tiling;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;

                // 기본 UI 텍스처 변환 + 타일링
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex) * _Tiling.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 시간 기반 스크롤 (Repeat)
                float2 offset = frac(_Time.y * _ScrollSpeed.xy);
                float2 uv = i.uv + offset;

                fixed4 c = tex2D(_MainTex, uv) * i.color;
                return c;
            }
            ENDCG
        }
    }
}
