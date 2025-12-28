Shader "Custom/GlitchHologram"
{
    Properties
    {
        _MainTex ("桌面贴图", 2D) = "white" {}
        _Color ("全息色调", Color) = (0.5, 0.9, 1, 1)
        _Transparency ("透明度", Range(0, 1)) = 0.6
        _TintStrength ("色调强度", Range(0, 1)) = 0.3
        
        [Header(Wave Distortion)]
        _DistortSpeed ("扭曲速度", Range(0, 3)) = 1
        _DistortAmount ("扭曲强度", Range(0, 0.3)) = 0.08
        
        [Header(Glitch Effect)]
        _GlitchFrequency ("Glitch频率", Range(0, 30)) = 8
        _GlitchIntensity ("Glitch强度", Range(0, 0.5)) = 0.15
        _RGBSplitAmount ("RGB分离强度", Range(0, 0.1)) = 0.03
        _InvertChance ("取反色几率", Range(0, 1)) = 0.1
        
        [Header(Scanline)]
        _ScanlineSpeed ("扫描线速度", Range(0, 10)) = 2
        _ScanlineFrequency ("扫描线频率", Range(1, 100)) = 30
        _ScanlineWidth ("扫描线宽度", Range(0.001, 0.1)) = 0.02
        _ScanlineColor ("扫描线颜色", Color) = (0.5, 1, 1, 1)
        _ScanlineBrightness ("扫描线亮度", Range(0, 3)) = 1
        
        [Header(Noise Lines)]
        _NoiseLineChance ("噪声线几率", Range(0, 1)) = 0.3
        _NoiseLineThickness ("噪声线粗细", Range(0.001, 0.05)) = 0.01
        
        [Header(Edge)]
        _EdgeGlow ("边缘发光", Range(0, 5)) = 2
    }
    
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _Transparency;
            float _TintStrength;
            float _DistortSpeed;
            float _DistortAmount;
            float _GlitchFrequency;
            float _GlitchIntensity;
            float _RGBSplitAmount;
            float _InvertChance;
            float _ScanlineSpeed;
            float _ScanlineFrequency;
            float _ScanlineWidth;
            float4 _ScanlineColor;
            float _ScanlineBrightness;
            float _NoiseLineChance;
            float _NoiseLineThickness;
            float _EdgeGlow;

            float random(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;
                float2 uv = i.uv;
                
                // === Glitch块状偏移 ===
                float glitchTime = floor(time * _GlitchFrequency);
                float2 blockID = floor(uv * 20.0);
                float blockRandom = random(blockID + glitchTime);
                
                // 随机水平块偏移
                if(blockRandom > 0.85)
                {
                    uv.x += (random(blockID + glitchTime + 10.0) - 0.5) * _GlitchIntensity;
                }
                
                // 整行随机偏移（更明显的glitch）
                float rowID = floor(uv.y * 50.0);
                float rowRandom = random(float2(rowID, glitchTime));
                if(rowRandom > 0.9)
                {
                    uv.x += (random(float2(rowID + 20.0, glitchTime)) - 0.5) * _GlitchIntensity * 2.0;
                }
                
                // === 波浪扭曲 ===
                uv.x += sin(uv.y * 10.0 + time * _DistortSpeed) * _DistortAmount;
                uv.y += cos(uv.x * 8.0 - time * _DistortSpeed * 0.7) * _DistortAmount;
                
                // === RGB色彩分离效果 ===
                float shouldSplit = step(0.92, random(float2(glitchTime, 1.0)));
                float2 offsetR = float2(_RGBSplitAmount, 0) * shouldSplit;
                float2 offsetB = float2(-_RGBSplitAmount, 0) * shouldSplit;
                
                float r = tex2D(_MainTex, uv + offsetR).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv + offsetB).b;
                float a = tex2D(_MainTex, uv).a;
                
                fixed4 col = fixed4(r, g, b, a);
                
                // === 取反色效果 ===
                float invertRandom = random(float2(glitchTime, 2.0));
                if(invertRandom < _InvertChance && blockRandom > 0.8)
                {
                    col.rgb = 1.0 - col.rgb;
                }
                
                // === 扫描线 ===
                float scanlinePos = frac(i.worldPos.y * _ScanlineFrequency / 10.0 - time * _ScanlineSpeed);
                float scanline = smoothstep(_ScanlineWidth, 0.0, abs(scanlinePos - 0.5)) * _ScanlineBrightness;
                
                // === 随机噪声横线 ===
                float noiseLine = 0.0;
                float lineY = floor(i.uv.y * 100.0);
                float lineRandom = random(float2(lineY, floor(time * 5.0)));
                if(lineRandom < _NoiseLineChance)
                {
                    float linePos = frac(i.uv.y * 100.0);
                    noiseLine = step(linePos, _NoiseLineThickness);
                }
                
                // === Fresnel边缘发光 ===
                float fresnel = pow(1.0 - saturate(dot(normalize(i.worldNormal), i.viewDir)), 2.5);
                fresnel *= _EdgeGlow;
                
                // === 组合效果 ===
                // 保持贴图清晰可见
                col.rgb = lerp(col.rgb, col.rgb * _Color.rgb, _TintStrength);
                
                // 添加扫描线（用自定义颜色）
                col.rgb += scanline * _ScanlineColor.rgb;
                
                // 添加噪声线
                col.rgb += noiseLine * float3(1, 1, 1) * 0.5;
                
                // 添加边缘发光
                col.rgb += fresnel * _Color.rgb;
                
                // Glitch时的额外效果
                if(blockRandom > 0.88)
                {
                    // 添加色块
                    col.rgb += float3(
                        random(blockID + 30.0),
                        random(blockID + 40.0),
                        random(blockID + 50.0)
                    ) * 0.3;
                }
                
                // 透明度
                col.a = _Transparency;
                col.a += scanline * 0.3;
                col.a += fresnel * 0.4;
                
                return col;
            }
            ENDCG
        }
    }
}
