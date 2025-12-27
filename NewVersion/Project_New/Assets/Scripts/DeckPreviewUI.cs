using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckPreviewUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject deckPreviewPanel;       // 预览面板
    [SerializeField] private Transform gridParent;              // Grid的父物体
    [SerializeField] private GameObject cardIconPrefab;         // 卡牌图标预制体

    [Header("Card Icon Settings")]
    [SerializeField] private Color normalColor = Color.white;                       // 在牌堆：正常颜色
    [SerializeField] private Color inHandColor = new Color(1f, 1f, 1f, 0.5f);      // 在手牌：半透明
    [SerializeField] private Color playedColor = new Color(0.5f, 0.5f, 0.5f, 0.3f); // 已打出：灰色半透明

    private Dictionary<CardData, Image> cardIcons = new Dictionary<CardData, Image>();
    private bool isInitialized = false;

    private void Start()
    {
        // 默认隐藏预览面板
        if (deckPreviewPanel != null)
        {
            deckPreviewPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 初始化牌堆预览UI（创建所有卡牌图标）
    /// </summary>
    public void Initialize()
    {
        if (isInitialized)
        {
            Debug.LogWarning("牌堆预览UI已经初始化过了");
            return;
        }

        if (DeckManager.Instance == null)
        {
            Debug.LogError("DeckManager不存在！无法初始化牌堆预览");
            return;
        }

        if (gridParent == null)
        {
            Debug.LogError("gridParent未设置！");
            return;
        }

        if (cardIconPrefab == null)
        {
            Debug.LogError("cardIconPrefab未设置！");
            return;
        }

        // 清空现有的图标
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }
        cardIcons.Clear();

        // 按顺序创建40张卡的图标
        // 排列顺序：4种花色（红心、方块、梅花、黑桃）各9张 + 黑牌1张 + 金牌3张
        var allCards = DeckManager.Instance.allCardsDatabase;

        if (allCards == null || allCards.Count == 0)
        {
            Debug.LogError("DeckManager的卡牌数据库为空！");
            return;
        }

        // 按以下顺序排列：
        // 1. 红心 1-9
        CreateCardsForSuit(allCards, Suit.Heart);
        
        // 2. 方块 1-9
        CreateCardsForSuit(allCards, Suit.Diamond);
        
        // 3. 梅花 1-9
        CreateCardsForSuit(allCards, Suit.Club);
        
        // 4. 黑桃 1-9
        CreateCardsForSuit(allCards, Suit.Spade);
        
        // 5. 黑牌
        CreateCardsForType(allCards, CardType.Black);
        
        // 6. 金牌
        CreateCardsForType(allCards, CardType.Gold);

        isInitialized = true;
        Debug.Log($"牌堆预览UI初始化完成，共创建 {cardIcons.Count} 个图标");
    }

    /// <summary>
    /// 为指定花色创建卡牌图标
    /// </summary>
    private void CreateCardsForSuit(List<CardData> allCards, Suit suit)
    {
        var suitCards = allCards.FindAll(c => c.cardType == CardType.Basic && c.suit == suit);
        
        // 按rank排序
        suitCards.Sort((a, b) => a.rank.CompareTo(b.rank));

        foreach (var cardData in suitCards)
        {
            CreateCardIcon(cardData);
        }
    }

    /// <summary>
    /// 为指定类型创建卡牌图标
    /// </summary>
    private void CreateCardsForType(List<CardData> allCards, CardType type)
    {
        var typeCards = allCards.FindAll(c => c.cardType == type);

        foreach (var cardData in typeCards)
        {
            CreateCardIcon(cardData);
        }
    }

    /// <summary>
    /// 创建单个卡牌图标
    /// </summary>
    private void CreateCardIcon(CardData cardData)
    {
        GameObject icon = Instantiate(cardIconPrefab, gridParent);
        Image img = icon.GetComponent<Image>();

        if (img != null && cardData.sprite != null)
        {
            img.sprite = cardData.sprite;
            img.color = normalColor; // 初始为正常颜色
            cardIcons[cardData] = img;
        }
        else
        {
            Debug.LogWarning($"卡牌 {cardData.cardName} 缺少Image组件或sprite");
        }
    }

    /// <summary>
    /// 打开牌堆预览面板
    /// </summary>
    public void OpenPreview()
    {
        if (!isInitialized)
        {
            Initialize();
        }

        if (deckPreviewPanel != null)
        {
            deckPreviewPanel.SetActive(true);
            UpdateAllCardStatus();
            Debug.Log("牌堆预览面板已打开");
        }
    }

    /// <summary>
    /// 关闭牌堆预览面板
    /// </summary>
    public void ClosePreview()
    {
        if (deckPreviewPanel != null)
        {
            deckPreviewPanel.SetActive(false);
            Debug.Log("牌堆预览面板已关闭");
        }
    }

    /// <summary>
    /// 切换牌堆预览面板的显示状态
    /// </summary>
    public void TogglePreview()
    {
        if (deckPreviewPanel != null)
        {
            if (deckPreviewPanel.activeSelf)
            {
                ClosePreview();
            }
            else
            {
                OpenPreview();
            }
        }
    }

    /// <summary>
    /// 更新所有卡牌的显示状态
    /// </summary>
    public void UpdateAllCardStatus()
    {
        if (DeckManager.Instance == null)
            return;

        foreach (var kvp in cardIcons)
        {
            CardData data = kvp.Key;
            Image img = kvp.Value;

            if (img == null)
                continue;

            // 判断卡牌状态
            if (DeckManager.Instance.IsPlayed(data) || DeckManager.Instance.IsDiscarded(data))
            {
                // 已打出或已弃掉：灰色半透明
                img.color = playedColor;
            }
            else if (DeckManager.Instance.IsInHand(data))
            {
                // 在手牌中：半透明
                img.color = inHandColor;
            }
            else
            {
                // 在牌堆中：正常颜色
                img.color = normalColor;
            }
        }
    }

    /// <summary>
    /// 更新单张卡牌的显示状态
    /// </summary>
    public void UpdateCardStatus(CardData card)
    {
        if (card == null || !cardIcons.ContainsKey(card))
            return;

        Image img = cardIcons[card];
        if (img == null)
            return;

        if (DeckManager.Instance == null)
            return;

        // 判断卡牌状态
        if (DeckManager.Instance.IsPlayed(card) || DeckManager.Instance.IsDiscarded(card))
        {
            img.color = playedColor;
        }
        else if (DeckManager.Instance.IsInHand(card))
        {
            img.color = inHandColor;
        }
        else
        {
            img.color = normalColor;
        }
    }
}
