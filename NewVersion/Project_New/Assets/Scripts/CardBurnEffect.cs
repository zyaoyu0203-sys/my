using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 独立的卡牌燃烧效果管理器
/// 不修改原有CardManager逻辑，只负责视觉效果
/// </summary>
public class CardBurnEffect : MonoBehaviour
{
    public static CardBurnEffect Instance { get; private set; }

    [Header("Sequential Animation")]
    [Tooltip("卡牌依次上升的间隔时间（秒）")]
    public float cardSequenceDelay = 0.2f;
    
    [Tooltip("卡牌在中间并列时的间距")]
    public float cardSpacing = 1.5f;

    [Header("Burn Effect")]
    [Tooltip("燃烧效果持续时间（秒）")]
    public float burnDuration = 1.5f;
    
    [Tooltip("燃烧时向上飘动的距离")]
    public float burnFloatHeight = 2f;
    
    [Tooltip("燃烧时的最大缩放")]
    public float burnScaleMax = 1.3f;
    
    [Tooltip("燃烧颜色1 - 橙色")]
    public Color burnColor1 = new Color(1f, 0.5f, 0f, 1f);
    
    [Tooltip("燃烧颜色2 - 黄色")]
    public Color burnColor2 = new Color(1f, 1f, 0f, 1f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        Debug.Log("CardBurnEffect初始化成功！");
    }

    /// <summary>
    /// 公开方法：播放卡牌燃烧效果
    /// </summary>
    /// <param name="cards">要燃烧的卡牌列表</param>
    /// <param name="targetCenter">目标中心位置</param>
    /// <param name="onComplete">完成后的回调</param>
    public void PlayBurnEffect(List<Card> cards, Vector3 targetCenter, System.Action onComplete = null)
    {
        StartCoroutine(BurnCardsSequence(cards, targetCenter, onComplete));
    }

    /// <summary>
    /// 燃烧卡牌序列（依次上升+并列+燃烧）
    /// </summary>
    private IEnumerator BurnCardsSequence(List<Card> cards, Vector3 center, System.Action onComplete)
    {
        Debug.Log($"=== 开始燃烧序列，卡牌数量: {cards.Count} ===");

        // === 第1阶段：依次上升到并列位置 ===
        float totalWidth = (cards.Count - 1) * cardSpacing;
        float startX = center.x - totalWidth / 2f;

        for (int i = 0; i < cards.Count; i++)
        {
            Card card = cards[i];
            if (card == null) continue;

            // 计算并列位置
            Vector3 targetPos = new Vector3(startX + i * cardSpacing, center.y, center.z);

            // 杀掉现有动画
            DOTween.Kill(card.transform);

            // 移动到位置
            card.transform.DOMove(targetPos, 0.5f).SetEase(Ease.OutQuad);

            // 等待一小段时间再移动下一张
            yield return new WaitForSeconds(cardSequenceDelay);
        }

        Debug.Log("所有卡牌移动完成，开始燃烧");

        // === 第2阶段：燃烧效果 ===
        foreach (var card in cards)
        {
            if (card != null)
            {
                StartCoroutine(BurnSingleCard(card));
            }
        }

        // 等待燃烧完成
        yield return new WaitForSeconds(burnDuration);

        Debug.Log("燃烧完成，开始重置");

        // === 第3阶段：重置视觉状态 ===
        foreach (var card in cards)
        {
            if (card != null)
            {
                ResetCardVisuals(card);
            }
        }

        Debug.Log("=== 燃烧序列完成 ===");

        // 完成回调
        onComplete?.Invoke();
    }

    /// <summary>
    /// 燃烧单张卡牌
    /// </summary>
    private IEnumerator BurnSingleCard(Card card)
    {
        if (card == null || card.cardVisual == null)
        {
            Debug.LogWarning("卡牌或cardVisual是null");
            yield break;
        }

        Transform cardTransform = card.transform;
        Image cardImage = card.cardVisual.cardImage;

        if (cardImage == null)
        {
            Debug.LogWarning("cardImage是null");
            yield break;
        }

        Debug.Log($"开始燃烧卡牌: {card.name}");

        Vector3 startPos = cardTransform.position;
        Vector3 originalScale = cardTransform.localScale;

        float elapsed = 0f;

        while (elapsed < burnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / burnDuration;

            // 颜色变化（白→橙→黄→透明）
            Color currentColor;
            if (t < 0.3f)
            {
                currentColor = Color.Lerp(Color.white, burnColor1, t / 0.3f);
            }
            else if (t < 0.6f)
            {
                float t2 = (t - 0.3f) / 0.3f;
                currentColor = Color.Lerp(burnColor1, burnColor2, t2);
            }
            else
            {
                float t3 = (t - 0.6f) / 0.4f;
                currentColor = Color.Lerp(burnColor2, new Color(1, 1, 0, 0), t3);
            }

            cardImage.color = currentColor;

            // 缩放变化（先放大再缩小）
            float scale;
            if (t < 0.4f)
            {
                scale = Mathf.Lerp(1f, burnScaleMax, t / 0.4f);
            }
            else
            {
                float t2 = (t - 0.4f) / 0.6f;
                scale = Mathf.Lerp(burnScaleMax, 0f, t2);
            }
            cardTransform.localScale = originalScale * scale;

            // 向上飘动
            float yOffset = Mathf.Lerp(0f, burnFloatHeight, Mathf.Pow(t, 1.5f));
            cardTransform.position = startPos + Vector3.up * yOffset;

            // 轻微旋转
            float rotation = Mathf.Sin(t * Mathf.PI * 4) * 15f * (1f - t);
            cardTransform.rotation = Quaternion.Euler(0, 0, rotation);

            yield return null;
        }

        // 确保最终透明
        cardImage.color = new Color(1, 1, 0, 0);
        Debug.Log($"卡牌 {card.name} 燃烧完成");
    }

    /// <summary>
    /// 重置卡牌视觉状态（彻底版本）
    /// </summary>
    private void ResetCardVisuals(Card card)
    {
        if (card == null) return;

        Debug.Log($"重置卡牌视觉: {card.name}");

        // 杀掉所有DOTween动画
        DOTween.Kill(card.transform);
        if (card.cardVisual != null)
        {
            DOTween.Kill(card.cardVisual.transform);
            if (card.cardVisual.cardImage != null)
            {
                DOTween.Kill(card.cardVisual.cardImage);
            }
        }

        // 重置Transform
        card.transform.localScale = Vector3.one;
        card.transform.rotation = Quaternion.identity;

        // 重置cardVisual的Image颜色为纯白且完全不透明
        if (card.cardVisual != null && card.cardVisual.cardImage != null)
        {
            card.cardVisual.cardImage.color = new Color(1f, 1f, 1f, 1f);
        }
    }
}