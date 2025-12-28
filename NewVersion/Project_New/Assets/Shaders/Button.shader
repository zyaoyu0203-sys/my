Shader "UI/HologramButton"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("按钮颜色", Color) = (0.3, 0.8, 1, 0.6)
        
        [Header(Outline)]
        _OutlineColor ("描边颜色", Color) = (0.5, 1, 1, 1)
        _OutlineWidth ("描边宽度", Range(1, 10)) = 3
        
        [Header(Drop Shadow)]
        _ShadowColor ("外阴影颜色", Color) = (0, 0.5, 0.8, 0.5)
        _ShadowOffsetX ("阴影X偏移", Range(-10, 10)) = 3
        _ShadowOffsetY ("阴影Y偏移", Range(-10, 10)) = -3
        
        [Header(Inner Shadow)]
        _InnerShadowColor ("内阴影颜色", Color) = (0, 0, 0, 0.5)
        _InnerShadowSize ("内阴影大小", Range(1, 20)) = 5
        
        [Header(Hologram Effect)]
        _NoiseAmount ("噪点强度", Range(0, 1)) = 0.3
        _NoiseSpeed ("噪点速度", Range(0, 10)) = 3
        _ScanlineSpeed ("扫描线速度", Range(0, 10)) = 2
        _ScanlineColor ("扫描线颜色", Color) = (0.3, 1, 1, 0.5)
        _Glitch ("故障强度", Range(0, 0.1)) = 0.02
        
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
            "CanUseSpriteAtlas"="True"
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
            Name "Outline"
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
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            fixed4 _ShadowColor;
            float _ShadowOffsetX;
            float _ShadowOffsetY;
            fixed4 _InnerShadowColor;
            float _InnerShadowSize;
            float _NoiseAmount;
            float _NoiseSpeed;
            float _ScanlineSpeed;
            fixed4 _ScanlineColor;
            float _Glitch;
            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float random(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;
                float2 uv = i.uv;
                
                // === Glitch效果 ===
                float glitchLine = floor(uv.y * 50.0);
                float glitchRandom = random(float2(glitchLine, floor(time * 5.0)));
                if(glitchRandom > 0.95)
                {
                    uv.x += (random(float2(glitchLine + 10.0, time)) - 0.5) * _Glitch;
                }
                
                // === 采样主贴图 ===
                fixed4 mainColor = tex2D(_MainTex, uv);
                
                // === 外阴影 ===
                float2 shadowUV = uv - float2(_ShadowOffsetX, _ShadowOffsetY) * _MainTex_TexelSize.xy;
                fixed4 shadowSample = tex2D(_MainTex, shadowUV);
                
                // === 描边 ===
                fixed4 outline = fixed4(0, 0, 0, 0);
                float outlineAlpha = 0;
                
                for(int x = -1; x <= 1; x++)
                {
                    for(int y = -1; y <= 1; y++)
                    {
                        if(x == 0 && y == 0) continue;
                        
                        float2 offsetUV = uv + float2(x, y) * _OutlineWidth * _MainTex_TexelSize.xy;
                        fixed4 sample = tex2D(_MainTex, offsetUV);
                        outlineAlpha = max(outlineAlpha, sample.a);
                    }
                }
                
                outlineAlpha = outlineAlpha * (1.0 - mainColor.a);
                
                // === 内阴影 ===
                float innerShadowAlpha = 0;
                for(int ix = -1; ix <= 1; ix++)
                {
                    for(int iy = -1; iy <= 1; iy++)
                    {
                        float2 innerUV = uv + float2(ix, iy) * _InnerShadowSize * _MainTex_TexelSize.xy;
                        innerShadowAlpha += (1.0 - tex2D(_MainTex, innerUV).a);
                    }
                }
                innerShadowAlpha = saturate(innerShadowAlpha * 0.2) * mainColor.a;
                
                // === 噪点 ===
                float noise = random(uv * 100.0 + time * _NoiseSpeed);
                noise = (noise - 0.5) * _NoiseAmount;
                
                // === 扫描线 ===
                float scanline = frac(uv.y * 30.0 - time * _ScanlineSpeed);
                scanline = smoothstep(0.95, 1.0, scanline);
                
                // === 组合 ===
                fixed4 finalColor = fixed4(0, 0, 0, 0);
                
                // 外阴影
                finalColor = lerp(finalColor, _ShadowColor, shadowSample.a * (1.0 - mainColor.a - outlineAlpha) * _ShadowColor.a);
                
                // 描边
                finalColor = lerp(finalColor, _OutlineColor, outlineAlpha);
                
                // 主按钮
                fixed4 buttonColor = _Color * mainColor * i.color;
                buttonColor.rgb += noise;
                buttonColor.rgb += scanline * _ScanlineColor.rgb * _ScanlineColor.a;
                finalColor = lerp(finalColor, buttonColor, mainColor.a);
                
                // 内阴影
                finalColor.rgb = lerp(finalColor.rgb, finalColor.rgb * _InnerShadowColor.rgb, innerShadowAlpha * _InnerShadowColor.a);
                
                // UI裁剪
                finalColor.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                
                clip(finalColor.a - 0.001);
                
                return finalColor;
            }
            ENDCG
        }
    }
}