Shader "UI/GlitchPanel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("面板颜色", Color) = (0.2, 0.2, 0.2, 0.6)
        
        [Header(Stripes)]
        _StripeColor ("条纹颜色", Color) = (1, 1, 1, 0.1)
        _StripeAngle ("条纹角度", Range(-180, 180)) = 45
        _StripeWidth ("条纹宽度", Range(0.01, 0.5)) = 0.05
        _StripeSpacing ("条纹间距", Range(0.1, 2)) = 0.3
        
        [Header(Glitch)]
        _GlitchSpeed ("故障速度", Range(0, 10)) = 2
        _GlitchIntensity ("故障强度", Range(0, 0.2)) = 0.05
        
        [Header(Fog)]
        _FogAmount ("雾面强度", Range(0, 1)) = 0.3
        
        // UI必需
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _StripeColor;
            float _StripeAngle;
            float _StripeWidth;
            float _StripeSpacing;
            float _GlitchSpeed;
            float _GlitchIntensity;
            float _FogAmount;
            float4 _ClipRect;

            float random(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;
                float2 uv = i.uv;
                
                // === 故障效果 ===
                float glitchLine = floor(uv.y * 20.0);
                float glitchRandom = random(float2(glitchLine, floor(time * _GlitchSpeed)));
                
                if(glitchRandom > 0.95)
                {
                    uv.x += (random(float2(glitchLine + 10.0, time)) - 0.5) * _GlitchIntensity;
                }
                
                // === 基础颜色 ===
                fixed4 col = tex2D(_MainTex, uv) * i.color;
                
                // === 斜条纹 ===
                float angle = radians(_StripeAngle);
                float2 rotatedUV = float2(
                    uv.x * cos(angle) - uv.y * sin(angle),
                    uv.x * sin(angle) + uv.y * cos(angle)
                );
                
                float stripe = frac(rotatedUV.x / _StripeSpacing);
                stripe = smoothstep(_StripeWidth, _StripeWidth + 0.01, stripe);
                
                col.rgb = lerp(col.rgb + _StripeColor.rgb * _StripeColor.a, col.rgb, stripe);
                
                // === 雾面效果（噪声叠加） ===
                float fog = random(uv * 50.0 + time * 0.1) * _FogAmount;
                col.rgb += fog * 0.1;
                
                // === 随机闪烁块 ===
                float2 blockID = floor(uv * 10.0);
                float blockRandom = random(blockID + floor(time * 3.0));
                if(blockRandom > 0.97)
                {
                    col.rgb += 0.1;
                }
                
                // UI裁剪
                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                
                return col;
            }
            ENDCG
        }
    }
}