using UnityEngine;
using UnityEngine.UI;

public class CoinMatrixSpawner : MonoBehaviour
{
    [Header("1. 要复制的列模板")]
    public GameObject columnPrefab; 

    [Header("2. 布局参数")]
    public float columnWidth = 100f; 
    public float columnSpacing = 150f; 
    public float maxRangeForToggle = 120f; 

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

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        if (canvasRect == null) return;
        
        // --- 开始生成逻辑 ---
        float canvasWidth = canvasRect.rect.width;
        float totalColumnSpan = columnWidth + columnSpacing; 
        int numColumns = Mathf.CeilToInt(canvasWidth / totalColumnSpan) + 2; 

        float startX = (-numColumns / 2f) * totalColumnSpan; 
        
        for (int i = 0; i < numColumns; i++)
        {
            GameObject newColumn = Instantiate(columnPrefab, this.transform);
            
            // =============================================
            // 🚨 【关键修复】强制激活！让它“睁开眼睛” 🚨
            // 否则如果模板是关着的，生出来的也是关着的。
            // =============================================
            newColumn.SetActive(true); 

            RectTransform columnRect = newColumn.GetComponent<RectTransform>();
            
            float currentX = startX + i * totalColumnSpan;
            float currentY = 0f;

            if (i % 2 != 0) 
            {
                currentY = maxRangeForToggle; 
            }

            columnRect.anchorMin = new Vector2(0.5f, 0.5f);
            columnRect.anchorMax = new Vector2(0.5f, 0.5f);
            columnRect.anchoredPosition = new Vector2(currentX, currentY);
            
            newColumn.name = $"CoinColumn_{i}";
        }

        // 生成完之后，把原来的模板隐藏掉，防止重叠
        if (columnPrefab != null)
        {
            columnPrefab.SetActive(false);
        }
    }
}