Shader "Custom/HologramSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("全息颜色", Color) = (0.5, 0.9, 1, 0.9)
        _Brightness ("亮度", Range(0.5, 3)) = 1.5
        
        [Header(Hologram Effect)]
        _ScanlineSpeed ("扫描线速度", Range(0, 5)) = 1.5
        _ScanlineIntensity ("扫描线强度", Range(0, 1)) = 0.3
        _EdgeGlow ("边缘发光", Range(0, 2)) = 0.5
        _Flicker ("闪烁强度", Range(0, 0.2)) = 0.05
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
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        
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
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Brightness;
            float _ScanlineSpeed;
            float _ScanlineIntensity;
            float _EdgeGlow;
            float _Flicker;

            float random(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;
                
                // 采样贴图
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // 如果透明就丢弃
                if(col.a < 0.1) discard;
                
                // 增加基础亮度
                col.rgb *= _Brightness;
                
                // 应用全息颜色
                col.rgb *= _Color.rgb;
                col *= i.color;
                
                // 扫描线效果
                float scanline = frac(i.worldPos.y * 8.0 - time * _ScanlineSpeed);
                scanline = smoothstep(0.92, 1.0, scanline) * _ScanlineIntensity;
                col.rgb += scanline * _Color.rgb;
                
                // 边缘发光
                float2 edge = abs(i.uv - 0.5) * 2.0;
                float edgeFactor = 1.0 - max(edge.x, edge.y);
                edgeFactor = pow(edgeFactor, 3.0) * _EdgeGlow;
                col.rgb += edgeFactor * _Color.rgb;
                
                // 全息闪烁
                float flicker = 1.0 + sin(time * 8.0) * _Flicker;
                flicker *= (1.0 + random(floor(time * 3.0)) * _Flicker);
                col.rgb *= flicker;
                
                // 透明度
                col.a *= _Color.a;
                
                // Premultiply alpha
                col.rgb *= col.a;
                
                return col;
            }
            ENDCG
        }
    }
}