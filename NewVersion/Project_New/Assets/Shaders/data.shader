Shader "Custom/HologramTopOnly"
{
    Properties
    {
        _MainTex ("桌面贴图（只显示在顶面）", 2D) = "white" {}
        _Color ("全息颜色", Color) = (0.4, 0.8, 1, 1)
        _Transparency ("透明度", Range(0, 1)) = 0.5
        
        [Header(Distortion)]
        _DistortionAmount ("失真强度", Range(0, 0.2)) = 0.05
        _DistortionSpeed ("失真速度", Range(0, 3)) = 1
        
        [Header(Scanline)]
        _ScanlineSpeed ("扫描线速度", Range(0, 10)) = 2
        _ScanlineCount ("扫描线数量", Range(10, 100)) = 40
        _ScanlineColor ("扫描线颜色", Color) = (0.3, 0.9, 1, 1)
        _ScanlineBrightness ("扫描线亮度", Range(0, 2)) = 0.8
        
        [Header(Edge Glow)]
        _EdgeColor ("边缘颜色", Color) = (0.5, 1, 1, 1)
        _EdgeIntensity ("边缘强度", Range(0, 5)) = 2
        _FresnelPower ("边缘锐度", Range(1, 10)) = 3
        
        [Header(Flicker)]
        _FlickerSpeed ("闪烁速度", Range(0, 20)) = 5
        _FlickerAmount ("闪烁强度", Range(0, 0.3)) = 0.1
        
        [Header(Side Settings)]
        _SideTransparency ("侧面透明度", Range(0, 1)) = 0.1
        
        [Header(Transparency Control)]
        _AlphaCutoff ("透明度阈值", Range(0, 1)) = 0.1
        _DarknessThreshold ("黑色阈值", Range(0, 1)) = 0.05
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent"
        }
        
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
            float4 _MainTex_ST;
            float4 _Color;
            float _Transparency;
            float _DistortionAmount;
            float _DistortionSpeed;
            float _ScanlineSpeed;
            float _ScanlineCount;
            float4 _ScanlineColor;
            float _ScanlineBrightness;
            float4 _EdgeColor;
            float _EdgeIntensity;
            float _FresnelPower;
            float _FlickerSpeed;
            float _FlickerAmount;
            float _SideTransparency;
            float _AlphaCutoff;
            float _DarknessThreshold;

            float random(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;
                float2 uv = i.uv;
                
                // === 判断是否为顶面 ===
                float3 worldNormal = normalize(i.worldNormal);
                float isTopFace = step(0.9, worldNormal.y);
                
                // === UV失真 ===
                float2 distortion = float2(
                    sin(uv.y * 15.0 + time * _DistortionSpeed) * _DistortionAmount,
                    cos(uv.x * 12.0 - time * _DistortionSpeed * 0.8) * _DistortionAmount
                );
                
                float noise = random(floor(uv * 50.0) + floor(time * 2.0));
                distortion += (noise - 0.5) * _DistortionAmount * 0.5;
                
                uv += distortion;
                
                // === 采样贴图 ===
                fixed4 texColor = tex2D(_MainTex, uv);
                
                // ====== 新增：检测无贴图区域 ======
                // 计算亮度
                float brightness = dot(texColor.rgb, float3(0.299, 0.587, 0.114));
                
                // 如果贴图alpha太低或者颜色太暗，直接丢弃（完全透明）
                if(texColor.a < _AlphaCutoff || brightness < _DarknessThreshold)
                {
                    discard;  // 完全不渲染这个像素
                }
                // ====================================
                
                // === 扫描线 ===
                float scanlinePos = frac(i.worldPos.y * _ScanlineCount / 10.0 - time * _ScanlineSpeed);
                float scanline = smoothstep(0.02, 0.0, abs(scanlinePos - 0.5)) * _ScanlineBrightness;
                
                // === Fresnel边缘发光 ===
                float fresnel = 1.0 - saturate(dot(worldNormal, i.viewDir));
                fresnel = pow(fresnel, _FresnelPower) * _EdgeIntensity;
                
                // === 全息闪烁 ===
                float flicker = sin(time * _FlickerSpeed) * _FlickerAmount + 1.0;
                flicker *= (1.0 + random(floor(time * _FlickerSpeed)) * _FlickerAmount);
                
                // === 组合效果 ===
                fixed4 finalColor;
                
                if(isTopFace > 0.5)
                {
                    // 顶面：显示贴图 + 所有效果
                    finalColor = texColor;
                    finalColor.rgb = lerp(finalColor.rgb, finalColor.rgb * _Color.rgb, 0.3);
                    finalColor.rgb += scanline * _ScanlineColor.rgb;
                    finalColor.rgb += fresnel * _EdgeColor.rgb;
                    finalColor.rgb *= flicker;
                    finalColor.a = _Transparency + fresnel * 0.5 + scanline * 0.2;
                }
                else
                {
                    // 侧面：只有边缘光，超透明
                    finalColor = float4(_EdgeColor.rgb * fresnel, _SideTransparency + fresnel * 0.3);
                }
                
                finalColor.a = saturate(finalColor.a);
                
                return finalColor;
            }
            ENDCG
        }
    }
}