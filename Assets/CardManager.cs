using UnityEngine;
using UnityEngine.UI; // 引入 UI 命名空间
using System.Collections.Generic; // 引入泛型集合命名空间

public class CardManager : MonoBehaviour
{
    public static CardManager Instance; // 单例模式
    private List<CardSelectable3D> selectedCards = new List<CardSelectable3D>(); // 当前选中的卡牌列表

    void Awake()
    {
        // 设置单例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 切换场景时不销毁
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 自动寻找并绑定按钮
        AutoBindButtons();
    }

    // 自动寻找场景中的 Play 按钮并绑定事件
    private void AutoBindButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>();
        bool found = false;

        foreach (Button btn in buttons)
        {
            // 检查按钮名字是否包含 "play" (忽略大小写)
            if (btn.gameObject.name.ToLower().Contains("play"))
            {
                // 先移除所有监听器，防止重复
                btn.onClick.RemoveListener(PlaySelectedCard);
                // 添加监听器
                btn.onClick.AddListener(PlaySelectedCard);
                Debug.Log($"CardManager: 已自动绑定按钮 {btn.gameObject.name} 到打牌功能！");
                found = true;
            }
        }

        if (!found)
        {
            Debug.LogError("CardManager: 警告！场景中没找到名字包含 'play' 的按钮！请检查按钮名称。");
        }
    }

    // 添加选中的卡牌
    public void AddSelectedCard(CardSelectable3D card)
    {
        if (!selectedCards.Contains(card))
        {
            selectedCards.Add(card);
            Debug.Log($"CardManager: 添加选中的卡牌 - {card.gameObject.name}. 当前选中数量: {selectedCards.Count}");
        }
    }

    // 移除选中的卡牌
    public void RemoveSelectedCard(CardSelectable3D card)
    {
        if (selectedCards.Contains(card))
        {
            selectedCards.Remove(card);
            Debug.Log($"CardManager: 移除选中的卡牌 - {card.gameObject.name}. 当前选中数量: {selectedCards.Count}");
        }
    }

    // 打出所有选中的卡牌（供按钮调用）
    public void PlaySelectedCard()
    {
        Debug.Log("CardManager: PlaySelectedCard 被调用！");
        
        if (selectedCards.Count > 0)
        {
            Debug.Log($"CardManager: 准备打出 {selectedCards.Count} 张卡牌");
            
            // 创建一个临时列表来存储要打出的卡牌，因为在遍历过程中修改原列表是不安全的
            // (PlayCard方法内部可能会改变选中状态导致从列表中移除)
            List<CardSelectable3D> cardsToPlay = new List<CardSelectable3D>(selectedCards);
            
            foreach (var card in cardsToPlay)
            {
                if (card != null)
                {
                    card.PlayCard();
                }
            }
            
            selectedCards.Clear(); // 打出后清空列表
        }
        else
        {
            Debug.Log("CardManager: 没有选中的卡牌！");
        }
    }
}
