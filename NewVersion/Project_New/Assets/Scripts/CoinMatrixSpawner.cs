using UnityEngine;
using UnityEngine.UI;

public class CoinMatrixSpawner : MonoBehaviour
{
    [Header("1. 要复制的列模板")]
    public GameObject columnPrefab; 

    [Header("2. 布局参数")]
    public float columnWidth = 100f; 
    public float columnSpacing = 150f; // 间隔 150
    public float maxRangeForToggle = 120f; // UIMover 脚本中的 MaxMoveRange

    [Header("3. 引用 (已优化，请拖入整个 Canvas 对象)")]
    public GameObject canvasObject; 

    void Start()
    {
        // 1. 检查 Canvas 引用是否设置
        if (canvasObject == null)
        {
            Debug.LogError("请将 Hierarchy 中的 Canvas 对象拖入 'Canvas Object' 槽位。");
            return;
        }

        // 2. 从 Canvas 对象中获取 RectTransform 组件
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        // 3. 检查 RectTransform 是否获取成功
        if (canvasRect == null)
        {
            Debug.LogError("Canvas 对象缺少 RectTransform 组件。");
            return;
        }
        
        // --- 开始生成逻辑 ---
        float canvasWidth = canvasRect.rect.width;
        float totalColumnSpan = columnWidth + columnSpacing; 
        int numColumns = Mathf.CeilToInt(canvasWidth / totalColumnSpan) + 2; 

        float startX = (-numColumns / 2f) * totalColumnSpan; 
        
        for (int i = 0; i < numColumns; i++)
        {
            // 这里保持你原来的逻辑：生成在 BatcherManager 下面
            GameObject newColumn = Instantiate(columnPrefab, this.transform);
            RectTransform columnRect = newColumn.GetComponent<RectTransform>();
            
            float currentX = startX + i * totalColumnSpan;
            float currentY = 0f;

            // 核心逻辑：设置奇数列的初始位置在摆动范围的最高点
            if (i % 2 != 0) // 奇数列 (i=1, 3, 5...)
            {
                currentY = maxRangeForToggle; 
            }

            // 设置位置
            columnRect.anchorMin = new Vector2(0.5f, 0.5f);
            columnRect.anchorMax = new Vector2(0.5f, 0.5f);
            columnRect.anchoredPosition = new Vector2(currentX, currentY);
            
            newColumn.name = $"CoinColumn_{i}";
        }

        // ==========================================
        // 👇 只加了这一步：生成完之后，把原来的模板藏起来 👇
        // ==========================================
        if (columnPrefab != null)
        {
            columnPrefab.SetActive(false);
        }
    }
}