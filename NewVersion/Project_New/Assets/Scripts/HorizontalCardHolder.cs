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

        int dealtCount = 0;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].IsEmpty())
            {
                CardData drawnCard = DeckManager.Instance.DrawCard();
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

        Debug.Log($"发牌完成，共发了 {dealtCount} 张牌");
    }

    private void BeginDrag(Card card)
    {
        selectedCard = card;
    }


    void EndDrag(Card card)
    {
        if (selectedCard == null)
            return;

        selectedCard.transform.DOLocalMove(selectedCard.selected ? new Vector3(0,selectedCard.selectionOffset,0) : Vector3.zero, tweenCardReturn ? .15f : 0).SetEase(Ease.OutBack);

        rect.sizeDelta += Vector2.right;
        rect.sizeDelta -= Vector2.right;

        selectedCard = null;

    }

    void CardPointerEnter(Card card)
    {
        hoveredCard = card;

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
