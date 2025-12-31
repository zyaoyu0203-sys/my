using UnityEngine;
using TMPro;

public class DiscardCounter : MonoBehaviour
{
    public static DiscardCounter Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI counterText;  // 如果用普通Text，改成 public Text counterText;

    [Header("Settings")]
    public int maxDiscards = 10;

    private int currentDiscards;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        currentDiscards = maxDiscards;
        UpdateUI();
    }

    /// <summary>
    /// 弃牌一次，减1
    /// </summary>
    public void UseDiscard()
    {
        if (currentDiscards > 0)
        {
            currentDiscards--;
            UpdateUI();
            Debug.Log($"弃牌次数减1，剩余: {currentDiscards}");
        }
        else
        {
            Debug.LogWarning("弃牌次数已用完！");
        }
    }

    /// <summary>
    /// 检查是否还能弃牌
    /// </summary>
    public bool CanDiscard()
    {
        return currentDiscards > 0;
    }

    /// <summary>
    /// 获取剩余次数
    /// </summary>
    public int GetRemainingDiscards()
    {
        return currentDiscards;
    }

    /// <summary>
    /// 重置次数（游戏重新开始时）
    /// </summary>
    public void ResetCounter()
    {
        currentDiscards = maxDiscards;
        UpdateUI();
        Debug.Log("弃牌次数已重置");
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (counterText != null)
        {
            counterText.text = $"Discard: {currentDiscards}";
            
            // 可选：次数少时变红色警告
            if (currentDiscards <= 3)
            {
                counterText.color = Color.red;
            }
            else if (currentDiscards <= 5)
            {
                counterText.color = Color.yellow;
            }
            else
            {
                counterText.color = Color.white;
            }
        }
    }
}