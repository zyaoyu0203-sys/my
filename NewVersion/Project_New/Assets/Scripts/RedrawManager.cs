using UnityEngine;
using System.Collections;

/// <summary>
/// 一键重抽管理器 - 调试版本
/// 按J键把当前手牌全部放回牌堆，重新抽牌
/// </summary>
public class RedrawManager : MonoBehaviour
{
    public static RedrawManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("重抽快捷键")]
    public KeyCode redrawKey = KeyCode.J;
    
    [Tooltip("重抽次数限制（0=无限）")]
    public int maxRedraws = 0;
    
    [Tooltip("是否在重抽前清除选中状态")]
    public bool clearSelectionBeforeRedraw = true;

    private int redrawCount = 0;
    private HorizontalCardHolder cardHolder;

    private void Awake()
    {
        Debug.Log("=== RedrawManager Awake 开始 ===");
        
        if (Instance != null && Instance != this)
        {
            Debug.LogError("RedrawManager重复！销毁");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        Debug.Log("RedrawManager Instance已设置");
    }

    private void Start()
    {
        Debug.Log("=== RedrawManager Start 开始 ===");
        
        cardHolder = FindObjectOfType<HorizontalCardHolder>();
        
        if (cardHolder == null)
        {
            Debug.LogError("未找到HorizontalCardHolder！重抽功能无法使用");
        }
        else
        {
            Debug.Log($"找到HorizontalCardHolder: {cardHolder.name}");
        }
        
        if (DeckManager.Instance == null)
        {
            Debug.LogError("未找到DeckManager.Instance！");
        }
        else
        {
            Debug.Log("找到DeckManager.Instance");
        }
        
        Debug.Log($"RedrawManager已启动！按 {redrawKey} 键重抽手牌");
    }

    private void Update()
    {
        // 按J键重抽
        if (Input.GetKeyDown(redrawKey))
        {
            Debug.Log($"检测到按键 {redrawKey}");
            
            if (CanRedraw())
            {
                Debug.Log("开始重抽流程");
                StartCoroutine(RedrawCards());
            }
            else
            {
                Debug.LogWarning($"无法重抽：已达到次数上限（{maxRedraws}次），当前已重抽{redrawCount}次");
            }
        }
    }

    /// <summary>
    /// 执行重抽
    /// </summary>
    private IEnumerator RedrawCards()
    {
        Debug.Log("=== 开始重抽流程 ===");

        // 1. 清除选中状态
        if (clearSelectionBeforeRedraw && CardManager.Instance != null)
        {
            Debug.Log("清除选中状态");
            CardManager.Instance.DeselectAllCards();
        }

        // 2. 检查cardHolder
        if (cardHolder == null)
        {
            Debug.LogError("cardHolder是null！无法重抽");
            yield break;
        }

        // 3. 获取所有手牌
        Card[] allCards = cardHolder.GetComponentsInChildren<Card>();
        Debug.Log($"找到 {allCards.Length} 张卡牌对象");
        
        // 4. 把所有有数据的卡牌放回牌堆
        int returnedCount = 0;
        foreach (var card in allCards)
        {
            if (card != null)
            {
                Debug.Log($"检查卡牌: {card.name}, IsEmpty: {card.IsEmpty()}");
                
                if (!card.IsEmpty() && card.cardData != null)
                {
                    Debug.Log($"准备放回卡牌: {card.cardData.cardName}");
                    
                    // 放回牌堆
                    if (DeckManager.Instance != null)
                    {
                        DeckManager.Instance.ReturnCardToDeck(card.cardData);
                        returnedCount++;
                    }
                    else
                    {
                        Debug.LogError("DeckManager.Instance是null！");
                    }
                    
                    // 清空卡牌
                    card.ClearCard();
                    Debug.Log($"卡牌已清空");
                }
            }
        }

        Debug.Log($"已将 {returnedCount} 张卡牌放回牌堆");

        // 5. 等待一小会
        yield return new WaitForSeconds(0.3f);

        // 6. 重新发牌
        Debug.Log("准备重新发牌");
        if (cardHolder != null)
        {
            cardHolder.DealCards();
            Debug.Log("DealCards()已调用");
        }
        else
        {
            Debug.LogError("cardHolder是null，无法发牌");
        }

        // 7. 增加重抽次数
        redrawCount++;

        Debug.Log($"=== 重抽完成（第 {redrawCount} 次）===");
    }

    /// <summary>
    /// 检查是否还能重抽
    /// </summary>
    public bool CanRedraw()
    {
        if (maxRedraws <= 0)
            return true; // 无限重抽
        
        return redrawCount < maxRedraws;
    }

    /// <summary>
    /// 获取剩余重抽次数
    /// </summary>
    public int GetRemainingRedraws()
    {
        if (maxRedraws <= 0)
            return -1; // 无限

        return Mathf.Max(0, maxRedraws - redrawCount);
    }

    /// <summary>
    /// 重置重抽次数（游戏重新开始时调用）
    /// </summary>
    public void ResetRedrawCount()
    {
        redrawCount = 0;
        Debug.Log("重抽次数已重置");
    }
}