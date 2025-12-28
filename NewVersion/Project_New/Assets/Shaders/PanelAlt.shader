Shader "UI/GlitchPanelEnhanced"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Panel Color", Color) = (0.2, 0.2, 0.2, 0.6)
        
        [Header(Rounded Corners)]
        _Radius ("Corner Radius", Range(0, 0.5)) = 0.1
        
        [Header(Border)]
        _BorderColor ("Border Color", Color) = (0.5, 1, 1, 1)
        _BorderWidth ("Border Width", Range(0, 0.1)) = 0.02
        _BorderGlow ("Border Glow", Range(0, 5)) = 1.5
        
        [Header(Chromatic)]
        _ChromaticAmount ("Chromatic Amount", Range(0, 0.2)) = 0.08
        _ChromaticEdge ("Edge Power", Range(1, 5)) = 2
        _ChromaticShift ("Color Shift", Range(0, 1)) = 0.5
        
        [Header(Scanlines)]
        _ScanlineCount ("Scanline Count", Range(50, 500)) = 200
        _ScanlineSpeed ("Scanline Speed", Range(-5, 5)) = 1
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.3
        
        [Header(Moire)]
        _MoireScale ("Moire Scale", Range(5, 100)) = 30
        _MoireIntensity ("Moire Intensity", Range(0, 2)) = 0.8
        _MoireSpeed ("Moire Speed", Range(0, 5)) = 1
        
        [Header(Stripes)]
        _StripeColor ("Stripe Color", Color) = (0.5, 1, 1, 0.3)
        _StripeAngle ("Stripe Angle", Range(-180, 180)) = 45
        _StripeWidth ("Stripe Width", Range(0.01, 0.3)) = 0.1
        _StripeSpacing ("Stripe Spacing", Range(0.05, 1)) = 0.15
        _StripeSpeed ("Stripe Speed", Range(-5, 5)) = 2
        
        [Header(Glitch)]
        _GlitchSpeed ("Glitch Speed", Range(0, 10)) = 2
        _GlitchIntensity ("Glitch Intensity", Range(0, 0.2)) = 0.05
        
        [Header(Fog)]
        _FogAmount ("Fog Amount", Range(0, 1)) = 0.3
        
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
            float _Radius;
            fixed4 _BorderColor;
            float _BorderWidth;
            float _BorderGlow;
            float _ChromaticAmount;
            float _ChromaticEdge;
            float _ChromaticShift;
            float _ScanlineCount;
            float _ScanlineSpeed;
            float _ScanlineIntensity;
            float _MoireScale;
            float _MoireIntensity;
            float _MoireSpeed;
            fixed4 _StripeColor;
            float _StripeAngle;
            float _StripeWidth;
            float _StripeSpacing;
            float _StripeSpeed;
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
                
                // === 圆角计算 ===
                float2 d = min(uv, 1.0 - uv);
                float cornerAlpha = 1.0;
                
                if(d.x < _Radius && d.y < _Radius)
                {
                    float2 toCorner = _Radius - d;
                    float dist = length(toCorner);
                    cornerAlpha = smoothstep(_Radius, _Radius - 0.01, dist);
                }
                
                // === 边缘距离 ===
                float2 edgeDist = min(uv, 1.0 - uv);
                float minEdgeDist = min(edgeDist.x, edgeDist.y);
                
                float edgeFactor = 1.0 - pow(minEdgeDist * 2.0, _ChromaticEdge);
                edgeFactor = saturate(edgeFactor);
                
                float chromaticStrength = _ChromaticAmount * edgeFactor;
                
                float2 offsetR = float2(chromaticStrength * 3.0, 0);
                float2 offsetG = float2(chromaticStrength * 1.5, 0);
                float2 offsetB = float2(-chromaticStrength, 0);
                
                float glitchLine = floor(uv.y * 20.0);
                float glitchRandom = random(float2(glitchLine, floor(time * _GlitchSpeed)));
                
                if(glitchRandom > 0.95)
                {
                    float glitchOffset = (random(float2(glitchLine + 10.0, time)) - 0.5) * _GlitchIntensity;
                    uv.x += glitchOffset;
                    offsetR.x += glitchOffset * 3.0;
                    offsetG.x += glitchOffset * 1.5;
                    offsetB.x -= glitchOffset;
                }
                
                float r = tex2D(_MainTex, uv + offsetR).r;
                float g = tex2D(_MainTex, uv + offsetG).g;
                float b = tex2D(_MainTex, uv + offsetB).b;
                float a = tex2D(_MainTex, uv).a;
                
                fixed4 col = fixed4(r, g, b, a) * i.color;
                
                col.r += edgeFactor * _ChromaticShift * 0.3;
                col.g -= edgeFactor * _ChromaticShift * 0.2;
                col.b += edgeFactor * _ChromaticShift * 0.4;
                
                float scanlinePos = frac((uv.y * _ScanlineCount) - (time * _ScanlineSpeed));
                float scanline = step(0.5, scanlinePos);
                col.rgb *= 1.0 - (scanline * _ScanlineIntensity);
                
                float2 moireUV = uv * _MoireScale;
                float moire1 = sin(moireUV.x + time * _MoireSpeed);
                float moire2 = sin(moireUV.y - time * _MoireSpeed * 0.7);
                float moirePattern = (moire1 + moire2) * 0.5;
                
                col.r += moirePattern * _MoireIntensity * 0.5;
                col.g -= moirePattern * _MoireIntensity * 0.4;
                col.b += moirePattern * _MoireIntensity * 0.6;
                
                float angle = radians(_StripeAngle);
                float cosA = cos(angle);
                float sinA = sin(angle);
                float2 rotatedUV = float2(uv.x * cosA - uv.y * sinA, uv.x * sinA + uv.y * cosA);
                
                float stripePattern = frac((rotatedUV.x + time * _StripeSpeed * 0.1) / _StripeSpacing);
                float stripe = smoothstep(_StripeWidth, 0.0, stripePattern);
                col.rgb += stripe * _StripeColor.rgb * _StripeColor.a;
                
                float border = smoothstep(_BorderWidth + 0.005, _BorderWidth, minEdgeDist);
                float borderGlow = smoothstep(_BorderWidth * 2.0, _BorderWidth, minEdgeDist);
                borderGlow = pow(borderGlow, 2.0) * _BorderGlow;
                
                float borderR = smoothstep(_BorderWidth + 0.01, _BorderWidth + 0.005, minEdgeDist);
                float borderG = smoothstep(_BorderWidth + 0.008, _BorderWidth + 0.003, minEdgeDist);
                float borderB = smoothstep(_BorderWidth + 0.006, _BorderWidth + 0.001, minEdgeDist);
                
                fixed3 borderChromatic = fixed3(borderR * _BorderColor.r, borderG * _BorderColor.g, borderB * _BorderColor.b);
                
                col.rgb = lerp(col.rgb, borderChromatic * 2.0, border);
                col.rgb += borderGlow * _BorderColor.rgb * 0.3;
                
                float fog = random(uv * 50.0 + time * 0.1) * _FogAmount;
                col.rgb += fog * 0.1;
                
                col.a *= cornerAlpha;
                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                
                return col;
            }
            ENDCG
        }
    }
}