using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [Header("Card Database")]
    [Tooltip("拖拽所有40个CardData到这里")]
    public List<CardData> allCardsDatabase = new List<CardData>();

    [Header("Deck State")]
    private List<CardData> deckPool = new List<CardData>();          // 当前牌堆（未发出的牌）
    private List<CardData> inHand = new List<CardData>();            // 在手牌中
    private List<CardData> playedCards = new List<CardData>();       // 已打出
    private List<CardData> discardedCards = new List<CardData>();    // 已弃掉

    [Header("Events")]
    public UnityEvent OnDeckInitialized;
    public UnityEvent OnDeckEmpty;
    public UnityEvent<int> OnCardDrawn;  // 参数：牌堆剩余数量

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
    /// 初始化牌堆（游戏开始时调用）
    /// </summary>
    public void InitializeDeck()
    {
        deckPool.Clear();
        inHand.Clear();
        playedCards.Clear();
        discardedCards.Clear();

        // 将所有卡牌加入牌堆
        deckPool.AddRange(allCardsDatabase);

        // 洗牌
        ShuffleDeck();

        Debug.Log($"牌堆已初始化，共 {deckPool.Count} 张卡牌");
        OnDeckInitialized?.Invoke();
    }

    /// <summary>
    /// 洗牌（Fisher-Yates 算法）
    /// </summary>
    private void ShuffleDeck()
    {
        for (int i = deckPool.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            CardData temp = deckPool[i];
            deckPool[i] = deckPool[randomIndex];
            deckPool[randomIndex] = temp;
        }
    }

    /// <summary>
    /// 抽一张牌
    /// </summary>
    /// <returns>抽到的卡牌数据，如果牌堆为空返回null</returns>
    public CardData DrawCard()
    {
        if (deckPool.Count == 0)
        {
            Debug.LogWarning("牌堆已空！无法抽牌");
            OnDeckEmpty?.Invoke();
            return null;
        }

        CardData drawn = deckPool[0];
        deckPool.RemoveAt(0);
        inHand.Add(drawn);

        Debug.Log($"抽到卡牌: {drawn.cardName}, 牌堆剩余: {deckPool.Count}");
        OnCardDrawn?.Invoke(deckPool.Count);

        return drawn;
    }

    /// <summary>
    /// 标记卡牌为已打出
    /// </summary>
    public void MarkAsPlayed(CardData card)
    {
        if (card == null) return;

        inHand.Remove(card);
        if (!playedCards.Contains(card))
        {
            playedCards.Add(card);
            Debug.Log($"卡牌已打出: {card.cardName}");
        }
    }

    /// <summary>
    /// 标记卡牌为已弃掉
    /// </summary>
    public void MarkAsDiscarded(CardData card)
    {
        if (card == null) return;

        inHand.Remove(card);
        if (!discardedCards.Contains(card))
        {
            discardedCards.Add(card);
            Debug.Log($"卡牌已弃掉: {card.cardName}");
        }
    }

    /// <summary>
    /// 获取牌堆剩余数量
    /// </summary>
    public int GetDeckCount() => deckPool.Count;

    /// <summary>
    /// 获取手牌数量
    /// </summary>
    public int GetHandCount() => inHand.Count;

    /// <summary>
    /// 获取已打出卡牌数量
    /// </summary>
    public int GetPlayedCount() => playedCards.Count;

    /// <summary>
    /// 获取已弃掉卡牌数量
    /// </summary>
    public int GetDiscardedCount() => discardedCards.Count;

    /// <summary>
    /// 获取牌堆剩余卡牌（只读）
    /// </summary>
    public List<CardData> GetDeckCards() => new List<CardData>(deckPool);

    /// <summary>
    /// 获取手牌中的卡牌（只读）
    /// </summary>
    public List<CardData> GetHandCards() => new List<CardData>(inHand);

    /// <summary>
    /// 获取已打出的卡牌（只读）
    /// </summary>
    public List<CardData> GetPlayedCards() => new List<CardData>(playedCards);

    /// <summary>
    /// 获取已弃掉的卡牌（只读）
    /// </summary>
    public List<CardData> GetDiscardedCards() => new List<CardData>(discardedCards);

    /// <summary>
    /// 检查某张卡是否在牌堆中
    /// </summary>
    public bool IsInDeck(CardData card) => deckPool.Contains(card);

    /// <summary>
    /// 检查某张卡是否在手牌中
    /// </summary>
    public bool IsInHand(CardData card) => inHand.Contains(card);

    /// <summary>
    /// 检查某张卡是否已打出
    /// </summary>
    public bool IsPlayed(CardData card) => playedCards.Contains(card);

    /// <summary>
    /// 检查某张卡是否已弃掉
    /// </summary>
    public bool IsDiscarded(CardData card) => discardedCards.Contains(card);
}
