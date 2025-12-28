using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    [Header("抖动设置")]
    public float shakeIntensity = 0.5f;
    public float shakeDuration = 0.8f;
    
    [Header("睁眼效果")]
    public GameObject eyeOverlay;
    public float initialLookDownAngle = -20f;  // 初始低头角度（更低）
    
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isShaking = false;
    
    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        // 游戏开始时低头
        transform.rotation = originalRotation * Quaternion.Euler(initialLookDownAngle, 0f, 0f);
        
        Debug.Log("摄像机初始化：低头 " + initialLookDownAngle + " 度");
        
        // 检查EyeOverlay
        if(eyeOverlay == null)
        {
            Debug.LogError("Eye Overlay 未连接！请在Inspector里连接UI的EyeOverlay");
        }
        else
        {
            Debug.Log("Eye Overlay 已连接");
        }
    }
    
    public void Shake()
    {
        if(!isShaking)
        {
            StartCoroutine(ShakeCoroutine());
        }
    }
    
    public void Shake(float intensity, float duration)
    {
        if(!isShaking)
        {
            StartCoroutine(ShakeCoroutine(intensity, duration));
        }
    }
    
    IEnumerator ShakeCoroutine()
    {
        yield return ShakeCoroutine(shakeIntensity, shakeDuration);
    }
    
    IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        isShaking = true;
        Vector3 startPos = transform.position;
        
        float elapsed = 0f;
        
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percentComplete = elapsed / duration;
            float damper = 1.0f - Mathf.Clamp01(percentComplete);
            
            float x = Random.Range(-1f, 1f) * intensity * damper;
            float y = Random.Range(-1f, 1f) * intensity * damper;
            
            transform.position = new Vector3(
                startPos.x + x,
                startPos.y + y,
                startPos.z
            );
            
            yield return null;
        }
        
        transform.position = startPos;
        isShaking = false;
    }
    
    public void WakeUpSequence()
    {
        StartCoroutine(WakeUpCoroutine());
    }
    
    IEnumerator WakeUpCoroutine()
    {
        Debug.Log("=== 开始睁眼序列 ===");
        
        // === 检查EyeOverlay ===
        if(eyeOverlay == null)
        {
            Debug.LogError("Eye Overlay未连接，跳过睁眼动画，直接抬头");
            // 只做抬头动画
            yield return StartCoroutine(LiftHeadWithBounce(2.0f));
            yield break;
        }
        
        // 确保遮罩激活
        eyeOverlay.SetActive(true);
        CanvasGroup group = eyeOverlay.GetComponent<CanvasGroup>();
        if(group == null) 
        {
            group = eyeOverlay.AddComponent<CanvasGroup>();
            Debug.Log("自动添加CanvasGroup组件");
        }
        group.alpha = 1f;
        
        Debug.Log("Eye Overlay已激活，Alpha = 1");
        
        yield return new WaitForSeconds(0.5f);
        
        // 第一次睁眼
        Debug.Log("第一次快速睁眼");
        yield return StartCoroutine(BlinkEye(0.4f, 0.3f));
        
        // 闭眼
        Debug.Log("闭眼");
        yield return StartCoroutine(CloseEye(0.2f));
        
        yield return new WaitForSeconds(0.4f);
        
        // 完全睁开 + 抬头（带回弹）
        Debug.Log("完全睁眼 + 抬头");
        StartCoroutine(LiftHeadWithBounce(2.0f));
        yield return StartCoroutine(OpenEyesFully(2.0f));
        
        Debug.Log("=== 睁眼序列完成 ===");
    }
    
    IEnumerator BlinkEye(float targetAlpha, float duration)
    {
        if(eyeOverlay == null) yield break;
        
        CanvasGroup group = eyeOverlay.GetComponent<CanvasGroup>();
        float elapsed = 0f;
        float startAlpha = 1f;
        
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        
        group.alpha = targetAlpha;
    }
    
    IEnumerator CloseEye(float duration)
    {
        if(eyeOverlay == null) yield break;
        
        CanvasGroup group = eyeOverlay.GetComponent<CanvasGroup>();
        float elapsed = 0f;
        float startAlpha = group.alpha;
        
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / duration);
            yield return null;
        }
        
        group.alpha = 1f;
    }
    
    IEnumerator OpenEyesFully(float duration)
    {
        if(eyeOverlay == null) yield break;
        
        CanvasGroup group = eyeOverlay.GetComponent<CanvasGroup>();
        float elapsed = 0f;
        
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            group.alpha = Mathf.Lerp(1f, 0f, easeT);
            yield return null;
        }
        
        group.alpha = 0f;
        eyeOverlay.SetActive(false);
    }
    
    // === 修改：抬头带回弹效果 ===
    IEnumerator LiftHeadWithBounce(float duration)
    {
        float elapsed = 0f;
        Quaternion startRotation = transform.rotation;  // 低头状态
        Quaternion targetRotation = originalRotation;   // 正常视角
        
        // 计算中间过冲角度（抬过头再回来一点）
        float overshootAngle = 5f;  // 过冲5度
        Quaternion overshootRotation = originalRotation * Quaternion.Euler(overshootAngle, 0f, 0f);
        
        Debug.Log("开始抬头动画：低头 → 过冲 → 正常");
        
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            Quaternion currentRotation;
            
            // 分阶段：0-0.6是抬头并过冲，0.6-1.0是回落到正常
            if(t < 0.6f)
            {
                // 阶段1：从低头到过冲（抬过头）
                float t1 = t / 0.6f;
                float easeT1 = EaseOutCubic(t1);
                currentRotation = Quaternion.Slerp(startRotation, overshootRotation, easeT1);
            }
            else
            {
                // 阶段2：从过冲回到正常
                float t2 = (t - 0.6f) / 0.4f;
                float easeT2 = EaseInOutQuad(t2);
                currentRotation = Quaternion.Slerp(overshootRotation, targetRotation, easeT2);
            }
            
            transform.rotation = currentRotation;
            yield return null;
        }
        
        transform.rotation = originalRotation;
        Debug.Log("抬头完成");
    }
    
    // 缓动函数
    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
    
    float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }
}