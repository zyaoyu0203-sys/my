using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    // 追踪所有被选中的卡牌
    private List<Card> selectedCards = new List<Card>();

    [Header("Animation Settings")]
    [SerializeField] private float cardMoveSpeed = 0.5f;
    [SerializeField] private float cardStayDuration = 1f;
    [SerializeField] private Vector3 centerScreenOffset = Vector3.zero;
    [Tooltip("卡牌渐隐消失的时间（秒）")]
    [SerializeField] private float cardFadeOutDuration = 1f;
    
    [Header("Auto Refill")]
    [Tooltip("打出或弃牌后是否自动补牌")]
    [SerializeField] private bool autoRefillCards = true;
    [Tooltip("自动补牌延迟时间（秒）")]
    [SerializeField] private float refillDelay = 0.3f;

    [Header("Special Deal Mode")]
    [Tooltip("启用特殊发牌模式：第一次发基础牌，第二次发金牌+黑牌，然后恢复正常")]
    [SerializeField] private bool enableSpecialDealMode = false;
    [Tooltip("第二次发牌的金牌数量（0-3）")]
    [SerializeField] [Range(0, 3)] private int goldCardsInSecondDeal = 1;
    [Tooltip("第二次发牌的黑牌数量（0-1）")]
    [SerializeField] [Range(0, 1)] private int blackCardsInSecondDeal = 1;

    [Header("Play Events")]
    public UnityEvent OnPlaySuccess;              // 出牌成功
    public UnityEvent OnPlayFailed;               // 出牌失败（牌型不符合）
    public UnityEvent OnGoldCardWin;              // 金牌胜利
    public UnityEvent OnBlackCardPlayAttempt;     // 尝试打出黑牌

    [Header("Discard Events")]
    public UnityEvent OnDiscardSuccess;           // 弃牌成功
    public UnityEvent OnBlackCardDiscardAttempt;  // 尝试弃掉黑牌
    public UnityEvent<int> OnDiscardLimitExceeded; // 预留：超过弃牌上限

    [Header("Selection Events")]
    public UnityEvent<int> OnSelectionChanged;    // 选中数量变化

    // 特殊发牌模式状态跟踪
    private int dealCount = 0;  // 当前是第几次发牌

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return; 
        }
        
        Instance = this;
    }

    /// <summary>
    /// 注册或注销卡牌的选中状态
    /// </summary>
    /// <param name="card">卡牌对象</param>
    /// <param name="isSelected">是否被选中</param>
    public void RegisterCardSelection(Card card, bool isSelected)
    {
        if (card == null)
            return;

        if (isSelected)
        {
            // 添加到选中列表（如果不存在）
            if (!selectedCards.Contains(card))
            {
                selectedCards.Add(card);
                Debug.Log($"卡牌 {card.name} 被选中。当前选中数量: {selectedCards.Count}");
            }
        }
        else
        {
            // 从选中列表移除
            if (selectedCards.Contains(card))
            {
                selectedCards.Remove(card);
                Debug.Log($"卡牌 {card.name} 取消选中。当前选中数量: {selectedCards.Count}");
            }
        }
    }

    /// <summary>
    /// 取消所有卡牌的选中状态
    /// </summary>
    public void DeselectAllCards()
    {
        // 创建副本以避免在遍历时修改列表
        List<Card> cardsToDeselect = new List<Card>(selectedCards);
        
        foreach (Card card in cardsToDeselect)
        {
            if (card != null)
            {
                card.Deselect();
            }
        }

        selectedCards.Clear();
        Debug.Log("所有卡牌已取消选中");
    }

    /// <summary>
    /// 获取当前选中的卡牌列表（副本）
    /// </summary>
    /// <returns>选中卡牌列表的副本</returns>
    public List<Card> GetSelectedCards()
    {
        return new List<Card>(selectedCards);
    }

    /// <summary>
    /// 获取当前选中卡牌的数量
    /// </summary>
    /// <returns>选中数量</returns>
    public int GetSelectedCount()
    {
        return selectedCards.Count;
    }

    /// <summary>
    /// 尝试打出选中的卡牌（通过按钮调用）
    /// </summary>
    public void AttemptPlayCards()
    {
        List<Card> selected = GetSelectedCards();

        if (selected.Count == 0)
        {
            Debug.Log("没有选中任何卡牌");
            return;
        }

        // 过滤掉空卡
        selected = selected.Where(c => c != null && !c.IsEmpty()).ToList();

        if (selected.Count == 0)
        {
            Debug.LogWarning("选中的卡牌都是空的");
            return;
        }

        // 检查是否包含黑牌
        bool hasBlackCard = selected.Any(c => c.cardData.cardType == CardType.Black);
        if (hasBlackCard)
        {
            Debug.Log("选中了黑牌，无法打出！");
            OnBlackCardPlayAttempt?.Invoke();
            return;
        }

        // 检查金牌胜利条件（恰好3张金牌）
        if (selected.Count == 3 && selected.All(c => c.cardData.cardType == CardType.Gold))
        {
            Debug.Log("金牌胜利条件达成！");
            StartCoroutine(PlayCardsAnimation(selected, true));
            return;
        }

        // 德扑规则判定
        if (PokerRuleEvaluator.CanPlayCards(selected))
        {
            Debug.Log("牌型有效，开始打出");
            StartCoroutine(PlayCardsAnimation(selected, false));
        }
        else
        {
            Debug.Log("牌型无效，无法打出");
            OnPlayFailed?.Invoke();
        }
    }

    /// <summary>
    /// 尝试弃掉选中的卡牌（通过按钮调用）
    /// </summary>
    public void AttemptDiscardCards()
    {
        List<Card> selected = GetSelectedCards();

        if (selected.Count == 0)
        {
            Debug.Log("没有选中任何卡牌");
            return;
        }

        // 过滤掉空卡
        selected = selected.Where(c => c != null && !c.IsEmpty()).ToList();

        if (selected.Count == 0)
        {
            Debug.LogWarning("选中的卡牌都是空的");
            return;
        }

        // 检查是否包含黑牌
        bool hasBlackCard = selected.Any(c => c.cardData.cardType == CardType.Black);
        if (hasBlackCard)
        {
            Debug.Log("选中了黑牌，无法弃掉！");
            OnBlackCardDiscardAttempt?.Invoke();
            return;
        }

        // 检查是否所有卡都可以弃掉
        bool allCanDiscard = selected.All(c => c.cardData.canBeDiscarded);
        if (!allCanDiscard)
        {
            Debug.LogWarning("部分卡牌无法弃掉");
            return;
        }

        Debug.Log($"弃掉 {selected.Count} 张卡牌");
        StartCoroutine(DiscardCardsAnimation(selected));
    }

    /// <summary>
    /// 打出卡牌的动画协程
    /// </summary>
    private IEnumerator PlayCardsAnimation(List<Card> cards, bool isGoldWin)
    {
        // 计算屏幕中心位置（世界坐标）
        Vector3 center = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 10));
        center += centerScreenOffset;

        // 杀掉所有卡牌的现有动画
        foreach (var card in cards)
        {
            DOTween.Kill(card.transform);
            if (card.cardVisual != null)
            {
                DOTween.Kill(card.cardVisual.transform);
            }
        }

        // 移动到中心
        foreach (var card in cards)
        {
            card.transform.DOMove(center, cardMoveSpeed).SetEase(Ease.OutQuad);
            
            // 标记到DeckManager
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.MarkAsPlayed(card.cardData);
            }
        }

        // 如果是金牌胜利，先触发胜利事件
        if (isGoldWin)
        {
            OnGoldCardWin?.Invoke();
        }
        else
        {
            OnPlaySuccess?.Invoke();
        }

        // 等待停留时间
        yield return new WaitForSeconds(cardStayDuration);

        // 渐隐效果
        foreach (var card in cards)
        {
            if (card.cardVisual != null && card.cardVisual.cardImage != null)
            {
                // 使用DOTween淡出卡牌图片
                card.cardVisual.cardImage.DOFade(0f, cardFadeOutDuration).SetEase(Ease.InQuad);
            }
        }
        
        // 等待渐隐完成
        yield return new WaitForSeconds(cardFadeOutDuration);

        // 清空卡牌数据（但保留slot）
        foreach (var card in cards)
        {
            card.ClearCard();
        }

        // 取消所有选中
        DeselectAllCards();

        Debug.Log("打出动画完成");
        
        // 自动补牌
        if (autoRefillCards)
        {
            yield return new WaitForSeconds(refillDelay);
            RefillEmptySlots();
        }
    }

    /// <summary>
    /// 弃牌的动画协程
    /// </summary>
    private IEnumerator DiscardCardsAnimation(List<Card> cards)
    {
        // 计算屏幕中心位置（世界坐标）
        Vector3 center = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 10));
        center += centerScreenOffset;

        // 杀掉所有卡牌的现有动画
        foreach (var card in cards)
        {
            DOTween.Kill(card.transform);
            if (card.cardVisual != null)
            {
                DOTween.Kill(card.cardVisual.transform);
            }
        }

        // 移动到中心
        foreach (var card in cards)
        {
            card.transform.DOMove(center, cardMoveSpeed).SetEase(Ease.OutQuad);

            // 标记到DeckManager
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.MarkAsDiscarded(card.cardData);
            }
        }

        OnDiscardSuccess?.Invoke();

        // 等待停留时间
        yield return new WaitForSeconds(cardStayDuration);

        // 渐隐效果
        foreach (var card in cards)
        {
            if (card.cardVisual != null && card.cardVisual.cardImage != null)
            {
                // 使用DOTween淡出卡牌图片
                card.cardVisual.cardImage.DOFade(0f, cardFadeOutDuration).SetEase(Ease.InQuad);
            }
        }
        
        // 等待渐隐完成
        yield return new WaitForSeconds(cardFadeOutDuration);

        // 清空卡牌数据（但保留slot）
        foreach (var card in cards)
        {
            card.ClearCard();
        }

        // 取消所有选中
        DeselectAllCards();

        Debug.Log("弃牌动画完成");
        
        // 自动补牌
        if (autoRefillCards)
        {
            yield return new WaitForSeconds(refillDelay);
            RefillEmptySlots();
        }
    }
    
    /// <summary>
    /// 自动补齐空槽位的卡牌
    /// </summary>
    private void RefillEmptySlots()
    {
        // 查找HorizontalCardHolder
        HorizontalCardHolder cardHolder = FindObjectOfType<HorizontalCardHolder>();

        if (cardHolder == null)
        {
            Debug.LogWarning("未找到HorizontalCardHolder，无法自动补牌");
            return;
        }

        // 调用HorizontalCardHolder的发牌方法
        cardHolder.DealCards();

        Debug.Log("已触发自动补牌");
    }

    // 特殊发牌模式相关的公开属性和方法
    public bool EnableSpecialDealMode => enableSpecialDealMode;
    public int DealCount => dealCount;
    public int GoldCardsInSecondDeal => goldCardsInSecondDeal;
    public int BlackCardsInSecondDeal => blackCardsInSecondDeal;

    public void IncrementDealCount()
    {
        dealCount++;
        Debug.Log($"发牌计数增加到: {dealCount}");
    }

    public void SetGoldCardsInSecondDeal(int count)
    {
        goldCardsInSecondDeal = Mathf.Clamp(count, 0, 3);
    }

    public void SetBlackCardsInSecondDeal(int count)
    {
        blackCardsInSecondDeal = Mathf.Clamp(count, 0, 1);
    }

    /// <summary>
    /// 重置发牌计数（游戏重新开始时调用）
    /// </summary>
    public void ResetDealCount()
    {
        dealCount = 0;
        Debug.Log("发牌计数已重置");
    }

}
