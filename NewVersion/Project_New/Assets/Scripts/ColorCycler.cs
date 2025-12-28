using UnityEngine;

public class PsychedelicCamera : MonoBehaviour
{
    [Header("1. 迷幻参数")]
    [Tooltip("流动的速度")]
    public float speed = 1.0f;

    [Tooltip("色彩饱和度 (0=灰, 0.5=柔和, 1=极其鲜艳)")]
    [Range(0, 1)]
    public float saturation = 0.8f;

    [Tooltip("亮度 (0=黑, 1=亮)")]
    [Range(0, 1)]
    public float brightness = 1.0f;

    [Header("2. RGB 相位偏移 (数学魔法)")]
    // 默认设置好的黄金比例偏移，能产生最完美的彩虹循环
    // 你也可以乱改这三个数，会产生不同的色调风格
    public float redOffset = 0f;
    public float greenOffset = 2f; // 2.094弧度 = 120度
    public float blueOffset = 4f;  // 4.188弧度 = 240度

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        // 自动确保摄像机模式正确
        if (cam.clearFlags != CameraClearFlags.SolidColor)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
    }

    void Update()
    {
        if (cam == null) return;

        // --- 核心算法：基于时间的连续正弦波 ---
        // Mathf.Sin 输出 -1 到 1
        // 我们把它映射到 0 到 1 的颜色区间
        
        float time = Time.time * speed;

        // 计算 RGB 通道 (算法核心)
        // 这里的 * 0.5 + 0.5 是为了把 Sin 的 (-1, 1) 区间变成 (0, 1)
        float r = (Mathf.Sin(time + redOffset) * 0.5f) + 0.5f;
        float g = (Mathf.Sin(time + greenOffset) * 0.5f) + 0.5f;
        float b = (Mathf.Sin(time + blueOffset) * 0.5f) + 0.5f;

        // 此时我们得到的是纯粹的 RGB 彩虹
        Color trippyColor = new Color(r, g, b);

        // --- 进阶处理：转换到 HSV 调整饱和度和亮度 ---
        // 这一步是为了防止颜色太“生硬”或者“太暗”
        float H, S, V;
        Color.RGBToHSV(trippyColor, out H, out S, out V);
        
        // 强制应用你设置的饱和度和亮度，保留算法生成的色相(H)
        Color finalColor = Color.HSVToRGB(H, saturation, brightness);

        // 应用给摄像机
        cam.backgroundColor = finalColor;
    }
}