using UnityEngine;

public class UIMover : MonoBehaviour
{
    [Header("运动参数")]
    public float MaxMoveRange = 120f; // 记得保持和 Spawner 一致
    public float MoveSpeed = 35f; 

    private Vector3 initialPosition;
    private int moveDirection = 1; 

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        float currentY = rectTransform.anchoredPosition.y;

        // 如果出生在高处 (大于1)
        if (Mathf.Abs(currentY) > 1f)
        {
            moveDirection = -1; // 向下走

            // 【修复点在此】把 new Vector3 改成了 new Vector2
            // 这样 Vector2 减 Vector2 就不会报错了
            initialPosition = rectTransform.anchoredPosition - new Vector2(0, MaxMoveRange);
        }
        else
        {
            moveDirection = 1; // 向上走
            initialPosition = rectTransform.anchoredPosition;
        }
    }

    void Update()
    {
        Vector2 currentPosition = rectTransform.anchoredPosition;
        
        currentPosition.y += moveDirection * MoveSpeed * Time.deltaTime;
        rectTransform.anchoredPosition = currentPosition;

        float yOffset = currentPosition.y - initialPosition.y;

        if (moveDirection > 0 && yOffset >= MaxMoveRange)
        {
            currentPosition.y = initialPosition.y + MaxMoveRange;
            rectTransform.anchoredPosition = currentPosition;
            moveDirection = -1;
        }
        else if (moveDirection < 0 && yOffset <= 0)
        {
            currentPosition.y = initialPosition.y;
            rectTransform.anchoredPosition = currentPosition;
            moveDirection = 1;
        }
    }
}