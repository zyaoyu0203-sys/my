Shader "Custom/VaporwaveBackground"
{
    Properties
    {
        _Color1 ("颜色 1", Color) = (1, 0.44, 0.81, 1)      // 粉红色
        _Color2 ("颜色 2", Color) = (0, 1, 0.96, 1)         // 青色
        _Color3 ("颜色 3", Color) = (0.73, 0.4, 1, 1)       // 紫色
        _Color4 ("颜色 4", Color) = (0.01, 0.8, 1, 1)       // 蓝色
        _Color5 ("颜色 5", Color) = (1, 0.6, 0.2, 1)        // 橙色
        _Color6 ("颜色 6", Color) = (1, 1, 0.4, 1)          // 黄色
        _Speed ("变换速度", Range(0, 5)) = 0.8
        _Scale ("图案缩放", Range(0.1, 10)) = 2
        _WaveIntensity ("波浪强度", Range(0, 2)) = 0.6
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
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
            };

            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float4 _Color4;
            float4 _Color5;
            float4 _Color6;
            float _Speed;
            float _Scale;
            float _WaveIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float noise(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float smoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y * _Speed;
                float2 uv = i.uv * _Scale;
                
                float wave1 = sin(uv.x * 3.0 + time) * cos(uv.y * 2.0 + time * 0.5);
                float wave2 = sin(uv.y * 4.0 - time * 0.7) * cos(uv.x * 3.5 - time);
                float wave3 = smoothNoise(uv + time * 0.3);
                
                float pattern = (wave1 + wave2 + wave3 * 2.0) * _WaveIntensity;
                
                // 6个颜色的混合因子
                float factor1 = sin(pattern + time * 0.5) * 0.5 + 0.5;
                float factor2 = cos(pattern * 1.3 - time * 0.3) * 0.5 + 0.5;
                float factor3 = sin(pattern * 0.8 + time * 0.7) * 0.5 + 0.5;
                float factor4 = cos(pattern * 1.1 + time * 0.4) * 0.5 + 0.5;
                float factor5 = sin(pattern * 0.9 - time * 0.6) * 0.5 + 0.5;
                
                // 分层混合6个颜色
                fixed4 blend1 = lerp(_Color1, _Color2, factor1);
                fixed4 blend2 = lerp(_Color3, _Color4, factor2);
                fixed4 blend3 = lerp(_Color5, _Color6, factor3);
                
                fixed4 temp = lerp(blend1, blend2, factor4);
                fixed4 finalColor = lerp(temp, blend3, factor5);
                
                finalColor += sin(time + pattern) * 0.15;
                
                return finalColor;
            }
            ENDCG
        }
    }
}