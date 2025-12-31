Shader "Custom/PlasmaLine"
{
    Properties
    {
        _Color ("Line Color", Color) = (0.3, 0.8, 1, 1)
        _Brightness ("Brightness", Range(0, 5)) = 2
        _FlowSpeed ("Flow Speed", Range(0, 10)) = 3
        _FlickerSpeed ("Flicker Speed", Range(0, 20)) = 8
        _Glow ("Glow", Range(0, 3)) = 1.5
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        
        Blend One One
        Cull Off
        ZWrite Off
        Lighting Off
        
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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            fixed4 _Color;
            float _Brightness;
            float _FlowSpeed;
            float _FlickerSpeed;
            float _Glow;

            float hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;
                float2 uv = i.uv;
                
                // 距离中心
                float dist = abs(uv.y - 0.5);
                
                // 基础线条强度
                float lineIntensity = 1.0 - smoothstep(0.0, 0.15, dist);
                
                // 流动效果
                float flow1 = sin(uv.x * 10.0 - time * _FlowSpeed) * 0.5 + 0.5;
                float flow2 = sin(uv.x * 15.0 + time * _FlowSpeed * 1.3) * 0.5 + 0.5;
                lineIntensity *= (0.5 + max(flow1, flow2) * 0.8);
                
                // 闪烁
                float flickerTime = floor(time * _FlickerSpeed);
                float flicker = hash(flickerTime);
                
                if(flicker > 0.7)
                {
                    lineIntensity *= lerp(0.3, 1.0, hash(flickerTime * 10.0));
                }
                
                // 随机消失
                if(flicker > 0.95)
                {
                    lineIntensity *= 0.1;
                }
                
                // 发光
                float glowIntensity = exp(-dist * 8.0) * _Glow;
                glowIntensity *= (1.0 - flicker * 0.3);
                
                // 最终颜色
                float totalIntensity = (lineIntensity + glowIntensity) * _Brightness;
                fixed3 finalColor = _Color.rgb * totalIntensity * i.color.rgb;
                
                return fixed4(finalColor, totalIntensity * _Color.a);
            }
            ENDCG
        }
    }
}
