using UnityEngine;
using System.Collections;

public class CharacterController : MonoBehaviour
{
    [Header("飘浮效果")]
    public float floatSpeed = 1f;
    public float floatAmount = 0.3f;
    private Vector3 startPos;
    
    [Header("眨眼效果")]
    public Sprite normalSprite;
    public Sprite blinkSprite;
    public float blinkInterval = 3f;
    public float blinkDuration = 0.1f;
    private SpriteRenderer spriteRenderer;
    
    [Header("说话抖动")]
    public float shakeAmount = 0.15f;      // 改大一点
    public float shakeDuration = 0.08f;    // 改快一点
    
    private bool isTalking = false;
    
    void Start()
    {
        startPos = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if(blinkSprite != null)
        {
            StartCoroutine(BlinkRoutine());
        }
    }
    
    void Update()
    {
        // 只有不在说话时才飘浮
        if(!isTalking)
        {
            Float();
        }
    }
    
    void Float()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
    
    IEnumerator BlinkRoutine()
    {
        while(true)
        {
            yield return new WaitForSeconds(blinkInterval + Random.Range(-1f, 1f));
            
            spriteRenderer.sprite = blinkSprite;
            yield return new WaitForSeconds(blinkDuration);
            
            spriteRenderer.sprite = normalSprite;
        }
    }
    
    public void Talk()
    {
        Debug.Log("Talk() 被调用了！");
        StopCoroutine("ShakeRoutine");  // 停止之前的抖动
        StartCoroutine(ShakeRoutine());
    }
    
    IEnumerator ShakeRoutine()
    {
        isTalking = true;
        Vector3 originalPos = transform.position;
        
        Debug.Log("开始抖动，原始位置: " + originalPos);
        
        // 抖3次，更明显
        for(int i = 0; i < 3; i++)
        {
            // 下
            transform.position = originalPos + Vector3.down * shakeAmount;
            yield return new WaitForSeconds(shakeDuration);
            
            // 上
            transform.position = originalPos + Vector3.up * shakeAmount;
            yield return new WaitForSeconds(shakeDuration);
            
            // 回中
            transform.position = originalPos;
            yield return new WaitForSeconds(shakeDuration);
        }
        
        // 确保回到原位
        transform.position = originalPos;
        isTalking = false;
        
        Debug.Log("抖动结束");
    }
}
