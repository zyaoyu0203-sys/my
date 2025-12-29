using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;
using System.Linq;

public class HorizontalCardHolder : MonoBehaviour
{
    public AudioSource audio_hover;

    [SerializeField] private Card selectedCard;
    [SerializeReference] private Card hoveredCard;

    [SerializeField] private GameObject slotPrefab;
    private RectTransform rect;

    [Header("Spawn Settings")]
    [SerializeField] private int cardsToSpawn = 7;
    public List<Card> cards;

    [Header("Deal Settings")]
    [SerializeField] private bool dealOnStart = true;  // 游戏开始时自动发牌
    [SerializeField] private float dealDelay = 0.1f;   // 每张牌之间的延迟

    bool isCrossing = false;
    [SerializeField] private bool tweenCardReturn = true;

    void Start()
    {
        for (int i = 0; i < cardsToSpawn; i++)
        {
            Instantiate(slotPrefab, transform);
        }

        rect = GetComponent<RectTransform>();
        cards = GetComponentsInChildren<Card>().ToList();

        int cardCount = 0;

        foreach (Card card in cards)
        {
            card.PointerEnterEvent.AddListener(CardPointerEnter);
            card.PointerExitEvent.AddListener(CardPointerExit);
            card.BeginDragEvent.AddListener(BeginDrag);
            card.EndDragEvent.AddListener(EndDrag);
            card.name = cardCount.ToString();
            cardCount++;
        }

        StartCoroutine(Frame());

        IEnumerator Frame()
        {
            yield return new WaitForSecondsRealtime(.1f);
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].cardVisual != null)
                    cards[i].cardVisual.UpdateIndex(transform.childCount);
            }

            // 初始化牌堆并发牌
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.InitializeDeck();

                if (dealOnStart)
                {
                    StartCoroutine(DealInitialHand());
                }
            }
            else
            {
                Debug.LogError("DeckManager未找到！无法发牌");
            }
        }
    }

    /// <summary>
    /// 初始发牌协程
    /// </summary>
    private IEnumerator DealInitialHand()
    {
        Debug.Log("开始初始发牌...");

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].IsEmpty())
            {
                CardData drawnCard = DeckManager.Instance.DrawCard();
                if (drawnCard != null)
                {
                    cards[i].SetCardData(drawnCard);
                    yield return new WaitForSeconds(dealDelay);
                }
                else
                {
                    Debug.LogWarning("牌堆已空，无法继续发牌");
                    break;
                }
            }
        }

        Debug.Log("初始发牌完成");
    }

    /// <summary>
    /// 手动发牌（通过按钮调用）- 补充手牌到满
    /// </summary>
    public void DealCards()
    {
        StartCoroutine(DealCardsCoroutine());
    }

    /// <summary>
    /// 发牌协程（补充空位）
    /// </summary>
    private IEnumerator DealCardsCoroutine()
    {
        if (DeckManager.Instance == null)
        {
            Debug.LogError("DeckManager不存在！");
            yield break;
        }

        // 检查是否启用特殊发牌模式
        bool useSpecialMode = CardManager.Instance != null && CardManager.Instance.EnableSpecialDealMode;
        int dealCount = useSpecialMode ? CardManager.Instance.DealCount : -1;

        int dealtCount = 0;
        int goldDealt = 0;
        int blackDealt = 0;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].IsEmpty())
            {
                CardData drawnCard = null;

                // 根据发牌次数决定发牌策略
                if (useSpecialMode && dealCount == 0)
                {
                    // 第一次：只发基础牌
                    drawnCard = DrawBasicCard();
                    if (drawnCard == null)
                    {
                        Debug.LogWarning("没有可用的基础牌，使用随机抽牌");
                        drawnCard = DeckManager.Instance.DrawCard();
                    }
                }
                else if (useSpecialMode && dealCount == 1)
                {
                    // 第二次：发金牌和黑牌
                    int goldTarget = CardManager.Instance.GoldCardsInSecondDeal;
                    int blackTarget = CardManager.Instance.BlackCardsInSecondDeal;

                    if (goldDealt < goldTarget)
                    {
                        drawnCard = DrawSpecificCard(CardType.Gold);
                        if (drawnCard != null) goldDealt++;
                    }
                    else if (blackDealt < blackTarget)
                    {
                        drawnCard = DrawSpecificCard(CardType.Black);
                        if (drawnCard != null) blackDealt++;
                    }

                    // 如果金牌和黑牌都发完了，或者找不到指定类型的牌
                    if (drawnCard == null)
                    {
                        drawnCard = DeckManager.Instance.DrawCard();
                    }
                }
                else
                {
                    // 第三次及以后：正常随机
                    drawnCard = DeckManager.Instance.DrawCard();
                }

                if (drawnCard != null)
                {
                    cards[i].SetCardData(drawnCard);
                    dealtCount++;
                    yield return new WaitForSeconds(dealDelay);
                }
                else
                {
                    Debug.LogWarning("牌堆已空，无法继续发牌");
                    break;
                }
            }
        }

        // 增加发牌计数
        if (useSpecialMode && CardManager.Instance != null)
        {
            CardManager.Instance.IncrementDealCount();
        }

        Debug.Log($"发牌完成，共发了 {dealtCount} 张牌");
    }

    /// <summary>
    /// 抽取一张基础牌（CardType.Basic）
    /// </summary>
    private CardData DrawBasicCard()
    {
        if (DeckManager.Instance == null) return null;

        List<CardData> deckCards = DeckManager.Instance.GetDeckCards();
        CardData basicCard = deckCards.FirstOrDefault(card => card.cardType == CardType.Basic);

        if (basicCard != null)
        {
            // 从牌堆中抽出这张牌
            DeckManager.Instance.DrawCard(); // 先随机抽一张
            // 找到并返回基础牌
            CardData drawnCard = DeckManager.Instance.DrawCard();
            while (drawnCard != null && drawnCard.cardType != CardType.Basic)
            {
                // 如果不是基础牌，继续抽
                drawnCard = DeckManager.Instance.DrawCard();
            }
            return drawnCard;
        }

        return null;
    }

    /// <summary>
    /// 抽取指定类型的牌
    /// </summary>
    private CardData DrawSpecificCard(CardType targetType)
    {
        if (DeckManager.Instance == null) return null;

        List<CardData> deckCards = DeckManager.Instance.GetDeckCards();
        CardData targetCard = deckCards.FirstOrDefault(card => card.cardType == targetType);

        if (targetCard != null)
        {
            // 从牌堆中抽出这张牌
            CardData drawnCard = DeckManager.Instance.DrawCard();
            while (drawnCard != null && drawnCard.cardType != targetType)
            {
                // 如果不是目标类型，继续抽
                drawnCard = DeckManager.Instance.DrawCard();
            }
            return drawnCard;
        }

        return null;
    }

    private void BeginDrag(Card card)
    {
        selectedCard = card;
    }


    void EndDrag(Card card)
    {
        if (selectedCard == null)
            return;

        selectedCard.transform.DOLocalMove(selectedCard.selected ? new Vector3(0, selectedCard.selectionOffset, 0) : Vector3.zero, tweenCardReturn ? .15f : 0).SetEase(Ease.OutBack);

        rect.sizeDelta += Vector2.right;
        rect.sizeDelta -= Vector2.right;

        selectedCard = null;

    }

    void CardPointerEnter(Card card)
    {
        hoveredCard = card;

        if (audio_hover != null)
            audio_hover.Play();
    }

    void CardPointerExit(Card card)
    {
        hoveredCard = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (hoveredCard != null)
            {
                Destroy(hoveredCard.transform.parent.gameObject);
                cards.Remove(hoveredCard);

            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            // 使用 CardManager 统一管理取消选中
            if (CardManager.Instance != null)
            {
                CardManager.Instance.DeselectAllCards();
            }
        }

        if (selectedCard == null)
            return;

        if (isCrossing)
            return;

        for (int i = 0; i < cards.Count; i++)
        {

            if (selectedCard.transform.position.x > cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() < cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }

            if (selectedCard.transform.position.x < cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() > cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }
        }
    }

    void Swap(int index)
    {
        isCrossing = true;

        Transform focusedParent = selectedCard.transform.parent;
        Transform crossedParent = cards[index].transform.parent;

        cards[index].transform.SetParent(focusedParent);
        cards[index].transform.localPosition = cards[index].selected ? new Vector3(0, cards[index].selectionOffset, 0) : Vector3.zero;
        selectedCard.transform.SetParent(crossedParent);

        isCrossing = false;

        if (cards[index].cardVisual == null)
            return;

        bool swapIsRight = cards[index].ParentIndex() > selectedCard.ParentIndex();
        cards[index].cardVisual.Swap(swapIsRight ? -1 : 1);

        //Updated Visual Indexes
        foreach (Card card in cards)
        {
            card.cardVisual.UpdateIndex(transform.childCount);
        }
    }

}
