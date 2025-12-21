using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    // 追踪所有被选中的卡牌
    private List<Card> selectedCards = new List<Card>();

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

}
