using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

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
    public UnityEvent OnPlaySuccess;
    public UnityEvent OnPlayFailed;
    public UnityEvent OnGoldCardWin;
    public UnityEvent OnBlackCardPlayAttempt;

    [Header("Discard Events")]
    public UnityEvent OnDiscardSuccess;
    public UnityEvent OnBlackCardDiscardAttempt;
    public UnityEvent<int> OnDiscardLimitExceeded;

    [Header("Selection Events")]
    public UnityEvent<int> OnSelectionChanged;

    private int dealCount = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return; 
        }
        
        Instance = this;
    }

    public void RegisterCardSelection(Card card, bool isSelected)
    {
        if (card == null)
            return;

        if (isSelected)
        {
            if (!selectedCards.Contains(card))
            {
                selectedCards.Add(card);
                Debug.Log($"卡牌 {card.name} 被选中。当前选中数量: {selectedCards.Count}");
            }
        }
        else
        {
            if (selectedCards.Contains(card))
            {
                selectedCards.Remove(card);
                Debug.Log($"卡牌 {card.name} 取消选中。当前选中数量: {selectedCards.Count}");
            }
        }
    }

    public void DeselectAllCards()
    {
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

    public List<Card> GetSelectedCards()
    {
        return new List<Card>(selectedCards);
    }

    public int GetSelectedCount()
    {
        return selectedCards.Count;
    }

    public void AttemptPlayCards()
    {
        List<Card> selected = GetSelectedCards();

        if (selected.Count == 0)
        {
            Debug.Log("没有选中任何卡牌");
            return;
        }

        selected = selected.Where(c => c != null && !c.IsEmpty()).ToList();

        if (selected.Count == 0)
        {
            Debug.LogWarning("选中的卡牌都是空的");
            return;
        }

        bool hasBlackCard = selected.Any(c => c.cardData.cardType == CardType.Black);
        if (hasBlackCard)
        {
            Debug.Log("选中了黑牌，无法打出！");
            OnBlackCardPlayAttempt?.Invoke();
            return;
        }

        if (selected.Count == 3 && selected.All(c => c.cardData.cardType == CardType.Gold))
        {
            Debug.Log("金牌胜利条件达成！");
            StartCoroutine(PlayCardsAnimation(selected, true));
            return;
        }

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

   public void AttemptDiscardCards()
{
    // 检查弃牌次数
    if (DiscardCounter.Instance != null && !DiscardCounter.Instance.CanDiscard())
    {
        Debug.Log("弃牌次数已用完！");
        return;
    }
    
    List<Card> selected = GetSelectedCards();

    if (selected.Count == 0)
    {
        Debug.Log("没有选中任何卡牌");
        return;
    }

    selected = selected.Where(c => c != null && !c.IsEmpty()).ToList();

    if (selected.Count == 0)
    {
        Debug.LogWarning("选中的卡牌都是空的");
        return;
    }

    bool hasBlackCard = selected.Any(c => c.cardData.cardType == CardType.Black);
    if (hasBlackCard)
    {
        Debug.Log("选中了黑牌，无法弃掉！");
        OnBlackCardDiscardAttempt?.Invoke();
        return;
    }

    bool allCanDiscard = selected.All(c => c.cardData.canBeDiscarded);
    if (!allCanDiscard)
    {
        Debug.LogWarning("部分卡牌无法弃掉");
        return;
    }

    Debug.Log($"弃掉 {selected.Count} 张卡牌");
    StartCoroutine(DiscardCardsAnimation(selected));
}

    private IEnumerator PlayCardsAnimation(List<Card> cards, bool isGoldWin)
    {
        Vector3 center = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 10));
        center += centerScreenOffset;

        foreach (var card in cards)
        {
            DOTween.Kill(card.transform);
            if (card.cardVisual != null)
            {
                DOTween.Kill(card.cardVisual.transform);
            }
        }

        foreach (var card in cards)
        {
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.MarkAsPlayed(card.cardData);
            }
        }

        if (isGoldWin)
        {
            OnGoldCardWin?.Invoke();
            
            // 触发金牌胜利流程
            if (GoldCardVictoryManager.Instance != null)
            {
                GoldCardVictoryManager.Instance.TriggerVictory(cards);
                yield break;
            }
        }
        else
        {
            OnPlaySuccess?.Invoke();
        }
        
        if (CardBurnEffect.Instance != null)
        {
            bool burnComplete = false;
            CardBurnEffect.Instance.PlayBurnEffect(cards, center, () => burnComplete = true);
            
            while (!burnComplete)
            {
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("未找到CardBurnEffect，使用默认渐隐效果");
            foreach (var card in cards)
            {
                card.transform.DOMove(center, cardMoveSpeed).SetEase(Ease.OutQuad);
            }
            yield return new WaitForSeconds(cardMoveSpeed);
            
            foreach (var card in cards)
            {
                if (card.cardVisual != null && card.cardVisual.cardImage != null)
                {
                    card.cardVisual.cardImage.DOFade(0f, cardFadeOutDuration).SetEase(Ease.InQuad);
                }
            }
            yield return new WaitForSeconds(cardFadeOutDuration);
        }

        foreach (var card in cards)
        {
            card.ClearCard();
        }

        DeselectAllCards();

        Debug.Log("打出动画完成");
        
        if (autoRefillCards)
        {
            yield return new WaitForSeconds(refillDelay);
            RefillEmptySlots();
        }
    }

    private IEnumerator DiscardCardsAnimation(List<Card> cards)
    {
        foreach (var card in cards)
        {
            DOTween.Kill(card.transform);
            if (card.cardVisual != null)
            {
                DOTween.Kill(card.cardVisual.transform);
            }
        }

        foreach (var card in cards)
        {
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.MarkAsDiscarded(card.cardData);
            }
        }

        OnDiscardSuccess?.Invoke();

         // 减少弃牌次数
        if (DiscardCounter.Instance != null)
        {
            DiscardCounter.Instance.UseDiscard();
        }

        // === 使用CardDiscardEffect ===
        if (CardDiscardEffect.Instance != null)
        {
            bool discardComplete = false;
            CardDiscardEffect.Instance.PlayDiscardEffect(cards, () => discardComplete = true);
            
            while (!discardComplete)
            {
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("未找到CardDiscardEffect，使用默认效果");
            
            Vector3 center = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 10));
            center += centerScreenOffset;
            
            foreach (var card in cards)
            {
                card.transform.DOMove(center, cardMoveSpeed).SetEase(Ease.OutQuad);
            }
            yield return new WaitForSeconds(cardStayDuration);
            
            foreach (var card in cards)
            {
                if (card.cardVisual != null && card.cardVisual.cardImage != null)
                {
                    card.cardVisual.cardImage.DOFade(0f, cardFadeOutDuration).SetEase(Ease.InQuad);
                }
            }
            yield return new WaitForSeconds(cardFadeOutDuration);
        }

        foreach (var card in cards)
        {
            card.ClearCard();
        }

        DeselectAllCards();

        Debug.Log("弃牌动画完成");
        
        if (autoRefillCards)
        {
            yield return new WaitForSeconds(refillDelay);
            RefillEmptySlots();
        }
    }
    
    private void RefillEmptySlots()
    {
        HorizontalCardHolder cardHolder = FindObjectOfType<HorizontalCardHolder>();

        if (cardHolder == null)
        {
            Debug.LogWarning("未找到HorizontalCardHolder，无法自动补牌");
            return;
        }

        cardHolder.DealCards();

        Debug.Log("已触发自动补牌");
    }

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

    public void ResetDealCount()
    {
        dealCount = 0;
        Debug.Log("发牌计数已重置");
    }
}