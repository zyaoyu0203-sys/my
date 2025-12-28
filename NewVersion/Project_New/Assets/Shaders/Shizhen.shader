Shader "Custom/GlitchSymbol"
{
    Properties
    {
        _MainTex ("符号贴图", 2D) = "white" {}
        _Color ("颜色叠加", Color) = (1, 1, 1, 1)
        _DistortionAmount ("失真强度", Range(0, 0.1)) = 0.02
        _DistortionSpeed ("失真速度", Range(0, 5)) = 1.5
        _FlickerChance ("闪烁几率", Range(0, 1)) = 0.05  // 1/20 = 0.05
        _FlickerBrightness ("闪烁亮度", Range(1, 3)) = 1.8
        _FlickerSpeed ("闪烁检测速度", Range(1, 20)) = 10
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _DistortionAmount;
            float _DistortionSpeed;
            float _FlickerChance;
            float _FlickerBrightness;
            float _FlickerSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            // 随机数生成
            float random(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // 基于时间和位置的随机
            float randomTime(float2 p, float time)
            {
                return random(p + floor(time));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y * _DistortionSpeed;
                float flickerTime = _Time.y * _FlickerSpeed;
                
                // === 失真效果 ===
                // 使用正弦波产生轻微的UV扭曲
                float2 distortion = float2(
                    sin(i.uv.y * 10.0 + time * 2.0) * _DistortionAmount,
                    cos(i.uv.x * 8.0 + time * 1.5) * _DistortionAmount
                );
                
                // 添加噪声失真
                float noise = random(i.uv + time * 0.1);
                distortion += (noise - 0.5) * _DistortionAmount * 0.5;
                
                // 应用失真到UV
                float2 distortedUV = i.uv + distortion;
                
                // 采样贴图
                fixed4 col = tex2D(_MainTex, distortedUV);
                
                // === 闪烁效果 ===
                // 使用物体的世界坐标作为随机种子，确保每个符号独立闪烁
                float2 seed = i.worldPos.xy * 100.0;
                float flickerRandom = randomTime(seed, flickerTime);
                
                // 判断是否应该闪烁 (1/20的概率)
                float isFlickering = step(1.0 - _FlickerChance, flickerRandom);
                
                // 闪烁时增加亮度
                float brightness = lerp(1.0, _FlickerBrightness, isFlickering);
                
                // 应用颜色和闪烁
                col *= _Color;
                col.rgb *= brightness;
                
                // === RGB偏移效果（可选的额外失真） ===
                // 在闪烁时添加轻微的色彩分离效果
                if(isFlickering > 0.5)
                {
                    float2 offset = float2(_DistortionAmount * 2.0, 0);
                    float r = tex2D(_MainTex, distortedUV + offset).r;
                    float b = tex2D(_MainTex, distortedUV - offset).b;
                    col.r = lerp(col.r, r, 0.3);
                    col.b = lerp(col.b, b, 0.3);
                }
                
                return col;
            }
            ENDCG
        }
    }
}